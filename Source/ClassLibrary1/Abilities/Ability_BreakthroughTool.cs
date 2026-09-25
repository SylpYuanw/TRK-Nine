using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace XIYUNTE
{
    // ========================================================================
    // 「突破工具」武器技能：
    // 冲刺到目标正前方，落地瞬间对目标造成一次钝击，并把目标沿冲刺方向击退若干格；
    // 击退路径撞到不可通行建筑时提前停止并追加伤害。伤害/击退格数/撞墙额外伤害/飞行参数
    // 全部由 XML 传导。
    //
    // 关键设计：
    //   - 冲刺使用贴地飞行器(参考 Flugel 的 Flyer_LeapStrike：heightFactor = 0 + 线性 progressCurve
    //     + 自定义 worker 让 GetHeight 恒为 0)。因此有冲刺动画但角色不腾空。
    //   - 伤害与击退在**落地之后**结算：由 PawnFlyer.RespawnPawn 回调
    //     ICompAbilityEffectOnJumpCompleted.OnJumpCompleted 触发，与参考实现一致；
    //     不需要自建控制器、状态机或常驻 Thing。
    //   - 击退方向 = 冲刺方向(施放者落点 -> 目标)，保持原角度，因此从北向南冲就向南击退，
    //     斜向冲刺则斜向击退。
    //   - 本技能由 CompWeaponTransformer 随「突破工具」形态挂载/卸载(与高功率同一套模型)，
    //     因此进入原版技能列表，冷却能正常结束。
    // ========================================================================

    public static class BreakthroughToolUtility
    {
        // 求「目标正前方」的可站立落点：优先取冲刺方向外侧格，否则取相邻八格中最靠近施放者的一格。
        public static bool TryGetStandCell(Pawn caster, Pawn victim, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (caster == null || victim == null || caster.Map == null)
                return false;

            Map map = caster.Map;
            IntVec3 from = caster.Position;
            IntVec3 to = victim.Position;

            IntVec3 dir = new IntVec3(to.x - from.x, 0, to.z - from.z);
            if (dir.x == 0 && dir.z == 0)
                dir = victim.Rotation.FacingCell;
            dir = NormalizeDirection(dir);

            // 落点必须是目标**靠施放者这一侧**的邻格，即 to - dir：
            // 用 to + dir 会落到目标背后，之后的击退方向(victim - 落点)就会反向。
            IntVec3 preferred = to - dir;
            if (IsValidStandCell(preferred, map, victim))
            {
                cell = preferred;
                return true;
            }

            // 备选：相邻八格中取第一个可站立格，尽量靠近施放者一侧。
            IntVec3 best = IntVec3.Invalid;
            float bestDist = float.MaxValue;
            for (int i = 0; i < GenAdj.AdjacentCellsAround.Length; i++)
            {
                IntVec3 candidate = to + GenAdj.AdjacentCellsAround[i];
                if (!IsValidStandCell(candidate, map, victim))
                    continue;
                float d = (candidate - from).LengthHorizontalSquared;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = candidate;
                }
            }
            if (best.IsValid)
            {
                cell = best;
                return true;
            }
            return false;
        }

        private static bool IsValidStandCell(IntVec3 c, Map map, Pawn victim)
        {
            if (!c.IsValid || !c.InBounds(map) || c == victim.Position)
                return false;
            if (c.Fogged(map))
                return false;
            if (!c.WalkableBy(map, victim))
                return false;
            Building_Door door = c.GetEdifice(map) as Building_Door;
            if (door != null && !door.Open)
                return false;
            return true;
        }

        // 沿固定方向逐格推进，遇到不可通行建筑即停止并记录撞墙；返回实际格数与落点。
        public static KnockbackResult ComputeKnockback(Pawn victim, IntVec3 pushDir, int maxDistance, Map map)
        {
            KnockbackResult result = new KnockbackResult
            {
                distance = 0,
                destCell = victim.Position,
                hitWall = false
            };
            if (map == null || victim == null || !victim.Spawned)
                return result;

            IntVec3 cell = victim.Position;
            for (int step = 0; step < maxDistance; step++)
            {
                IntVec3 next = cell + pushDir;
                if (!next.IsValid || !next.InBounds(map))
                    break;

                Building building = next.GetEdifice(map);
                if (building != null && building.def.passability == Traversability.Impassable)
                {
                    result.hitWall = true;
                    break;
                }

                if (!next.WalkableBy(map, victim))
                    break;

                cell = next;
                result.distance = step + 1;
                result.destCell = cell;
            }
            return result;
        }

        // 落点复核：失效时沿击退方向由远及近取第一个合法格；都不可用则返回原位。
        public static IntVec3 FindFallbackDest(Pawn victim, IntVec3 pushDir, Map map, int maxStep)
        {
            if (victim == null || map == null)
                return victim?.Position ?? IntVec3.Invalid;

            IntVec3 origin = victim.Position;
            for (int step = maxStep; step >= 1; step--)
            {
                IntVec3 c = origin + pushDir * step;
                if (JumpUtility.ValidJumpTarget(victim, map, c))
                    return c;
            }
            return origin;
        }

        // 一次性特效：只投 Fleck 与尘雾，不生成 Thing，因此没有需要回收的对象。
        public static void SpawnFx(FleckDef fleck, IntVec3 cell, Map map)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
                return;

            if (fleck != null)
                FleckMaker.Static(cell.ToVector3Shifted(), map, fleck, 2f);
            FleckMaker.ThrowDustPuff(cell.ToVector3Shifted(), map, 1.2f);
        }

        // 把任意方向归一为八方向之一，保证击退沿直线格推进且角度可复现。
        public static IntVec3 NormalizeDirection(IntVec3 dir)
        {
            return new IntVec3(System.Math.Sign(dir.x), 0, System.Math.Sign(dir.z));
        }
    }

    public struct KnockbackResult
    {
        public int distance;
        public IntVec3 destCell;
        public bool hitWall;
    }

    // 突破工具使用原版 Verb_CastAbility：射程即 verbProps.range(6 格)，按原版欧氏距离判定，
    // 高亮也是原版的圆形环(斜角按欧氏距离缩短)。
    // 不使用自定义方形射程/方形高亮：那会与原版圆形高亮叠加，出现「一个方框 + 一个圆环」。

    // 冲刺飞行器工作器：进度线性推进、高度恒为 0，得到贴地冲刺而不是抛物线跳跃。
    public class PawnFlyerWorker_Breakthrough : PawnFlyerWorker
    {
        public PawnFlyerWorker_Breakthrough(PawnFlyerProperties properties) : base(properties) { }
        public override float AdjustedProgress(float t) => t;
        public override float GetHeight(float t) => 0f;
    }

    public class Ability_BreakthroughTool : Ability
    {
        public Ability_BreakthroughTool() { }
        public Ability_BreakthroughTool(Pawn pawn) : base(pawn) { }
        public Ability_BreakthroughTool(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        // 技能随形态挂载/卸载，此处再做一次防御性校验。
        private bool HasBreakthroughToolEquipped
        {
            get
            {
                Thing_DeliveryBox box = pawn?.equipment?.Primary as Thing_DeliveryBox;
                CompWeaponTransformer transformer = box?.GetComp<CompWeaponTransformer>();
                return transformer?.CurrentMode != null && transformer.CurrentMode.isBreakthroughTool;
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target)
        {
            if (pawn == null || pawn.Dead || !HasBreakthroughToolEquipped)
                return false;
            return base.CanApplyOn(target);
        }

        public override bool GizmoDisabled(out string reason)
        {
            if (!HasBreakthroughToolEquipped)
            {
                reason = "RK_BreakthroughTool_RequiresMode".Translate();
                return true;
            }
            return base.GizmoDisabled(out reason);
        }
    }

    public class CompProperties_BreakthroughTool : CompProperties_AbilityEffect
    {
        // 对目标造成的基础钝击伤害。
        public float damage = 47.5f;
        // 击退格数上限。
        public int knockbackDistance = 5;
        // 击退路径撞到不可通行建筑时追加的伤害。
        public float wallExtraDamage = 20f;
        // 冲刺用贴地飞行器；必须在 XML 指向 heightFactor=0 的飞行器，否则会变成腾空跳跃。
        public ThingDef dashFlyerDef;
        // 释放技能时的音效(默认与切换突破工具形态同一音效)。
        public SoundDef castSound;
        // 命中时的音效(默认宙斯锤钝器命中音效)与冲击波特效。
        public SoundDef impactSound;
        public FleckDef impactFleck;

        public CompProperties_BreakthroughTool()
        {
            compClass = typeof(CompAbilityEffect_BreakthroughTool);
        }
    }

    public class CompAbilityEffect_BreakthroughTool : CompAbilityEffect, ICompAbilityEffectOnJumpCompleted
    {
        public new CompProperties_BreakthroughTool Props => (CompProperties_BreakthroughTool)props;

        // 目标校验：失败时按 throwMessages 给出具体原因，不再静默拒绝。
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            Pawn caster = parent.pawn;
            if (caster == null)
                return false;

            if (!(caster.equipment?.Primary is Thing_DeliveryBox))
            {
                if (throwMessages) Fail("RK_BreakthroughTool_NeedsWeapon", caster);
                return false;
            }

            Pawn victim = target.Pawn;
            if (victim == null)
            {
                if (throwMessages) Fail("RK_BreakthroughTool_NeedsPawn", caster);
                return false;
            }
            if (victim == caster)
            {
                if (throwMessages) Fail("RK_BreakthroughTool_NotSelf", caster);
                return false;
            }
            if (victim.Dead)
            {
                if (throwMessages) Fail("RK_BreakthroughTool_TargetDead", caster);
                return false;
            }
            if (caster.Faction != null && victim.Faction == caster.Faction)
            {
                if (throwMessages) Fail("RK_BreakthroughTool_NotFriendly", caster);
                return false;
            }
            if (!BreakthroughToolUtility.TryGetStandCell(caster, victim, out _))
            {
                if (throwMessages) Fail("RK_BreakthroughTool_NoStandCell", caster);
                return false;
            }
            return true;
        }

        private static void Fail(string key, Pawn caster)
        {
            Messages.Message(key.Translate(), caster, MessageTypeDefOf.RejectInput, historical: false);
        }

        // 施放：只负责起跳——特效 + 贴地飞行器把施放者送到目标正前方。
        // 伤害与击退留到落地回调，保证命中判定发生在落地之后。
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            Pawn victim = target.Pawn;
            if (caster == null || victim == null || caster.Map == null || !victim.Spawned)
                return;
            if (!BreakthroughToolUtility.TryGetStandCell(caster, victim, out IntVec3 standCell))
                return;

            ThingDef flyerDef = Props.dashFlyerDef;
            if (flyerDef == null)
            {
                Log.Error("[BreakthroughTool] dashFlyerDef is not configured; dash was not performed.");
                return;
            }

            Map map = caster.Map;

            // 释放音效：与切换到突破工具形态同一音效(由 XML 配置)。
            Props.castSound?.PlayOneShot(new TargetInfo(caster.Position, map));

            VerbProperties verbProps = parent?.verb?.verbProps;
            PawnFlyer flyer = PawnFlyer.MakeFlyer(
                flyerDef,
                caster,
                standCell,
                verbProps?.flightEffecterDef,
                verbProps?.soundLanding,
                flyWithCarriedThing: false,
                overrideStartVec: null,
                triggeringAbility: parent,
                target: target);

            if (flyer == null)
            {
                Log.Error("[BreakthroughTool] Failed to create dash flyer for " + flyerDef.defName + ".");
                return;
            }

            GenSpawn.Spawn(flyer, standCell, map);
        }

        // 落地回调：PawnFlyer.RespawnPawn 在放下施放者后调用，此时施放者已在目标正前方。
        public void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
        {
            Pawn caster = parent.pawn;
            Pawn victim = target.Pawn;
            if (caster == null || victim == null || caster.Map == null)
                return;
            if (victim.Dead || !victim.Spawned || victim.Map != caster.Map)
                return;

            Map map = caster.Map;
            IntVec3 standCell = caster.Position;

            // 命中：冲击波特效 + 钝器命中音效都在这里播放(不是释放技能时)。
            BreakthroughToolUtility.SpawnFx(Props.impactFleck, victim.Position, map);
            Props.impactSound?.PlayOneShot(new TargetInfo(victim.Position, map));

            // 钝击：47.5 命中躯干，躯干缺失时退化为原版随机部位。
            ApplyBluntDamage(caster, victim, Props.damage);

            if (victim.Dead || !victim.Spawned || victim.Map != map)
                return;

            // 击退方向 = 冲刺方向(落点 -> 目标)，保持原角度：从北向南冲就向南推，斜向冲斜向推。
            // 落点在目标靠施放者一侧(to - dir)，因此这里得到的方向必然是「远离施放者」。
            IntVec3 pushDir = BreakthroughToolUtility.NormalizeDirection(victim.Position - standCell);
            if (pushDir.x == 0 && pushDir.z == 0)
                return;

            KnockbackResult kb = BreakthroughToolUtility.ComputeKnockback(victim, pushDir, Props.knockbackDistance, map);

            // 撞墙：追加固定伤害。
            if (kb.hitWall && Props.wallExtraDamage > 0f)
                ApplyBluntDamage(caster, victim, Props.wallExtraDamage);

            if (victim.Dead || !victim.Spawned)
                return;

            IntVec3 destCell = kb.distance > 0 ? kb.destCell : victim.Position;
            if (destCell != victim.Position && !JumpUtility.ValidJumpTarget(victim, map, destCell))
                destCell = BreakthroughToolUtility.FindFallbackDest(victim, pushDir, map, Props.knockbackDistance);

            if (destCell == victim.Position)
                return;

            // 击退动画：原版「被击飞」飞行器，落地自带眩晕。
            PawnFlyer flyer = PawnFlyer.MakeFlyer(ThingDefOf.PawnFlyer_Stun, victim, destCell, null, null);
            if (flyer != null)
                GenSpawn.Spawn(flyer, destCell, map);
        }

        private static void ApplyBluntDamage(Pawn caster, Pawn target, float amount)
        {
            if (amount <= 0f || target?.health == null)
                return;

            BodyPartRecord torso = target.health.hediffSet.GetNotMissingParts()
                .FirstOrFallback(p => p.def == BodyPartDefOf.Torso);

            DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, amount, 1f, -1f, caster, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            if (torso != null)
                dinfo.SetHitPart(torso);
            target.TakeDamage(dinfo);
        }
    }
}
