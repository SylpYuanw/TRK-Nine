using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace XIYUNTE
{
    // ========================================================================
    // 「六协会」套装：
    // 内衬(liuA)与制服(liuB)各挂 Comp_LiuPiece，装备时把 RK_LiuSet 的 severity 累加 1、
    // 卸下时减 1。因此**只有一个 Hediff** 就表达进度：
    //   severity 1 = 1/2  无任何效果
    //   severity 2 = 2/2  套装成员，获得意识加成并向外提供光环
    // 加成完全由光环发放，且**每个来源一份独立增益实例**（Hediff_LiuAuraBuff.TryMergeWith 返回
    // false 阻止同 def 合并），因此同时处于 N 个成员的圈内就是 N*6% 意识，不设上限。
    // 圈半径由每个成员按自己周围的成员数算出：基础半径 + (人数-1) * 每人格数。
    //
    // 检测不扫描装备：套装进度由装备/卸下事件维护在 Hediff 上（Hediff 随存档保存），
    // 光环组件只负责定期扫描**附近成员**并发放增益。
    // ========================================================================

    [DefOf]
    public static class LiuSetDefOf
    {
        public static HediffDef RK_LiuSet;
        public static HediffDef RK_LiuAuraBuff;
    }

    // 套装状态：severity = 已穿戴的件数(1 或 2)，即 1/2 与 2/2。
    public class Hediff_LiuSet : HediffWithComps
    {
    }

    // 光环增益：每个来源一份独立实例，禁止同 def 合并，使加成按份数叠加。
    public class Hediff_LiuAuraBuff : HediffWithComps
    {
        public override bool TryMergeWith(Hediff other) => false;
    }

    public class HediffCompProperties_LiuAuraLink : HediffCompProperties
    {
        public HediffCompProperties_LiuAuraLink()
        {
            compClass = typeof(HediffComp_LiuAuraLink);
        }
    }

    // 记录本份增益由哪个成员提供：刷新与查找都靠它区分来源，因此不会互相覆盖。
    public class HediffComp_LiuAuraLink : HediffComp
    {
        public Pawn source;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref source, "source");
        }
    }

    // 着装件组件：装备/卸下时维护套装进度。挂在六协会内衬与制服两件上。
    public class CompProperties_LiuPiece : CompProperties
    {
        public CompProperties_LiuPiece()
        {
            compClass = typeof(Comp_LiuPiece);
        }
    }

    public class Comp_LiuPiece : ThingComp
    {
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            LiuSetUtility.AddPiece(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            LiuSetUtility.RemovePiece(pawn);
        }
    }

    public class HediffCompProperties_LiuSetAura : HediffCompProperties
    {
        // 单人时的基础光环半径(格)。
        public float baseRange = 6f;
        // 每多一名套装成员增加的光环半径(格)。
        public float rangePerExtraPawn = 2f;
        // 光环扫描间隔(tick)；0.5 秒 = 30 tick。
        public int scanIntervalTicks = 30;

        public HediffCompProperties_LiuSetAura()
        {
            compClass = typeof(HediffComp_LiuSetAura);
        }
    }

    // 光环组件：挂在套装状态 Hediff 上；只有 2/2 的成员才向外提供增益。
    // 每次扫描按自身周围的成员数算出自己的圈半径，再给圈内每个套装成员发放一份属于自己的增益。
    public class HediffComp_LiuSetAura : HediffComp
    {
        public HediffCompProperties_LiuSetAura Props => (HediffCompProperties_LiuSetAura)props;

        // 复用缓冲，避免每次扫描分配新 List。
        private readonly List<Pawn> candidatesBuffer = new List<Pawn>();
        private readonly List<Pawn> wearersBuffer = new List<Pawn>();

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn self = Pawn;
            if (self == null || self.Dead || !self.Spawned || self.Map == null)
                return;
            // 仅 2/2 成员发光；1/2 无任何效果。
            if (!LiuSetUtility.IsTwoOfTwo(self))
                return;
            if (LiuSetDefOf.RK_LiuAuraBuff == null)
                return;

            int interval = Props.scanIntervalTicks > 0 ? Props.scanIntervalTicks : 30;
            if (!self.IsHashIntervalTick(interval))
                return;

            Map map = self.Map;
            LiuSetUtility.CollectCandidates(self, candidatesBuffer);
            LiuSetUtility.CollectWearers(candidatesBuffer, wearersBuffer);

            float range = LiuSetUtility.ComputeRange(self, wearersBuffer, Props.baseRange, Props.rangePerExtraPawn);

            for (int i = 0; i < wearersBuffer.Count; i++)
            {
                Pawn target = wearersBuffer[i];
                if (!LiuSetUtility.IsValidTarget(target, map))
                    continue;
                // 方形光环：按绝对值判定，斜角与横竖同格数。
                if (!LiuSetUtility.InAuraRange(self.Position, target.Position, range))
                    continue;

                // 每个来源一份实例：目标可同时持有来自多个成员的增益，从而叠加。
                LiuSetUtility.GrantAuraFrom(target, self, interval + 5);
            }
        }
    }

    // 光环可视化：仿原版角色能力(领袖命令)的表现 —— 地面上的**方形光环** + 成员之间的**连线**。
    // 只在「该 Pawn 被选中」时绘制，不常驻，避免满屏特效。
    // 用绝对值方形绘制(横竖斜同格数)，边长随成员数增长：1 人、2 人、3 人各不同。
    // 借用每帧渲染 Pawn 的时机(PawnRenderer.RenderPawnAt)，并做一次/帧去重。
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    internal static class Patch_PawnRenderer_LiuAuraOverlay
    {
        // 橙红：与 HediffDef 的 defaultLabelColor 同一色系。
        private static readonly Color RingColor = new Color(1f, 0.42f, 0.12f, 0.95f);
        private static readonly Material LineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.48f, 0.16f, 0.9f));

        private static int lastDrawnFrame = -1;
        private static readonly List<Pawn> wearersBuffer = new List<Pawn>();
        // 方形光环的格子缓冲，复用避免每帧新建 List。
        private static readonly List<IntVec3> squareCells = new List<IntVec3>();

        static void Postfix()
        {
            // 同一帧内 RenderPawnAt 会对每个 Pawn 各调一次，这里只处理第一次。
            if (lastDrawnFrame == Time.frameCount)
                return;
            lastDrawnFrame = Time.frameCount;

            Map map = Find.CurrentMap;
            if (map == null)
                return;

            List<object> selected = Find.Selector?.SelectedObjectsListForReading;
            if (selected == null)
                return;

            for (int i = 0; i < selected.Count; i++)
            {
                Pawn pawn = selected[i] as Pawn;
                if (pawn == null || pawn.Map != map || pawn.Dead || !pawn.Spawned)
                    continue;
                if (!LiuSetUtility.IsTwoOfTwo(pawn))
                    continue;

                DrawFor(pawn, map);
            }
        }

        // 画该成员的方形光环 + 到范围内每个成员的连线。
        private static void DrawFor(Pawn pawn, Map map)
        {
            float range = LiuSetUtility.GetCurrentAuraRange(pawn, wearersBuffer);
            DrawSquare(map, pawn.Position, range);

            Vector3 from = pawn.DrawPos;
            for (int i = 0; i < wearersBuffer.Count; i++)
            {
                Pawn other = wearersBuffer[i];
                if (other == null || other == pawn || other.Map != map)
                    continue;
                if (!LiuSetUtility.InAuraRange(pawn.Position, other.Position, range))
                    continue;
                GenDraw.DrawLineBetween(from, other.DrawPos, LineMat);
            }
        }

        // 用绝对值方形(切比雪夫距离 <= range)收集格子后描边，得到方形光环而不是圆形环。
        private static void DrawSquare(Map map, IntVec3 center, float range)
        {
            int r = Mathf.Max(0, Mathf.RoundToInt(range));
            squareCells.Clear();
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    IntVec3 cell = new IntVec3(center.x + dx, 0, center.z + dz);
                    if (!cell.InBounds(map))
                        continue;
                    squareCells.Add(cell);
                }
            }
            if (squareCells.Count > 0)
                GenDraw.DrawFieldEdges(squareCells, RingColor);
        }
    }

    // 套装进度维护与光环发放。
    public static class LiuSetUtility
    {
        // 满足套装所需的件数。
        public const int PiecesForFullSet = 2;
        // severity 比较容差。
        private const float Epsilon = 0.001f;

        // 套装进度 +1：没有则新建(severity 1)，已有则累加(受 maxSeverity 上限约束)。
        public static void AddPiece(Pawn pawn)
        {
            if (pawn?.health == null)
                return;

            HediffDef def = LiuSetDefOf.RK_LiuSet;
            if (def == null)
            {
                Log.ErrorOnce("Missing HediffDef for Liu set; piece will not be counted.", 0x4C6976);
                return;
            }

            Hediff set = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (set == null)
            {
                set = HediffMaker.MakeHediff(def, pawn);
                set.Severity = 1f;
                pawn.health.AddHediff(set);
            }
            else if (set.Severity < PiecesForFullSet - Epsilon)
            {
                set.Severity += 1f;
            }
        }

        // 套装进度 -1：降到 0 则移除，避免留下 0/2 的空状态。
        public static void RemovePiece(Pawn pawn)
        {
            if (pawn?.health == null)
                return;

            Hediff set = GetSet(pawn);
            if (set == null)
                return;

            float next = set.Severity - 1f;
            if (next <= Epsilon)
                pawn.health.RemoveHediff(set);
            else
                set.Severity = next;
        }

        public static Hediff GetSet(Pawn pawn)
        {
            HediffDef def = LiuSetDefOf.RK_LiuSet;
            if (def == null || pawn?.health == null)
                return null;
            return pawn.health.hediffSet.GetFirstHediffOfDef(def);
        }

        // 是否 2/2：唯一判定依据是套装 Hediff 的 severity，不读取穿戴列表。
        public static bool IsTwoOfTwo(Pawn pawn)
        {
            Hediff set = GetSet(pawn);
            return set != null && set.Severity >= PiecesForFullSet - Epsilon;
        }

        // 光环候选者：同阵营角色；无阵营时退回地图全部角色(与原版领导者光环同语义)。
        public static void CollectCandidates(Pawn self, List<Pawn> outBuffer)
        {
            outBuffer.Clear();
            Map map = self?.Map;
            if (map == null)
                return;

            if (self.Faction != null)
            {
                List<Pawn> factionPawns = map.mapPawns.SpawnedPawnsInFaction(self.Faction);
                for (int i = 0; i < factionPawns.Count; i++)
                    outBuffer.Add(factionPawns[i]);
                return;
            }

            IReadOnlyList<Pawn> all = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < all.Count; i++)
                outBuffer.Add(all[i]);
        }

        // 从候选者中筛出 2/2 成员，供本 tick 内半径计算与发放共用。
        public static void CollectWearers(List<Pawn> candidates, List<Pawn> outBuffer)
        {
            outBuffer.Clear();
            if (candidates == null)
                return;

            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn pawn = candidates[i];
                if (!IsValidTarget(pawn, pawn?.Map))
                    continue;
                if (IsTwoOfTwo(pawn))
                    outBuffer.Add(pawn);
            }
        }

        public static bool IsValidTarget(Pawn target, Map map)
        {
            return target != null
                && !target.Dead
                && target.Spawned
                && target.Map == map
                && target.RaceProps.Humanlike
                && target.health != null;
        }

        // 光环按「绝对值」判定：max(|dx|,|dz|) <= range —— 横、竖、斜都是同样的格数，
        // 斜角不因欧氏距离而缩短，因此光环形状是方形而不是圆形。
        public static bool InAuraRange(IntVec3 a, IntVec3 b, float range)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.z - b.z)) <= range;
        }

        // 供可视化使用：按当前成员分布算出 self 的当前范围值，并把范围内的成员写入 outBuffer。
        // 每帧会被调用，因此候选者缓冲复用静态字段，不新建 List。
        private static readonly List<Pawn> candidatesBufferForRange = new List<Pawn>();

        public static float GetCurrentAuraRange(Pawn self, List<Pawn> outBuffer)
        {
            outBuffer.Clear();
            HediffCompProperties_LiuSetAura props = LiuSetDefOf.RK_LiuSet?.CompProps<HediffCompProperties_LiuSetAura>();
            float baseRange = props?.baseRange ?? 6f;
            float perExtra = props?.rangePerExtraPawn ?? 2f;

            if (self?.Map == null)
                return baseRange;

            CollectCandidates(self, candidatesBufferForRange);
            CollectWearers(candidatesBufferForRange, outBuffer);
            return ComputeRange(self, outBuffer, baseRange, perExtra);
        }

        // 范围值 = 基础范围 + (基础方形范围内的成员数 - 1) * 每人增量；人数含自身。
        // 人数按绝对值方形统计，与光环形状一致。
        public static float ComputeRange(Pawn self, List<Pawn> wearers, float baseRange, float rangePerExtraPawn)
        {
            if (self == null || wearers == null)
                return baseRange;

            int count = 0;
            for (int i = 0; i < wearers.Count; i++)
            {
                Pawn other = wearers[i];
                if (other == null || other.Dead || !other.Spawned || other.Map != self.Map)
                    continue;
                if (!InAuraRange(self.Position, other.Position, baseRange))
                    continue;
                count++;
            }

            int extra = count > 1 ? count - 1 : 0;
            return baseRange + extra * rangePerExtraPawn;
        }

        // 给目标发放/刷新「来自 source 的这一份」增益；不同来源各占一份实例，因此加成叠加。
        public static void GrantAuraFrom(Pawn target, Pawn source, int lingerTicks)
        {
            HediffDef auraDef = LiuSetDefOf.RK_LiuAuraBuff;
            if (target?.health == null || auraDef == null || source == null)
                return;

            Hediff mine = FindAuraFrom(target, auraDef, source);
            if (mine == null)
            {
                mine = HediffMaker.MakeHediff(auraDef, target);
                mine.Severity = 1f;
                HediffComp_LiuAuraLink link = mine.TryGetComp<HediffComp_LiuAuraLink>();
                if (link == null)
                {
                    Log.ErrorOnce(auraDef.defName + " needs HediffCompProperties_LiuAuraLink; aura buff was not applied.", auraDef.shortHash ^ 0x5A18);
                    return;
                }
                link.source = source;
                target.health.AddHediff(mine);
            }

            HediffComp_Disappears disappears = mine.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
                disappears.ticksToDisappear = lingerTicks;
            else
                Log.ErrorOnce(auraDef.defName + " needs HediffCompProperties_Disappears; aura buff would never expire.", auraDef.shortHash ^ 0x5A19);
        }

        // 找出目标身上「由指定来源提供」的那一份增益。
        private static Hediff FindAuraFrom(Pawn target, HediffDef auraDef, Pawn source)
        {
            List<Hediff> hediffs = target.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (hediff.def != auraDef)
                    continue;
                if (hediff.TryGetComp<HediffComp_LiuAuraLink>()?.source == source)
                    return hediff;
            }
            return null;
        }
    }
}
