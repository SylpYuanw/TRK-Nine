# 六协会光环与 Cinq 特效自测记录

2026-10-08。Release 编译成功；三条独立进程模拟链路共 157 项检查通过，另核对两把武器的四向 XML 参数与南向方向计算。此记录供用户验收，不代表游戏内验收完成。

## 实现与修改边界

- 原 `RK_LiuSet`、`RK_LiuAuraBuff` 描述及套装阶段提示保留。Thought 名称为“六协会之火”，前三阶段描述为“虽非六协会成员，火焰仍鼓舞着我。”，后三阶段描述为“同为六协会成员，火焰鼓舞着我。”。
- 沿用现有光环 Hediff，每来源一份。首份以 severity 1/2/3 表示非套装 2/4/6 点心情，以 4/5/6 表示完整套装 6/12/18 点心情；其他实例维持单来源阶段。六个阶段的意识偏移为 2%/2%/2%/6%/6%/6%，因此意识仍按来源无限叠加。
- 单个 `ThoughtDef` 使用继承自 `ThoughtWorker_Hediff` 的 `ThoughtWorker_LiuAura`，只重写描述输出；阶段判定方法仍声明于原版基类，不创建额外心情 Hediff。原版不会自动统计同名 Hediff 数量，因此现有 C# 仍需在来源增删、穿脱事件中同步阶段。
- 提示框只输出阶段描述，去除原版来源段落附加的两个换行；标题与正文间距继续由原版需求面板处理。
- 同步只遍历一次列表，只对数值变化的实例设置 severity；同来源每 30 tick 刷新只重置到期时间，不同步阶段、不增加新的 tick 扫描。离开范围或来源失去套装后，沿用最多 35 tick 到期移除。
- Cinq Fleck 继承派送箱现有抽象父定义。现有近战 Postfix 增加一个 Def 扩展入口；派送箱继续读取当前形态，Cinq 读取扩展，随后共同调用原版 `EffecterDef.Spawn(A, B).Cleanup()`。未把派送箱组件或形态逻辑改写为框架。

## 三条模拟链路

| 链路 | 执行内容与结果 |
|---|---|
| 1：单来源、目标参数与刷新 | 生产 `GrantAuraFrom` → 原版 `HediffMaker` / `AddHediff` → 生产阶段同步 → 继承的原版阶段判定；完整/非完整套装心情 6/2、意识偏移 6%/2%。同一来源刷新 100 次，没有重复实例或缓存通知。实际调用领袖命令参数的 `CanTarget`，玩家人形 Pawn 通过，访客、友方动物、友方机械族不通过。方形范围 (6,6) 通过、(7,0) 不通过。六阶段均有描述且通过原版 `ThoughtStage.ConfigErrors`，两组成员描述不同；已持有可见光环时，提示输出不追加来源段落或换行。 |
| 2：多来源、换装与消失 | 原版增删 Hediff → 生产同步 → 原版 ThoughtWorker；1/2/3/4 来源，完整套装心情 6/12/18/18、意识偏移 6%/12%/18%/24%；非套装心情 2/4/6/6、意识偏移 2%/4%/6%/8%。四来源穿脱时 18↔6、24%↔8%；首来源移除后下一份接替。原版 `HediffComp_Disappears` 经过 35 tick 标记到期，随后调用原版移除入口，直至无光环时心情停用。新增来源至多触发两次缓存通知。 |
| 3：近战特效与共用类隔离 | 直接调用生产近战 Postfix → 原版 Effecter → 原版 Sprayer → 截获 Fleck 数据。Cinq 八向均生成一个特效，大小与沿 A→B 偏移距离读取当前 XML（1.2、2.0 格），角度含 -90° 贴图修正；派送箱本体无特效、四个战斗形态选择各自特效；普通武器无自定义特效。二协会复用的增益类增删不进入六协会阶段同步。 |

南向手持额外核对：Cinq/Zwei 的 SYS `southOffset.angle` 从 75° 改为 -15°，XYZ 仍为 (-0.3,0.3,0)，北/东/西向参数与修改前一致。按 SYS 镜像网格与绕 Y 轴旋转计算，斜剑尖方向从右上转为左上；未验证游戏窗口的最终渲染。

## 性能

本机独立 .NET Framework 进程，Release MOD DLL。每个 Pawn 另有 25 个无关 Hediff；1/3/20 来源分别测试。各测项预热后运行五组，心情/同步/刷新每组 300000 次，特效每组 30000 次，时间取中位数。分配由 `GC.GetAllocatedBytesForCurrentThread` 读取。

| 测项 | 1 来源 | 3 来源 | 20 来源 | 每次托管分配 |
|---|---:|---:|---:|---:|
| 原版心情阶段直接查询 | 29.80 ns | 30.91 ns | 31.87 ns | 40 B |
| 阶段同步，阶段未改变 | 32.09 ns | 35.53 ns | 79.69 ns | 0 B |
| 已有首来源刷新到期时间 | 42.85 ns | 48.45 ns | 43.49 ns | 0 B |

Cinq 生产 Postfix → 原版 Effecter → 截获 Fleck：994.76 ns/次，712 B/次。该数值包括模拟边界补丁开销，不含贴图加载、Fleck 渲染和 Unity 原生旋转。

原自定义 Worker 的已保存基准为 42.18/40.77/41.69 ns，150 万次查询未发生 Gen0 回收；切回原版 Worker 后分别发生 9/10/10 次 Gen0 回收。原版三参数 `Mathf.Min` 的数组分配属于原版查询链路；当前方案以原版映射和较少自定义代码为优先，不宣称比自定义 Worker 在所有性能维度都更优。原版心情调度和缓存未在本测试中执行，以上不能换算为游戏 TPS。

## 模拟边界与复现

- 使用 MOD 自带的原版 managed 程序集及实际生产 DLL；未使用本地游戏反编译源码。
- 手工构造 Pawn、Map、DefOf、Def 数据。实际执行光环新建、原版 Hediff 增删、Severity、原版到期组件、原版 ThoughtWorker，以及 Effecter/Sprayer 链路。
- 健康状态结算及渲染缓存执行被隔离；缓存通知仍计数。意识结果是各光环 `capMods.offset` 之和，不包含其他健康状态对最终意识的影响。
- 八向测试用 `atan2(x,z)` 替代依赖 Unity 原生 Quaternion 的平面角入口；Pawn DrawPos 使用格心。Fleck 在提交入口截获，没有真实绘图。未执行实际近战伤害或派送箱连击随机结算。
- 同阵营候选收集及完整心情面板调度未构造地图级集成测试；目标测试执行原版 `CanTarget`，另核对六协会 XML 与领袖命令参数一致。
- 模拟脚本的旧 Framework 编译器报告三条 CS1684 警告，涉及游戏程序集的 Span/ReadOnlySpan 元数据；测试不调用这些接口，编译及运行退出码均为 0。生产 Release 编译未报错或警告。

在此 MOD 根目录运行 `Source/Verification/Run-LiuAuraCinqSimulation.ps1`，结果写入 `simulation-results.txt`。脚本不加载游戏窗口；测试 Harmony 补丁只存在于独立测试进程。历史 `baseline-results.txt` 保留，脚本没有覆盖历史基准的入口。

证据来源：RIMSAGE `RimWorld/ThoughtWorker_Hediff.cs`、`RimWorld/ThoughtWorker.cs`、`RimWorld/ThoughtDef.cs:218-251`、`Verse/HediffSet.cs`（`AddDirect`、`GetFirstHediffOfDef`）、`Verse/Pawn_HealthTracker.cs`（`RemoveHediff`）、`Verse/Hediff.cs`（`Severity`）、`Verse/HediffComp_Disappears.cs`、`RimWorld/TargetingParameters.cs:89-313`、`Verse/EffecterDef.cs:112-118`、`Verse/SubEffecter_Sprayer.cs:27-177`、`Verse/IntVec3.cs`（`AngleFlat`）。入口：`https://mcp.rimsage.com/mcp`。
