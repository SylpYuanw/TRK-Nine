using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FlugelLibrary
{
    public class CompProperties_FlugelShinOn : CompProperties_AbilityEffect
    {
        public HediffDef buffHediffDef;
        public AbilityDef offAbilityDef;

        public CompProperties_FlugelShinOn()
        {
            compClass = typeof(CompAbilityEffect_FlugelShinOn);
        }
    }

    public class CompAbilityEffect_FlugelShinOn : CompAbilityEffect
    {
        public new CompProperties_FlugelShinOn Props => (CompProperties_FlugelShinOn)props;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Dead) return false;

            Gene_Elf gene = pawn.genes?.GetFirstGeneOfType<Gene_Elf>();
            if (gene == null || gene.Value < 5f)
            {
                if (throwMessages)
                    Messages.Message("需要5点精灵能量才能开启过载", pawn, MessageTypeDefOf.RejectInput);
                return false;
            }

            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Dead || Props.buffHediffDef == null) return;

            Gene_Elf gene = pawn.genes?.GetFirstGeneOfType<Gene_Elf>();
            if (gene == null || gene.Value < 5f) return;

            pawn.health.AddHediff(Props.buffHediffDef);

            // 冲击波
            FleckDef shock = DefDatabase<FleckDef>.GetNamedSilentFail("ShockwaveFast");
            if (shock != null && pawn.Map != null)
                FleckMaker.Static(pawn.Position.ToVector3Shifted(), pawn.Map, shock, 2.0f);

            SoundDef.Named("Resurrect_Cast").PlayOneShot(
                new TargetInfo(pawn.Position, pawn.Map));

            if (Props.offAbilityDef != null)
                FlugelShinUtility.SwitchAbility(pawn, parent.def, Props.offAbilityDef);
        }
    }

    public class CompProperties_FlugelShinOff : CompProperties_AbilityEffect
    {
        public HediffDef buffHediffDef;
        public HediffDef debuffHediffDef;
        public AbilityDef onAbilityDef;

        public CompProperties_FlugelShinOff()
        {
            compClass = typeof(CompAbilityEffect_FlugelShinOff);
        }
    }

    public class CompAbilityEffect_FlugelShinOff : CompAbilityEffect
    {
        public new CompProperties_FlugelShinOff Props => (CompProperties_FlugelShinOff)props;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Dead) return false;

            if (IsStage4Locked(pawn))
            {
                if (throwMessages)
                    Messages.Message("回廊已超过可控极限，只能等待崩解。", pawn, MessageTypeDefOf.RejectInput);
                return false;
            }

            return base.Valid(target, throwMessages);
        }

        public override bool GizmoDisabled(out string reason)
        {
            if (IsStage4Locked(parent.pawn))
            {
                reason = "回廊已超过可控极限，只能等待崩解。";
                return true;
            }
            return base.GizmoDisabled(out reason);
        }

        private bool IsStage4Locked(Pawn pawn)
        {
            if (pawn == null) return false;
            Hediff heart = pawn.health.hediffSet.GetFirstHediffOfDef(Props.buffHediffDef);
            if (heart == null) return false;
            if (heart.CurStageIndex < 3) return false;
            var comp = heart.TryGetComp<HediffComp_FlugelShin>();
            return comp != null && comp.Stage4ElapsedTicks >= 1800;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null) return;

            Hediff heart = pawn.health.hediffSet.GetFirstHediffOfDef(Props.buffHediffDef);
            int stage = heart?.CurStageIndex ?? 0;
            int elapsed = heart?.TryGetComp<HediffComp_FlugelShin>()?.ElapsedTicks ?? 0;

            if (heart != null)
                pawn.health.RemoveHediff(heart);

            if (Props.debuffHediffDef != null && elapsed > 1800)
            {
                int debuffDuration = UnityEngine.Mathf.Min(elapsed - 1800, 5400);
                Hediff debuff = HediffMaker.MakeHediff(Props.debuffHediffDef, pawn);
                debuff.Severity = UnityEngine.Mathf.Clamp01(stage * 0.25f);
                debuff.TryGetComp<HediffComp_Disappears>()?.SetDuration(debuffDuration);
                pawn.health.AddHediff(debuff);
            }

            if (Props.onAbilityDef != null)
            {
                FlugelShinUtility.SwitchAbility(pawn, parent.def, Props.onAbilityDef);
                pawn.abilities?.GetAbility(Props.onAbilityDef)?.StartCooldown(2500 + elapsed);
            }
        }
    }

    public static class FlugelShinUtility
    {
        public static void SwitchAbility(Pawn pawn, AbilityDef removeDef, AbilityDef addDef)
        {
            if (pawn?.abilities == null) return;
            if (removeDef != null) pawn.abilities.RemoveAbility(removeDef);
            if (addDef != null) pawn.abilities.GainAbility(addDef);
        }
    }
}
