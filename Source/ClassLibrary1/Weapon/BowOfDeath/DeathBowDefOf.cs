using RimWorld;
using Verse;

namespace XIYUNTE
{
    // 死之弓依赖的 Def 引用:箭矢挂载 Hediff、射击后的意识增益 Hediff、补充次数所用的弹药、蓄力阶切换浮现印迹。
    [DefOf]
    public static class DeathBowDefOf
    {
        public static HediffDef BowOfDeath_StuckArrow;

        public static HediffDef BowOfDeath_Consciousness;

        public static ThingDef Bow_of_Death_Arrow;

        // 蓄力阶切换特效的 Mote 定义(Defs/Misc/Mote_BowOfDeath.xml)。
        public static ThingDef BowOfDeath_StageVfx;

        // 蓄力瞄准特效的 Mote 定义(Defs/Misc/Mote_BowOfDeath.xml)。
        public static ThingDef BowOfDeath_AimVfx;

        // 箭头蓄力扩散层(沿瞄准方向定向、由 Shader 向后推移)的 Mote 定义(Defs/Misc/Mote_BowOfDeath.xml)。
        public static ThingDef BowOfDeath_AimChargeVfx;

        // 蓄力流线层(沿瞄准方向定向)的 Mote 定义(Defs/Misc/Mote_BowOfDeath.xml)。
        public static ThingDef BowOfDeath_AimStreakVfx;

        static DeathBowDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DeathBowDefOf));
        }
    }
}
