using UnityEngine;
using Verse;

namespace XIYUNTE
{
    // 死之弓蓄力特效的公共实现:附着在装备者身上,只在这把弓的预热状态存活,并提供呼吸缩放、摇曳位移与轻微旋转。
    // 子类只决定三件事:透明度上限与呼吸节奏,以及朝向与位移取"摇曳"还是"沿瞄准方向定向"。
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

        // 朝向与位移的默认实现是摇曳;沿瞄准方向定向的层覆写下面两个成员。
        protected virtual float RotationAt(float t)
        {
            return SwayRotationDegrees * Mathf.Sin(t * SwaySpeedX * 0.8f);
        }

        protected virtual Vector3 OffsetAt(float t)
        {
            return new Vector3(SwayAmplitudeX * Mathf.Sin(t * SwaySpeedX), 0f, SwayAmplitudeZ * Mathf.Sin(t * SwaySpeedZ + 1.7f));
        }

        // 呼吸缩放的振幅:0 = 尺寸恒定,沿瞄准方向定向的层据此关掉尺寸呼吸。
        protected virtual float BreathScaleAmplitude => BreathAmplitude;

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
        public override Vector3 DrawPos => base.DrawPos + OffsetAt(AgeSecs + phase);

        protected override void TimeInterval(float deltaTime)
        {
            if (IsStillAiming())
            {
                Maintain();
            }
            // 呼吸缩放:ExactScale 不是可重写成员,因此改写 linearScale(与原版 Mote.Scale 写同一字段)。
            float t = AgeSecs + phase;
            Scale = 1f + BreathScaleAmplitude * Mathf.Sin(t * BreathSpeed);
            exactRotation = RotationAt(t);
            base.TimeInterval(deltaTime);
        }

        // 仍然在瞄准:宿主是存活且在地图上的角色,且当前状态是这把弓的射击动词正在预热。
        private bool IsStillAiming()
        {
            return CurrentWarmup != null;
        }

        // 当前预热状态:宿主缺失/已死/离图,或当前状态不是这把弓的射击动词时返回 null。
        protected Stance_Warmup CurrentWarmup
        {
            get
            {
                Pawn pawn = link1.Target.Thing as Pawn;
                if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.MapHeld == null)
                {
                    return null;
                }
                Stance_Warmup warmup = pawn.stances?.curStance as Stance_Warmup;
                return warmup != null && warmup.verb is Verb_DeathBowShot && warmup.verb.WarmingUp ? warmup : null;
            }
        }

        // 瞄准角(AngleFlat 口径:0 = 北、90 = 东、180 = 南、270 = 西):装备者到预热目标的水平方向,
        // 与原版武器绘制取的是同一份数据(Stance_Busy.focusTarg)。
        // 预热未建立,或目标与装备者几乎重合时返回 false,调用方保留上一次角度。
        protected bool TryGetAimAngle(out float angle)
        {
            angle = 0f;
            Stance_Warmup warmup = CurrentWarmup;
            Pawn pawn = link1.Target.Thing as Pawn;
            if (warmup == null || pawn == null || !warmup.focusTarg.IsValid)
            {
                return false;
            }
            Vector3 delta = warmup.focusTarg.CenterVector3 - pawn.DrawPos;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.01f)
            {
                return false;
            }
            angle = delta.AngleFlat();
            return true;
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

    // 流线层:贴图长轴指向角色面朝方向,并沿该方向附加一个前移量,使线段落在弓的绘制位置上,
    // 覆盖"弓前(正前方)至身后(正后方)"一段。
    // 角度口径与武器绘制完全一致:Mote 与武器都是"用 Quaternion.AngleAxis(角度, Vector3.up) 画一张平面贴图",
    // 因此"面朝角 - 90"就是让贴图长轴指向面朝方向的取值(武器绘制取的是 面朝角 - 90 + equippedAngleOffset)。
    // 与两层烟雾不同,它随面朝方向定向,因此不做摇曳旋转与尺寸呼吸,瞄准期间稳定显示,淡入/淡出各 1 秒。
    public class Mote_DeathBowAimStreakVfx : Mote_DeathBowAimVfxBase
    {
        // 武器绘制时的前移量:原版 PawnRenderUtility.DrawEquipmentAndApparelExtras 用 0.4 + 武器 Def 的 equippedDistanceOffset。
        private const float WeaponDrawDistance = 0.4f;

        // 当前面朝角(AngleFlat 口径);预热未建立时保留上一次取值。
        private float aimAngle;

        protected override float Opacity => 0.8f;

        // 最低不透明度与上限相同,呼吸曲线恒为 1(流线不反复淡出)。
        protected override float PulseMinAlpha => 1f;
        protected override float PulsePeriodSeconds => 1f;

        // 尺寸恒定:流线长度只由 Def 的 drawSize.x 决定。
        protected override float BreathScaleAmplitude => 0f;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            UpdateAimAngle();
        }

        // 朝向 = 面朝方向:与原版武器绘制同一套角度口径,不需要额外的贴图旋转量。
        protected override float RotationAt(float t)
        {
            return aimAngle - 90f;
        }

        // 位移 = 沿面朝方向前移(等价于在角色局部坐标系里加一个 Z 偏移),与弓的落点一致。
        protected override Vector3 OffsetAt(float t)
        {
            return new Vector3(0f, 0f, WeaponDrawDistance + EquippedDistanceOffset).RotatedBy(aimAngle);
        }

        protected override void TimeInterval(float deltaTime)
        {
            UpdateAimAngle();
            base.TimeInterval(deltaTime);
        }

        // 武器 Def 上的额外持握距离(本武器未配置时为 0),与武器绘制同源,避免流线与弓错位。
        private float EquippedDistanceOffset
        {
            get
            {
                Pawn pawn = link1.Target.Thing as Pawn;
                Thing weapon = pawn?.equipment?.Primary;
                return weapon == null ? 0f : weapon.def.equippedDistanceOffset;
            }
        }

        private void UpdateAimAngle()
        {
            float angle;
            if (TryGetAimAngle(out angle))
            {
                aimAngle = angle;
                return;
            }
            // 预热状态取不到时(刚生成、瞄准已结束进入淡出)退回角色当前朝向,方向不跳变。
            Pawn pawn = link1.Target.Thing as Pawn;
            if (pawn != null)
            {
                aimAngle = pawn.Rotation.AsAngle;
            }
        }
    }
}
