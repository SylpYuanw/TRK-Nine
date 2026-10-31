using UnityEngine;
using Verse;

namespace XIYUNTE
{
    // 死之弓蓄力特效的公共实现:附着在装备者身上,只在这把弓的预热状态存活,提供层呼吸、尺寸呼吸与瞄准角读取。
    // 子类决定三件事:透明度上限与呼吸节奏、朝向与位移的取法、以及是否做尺寸呼吸。
    // 时间轴:按各 MoteDef 的 fadeInTime 淡入 → 持续 → 射击完成或瞄准被中断后停止维护,
    // 由 needsMaintenance/fadeOutUnmaintained 按 fadeOutTime 淡出并自毁(1 秒)。
    // 透明度最终值 = 原版淡入淡出曲线 × 上限 Opacity × Pulse；自定义图层通过实例PropertyBlock传递。
    public abstract class Mote_DeathBowAimVfxBase : MoteAttached
    {
        protected const float BreathAmplitude = 0.05f;
        protected const float BreathSpeed = 1.05f;

        // 每个实例一个随机相位:多把弓同时蓄力时不会整齐划一地一起呼吸。
        private float phase;

        public float EffectSeed => phase;

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

        // 呼吸曲线在PulseMinAlpha与1之间变化；最低值为1时不叠加整层呼吸。
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
    // 贴图 +X 轴指向瞄准方向；左半平面先镜像局部纵轴，再对画布和偏移使用同一旋转。
    // 局部 +X 为角色正前方；东西两向的局部 +Z 均对应画面上方。
    // MoteAttached 会把 Def 的 attachedDrawOffset 按世界坐标预先加进宿主位置(见 Verse/MoteAttached.cs),
    // 这里扣掉原值再加旋转后的值,等价于把该字段解释为随瞄准方向一起旋转的局部偏移。
    public abstract class Mote_DeathBowAimAlignedVfx : Mote_DeathBowAimVfxBase
    {
        // 当前瞄准角(AngleFlat 口径);预热未建立时保留上一次取值。
        private float aimAngle;
        private bool hasAimAngle;
        private Vector3 weaponDrawOffset;
        protected float WeaponAimDistance { get; private set; }
        protected virtual bool FollowWeaponDrawOffset => false;
        protected float AimAngle => aimAngle;


        protected virtual bool MirrorWithAim => true;

        protected override float RotationAt(float t)
        {
            return DeathBowVfxGeometry.Rotation(aimAngle) + SwayRotationAt(t);
        }

        protected override Vector3 OffsetAt(float t)
        {
            return DeathBowVfxGeometry.LocalToWorld(LocalOffsetAt(t), aimAngle) - def.mote.attachedDrawOffset
                + (FollowWeaponDrawOffset ? weaponDrawOffset : Vector3.zero);
        }

        // 子类的局部偏移(单位:格)。默认取 Def 自身的 attachedDrawOffset,即该字段提供局部偏移，镜像和旋转由本类完成。
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
            exactRotation = RotationAt(AgeSecs);
            linearScale.z *= MirrorWithAim ? DeathBowVfxGeometry.MirrorSign(aimAngle) : 1f;
        }

        protected override void TimeInterval(float deltaTime)
        {
            UpdateAimAngle();
            base.TimeInterval(deltaTime);
            linearScale.z = Mathf.Abs(linearScale.z) * (MirrorWithAim ? DeathBowVfxGeometry.MirrorSign(aimAngle) : 1f);
        }

        private void UpdateAimAngle()
        {
            float angle;
            if (TryGetAimAngle(out angle))
            {
                aimAngle = angle;
                hasAimAngle = true;
            }
            // 首次生成尚无有效瞄准时取角色朝向；进入淡出后保留最后一个有效瞄准角。
            Pawn pawn = link1.Target.Thing as Pawn;
            if (!hasAimAngle && pawn != null)
            {
                aimAngle = pawn.Rotation.AsAngle;
            }
            // 淡出后保留最后一次有效武器偏移，避免卸下武器时特效跳回原点。
            if (FollowWeaponDrawOffset && pawn?.equipment?.Primary is Thing_DeathBow bow)
            {
                weaponDrawOffset = DeathBowAimDrawOffsetPatch.OffsetFor(bow.GetComp<CompEquippable_DeathBow>().Props, aimAngle);
                WeaponAimDistance = (0.4f + bow.def.equippedDistanceOffset)
                    * pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;
            }
        }
    }

    // 身体画布保持直立；水平瞄准分量控制左右镜像和倾斜，脚源高度恒定。
    // 局部消散由Shader节点执行，Mote仅控制瞄准开始和结束时的淡入淡出。
    public class Mote_DeathBowAimVfx : Mote_DeathBowAimAlignedVfx
    {
        protected override float Opacity => 0.7f;
        protected override float PulseMinAlpha => 1f;
        protected override float PulsePeriodSeconds => 1.7f;
        protected override float BreathScaleAmplitude => 0f;
        protected override bool MirrorWithAim => false;
        public float BodyAimSide => DeathBowVfxGeometry.BodySide(AimAngle);

        protected override float RotationAt(float t) => 0f;

        protected override Vector3 OffsetAt(float t)
        {
            return DeathBowVfxGeometry.BodyLocalToWorld(def.mote.attachedDrawOffset, AimAngle)
                - def.mote.attachedDrawOffset;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            DeathBowVfxTextures.Apply(Graphic.MatSingle, def.graphicData.drawSize, def.shortHash);
        }
    }

    // 流线层:贴图长轴指向角色面朝方向,并沿该方向附加一个前移量,使线段落在弓的绘制位置上,
    // 覆盖"弓前(正前方)至身后(正后方)"一段。
    // 与烟雾层不同,它不做摇曳旋转与尺寸呼吸,瞄准期间稳定显示,淡入/淡出各 1 秒。
    public class Mote_DeathBowAimStreakVfx : Mote_DeathBowAimAlignedVfx
    {
        // 原版流线材质保留原网格绕序；两层自定义Shader以Cull Off支持实例镜像。
        protected override bool MirrorWithAim => false;
        protected override bool FollowWeaponDrawOffset => true;

        protected override float Opacity => 0.8f;

        // 最低不透明度与上限相同,呼吸曲线恒为 1(流线不反复淡出)。
        protected override float PulseMinAlpha => 1f;
        protected override float PulsePeriodSeconds => 1f;

        // 尺寸恒定:流线长度只由 Def 的 drawSize.x 决定。
        protected override float BreathScaleAmplitude => 0f;

        // 流线沿画布纵向的偏移复用几何常量，与弓头层取同一值。
        // 以武器实际绘制中心为分界，将流线长度分配为身体侧50%、射击侧50%，
        // 再沿画布纵向偏移，使红色线段落在弓线上。
        protected override Vector3 LocalOffsetAt(float t)
        {
            return new Vector3(DeathBowVfxGeometry.StreakCenter(WeaponAimDistance, def.graphicData.drawSize.x), 0f, DeathBowVfxGeometry.LineSideOffset);
        }

    }

    // 弓头风雾层：Shader以风形RGB作遮罩，无缝噪声提供滚动形变与消散。
    // 本类绑定噪声贴图和画布纵横比；朝向取瞄准角 - 90，局部偏移按同一角度旋转，
    // 左半平面同步镜像局部纵轴；身体层单独保持画面竖直方向。
    // 与流线层相同:不做尺寸呼吸与反复淡出,瞄准期间稳定显示,淡入/淡出各 1 秒。
    public class Mote_DeathBowAimChargeVfx : Mote_DeathBowAimAlignedVfx
    {
        protected override bool FollowWeaponDrawOffset => true;
        // 噪声路径相对MOD共用Textures目录，不含扩展名；主风形图由Def的texPath加载。
        protected override float Opacity => 0.9f;
        protected override float PulseMinAlpha => 1f;
        protected override float PulsePeriodSeconds => 1f;

        // 画布尺寸恒定，范围和局部角度由Shader分别控制。
        protected override float BreathScaleAmplitude => 0f;

        // 弓头画布与流线层共用同一条弓线纵向偏移，保证两层沿弓线方向不错开；
        // attachedDrawOffset 只负责沿瞄准方向的头部对位。
        protected override Vector3 LocalOffsetAt(float t)
        {
            return new Vector3(def.mote.attachedDrawOffset.x + WeaponAimDistance - 0.4f, def.mote.attachedDrawOffset.y, DeathBowVfxGeometry.LineSideOffset);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            DeathBowVfxTextures.Apply(Graphic.MatSingle, def.graphicData.drawSize, def.shortHash);
        }

    }

    internal static class DeathBowVfxTextures
    {
        private const string NoiseTexPath = "VFX/DeathBow_ChargeNoise";

        // MaterialPool提供主风形图；共享材质补绑固定噪声纹理，不逐实例创建材质。
        public static void Apply(Material material, Vector2 drawSize, int errorId)
        {
            if (material == null)
            {
                Log.ErrorOnce("Bow of death vfx material is missing.", errorId ^ 0x5B0A);
                return;
            }
            // 以画布高度为距离单位，纵横比用于保持非正方形画布中的实际角度。
            material.SetFloat("_CanvasAspect", drawSize.x / drawSize.y);
            Texture2D noiseTexture = ContentFinder<Texture2D>.Get(NoiseTexPath);
            if (noiseTexture == null)
            {
                Log.ErrorOnce("Bow of death vfx noise texture is missing: " + NoiseTexPath, errorId ^ 0x5B0B);
                return;
            }
            // Shader默认纹理非null，按目标纹理引用判断绑定状态。
            if (material.GetTexture("_StartTex") == noiseTexture)
            {
                return;
            }
            material.SetTexture("_StartTex", noiseTexture);
        }
    }
}
