using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FlugelLibrary
{
    //表示精灵回廊过载状态，并按当前阶段提供状态标签颜色。
    public class Hediff_Shin : HediffWithComps
    {
        public override Color LabelColor
        {
            get
            {
                var comp = GetComp<HediffComp_FlugelShin>();
                if (comp?.Props?.stageLabelColors == null || comp.Props.stageLabelColors.Count == 0)
                    return base.LabelColor;
                int idx = Mathf.Clamp(CurStageIndex, 0, comp.Props.stageLabelColors.Count - 1);
                return comp.Props.stageLabelColors[idx];
            }
        }
    }

    //保存精灵回廊过载状态的消耗、伤害、持续时间、阶段显示和特效绘制配置。
    public class HediffCompProperties_FlugelShin : HediffCompProperties
    {
        public float severityGainPerDay = 8.34f;
        public float spiritCostPerSecond = 0.5f;
        public int stage4MaxTicks = 3600;
        public AbilityDef onAbilityDef;
        public AbilityDef offAbilityDef;
        public HediffDef debuffHediffDef;
        public HediffDef collapseHediffDef;
        public int stage4DamageIntervalTicks = 120;
        public float stage4DamagePerInterval = 2f;
        public HediffDef overloadBurnDef;
        public List<Color> stageLabelColors = new List<Color>();
        public FlugelShaderMaterialDef preBurnEffectMaterial;
        public Vector2 preBurnEffectDrawSize = new Vector2(2.24f, 2.24f);
        public float preBurnEffectHorizontalOffset;
        public float preBurnEffectVerticalOffset;
        public float preBurnEffectAltitudeLayer = -10f;
        public FlugelShaderMaterialDef stage4FlameMaterial;
        public Vector2 stage4FlameDrawSize = new Vector2(11.8f, 11.8f);
        public float stage4FlameHorizontalOffset;
        public float stage4FlameVerticalOffset = 4.2f;
        public float stage4FlameAltitudeLayer = -10f;
        public FlugelShaderMaterialDef stage4FrontMaterial;
        public Vector2 stage4FrontDrawSize = new Vector2(2.36f, 2.36f);
        public float stage4FrontHorizontalOffset;
        public float stage4FrontVerticalOffset;
        public float stage4FrontAltitudeLayer = 100f;

        //负责把配置绑定到精灵回廊过载运行组件。
        public HediffCompProperties_FlugelShin()
        {
            compClass = typeof(HediffComp_FlugelShin);
        }
    }

    //负责推进过载阶段、消耗精灵能量并处理第四阶段灼伤与崩解。
    public class HediffComp_FlugelShin : HediffComp
    {
        private static readonly BodyPartDef[] AllowedParts = {
            BodyPartDefOf.Torso,
            BodyPartDefOf.Arm, BodyPartDefOf.Hand,
            BodyPartDefOf.Leg
        };

        private static BodyPartDef FootDef;

        private int stage4ElapsedTicks;
        private int elapsedTicks;
        private int nextSpiritCheckTick = -1;
        private bool closing;
        private bool stage4SoundPlayed;
        private List<BodyPartRecord> cachedParts;

        public int ElapsedTicks => elapsedTicks;
        public int Stage4ElapsedTicks => stage4ElapsedTicks;

        public HediffCompProperties_FlugelShin Props => (HediffCompProperties_FlugelShin)props;

        //负责初始化能量检查时间和可承受灼伤的身体部位缓存。
        public override void CompPostMake()
        {
            base.CompPostMake();
            nextSpiritCheckTick = Find.TickManager.TicksGame + 60;

            Pawn p = Pawn;
            if (p != null)
            {
                if (FootDef == null)
                    FootDef = DefDatabase<BodyPartDef>.GetNamedSilentFail("Foot");

                cachedParts = p.RaceProps.body.AllParts
                    .Where(pt => AllowedParts.Contains(pt.def)
                        || (FootDef != null && pt.def == FootDef))
                    .ToList();
            }
        }

        //负责逐 Tick 推进阶段并执行能量消耗和第四阶段伤害。
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn p = Pawn;
            if (p == null || !p.Spawned || p.Map == null) return;

            elapsedTicks++;

            Hediff hediff = parent;
            float gainPerTick = Props.severityGainPerDay / 60000f;
            hediff.Severity += gainPerTick;

            int now = Find.TickManager.TicksGame;
            if (now >= nextSpiritCheckTick)
            {
                nextSpiritCheckTick += 60;
                Gene_Elf gene = p.genes?.GetFirstGeneOfType<Gene_Elf>();
                if (gene != null)
                {
                    gene.Value -= Props.spiritCostPerSecond;
                    if (gene.Value < 2f)
                    {
                        DoManualClose(p, false);
                        Messages.Message("精灵能量耗尽，过载强制终止。", p, MessageTypeDefOf.NegativeEvent);
                        return;
                    }
                }
            }

            if (hediff.CurStageIndex >= 3)
            {
                stage4ElapsedTicks++;
                if (!stage4SoundPlayed)
                {
                    stage4SoundPlayed = true;
                    SoundDef.Named("Resurrect_Cast").PlayOneShot(
                        new TargetInfo(p.Position, p.Map));
                }
                if (stage4ElapsedTicks >= Props.stage4MaxTicks)
                {
                    Messages.Message("精灵回廊过载过度，{PAWN}倒下！".Replace("{PAWN}", p.LabelShort),
                        p, MessageTypeDefOf.NegativeEvent);
                    stage4ElapsedTicks = 0;
                    DoManualClose(p, true);
                    return;
                }

                if (stage4ElapsedTicks % Props.stage4DamageIntervalTicks == 0
                    && cachedParts != null && cachedParts.Count > 0
                    && Props.overloadBurnDef != null)
                {
                    var available = cachedParts
                        .Where(pt => !p.health.hediffSet.PartIsMissing(pt))
                        .ToList();
                    BodyPartRecord part = available.Any()
                        ? available.RandomElement()
                        : cachedParts.FirstOrDefault();

                    if (part != null)
                    {
                        Hediff_Injury wound = (Hediff_Injury)HediffMaker.MakeHediff(
                            Props.overloadBurnDef, p, part);
                        wound.Severity = Props.stage4DamagePerInterval;
                        p.health.AddHediff(wound);
                    }
                }
            }
        }

        //负责结束过载、恢复切换能力，并根据结束原因施加反噬或崩解。
        private void DoManualClose(Pawn p, bool collapse)
        {
            if (closing) return;
            closing = true;

            int cdTicks = 2500 + elapsedTicks;

            if (Props.offAbilityDef != null)
                p.abilities?.RemoveAbility(Props.offAbilityDef);
            if (Props.onAbilityDef != null)
            {
                p.abilities?.GainAbility(Props.onAbilityDef);
                Ability newAb = p.abilities?.GetAbility(Props.onAbilityDef);
                newAb?.StartCooldown(cdTicks);
            }

            if (collapse && Props.collapseHediffDef != null)
            {
                p.health.AddHediff(Props.collapseHediffDef);
            }
            else if (Props.debuffHediffDef != null && elapsedTicks > 1800)
            {
                int stage = parent.CurStageIndex;
                int debuffDuration = Mathf.Min(elapsedTicks - 1800, 5400);
                Hediff debuff = HediffMaker.MakeHediff(Props.debuffHediffDef, p);
                debuff.Severity = Mathf.Clamp01(stage * 0.25f);
                debuff.TryGetComp<HediffComp_Disappears>()?.SetDuration(debuffDuration);
                p.health.AddHediff(debuff);
            }

            p.health.RemoveHediff(parent);
        }

        //负责保存和读取过载阶段计时及关闭状态。
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref stage4ElapsedTicks, "stage4ElapsedTicks", 0);
            Scribe_Values.Look(ref elapsedTicks, "elapsedTicks", 0);
            Scribe_Values.Look(ref nextSpiritCheckTick, "nextSpiritCheckTick", -1);
            Scribe_Values.Look(ref closing, "closing", false);
            Scribe_Values.Look(ref stage4SoundPlayed, "stage4SoundPlayed", false);
        }
    }
}
