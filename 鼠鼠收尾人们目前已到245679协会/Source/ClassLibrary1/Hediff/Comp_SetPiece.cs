using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XIYUNTE
{
    // 通用套装件计数：同一套装的所有部件引用同一个 setHediff。
    // 装备时 severity +1，卸下时 -1；上限由 HediffDef.maxSeverity 约束。
    public class CompProperties_SetPiece : CompProperties
    {
        public HediffDef setHediff;

        // 套装集齐(severity 达到 setHediff.maxSeverity)时授予的技能；为空则不处理。
        public AbilityDef abilityDef;

        public CompProperties_SetPiece()
        {
            compClass = typeof(Comp_SetPiece);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }

            if (setHediff == null)
            {
                yield return parentDef.defName + " has CompProperties_SetPiece without setHediff.";
            }
        }
    }

    public class Comp_SetPiece : ThingComp
    {
        public CompProperties_SetPiece Props => (CompProperties_SetPiece)props;

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);

            if (Props.setHediff == null || pawn?.health?.hediffSet == null)
            {
                return;
            }

            Hediff set = pawn.health.hediffSet.GetFirstHediffOfDef(Props.setHediff);
            if (set == null)
            {
                set = HediffMaker.MakeHediff(Props.setHediff, pawn);
                set.Severity = 1f;
                pawn.health.AddHediff(set);
            }
            else
            {
                set.Severity += 1f;
            }

            SyncAbility(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);

            if (Props.setHediff == null || pawn?.health?.hediffSet == null)
            {
                return;
            }

            Hediff set = pawn.health.hediffSet.GetFirstHediffOfDef(Props.setHediff);
            if (set == null)
            {
                return;
            }

            if (set.Severity <= 1f)
            {
                pawn.health.RemoveHediff(set);
            }
            else
            {
                set.Severity -= 1f;
            }

            SyncAbility(pawn);
        }

        // 套装集齐时授予技能。技能实例不回收：RemoveAbility 会丢掉冷却(冷却存在 Ability 实例里)，
        // 未集齐时由技能自身隐藏按钮(CompAbilityEffect_ZweiAura.ShouldHideGizmo)。
        private void SyncAbility(Pawn pawn)
        {
            if (Props.abilityDef == null || Props.setHediff == null || pawn?.abilities == null)
                return;
            if (pawn.abilities.GetAbility(Props.abilityDef) != null)
                return;

            Hediff set = pawn.health.hediffSet.GetFirstHediffOfDef(Props.setHediff);
            if (set != null && set.Severity >= Props.setHediff.maxSeverity)
                pawn.abilities.GainAbility(Props.abilityDef);
        }
    }
}
