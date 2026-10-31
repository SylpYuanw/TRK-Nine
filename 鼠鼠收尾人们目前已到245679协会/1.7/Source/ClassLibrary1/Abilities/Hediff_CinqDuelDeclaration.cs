using RimWorld;
using UnityEngine;
using Verse;

namespace XIYUNTE
{
    public class CompProperties_CinqDuelDeclaration : CompProperties_AbilityGiveHediff
    {
        public ThoughtDef moodThought;
        public HediffDef setHediff;

        public CompProperties_CinqDuelDeclaration()
        {
            compClass = typeof(CompAbilityEffect_CinqDuelDeclaration);
        }

    }

    public class CompAbilityEffect_CinqDuelDeclaration : CompAbilityEffect_GiveHediff
    {
        public new CompProperties_CinqDuelDeclaration Props => (CompProperties_CinqDuelDeclaration)props;

        public override bool ShouldHideGizmo => !HasFullSet(parent.pawn);

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            parent.pawn.needs.mood.thoughts.memories.TryGainMemory(Props.moodThought);
        }

        private bool HasFullSet(Pawn pawn)
        {
            HediffDef setDef = Props.setHediff;
            if (setDef == null || pawn?.health?.hediffSet == null)
            {
                return false;
            }

            Hediff set = pawn.health.hediffSet.GetFirstHediffOfDef(setDef);
            return set != null && set.Severity >= setDef.maxSeverity;
        }

    }

    public class HediffCompProperties_CinqDuelDeclaration : HediffCompProperties
    {
        public int maxStacks = 4;

        public HediffCompProperties_CinqDuelDeclaration()
        {
            compClass = typeof(HediffComp_CinqDuelDeclaration);
        }
    }

    public class HediffComp_CinqDuelDeclaration : HediffComp
    {
        public HediffCompProperties_CinqDuelDeclaration Props => (HediffCompProperties_CinqDuelDeclaration)props;

        private int Stacks => Mathf.RoundToInt(parent.Severity) - 1;

        public override string CompLabelInBracketsExtra => Stacks + "/" + Props.maxStacks;

        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            if (!(verb is Verb_MeleeAttack) || verb.CasterPawn != Pawn || Pawn.Dead)
            {
                return;
            }

            float maxSeverity = 1f + Props.maxStacks;
            if (parent.Severity < maxSeverity)
            {
                parent.Severity = Mathf.Min(parent.Severity + 1f, maxSeverity);
            }
        }

        public override string CompDebugString()
        {
            return "duel stacks: " + Stacks + "/" + Props.maxStacks;
        }
    }
}
