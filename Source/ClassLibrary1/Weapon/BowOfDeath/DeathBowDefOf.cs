using RimWorld;
using Verse;

namespace XIYUNTE
{
    // 死之弓依赖的 Def 引用:箭矢挂载 Hediff、射击后的意识增益 Hediff、补充次数所用的弹药。
    [DefOf]
    public static class DeathBowDefOf
    {
        public static HediffDef BowOfDeath_StuckArrow;

        public static HediffDef BowOfDeath_Consciousness;

        public static ThingDef Bow_of_Death_Arrow;

        static DeathBowDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DeathBowDefOf));
        }
    }
}
