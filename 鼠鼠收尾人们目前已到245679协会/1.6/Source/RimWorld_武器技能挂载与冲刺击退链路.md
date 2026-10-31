# RimWorld 武器技能挂载与冲刺/击退链路

结论先行：一件武器要挂**多个**技能，不能靠 `CompProperties_EquippableAbility`（只支持单个 `abilityDef`）；多技能必须自建第二个 Ability 并把它注入 `CompGetEquippedGizmosExtra`。冲刺用原版 `PawnFlyer` 链路，击退用 `ThingDefOf.PawnFlyer_Stun`。

## 1. 武器技能的可见性链路（关键陷阱）

`Pawn_AbilityTracker.AllAbilitiesForReading`（用于技能栏与 `AllAbilitiesForReading` 遍历）：

```
CompEquippableAbility compEquippableAbility = pawn.equipment.Primary?.TryGetComp<CompEquippableAbility>();
if (compEquippableAbility != null && compEquippableAbility.AbilityForReading != null)
    allAbilitiesCached.Add(compEquippableAbility.AbilityForReading);
```

**只收一个**（`AbilityForReading`，即 `Props.abilityDef`）。所以：

- 想让第二个武器技能出现在技能栏 → 必须自己维护并在 `Notify_TemporaryAbilitiesChanged()` 后仍能显示，或者
- 更简单可靠：在 `CompEquippable.CompGetEquippedGizmosExtra()` 里直接 `foreach (Gizmo g in secondAbility.GetGizmos()) yield return g;`

装备栏 Gizmo 的实际收集点：`Pawn_EquipmentTracker.cs:235` → `PrimaryEq.CompGetEquippedGizmosExtra()`。

第二条陷阱：`AbilityUtility.MakeAbility(AbilityDef, Pawn)` **要求 Pawn 非空**（`Activator.CreateInstance(def.abilityClass, pawn, def)`）。未装备时不能构造，惰性 getter 必须先取 `EquippedPawn` 再创建，否则 `Command_Ability` 构造会失败。

## 2. 冲刺技能：别用 Verb_CastAbilityJump 的默认射程

`Verb_CastAbilityJump.EffectiveRange`：

```
if (base.EquipmentSource != null)
    cachedEffectiveRange = base.EquipmentSource.GetStatValue(StatDefOf.JumpRange);
else
    cachedEffectiveRange = base.EffectiveRange;
```

**陷阱**：`StatDefOf.JumpRange` 没有 `defaultBaseValue`（默认为 0）。武器技能走 `EquipmentSource != null` 分支，于是武器没定义 JumpRange 时 `EffectiveRange = 0` → `CanHitTarget` 对任意目标返回 false → 技能永远无法选中目标。

**解法**：自建 `Verb_CastAbilityJump` 子类覆盖 `EffectiveRange => verbProps.range`（`Verb.EffectiveRange` 是 `virtual`，`VerbProperties.range` 是 public float，范围 3 直接来自 XML）。

`JumpUtility.ValidJumpTarget` 只检查 `Impassable / Fogged / WalkableBy / 关着的门`，**不检查格子是否被 Pawn 占据** —— 因此可以把敌方 Pawn 所在格作为技能目标，再自行选择相邻落点。

## 3. 击退：用 PawnFlyer_Stun

`ThingDefOf.PawnFlyer_Stun`（`PawnFlyerBase` 子定义）就是原版用于「被击飞」的飞行器：

```
<pawnFlyer>
  <flightDurationMin>0.75</flightDurationMin>
  <flightSpeed>8</flightSpeed>
  <workerClass>PawnFlyerWorker</workerClass>
  <heightFactor>2</heightFactor>
  <stunDurationTicksRange>60~180</stunDurationTicksRange>
</pawnFlyer>
```

用法：`PawnFlyer.MakeFlyer(ThingDefOf.PawnFlyer_Stun, victim, destCell, null, null)` 然后 `GenSpawn.Spawn(flyer, destCell, map)`。落地时 `RespawnPawn` 会按 `stunDurationTicksRange` 眩晕，符合击退表现。

`PawnFlyer.CheckDestination` 每 15 tick 校验 `destCell` 是否仍是合法跳跃目标，不合法则在 `GenRadial.NumCellsInRadius(3.9f)` 内找替代格 —— 所以落点被占时会自动微调，不会卡死。

**自定义飞行器**：继承 `PawnFlyerWorker`（构造签名 `PawnFlyerWorker(PawnFlyerProperties properties)`），覆盖 `AdjustedProgress(t) => t` 与 `GetHeight(t) => 0f` 即得到贴地直线冲刺；XML 中 `<workerClass>` 填自定义类、`<heightFactor>0</heightFactor>`。父定义用 `PawnFlyerBase`（Core `Ethereal_Various.xml` 里的 abstract Name）。

## 4. 结算时序陷阱（控制器模式）

若用「独立控制器 Thing」在冲刺结束后结算伤害/击退，注意 `Apply` 里的顺序：控制器先 `GenSpawn.Spawn`，`PawnFlyer.MakeFlyer` 之后才把施放者 `DeSpawn`。因此控制器首个 tick 会看到「施放者仍在场」，若直接判定「已落地」就会提前结算。

解法：控制器内用三态机 `未起飞 → 已起飞(!caster.Spawned) → 已落地(caster.Spawned)`，并跳过创建当 tick；另设「起飞等待上限」兜底，避免飞行器创建失败时控制器常驻地图泄漏。

**不要**给 `PawnFlyer` 加自定义字段做关联（原版类不可改），用状态机或 `Find` 反查即可。

## 5. 躯干命中与「无躯干退化」

- 取躯干：`target.health.hediffSet.GetBodyPartRecord(BodyPartDefOf.Torso)`。
- `DamageInfo` 的构造函数签名是 `(DamageDef def, float amount, float armorPenetration, float angle, Thing instigator, BodyPartRecord hitPart, ThingDef weapon, SourceCategory category, ...)` —— 第 6 参才是 `hitPart`，第 7 参是 `weapon`。**别把 BodyPartRecord 传到 weapon 位**。
- 更稳的写法：先按默认构造，再 `dinfo.SetHitPart(torso)`；`torso == null` 时不设置，由原版按随机部位结算（即需求的「随机」退化）。

## 6. 该 MOD 内的具体落地（突破工具）

- `Ability_BreakthroughTool`（`canApplyOn`/`GizmoDisabled` 检查武器是否为派送箱且当前形态 `isBreakthroughTool`）。
- `CompAbilityEffect_BreakthroughTool`（`damage 47.5` / `knockbackDistance 5` / `wallExtraDamage 20` / `distortionFxDef`，全部 XML 传导）。
- `Verb_BreakthroughDash`：覆盖 `EffectiveRange = verbProps.range`（3 格）。
- `BreakthroughController` + `BreakthroughToolUtility.ComputeKnockback`：直线逐格推进，遇 `Traversability.Impassable` 建筑停止并记录撞墙（用户指定「仅 Impassable 的建筑」），击退方向恒为 施放者→目标 的八方向归一值，撞墙与自由飞行共用同一角度。
- `RK_BreakthroughFlyer`（`PawnFlyerWorker_Breakthrough`）、`RK_BreakthroughController`、`RK_BreakthroughFx`。
- 技能通过 `Box_shift.secondAbilityDef` 配置，并在 `CompEquippable_MultiForm.CompGetEquippedGizmosExtra` 注入。

## 7. XML 字段名写错是「静默无效」，不是报错

`Verse/DirectXmlToObject.cs:201-204`：

```csharp
FieldInfo fieldInfo = XmlToObjectUtils.DoFieldSearch(type2, xmlNode, xmlRoot);
if (fieldInfo == null)
{
    continue;   // 未知字段被直接跳过，没有任何 Log
}
```

**结论：Def XML 里写错的字段名会被静默忽略**，既不报错也不生效。这与「不允许静默错误」的项目原则直接冲突，因此新增 ThingDef/HediffDef 字段后必须逐个核对真实字段名。

本项目实际踩到的两个错名（都不存在）：
- `canBeSaved` → 正确的是 `isSaveable`（`Verse/ThingDef.cs:302`，默认 true）。
- `canBeDamaged` → ThingDef 上没有这个字段；建筑相关的是 `BuildingProperties.canBeDamagedByAttacks`。不可受伤只需不写（`useHitPoints=false` 已足够）。

生效链路：`Map.cs:652` `if (allThing.def.isSaveable && !allThing.IsSaveCompressible())` 决定是否写入存档。

推论：若某个 ThingDef 故意不存档，必须写 `isSaveable=false`；写了错名则**会被存档**，此时类里没写全 `ExposeData` 就会出现「读档后状态错乱」。

## 8. 不可见临时 Thing 用原版 EtherealThingBase

`Defs/Core/ThingDefs_Misc/Ethereal_Various.xml`：

```xml
<ThingDef Abstract="True" Name="EtherealThingBase">
  <category>Ethereal</category>
  <useHitPoints>false</useHitPoints>
  <drawerType>None</drawerType>
</ThingDef>
```

`RectTrigger`（原版隐形触发器）就是 `<ThingDef ParentName="EtherealThingBase">` + `tickerType Normal`，**完全没有 `graphicData`** —— 因为 `drawerType=None` 不绘制。

因此自建「无图形、只做逻辑结算」的 ThingDef 应继承 `EtherealThingBase`，不要写 `graphicData`（避免凭空依赖某张占位贴图），也不要写 `category/useHitPoints/drawerType`（已被父定义设定）。

若要直线冲刺的飞行器而非抛物线跳跃：继承 `PawnFlyerBase`，`<workerClass>` 指向 `PawnFlyerWorker` 子类（`AdjustedProgress(t)=>t`、`GetHeight(t)=>0f`），`<heightFactor>0</heightFactor>`。

## 9. 补丁类名不一致：一个错字引发满屏假报错

**症状**：启动日志出现
```
Exception loading def from file Stats_Pawns_General.xml:
  System.ArgumentException: Could not find type named XIYUNTE.StatPart_DeliveryBoxMoveSpeed ...
```
紧接着几十条 `Could not resolve cross-reference: No RimWorld.StatDef named MoveSpeed found to give to RimWorld.StatModifier (null stat)` 与
`Failed to find RimWorld.StatDef named MoveSpeed`。

**根因**：`Patches/*.xml` 里 `<li Class="XIYUNTE.XXX" />` 引用的 C# 类型在 DLL 中不存在（类改名后补丁没同步）。该补丁作用于 `StatDef[defName="MoveSpeed"]`，补丁抛异常导致这个 StatDef 整体加载失败 —— 于是所有引用 `MoveSpeed` 的 `StatModifier` 全部解析失败，产生几十条看似各异的报错。**它们是同一个根因的级联，不要逐条修。**

**排查手法**（比逐条读报错快得多）：
1. 直接在 DLL 里查类名是否存在，不需要反编译，元数据字符串即可：
   ```powershell
   $t=[Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($dll)); $t.Contains("StatPart_Xxx")
   ```
2. 若补丁引用的类名在 DLL 里查不到 → 是补丁过期，**不是 DLL 版本问题**。
3. 拿权威版本（仓库/已发布版）的同名补丁对比，通常它已改成正确类名。

**教训**：C# 类改名时必须全局搜索 `Patches/`、`Defs/` 中的 `Class="..."` 引用；RimWorld 编译期不会发现这种不一致。

## 10. 工作区分叉：Source 比 Defs 新

**症状**：`Failed to find Verse.ThingDef named XxxVfx` —— C# 引用的 def 在 `Defs/` 里根本不存在。

**判定**：若某 def 缺失，但 `Source/` 里有 `DefsOf` 字段或 `DefDatabase.GetNamedSilentFail("Xxx")` 在引用它，
说明 **Source 比 Defs 新** —— 分叉/复制工程时只带了代码没带资源。

**核对手法**：与权威版本做**文件级清单对比**，找出「权威有、工作区缺」的集合；再对共有文件做 MD5 对比，区分「真差异」与「只是缺文件」：

```powershell
Compare-Object (Get-ChildItem $old -Recurse -File | % FullName) (Get-ChildItem $new -Recurse -File | % FullName)
```

**资源类 def 的依赖闭包**（补 def 时必须一起补，否则只是换个地方继续报错）：
`Mote_*.xml` → `texPath` 对应的 `Textures/**/*.png` → `shaderType` 对应的 `Defs/ShaderTypeDefs/*.xml` → `shaderPath` 对应的 `AssetBundles/*`；
`SoundDef` → `clipPath` 对应的 `Sounds/*.wav`。

**易错**：`Weapon_*.xml` 这类既含结构又含音效/特效开关的文件，新旧版本可能一边用原版音效、一边用自定义 SoundDef。
覆盖前先 diff，并确认新版的 `aimVfx`/`soundCast` 等字段确实被 C# 读取，否则会出现「def 补齐了但不生效」。

## 11. 证据来源

- `RimWorld/Pawn_AbilityTracker.cs:19-56`（`AllAbilitiesForReading`）
- `Verse/Pawn_EquipmentTracker.cs:231-235`（`CompGetEquippedGizmosExtra`）
- `RimWorld/AbilityUtility.cs`（`MakeAbility(def, pawn)`）
- `RimWorld/Verb_CastAbilityJump.cs:13-35`、`RimWorld/JumpUtility.cs:56-83`
- `RimWorld/PawnFlyer.cs`（`MakeFlyer`/`CheckDestination`/`RespawnPawn`）、`Verse/PawnFlyerProperties.cs`、`Verse/PawnFlyerWorker.cs`
- `Defs/Core/ThingDefs_Misc/Ethereal_Various.xml`（`PawnFlyerBase`）、`PawnFlyer_Stun` 合并后 XML
- `Verse/DamageInfo.cs`、`Verse/HediffSet.cs:736`、`RimWorld/BodyPartDefOf.cs:22`
- `Verse/DirectXmlToObject.cs:201-204`（未知 XML 字段静默 `continue`）
- `Verse/ThingDef.cs:302`（`isSaveable`）、`Verse/Map.cs:652`（存档判定）
- `Defs/Core/ThingDefs_Misc/Ethereal_Various.xml`（`EtherealThingBase` / `RectTrigger`）
- 案例证据：`Patches/MoveSpeed_DeliveryBox.xml`（旧，引用不存在的 `StatPart_DeliveryBoxMoveSpeed`）vs 仓库 `Patches/StatParts_DeliveryBox.xml`（正确引用 `StatPart_DeliveryBoxModeOffset`/`ModeFactor`）；DLL 元数据字符串查询确认 `StatPart_DeliveryBoxMoveSpeed` 在工作区与旧版两个 DLL 中均不存在
- 项目文件：`Source/ClassLibrary1/Abilities/Ability_BreakthroughTool.cs`、`Defs/AbilityDefs/Ability_BreakthroughTool.xml`
