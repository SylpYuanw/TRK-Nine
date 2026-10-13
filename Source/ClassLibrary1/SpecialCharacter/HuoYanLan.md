# 火焰兰：导入核对

2026-10-14。资料来自用户授权的 `D:\QQFL\pawnslots (2).txt` 第 5 行；原始 57 字段记录见 [CE 导出快照](pawnslots_HuoYanLan.txt)。本次火焰兰导入不更新文件中已有三人的配置。

| 项目 | 固定初始配置 |
| --- | --- |
| 身份 | 火焰兰，女性，29 岁，南部 Liu 协会，2000 银 |
| 文件／图标 | `Defs/SpecialCharacter/HuoYanLan.xml`／`SpecialCharacter/HuoYanLan` |
| 体型／普通头型／发型 | Thin／RK_HeadType_Head5／RK_LongB（原鼠族发型） |
| 主发色／副发色 | (0.576,0.353,0.192,1.000)／白色 |
| 喜爱颜色 | LightPink |
| 童年／成年 | RK_Backstory_WandererChild／Ratkin_Soldier |
| 普通特质 | Nerves 2（坚韧不拔）、SpeedOffset 2（健步如飞） |
| FA 六项 | Ratkin_Head、Ratkin_EyeStare、Ratkin_LidPointy、Ratkin_Futomayu、Ratkin_MouthNormal、Ratkin_SkinNormal |
| 主副眼色 | 均为 (0.580,0.373,0.427,1.000) |
| 武器 | 钢 Liu 长剑，极佳，100 耐久 |
| 衣着 | 超织物 Liu 西装与外套，均极佳、390 耐久；颜色 (0.678,0.859,0.890,1.000) |
| 库存 | 硬饼干×6、草莓酒×6 |

采用鼠族异种 `RK_XenoType_Ratkin` 及导出的 14 个内源基因，其中个体颜色基因为 Skin_Melanin1 和 Hair_MidBlack。发色按快照固定，肤色沿用纯白肤色基因，不导入 CE 肤色覆盖。

12 项技能与热情保留导出的最终目标；采矿 6、种植 5 在正常 +4 资质下基础等级分别为 2、1。年龄固定为完整 29 岁，不搬入原世界出生时间和经验进度。FA 数据仅在 FA 与鼠族适配补丁同时加载时应用。

不直接导入 Hediff、能力、社交、记忆、需求、工作安排和记录。Liu 套装与长剑通过现有穿戴／装备通知产生对应效果，不复制导出中的临时光环 Buff、持续时间和重复套装状态。详细报告沿用用户授权拟写故事的规则，故事不追加人物机制。

生成、存档、购买、约一小时空投与一次 Letter 全部复用现有模块；没有新增运行 C#。

验证：既有验证程序 767 项检查及 95 项唯一 Def 引用核对通过，三位已导入扩展人物均与留存的 CE 快照一致。火焰兰 XML 与 FuFu 图标已同步到游戏模组目录并核对哈希；游戏当前运行，新增配置下次启动加载。未进行实机创建／购买，完整 Pawn 存读档、渲染及 GUI 不在本轮自动验证范围。

证据：RIMSAGE（2026-10-14）的 Nerves、Hair_MidBlack、LightPink；已核对的 SpeedOffset；原鼠族 1.6 HairDefs/Hairs.xml、WanderingTrader/Backstory_Caravan_Child.xml、BackStoryDefs/BackstoryDef_Adulthood.xml、Things_ItemDefs/Thing_Drink.xml；鼠族 FA 补丁 1.6 FaceTypeDefs；本 MOD Clothes_Liu.xml、Weapon_liu.xml。超织物耐久系数 1.3 沿用已核对证据，300×1.3=390。
