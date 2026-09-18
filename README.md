# TRK-Nine

鼠鼠收尾人（RimWorld MOD）。
GitHub仓库文件

## 仓库结构

- `鼠鼠收尾人们目前已到4579协会/`：MOD 本体，可直接放入 RimWorld 的 `Mods` 目录（About / Assemblies / Defs / Languages / Patches / Textures）。
- `Source/`：C# 源码工程（`Source/ClassLibrary1`、`Source/WeaponAbility`）与设计文档。

## 更新记录

### 2026-09-17 派送箱形态显示修复

修复派送箱切换形态后图标与描述不随形态刷新的问题：

- 装备栏槽位图标、近战攻击按钮（H）图标改为显示当前形态贴图，不再固定为默认派送箱贴图。
- 装备栏武器提示框与近战攻击按钮提示框的描述改为显示当前形态文本。
- 提示框描述改为在悬停显示时求值，形态描述与贴图按当前形态索引缓存，形态未变化时不产生额外开销。

关联文件：`Source/ClassLibrary1/Weapon/Weapon_DeliveryBox.cs`、`Source/ClassLibrary1/Weapon/Weapon_DeliveryBox_DisplayPatches.cs`、`鼠鼠收尾人们目前已到4579协会/Assemblies/ClassLibrary1.dll`



### 2026-09-17 Delivery box form display fix

Fixed stale icon and description after switching the delivery box form:

- The gear slot icon and the melee attack (H) button icon now show the texture of the active form instead of the default delivery box texture.
- The gear tooltip and the attack button tooltip now show the description of the active form.
- Tooltip descriptions are evaluated only while shown, and form descriptions and textures are cached per form index, so an unchanged form adds no extra cost.

Related files: `Source/ClassLibrary1/Weapon/Weapon_DeliveryBox.cs`, `Source/ClassLibrary1/Weapon/Weapon_DeliveryBox_DisplayPatches.cs`, `鼠鼠收尾人们目前已到4579协会/Assemblies/ClassLibrary1.dll`
...
