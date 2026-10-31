using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XIYUNTE
{
    // 死之弓武器实例:信息卡按当前蓄力阶输出远程数据。
    // 原版的远程条目(伤害/穿透/瞄准时间/射程等)读的是武器 Def 上那条默认弹丸与静态动词参数,
    // 蓄力阶切换后不会变化,因此这里过滤掉静态远程条目,再用当前阶的弹丸重新生成。
    public class Thing_DeathBow : ThingWithComps
    {
        private Graphic heldSouthGraphic;

        // 非瞄准朝南持握使用独立贴图；其他方向与瞄准时使用武器原图。
        public override Graphic Graphic
        {
            get
            {
                Pawn pawn = (ParentHolder as Pawn_EquipmentTracker)?.pawn;
                if (pawn == null || pawn.Rotation != Rot4.South)
                {
                    return base.Graphic;
                }

                Stance_Busy stance = pawn.stances.curStance as Stance_Busy;
                if (stance != null && !stance.neverAimWeapon && stance.focusTarg.IsValid)
                {
                    return base.Graphic;
                }

                if (heldSouthGraphic == null)
                {
                    GraphicData source = def.graphicData;
                    heldSouthGraphic = new GraphicData
                    {
                        texPath = GetComp<CompEquippable_DeathBow>().Props.heldSouthTexPath,
                        graphicClass = typeof(Graphic_Single),
                        shaderType = source.shaderType,
                        drawSize = source.drawSize,
                        ignoreThingDrawColor = source.ignoreThingDrawColor
                    }.GraphicColoredFor(this);
                }
                return heldSouthGraphic;
            }
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            CompEquippable_DeathBow bow = GetComp<CompEquippable_DeathBow>();
            foreach (StatDrawEntry entry in base.SpecialDisplayStats())
            {
                if (bow != null && entry.category == StatCategoryDefOf.Weapon_Ranged)
                {
                    continue;
                }
                yield return entry;
            }
            if (bow == null)
            {
                yield break;
            }
            yield return new StatDrawEntry(StatCategoryDefOf.BasicsImportant, "BowOfDeath_StatMode".Translate(), GetModeText(bow), "BowOfDeath_StatModeDesc".Translate(), 6010);
            DeathBowStage stage = bow.CurrentStage;
            if (stage?.projectile?.projectile == null)
            {
                yield break;
            }
            ProjectileProperties projectile = stage.projectile.projectile;
            yield return new StatDrawEntry(StatCategoryDefOf.BasicsImportant, "BowOfDeath_StatStage".Translate(), (bow.StageIndex + 1).ToString(), CompEquippable_DeathBow.GetStageDescription(stage), 6000);
            yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "RangedWarmupTime".Translate(), stage.warmupTime.ToString("0.##") + " " + "LetterSecond".Translate(), "Stat_Thing_Weapon_RangedWarmupTime_Desc".Translate(), 5555);
            yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "Damage".Translate(), projectile.GetDamageAmount(this).ToString(), "Stat_Thing_Damage_Desc".Translate(), 5500);
            if (projectile.damageDef != null && projectile.damageDef.armorCategory != null)
            {
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "ArmorPenetration".Translate(), projectile.GetArmorPenetration(this).ToStringPercent(), "ArmorPenetrationExplanation".Translate(), 5400);
            }
            if (projectile.stoppingPower > 0f)
            {
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StoppingPower".Translate(), projectile.stoppingPower.ToString("F1"), "StoppingPowerExplanation".Translate(), 5402);
            }
            VerbProperties primaryVerb = def.Verbs?.FirstOrDefault(v => v.isPrimary);
            if (primaryVerb != null)
            {
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "Range".Translate(), primaryVerb.range.ToString("F0"), "Stat_Thing_Weapon_Range_Desc".Translate(), 5390);
            }
        }

        private static string GetModeText(CompEquippable_DeathBow bow)
        {
            string mode = bow.FlashBowActive ? "BowOfDeath_Mode_FlashBow".Translate(bow.Charges, bow.Props.maxCharges) : "BowOfDeath_Mode_Melee".Translate();
            return mode.ToString();
        }
    }
}
