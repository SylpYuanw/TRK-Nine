using UnityEngine;
using Verse;

namespace XIYUNTE
{
    // 死之弓蓄力特效的公共实现:附着在装备者身上,只在这把弓的预热状态存活,并提供呼吸缩放、摇曳位移与轻微旋转。
    // 两个子类只决定"透明度怎么呼吸":主体层反复淡出(强节拍),基底层常驻轻呼吸。
    // 时间轴:按各 MoteDef 的 fadeInTime 淡入 → 持续 → 射击完成或瞄准被中断后停止维护,
    // 由 needsMaintenance/fadeOutUnmaintained 按 fadeOutTime 淡出并自毁(1 秒)。
    // 透明度最终值 = 原版淡入淡出曲线 × 上限 Opacity × 反复呼吸曲线 Pulse(Graphic_Mote.DrawMote 会把它乘进顶点色)。
    public abstract class Mote_DeathBowAimVfxBase : MoteAttached
    {
        protected const float BreathAmplitude = 0.05f;
        protected const float BreathSpeed = 1.05f;
        protected const float SwayAmplitudeX = 0.06f;
        protected const float SwayAmplitudeZ = 0.04f;
        protected const float SwaySpeedX = 0.72f;
        protected const float SwaySpeedZ = 0.47f;
        protected const float SwayRotationDegrees = 4f;

        // 每个实例一个随机相位:多把弓同时蓄力时不会整齐划一地一起呼吸。
        private float phase;

        // 透明度上限(统一压暗用)。
        protected abstract float Opacity { get; }

        // 反复呼吸:最低不透明度与一个完整来回的周期(秒)。
        protected abstract float PulseMinAlpha { get; }
        protected abstract float PulsePeriodSeconds { get; }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            phase = Rand.Range(0f, 100f);
        }

        public override float Alpha => base.Alpha * Opacity * Pulse;

        // 呼吸曲线:在 PulseMinAlpha 与 1 之间来回,周期由子类给定 —— 主体层每 1.7 秒一次"淡出再回来"。
        private float Pulse
        {
            get
            {
                float t = (AgeSecs + phase) * Mathf.PI * 2f / PulsePeriodSeconds;
                return Mathf.Lerp(PulseMinAlpha, 1f, (Mathf.Sin(t) + 1f) * 0.5f);
            }
        }

        // 摇曳:位置在水平面做两条不同频率的正弦位移,与呼吸节奏错开。
        public override Vector3 DrawPos => base.DrawPos + new Vector3(SwayAmplitudeX * Mathf.Sin((AgeSecs + phase) * SwaySpeedX), 0f, SwayAmplitudeZ * Mathf.Sin((AgeSecs + phase) * SwaySpeedZ + 1.7f));

        protected override void TimeInterval(float deltaTime)
        {
            if (IsStillAiming())
            {
                Maintain();
            }
            // 呼吸缩放:ExactScale 不是可重写成员,因此改写 linearScale(与原版 Mote.Scale 写同一字段)。
            float t = AgeSecs + phase;
            Scale = 1f + BreathAmplitude * Mathf.Sin(t * BreathSpeed);
            exactRotation = SwayRotationDegrees * Mathf.Sin(t * SwaySpeedX * 0.8f);
            base.TimeInterval(deltaTime);
        }

        // 仍然在瞄准:宿主是存活且在地图上的角色,且当前状态是这把弓的射击动词正在预热。
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

    // 主体层:蓄力期间反复淡出(最低 8%,周期 1.7 秒),透明度上限 70%,淡出节点就是瞄准结束的那一刻。
    public class Mote_DeathBowAimVfx : Mote_DeathBowAimVfxBase
    {
        protected override float Opacity => 0.7f;
        protected override float PulseMinAlpha => 0.08f;
        protected override float PulsePeriodSeconds => 1.7f;
    }

    // 基底层:全程常驻不消失,只做最低 55%、周期 2.6 秒的轻呼吸,作为主体层下面的底。
    public class Mote_DeathBowAimBaseVfx : Mote_DeathBowAimVfxBase
    {
        protected override float Opacity => 0.85f;
        protected override float PulseMinAlpha => 0.55f;
        protected override float PulsePeriodSeconds => 2.6f;
    }
}
