# RimWorld 装备授予Hediff与范围光环链路

结论先行：原版为衣物提供「穿戴 → 授予 Hediff → Hediff 定期给范围内角色增益」的完整链路；普通武器没有同等的 Apparel 组件。武器若只需固定属性，优先用 `equippedStatOffsets`；若必须让健康页显示 Hediff，再用仅响应装备/卸下事件的轻量 `ThingComp`，无需 Harmony、周期扫描或全局 GameComponent。

## 1. 装备授予 Hediff（原版，零代码）

`CompProperties_CauseHediff_Apparel`（`compClass = CompCauseHediff_Apparel`）：

- 字段：`hediff`（HediffDef）、`part`（BodyPartDef）。
- 行为：`Notify_Equipped` 时若身上没有该 Hediff，则在 `GetNotMissingParts().FirstOrFallback(p => p.def == part)` 部位挂载。
- **必须**给被授予的 HediffDef 加 `HediffCompProperties_RemoveIfApparelDropped`，其 `CompShouldRemove => !parent.pawn.apparel.Wearing(wornApparel)`，否则脱下衣服 Hediff 不会消失。
- 该组件由 `CompCauseHediff_Apparel` 在挂载时通过 `TryGetComp` 把 `wornApparel` 回填，**不需要**手写赋值。

用途：把「穿着判定」的持续 tick 挂在 Hediff 上，而不是给 Apparel 写 ThingComp（Apparel 的 ThingComp 没有可靠的「仅穿着时 tick」入口）。

## 1.1 普通武器授予 Hediff 的边界

- 若目标只是“装备期间获得固定属性偏移”，原版最轻方案是 `ThingDef.equippedStatOffsets`。它不创建状态、不 tick、不存档额外引用，装备/卸下与读档由原版属性系统自动处理。
- `CompCauseHediff_Apparel` 不能直接用于武器：代码把 `parent` 强转为 `Apparel`，配套移除组件也按 `pawn.apparel.Wearing(...)` 判定。
- 原版皇权通过 `CompBladelinkWeapon.Notify_Equipped` 调用 `WeaponTraitWorker.Notify_Equipped`，再读取 `WeaponTraitDef.equippedHediffs` 挂载或移除 Hediff。
- `CompBladelinkWeapon` 在运行时随机生成武器特质，普通武器 Def 不能直接固定指定某个 `WeaponTraitDef`。
- 若不允许新增 C#，普通武器没有可直接固定配置的纯 XML“装备即 Hediff”路径。

## 1.2 本项目通用装备 Hediff 组件

组件统一放在 `Source/ClassLibrary1/Hediff/`，通过原版 `ThingComp.Notify_Equipped` / `Notify_Unequipped` 响应装备变化。

### 单 Hediff：`CompProperties_EquippedHediff`

XML：

```xml
<li Class="XIYUNTE.CompProperties_EquippedHediff">
  <hediffDef>zweiC_Defense</hediffDef>
</li>
```

行为：装备时添加指定 Hediff；卸下时检查武器和衣服，只有不存在其他同组件、同 HediffDef 的装备时才移除。适用于单件武器或衣服，不会因多来源重复添加或提前移除。

### 套装计数：`CompProperties_SetPiece`

XML：

```xml
<li Class="XIYUNTE.CompProperties_SetPiece">
  <setHediff>zweiSet</setHediff>
</li>
```

行为：同一套装部件引用同一个 `setHediff`，装备时 severity +1，卸下时 -1；severity 上限由对应 `HediffDef.maxSeverity` 控制，降到 0 时移除 Hediff。

套装 Hediff 阶段示例：

```xml
<maxSeverity>2</maxSeverity>
<stages>
  <li><minSeverity>1</minSeverity></li>
  <li>
    <minSeverity>2</minSeverity>
    <statOffsets>
      <ArmorRating_Blunt>0.20</ArmorRating_Blunt>
      <ArmorRating_Sharp>0.20</ArmorRating_Sharp>
    </statOffsets>
  </li>
</stages>
```

标准调试菜单的穿戴/装备操作调用 `Pawn_ApparelTracker.Wear` 或 `Pawn_EquipmentTracker.AddEquipment`，会触发这两个事件；直接绕过装备 API 修改内部列表不会触发。

### 1.3 武器装备偏移的边界：体感隔温只统计服装

- `StatWorker.GetValueUnfinalized` 会把 `equippedStatOffsets` 计入 Pawn 的同名 Stat：服装遍历全部穿着，武器只算 `equipment.Primary`。
- 但温度系统读的是 `ComfyTemperatureMin` / `ComfyTemperatureMax`，它们的 `StatPart_GearStatOffset` 默认只遍历 **apparel**：字段 `includeWeapon` 默认 false，原版 Def 未开启 —— 所以**武器的 `Insulation_Cold` / `Insulation_Heat` 偏移对体感温度无效**，只改 Pawn 的 `Insulation_*` Stat（该 Stat `showOnPawns=false`，实际不参与失温判定）。
- 需要"武器级寒冷/耐热"效果时，用 Hediff 直接偏移 Pawn 的 `ComfyTemperatureMin`(+N = 更怕冷) / `ComfyTemperatureMax`(-N = 更怕热)。本 MOD 六协会直剑即此写法：`liu_LongSword_WarmFlame` 带 `ComfyTemperatureMin +60`，等效"隔温-寒冷 -60°"。

证据：`RimWorld/StatPart_GearStatOffset.cs`（`includeWeapon` 分支）、`Defs/Core/Stats/Stats_Pawns_General.xml`（`ComfyTemperatureMin` 的 part 只写 `apparelStat`、未写 `includeWeapon`）、`RimWorld/StatWorker.cs:214-219`（装备偏移计入 Pawn Stat）、`RimWorld/StatWorker.cs:148`（Hediff stage 偏移计入 Pawn Stat）。

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

1. 沿用原版同阵营候选集，以领袖命令的 `TargetingParameters.CanTarget` 筛出殖民者。
2. 从候选集中另筛完整套装成员，只用于半径计算。
3. 以**光环来源自身**为中心统计基础方形范围内的套装成员数（含自身），得 `range = base + (count-1) * perExtra`。
4. 给实际方形范围内的全部候选者发放/刷新增益；完整套装成员每来源 +6% 意识，非套装成员 +2%。

要点：`SpawnedPawnsInFaction` 返回 `List<Pawn>`，`AllPawnsSpawned` 返回 `IReadOnlyList<Pawn>` —— 类型不同，不要写「返回其中一个」的公共方法（会编译不过）。改用「把结果写入调用方传入的 `List<Pawn>` 缓冲」的形式，既兼容两种来源又可复用缓冲。

### 3.1 性能：先把「满足条件者」筛一遍再算半径

若半径依赖「附近满足条件的同伴数」，**不要**在每个目标上重新遍历全部候选者并逐个判定穿戴（会变成 (目标 × 邻员) 次 `WornApparel` 读取，且每名光环持有者都跑一遍）。正确做法是每个扫描 tick：

1. 收集合格同阵营殖民者一次，复用 `List` 缓冲。
2. 另筛完整套装成员，写入第二个复用缓冲。
3. 仅用套装成员计算来源半径，再向第 1 步的候选集发放增益。

受益者与扩大半径的人数分开：非套装成员可接收光环，但不扩大半径。

`HasFullSet` 这类判定只与穿戴状态有关，同一 tick 内对同一角色只需判定一次。

六协会目标参数与原版领袖命令一致：`canTargetAnimals=false`、`canTargetMechs=false`、`canTargetBuildings=false`、`onlyTargetColonists=true`；不另外判断敌对、访客或囚犯。

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

结论：源头不加属性，全部由光环 buff 发放，避免自身重复受益。
单来源刷新必须按来源 Pawn 查找实例。`TryMergeWith=false` 允许同 def 的多个来源独立存在；`GetFirstHediffOfDef` 本身不保证实例唯一，也不决定叠加上限。

## 4. 该 MOD 内的具体落地（六协会套装）

两个 Hediff + 一个情境 ThoughtDef（2026-10-08）：

| 组成 | 职责 |
|---|---|
| `Comp_LiuPiece` | 装备/卸下事件维护 `RK_LiuSet` 的件数；套装 Hediff 随存档保存 |
| `RK_LiuSet` | **2/2 身份标记 + 光环源头**。自身**不加任何属性**；带 `HediffComp_LiuSetAura` |
| `RK_LiuAuraBuff` | 每来源一份独立实例；阶段 1/2/3 每份 +2% 意识，4/5/6 每份 +6%；第一份保存最多三来源的心情汇总阶段；每 30 tick 刷新到 35 tick |
| `RK_Liu_AuraMoodThought` | `ThoughtWorker_LiuAura` 继承原版 `ThoughtWorker_Hediff` 的阶段判定，只重写描述输出，显示单条“六协会之火” |

要点：

- 目标集采用原版同阵营与领袖命令目标参数；1/2 与未穿戴者接收非套装档位。
- 授予者自身距离 0，必在自身光环范围内 → 单人 2/2 也能稳定拿到 +6%。
- 意识按来源无限叠加；只有心情封顶：套装成员 6/12/18，非套装成员 2/4/6。
- 套装身份只读 `RK_LiuSet.Severity`，不扫描衣物；光环增删与装备/卸下事件同步阶段，同来源刷新只重置到期时间。
- `Consciousness` 走 `capMods`（见 3.2）。
- 原版心情 Worker 只读第一份指定 Hediff 的 `CurStageIndex`，不会自动相加同名实例。来源数在现有光环代码内汇总，不另建心情 Hediff、不增加 tick 扫描。描述子类不重写 `CurrentStateInternal`。
- 六个 Thought 阶段分别填写描述，前三阶段为非成员、后三阶段为完整套装成员。描述输出直接返回阶段文本，不追加原版 Hediff Worker 的“由于”来源段落；需求面板仍使用原版标题与正文间距。
- 阶段同步遍历一次光环列表；只给发生变化的实例设置 severity，沿原版 Setter 通知缓存。首份移除后，下一份自动接替汇总。
- 范围与增益继续使用原版到期组件；选中时连线覆盖所有合格受益者，非套装人员不扩大半径。
- 二协会继续使用无目标参数的候选收集调用，保留原有筛选与三来源上限。

证据：RIMSAGE `RimWorld/ThoughtWorker_Hediff.cs:8-18`、`Verse/HediffSet.cs` 的 `AddDirect`、`Pawn_HealthTracker.RemoveHediff`、`Hediff.Severity`；可复现模拟与限制见 [六协会与 Cinq 自测记录](Verification/六协会与Cinq自测记录.md)。

## 4.1 边界：固定加成不要建 Hediff（二协会内衬/外衣实例）

结论：装备期间只提供**固定属性**的加成，用原版 `ThingDef.equippedStatOffsets` 就够，不需要 Hediff、组件或补丁 —— 装备/卸下/读档全由原版属性链处理，无额外对象、无残留状态、无存档兼容问题。

- 二协会实现：`zweiA`(内衬) `SlaveSuppressionOffset -0.10`、`zweiB`(外衣) `SlaveSuppressionOffset -0.20` 走 `equippedStatOffsets`；`zweiB` 的承伤系数 ×0.8 走 Hediff `zweiB_Coat` 的 `statFactors`（`Defs/ThingDefs_Items/Clothes_zwei.xml`、`Defs/HediffDefs/Hediffs_ZweiClothes.xml`）。
- 原版 `ThingDef` **只有** `equippedStatOffsets` 一个装备字段，没有 `equippedStatFactors`：装备字段只能做**加算偏移**；要"×N"的**乘算倍率**必须走 Hediff 的 `statFactors`（或自定义 StatPart）。两者在同名 Stat 有多个来源时结果不同：偏移写法为 `1+Σ偏移`，乘算写法为 `(1+Σ其它偏移)×Π倍率`。
- 同一加成只保留一个来源：从原版风衣继承来的 `SlaveSuppressionOffset -0.05` 已删除，否则与 -0.20 叠加成 -0.25。
- 武器同理：Cinq 迅捷剑的 `+2.5 近战命中率` 是 `equippedStatOffsets/MeleeHitChance`，语义本就是加算偏移，不走 Hediff。
- 需要 Hediff 的情形：装备期间要**持续 tick**(光环/范围发放)、要在健康页显示状态、要按**件数**计数、需要**乘算倍率**的属性。

## 4.2 二协会光环技能（套装授予 → 定时发放 → 按来源叠加）

结论：把「常驻光环」改成「技能开启」时，三件事分开做最省：套装状态负责授予技能、来源 Hediff 负责定时发放、增益 Hediff 负责数值与连线。

- 授予：`XIYUNTE.CompProperties_SetPiece.abilityDef` 在套件集齐(severity 达到 `setHediff.maxSeverity`)时 `pawn.abilities.GainAbility`；集齐判定统一走 `SetPieceUtility.HasFullSet(pawn, setHediff)`，套装计数组件、技能隐藏、光环发放共用同一判据，不另写副本。
- **技能只授予不回收**：未集齐时隐藏按钮，因此换装不会重置冷却。原因：`Ability` 的冷却存在实例里，`RemoveAbility` 后再 `GainAbility` 会得到全新实例、冷却归零。
- 隐藏钩子在 **`CompAbilityEffect.ShouldHideGizmo`**，`Ability` 上**没有**该成员（写成 `Ability` 的 override 会编译报 CS0115）；做法是自定义 `CompProperties_AbilityGiveHediff` 子类 + `CompAbilityEffect_GiveHediff` 子类，只重写 `ShouldHideGizmo`，其余继承原版效果。
- 持续时间：`CompAbilityEffect_GiveHediff` 用 `CompAbilityEffect_WithDuration.GetDurationSeconds` 取 `Ability_Duration` StatDef，**单位是秒**，再 `SecondsToTicks()`(×60)。12 小时 = 500 秒 = 30000 tick；原版战斗命令写 1000 = 24 小时。
- 定时发放：来源 Hediff(`zwei_AuraActive`) 挂自定义 `HediffComp`，按 `IsHashIntervalTick(30)` 扫描同阵营人形单位(含自身，原版 `HediffComp_GiveHediffsInRange` 会排除自身)，逐个把增益的 `HediffComp_Disappears.ticksToDisappear` 刷成「扫描间隔 + 5」。
- 叠加与上限：增益 Hediff 的 `TryMergeWith` 返回 false，每个来源各占一份实例；「最多 N 层」按目标身上该 def 的实例数判断，达到上限不再新增来源。
- 连线：直接复用原版 `HediffCompProperties_Link`(`other` 记录来源、`drawConnection` 控制绘制、`maxDistance` 超距即移除)，缺 `customMote` 时自动回落 `ThingDefOf.Mote_PsychicLinkLine`。
- 范围绘制：`GenDraw.DrawRadiusRing` 挂在 `PawnRenderer.RenderPawnAt` 后置补丁里，按帧去重；地面光环贴图用 `MoteMaker.MakeAttachedOverlay` + `Maintain()` 跟随来源 Pawn。
- 缺 DLC 的写法：Def 引用字段元素本身可以写 `MayRequire`，跨引用加载器会跳过该字段并保持 null（`Verse/DirectXmlToObject.cs:204-213`），因此引用文化 DLC 的 `Mote_CombatCommand` 不会在缺 DLC 时报错。

## 4.3 Cinq自身施法与Seven攻击标记

- `AbilityDef.targetRequired=false` 的自身技能不需要 `verbProperties.targetParams`；`canTargetSelf` 只是目标参数中的自目标许可，不是范围字段。保留多余目标参数会进入目标选择链，Cinq“决斗宣告”因此只保留 `targetRequired=false` 与自身施法效果。
- Cinq 的攻击叠层与 Seven 的“弱点解析”都挂在自身套装 Hediff 的 `Notify_PawnUsedVerb`。组件先确认套装达到最大 severity，再从本次 `Verb_MeleeAttack` 的 `LocalTargetInfo` 取得目标；没有全局攻击 Harmony 补丁。
- Seven 的目标 Hediff 直接使用 `statFactors` 将 `ArmorRating_Blunt`、`ArmorRating_Sharp`、`ArmorRating_Heat` 乘以 0.8；重复命中查找已有实例并通过 `HediffComp_Disappears.SetDuration` 刷新 7500 tick，因此不叠加层数。

实现文件：`Defs/AbilityDefs/Ability_Cinq.xml`、`Defs/HediffDefs/Hediffs_Seven.xml`、`Defs/ThingDefs_Items/Clothes_Seven.xml`、`Source/ClassLibrary1/Abilities/Hediff_CinqDuelDeclaration.cs`、`Source/ClassLibrary1/Abilities/Hediff_SevenWeaknessAnalysis.cs`。

## 5. 证据来源

- `Verse/HediffComp_GiveHediffsInRange.cs`、`Verse/HediffCompProperties_GiveHediffsInRange.cs`
- `RimWorld/CompCauseHediff_Apparel.cs`、`RimWorld/CompProperties_CauseHediff_Apparel.cs`
- `Verse/HediffComp_RemoveIfApparelDropped.cs`、`Verse/HediffComp_Disappears.cs`、`Verse/HediffCompProperties_Disappears.cs`
- `Verse/MapPawns.cs:781`（`SpawnedPawnsInFaction` 返回 `List<Pawn>`）
- `Verse/Pawn_ApparelTracker.cs:781`（`Wear` → `Notify_Equipped`）、`:319/:340`（`ApparelTrackerTickRare/TickInterval` 都不 tick 穿着的衣服）、`:287`（`ExposeData` 的 `PostLoadInit` 只处理 Verb，不重发 `Notify_Equipped`）
- `Verse/Pawn.cs:2929`（`TickInterval` 对每个未暂停 Pawn 调 `apparel.ApparelTrackerTickInterval(delta)`）
- `Verse/Pawn_HealthTracker.cs:1020-1050`（`HealthTick` 调 `Hediff.PostTick` → `CompPostTick`，异常时移除 Hediff 并写日志）
- `Verse/HediffWithComps.cs:209-224`（`PostTick` → `comps[i].CompPostTick`）
- RIMSAGE（2026-10-08）：`CombatCommand` HediffDef 的 `targetingParameters`、`RimWorld/TargetingParameters.cs:89-313`、`RimWorld/ThoughtDef.cs`、`RimWorld/ThoughtWorker.cs`、`RimWorld/SituationalThoughtHandler.cs:195-221`、`Verse/Hediff.cs:236-265`。
- 项目文件：`Source/ClassLibrary1/Abilities/Hediff_LiuSet.cs`、`Defs/HediffDefs/Hediffs_LiuSet.xml`、`Defs/ThoughtDefs/Thoughts_Liu.xml`、`Defs/ThingDefs_Items/Clothes_Liu.xml`
