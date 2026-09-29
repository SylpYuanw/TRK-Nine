using UnityEngine;
using Verse;

namespace XIYUNTE
{
    // 死之弓蓄力特效的公共实现:附着在装备者身上,只在这把弓的预热状态存活,提供层呼吸、尺寸呼吸与瞄准角读取。
    // 子类决定三件事:透明度上限与呼吸节奏、朝向与位移的取法、以及是否做尺寸呼吸。
    // 时间轴:按各 MoteDef 的 fadeInTime 淡入 → 持续 → 射击完成或瞄准被中断后停止维护,
    // 由 needsMaintenance/fadeOutUnmaintained 按 fadeOutTime 淡出并自毁(1 秒)。
    // 透明度最终值 = 原版淡入淡出曲线 × 上限 Opacity × 反复呼吸曲线 Pulse(Graphic_Mote.DrawMote 会把它乘进顶点色)。
    public abstract class Mote_DeathBowAimVfxBase : MoteAttached
    {
        protected const float BreathAmplitude = 0.05f;
        protected const float BreathSpeed = 1.05f;

        // 每个实例一个随机相位:多把弓同时蓄力时不会整齐划一地一起呼吸。
        private float phase;

        // 透明度上限(统一压暗用)。
        protected abstract float Opacity { get; }

        // 反复呼吸:最低不透明度与一个完整来回的周期(秒)。
        protected abstract float PulseMinAlpha { get; }
        protected abstract float PulsePeriodSeconds { get; }

        // 朝向与位移由子类给出:沿瞄准方向定向的层在 Mote_DeathBowAimAlignedVfx 里覆写。
        protected abstract float RotationAt(float t);

        protected virtual Vector3 OffsetAt(float t)
        {
            return Vector3.zero;
        }

        // 呼吸缩放的振幅:0 = 尺寸恒定,不做缩放的层据此关掉尺寸呼吸。
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

        // 位置 = 宿主绘制位置 + 子类给出的位移(0 表示只跟随宿主)。
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

    // 沿瞄准方向定向的层:每 tick 从预热状态重取瞄准角,朝向取"瞄准角 - 90",与武器绘制、流线层同一套口径,
    // 因此贴图 +X 轴始终指向瞄准方向;位移把局部偏移按同一个"瞄准角 - 90"旋转:
    // 局部 +X 为瞄准方向(角色正前方),局部 +Z 为瞄准方向的左侧,两者与贴图在屏幕上的纵横方向一致。
    // MoteAttached 会把 Def 的 attachedDrawOffset 按世界坐标预先加进宿主位置(见 Verse/MoteAttached.cs),
    // 这里扣掉原值再加旋转后的值,等价于把该字段解释为随瞄准方向一起旋转的局部偏移。
    public abstract class Mote_DeathBowAimAlignedVfx : Mote_DeathBowAimVfxBase
    {
        // 当前瞄准角(AngleFlat 口径);预热未建立时保留上一次取值。
        private float aimAngle;

        protected override float RotationAt(float t)
        {
            return aimAngle - 90f + SwayRotationAt(t);
        }

        protected override Vector3 OffsetAt(float t)
        {
            return LocalOffsetAt(t).RotatedBy(aimAngle - 90f) - def.mote.attachedDrawOffset;
        }

        // 子类的局部偏移(单位:格)。默认取 Def 自身的 attachedDrawOffset,即该字段只提供大小,旋转由本类完成。
        protected virtual Vector3 LocalOffsetAt(float t)
        {
            return def.mote.attachedDrawOffset;
        }

        // 叠加在瞄准方向上的小角度摆动,默认不摆动。
        protected virtual float SwayRotationAt(float t)
        {
            return 0f;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            UpdateAimAngle();
        }

        protected override void TimeInterval(float deltaTime)
        {
            UpdateAimAngle();
            base.TimeInterval(deltaTime);
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

    // 主体层:蓄力期间反复淡出(最低 8%,周期 1.7 秒),透明度上限 70%,淡出节点就是瞄准结束的那一刻。
    // 朝向与位移跟随瞄准方向:Def 的 attachedDrawOffset 决定烟雾场沿瞄准方向的位置,再叠加一层局部的轻微摇曳。
    public class Mote_DeathBowAimVfx : Mote_DeathBowAimAlignedVfx
    {
        private const float SwayAmplitudeX = 0.06f;
        private const float SwayAmplitudeZ = 0.04f;
        private const float SwaySpeedX = 0.72f;
        private const float SwaySpeedZ = 0.47f;
        private const float SwayRotationDegrees = 4f;

        protected override float Opacity => 0.7f;
        protected override float PulseMinAlpha => 0.08f;
        protected override float PulsePeriodSeconds => 1.7f;

        protected override float SwayRotationAt(float t)
        {
            return SwayRotationDegrees * Mathf.Sin(t * SwaySpeedX * 0.8f);
        }

        protected override Vector3 LocalOffsetAt(float t)
        {
            return def.mote.attachedDrawOffset
                + new Vector3(SwayAmplitudeX * Mathf.Sin(t * SwaySpeedX), 0f, SwayAmplitudeZ * Mathf.Sin(t * SwaySpeedZ + 1.7f));
        }
    }

    // 流线层:贴图长轴指向角色面朝方向,并沿该方向附加一个前移量,使线段落在弓的绘制位置上,
    // 覆盖"弓前(正前方)至身后(正后方)"一段。
    // 与烟雾层不同,它不做摇曳旋转与尺寸呼吸,瞄准期间稳定显示,淡入/淡出各 1 秒。
    public class Mote_DeathBowAimStreakVfx : Mote_DeathBowAimAlignedVfx
    {
        // 武器绘制时的前移量:独立调参常量,不决定武器本体位置,武器 Def 的 equippedDistanceOffset 会叠加。
        private const float WeaponDrawDistance = -1f;

        protected override float Opacity => 0.8f;

        // 最低不透明度与上限相同,呼吸曲线恒为 1(流线不反复淡出)。
        protected override float PulseMinAlpha => 1f;
        protected override float PulsePeriodSeconds => 1f;

        // 尺寸恒定:流线长度只由 Def 的 drawSize.x 决定。
        protected override float BreathScaleAmplitude => 0f;

        // 位移 = 沿瞄准方向前移,与弓的落点一致。
        protected override Vector3 LocalOffsetAt(float t)
        {
            return new Vector3(WeaponDrawDistance + EquippedDistanceOffset, 0f, 0f);
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
    }

    // 扩散层:贴图是四条红色能量条(按朝右射击绘制:头在右、尾在左),Shader 负责把整图沿画布 -X 逐份向后推移,
    // 本类只决定朝向与位置:朝向 = 瞄准角 - 90,位置 = Def 的局部偏移按瞄准角旋转,
    // 因此朝正上方瞄准时整块画布落在角色正下方,朝右上方瞄准时落在左下方。
    // 与流线层相同:不做尺寸呼吸与反复淡出,瞄准期间稳定显示,淡入/淡出各 1 秒。
    public class Mote_DeathBowAimChargeVfx : Mote_DeathBowAimAlignedVfx
    {
        protected override float Opacity => 0.9f;
        protected override float PulseMinAlpha => 1f;
        protected override float PulsePeriodSeconds => 1f;

        // 尺寸恒定:扩散只由 Shader 的平移完成,不做任何缩放,避免贴图被压缩。
        protected override float BreathScaleAmplitude => 0f;
    }
}
