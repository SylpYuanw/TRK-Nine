using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XIYUNTE
{
    // ThingDef.SpecialDisplayStats 输出的远程条目(瞄准时间/伤害/护甲穿透/射程/抑止等)全部取武器 Def 上的主动词与默认弹丸,
    // 属于静态值,蓄力阶切换后不会变化,会和实例动态条目重复。
    // 这里只在信息卡对象是死之弓实例时过滤掉这批静态远程条目,改由 Thing_DeathBow.SpecialDisplayStats 按当前阶输出;
    // 其他武器与其他查看对象(无具体实例的 Def 浏览)保持原版行为。
    // 注意:本程序集的 Harmony 由 StatWorker_MeleeAveragePatches.cs 中的 [StaticConstructorOnStartup] 统一安装
    // (Harmony.PatchAll() 作用于整个程序集),本文件不再单独建初始化器,避免同一补丁被重复挂载。
    [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats), new Type[] { typeof(StatRequest) })]
    internal static class Patch_ThingDef_SpecialDisplayStats
    {
        private static void Postfix(StatRequest req, ref IEnumerable<StatDrawEntry> __result)
        {
            if (!(req.Thing is Thing_DeathBow))
            {
                return;
            }
            List<StatDrawEntry> entries = new List<StatDrawEntry>();
            foreach (StatDrawEntry entry in __result)
            {
                if (entry.category == StatCategoryDefOf.Weapon_Ranged)
                {
                    continue;
                }
                entries.Add(entry);
            }
            __result = entries;
        }
    }
}
