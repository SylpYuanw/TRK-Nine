using UnityEngine;

namespace XIYUNTE
{
    // 弓头随瞄准旋转；身体保持直立，水平分量控制镜像与横向偏移。
    public static class DeathBowVfxGeometry
    {
        // 流线贴图有效红色线段的横向中心(UV,0~1)。贴图内可见像素位于 x 5~155,中心 0.3125,
        // 即有效内容整体偏画布左侧;画布中心必须按该差值的反方向前移,红色线段的中点才会落在弓上。
        private const float StreakContentCenterU = 0.3125f;

        // 全部瞄准图层相对弓身绘制中心沿画布纵轴的共用偏移(格),负值指向弓线下方的持握侧。
        // 流线与弓头层必须取同一值,否则两层会沿弓线方向错开。
        public const float LineSideOffset = -0.2f;

        // 流线有效线段以弓身绘制中心为界，后侧与前侧各占50%。
        public static float StreakCenter(float weaponAimDistance, float length)
        {
            return weaponAimDistance + length * (0.5f - StreakContentCenterU);
        }

        public static float BodySide(float aimAngle)
        {
            float side = Mathf.Sin(aimAngle * Mathf.Deg2Rad);
            return Mathf.Abs(side) < 0.00001f ? 0f : side;
        }

        public static Vector3 BodyLocalToWorld(Vector3 local, float aimAngle)
        {
            return new Vector3(local.x * BodySide(aimAngle), local.y, local.z);
        }

        public static float MirrorSign(float aimAngle)
        {
            float angle = Mathf.Repeat(aimAngle, 360f);
            return angle > 180f && angle < 360f ? -1f : 1f;
        }

        public static float Rotation(float aimAngle)
        {
            return aimAngle - 90f;
        }

        public static Vector3 LocalToWorld(Vector3 local, float aimAngle)
        {
            local.z *= MirrorSign(aimAngle);
            return Quaternion.AngleAxis(Rotation(aimAngle), Vector3.up) * local;
        }
    }
}
