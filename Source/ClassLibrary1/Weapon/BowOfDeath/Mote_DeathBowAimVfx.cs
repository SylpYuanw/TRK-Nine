using UnityEngine;
using Verse;

namespace XIYUNTE
{
    // 死之弓蓄力瞄准特效:瞄准开始时挂在装备者身上的烟雾 Mote,四阶蓄力共用。
    // 时间轴:淡入 1 秒(由 MoteDef 的 fadeInTime 驱动)→ 持续期间做呼吸缩放与摇曳位移 → 停止维护后 1 秒淡出并自毁
    // (由 MoteDef 的 fadeOutTime + needsMaintenance/fadeOutUnmaintained 驱动,不需要手写计时与透明度曲线)。
    // 是否继续维护由本类每 tick 查询"装备者是否仍在这把弓的射击动词预热状态"决定,因此射击完成与瞄准被中断都会自然进入淡出。
    public class Mote_DeathBowAimVfx : MoteAttached
    {
        private const float Opacity = 0.7f;
        private const float BreathAmplitude = 0.05f;
        private const float BreathSpeed = 1.05f;
        private const float SwayAmplitudeX = 0.06f;
        private const float SwayAmplitudeZ = 0.04f;
        private const float SwaySpeedX = 0.72f;
        private const float SwaySpeedZ = 0.47f;
        private const float SwayRotationDegrees = 4f;

        // 每个实例给一个随机相位,多把弓同时蓄力时不会整齐划一地一起呼吸。
        private float phase;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            phase = Rand.Range(0f, 100f);
        }

        // 透明度上限 70%:素材本身约 81%~88% 不透明,这里再乘 70%,与淡入淡出曲线相乘后仍受其约束。
        public override float Alpha => base.Alpha * Opacity;

        // 摇曳:位置在水平面上做两条不同频率的正弦位移,与呼吸缩放错开节奏。
        public override Vector3 DrawPos => base.DrawPos + new Vector3(SwayAmplitudeX * Mathf.Sin((AgeSecs + phase) * SwaySpeedX), 0f, SwayAmplitudeZ * Mathf.Sin((AgeSecs + phase) * SwaySpeedZ + 1.7f));

        protected override void TimeInterval(float deltaTime)
        {
            if (IsStillAiming())
            {
                Maintain();
            }
            // 呼吸:整体缓慢缩放;ExactScale 不是可重写成员,因此改写 linearScale(与原版 Mote.Scale 写入同一字段)。
            float t = AgeSecs + phase;
            Scale = 1f + BreathAmplitude * Mathf.Sin(t * BreathSpeed);
            exactRotation = SwayRotationDegrees * Mathf.Sin(t * SwaySpeedX * 0.8f);
            base.TimeInterval(deltaTime);
        }

        // 仍然在瞄准:宿主是同派系存活且在地图上的角色,且当前状态是这把弓的射击动词正在预热。
        private bool IsStillAiming()
        {
            Pawn pawn = link1.Target.Thing as Pawn;
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.MapHeld == null)
            {
                return false;
            }
            Stance_Warmup warmup = pawn.stances?.curStance as Stance_Warmup;
            return warmup != null && warmup.verb is Verb_DeathBowShot && warmup.verb.WarmingUp;
        }
    }
}
