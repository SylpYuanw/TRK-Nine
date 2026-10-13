# pawnslots：满天星当前配置与读取规则

更新：2026-10-13。依据：2026-10-12 重新提供的 pawnslots.txt，修改时间 23:21:58。当前记录取代此前提取结果，不以旧截图补填新文件。固定人物配置已实现并通过自动数据核对，实际游戏外观仍待验收。

新文件共 1 个非空人物槽、57 字段；拆分时保留空字段。异种为 RK_XenoType_Ratkin，内源基因 14 项，没有 OA 标识。已核对的 CE 1.5 公开参考定义 0—52 字段，53—56 扩展字段不猜解。

## 身份与外观

| 项目 | 当前值／处理 |
| --- | --- |
| 姓名 | NameTriple：名=满天星，昵称=满天星，姓为空 |
| 原 PawnKind | RatkinColonist，种族 Ratkin；实现时建立本 MOD 专用 PawnKind，避免通用角色的额外围裙、随机装备及商队加入扩展 |
| 年龄／性别／体型 | 生理年龄 90000000 Tick，生成时固定 25 岁；Female；Thin |
| 发型／头型 | RK_WavelongA／RK_HeadType_Head1，采用新文件，不采用旧截图 Head3 |
| 头部附加项 | 原值 0:0:0；已核对为鼠族 HAR addons 的尾巴、左耳、右耳三个 variant 均为 0 |
| 胡须／纹身 | NoBeard；面部字段原值 NoTattoo_Body、身体字段 NoTattoo_Face，语义均为无纹身，不强行挂入不适配部位的 Def |
| 原世界派系 | 新开端和 RK_PlayerFaction 不作为联络方配置；加入时使用玩家派系 |
| 出生时间 | 原值 -89985000 是 BirthAbsTicks，不照搬原世界时钟；实际年龄按新世界原版流程初始化 |

颜色统一按字节除以 255，四舍五入保留 3 位小数。

| 通道 | 原始 RGBA 字节 | 采用的 Unity RGBA |
| --- | --- | --- |
| 主发色 | 87,87,90,255 | (0.341, 0.341, 0.353, 1.000) |
| 副发色 | 255,255,255,255 | (1.000, 1.000, 1.000, 1.000) |
| 主肤色 | 255,239,201,255 | (1.000, 0.937, 0.788, 1.000) |
| 副肤色 | 0,0,0,0 | (0.000, 0.000, 0.000, 0.000) |
| 眼色字段 | 255,255,255,255 | (1.000, 1.000, 1.000, 1.000) |
| 三件衣着 | 174,219,228,255 | (0.682, 0.859, 0.894, 1.000) |
| 喜爱颜色 | 227,115,255,255 | (0.890, 0.451, 1.000, 1.000) |

鼠族颜色通过 HAR AlienComp 的 `OverwriteColorChannel` 写入 `hair`、`skin`、`skinBase`，story 发色与种族通道保持一致。CE 参考的眼色字段来自 Facial Animation 的眼部控制器；没有控制器时返回白色占位，不能据此创建或写入 HAR `eyes` 通道。本轮不导入该眼色字段。副通道数值不直接代表贴图最终像素颜色。melanin=-1 是参考编辑器保存的占位值，不作为黑色素配置。

## 经历、特质和技能

童年 Ratkin_ServentChild，成年 Ratkin_Idol。普通特质固定 Beauty degree=2、Industriousness degree=1、Kind degree=0。新基因中没有 PsychicAbility_Dull，不添加旧截图的心灵迟钝。

技能字段作为最终显示等级目标，经验字段均为 0。基因资质按当前已核对清单正常激活计算：

| 技能 | 最终目标 | 基因资质 | 基础 levelInt | 热情 |
| --- | --- | --- | --- | --- |
| Shooting 射击 | 0 | 0 | 0 | 无 |
| Melee 格斗 | 6 | 0 | 6 | 单火 |
| Construction 建造 | 1 | 0 | 1 | 无 |
| Mining 采矿 | 4 | +4 | 0 | 无 |
| Cooking 烹饪 | 2 | 0 | 2 | 无 |
| Plants 种植 | 8 | +4 | 4 | 单火 |
| Animals 驯兽 | 8 | 0 | 8 | 单火 |
| Crafting 手工 | 1 | 0 | 1 | 无 |
| Artistic 艺术 | 7 | 0 | 7 | 双火 |
| Medicine 医疗 | 0 | 0 | 0 | 无 |
| Social 社交 | 15 | 0 | 15 | 双火 |
| Intellectual 知识 | 0 | 0 | 0 | 无 |

应用顺序：先完整生成 Pawn，确定经历、基因、普通特质和正常健康状态；再刷新资质缓存，以实际 SkillRecord.Aptitude 反算基础等级，设置热情和经验，最后读取最终等级逐项校验。不能将最终目标赋给 Level 后再让基因重复叠加：原版 Level 的 setter 写基础值，getter 才计算资质。

经历加成在原版生成技能阶段已参与计算，不在导入最终目标后再次累加。童年提供射击-1、格斗+2、烹饪+1、种植+1、手工+1；成年提供社交+6。不能用简单减去经历加成来还原生成前随机值，因为原版还执行年龄曲线、随机倍率和最终调整。

AptitudeStrong_Mining、AptitudeStrong_Plants 各提供 +4。RK_Gene_GoodGardening 只提升收获产量，不提供技能等级；RK_Gene_Nimble 影响移速和闪避，不提供格斗等级。实现按实际活跃资质处理；额外 MOD 改变资质而使固定目标无法满足时明确报告，不静默改数值或基因。

## 基因

异种 RK_XenoType_Ratkin，14 个内源基因：

Skin_Melanin3、RK_Gene_SmallBody、RK_Gene_Nimble、RK_Gene_PoorEyesight、RK_Gene_FragilePain、RK_Gene_GoodGardening、Skin_SheerWhite、Beard_NoBeardOnly、RK_Gene_LargeEars、RK_Gene_ThinTail、RK_Gene_CuteFace、AptitudeStrong_Mining、AptitudeStrong_Plants、Hair_DarkReddish。

外源基因字段为空，不添加外源基因。采用新文件实际清单，不重新随机挑选发色基因。RK 项属于 Ratkin；没有 OA 不等于只包含原版游戏基因。最终外观采用固定颜色表。

## 装备和物品

| 所在栏 | 物品 | 材料 | 品质 | 数量 | 当前耐久 |
| --- | --- | --- | --- | --- | --- |
| 主装备 | RK_DeliveryBox 派送箱 | Steel | 极佳 | 1 | 100 |
| 衣着 | Apparel_ninec 羽绒服 | Synthread | 极佳 | 1 | 375 |
| 衣着 | nineja 外套 | Synthread | 极佳 | 1 | 375 |
| 衣着 | RK_nineh 护目镜 | Synthread | 极佳 | 1 | 104 |
| 库存 | RK_Food_Hardtack 硬饼干 | 无 | 无品质 | 9 | 50 |
| 库存 | Silver 白银 | 无 | 无品质 | 300 | 100 |

衣着采用固定颜色表，其他物品保存颜色为白色。耐久是当前 HitPoints，不是百分比；实现时核对当前 Def 上限。300 白银是携带物品，与购买人物的白银价格独立。主装备、穿戴和库存按当前文件固定，不附加通用 PawnKind 的围裙或随机物品。

派送箱使用正常装备通知链路，初始采用现有默认箱体形态，冷却按新装备初始化；槽位没有保存本 MOD 的自定义模式和冷却状态。

## 状态字段取舍

| 项目 | 特殊人物处理 |
| --- | --- |
| 心情、饱食、休息等需求 | 由原版创建实际适用的 Need，首次加入时填满；不恢复槽位小数、不永久锁满、不凭保存清单额外创建 Need |
| Hediff | 健康字段为空不代表应清空健康系统。保留游戏、基因、种族和装备正常添加的 Hediff，额外指定项以后按用户指令增加 |
| 意识形态 | 交给原版分配；不导入保存时的文化、名称、扩展数据，也不使用 ForceNoIdeo 代替自动分配 |
| 社交关系 | 无指定关系就不注入、不虚构关系；生成时关闭随机亲属，正常建立 tracker 和空集合，绝不赋 null；加入后正常社交 |
| 记忆 | 不导入槽位记忆，不制造空 Thought 或悬空 otherPawn；保留 ThoughtHandler、MemoryThoughtHandler 和集合，游戏正常产生的记忆照常运行 |
| 工作优先级 | 使用新殖民者的正常初始化，不搬入含其他 MOD 工作类型的保存安排；工作能力按经历、基因、健康等正常计算 |
| 记录、征召、任务与原世界计时 | 使用新角色初始化，不导入历史记录、任务引用和计时；当前全零记录无需逐项覆盖 |
| 皇室、灵能、第三方个性 | 当前没有指定内容，由加载的 DLC／MOD 正常初始化，不人为填充或清空系统 |

没有额外条目与基础对象为空是不同情况。生成器必须建立完整 Pawn 组件；空字段表示不导入定制内容，不能用 null 替换已初始化的 tracker、集合或记忆对象。

25 岁不是绝对无伤病保证：原版生成仍执行年龄伤病和生日 Hediff 检查。按用户要求保留自动健康机制，不在生成后统一清空 Hediff，也不为背景失忆自行添加脑损伤。

## 扩展字段与依据

字段 53=FF-FF-FF-FF，54、55为空，56=LightPurple。现有参考未定义其含义，保留为读取证据，不覆盖人物系统。

- 当前 pawnslots.txt 第1行：1个人物、57字段，OA标识0个；新文件不再使用旧截图校正。
- 用户本轮规则：需求满、Hediff自动、额外项以后指定、颜色三位小数、意识形态自动、无内容的社交／记忆不填入。
- [CE PresetPawn 公开参考](https://github.com/FerrisComplex/Rimworld-CharacterEditor-1.5/blob/4e0523fccc563ebb8ed5be41cd8df50a9ae75431/Source/Main/PresetPawn.cs#L92)，同提交的 AgeTool、SkillTool、ThingTool、ColorTool、SkinTool、HairTool、AlienRaceTool。参考版本为1.5，不推断新版扩展字段。
- RIMSAGE（2026-10-12）：SkillRecord.cs:57-184、SkillRecord.GetLevel、PawnGenerator.GenerateSkills/FinalLevelOfSkill、GeneTemplateDef:AptitudeStrong、GeneDefGenerator.GetFromTemplate，核对基础等级、经历与+4资质。
- RIMSAGE：PawnComponentsUtility.CreateInitialComponents、Need_Mood构造函数、MemoryThoughtHandler、Pawn_RelationsTracker，核对组件、记忆与空集合初始化。
- RIMSAGE：TattooDef:NoTattoo_Body/NoTattoo_Face，均为 noGraphic=true，分别适用身体和面部；无纹身语义不需要照搬槽位中互换部位的字段。
- RIMSAGE：PawnGenerator.GenerateInitialHediffs、AgeInjuryUtility.GenerateRandomOldAgeInjuries/RandomHediffsToGainOnBirthday，核对年龄与自动健康处理。
- 本地 Ratkin：Biotech/Defs/GeneDefs/XenotypeDefs.xml、CustomGeneDefs.xml；1.6/Defs/BackStoryDefs 两段经历；PawnKinds_Player.xml 的 RatkinColonist。
- 本地 Ratkin：1.6/Defs/ThingDefs_Races/Races_Rakinlike.xml，addons 顺序为尾巴、左耳、右耳，颜色通道均为 skin。
- [HAR AlienPartGenerator](https://github.com/erdelf/AlienRaces/blob/ae8412cdb4a047cb1a5d3945bebca44d061da569/Source/AlienRace/AlienRace/AlienPartGenerator.cs)，固定提交的 AlienComp 公共 `addonVariants` 和 `OverwriteColorChannel`；同提交 HarmonyPatches 核对外观初始化。
- [CE HeadTool](https://github.com/FerrisComplex/Rimworld-CharacterEditor-1.5/blob/4e0523fccc563ebb8ed5be41cd8df50a9ae75431/Source/Main/Tools/HeadTool.cs#L319)，眼色读取依赖 Facial Animation，缺控制器时返回白色。
