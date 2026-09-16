using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RimWorld;
using SYS;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace XIYUNTE
{
    // 派送箱多形态系统核心实现:形态状态组件、动态攻击/图形/统计、切换 Gizmo、高功率技能配套的装备组件与武器类。
    // 攻击数据由运行时 IVerbOwner 按当前形态提供;ThingDef 静态保存箱体形态的 tools,用于通过原版配置检查并显示默认 DPS/穿透。

    // Box_shift 是武器组件的属性类,XML 中通过 <modes> 列表配置全部形态数据。
    public class Box_shift : CompProperties
    {
        public List<WeaponMode> modes = new List<WeaponMode>();
        public Box_shift() { compClass = typeof(CompWeaponTransformer); }

        // Def 加载解析阶段为每个形态的 tool 分配稳定 id,保证 VerbTracker 生成的动词 loadID 不随加载顺序变化。
        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            if (modes == null) return;
            for (int i = 0; i < modes.Count; i++)
            {
                if (modes[i].tools == null) continue;
                for (int j = 0; j < modes[i].tools.Count; j++)
                {
                    if (modes[i].tools[j].id.NullOrEmpty())
                    {
                        modes[i].tools[j].id = "rk_deliverybox_mode" + i + "_tool" + j;
                    }
                }
            }
        }
    }

    // WeaponMode 保存单个武器形态的数据:显示名、描述、贴图、装备属性偏移、背负图片、切换音效和原版近战 tools。
    public class WeaponMode
    {
        public string label;
        public string description;
        public string texPath;
        public float drawSize = 1f;
        public string commandIcon;
        public string sheathTexPath;
        public bool sheathGraphicMulti;
        public SoundDef switchSound;
        public List<StatModifier> equippedStatOffsets = new List<StatModifier>();
        public List<Tool> tools;

        [Unsaved(false)] private Graphic graphic;
        [Unsaved(false)] private Texture2D uiIconTex;

        // 按当前形态的 texPath 构造并缓存单张贴图;shaderType 固定为 Cutout,且关闭 Thing.DrawColor 的材料染色。
        public Graphic GetGraphic(Thing parent)
        {
            if (graphic == null)
            {
                GraphicData data = new GraphicData { texPath = texPath, graphicClass = typeof(Graphic_Single), shaderType = ShaderTypeDefOf.Cutout, drawSize = new Vector2(drawSize, drawSize), ignoreThingDrawColor = true };
                graphic = data.GraphicColoredFor(parent);
            }
            return graphic;
        }

        // 按当前形态 texPath 加载并缓存 UI 图标贴图;供 Thing_DeliveryBox.UIIconOverride 使用。
        // 贴图缺失时由 ContentFinder 记录原版加载错误并返回 null,此时原版图标链路回退到 ThingDef.uiIcon。
        public Texture2D GetUITexture()
        {
            if (uiIconTex == null)
            {
                uiIconTex = ContentFinder<Texture2D>.Get(texPath);
            }
            return uiIconTex;
        }
    }

    // CompWeaponTransformer 是形态状态组件:保存当前武器实例的形态索引,并把当前形态应用到原版 Verb、SYS 背负组件与统计入口。
    public class CompWeaponTransformer : ThingComp
    {
        private static readonly FieldInfo SheathGraphicField = typeof(CompSheath).GetField("fullGraphicInt", BindingFlags.Instance | BindingFlags.NonPublic);
        private int modeIndex;
        public Box_shift Props => (Box_shift)props;
        public int CurrentModeIndex => ClampModeIndex();
        public WeaponMode CurrentMode => Props.modes[CurrentModeIndex];
        public Graphic CurrentGraphic => CurrentMode.GetGraphic(parent);
        // 当前形态的首个近战 tool 与其匹配的原版机动(Blunt→Smash,Cut→Slash);每形态只配置一个 tool,一一对应。
        public Tool CurrentTool => CurrentMode.tools?.FirstOrDefault();
        public ManeuverDef CurrentManeuver => CurrentTool?.Maneuvers.FirstOrDefault();

        // 校验形态索引:模式列表为空时直接抛异常(禁止静默运行);索引越界时记录一次错误并回退到 0。
        private int ClampModeIndex()
        {
            if (Props.modes == null || Props.modes.Count == 0) throw new InvalidOperationException(parent.def.defName + " has no configured weapon modes.");
            if (modeIndex < 0 || modeIndex >= Props.modes.Count) { Log.ErrorOnce(parent.def.defName + " has invalid mode index. Resetting to 0.", parent.thingIDNumber ^ 0x48120); modeIndex = 0; }
            return modeIndex;
        }

        // 武器生成/部署与装备时都重新应用当前形态,保证地面、装备、读档后表现一致。
        public override void PostSpawnSetup(bool respawningAfterLoad) { base.PostSpawnSetup(respawningAfterLoad); ApplyMode(); }
        public override void Notify_Equipped(Pawn pawn) { base.Notify_Equipped(pawn); ApplyMode(); }

        // 形态装备属性偏移(如 MoveSpeed):汇总当前形态 equippedStatOffsets 中目标 stat 的偏移值。
        public override float GetStatOffset(StatDef stat)
        {
            float value = 0f;
            if (CurrentMode.equippedStatOffsets != null) foreach (StatModifier item in CurrentMode.equippedStatOffsets) if (item.stat == stat) value += item.value;
            return value;
        }

        // 把原版标签替换为当前形态名称,并保留原版的 HP/品质后缀。
        public override string TransformLabel(string label)
        {
            return CurrentMode.label + GenLabel.LabelExtras(parent, includeHp: true, includeQuality: true);
        }

        // 形态切换入口:仅对已装备且为主装备的武器生效;正在预热或爆发射击时拒绝切换(避免半个攻击使用旧形态参数);
        // 切换成功后按目标形态播放切换音效,地图内以 Pawn 位置播放,非地图状态用镜头音效兜底。
        public bool TrySetMode(int index)
        {
            if (index < 0 || index >= Props.modes.Count) throw new ArgumentOutOfRangeException(nameof(index));
            Pawn_EquipmentTracker tracker = parent.ParentHolder as Pawn_EquipmentTracker;
            if (tracker?.pawn == null || tracker.pawn.equipment.Primary != parent) return false;
            CompEquippable equippable = parent.GetComp<CompEquippable>();
            // 多攻击形态：任一动词(主/副)正在爆发或预热都拒绝切换，避免半个攻击使用旧形态参数。
            if (equippable?.verbTracker?.AllVerbs != null)
            {
                foreach (Verb v in equippable.verbTracker.AllVerbs)
                    if (v.Bursting || v.WarmingUp) return false;
            }
            WeaponMode targetMode = Props.modes[index];
            modeIndex = index;
            ApplyMode();
            if (targetMode.switchSound != null)
            {
                if (tracker.pawn.MapHeld != null)
                {
                    targetMode.switchSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(tracker.pawn.Position, tracker.pawn.MapHeld, false)));
                }
                else
                {
                    targetMode.switchSound.PlayOneShot(SoundInfo.OnCamera());
                }
            }
            return true;
        }

        // 提供武器栏切换 Gizmo:只有玩家阵营角色装备且为主装备时显示;图标缺失时使用 BadTex 占位。
        public IEnumerable<Gizmo> GetModeGizmos()
        {
            Pawn_EquipmentTracker tracker = parent.ParentHolder as Pawn_EquipmentTracker;
            if (tracker?.pawn == null || tracker.pawn.Faction != Faction.OfPlayer || tracker.pawn.equipment.Primary != parent) yield break;
            Texture2D icon = CurrentMode.commandIcon.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(CurrentMode.commandIcon, false);
            yield return new Command_Action
            {
                defaultLabel = "RK_DeliveryBox_SwitchWeaponForm".Translate().ToString(),
                defaultDesc = "RK_DeliveryBox_SelectWeaponForm".Translate().ToString(),
                icon = icon ?? BaseContent.BadTex,
                action = OpenModeMenu
            };
        }

        // 浮动菜单项文本:当前形态追加绿色"装备中"标记。
        private static string GetModeMenuLabel(WeaponMode mode, bool equipped)
        {
            if (!equipped)
            {
                return mode.label;
            }

            string status = "RK_DeliveryBox_Equipped".Translate().ToString();
            return mode.label + " (<color=#66FF66>" + status + "</color>)";
        }

        // 打开形态选择浮动菜单;菜单回调只调用 TrySetMode,不在此处保存任何第二套形态状态。
        private void OpenModeMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < Props.modes.Count; i++)
            {
                int target = i;
                options.Add(new FloatMenuOption(GetModeMenuLabel(Props.modes[i], target == CurrentModeIndex), delegate { TrySetMode(target); }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        // 把当前形态应用到运行链路:替换装备组件主 Verb 的机动参数与 tool、恢复 caster,并更新 SYS 背负图形缓存。
        // 多攻击形态：逐个同步每个近战动词的 tool/maneuver/loadID；保留同一批 Verb 对象并同步 loadID，
        // 避免重建 VerbTracker 导致攻击中的 Stance 与新 Verb 对象脱节。
        private void ApplyMode()
        {
            WeaponMode mode = CurrentMode;
            CompEquippable equippable = parent.GetComp<CompEquippable>();
            if (equippable?.verbTracker?.AllVerbs == null || mode.tools == null || mode.tools.Count == 0)
            {
                Log.ErrorOnce(parent.def.defName + " mode '" + mode.label + "' is missing melee tool/maneuver data; attack verb cannot be applied.", parent.thingIDNumber ^ 0x771C0D);
                return;
            }

            Pawn_EquipmentTracker tracker = parent.ParentHolder as Pawn_EquipmentTracker;
            Pawn pawn = tracker?.pawn;
            List<Verb> verbs = equippable.verbTracker.AllVerbs;
            int count = Math.Min(verbs.Count, mode.tools.Count);
            for (int i = 0; i < count; i++)
            {
                Tool tool = mode.tools[i];
                ManeuverDef maneuver = tool.Maneuvers.FirstOrDefault();
                if (maneuver == null) continue;
                Verb verb = verbs[i];
                verb.Reset();
                verb.verbProps = maneuver.verb;
                verb.tool = tool;
                verb.maneuver = maneuver;
                verb.loadID = Verb.CalculateUniqueLoadID(equippable, tool, maneuver);
                if (pawn != null) verb.caster = pawn;
            }
            // SYS 背负图形:替换为当前形态的背负贴图,并通过反射清空组件缓存的旧图形,强制下次重新加载。
            CompSheath sheath = parent.GetComp<CompSheath>();
            if (sheath != null && !mode.sheathTexPath.NullOrEmpty())
            {
                sheath.Props.fullGraphicData = new GraphicData { texPath = mode.sheathTexPath, graphicClass = mode.sheathGraphicMulti ? typeof(Graphic_Multi) : typeof(Graphic_Single), shaderType = ShaderTypeDefOf.Cutout, drawSize = new Vector2(mode.drawSize, mode.drawSize), ignoreThingDrawColor = true };
                if (SheathGraphicField != null) SheathGraphicField.SetValue(sheath, null);
            }
        }

        // 存档:保存形态索引;读档完成阶段重新应用当前形态,保证读档后形态数据一致。
        public override void PostExposeData() { base.PostExposeData(); Scribe_Values.Look(ref modeIndex, "modeIndex", 0); if (Scribe.mode == LoadSaveMode.PostLoadInit) ApplyMode(); }
    }

    // StatPart_DeliveryBoxMoveSpeed 把当前装备武器的形态 MoveSpeed 偏移接入 Pawn 移动速度统计。
    // 原版武器组件 GetStatOffset 不作用于 Pawn 的 MoveSpeed 面板,因此通过 Patch 挂到 MoveSpeed 的 parts 上。
    public class StatPart_DeliveryBoxMoveSpeed : StatPart
    {
        // 从 StatRequest 中的 Pawn 取主装备,读取其形态组件的 MoveSpeed 偏移;无武器/无组件/偏移为 0 时返回 false。
        private static bool TryGetOffset(StatRequest request, out Pawn pawn, out float offset)
        {
            pawn = request.Thing as Pawn;
            offset = 0f;
            if (pawn?.equipment?.Primary == null) return false;
            CompWeaponTransformer transformer = pawn.equipment.Primary.GetComp<CompWeaponTransformer>();
            if (transformer == null) return false;
            offset = transformer.CurrentMode.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.MoveSpeed);
            return !Mathf.Approximately(offset, 0f);
        }

        // 移动速度最终值叠加当前形态偏移。
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (TryGetOffset(req, out _, out float offset)) val += offset;
        }

        // 面板说明:列出武器名称与形态移动速度偏移。
        public override string ExplanationPart(StatRequest req)
        {
            if (!TryGetOffset(req, out Pawn pawn, out float offset)) return null;
            return "    " + pawn.equipment.Primary.LabelCap + ": " + offset.ToStringByStyle(parentStat.toStringStyle, ToStringNumberSense.Offset);
        }
    }

    // CompEquippable_MultiForm 替代原版 CompEquippable:保留 Ability 的装备 Gizmo 链路,
    // 并作为动态 IVerbOwner 向 VerbTracker 提供当前形态的 VerbProperties 与 Tools。
    public class CompEquippable_MultiForm : CompEquippableAbility, IVerbOwner
    {
        // 取得当前装备者(仅装备中的武器可行)。
        private Pawn EquippedPawn => (parent.ParentHolder as Pawn_EquipmentTracker)?.pawn;

        // 关闭技能后给「开启」能力手动进入冷却(企划-2:冷却在关闭时进入)。
        public void StartHighPowerCooldown(int ticks)
        {
            AbilityForReading?.StartCooldown(ticks);
            EquippedPawn?.abilities?.Notify_TemporaryAbilitiesChanged();
        }

        // 卸下武器 => 视作停止技能：立即移除主高功率 Hediff（其 CompPostPostRemoved 会清护盾与「关闭」能力），
        // 并给「开启」能力进入 24 小时冷却；再次拿起时按正常冷却计时。
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if (pawn == null || pawn.health == null)
                return;
            HediffDef mainDef = DefDatabase<HediffDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower");
            if (mainDef == null)
                return;
            Hediff main = pawn.health.hediffSet.GetFirstHediffOfDef(mainDef);
            if (main == null)
                return;
            pawn.health.RemoveHediff(main);
            AbilityForReading?.StartCooldown(60000);
        }

        // 动态动词属性:近战攻击动词由 Tools 的机动生成,此处固定返回空列表,避免额外生成无 tool 的攻击动词。
        List<VerbProperties> IVerbOwner.VerbProperties => new List<VerbProperties>();

        // 动态工具列表:返回当前形态的 tools,VerbTracker 据此生成带 tool/maneuver 的原版近战动词。
        List<Tool> IVerbOwner.Tools
        {
            get
            {
                CompWeaponTransformer transformer = parent.GetComp<CompWeaponTransformer>();
                return transformer == null ? new List<Tool>() : transformer.CurrentMode.tools;
            }
        }

        // 装备 Gizmo:输出 Ability 自身的 Gizmo,再追加武器形态切换 Gizmo。
        public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetEquippedGizmosExtra()) yield return gizmo;
            CompWeaponTransformer transformer = parent.GetComp<CompWeaponTransformer>();
            if (transformer != null) foreach (Gizmo gizmo in transformer.GetModeGizmos()) yield return gizmo;
        }
    }

    // Thing_DeliveryBox 是派送箱武器实例类:动态提供当前形态的图形、描述与信息卡统计。
    public class Thing_DeliveryBox : ThingWithComps
    {
        // 形态显示缓存:按当前形态索引在首次读取时重建。形态未变化时,所有显示入口只做一次索引比较,
        // 不重复构造描述文本或查找贴图,避免进入每帧 GUI 绘制路径后产生额外的字符串分配。
        private CompWeaponTransformer transformerInt;
        private int cachedModeIndex = -1;
        private string cachedFlavor;
        private string cachedDetailed;
        private string cachedGizmoDesc;
        private Texture2D cachedIcon;

        // 完全屏蔽材料颜色：所有渲染路径(含攻击/贴图态)读 DrawColor 都得到白色,不再被材质染色；
        // 材料仍参与属性与品质加成,仅影响视觉,不含材质颜色。
        public override Color DrawColor => Color.white;

        public override Color DrawColorTwo => Color.white;

        // 地面/手持/背负渲染统一使用当前形态贴图;无形态组件时回退原版图形。
        public override Graphic Graphic { get { CompWeaponTransformer transformer = GetComp<CompWeaponTransformer>(); return transformer == null ? base.Graphic : transformer.CurrentGraphic; } }

        // 形态显示数据缓存:形态索引变化时重建描述文本与 UI 图标,否则直接复用。
        private bool TryGetDisplayCache(out CompWeaponTransformer transformer)
        {
            if (transformerInt == null) transformerInt = GetComp<CompWeaponTransformer>();
            transformer = transformerInt;
            if (transformer == null) return false;
            int index = transformer.CurrentModeIndex;
            if (cachedModeIndex != index)
            {
                cachedModeIndex = index;
                WeaponMode mode = transformer.CurrentMode;
                cachedDetailed = mode.description;
                cachedGizmoDesc = mode.description.CapitalizeFirst();
                cachedIcon = mode.GetUITexture();
                cachedFlavor = BuildFlavor(mode, transformer);
            }
            return true;
        }

        // 形态描述 + 其余组件(艺术等)的描述段落,结构与原版 ThingWithComps.DescriptionFlavor 一致。
        private string BuildFlavor(WeaponMode mode, CompWeaponTransformer transformer)
        {
            StringBuilder result = new StringBuilder(mode.description);
            foreach (ThingComp comp in AllComps)
            {
                if (comp == transformer) continue;
                string descriptionPart = comp.GetDescriptionPart();
                if (!descriptionPart.NullOrEmpty())
                {
                    result.AppendLine();
                    result.AppendLine();
                    result.Append(descriptionPart);
                }
            }
            return result.ToString();
        }

        // 装备栏武器攻击 Gizmo 由原版 Command_VerbTarget.DrawIcon 经 Widgets.ThingIcon 绘制,
        // 对有 uiIconPath 的 ThingDef 直接使用静态 def.uiIcon;此处返回缓存的当前形态贴图,使 Gizmo 图标随形态刷新。
        public override Texture UIIconOverride
        {
            get { return TryGetDisplayCache(out _) ? cachedIcon : null; }
        }

        // 物品信息卡描述:以当前形态描述为开头,再拼接其余组件(如艺术组件)的描述段落。
        public override string DescriptionFlavor
        {
            get { return TryGetDisplayCache(out _) ? cachedFlavor : base.DescriptionFlavor; }
        }

        // 装备栏 tooltip 走原版 ThingWithComps.GetTooltip(),其中读取 DescriptionDetailed(原版取静态 ThingDef 文本);
        // 此处返回当前形态描述,使装备栏说明与攻击 Gizmo 一致地随形态刷新。
        public override string DescriptionDetailed
        {
            get { return TryGetDisplayCache(out _) ? cachedDetailed : base.DescriptionDetailed; }
        }

        // 近战攻击 Gizmo 的提示描述文本(不含武器标签),供 Command.Desc 补丁使用;无形态组件时返回 null 表示保留原版文本。
        public string GizmoTooltipDescription
        {
            get { return TryGetDisplayCache(out _) ? cachedGizmoDesc : null; }
        }

        // 信息卡统计:输出基础条目、当前形态名称与移动速度偏移。
        // 近战每秒伤害/护甲穿透由原版 StatWorker 提供,并经 StatWorker_MeleeAveragePatches 按当前形态重算,不再手写伤害条目。
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            foreach (StatDrawEntry entry in base.SpecialDisplayStats()) yield return entry;
            CompWeaponTransformer transformer = GetComp<CompWeaponTransformer>();
            if (transformer == null) yield break;
            WeaponMode mode = transformer.CurrentMode;
            yield return new StatDrawEntry(StatCategoryDefOf.BasicsImportant, "RK_DeliveryBox_CurrentForm".Translate(), mode.label, mode.description, 6000);
            float moveSpeedOffset = mode.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.MoveSpeed);
            if (!Mathf.Approximately(moveSpeedOffset, 0f))
            {
                yield return new StatDrawEntry(StatCategoryDefOf.EquippedStatOffsets, StatDefOf.MoveSpeed, moveSpeedOffset, StatRequest.ForEmpty(), ToStringNumberSense.Offset);
            }
        }
    }
}
