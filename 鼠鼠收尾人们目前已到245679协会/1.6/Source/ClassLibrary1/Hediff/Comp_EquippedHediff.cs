using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XIYUNTE
{
    // 单一装备状态：同一 HediffDef 可被多件已装备物品共同引用。
    // 装备时在缺失的情况下添加；卸下时仅在没有任何其他同组件、同 HediffDef 的装备时移除。
    public class CompProperties_EquippedHediff : CompProperties
    {
        public HediffDef hediffDef;

        public CompProperties_EquippedHediff()
        {
            compClass = typeof(Comp_EquippedHediff);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }

            if (hediffDef == null)
            {
                yield return parentDef.defName + " has CompProperties_EquippedHediff without hediffDef.";
            }
        }
    }

    public class Comp_EquippedHediff : ThingComp
    {
        public CompProperties_EquippedHediff Props => (CompProperties_EquippedHediff)props;

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);

            if (Props.hediffDef == null || pawn?.health?.hediffSet == null)
            {
                return;
            }

            if (pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef) == null)
            {
                pawn.health.AddHediff(Props.hediffDef);
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);

            if (Props.hediffDef == null || pawn?.health?.hediffSet == null)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
            if (hediff == null || HasOtherEquippedSource(pawn))
            {
                return;
            }

            pawn.health.RemoveHediff(hediff);
        }

        private bool HasOtherEquippedSource(Pawn pawn)
        {
            if (pawn.equipment != null)
            {
                List<ThingWithComps> equipment = pawn.equipment.AllEquipmentListForReading;
                for (int i = 0; i < equipment.Count; i++)
                {
                    if (equipment[i] != parent && UsesSameHediff(equipment[i]))
                    {
                        return true;
                    }
                }
            }

            if (pawn.apparel != null)
            {
                List<Apparel> apparel = pawn.apparel.WornApparel;
                for (int i = 0; i < apparel.Count; i++)
                {
                    if (apparel[i] != parent && UsesSameHediff(apparel[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool UsesSameHediff(ThingWithComps thing)
        {
            Comp_EquippedHediff comp = thing.TryGetComp<Comp_EquippedHediff>();
            return comp != null && comp.Props.hediffDef == Props.hediffDef;
        }
    }
}
