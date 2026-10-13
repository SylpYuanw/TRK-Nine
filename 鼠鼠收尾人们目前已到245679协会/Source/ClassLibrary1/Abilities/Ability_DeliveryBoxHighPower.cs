using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using HarmonyLib;

namespace XIYUNTE
{
    // ========================================================================
    // 「高功率模式」能力核心。结构对齐企划-2：Ability + Hediff + AbilitySwitch。
    // 开启(ON)是武器临时 Ability(CompEquippable_MultiForm)，激活时用 ShouldHideGizmo 隐藏；
    // 关闭(OFF)参考 Shin 的开关切换，开启后 GainAbility 挂到 pawn.abilities、关闭后 RemoveAbility，
    // 因此所有形态都会显示「关闭」按钮。Hediff 以 严重度=时间进度 推进，最终 100% 判死。
    // ========================================================================

    // ---------- 开启能力 ----------
    public class Ability_DeliveryBoxHighPower : Ability
    {
        public Ability_DeliveryBoxHighPower() { }
        public Ability_DeliveryBoxHighPower(Pawn pawn) : base(pawn) { }
        public Ability_DeliveryBoxHighPower(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool CanApplyOn(LocalTargetInfo target)
        {
            if (pawn == null || pawn.Dead || pawn.equipment == null || !(pawn.equipment.Primary is Thing_DeliveryBox))
                return false;

            HediffDef highPowerDef = DefDatabase<HediffDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower");
            return highPowerDef != null
                && pawn.health != null
                && pawn.health.hediffSet.GetFirstHediffOfDef(highPowerDef) == null
                && base.CanApplyOn(target);
        }

        public override bool GizmoDisabled(out string reason)
        {
            if (pawn == null || pawn.equipment == null || !(pawn.equipment.Primary is Thing_DeliveryBox))
            {
                reason = "RK_DeliveryBox_HighPower_RequiresWeapon".Translate();
                return true;
            }

            HediffDef highPowerDef = DefDatabase<HediffDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower");
            if (highPowerDef != null && pawn.health != null && pawn.health.hediffSet.GetFirstHediffOfDef(highPowerDef) != null)
            {
                reason = "RK_DeliveryBox_HighPower_AlreadyActive".Translate();
                return true;
            }
            return base.GizmoDisabled(out reason);
        }
    }

    // 开启能力的施放组件：给施放者添加主高功率 Hediff；随后通知装备临时能力刷新，
    // 使武器栏立即由「开启」变为「关闭」。冷却不在开启时进入，而在关闭时手动 StartCooldown。
    public class CompProperties_DeliveryBoxHighPower : CompProperties_AbilityEffect
    {
        public HediffDef hediffDef;

        public CompProperties_DeliveryBoxHighPower()
        {
            compClass = typeof(CompAbilityEffect_DeliveryBoxHighPower);
        }
    }

    public class CompAbilityEffect_DeliveryBoxHighPower : CompAbilityEffect
    {
        public new CompProperties_DeliveryBoxHighPower Props => (CompProperties_DeliveryBoxHighPower)props;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn caster = parent.pawn;
            if (caster == null || caster.health == null || caster.equipment == null || !(caster.equipment.Primary is Thing_DeliveryBox))
                return false;

            HediffDef hediffDef = Props.hediffDef ?? DefDatabase<HediffDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower");
            if (hediffDef == null || caster.health.hediffSet.GetFirstHediffOfDef(hediffDef) != null)
                return false;

            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent.pawn;
            HediffDef hediffDef = Props.hediffDef ?? DefDatabase<HediffDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower");
            if (caster == null || caster.health == null || hediffDef == null)
                return;

            Hediff highPower = HediffMaker.MakeHediff(hediffDef, caster);
            highPower.Severity = 0f;
            caster.health.AddHediff(highPower);

            // 开启后把「关闭」能力挂到 pawn.abilities(参考 Shin 的开关切换),使所有形态都显示关闭按钮。
            AbilityDef offDef = DefDatabase<AbilityDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower_Off");
            if (offDef != null)
                caster.abilities?.GainAbility(offDef);

            // 刷新装备临时能力，使武器栏立即显示「关闭」按钮。
            caster.abilities?.Notify_TemporaryAbilitiesChanged();
        }

        // 高功率激活时隐藏「开启」按钮(此时有独立的「关闭」按钮可用)。
        public override bool ShouldHideGizmo
        {
            get
            {
                Pawn caster = parent.pawn;
                HediffDef def = Props.hediffDef ?? DefDatabase<HediffDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower");
                return caster != null && caster.health != null && def != null
                    && caster.health.hediffSet.GetFirstHediffOfDef(def) != null;
            }
        }
    }

    // ---------- 关闭能力 ----------
    public class Ability_DeliveryBoxHighPowerOff : Ability
    {
        public Ability_DeliveryBoxHighPowerOff() { }
        public Ability_DeliveryBoxHighPowerOff(Pawn pawn) : base(pawn) { }
        public Ability_DeliveryBoxHighPowerOff(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool CanApplyOn(LocalTargetInfo target)
        {
            if (pawn == null || pawn.Dead || pawn.equipment == null || !(pawn.equipment.Primary is Thing_DeliveryBox))
                return false;
            HediffDef highPowerDef = DefDatabase<HediffDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower");
            return highPowerDef != null
                && pawn.health != null
                && pawn.health.hediffSet.GetFirstHediffOfDef(highPowerDef) != null
                && base.CanApplyOn(target);
        }

        public override bool GizmoDisabled(out string reason)
        {
            HediffDef highPowerDef = DefDatabase<HediffDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower");
            if (highPowerDef == null || pawn == null || pawn.health == null || pawn.health.hediffSet.GetFirstHediffOfDef(highPowerDef) == null)
            {
                reason = "RK_DeliveryBox_HighPower_NotActive".Translate();
                return true;
            }
            return base.GizmoDisabled(out reason);
        }
    }

    // 关闭能力施放组件：移除主高功率 Hediff 与护盾 Hediff，并给「开启」能力手动进入 24 小时冷却。
    public class CompProperties_DeliveryBoxHighPowerOff : CompProperties_AbilityEffect
    {
        public HediffDef mainHediffDef;
        public HediffDef shieldDef;
        public int cooldownTicks = 60000; // 24 小时 = 1 游戏日(60000 tick)

        public CompProperties_DeliveryBoxHighPowerOff()
        {
            compClass = typeof(CompAbilityEffect_DeliveryBoxHighPowerOff);
        }
    }

    public class CompAbilityEffect_DeliveryBoxHighPowerOff : CompAbilityEffect
    {
        public new CompProperties_DeliveryBoxHighPowerOff Props => (CompProperties_DeliveryBoxHighPowerOff)props;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn caster = parent.pawn;
            HediffDef mainDef = Props.mainHediffDef ?? DefDatabase<HediffDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower");
            if (caster == null || caster.health == null || mainDef == null)
                return false;
            return caster.health.hediffSet.GetFirstHediffOfDef(mainDef) != null;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent.pawn;
            if (caster == null || caster.health == null)
                return;

            AbilityDef offDef = DefDatabase<AbilityDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower_Off");
            HediffDef mainDef = Props.mainHediffDef ?? DefDatabase<HediffDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower");
            if (mainDef != null)
            {
                Hediff main = caster.health.hediffSet.GetFirstHediffOfDef(mainDef);
                if (main != null)
                    caster.health.RemoveHediff(main);
            }

            HediffDef shieldDef = Props.shieldDef ?? DefDatabase<HediffDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower_Shield");
            if (shieldDef != null)
            {
                Hediff shield = caster.health.hediffSet.GetFirstHediffOfDef(shieldDef);
                if (shield != null)
                    caster.health.RemoveHediff(shield);
            }

            // 关闭后移除「关闭」能力(主 Hediff 的清理也会做兜底),并给「开启」能力进入冷却。
            CompEquippable_MultiForm multiForm = caster.equipment?.Primary?.GetComp<CompEquippable_MultiForm>();
            if (offDef != null)
                caster.abilities?.RemoveAbility(offDef);
            multiForm?.StartHighPowerCooldown(Props.cooldownTicks);
            caster.abilities?.Notify_TemporaryAbilitiesChanged();
        }
    }

    // ========================================================================
    // 主高功率 Hediff：以「严重度=已运行 tick/总时长」推进，跨阶段并驱动护盾/灼伤。
    // lethalSeverity=1，到 12 小时严重度=100%，由原版健康系统判死。
    // ========================================================================
    public class Hediff_DeliveryBoxHighPower : HediffWithComps
    {
        // 显示高功率状态 Gizmo(剩余时间 + 护盾伤害条),仅玩家阵营角色显示。
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;
            if (pawn != null && pawn.Faction == Faction.OfPlayer)
                yield return new Gizmo_DeliveryBoxHighPower(this);
        }
    }

    public class HediffCompProperties_DeliveryBoxHighPower : HediffCompProperties
    {
        public int totalTicks = 30000;       // 12 小时
        public int stage2Tick = 7500;        // 3 小时进入二阶段（护盾）
        public int stage3Tick = 22500;       // 9 小时进入三阶段（化学灼伤）
        public int burnIntervalTicks = 1875; // 45 分钟
        public float burnSeverity = 9f;
        public HediffDef shieldDef;
        public HediffDef burnDef;

        public HediffCompProperties_DeliveryBoxHighPower()
        {
            compClass = typeof(HediffComp_DeliveryBoxHighPower);
        }
    }

    public class HediffComp_DeliveryBoxHighPower : HediffComp
    {
        public HediffCompProperties_DeliveryBoxHighPower Props => (HediffCompProperties_DeliveryBoxHighPower)props;

        private int elapsedTicks;
        private int nextBurnTick;
        private bool shieldAdded;
        private bool stage3Announced;

        public int ElapsedTicks => elapsedTicks;

        public override void CompPostMake()
        {
            base.CompPostMake();
            nextBurnTick = Props.stage3Tick;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn p = Pawn;
            if (p == null || !p.Spawned)
                return;

            elapsedTicks++;
            parent.Severity = Mathf.Clamp01((float)elapsedTicks / Mathf.Max(1, Props.totalTicks));

            // 二阶段：添加护盾（只加一次）。
            if (!shieldAdded && elapsedTicks >= Props.stage2Tick)
            {
                shieldAdded = true;
                if (Props.shieldDef != null && p.health.hediffSet.GetFirstHediffOfDef(Props.shieldDef) == null)
                {
                    Hediff shield = HediffMaker.MakeHediff(Props.shieldDef, p);
                    p.health.AddHediff(shield);
                }
            }

            // 进入三阶段时给玩家一次明确提示。
            if (!stage3Announced && elapsedTicks >= Props.stage3Tick)
            {
                stage3Announced = true;
                Messages.Message(
                    "RK_DeliveryBox_HighPower_Stage3".Translate().ToString(),
                    p, MessageTypeDefOf.NegativeEvent);
            }

            // 三阶段：按间隔在随机非豁免部位添加化学灼伤。
            if (elapsedTicks >= Props.stage3Tick && elapsedTicks >= nextBurnTick)
            {
                nextBurnTick += Props.burnIntervalTicks;
                TryAddBurn(p);
            }

            // 严重度达到 1.0（12 小时）由原版 lethalSeverity 判死，无需在此手写 kill。
        }

        private void TryAddBurn(Pawn p)
        {
            if (Props.burnDef == null)
                return;

            // 逆向豁免：仅烧「肤覆盖」的外表/附着器(含 mod 翅膀/耳/尾)；内部器官/骨头/大脑/心脏(非肤覆盖)豁免。
            List<BodyPartRecord> available = p.RaceProps.body.AllParts
                .Where(pt => !p.health.hediffSet.PartIsMissing(pt)
                    && pt.def.IsSkinCovered(pt, p.health.hediffSet))
                .ToList();
            if (available.Count == 0)
                return;

            BodyPartRecord part = available.RandomElement();
            Hediff_Injury wound = (Hediff_Injury)HediffMaker.MakeHediff(Props.burnDef, p, part);
            wound.Severity = Props.burnSeverity;
            p.health.AddHediff(wound);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref elapsedTicks, "elapsedTicks", 0);
            Scribe_Values.Look(ref nextBurnTick, "nextBurnTick", Props.stage3Tick);
            Scribe_Values.Look(ref shieldAdded, "shieldAdded", false);
            Scribe_Values.Look(ref stage3Announced, "stage3Announced", false);
        }

        // 主 Hediff 被移除（手动关闭或其它原因）时一并清掉护盾，避免护盾残留。
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            // 兜底:无论因何移除主 Hediff,都一并移除「关闭」能力与护盾,避免残留。
            AbilityDef offDef = DefDatabase<AbilityDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower_Off");
            if (offDef != null && Pawn != null && Pawn.abilities != null)
                Pawn.abilities.RemoveAbility(offDef);
            if (Props.shieldDef == null || Pawn == null || Pawn.health == null)
                return;
            Hediff shield = Pawn.health.hediffSet.GetFirstHediffOfDef(Props.shieldDef);
            if (shield != null)
                Pawn.health.RemoveHediff(shield);
        }
    }

    // ========================================================================
    // 二阶段护盾：真实伤害池（非能量），50 上限，15/秒(0.25/tick) 无受击冷却回复。
    // 只拦外部伤害，手术伤害（SurgicalCut）放行。
    // ========================================================================
    public class Hediff_DeliveryBoxShield : HediffWithComps
    {
    }

    public class HediffCompProperties_DeliveryBoxShieldEnergy : HediffCompProperties
    {
        public float maxDamage = 50f;
        public float regenPerTick = 0.25f; // 15/秒

        public HediffCompProperties_DeliveryBoxShieldEnergy()
        {
            compClass = typeof(HediffComp_DeliveryBoxShieldEnergy);
        }
    }

    public class HediffComp_DeliveryBoxShieldEnergy : HediffComp
    {
        public HediffCompProperties_DeliveryBoxShieldEnergy Props => (HediffCompProperties_DeliveryBoxShieldEnergy)props;

        private float damagePool;

        public float DamagePool => damagePool;
        public bool Depleted => damagePool <= 0f;

        public override void CompPostMake()
        {
            base.CompPostMake();
            damagePool = Props.maxDamage;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (damagePool < Props.maxDamage)
                damagePool = Mathf.Min(Props.maxDamage, damagePool + Props.regenPerTick);
        }

        // 从护盾池扣除伤害，返回实际吸收量；未吸收完的部分留在 damage 中（溢出漏给受术者）。
        public float AbsorbDamage(ref float damage)
        {
            if (damage <= 0f || damagePool <= 0f)
                return 0f;
            float absorbed = Mathf.Min(damagePool, damage);
            damagePool -= absorbed;
            damage -= absorbed;
            return absorbed;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref damagePool, "damagePool", Props.maxDamage);
        }
    }

    // 护盾泡渲染节点：仿原版/Flugel Defense_Shield，按能量>0 才绘制深绿色半透明泡。
    public class PawnRenderNodeProperties_DeliveryBoxShield : PawnRenderNodeProperties
    {
        public PawnRenderNodeProperties_DeliveryBoxShield()
        {
            workerClass = typeof(PawnRenderNodeWorker_DeliveryBoxShield);
        }
    }

    public class PawnRenderNodeWorker_DeliveryBoxShield : PawnRenderNodeWorker
    {
        private static readonly Color ShieldBaseColor = new Color(0.0f, 0.55f, 0.25f, 0.82f);   // 深绿
        private static readonly Color ShieldPulseColor = new Color(0.1f, 0.8f, 0.4f, 0.94f);
        private static readonly Material ShieldMat = MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent);
        private const float ShieldSize = 1.7f;
        private const float SpinAnglePerSecond = 18f;
        private const float PulseFrequency = 2.4f;

        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms))
                return false;
            if (parms.pawn.Dead || !parms.pawn.Spawned || parms.Portrait || parms.Statue)
                return false;
            HediffComp_DeliveryBoxShieldEnergy comp = node.hediff?.TryGetComp<HediffComp_DeliveryBoxShieldEnergy>();
            return comp != null && comp.DamagePool > 0f;
        }

        public override void AppendDrawRequests(PawnRenderNode node, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
        {
            requests.Add(new PawnGraphicDrawRequest(node, MeshPool.plane10, ShieldMat));
        }

        public override void PreDraw(PawnRenderNode node, Material mat, PawnDrawParms parms)
        {
            float pulse01 = 0.85f + 0.15f * (0.5f + 0.5f * Mathf.Sin(Time.time * PulseFrequency));
            node.MatPropBlock.SetColor(ShaderPropertyIDs.Color, Color.Lerp(ShieldBaseColor, ShieldPulseColor, pulse01));
        }

        public override MaterialPropertyBlock GetMaterialPropertyBlock(PawnRenderNode node, Material material, PawnDrawParms parms)
        {
            return node.MatPropBlock;
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            pivot = Vector3.zero;
            return new Vector3(0f, 0.02f, 0f);
        }

        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            return Quaternion.AngleAxis(Time.time * SpinAnglePerSecond, Vector3.up);
        }

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            return new Vector3(ShieldSize, 1f, ShieldSize);
        }
    }

    // 护盾伤害拦截：挂到 Pawn.PreApplyDamage，过滤手术伤害，其余外部伤害扣真实伤害池。
    [StaticConstructorOnStartup]
    public static class DeliveryBoxShieldDamagePatches
    {
        static DeliveryBoxShieldDamagePatches()
        {
            Harmony harmony = new Harmony("XIYUNTE.DeliveryBoxShield");
            System.Reflection.MethodInfo method = AccessTools.Method(typeof(Pawn), "PreApplyDamage");
            if (method != null)
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(DeliveryBoxShieldDamagePrefix), nameof(DeliveryBoxShieldDamagePrefix.Prefix)));
        }
    }

    public static class DeliveryBoxShieldDamagePrefix
    {
        public static void Prefix(Pawn __instance, ref DamageInfo dinfo)
        {
            if (__instance == null || __instance.Dead)
                return;

            // 手术伤害不拦截。
            if (dinfo.Def == DamageDefOf.SurgicalCut)
                return;

            HediffDef shieldDef = DefDatabase<HediffDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower_Shield");
            if (shieldDef == null || __instance.health == null)
                return;

            Hediff shield = __instance.health.hediffSet.GetFirstHediffOfDef(shieldDef);
            if (shield == null)
                return;

            HediffComp_DeliveryBoxShieldEnergy comp = shield.TryGetComp<HediffComp_DeliveryBoxShieldEnergy>();
            if (comp == null)
                return;

            float damage = dinfo.Amount;
            float absorbed = comp.AbsorbDamage(ref damage);
            if (absorbed > 0f)
                dinfo.SetAmount(Mathf.Max(0f, damage));
        }
    }

    // 兼容第三方 UI 模组(如 NiceHealthTab)通过反射调用 PawnRenderTree.TraverseTree。
    // SetDirty 会把 rootNode 置 null(RimWorld 未对第三方调用方保证已初始化),若遍历时 rootNode 为 null,
    // 原版会报 "Node is null - you must called EnsureGraphicsInitialized()"。此处前缀在遍历前先 EnsureGraphicsInitialized,
    // 使 rootNode 重建;树已就绪时为空操作,不影响正常渲染。
    // 注意:这个是因为特定MOD导致，可以修但没必要，会挂载到原版的PawnRenderTree点上，这个MOD本来BUG就多


    //[StaticConstructorOnStartup]
    //public static class DeliveryBoxRenderTreeSafe
    //{
    //    static DeliveryBoxRenderTreeSafe()
    //    {
    //        Harmony harmony = new Harmony("XIYUNTE.DeliveryBoxRenderTree");
    //        System.Reflection.MethodInfo method = AccessTools.Method(typeof(PawnRenderTree), "TraverseTree");
    //        if (method != null)
    //            harmony.Patch(method, prefix: new HarmonyMethod(typeof(DeliveryBoxRenderTreeSafe), nameof(EnsureInitBeforeTraverse)));
    //    }

    //    private static void EnsureInitBeforeTraverse(PawnRenderTree __instance)
    //    {
    //        if (__instance.rootNode == null && __instance.pawn != null)
    //            __instance.pawn.Drawer?.renderer?.EnsureGraphicsInitialized();
    //    }
    //}
}
