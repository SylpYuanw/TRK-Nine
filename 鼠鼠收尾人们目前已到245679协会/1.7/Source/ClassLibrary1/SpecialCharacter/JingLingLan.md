# 精灵兰：导入核对

2026-10-27。资料来自用户提供的 `D:\QQFL\pawnslots.txt` 精灵兰记录；原始 57 字段见 [CE 导出快照](pawnslots_JingLingLan.txt)。本轮只新增四协会人物，不修改已完成的五位人物配置。

| 项目 | 固定初始配置 |
| --- | --- |
| 身份 | 精灵兰，女性，23 岁，东部し协会，2000 银 |
| 文件／图标 | `Defs/SpecialCharacter/JingLingLan.xml`／`SpecialCharacter/JingLingLan` |
| 体型／普通头型／发型 | Thin／RK_HeadType_Head4／RK_PonyA |
| 主发色／副发色 | (0.714,0.718,0.729,1.000)／白色 |
| 喜爱颜色 | Olive |
| 童年／成年 | Ratkin_Successor／Ratkin_Hunter |
| 普通特质 | ShootingAccuracy 1、Psychopath 0、Immunity 1 |
| FA 六项 | Ratkin_Head、Ratkin_EyeStars、Ratkin_LidPointy、Ratkin_Futomayu、Ratkin_MouthNormal、Ratkin_SkinNormal |
| 主副眼色 | 均为 (0.576,0.757,0.910,1.000) |
| 武器 | 钢死之弓，极佳，100 耐久 |
| 衣着 | 超织物东部し协会制服与箭袋，均极佳，耐久 310／130 |
| 库存 | 硬饼干×3、工业药品×4、死之箭×4 |
| FuFu | `FuFu_JingLingLan`，仅在 Mya's fufu 激活时加载 |

采用鼠族异种 `RK_XenoType_Ratkin` 及导出的 14 个内源基因，其中个体颜色基因为 Skin_Melanin2 和 Hair_DarkBlack；肤色仍使用纯白肤色基因的正常结果。12 项技能和热情按导出最终目标固定，采矿与种植目标均能由正常 +4 资质满足。CE 快照的死之箭耐久为 `-1`，按现有库存物品的直接创建逻辑改用 100 作为初始耐久值，不新增特殊处理。

人物生成、购买、派遣、空投和到达 Letter 复用现有通用流程，没有新增协会模板或人物专用 C#。FuFu 已有资源和 Def，只在精灵兰库存中补上引用。

验证范围：1.6 `Verify-NewSpecialCharacters.ps1` 已扩展为五条快照和五份人物 XML；1.7 与 1.6 共用人物 Def 和生成器逻辑。完整游戏内生成人物、GUI、存读档和空投仍需启动游戏后复看。
