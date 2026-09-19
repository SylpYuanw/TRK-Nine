# TRK-Nine

鼠鼠收尾人（RimWorld MOD）。
GitHub仓库文件

## 仓库结构

- `鼠鼠收尾人们目前已到4579协会/`：MOD 本体，可直接放入 RimWorld 的 `Mods` 目录（About / Assemblies / Defs / Languages / Patches / Sounds / Textures）。
- `Source/`：C# 源码工程与设计文档。

## 更新记录

### 2026-09-19 死之弓：蓄力阶切换印迹与音效

为死之弓补上蓄力阶切换的表现与音效：

- 切换蓄力阶时，装备者头顶浮现一枚「死亡印记」：淡入、短暂停留、上浮回落并淡出，约 1.2 秒后自行消失；连续切换不叠加多枚，只把同一枚印迹的时间轴推回起点。
- 新增三条音效：瞄准（拉弓，瞄准开始时播放一次且不循环）、射击、蓄力阶切换（四阶共用，连点切换时新音覆盖旧音）。每条音效的素材、音量与音调都在 SoundDef 里独立可调。
- 装填死之箭时播放原先用于本武器射击的巨弓音效；射击音改为专用素材后，该音效转为装填用途。
- 上述表现与音效全部走原版现成钩子（Mote 生命周期、瞄准/射击音效字段、装备按钮音效），未新增每帧逻辑或补丁。
- 四阶切换音目前共用同一素材，后续要分阶细分只需替换对应阶的音效配置。

关联文件：`Source/ClassLibrary1/Weapon/BowOfDeath/CompEquippable_DeathBow.cs`、`Source/ClassLibrary1/Weapon/BowOfDeath/Properties_DeathBow.cs`、`Source/ClassLibrary1/Weapon/BowOfDeath/DeathBowDefOf.cs`、`Source/死之弓_实现与依据.md`、`鼠鼠收尾人们目前已到4579协会/Defs/SoundDefs/Sounds_BowOfDeath.xml`、`鼠鼠收尾人们目前已到4579协会/Defs/Misc/Mote_BowOfDeath.xml`、`鼠鼠收尾人们目前已到4579协会/Defs/ThingDefs_Items/Weapon_BowOfDeath.xml`、`鼠鼠收尾人们目前已到4579协会/Sounds/`、`鼠鼠收尾人们目前已到4579协会/Textures/VFX/Bow_of_Death_Stage_Vfx.png`、`鼠鼠收尾人们目前已到4579协会/Assemblies/ClassLibrary1.dll`



### 2026-09-19 Death bow: stage-switch sigil and sounds

Added presentation and sounds for the death bow's charge-stage switching:

- Switching a charge stage now spawns a "death sigil" above the wielder: it fades in, lingers briefly, floats and settles, then fades out and removes itself after about 1.2 seconds. Switching again does not stack copies; it restarts the timeline of the same sigil.
- Three new sounds: aiming (bow draw, played exactly once at the start of the aim and not looped), firing, and charge-stage switching (shared by all four stages, a new switch overrides the previous one so rapid switching does not stack voices). Each sound's clip, volume and pitch are configured independently in its SoundDef.
- Loading death arrows now plays the large-bow sound that this weapon used for firing before its own firing sound was introduced.
- Everything above uses vanilla hooks only (mote lifecycle, vanilla aim/fire sound fields, equipped gizmo sounds); no new per-frame logic or patches were added.
- All four stages currently share one switch sound; splitting them later only requires changing that stage's sound configuration.

Related files: `Source/ClassLibrary1/Weapon/BowOfDeath/CompEquippable_DeathBow.cs`, `Source/ClassLibrary1/Weapon/BowOfDeath/Properties_DeathBow.cs`, `Source/ClassLibrary1/Weapon/BowOfDeath/DeathBowDefOf.cs`, `Source/死之弓_实现与依据.md`, `鼠鼠收尾人们目前已到4579协会/Defs/SoundDefs/Sounds_BowOfDeath.xml`, `鼠鼠收尾人们目前已到4579协会/Defs/Misc/Mote_BowOfDeath.xml`, `鼠鼠收尾人们目前已到4579协会/Defs/ThingDefs_Items/Weapon_BowOfDeath.xml`, `鼠鼠收尾人们目前已到4579协会/Sounds/`, `鼠鼠收尾人们目前已到4579协会/Textures/VFX/Bow_of_Death_Stage_Vfx.png`, `鼠鼠收尾人们目前已到4579协会/Assemblies/ClassLibrary1.dll`
