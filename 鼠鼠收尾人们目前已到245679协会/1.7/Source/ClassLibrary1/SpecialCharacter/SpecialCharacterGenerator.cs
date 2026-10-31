using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace XIYUNTE.SpecialCharacter
{
    public static class SpecialCharacterGenerator
    {
        // HAR 外观接口只在正式人物首次生成时应用。
        private static readonly Type AlienCompType = AccessTools.TypeByName("AlienRace.AlienPartGenerator+AlienComp");
        private static readonly MethodInfo SetChannel = AlienCompType == null ? null :
            AlienCompType.GetMethod("OverwriteColorChannel", new[] { typeof(string), typeof(Color?), typeof(Color?) });
        private static readonly FieldInfo AddonVariants = AlienCompType == null ? null : AlienCompType.GetField("addonVariants");

        // 生成对象立即交给世界组件持有，初始化失败时由创建事务统一清理。
        public static void Generate(SpecialCharacterDef def, Map map, out Pawn preparedPawn, out Thing preparedItem)
        {
            preparedPawn = null;
            preparedItem = null;
            if (!ModsConfig.BiotechActive) throw new InvalidOperationException("Special character genes require Biotech.");
            string[] errors = def.ConfigErrors().ToArray();
            if (errors.Length != 0) throw new InvalidOperationException(def.defName + ": " + string.Join("; ", errors));
            List<GeneDef> genes = def.endogenes.Select(n => DefDatabase<GeneDef>.GetNamed(n)).ToList();
            XenotypeDef xenotype = DefDatabase<XenotypeDef>.GetNamed(def.xenotype);

            // 异种提供种族基因，人物清单只追加异种未包含的固定基因。
            PawnGenerationRequest request = new PawnGenerationRequest(def.pawnKind,
                tile: map.Tile, forceGenerateNewPawn: true, canGeneratePawnRelations: false,
                allowFood: false, fixedBiologicalAge: def.biologicalAge, fixedGender: def.gender,
                forcedXenotype: xenotype, forcedEndogenes: genes.Except(xenotype.genes).ToList(),
                onlyUseForcedBackstories: true, maximumAgeTraits: 0, forceNoGear: true);
            request.ForceBodyType = def.bodyType;
            request.ForceNoIdeoGear = true;
            Pawn pawn = preparedPawn = PawnGenerator.GeneratePawn(request);
            if (pawn == null || pawn.Dead || pawn.Destroyed)
                throw new InvalidOperationException("PawnGenerator did not return a living character.");
            pawn.Name = new NameTriple(def.firstName, def.nickname, def.lastName);
            // 原版可能额外生成肤色或发色基因，固定外观初始化移除清单外的随机颜色基因。
            foreach (Gene gene in pawn.genes.Endogenes.ToList())
                if ((gene.def.endogeneCategory == EndogeneCategory.Melanin ||
                    gene.def.endogeneCategory == EndogeneCategory.HairColor) && !genes.Contains(gene.def))
                    pawn.genes.RemoveGene(gene);
            if (pawn.genes.Xenogenes.Any() || pawn.genes.Endogenes.Count != genes.Count ||
                !new HashSet<GeneDef>(pawn.genes.Endogenes.Select(g => g.def)).SetEquals(genes))
                throw new InvalidOperationException("Generated genes differ from the fixed character configuration. Endogenes: " +
                    string.Join(", ", pawn.genes.Endogenes.Select(g => g.def.defName)) + "; Xenogenes: " +
                    string.Join(", ", pawn.genes.Xenogenes.Select(g => g.def.defName)));

            // 移除额外普通特质，保留基因正常提供的特质及其通知链路。
            List<TraitRequirement> traits = def.pawnKind.forcedTraits;
            foreach (Trait trait in pawn.story.traits.allTraits.ToList())
                if (trait.sourceGene == null && !traits.Any(t => t.def == trait.def && t.degree == trait.Degree))
                    pawn.story.traits.RemoveTrait(trait, unsuppressConflicts: true);
            if (pawn.story.traits.allTraits.Count(t => t.sourceGene == null) != traits.Count ||
                traits.Any(t => !pawn.story.traits.allTraits.Any(a => a.sourceGene == null && a.def == t.def && a.Degree == t.degree)))
                throw new InvalidOperationException("Generated ordinary traits differ from the fixed configuration.");
            if (pawn.story.Childhood != def.pawnKind.fixedChildBackstories.Single() ||
                pawn.story.Adulthood != def.pawnKind.fixedAdultBackstories.Single())
                throw new InvalidOperationException("Generated backstories differ from the fixed configuration.");

            ApplyAppearance(pawn, def);
            if (pawn.equipment.AllEquipmentListForReading.Any() || pawn.apparel.WornApparel.Any() ||
                pawn.inventory.innerContainer.Any())
                throw new InvalidOperationException("A dependency generated unexpected automatic gear.");
            ApplyOutfit(pawn, def, ref preparedItem);
            foreach (SpecialCharacterItem spec in def.inventory)
            {
                Thing item = MakeItem(spec, ref preparedItem);
                if (!pawn.inventory.innerContainer.TryAdd(item, canMergeWithExistingStacks: false))
                    throw new InvalidOperationException("Could not add " + spec.thing.defName + " to inventory.");
                preparedItem = null;
            }

            pawn.skills.DirtyAptitudes();
            if (pawn.skills.skills.Count != def.skills.Count)
                throw new InvalidOperationException("Fixed configuration does not cover all loaded skills.");
            foreach (SpecialCharacterSkill spec in def.skills)
            {
                SkillRecord skill = pawn.skills.GetSkill(spec.skill);
                int baseLevel = spec.level - skill.Aptitude;
                if (baseLevel < 0 || baseLevel > 20)
                    throw new InvalidOperationException("Cannot satisfy fixed skill target: " + spec.skill.defName);
                skill.levelInt = baseLevel;
                skill.passion = spec.passion;
                skill.xpSinceLastLevel = 0;
                skill.xpSinceMidnight = 0;
                if (skill.Level != spec.level)
                    throw new InvalidOperationException("Final skill level mismatch: " + spec.skill.defName);
            }
            pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        private static void ApplyAppearance(Pawn pawn, SpecialCharacterDef def)
        {
            if (AlienCompType == null || SetChannel == null || AddonVariants == null)
                throw new InvalidOperationException("Required HAR AlienComp appearance API is missing.");
            pawn.story.bodyType = def.bodyType;
            pawn.story.headType = def.headType;
            pawn.story.hairDef = def.hair;
            pawn.style.beardDef = BeardDefOf.NoBeard;
            if (ModsConfig.IdeologyActive)
            {
                pawn.style.FaceTattoo = DefDatabase<TattooDef>.GetNamed("NoTattoo_Face");
                pawn.style.BodyTattoo = DefDatabase<TattooDef>.GetNamed("NoTattoo_Body");
                pawn.story.favoriteColor = DefDatabase<ColorDef>.GetNamed(def.favoriteColor);
            }
            ThingComp alien = pawn.AllComps.Single(c => AlienCompType.IsInstanceOfType(c));
            // 首次访问 HAR 通道会初始化并写回 story 发色，固定发色在通道初始化完成后设置。
            SetChannel.Invoke(alien, new object[] { "hair", def.hairColor, def.hairColorSecond });
            pawn.story.HairColor = def.hairColor;
            AddonVariants.SetValue(alien, new List<int>(def.addonVariants));
            def.facialAnimation?.Apply(pawn);
        }

        private static void ApplyOutfit(Pawn pawn, SpecialCharacterDef def, ref Thing preparedItem)
        {
            ThingWithComps weapon = (ThingWithComps)MakeItem(def.weapon, ref preparedItem);
            pawn.equipment.AddEquipment(weapon);
            if (pawn.equipment.Primary != weapon)
                throw new InvalidOperationException("Could not equip fixed weapon.");
            preparedItem = null;
            foreach (SpecialCharacterItem spec in def.apparel)
            {
                if (pawn.apparel.WornApparel.Any(a => !ApparelUtility.CanWearTogether(a.def, spec.thing, pawn.RaceProps.body)))
                    throw new InvalidOperationException("Fixed apparel layers are incompatible.");
                Apparel apparel = (Apparel)MakeItem(spec, ref preparedItem);
                pawn.apparel.Wear(apparel, dropReplacedApparel: false);
                if (!pawn.apparel.WornApparel.Contains(apparel))
                    throw new InvalidOperationException("Could not wear " + spec.thing.defName);
                preparedItem = null;
            }
        }

        // 原版 Destroy 会转交 WorldPawns；撤销生成使用原版 Discard 完成销毁和解除引用。
        internal static void DiscardPreparedPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Discarded) return;
            if (pawn.Spawned) throw new InvalidOperationException("Cannot discard an arrived character.");
            if (Find.WorldPawns.Contains(pawn)) Find.WorldPawns.RemovePawn(pawn);
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
        }

        private static Thing MakeItem(SpecialCharacterItem spec, ref Thing preparedItem)
        {
            if (spec.thing == null || spec.count <= 0 || spec.count > spec.thing.stackLimit)
                throw new InvalidOperationException("Invalid fixed item configuration.");
            Thing item = preparedItem = ThingMaker.MakeThing(spec.thing, spec.stuff);
            item.stackCount = spec.count;
            CompQuality quality = item.TryGetComp<CompQuality>();
            if (quality == null && spec.quality != QualityCategory.Normal)
                throw new InvalidOperationException(spec.thing.defName + " fixed quality requires a quality component.");
            quality?.SetQuality(spec.quality, ArtGenerationContext.Colony);
            if (spec.hitPoints <= 0 || spec.hitPoints > item.MaxHitPoints)
                throw new InvalidOperationException(spec.thing.defName + " fixed durability exceeds its current limit.");
            item.HitPoints = spec.hitPoints;
            CompColorable colorable = item.TryGetComp<CompColorable>();
            if (colorable != null) colorable.SetColor(spec.color);
            else if (spec.color != Color.white)
                throw new InvalidOperationException(spec.thing.defName + " has no color component.");
            return item;
        }
    }
}
