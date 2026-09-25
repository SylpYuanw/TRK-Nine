# RimWorld 装备授予Hediff与范围光环链路

结论先行：原版已经提供「穿戴装备 → 授予 Hediff → 该 Hediff 定期给范围内角色授予增益」的完整链路，无需 Harmony、无需全局扫描器，也不需要自建 GameComponent。

## 1. 装备授予 Hediff（原版，零代码）

`CompProperties_CauseHediff_Apparel`（`compClass = CompCauseHediff_Apparel`）：

- 字段：`hediff`（HediffDef）、`part`（BodyPartDef）。
- 行为：`Notify_Equipped` 时若身上没有该 Hediff，则在 `GetNotMissingParts().FirstOrFallback(p => p.def == part)` 部位挂载。
- **必须**给被授予的 HediffDef 加 `HediffCompProperties_RemoveIfApparelDropped`，其 `CompShouldRemove => !parent.pawn.apparel.Wearing(wornApparel)`，否则脱下衣服 Hediff 不会消失。
- 该组件由 `CompCauseHediff_Apparel` 在挂载时通过 `TryGetComp` 把 `wornApparel` 回填，**不需要**手写赋值。

用途：把「穿着判定」的持续 tick 挂在 Hediff 上，而不是给 Apparel 写 ThingComp（Apparel 的 ThingComp 没有可靠的「仅穿着时 tick」入口）。

## 2. 范围内授予 Hediff（原版领导者光环写法）

`HediffComp_GiveHediffsInRange`（`Verse` 命名空间，Props 为 `HediffCompProperties_GiveHediffsInRange`）：

- 字段：`range`、`hediff`、`targetingParameters`、`mote`、`hideMoteWhenNotDrafted`、`initialSeverity`、`onlyPawnsInSameFaction`（默认 true）。
- 每 tick 执行，**不按间隔跳帧**；筛选条件是 `item.RaceProps.Humanlike && !item.Dead && item != parent.pawn && 距离 <= range && targetingParameters.CanTarget(item)`。
- 授予时把子 Hediff 的 `HediffComp_Disappears.ticksToDisappear` 直接设为 `5`。
- **子 Hediff 必须带 `HediffComp_Disappears`**，否则 `Log.Error`（"has a hediff in props which does not have a HediffComp_Disappears"）。
- 刷新语义：每个 tick 重置倒计时为 5，离开范围后 5 tick 内自动移除 —— 这就是「光环」不需要维护加入/离开事件的原因。

`HediffCompProperties_Disappears` 关键点：`disappearsAfterTicks` 是 `IntRange`；`CompPostMake` 用 `Props.disappearsAfterTicks.RandomInRange` 初始化 `ticksToDisappear` 与 `disappearsAfterTicks`；`CompShouldRemove => ticksToDisappear > 0 ? ... : true`，所以 `disappearsAfterTicks` 必须 > 0。

## 3. 动态半径（本项目六协会套装）

需求：光环半径随「范围内满足条件的同伴人数」变化（基础 6 格，每多 1 人 +2 格，且是**每人各自计算**）。

做法：光环组件不用原版 `GiveHediffsInRange`，改为自定义 `HediffComp`，按 `IsHashIntervalTick(interval)`（0.5 秒 = 30 tick）扫描：

1. 遍历同阵营角色（`map.mapPawns.SpawnedPawnsInFaction(faction)`；无阵营退回 `AllPawnsSpawned`）。
2. 从其中筛出**满足条件**的成员（只有他们才是加成对象）。
3. 对每个成员，以**成员自身**为中心统计 `baseRange` 内满足条件的人数（含自身），得 `range = base + (count-1) * perExtra`。
4. 若光环持有者与该成员距离 `<= range`，则授予/刷新增益 Hediff。

要点：`SpawnedPawnsInFaction` 返回 `List<Pawn>`，`AllPawnsSpawned` 返回 `IReadOnlyList<Pawn>` —— 类型不同，不要写「返回其中一个」的公共方法（会编译不过）。改用「把结果写入调用方传入的 `List<Pawn>` 缓冲」的形式，既兼容两种来源又可复用缓冲。

### 3.1 性能：先把「满足条件者」筛一遍再算半径

若半径依赖「附近满足条件的同伴数」，**不要**在每个目标上重新遍历全部候选者并逐个判定穿戴（会变成 (目标 × 邻员) 次 `WornApparel` 读取，且每名光环持有者都跑一遍）。正确做法是每个扫描 tick：

1. 收集同阵营候选者一次（复用调用方提供的 `List` 缓冲，不新建）。
2. 从候选者中筛出「满足条件」的成员一次，写入第二个复用缓冲。
3. **把第 2 步的成员集合直接当作光环目标集**（只有满足条件者才该吃到加成），并对其中每个成员以其自身为中心算半径、判定距离。

这样目标集与半径计算集是同一个集合，既满足「只有满足条件者获得加成」，又只遍历一次。

`HasFullSet` 这类判定只与穿戴状态有关，同一 tick 内对同一角色只需判定一次。

注意：若把目标集写成「全部同阵营角色」，就会把加成发给**未满足条件**的人（如只穿一件或什么都没穿的人），这与「满足 N/N 条件者才获得」的需求直接冲突 —— 这是实际踩过的坑。

### 3.2 意识等「能力」必须走 capMods，不是 statOffsets

`Consciousness`/`Sight`/`Moving` 等是 **`PawnCapacityDef`**（`PawnCapacityDefOf.Consciousness`），**不是 `StatDef`**。
`HediffStage.statOffsets` 只接受 `StatModifier`（目标是 StatDef），写 `<Consciousness>` 会解析不到。正确写法：

```xml
<stages>
  <li>
    <capMods>
      <li><capacity>Consciousness</capacity><offset>0.06</offset></li>
    </capMods>
  </li>
</stages>
```

生效链路：`Hediff.CapMods => CurStage?.capMods` → `PawnCapacityUtility`（原版实例：`Apparel_Blindfold` 的 HediffDef `Blindfold` 用 `capMods/Sight/setMax 0.2`）。

### 3.3 装备事件只能覆盖「装备瞬间」——读档不会重发

`CompCauseHediff_Apparel` 只在 `Pawn_ApparelTracker.Wear` → `Apparel.Notify_Equipped` 时授予 Hediff。
`Pawn_ApparelTracker.ExposeData` 的 `PostLoadInit` 分支**只处理 `CompApparelVerbOwner` 的 Verb**，不会重发 `Notify_Equipped`。

**后果**：往已有存档加入（或更新）这段逻辑时，存档里**已经穿在身上**的衣服永远拿不到该 Hediff，功能静默失效；玩家必须脱下再穿一次才会生效。原版 `Apparel_Blindfold` 也有同样性质，只是它的 Hediff 一旦授予就被存档保存，所以不易暴露。

**更稳的做法**：不要依赖装备事件，改为**按穿戴状态定期同步**。挂一个 Harmony 后置补丁在 `Pawn_ApparelTracker.ApparelTrackerTickInterval` 上（`Pawn.TickInterval` 对每个未暂停的 Pawn 都会调用它），用 `pawn.IsHashIntervalTick(30)` 门控后直接比较「是否满足条件」并增删目标 Hediff：

```csharp
[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.ApparelTrackerTickInterval))]
internal static class Patch_ApparelTracker_XxxSync
{
    static void Postfix(Pawn_ApparelTracker __instance)
    {
        Pawn pawn = __instance?.pawn;
        if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.health == null) return;
        if (!pawn.IsHashIntervalTick(30)) return;
        XxxUtility.SyncHediff(pawn);   // 满足则补挂，不满足则摘除
    }
}
```

一次同步即同时覆盖**装备、卸下、读档、换装**四种情形，也不需要「孤儿状态自检」这类补偿组件（少一个 Hediff、少两个组件）。
`IsHashIntervalTick` 按 Pawn 哈希偏移，会把不同 Pawn 的检测摊到不同 tick 上。

### 3.4 「加成只能有一个来源」——否则穿戴者自己吃两份

范围光环常见写法是「源头 Hediff 带光环组件 → 给范围内目标挂 buff Hediff」。若**同时**让源头 Hediff 也用 `capMods`/`statOffsets` 给自己加同样的属性，而光环目标集**包含授予者自身**（距离 0 必然在范围内），穿戴者就会同时吃到两份 → 数值翻倍。

结论：**要么**由源头给自身、光环只给他人（需显式排除自身）；**要么**源头不加属性、由光环 buff 统一发放（推荐，天然封顶）。
推荐后者的原因：同一 def 的 Hediff 在一个角色身上只会存在一个（`GetFirstHediffOfDef` 去重），所以「发放式」加成本质上不可叠加，需求里的「上限 X%、多人只加范围」由结构保证，不依赖额外判断。

## 4. 该 MOD 内的具体落地（六协会套装）

两个 Hediff + 一条同步补丁：

| 组成 | 职责 |
|---|---|
| `Patch_ApparelTracker_LiuSetSync` | **检测**。每 30 tick 按 `HasFullSet` 增删 `RK_LiuSet`，不依赖装备事件 |
| `RK_LiuSet` | **2/2 身份标记 + 光环源头**。自身**不加任何属性**；带 `HediffComp_LiuSetAura` |
| `RK_LiuAuraBuff` | **意识 +6% 的唯一来源**。带 `HediffComp_Disappears`，每次刷新 5 tick |

要点：

- 目标集 = **满足 2/2 的成员集合**，不是全部同阵营角色。1/2 或未穿戴者不获得任何效果。
- 授予者自身距离 0，必在自身光环范围内 → 单人 2/2 也能稳定拿到 +6%。
- 加成封顶 +6%：套装状态不加属性，光环 buff 同 def 去重，因此多人只放大范围（`6 + (N-1)*2`）不叠加数值。
- `HasFullSet` 按 `ThingDef.defName` 比较（`liuA`/`liuB`），不依赖 label。
- `Consciousness` 走 `capMods`（见 3.2）。
- 全部数值（半径/增量/扫描间隔/加成/残留 tick）经 XML 传导。
- 若仍需要「只穿一件」也有可见状态，可另加一个纯展示 Hediff；但**不要**让它承担检测职责，否则又回到装备事件的坑。

## 5. 证据来源

- `Verse/HediffComp_GiveHediffsInRange.cs`、`Verse/HediffCompProperties_GiveHediffsInRange.cs`
- `RimWorld/CompCauseHediff_Apparel.cs`、`RimWorld/CompProperties_CauseHediff_Apparel.cs`
- `Verse/HediffComp_RemoveIfApparelDropped.cs`、`Verse/HediffComp_Disappears.cs`、`Verse/HediffCompProperties_Disappears.cs`
- `Verse/MapPawns.cs:781`（`SpawnedPawnsInFaction` 返回 `List<Pawn>`）
- `Verse/Pawn_ApparelTracker.cs:781`（`Wear` → `Notify_Equipped`）、`:319/:340`（`ApparelTrackerTickRare/TickInterval` 都不 tick 穿着的衣服）、`:287`（`ExposeData` 的 `PostLoadInit` 只处理 Verb，不重发 `Notify_Equipped`）
- `Verse/Pawn.cs:2929`（`TickInterval` 对每个未暂停 Pawn 调 `apparel.ApparelTrackerTickInterval(delta)`）
- `Verse/Pawn_HealthTracker.cs:1020-1050`（`HealthTick` 调 `Hediff.PostTick` → `CompPostTick`，异常时移除 Hediff 并写日志）
- `Verse/HediffWithComps.cs:209-224`（`PostTick` → `comps[i].CompPostTick`）
- 项目文件：`Source/ClassLibrary1/Abilities/Hediff_LiuSet.cs`、`Defs/HediffDefs/Hediffs_LiuSet.xml`、`Defs/ThingDefs_Items/RK_liu Clothing.xml`
