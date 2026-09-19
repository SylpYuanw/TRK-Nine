# TRK-Nine

鼠鼠收尾人（RimWorld MOD）。
GitHub仓库文件

## 仓库结构

- `鼠鼠收尾人们目前已到4579协会/`：MOD 本体，可直接放入 RimWorld 的 `Mods` 目录（About / Assemblies / Defs / Languages / Patches / Sounds / Textures）。
- `Source/`：C# 源码工程与设计文档。

## 更新记录

### 2026-09-19 死之弓：四阶蓄力特效 Shader 化

- 新增基底层：在角色身下垫一个类高斯光团，亮度沿半径连续衰减、没有硬边；叠加贴图火苗的向上输送、噪声侵蚀与明暗闪烁，让基底真的在燃烧，而不是整张图一起淡入淡出。
- 侧面层主体是贴图烟雾整体缓慢向左卷动（颜色统一取暗红，不使用贴图自带的浅色像素）；粒子降为辅助拖尾，从右侧缓慢出生、向左上方飘散并淡出，一直产生到蓄力结束，头部只留轻微红色鼓包、不出现白点。
- 两层的呼吸与摇曳仍由 Mote 类按帧驱动（基底最低 55%、周期 2.6 秒；侧面最低 8%、周期 1.7 秒），Shader 只生成画面，不再做第二套呼吸。
- 实现方式：特效由随 MOD 分发的 Shader 包绘制，全部参数写在 Def 的 `shaderParameters` 中，调整观感不需要重新打包。
- 素材沿用原有两张贴图（分别作为火焰形体来源与烟雾软层），未新增图片。

关联文件：`鼠鼠收尾人们目前已到4579协会/AssetBundles/trk_bowofdeath_win`、`鼠鼠收尾人们目前已到4579协会/Defs/ShaderTypeDefs/BowOfDeath_Shaders.xml`、`鼠鼠收尾人们目前已到4579协会/Defs/Misc/Mote_BowOfDeath.xml`、`鼠鼠收尾人们目前已到4579协会/Defs/ThingDefs_Items/Weapon_BowOfDeath.xml`、`鼠鼠收尾人们目前已到4579协会/Assemblies/ClassLibrary1.dll`、`Source/ClassLibrary1/Weapon/BowOfDeath/Mote_DeathBowAimVfx.cs`、`Source/死之弓_实现与依据.md`



### 2026-09-19 Death bow: charge VFX rebuilt with custom shaders

- New base layer: a soft gaussian glow under the wielder whose brightness falls off smoothly along the radius with no hard edge, combined with upward-scrolling flame erosion and flicker taken from the existing texture, so the base really burns instead of fading in and out as a whole image.
- The side layer is now led by the smoke texture scrolling slowly to the left as a whole (colored uniformly in dark red, ignoring the texture's own light pixels); the particles are a secondary stream that spawns slowly on the right, drifts up and to the left, fades out and keeps emitting until the aim ends, with only a faint red bulge instead of a bright white head.
- Breathing and swaying of both layers are still driven per frame by the mote classes (base: minimum 55%, 2.6 s period; side: minimum 8%, 1.7 s period); the shaders only generate the imagery and do not duplicate the breathing.
- Implementation: the effects are drawn by a shader bundle shipped with the mod, and every parameter lives in the def's `shaderParameters`, so tuning the look needs no repackaging.
- Both existing textures are reused (as the flame source shape and the optional smoke layer); no new images were added.

Related files: `鼠鼠收尾人们目前已到4579协会/AssetBundles/trk_bowofdeath_win`, `鼠鼠收尾人们目前已到4579协会/Defs/ShaderTypeDefs/BowOfDeath_Shaders.xml`, `鼠鼠收尾人们目前已到4579协会/Defs/Misc/Mote_BowOfDeath.xml`, `鼠鼠收尾人们目前已到4579协会/Defs/ThingDefs_Items/Weapon_BowOfDeath.xml`, `鼠鼠收尾人们目前已到4579协会/Assemblies/ClassLibrary1.dll`, `Source/ClassLibrary1/Weapon/BowOfDeath/Mote_DeathBowAimVfx.cs`, `Source/死之弓_实现与依据.md`

### 2026-09-19 死之弓：蓄力瞄准烟雾特效

- 四个蓄力阶通用：瞄准开始时在装备者身上生成烟雾特效，1 秒淡入，之后持续以 5% 振幅缓慢呼吸、在水平面小幅摇曳并轻微旋转，透明度上限 70%（素材自身不透明度约 81%~88%）。
- 消散节点：射击完成或瞄准被中断后，1 秒内逐渐消散并自行销毁；读档时若正处于瞄准中会自动补挂特效。
- 实现沿用原版 Mote 生命周期（淡入 / 停止续命即按同样时长淡出并销毁），呼吸与摇曳由 Mote 类按帧驱动，未新增每帧补丁。
- 素材改名：`Textures/VFX/ChatGPT Image 2026年9月19日 14_39_40.png` → `Textures/VFX/Bow_of_Death_Aim_Vfx.png`。

关联文件：`Source/ClassLibrary1/Weapon/BowOfDeath/Mote_DeathBowAimVfx.cs`、`Source/ClassLibrary1/Weapon/BowOfDeath/Verb_DeathBowShot.cs`、`Source/ClassLibrary1/Weapon/BowOfDeath/CompEquippable_DeathBow.cs`、`Source/ClassLibrary1/Weapon/BowOfDeath/DeathBowDefOf.cs`、`Source/死之弓_实现与依据.md`、`鼠鼠收尾人们目前已到4579协会/Defs/Misc/Mote_BowOfDeath.xml`、`鼠鼠收尾人们目前已到4579协会/Textures/VFX/Bow_of_Death_Aim_Vfx.png`、`鼠鼠收尾人们目前已到4579协会/Assemblies/ClassLibrary1.dll`



### 2026-09-19 Death bow: aiming smoke VFX

- Shared by all four charge stages: a smoke effect is spawned on the wielder when aiming starts, fading in over 1 second and then breathing at 5% amplitude, swaying slightly in the horizontal plane and rotating gently, with opacity capped at 70% (the texture itself is about 81%~88% opaque).
- Dissipation timing: one second after the shot completes or the aim is interrupted the smoke fades out and destroys itself; loading a save mid-aim re-attaches the effect automatically.
- Built on the vanilla mote lifecycle (fade in / fade out and destroy once maintenance stops); the breathing and swaying are driven per frame by the mote class, with no per-frame patches added.
- Renamed asset: `Textures/VFX/ChatGPT Image 2026年9月19日 14_39_40.png` → `Textures/VFX/Bow_of_Death_Aim_Vfx.png`.

Related files: `Source/ClassLibrary1/Weapon/BowOfDeath/Mote_DeathBowAimVfx.cs`, `Source/ClassLibrary1/Weapon/BowOfDeath/Verb_DeathBowShot.cs`, `Source/ClassLibrary1/Weapon/BowOfDeath/CompEquippable_DeathBow.cs`, `Source/ClassLibrary1/Weapon/BowOfDeath/DeathBowDefOf.cs`, `Source/死之弓_实现与依据.md`, `鼠鼠收尾人们目前已到4579协会/Defs/Misc/Mote_BowOfDeath.xml`, `鼠鼠收尾人们目前已到4579协会/Textures/VFX/Bow_of_Death_Aim_Vfx.png`, `鼠鼠收尾人们目前已到4579协会/Assemblies/ClassLibrary1.dll`

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
