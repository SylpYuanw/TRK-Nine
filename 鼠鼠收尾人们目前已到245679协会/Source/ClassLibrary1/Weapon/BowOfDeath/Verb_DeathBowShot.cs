using Verse;

namespace XIYUNTE
{
    // 死之弓射击动词:瞄准时长与弹丸取自当前蓄力阶;只有闪弓模式且仍有次数时才可用。
    // 每次射击成功后扣一次次数并触发意识增益,命中判定、掩体与精度全部沿用原版射击链路。
    public class Verb_DeathBowShot : Verb_Shoot
    {
        private CompEquippable_DeathBow Bow => EquipmentSource?.GetComp<CompEquippable_DeathBow>();

        public override float WarmupTime
        {
            get
            {
                DeathBowStage stage = Bow?.CurrentStage;
                if (stage != null && stage.warmupTime > 0f)
                {
                    return stage.warmupTime;
                }
                return base.WarmupTime;
            }
        }

        public override ThingDef Projectile
        {
            get
            {
                ThingDef stageProjectile = Bow?.CurrentStage?.projectile;
                if (stageProjectile != null)
                {
                    return stageProjectile;
                }
                return base.Projectile;
            }
        }

        public override bool Available()
        {
            CompEquippable_DeathBow bow = Bow;
            if (bow == null)
            {
                return false;
            }
            return bow.FlashBowActive && !bow.IsSwitching && bow.Charges > 0 && base.Available();
        }

        protected override bool TryCastShot()
        {
            bool fired = base.TryCastShot();
            if (fired)
            {
                Bow?.Notify_ShotFired(CasterPawn);
            }
            return fired;
        }

        // 瞄准开始:预热状态建立后通知武器组件挂上瞄准特效;近战动词不经过这里,因此只有四阶蓄力射击会出特效。
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            bool started = base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
            if (started && WarmingUp)
            {
                Bow?.Notify_AimStarted();
            }
            return started;
        }
    }
}
