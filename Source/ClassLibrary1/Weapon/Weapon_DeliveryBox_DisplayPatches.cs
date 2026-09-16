using HarmonyLib;
using Verse;

namespace XIYUNTE
{
    // 近战攻击 Gizmo 的提示描述改写点。
    // 原版 VerbTracker.CreateVerbTargetCommand 把描述写死为 ownerThing.LabelCap + ThingDef.description,
    // 而该命令对象由 GizmoGridDrawer 每帧重建,在构造阶段改写会落在每帧高频路径上。
    // Command.GizmoOnGUIInt 只在鼠标悬停且允许 tooltip 时读取 Command.Desc,
    // 因此改在 Desc 求值处按当前形态覆盖描述,未悬停时不产生任何开销。
    // 图标不在此处处理:Command_VerbTarget.DrawIcon 经 Widgets.ThingIcon 读取 Thing.UIIconOverride,
    // 已由 Thing_DeliveryBox.UIIconOverride 提供缓存的当前形态贴图。
    [HarmonyPatch(typeof(Command), "get_Desc")]
    internal static class Patch_Command_Desc
    {
        // 仅对派送箱实例改写描述;其他命令在其他 Command 子类上按原版文本返回。
        static void Postfix(Command __instance, ref string __result)
        {
            if (!(__instance is Command_VerbTarget verbTarget)) return;
            Thing_DeliveryBox box = verbTarget.ownerThing as Thing_DeliveryBox;
            if (box == null) return;
            string modeDescription = box.GizmoTooltipDescription;
            if (modeDescription == null) return;
            __result = box.LabelCap + ": " + modeDescription;
        }
    }
}
