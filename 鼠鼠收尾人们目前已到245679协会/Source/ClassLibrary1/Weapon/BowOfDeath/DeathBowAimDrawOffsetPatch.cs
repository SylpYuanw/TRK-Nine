using HarmonyLib;
using UnityEngine;
using Verse;

namespace XIYUNTE
{
    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming))]
    public static class DeathBowAimDrawOffsetPatch
    {
        // 死之弓瞄准绘制前按瞄准方向叠加 XML 配置的世界坐标偏移。
        [HarmonyPrefix]
        public static void Prefix(Thing eq, ref Vector3 drawLoc, float aimAngle)
        {
            Thing_DeathBow bow = eq as Thing_DeathBow;
            if (bow == null)
            {
                return;
            }

            Properties_DeathBow props = bow.GetComp<CompEquippable_DeathBow>().Props;
            Rot4 direction = Rot4.FromAngleFlat(aimAngle);
            if (direction == Rot4.East)
            {
                drawLoc += props.aimDrawOffsetEast;
            }
            else if (direction == Rot4.West)
            {
                drawLoc += props.aimDrawOffsetWest;
            }
            else if (direction == Rot4.North)
            {
                drawLoc += props.aimDrawOffsetNorth;
            }
            else if (direction == Rot4.South)
            {
                drawLoc += props.aimDrawOffsetSouth;
            }
        }
    }
}
