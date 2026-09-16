using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XIYUNTE
{
    // 程序集启动时注册 Harmony,扫描并应用本文件内的全部补丁。
    [StaticConstructorOnStartup]
    public static class DeliveryBoxStatPatchesInit
    {
        static DeliveryBoxStatPatchesInit()
        {
            new Harmony("XIYUNTE.DeliveryBoxStatWorkerPatches").PatchAll();
        }
    }

    // 原版 StatWorker_MeleeAverageDPS / MeleeAverageArmorPenetration 读取静态 ThingDef.tools,
    // 无法感知派送箱的当前形态。以下补丁仅在该武器是派送箱具体实例时,
    // 用当前形态的 tools 重新执行原版计算,使武器卡"近战每秒伤害/护甲穿透"随形态刷新并保留品质/材质加成;
    // 其他武器或抽象查看(无具体实例)保持原版行为。
    internal static class DeliveryBoxStatPatchShared
    {
        // 取派送箱实例的当前形态 tools;非派送箱或无形态数据时返回 false,补丁不介入。
        public static bool TryGetCurrentModeTools(StatRequest req, out List<Tool> tools)
        {
            tools = null;
            if (!(req.Thing is Thing_DeliveryBox box)) return false;
            CompWeaponTransformer transformer = box.GetComp<CompWeaponTransformer>();
            if (transformer == null || transformer.CurrentMode.tools == null || transformer.CurrentMode.tools.Count == 0) return false;
            tools = transformer.CurrentMode.tools;
            return true;
        }

        // 按当前形态 tools 重算 DPS:加权伤害均值 / 加权冷却均值,与原版 GetValueUnfinalized 的具体实例分支一致。
        public static float RecalculateDPS(StatRequest req, List<Tool> tools)
        {
            Pawn attacker = StatWorker_MeleeAverageDPS.GetCurrentWeaponUser(req.Thing);
            float num = (from x in VerbUtility.GetAllVerbProperties(null, tools)
                where x.verbProps.IsMeleeAttack
                select x).AverageWeighted(
                    (VerbUtility.VerbPropertiesWithSource x) => x.verbProps.AdjustedMeleeSelectionWeight(x.tool, attacker, req.Thing, null, comesFromPawnNativeVerbs: false),
                    (VerbUtility.VerbPropertiesWithSource x) => x.verbProps.AdjustedMeleeDamageAmount(x.tool, attacker, req.Thing, null));
            float num2 = (from x in VerbUtility.GetAllVerbProperties(null, tools)
                where x.verbProps.IsMeleeAttack
                select x).AverageWeighted(
                    (VerbUtility.VerbPropertiesWithSource x) => x.verbProps.AdjustedMeleeSelectionWeight(x.tool, attacker, req.Thing, null, comesFromPawnNativeVerbs: false),
                    (VerbUtility.VerbPropertiesWithSource x) => x.verbProps.AdjustedCooldown(x.tool, attacker, req.Thing));
            if (num2 == 0f) return 0f;
            return num / num2;
        }

        // 按当前形态 tools 重算平均护甲穿透,与原版 GetValueUnfinalized 的具体实例分支一致。
        public static float RecalculateAP(StatRequest req, List<Tool> tools)
        {
            Pawn attacker = StatWorker_MeleeAverageDPS.GetCurrentWeaponUser(req.Thing);
            return (from x in VerbUtility.GetAllVerbProperties(null, tools)
                where x.verbProps.IsMeleeAttack
                select x).AverageWeighted(
                    (VerbUtility.VerbPropertiesWithSource x) => x.verbProps.AdjustedMeleeSelectionWeight(x.tool, attacker, req.Thing, null, comesFromPawnNativeVerbs: false),
                    (VerbUtility.VerbPropertiesWithSource x) => x.verbProps.AdjustedArmorPenetration(x.tool, attacker, req.Thing, null));
        }

        // 重建 DPS 展开说明:每把 tool 一行,列出实际伤害与每秒攻击次数。
        public static string RebuildDPSExplanation(StatRequest req, List<Tool> tools)
        {
            StringBuilder sb = new StringBuilder();
            Pawn currentWeaponUser = StatWorker_MeleeAverageDPS.GetCurrentWeaponUser(req.Thing);
            foreach (VerbUtility.VerbPropertiesWithSource item in from x in VerbUtility.GetAllVerbProperties(null, tools)
                where x.verbProps.IsMeleeAttack
                select x)
            {
                float num = item.verbProps.AdjustedMeleeDamageAmount(item.tool, currentWeaponUser, req.Thing, null);
                float num2 = item.verbProps.AdjustedCooldown(item.tool, currentWeaponUser, req.Thing);
                AppendToolLine(sb, item, num.ToString("F1") + " " + "DamageLower".Translate(), num2.ToString("F2") + " " + "SecondsPerAttackLower".Translate());
            }
            return sb.ToString();
        }

        // 重建护甲穿透展开说明:每把 tool 一行,列出实际穿透百分比。
        public static string RebuildAPExplanation(StatRequest req, List<Tool> tools)
        {
            StringBuilder sb = new StringBuilder();
            Pawn currentWeaponUser = StatWorker_MeleeAverageDPS.GetCurrentWeaponUser(req.Thing);
            foreach (VerbUtility.VerbPropertiesWithSource item in from x in VerbUtility.GetAllVerbProperties(null, tools)
                where x.verbProps.IsMeleeAttack
                select x)
            {
                float f = item.verbProps.AdjustedArmorPenetration(item.tool, currentWeaponUser, req.Thing, null);
                AppendToolLine(sb, item, f.ToStringPercent(), null);
            }
            return sb.ToString();
        }

        private static void AppendToolLine(StringBuilder sb, VerbUtility.VerbPropertiesWithSource item, string valueLine, string secondLine)
        {
            if (item.tool != null)
            {
                sb.AppendLine("  " + item.tool.LabelCap + " (" + item.ToolCapacity.label + ")");
            }
            else
            {
                sb.AppendLine(string.Format("  {0}:", "StatsReport_NonToolAttack".Translate()));
            }
            sb.AppendLine("    " + valueLine);
            if (secondLine != null)
            {
                sb.AppendLine("    " + secondLine);
            }
        }
    }

    [HarmonyPatch(typeof(StatWorker_MeleeAverageDPS), nameof(StatWorker_MeleeAverageDPS.GetValueUnfinalized), new Type[] { typeof(StatRequest), typeof(bool) })]
    internal static class Patch_MeleeAverageDPS_GetValue
    {
        static void Postfix(StatRequest req, ref float __result)
        {
            if (DeliveryBoxStatPatchShared.TryGetCurrentModeTools(req, out List<Tool> tools))
            {
                __result = DeliveryBoxStatPatchShared.RecalculateDPS(req, tools);
            }
        }
    }

    [HarmonyPatch(typeof(StatWorker_MeleeAverageDPS), nameof(StatWorker_MeleeAverageDPS.GetExplanationUnfinalized), new Type[] { typeof(StatRequest), typeof(ToStringNumberSense) })]
    internal static class Patch_MeleeAverageDPS_Explanation
    {
        static void Postfix(StatRequest req, ref string __result)
        {
            if (DeliveryBoxStatPatchShared.TryGetCurrentModeTools(req, out List<Tool> tools))
            {
                __result = DeliveryBoxStatPatchShared.RebuildDPSExplanation(req, tools);
            }
        }
    }

    [HarmonyPatch(typeof(StatWorker_MeleeAverageArmorPenetration), nameof(StatWorker_MeleeAverageArmorPenetration.GetValueUnfinalized), new Type[] { typeof(StatRequest), typeof(bool) })]
    internal static class Patch_MeleeAverageAP_GetValue
    {
        static void Postfix(StatRequest req, ref float __result)
        {
            if (DeliveryBoxStatPatchShared.TryGetCurrentModeTools(req, out List<Tool> tools))
            {
                __result = DeliveryBoxStatPatchShared.RecalculateAP(req, tools);
            }
        }
    }

    [HarmonyPatch(typeof(StatWorker_MeleeAverageArmorPenetration), nameof(StatWorker_MeleeAverageArmorPenetration.GetExplanationUnfinalized), new Type[] { typeof(StatRequest), typeof(ToStringNumberSense) })]
    internal static class Patch_MeleeAverageAP_Explanation
    {
        static void Postfix(StatRequest req, ref string __result)
        {
            if (DeliveryBoxStatPatchShared.TryGetCurrentModeTools(req, out List<Tool> tools))
            {
                __result = DeliveryBoxStatPatchShared.RebuildAPExplanation(req, tools);
            }
        }
    }
}
