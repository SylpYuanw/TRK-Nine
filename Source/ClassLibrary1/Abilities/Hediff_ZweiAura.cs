using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace XIYUNTE
{
    // ========================================================================
    // 「二协会」套装光环：继承六协会的光环组件，只覆写差异部分。
    //   沿用：Props 的 baseRange / rangePerExtraPawn / scanIntervalTicks；
    //         LiuSetUtility 的 CollectCandidates / IsValidTarget / ComputeRange；
    //         六协会的来源记录组件 HediffComp_LiuAuraLink、增益类 Hediff_LiuAuraBuff。
    //   差异：1) 来源不判定套装；2) 目标为范围内所有同阵营人形单位(含自身)；
    //         3) 增益 HediffDef 与单目标来源上限由本 Props 提供；4) 可选地面贴图。
    // 射程圈由原版 Command_Ability/Verb 绘制，成员连线交给原版 HediffComp_Link。
    // ========================================================================

    // 技能效果：沿用原版 CompAbilityEffect_GiveHediff，只覆写「未集齐套装时隐藏按钮」。
    // 技能实例不随套装收回，因此冷却不会被换装重置。
    public class CompProperties_ZweiAuraGiveHediff : CompProperties_AbilityGiveHediff
    {
        // 判定集齐用的套装 Hediff。
        public HediffDef setHediff;

        public CompProperties_ZweiAuraGiveHediff()
        {
            compClass = typeof(CompAbilityEffect_ZweiAura);
        }
    }

    public class CompAbilityEffect_ZweiAura : CompAbilityEffect_GiveHediff
    {
        public new CompProperties_ZweiAuraGiveHediff Props => (CompProperties_ZweiAuraGiveHediff)props;

        public override bool ShouldHideGizmo => !HasFullSet(parent.pawn);

        private bool HasFullSet(Pawn pawn)
        {
            HediffDef setDef = Props.setHediff;
            if (setDef == null || pawn?.health?.hediffSet == null)
                return false;

            Hediff set = pawn.health.hediffSet.GetFirstHediffOfDef(setDef);
            return set != null && set.Severity >= setDef.maxSeverity;
        }
    }

    public class HediffCompProperties_ZweiAura : HediffCompProperties_LiuSetAura
    {
        // 发放的光环增益 HediffDef。
        public HediffDef auraBuff;
        // 单个目标可叠加的来源上限(人)。
        public int maxSources = 3;
        // 地面光环 Mote：跟随来源 Pawn，圆形由 Mote 子类程序绘制。
        public ThingDef mote;
        // 文化 DLC 战斗命令的光罩；该 DLC 未加载时字段为空，此时回落到 mote(自绘圆)。
        public ThingDef motePreferred;

        public HediffCompProperties_ZweiAura()
        {
            compClass = typeof(HediffComp_ZweiAura);
        }
    }

    public class HediffComp_ZweiAura : HediffComp_LiuSetAura
    {
        public new HediffCompProperties_ZweiAura Props => (HediffCompProperties_ZweiAura)props;

        private Mote mote;
        // 复用缓冲，避免每次扫描分配新 List。
        private readonly List<Pawn> targetsBuffer = new List<Pawn>();

        public override void CompPostTick(ref float severityAdjustment)
        {
            Pawn self = Pawn;
            if (self == null || self.Dead || !self.Spawned || self.Map == null)
                return;
            if (Props.auraBuff == null)
                return;

            UpdateMote(self);

            int interval = Props.scanIntervalTicks > 0 ? Props.scanIntervalTicks : 30;
            if (!self.IsHashIntervalTick(interval))
                return;

            Map map = self.Map;
            LiuSetUtility.CollectCandidates(self, targetsBuffer);
            float range = LiuSetUtility.ComputeRange(self, targetsBuffer, Props.baseRange, Props.rangePerExtraPawn);

            for (int i = 0; i < targetsBuffer.Count; i++)
            {
                Pawn target = targetsBuffer[i];
                if (!LiuSetUtility.IsValidTarget(target, map))
                    continue;
                // 圆形范围：与领袖命令一致，按实际距离判定。
                if (self.Position.DistanceTo(target.Position) > range)
                    continue;

                GrantBuff(target, self, interval + 5);
            }
        }

        // 地面光环：按原版 HediffComp_GiveHediffsInRange 的写法挂一个跟随来源 Pawn 的 Mote，
        // 销毁后重建；透明度与颜色全部由 Mote 的贴图与 GraphicData 决定。
        private void UpdateMote(Pawn self)
        {
            ThingDef moteDef = Props.motePreferred ?? Props.mote;
            if (moteDef == null)
                return;
            if (mote == null || mote.Destroyed)
                mote = MoteMaker.MakeAttachedOverlay(self, moteDef, Vector3.zero);
            mote.Maintain();
        }

        // 发放/刷新「来自 source 的这一份」增益：每来源一份实例，达到上限后不再新增来源。
        private void GrantBuff(Pawn target, Pawn source, int lingerTicks)
        {
            HediffDef auraDef = Props.auraBuff;
            Hediff mine = FindFrom(target, auraDef, source);
            if (mine == null)
            {
                if (CountSources(target, auraDef) >= Props.maxSources)
                    return;

                mine = HediffMaker.MakeHediff(auraDef, target);
                mine.Severity = 1f;
                HediffComp_LiuAuraLink link = mine.TryGetComp<HediffComp_LiuAuraLink>();
                if (link == null)
                {
                    Log.ErrorOnce(auraDef.defName + " needs HediffCompProperties_LiuAuraLink; aura buff was not applied.", auraDef.shortHash ^ 0x5A51);
                    return;
                }
                link.source = source;
                HediffComp_Link drawLink = mine.TryGetComp<HediffComp_Link>();
                if (drawLink != null)
                {
                    drawLink.other = source;
                    drawLink.drawConnection = true;
                }
                target.health.AddHediff(mine);
            }

            HediffComp_Disappears disappears = mine.TryGetComp<HediffComp_Disappears>();
            if (disappears == null)
            {
                Log.ErrorOnce(auraDef.defName + " needs HediffCompProperties_Disappears; aura buff would never expire.", auraDef.shortHash ^ 0x5A52);
                return;
            }
            disappears.ticksToDisappear = lingerTicks;
        }

        // 找出目标身上「由指定来源提供」的那一份增益。
        private static Hediff FindFrom(Pawn target, HediffDef auraDef, Pawn source)
        {
            List<Hediff> hediffs = target.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (hediff.def != auraDef)
                    continue;
                if (hediff.TryGetComp<HediffComp_LiuAuraLink>()?.source == source)
                    return hediff;
            }
            return null;
        }

        private static int CountSources(Pawn target, HediffDef auraDef)
        {
            int count = 0;
            List<Hediff> hediffs = target.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].def == auraDef)
                    count++;
            }
            return count;
        }
    }

}
