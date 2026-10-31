using HarmonyLib;
using RimWorld;
using Verse;

namespace XIYUNTE
{
    // 远程闪避:单位受击时按属性值掷骰,命中则把该次伤害整段吸收。
    // 挂在 Pawn.PreApplyDamage:只有单位受击才进入判定,建筑与物品的伤害不进入;
    // 判定发生在伤害 worker 之前,因此不结算护甲耐久、不产生伤口 Hediff,也不触发 PostApplyDamage 类效果。
    // 判定条件为远程伤害(isRanged)且非爆炸伤害(isExplosive)。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
    internal static class Patch_Pawn_PreApplyDamage_RangeDodge
    {
        private static StatDef rangeDodgeStat;

        [HarmonyPrefix]
        private static bool Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (!dinfo.Def.isRanged || dinfo.Def.isExplosive)
            {
                return true;
            }
            if (rangeDodgeStat == null)
            {
                rangeDodgeStat = DefDatabase<StatDef>.GetNamedSilentFail("TRK_RangeDodgeChance");
                if (rangeDodgeStat == null)
                {
                    return true;
                }
            }
            if (Rand.Chance(__instance.GetStatValue(rangeDodgeStat)))
            {
                absorbed = true;
                return false;
            }
            return true;
        }
    }
}
