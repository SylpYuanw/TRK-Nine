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
            drawLoc += OffsetFor(props, aimAngle);
        }

        // 武器与附着在弓上的蓄力特效共用四向世界偏移。
        public static Vector3 OffsetFor(Properties_DeathBow props, float aimAngle)
        {
            Rot4 direction = Rot4.FromAngleFlat(aimAngle);
            if (direction == Rot4.East)
            {
                return props.aimDrawOffsetEast;
            }
            else if (direction == Rot4.West)
            {
                return props.aimDrawOffsetWest;
            }
            else if (direction == Rot4.North)
            {
                return props.aimDrawOffsetNorth;
            }
            else if (direction == Rot4.South)
            {
                return props.aimDrawOffsetSouth;
            }
            return Vector3.zero;
        }
    }
}
