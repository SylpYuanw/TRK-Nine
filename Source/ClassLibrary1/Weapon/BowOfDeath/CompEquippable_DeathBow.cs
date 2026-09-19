using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace XIYUNTE
{
    // 死之弓装备组件:保存模式、蓄力阶与剩余次数,产出闪弓射击、模式切换、蓄力阶选择与装填按钮,
    // 并把当前蓄力阶的精度加成叠加到武器的四段精度统计上。
    // 模式 0=普通(仅近战)、模式 1=闪弓(可远程射击);次数由死之箭补充,次数为 0 时无法远程射击。
    public class CompEquippable_DeathBow : CompEquippable
    {
        public const int MeleeMode = 0;

        public const int FlashBowMode = 1;

        private int modeIndex = MeleeMode;

        private int stageIndex;

        private int charges;

        private int pendingStageIndex;

        private int stageSwitchReadyTick;

        private bool hasPendingStageSwitch;

        private int pendingModeIndex = MeleeMode;

        private int modeSwitchReadyTick;

        private bool hasPendingModeSwitch;

        // 蓄力阶切换印迹的运行时实例:只用于刷新判定,不写入存档(mote 本身不可保存)。
        private Mote stageSwitchVfx;

        // 蓄力特效的运行时实例(主体层/基底层):存活期间由 Mote 自身按"是否仍在瞄准"续命,不写入存档。
        private Mote aimVfx;

        private Mote aimBaseVfx;

        private VerbProperties activeProps;

        private VerbProperties dormantProps;

        public Properties_DeathBow Props => props as Properties_DeathBow;

        private Pawn EquippedPawn => (parent.ParentHolder as Pawn_EquipmentTracker)?.pawn;

        public int Charges => charges;

        public bool FlashBowActive
        {
            get
            {
                ApplyPendingIfReady();
                return modeIndex == FlashBowMode;
            }
        }

        private int MaxStageIndex => Props.stages.NullOrEmpty() ? 0 : Props.stages.Count - 1;

        public int StageIndex
        {
            get
            {
                ApplyPendingIfReady();
                return Mathf.Clamp(stageIndex, 0, MaxStageIndex);
            }
        }

        public DeathBowStage CurrentStage => Props.stages.NullOrEmpty() ? null : Props.stages[StageIndex];

        // 切换耗时:等待期内新参数不生效,期间射击不可用,避免瞄准或爆发过程中参数被替换。
        // 蓄力阶切换与近战/远程模式切换各自独立计时、各自独立音效。
        public bool IsStageSwitching => hasPendingStageSwitch && Find.TickManager.TicksGame < stageSwitchReadyTick;

        public bool IsModeSwitching => hasPendingModeSwitch && Find.TickManager.TicksGame < modeSwitchReadyTick;

        public bool IsSwitching => IsStageSwitching || IsModeSwitching;

        public int SwitchTicksLeft
        {
            get
            {
                int ticksLeft = IsStageSwitching ? stageSwitchReadyTick - Find.TickManager.TicksGame : 0;
                if (IsModeSwitching)
                {
                    ticksLeft = Mathf.Max(ticksLeft, modeSwitchReadyTick - Find.TickManager.TicksGame);
                }
                return ticksLeft;
            }
        }

        // 参数来源为武器 Def 上的射击动词参数,休眠参数由它复制,不修改 Def 上的共享对象。
        private VerbProperties ActiveProps
        {
            get
            {
                if (activeProps == null)
                {
                    activeProps = parent.def.Verbs?.FirstOrDefault(v => v.verbClass == typeof(Verb_DeathBowShot));
                }
                return activeProps;
            }
        }

        private VerbProperties DormantProps
        {
            get
            {
                if (dormantProps == null && ActiveProps != null)
                {
                    dormantProps = ActiveProps.MemberwiseClone();
                    dormantProps.isPrimary = false;
                    dormantProps.hasStandardCommand = false;
                    dormantProps.targetable = false;
                }
                return dormantProps;
            }
        }

        private Verb BowVerb => verbTracker?.AllVerbs?.FirstOrDefault(v => v is Verb_DeathBowShot);

        // 把当前模式应用到射击动词:闪弓模式且仍有余弹时使用原版射击参数,原版攻击命令随之出现;
        // 其余情况换成休眠参数,武器只剩近战动词与近战命令。
        public void ApplyModeToVerb()
        {
            Verb bowVerb = BowVerb;
            if (bowVerb == null)
            {
                Log.ErrorOnce(parent.def.defName + " has no Verb_DeathBowShot; flash bow mode cannot be applied.", parent.def.shortHash ^ 0x5B08);
                return;
            }
            VerbProperties target = (FlashBowActive && charges > 0) ? ActiveProps : DormantProps;
            if (target == null || bowVerb.verbProps == target)
            {
                return;
            }
            if (bowVerb.WarmingUp || bowVerb.Bursting)
            {
                return;
            }
            bowVerb.Reset();
            bowVerb.verbProps = target;
        }

        // 出厂即携带满次数,之后只能靠死之箭补充。
        public override void PostPostMake()
        {
            base.PostPostMake();
            charges = Props.maxCharges;
            ApplyModeToVerb();
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            ApplyModeToVerb();
        }

        // 当前蓄力阶的精度加成作用于武器自身的四段精度统计,命中判定与武器面板同时读取该值。
        public override float GetStatOffset(StatDef stat)
        {
            float num = base.GetStatOffset(stat);
            DeathBowStage stage = CurrentStage;
            if (stage == null)
            {
                return num;
            }
            if (stat == StatDefOf.AccuracyTouch || stat == StatDefOf.AccuracyShort || stat == StatDefOf.AccuracyMedium || stat == StatDefOf.AccuracyLong)
            {
                num += stage.accuracyOffset;
            }
            return num;
        }

        // 精度统计的展开说明:补上当前蓄力阶的加成来源,避免只看到"基础值 → 最终值"而缺一行来源。
        public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace = "")
        {
            base.GetStatsExplanation(stat, sb, whitespace);
            DeathBowStage stage = CurrentStage;
            if (stage == null || Mathf.Approximately(stage.accuracyOffset, 0f))
            {
                return;
            }
            if (stat == StatDefOf.AccuracyTouch || stat == StatDefOf.AccuracyShort || stat == StatDefOf.AccuracyMedium || stat == StatDefOf.AccuracyLong)
            {
                sb.AppendLine(whitespace + "BowOfDeath_StatStage".Translate() + ": " + stage.accuracyOffset.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Offset));
            }
        }

        // 射击完成后扣一次次数,并给射手挂上意识增益。
        public void Notify_ShotFired(Pawn shooter)
        {
            if (charges > 0)
            {
                charges--;
            }
            ApplyModeToVerb();
            ApplyConsciousnessBoost(shooter);
        }

        // 意识增益:已存在时只刷新持续时间,避免多次射击把增益堆叠成更高数值。
        private void ApplyConsciousnessBoost(Pawn shooter)
        {
            if (shooter?.health?.hediffSet == null)
            {
                return;
            }
            HediffDef def = DeathBowDefOf.BowOfDeath_Consciousness;
            Hediff existing = shooter.health.hediffSet.GetFirstHediffOfDef(def);
            if (existing != null)
            {
                HediffCompProperties_Disappears disappearProps = def.CompProps<HediffCompProperties_Disappears>();
                HediffComp_Disappears disappears = existing.TryGetComp<HediffComp_Disappears>();
                if (disappears != null && disappearProps != null)
                {
                    disappears.SetDuration(disappearProps.disappearsAfterTicks.RandomInRange);
                }
                return;
            }
            shooter.health.AddHediff(def);
        }

        // 从角色背包取出死之箭补充次数,返回本次补充的数量。
        public int LoadAmmoFromInventory(Pawn pawn)
        {
            if (pawn?.inventory?.innerContainer == null)
            {
                return 0;
            }
            int space = Props.maxCharges - charges;
            if (space <= 0)
            {
                return 0;
            }
            int loaded = 0;
            List<Thing> arrows = pawn.inventory.innerContainer.Where(t => t.def == Props.ammoDef).ToList();
            for (int i = 0; i < arrows.Count && loaded < space; i++)
            {
                int take = Mathf.Min(arrows[i].stackCount, space - loaded);
                Thing split = arrows[i].SplitOff(take);
                if (split == null)
                {
                    continue;
                }
                split.Destroy();
                loaded += take;
            }
            charges += loaded;
            ApplyModeToVerb();
            return loaded;
        }

        // 蓄力阶切换:耗时来自 stageSwitchTicks,生效时播放该阶音效并浮现切换印迹。
        private void RequestStageSwitch(int stage)
        {
            int clampedStage = Mathf.Clamp(stage, 0, MaxStageIndex);
            if (Props.stageSwitchTicks <= 0)
            {
                stageIndex = clampedStage;
                hasPendingStageSwitch = false;
                PlaySwitchSound(Props.stages[clampedStage].switchSound);
                PlayStageSwitchVfx();
                ApplyModeToVerb();
                return;
            }
            pendingStageIndex = clampedStage;
            stageSwitchReadyTick = Find.TickManager.TicksGame + Props.stageSwitchTicks;
            hasPendingStageSwitch = true;
        }

        // 近战/远程模式切换:与阶段切换独立,耗时来自 modeSwitchTicks。
        private void RequestModeSwitch(int mode)
        {
            int clampedMode = Mathf.Clamp(mode, MeleeMode, FlashBowMode);
            if (Props.modeSwitchTicks <= 0)
            {
                modeIndex = clampedMode;
                hasPendingModeSwitch = false;
                PlaySwitchSound(Props.modeSwitchSound);
                ApplyModeToVerb();
                return;
            }
            pendingModeIndex = clampedMode;
            modeSwitchReadyTick = Find.TickManager.TicksGame + Props.modeSwitchTicks;
            hasPendingModeSwitch = true;
        }

        private void ApplyPendingIfReady()
        {
            int now = Find.TickManager.TicksGame;
            if (hasPendingStageSwitch && now >= stageSwitchReadyTick)
            {
                stageIndex = pendingStageIndex;
                hasPendingStageSwitch = false;
                PlaySwitchSound(Props.stages[stageIndex].switchSound);
                PlayStageSwitchVfx();
                ApplyModeToVerb();
            }
            if (hasPendingModeSwitch && now >= modeSwitchReadyTick)
            {
                modeIndex = pendingModeIndex;
                hasPendingModeSwitch = false;
                PlaySwitchSound(Props.modeSwitchSound);
                ApplyModeToVerb();
            }
        }

        // 切换音效:装备者在地图上按角色位置播放,否则退回镜头音效。
        private void PlaySwitchSound(SoundDef sound)
        {
            if (sound == null)
            {
                return;
            }
            Pawn pawn = EquippedPawn;
            if (pawn != null && pawn.Spawned && pawn.MapHeld != null)
            {
                sound.PlayOneShot(SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.MapHeld)));
            }
            else
            {
                sound.PlayOneShotOnCamera();
            }
        }

        // 蓄力阶切换的浮现印迹:在装备者身上挂一枚 MoteAttached,外观与时长由 Defs/Misc/Mote_BowOfDeath.xml 定义,
        // 淡入/保持/上浮淡出与超时销毁全部由原版 Mote 生命周期负责,这里只负责生成与刷新。
        // 刷新语义:上一枚仍在播放且宿主未变时,只把它的时间轴推回起点重播,不叠加第二枚;
        // 宿主已变(换装/换人)或印迹已销毁时,毁掉旧实例另起一枚。
        private void PlayStageSwitchVfx()
        {
            Pawn pawn = EquippedPawn;
            if (pawn == null || !pawn.Spawned || pawn.MapHeld == null)
            {
                return;
            }
            if (stageSwitchVfx != null && !stageSwitchVfx.Destroyed && stageSwitchVfx.Spawned)
            {
                if (stageSwitchVfx.link1.Target.Thing == pawn)
                {
                    stageSwitchVfx.ForceSpawnTick(Find.TickManager.TicksGame);
                    return;
                }
                stageSwitchVfx.Destroy();
            }
            stageSwitchVfx = MoteMaker.MakeAttachedOverlay(pawn, DeathBowDefOf.BowOfDeath_StageVfx, Vector3.zero);
        }

        // 蓄力特效:瞄准开始时在装备者身上同时挂两层 Mote —— 主体层(反复淡出)与基底层(常驻轻呼吸),
        // 时长、贴图与"停止维护即淡出"的规则都写在 Defs/Misc/Mote_BowOfDeath.xml,这里只负责生成一次。
        // 仍在使用同一枚时直接复用,避免 AI 反复重新瞄准把淡入打断、或叠出多枚特效。
        public void Notify_AimStarted()
        {
            // 只有配置了瞄准特效的蓄力阶才出特效(当前为四阶);其他阶正常瞄准但不挂烟雾。
            if (CurrentStage?.aimVfx != true)
            {
                return;
            }
            Pawn pawn = EquippedPawn;
            if (pawn == null || !pawn.Spawned || pawn.MapHeld == null)
            {
                return;
            }
            EnsureAimVfx(ref aimVfx, DeathBowDefOf.BowOfDeath_AimVfx, pawn);
            EnsureAimVfx(ref aimBaseVfx, DeathBowDefOf.BowOfDeath_AimBaseVfx, pawn);
        }

        // 已有存活实例且宿主相同时复用,宿主已变或已销毁时重建。
        private void EnsureAimVfx(ref Mote mote, ThingDef def, Pawn pawn)
        {
            if (mote != null && !mote.Destroyed && mote.Spawned)
            {
                if (mote.link1.Target.Thing == pawn)
                {
                    return;
                }
                mote.Destroy();
            }
            mote = MoteMaker.MakeAttachedOverlay(pawn, def, Vector3.zero);
        }

        // 图标:配置了路径就读取,缺失时回退到传入的默认图标(不静默使用错误贴图)。
        private Texture2D GetIcon(string path, Texture2D fallback)
        {
            if (path.NullOrEmpty())
            {
                return fallback;
            }
            Texture2D texture = ContentFinder<Texture2D>.Get(path, false);
            if (texture == null)
            {
                Log.ErrorOnce(parent.def.defName + " is configured with a missing icon path '" + path + "'.", path.GetHashCode() ^ 0x5B09);
                return fallback;
            }
            return texture;
        }

        // 阶段图标:该阶未配图标时回退到模式图标(通用能力图),再回退武器图标。
        private Texture2D GetStageIcon(DeathBowStage stage)
        {
            Texture2D fallback = GetIcon(Props.modeIconPath, parent.def.uiIcon);
            return stage == null ? fallback : GetIcon(stage.iconPath, fallback);
        }

        // 阶段介绍:直接取该阶在 XML 中填写的文字,不做运行时生成。
        public static string GetStageDescription(DeathBowStage stage)
        {
            return stage?.description;
        }

        public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetEquippedGizmosExtra())
            {
                yield return gizmo;
            }
            Pawn pawn = EquippedPawn;
            if (pawn == null || pawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            // 上一次射击后次数可能变化,这里把状态同步一次到射击动词。
            ApplyModeToVerb();
            yield return MakeModeGizmo();
            yield return MakeStageGizmo();
            yield return MakeLoadGizmo(pawn);
        }

        // 模式切换:普通模式只保留近战;闪弓模式且仍有余弹时恢复原版射击参数,原版远程攻击命令随之出现在装备按钮栏。
        private Gizmo MakeModeGizmo()
        {
            Command_Action command = new Command_Action();
            command.defaultLabel = FlashBowActive
                ? "BowOfDeath_Mode_FlashBow".Translate(charges, Props.maxCharges)
                : "BowOfDeath_Mode_Melee".Translate();
            command.defaultDesc = "BowOfDeath_ModeDesc".Translate();
            command.icon = GetIcon(Props.modeIconPath, parent.def.uiIcon);
            if (FlashBowActive && charges <= 0)
            {
                command.defaultDesc += "\n" + "BowOfDeath_NoAmmo".Translate();
            }
            command.action = delegate
            {
                RequestModeSwitch(FlashBowActive ? MeleeMode : FlashBowMode);
            };
            DisableIfUnswitchable(command);
            return command;
        }

        // 预热、爆发或切换等待期内禁止再次切换,避免半个攻击过程使用旧参数。
        private void DisableIfUnswitchable(Command_Action command)
        {
            if (IsSwitching)
            {
                command.defaultLabel = "BowOfDeath_Switching".Translate(SwitchTicksLeft.TicksToSeconds().ToString("0.#"));
                command.Disable("BowOfDeath_Busy".Translate().ToString());
                return;
            }
            Verb bowVerb = BowVerb;
            if (bowVerb != null && (bowVerb.WarmingUp || bowVerb.Bursting))
            {
                command.Disable("BowOfDeath_Busy".Translate().ToString());
            }
        }

        // 蓄力阶选择:原版没有轮盘控件,使用浮动菜单列出全部阶。
        private Gizmo MakeStageGizmo()
        {
            Command_Action command = new Command_Action();
            command.defaultLabel = "BowOfDeath_Stage".Translate(StageIndex + 1);
            command.defaultDesc = GetStageDescription(CurrentStage);
            command.icon = GetStageIcon(CurrentStage);
            if (Props.stages.NullOrEmpty())
            {
                command.Disable("BowOfDeath_NoStage".Translate().ToString());
                return command;
            }
            command.action = delegate
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                for (int i = 0; i < Props.stages.Count; i++)
                {
                    int target = i;
                    FloatMenuOption option = new FloatMenuOption("BowOfDeath_Stage".Translate(i + 1), delegate
                    {
                        RequestStageSwitch(target);
                    });
                    option.tooltip = new TipSignal(GetStageDescription(Props.stages[i]));
                    options.Add(option);
                }
                Find.WindowStack.Add(new FloatMenu(options));
            };
            DisableIfUnswitchable(command);
            return command;
        }

        // 装填死之箭:按背包存量一次补满可用次数。
        private Gizmo MakeLoadGizmo(Pawn pawn)
        {
            Command_Action command = new Command_Action();
            command.defaultLabel = "BowOfDeath_Load".Translate(charges, Props.maxCharges);
            command.defaultDesc = "BowOfDeath_LoadDesc".Translate(Props.ammoDef.label);
            command.icon = GetIcon(Props.loadIconPath, Props.ammoDef.uiIcon);
            command.action = delegate
            {
                int loaded = LoadAmmoFromInventory(pawn);
                if (loaded <= 0)
                {
                    Messages.Message("BowOfDeath_LoadFailed".Translate(Props.ammoDef.label), pawn, MessageTypeDefOf.RejectInput, historical: false);
                    return;
                }
                // 装填成功才出声:背包装不下或没有死之箭时只有拒绝提示,不响装填音。
                // 装填是独立 Gizmo,音效按装备者位置单独播放,不经过切换音效入口。
                if (pawn.Spawned && pawn.MapHeld != null)
                {
                    Props.loadSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.MapHeld)));
                }
                else
                {
                    Props.loadSound.PlayOneShotOnCamera();
                }
            };
            if (charges >= Props.maxCharges)
            {
                command.Disable("BowOfDeath_LoadFull".Translate().ToString());
            }
            return command;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref modeIndex, "deathBowMode", MeleeMode);
            Scribe_Values.Look(ref stageIndex, "deathBowStage", 0);
            Scribe_Values.Look(ref charges, "deathBowCharges", 0);
            Scribe_Values.Look(ref pendingStageIndex, "deathBowPendingStage", 0);
            Scribe_Values.Look(ref stageSwitchReadyTick, "deathBowStageSwitchReadyTick", 0);
            Scribe_Values.Look(ref hasPendingStageSwitch, "deathBowStageSwitching", defaultValue: false);
            Scribe_Values.Look(ref pendingModeIndex, "deathBowPendingMode", MeleeMode);
            Scribe_Values.Look(ref modeSwitchReadyTick, "deathBowModeSwitchReadyTick", 0);
            Scribe_Values.Look(ref hasPendingModeSwitch, "deathBowModeSwitching", defaultValue: false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ApplyModeToVerb();
                // Mote 不入存档:读档时若这把弓正处于瞄准预热中,补挂一次瞄准特效,避免这一段瞄准没有烟雾。
                if (BowVerb != null && BowVerb.WarmingUp)
                {
                    Notify_AimStarted();
                }
            }
        }
    }
}
