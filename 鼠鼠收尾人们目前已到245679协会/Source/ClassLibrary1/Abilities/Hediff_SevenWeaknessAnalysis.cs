using RimWorld;
using Verse;

namespace XIYUNTE
{
    public class HediffCompProperties_SevenWeaknessAnalysis : HediffCompProperties
    {
        public HediffDef weaknessHediff;
        public int durationTicks = 7500;

        public HediffCompProperties_SevenWeaknessAnalysis()
        {
            compClass = typeof(HediffComp_SevenWeaknessAnalysis);
        }
    }

    public class HediffComp_SevenWeaknessAnalysis : HediffComp
    {
        public HediffCompProperties_SevenWeaknessAnalysis Props => (HediffCompProperties_SevenWeaknessAnalysis)props;

        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            if (verb == null || (!verb.IsMeleeAttack && !verb.verbProps.Ranged) || verb.CasterPawn != Pawn || Pawn.Dead || parent.Severity < parent.def.maxSeverity)
            {
                return;
            }

            Pawn victim = target.Pawn;
            if (victim == null || victim.Dead || victim.health?.hediffSet == null)
            {
                return;
            }

            Hediff weakness = victim.health.hediffSet.GetFirstHediffOfDef(Props.weaknessHediff);
            if (weakness == null)
            {
                weakness = HediffMaker.MakeHediff(Props.weaknessHediff, victim);
                weakness.Severity = 1f;
                victim.health.AddHediff(weakness);
            }

            weakness.TryGetComp<HediffComp_Disappears>().SetDuration(Props.durationTicks);
        }
    }
}
