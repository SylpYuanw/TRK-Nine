using RimWorld;
using Verse;

namespace XIYUNTE
{
    // 死之弓箭矢:命中血肉单位后挂载"箭矢"Hediff,每个单位最多 4 层。
    // 效果挂在弹丸命中上,命中判定失败(打空或打在掩体上)不会触发。
    public class Projectile_DeathBowArrow : Bullet
    {
        private const int MaxArrowStacks = 4;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            base.Impact(hitThing, blockedByShield);
            Pawn pawn = hitThing as Pawn;
            if (pawn == null || pawn.Dead || pawn.health?.hediffSet == null || pawn.RaceProps == null || !pawn.RaceProps.IsFlesh)
            {
                return;
            }
            HediffDef arrowDef = DeathBowDefOf.BowOfDeath_StuckArrow;
            if (pawn.health.hediffSet.GetHediffCount(arrowDef) >= MaxArrowStacks)
            {
                return;
            }
            pawn.health.AddHediff(arrowDef, pawn.RaceProps.body.corePart);
        }
    }
}
