# 山荷叶：导入核对

2026-10-14。采用用户授权的 `D:\QQFL\pawnslots (3).txt` 第 4 行。文件包含五位人物，山荷叶只有一条记录；完整 57 字段见 [CE 导出快照](pawnslots_ShanHeYe.txt)。其他人物继续使用各自已确认的数据来源。

| 项目 | 固定初始配置 |
| --- | --- |
| 身份 | 山荷叶，女性，31 岁，南部 Zwei 协会，2000 银 |
| 文件／图标 | `Defs/SpecialCharacter/ShanHeYe.xml`／`SpecialCharacter/ShanHeYe` |
| 体型／普通头型 | Thin／RK_HeadType_Head4 |
| 发型 | 原鼠族 RK_LongA；加载 rpowder.ratkinhairplus 时使用导出 RK_Long_I |
| 主发色／副发色 | (0.824,0.737,0.541,1.000)／白色 |
| 喜爱颜色 | Structure_Blue |
| 童年／成年 | Ratkin_Kid／Ratkin_Knight |
| 普通特质 | Nerves 1、Tough 0、Brawler 0 |
| FA 六项 | Ratkin_Head、Ratkin_EyeStare、Ratkin_LidSleepy、Ratkin_Futomayu、Ratkin_MouthNormal、Ratkin_SkinNormal |
| 主副眼色 | 均为 (0.133,0.663,0.286,1.000) |
| 武器 | 钢 Zwei 双手剑，极佳，100 耐久 |
| 衣着 | 超织物 Zwei 西装与大衣，均极佳、415 耐久；颜色 (0.678,0.859,0.890,1.000) |
| 库存 | 硬饼干×2、烟叶卷×20 |

12 项技能与热情按新导出固定，近战为 14、双火；采矿 8、种植 6 在正常 +4 资质下基础等级为 4、2。经历与好斗特质的技能加成已包含在导出最终目标中，不再次叠加。采用鼠族异种及导出的 14 个内源基因，个体颜色基因为 Skin_Melanin3 与 Hair_DarkBrown；肤色保留纯白基因结果。

发型切换位于 `Patches/SpecialCharacter/ShanHeYe.xml`，两项替换使用 MayRequire="rpowder.ratkinhairplus"，同时替换 PawnKind.forcedHair 和人物 hair。扩展发型包名及名称来自 CE 导出；未读取该扩展模组目录。FA 六项及眼色沿用现有 FA＋鼠族补丁门控。

2026-10-14 发型核对：用户当前 ModsConfig 仅启用 momanacha.rkhairexpanded，未启用 rpowder.ratkinhairplus；CE 中山荷叶为鼠族长发 A，对应当前配置的未启用分支。已授权读取的 3329107474 模组只定义 RK_LongS、RK_Lucca、RK_SideponyO、RK_SidetailL、RK_TwintailR，不包含 RK_Long_I。山荷叶的导出发型与该模组中的发型不能直接按相同名称互换。

年龄固定完整 31 岁，不导入原世界出生时刻和经验进度。不搬入社交、记忆、需求、工作安排、记录及装备产生的 Hediff／能力。穿戴 Zwei 套装和装备双手剑时，由现有通知产生护甲、套装与武器效果。详细报告按已授权规则拟写，不追加人物机制。

生成、存档、购买、空投及一次 Letter 复用现有模块；首次查看详情创建实际 Pawn，之后展示与购买使用同一对象。运行 C# 无改动。

验证：既有验证程序 922 项检查及 106 项唯一 Def 引用核对通过，山荷叶身份、12 项技能／热情、特质、14 基因、发色、眼色、FA、装备物资和耐久均与新快照一致。可选发型补丁执行原版替换验证。XML、发型补丁和 FuFu 图标已同步到游戏目录并核对哈希；现有四位人物 XML、运行 C# 和 DLL 均保持原哈希。游戏当前运行，新增配置下次启动加载；完整 Pawn 生成／存读档、渲染及 GUI 不在本轮自动验证范围。

证据：RIMSAGE（2026-10-14）的 Nerves、Tough、Brawler、SmokeleafJoint、Structure_Blue；已核对的 Hair_DarkBrown；原鼠族 1.6 HairDefs/Hairs.xml、BackStoryDefs、HeadTypeDef；鼠族 FA 1.6 FaceTypeDefs；本 MOD Clothes_Zwei.xml、Weapon_zwei.xml。衣物基础耐久 320，超织物系数 1.3 得 416，按原版五点取整为 415。
