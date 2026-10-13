using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace XIYUNTE.SpecialCharacter
{
    public sealed class SpecialCharacterOrder : IExposable, IThingHolder
    {
        public SpecialCharacterDef character;
        public Pawn pawn;
        public SpecialCharacterPod pod;
        public Map destination;
        public IntVec3 refundCell;
        public DispatchLifecycle lifecycle = new DispatchLifecycle();
        public ThingOwner<Thing> payment;
        public string failure;
        public IThingHolder ParentHolder { get; set; }

        public SpecialCharacterOrder() { payment = new ThingOwner<Thing>(this); }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref character, "character");
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref pod, "pod");
            Scribe_References.Look(ref destination, "destination");
            Scribe_Values.Look(ref refundCell, "refundCell");
            Scribe_Values.Look(ref lifecycle.stage, "stage");
            Scribe_Values.Look(ref lifecycle.arrivalTick, "arrivalTick");
            Scribe_Values.Look(ref lifecycle.arrivalLetterSent, "arrivalLetterSent");
            Scribe_Values.Look(ref failure, "failure");
            Scribe_Deep.Look(ref payment, "payment", this);
        }

        public ThingOwner GetDirectlyHeldThings() => payment;
        public void GetChildHolders(List<IThingHolder> outChildren) =>
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, payment);
    }

    // 世界级订单保存唯一购买状态；只有待派遣订单参与到期处理。
    public sealed class WorldComponent_SpecialCharacters : WorldComponent, IThingHolder
    {
        private List<SpecialCharacterOrder> orders = new List<SpecialCharacterOrder>();
        private ThingOwner<Thing> waitingPods;
        private int nextArrivalTick = int.MaxValue;
        public IThingHolder ParentHolder => null;

        public WorldComponent_SpecialCharacters(World world) : base(world)
        {
            waitingPods = new ThingOwner<Thing>(this);
        }

        public SpecialCharacterOrder GetOrder(SpecialCharacterDef def) => orders.FirstOrDefault(o => o.character == def);

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
                foreach (SpecialCharacterOrder order in orders)
                    if ((order.lifecycle.stage == DispatchStage.Waiting || order.lifecycle.stage == DispatchStage.InFlight) &&
                        (order.destination == null || !Find.Maps.Contains(order.destination)))
                        Fail(order, "The destination map was removed before the dispatch completed.");
            Scribe_Deep.Look(ref waitingPods, "waitingPods", this);
            Scribe_Collections.Look(ref orders, "orders", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                LongEventHandler.ExecuteWhenFinished(ValidateAndSchedule);
        }

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            if (!fromLoad) ValidateAndSchedule();
        }

        // 世界初始化早于地图加载；读档校验在引用恢复及地图生成完成后执行。
        private void ValidateAndSchedule()
        {
            if (orders == null || waitingPods == null)
                throw new InvalidOperationException("Special character storage was not restored.");
            if (orders.Any(o => o == null || o.character == null) ||
                orders.Select(o => o.character).Distinct().Count() != orders.Count)
                throw new InvalidOperationException("Special character orders contain missing or duplicate definitions.");
            foreach (SpecialCharacterOrder order in orders)
            {
                order.ParentHolder = this;
                if (order.payment == null) throw new InvalidOperationException("Special character payment storage is missing.");
                if (order.lifecycle.stage == DispatchStage.Arrived && !order.lifecycle.arrivalLetterSent)
                    throw new InvalidOperationException("Completed special character order has no notification flag.");
                if (order.lifecycle.stage == DispatchStage.Waiting || order.lifecycle.stage == DispatchStage.InFlight)
                {
                    if (order.pawn == null || order.pawn.Destroyed || order.pod == null || order.pod.Destroyed ||
                        order.pod.Contents == null || !order.pod.Contents.innerContainer.Contains(order.pawn) ||
                        order.destination == null || !Find.Maps.Contains(order.destination) ||
                        (order.lifecycle.stage == DispatchStage.Waiting && !waitingPods.Contains(order.pod)))
                        Fail(order, "Pending dispatch references could not be restored.");
                }
            }
            RefreshSchedule();
        }

        public static List<Thing> AvailableSilver(Map map)
        {
            return TradeUtility.AllLaunchableThingsForTrade(map)
                .Where(t => t.Spawned && t.def == ThingDefOf.Silver).ToList();
        }

        public static int SilverBalance(Map map) => AvailableSilver(map).Sum(t => t.stackCount);

        public string UnavailableReason(SpecialCharacterDef def, Map map)
        {
            if (GetOrder(def) != null) return "TRK_SC_AlreadyPurchased".Translate();
            if (map == null || !Find.Maps.Contains(map) || !map.IsPlayerHome)
                return "TRK_SC_HomeMapRequired".Translate();
            if (!ModsConfig.BiotechActive) return "TRK_SC_BiotechRequired".Translate();
            return null;
        }

        public bool Purchase(SpecialCharacterDef def, Pawn negotiator, out string response)
        {
            Map map = negotiator.Map;
            response = UnavailableReason(def, map);
            if (response != null) return false;
            List<Thing> silver = AvailableSilver(map);
            if (silver.Sum(t => t.stackCount) < def.price)
            {
                response = "TRK_SC_InsufficientSilver".Translate();
                return false;
            }
            IntVec3 dropCell;
            if (!TryDropCell(map, negotiator.Position, out dropCell))
            {
                response = "TRK_SC_NoDropSpot".Translate();
                return false;
            }

            SpecialCharacterOrder order = null;
            Pawn pawn = null;
            SpecialCharacterPod pod = null;
            Thing preparedItem = null;
            try
            {
                SpecialCharacterGenerator.Generate(def, map, out pawn, out preparedItem);
                pod = (SpecialCharacterPod)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TRK_SpecialCharacterPod"));
                pod.character = def;
                pod.Contents = new ActiveTransporterInfo(pod) { openDelay = 0, leaveSlag = false };
                if (!pod.Contents.innerContainer.TryAdd(pawn, canMergeWithExistingStacks: false))
                    throw new InvalidOperationException("Could not place the generated character into the delivery pod.");
                order = new SpecialCharacterOrder
                {
                    character = def, pawn = pawn, pod = pod, destination = map,
                    refundCell = negotiator.Position, ParentHolder = this,
                    lifecycle = new DispatchLifecycle { arrivalTick = checked(Find.TickManager.TicksGame + GenDate.TicksPerHour) }
                };
                if (!waitingPods.TryAdd(pod, canMergeWithExistingStacks: false))
                    throw new InvalidOperationException("Could not store the prepared delivery pod.");
                // 生成依赖可能改变地图物品，扣款前再次读取当前余额。
                silver = AvailableSilver(map);
                if (GetOrder(def) != null || silver.Sum(t => t.stackCount) < def.price)
                    throw new InvalidOperationException("Purchase conditions changed before payment.");
                int remaining = def.price;
                foreach (Thing stack in silver)
                {
                    if (remaining == 0) break;
                    int amount = Math.Min(remaining, stack.stackCount);
                    // SplitOff 对整堆和部分堆分别脱离地图或创建无容器的新堆。
                    if (!order.payment.TryAdd(stack.SplitOff(amount), canMergeWithExistingStacks: false))
                        throw new InvalidOperationException("Could not store the deducted silver.");
                    remaining -= amount;
                }
                if (remaining != 0) throw new InvalidOperationException("Payment did not reach the configured price.");
                orders.Add(order);
                RefreshSchedule();
                response = "TRK_SC_PurchaseAccepted".Translate(def.LabelCap);
                return true;
            }
            catch (Exception ex)
            {
                if (order != null && order.payment.Count > 0)
                {
                    if (!orders.Contains(order)) orders.Add(order);
                    Fail(order, ex.ToString());
                }
                else Log.Error("[TRK SpecialCharacter] Purchase failed: " + ex);
                if (preparedItem != null && !preparedItem.Destroyed) preparedItem.Destroy();
                if (pod != null && !pod.Destroyed) pod.Destroy();
                if (pawn != null && !pawn.Spawned) SpecialCharacterGenerator.DiscardPreparedPawn(pawn);
                RefreshSchedule();
                response = "TRK_SC_PurchaseFailed".Translate();
                return false;
            }
        }

        public override void WorldComponentTick()
        {
            int tick = Find.TickManager.TicksGame;
            if (tick < nextArrivalTick) return;
            foreach (SpecialCharacterOrder order in orders)
            {
                if (!order.lifecycle.IsDue(tick)) continue;
                Skyfaller incoming = null;
                try
                {
                    IntVec3 cell;
                    if (order.destination == null || !Find.Maps.Contains(order.destination) ||
                        !order.destination.IsPlayerHome || !TryDropCell(order.destination, order.refundCell, out cell))
                        throw new InvalidOperationException("The destination colony or a safe drop location is unavailable.");
                    if (!waitingPods.Contains(order.pod))
                        throw new InvalidOperationException("The prepared delivery pod is missing.");
                    incoming = SkyfallerMaker.MakeSkyfaller(ThingDefOf.DropPodIncoming);
                    if (!incoming.innerContainer.TryAddOrTransfer(order.pod, canMergeWithExistingStacks: false))
                    {
                        incoming.Destroy();
                        throw new InvalidOperationException("Could not transfer the delivery pod into its skyfaller.");
                    }
                    order.lifecycle.Launch();
                    GenSpawn.Spawn(incoming, cell, order.destination);
                    if (!incoming.Spawned) throw new InvalidOperationException("The incoming delivery carrier did not spawn.");
                }
                catch (Exception ex)
                {
                    Fail(order, ex.ToString());
                    if (incoming != null && !incoming.Destroyed) incoming.Destroy();
                }
            }
            RefreshSchedule();
        }

        public void PodDestroyed(SpecialCharacterPod pod)
        {
            SpecialCharacterOrder order = GetOrder(pod.character);
            if (order == null || order.pod != pod ||
                (order.lifecycle.stage != DispatchStage.Waiting && order.lifecycle.stage != DispatchStage.InFlight)) return;
            if (order.lifecycle.stage != DispatchStage.InFlight || order.pawn == null || order.pawn.Dead ||
                order.pawn.Destroyed || !order.pawn.Spawned || order.pawn.Map != order.destination)
            {
                Fail(order, "Delivery pod was destroyed without its character arriving at the destination.");
                return;
            }
            Pawn pawn = order.pawn;
            foreach (Need need in pawn.needs.AllNeeds) need.CurLevelPercentage = 1f;
            // 完成标记先于通知写入；终态不保存已销毁载体及不再使用的地图引用。
            if (order.lifecycle.Complete())
            {
                order.pod = null;
                order.destination = null;
                order.payment.ClearAndDestroyContents();
                Find.LetterStack.ReceiveLetter("TRK_SC_ArrivalLabel".Translate(pawn.LabelShort),
                    "TRK_SC_ArrivalText".Translate(pawn.LabelShort, order.character.association),
                    LetterDefOf.PositiveEvent, pawn);
            }
        }

        public void PodFailed(SpecialCharacterPod pod, string reason)
        {
            SpecialCharacterOrder order = GetOrder(pod.character);
            if (order == null || order.lifecycle.stage == DispatchStage.Arrived || order.lifecycle.stage == DispatchStage.Failed)
            {
                Log.Error("[TRK SpecialCharacter] " + reason);
                return;
            }
            Fail(order, reason);
        }

        private void Fail(SpecialCharacterOrder order, string reason)
        {
            if (order.lifecycle.stage == DispatchStage.Failed) return;
            order.lifecycle.Fail();
            order.failure = reason;
            SpecialCharacterPod pod = order.pod;
            Pawn pawn = order.pawn;
            Map destination = order.destination;
            order.pod = null;
            order.destination = null;
            order.pawn = null;
            Log.Error("[TRK SpecialCharacter] " + order.character.defName + ": " + reason);
            if (pawn != null && !pawn.Spawned) SpecialCharacterGenerator.DiscardPreparedPawn(pawn);
            if (pod != null && !pod.Destroyed) pod.Destroy();
            if (destination != null && Find.Maps.Contains(destination))
                Refund(order, destination, order.refundCell);
            Messages.Message("TRK_SC_DispatchFailed".Translate(order.character.LabelCap), MessageTypeDefOf.RejectInput, false);
            RefreshSchedule();
        }

        public bool Refund(SpecialCharacterOrder order, Map map, IntVec3 cell)
        {
            if (order.lifecycle.stage != DispatchStage.Failed) throw new InvalidOperationException("Only failed orders can refund.");
            foreach (Thing part in order.payment.ToList())
                if (!GenPlace.TryPlaceThing(part, cell, map, ThingPlaceMode.Near))
                {
                    Log.Error("[TRK SpecialCharacter] Could not return escrowed silver; the order retains the refund.");
                    return false;
                }
            return order.payment.Count == 0;
        }

        private static bool TryDropCell(Map map, IntVec3 center, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomCellNear(center, map, Math.Max(map.Size.x, map.Size.z),
                c => DropCellFinder.IsGoodDropSpot(c, map, allowFogged: false, canRoofPunch: false), out cell);
        }

        private void RefreshSchedule()
        {
            nextArrivalTick = int.MaxValue;
            foreach (SpecialCharacterOrder order in orders)
                if (order.lifecycle.stage == DispatchStage.Waiting)
                    nextArrivalTick = Math.Min(nextArrivalTick, order.lifecycle.arrivalTick);
        }

        public ThingOwner GetDirectlyHeldThings() => waitingPods;
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, waitingPods);
            outChildren.AddRange(orders);
        }
    }
}
