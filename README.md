# TRK-Nine
鼠鼠收尾人
GitHub仓库文件

## 仓库结构

- `鼠鼠收尾人们目前已到4579协会/`：可直接放入 RimWorld `Mods` 目录的 MOD 本体（About / Assemblies / Defs / Languages / Patches / Textures）。
- `Source/`：C# 源码工程与设计文档（`Source/ClassLibrary1`、`Source/WeaponAbility`、项目规划与机制记录）。

## 本次更新

修复派送箱切换形态后显示不同步的问题，并移除为此引入的每帧开销：

- 装备栏槽位图标与近战攻击按钮（H）图标随形态显示对应贴图，不再固定为默认 `Box_Core`。
- 装备栏武器 tooltip 的描述改用当前形态文本（原版该处读取静态 `ThingDef.DescriptionDetailed`，不随武器实例变化）。
- 近战攻击按钮 tooltip 的图标与描述随形态刷新；描述改为在 tooltip 实际显示时求值，不再落在每帧 Gizmo 重建路径上。
- 形态描述与贴图按"当前形态索引"缓存，形态未变化时各显示入口只做一次索引比较，不构造字符串、不查贴图。
- 重新编译 `Assemblies/ClassLibrary1.dll`；相关机制结论同步写入 `Source/已知参考机制.md`。

## 发布流程（维护记录，后续无需重复确认）

- 源工作区：`C:\Users\Administrator\Desktop\天翼种\鼠鼠收尾人们目前已到4579协会`
- 编译：`MSBuild Source\ClassLibrary1\ClassLibrary1.csproj -p:Configuration=Release`，输出即 MOD 的 `Assemblies/ClassLibrary1.dll`。
- 同步规则：工作区 MOD 本体文件 → 仓库 `鼠鼠收尾人们目前已到4579协会/`；工作区 `Source/` → 仓库 `Source/`。
- 推送：`git add -A` → `git commit -m "<说明>"` → `git push origin main`。
- 远端：`https://github.com/SylpYuanw/TRK-Nine.git`（分支 `main`）。
