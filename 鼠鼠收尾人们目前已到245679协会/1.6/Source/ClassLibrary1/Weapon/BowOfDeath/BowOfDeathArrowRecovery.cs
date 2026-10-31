using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XIYUNTE
{
    public class Hediff_BowOfDeathStuckArrow : Hediff
    {
        public override bool TryMergeWith(Hediff other)
        {
            return false;
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (pawn.health.hediffSet.GetFirstHediffOfDef(DeathBowDefOf.BowOfDeath_StuckArrow) != this)
                return;

            BowOfDeathArrowRecovery.DropNear(pawn, BowOfDeathArrowRecovery.CountStacks(pawn));
        }
    }

    public class Recipe_RemoveBowOfDeathArrow : Recipe_RemoveHediff
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            int before = BowOfDeathArrowRecovery.CountStacks(pawn);
            base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
            int removed = before - BowOfDeathArrowRecovery.CountStacks(pawn);
            BowOfDeathArrowRecovery.DropNear(pawn, removed);
        }
    }

    internal static class BowOfDeathArrowRecovery
    {
        public static int CountStacks(Pawn pawn)
        {
            return pawn.health.hediffSet.GetHediffCount(DeathBowDefOf.BowOfDeath_StuckArrow);
        }

        public static void DropNear(Pawn pawn, int count)
        {
            if (count <= 0)
                return;

            Thing arrows = ThingMaker.MakeThing(DeathBowDefOf.Bow_of_Death_Arrow);
            arrows.stackCount = count;
            Map map = pawn.MapHeld;
            if (map == null)
            {
                Log.Error("Cannot place " + count + " recovered death arrows for " + pawn + " because it has no map.");
                arrows.Destroy();
                return;
            }

            if (!GenPlace.TryPlaceThing(arrows, pawn.PositionHeld, map, ThingPlaceMode.Near))
            {
                Log.Error("Failed to place " + count + " recovered death arrows near " + pawn + ".");
                arrows.Destroy();
            }
        }
    }
}
