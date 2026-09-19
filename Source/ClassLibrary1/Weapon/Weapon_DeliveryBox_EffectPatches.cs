using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XIYUNTE
{
    // 派送箱形态特性补丁:多功能工具连击、崩解锤对建筑增伤、本体形态负重加成。
    // 三处都挂在原版结算链路上,不新增自定义 ManeuverDef/ToolCapacityDef,也不改变原版近战伤害与冷却的来源。

    // 多功能工具连击:每次挥击结算后独立掷一次概率,触发时把本次挥击再结算两次,合计 3 次完整攻击。
    // 额外攻击反射回入原版 TryCastShot,因此命中、闪避、伤害、命中音效与战斗日志都按完整攻击各自结算。
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    internal static class Patch_Verb_MeleeAttack_DeliveryBoxMultiAttack
    {
        private static readonly MethodInfo TryCastShotMethod = AccessTools.Method(typeof(Verb_MeleeAttack), "TryCastShot");

        // 额外攻击的重入守卫:反射调用会再次进入本补丁,用线程静态标记阻止递归触发。
        [ThreadStatic] private static bool resolvingExtraAttack;

        static void Postfix(Verb_MeleeAttack __instance)
        {
            if (resolvingExtraAttack) return;
            CompWeaponTransformer transformer = __instance.EquipmentSource?.GetComp<CompWeaponTransformer>();
            WeaponMode mode = transformer?.CurrentMode;
            if (mode == null || mode.multiAttackChance <= 0f || mode.multiAttackCount <= 1) return;
            if (mode.tools == null || !mode.tools.Contains(__instance.tool)) return;
            if (!Rand.Chance(mode.multiAttackChance)) return;
            resolvingExtraAttack = true;
            try
            {
                for (int i = 1; i < mode.multiAttackCount; i++)
                {
                    if (__instance.CurrentTarget.ThingDestroyed) break;
                    InvokeTryCastShot(__instance);
                }
            }
            finally
            {
                resolvingExtraAttack = false;
            }
        }

        // 反射调用原版近战结算:反射异常解包后原样抛出,避免把原版错误静默吞掉。
        private static void InvokeTryCastShot(Verb_MeleeAttack verb)
        {
            try
            {
                TryCastShotMethod.Invoke(verb, null);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }
    }

    // 崩解锤对建筑增伤:原版 DamageWorker.Apply 按 DamageDef.buildingDamageFactor 计算建筑伤害,
    // 此处按当前形态配置的最终倍率换算为等价的伤害量,只对建筑生效,对 Pawn 伤害路径无影响。
    [HarmonyPatch(typeof(DamageWorker), nameof(DamageWorker.Apply))]
    internal static class Patch_DamageWorker_Apply_DeliveryBoxBuildingFactor
    {
        static void Prefix(ref DamageInfo dinfo, Thing victim)
        {
            if (victim.def.category != ThingCategory.Building) return;
            CompWeaponTransformer transformer = (dinfo.Instigator as Pawn)?.equipment?.Primary?.GetComp<CompWeaponTransformer>();
            WeaponMode mode = transformer?.CurrentMode;
            if (mode == null || mode.buildingDamageFactor <= 0f) return;
            DamageDef def = dinfo.Def;
            if (def == null) return;
            float currentFactor = def.buildingDamageFactor * ((victim.def.passability == Traversability.Impassable) ? def.buildingDamageFactorImpassable : def.buildingDamageFactorPassable);
            if (currentFactor <= 0f) return;
            dinfo.SetAmount(dinfo.Amount * (mode.buildingDamageFactor / currentFactor));
        }
    }

    // 本体形态负重加成:1.6 的小人负重与商队载重由 MassUtility.Capacity 按体形(BodySize × 35kg)计算,
    // 没有对应 StatDef,装备偏移与 Hediff 都无法接入,因此在原版结果上叠加当前形态配置的负重加成。
    [HarmonyPatch(typeof(MassUtility), nameof(MassUtility.Capacity), new Type[] { typeof(Pawn), typeof(StringBuilder) })]
    internal static class Patch_MassUtility_Capacity_DeliveryBox
    {
        static void Postfix(Pawn p, StringBuilder explanation, ref float __result)
        {
            CompWeaponTransformer transformer = p?.equipment?.Primary?.GetComp<CompWeaponTransformer>();
            float bonus = transformer?.CurrentMode?.massCapacityBonus ?? 0f;
            if (bonus <= 0f) return;
            __result += bonus;
            if (explanation != null)
            {
                if (explanation.Length > 0) explanation.AppendLine();
                explanation.Append("  - " + p.equipment.Primary.LabelCap + ": +" + bonus.ToString("0.#") + " " + "kg".Translate());
            }
        }
    }
}
