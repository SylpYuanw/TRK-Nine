using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace XIYUNTE.SpecialCharacter
{
    // 各协会人物的固定初始配置；人物加入后不再重新应用。
    public class SpecialCharacterDef : Def
    {
        public PawnKindDef pawnKind;
        public string association;
        [MustTranslate] public string detailedReport;
        [MustTranslate] public string arrivalGreeting;
        public string iconPath;
        public int price;
        public string firstName;
        public string nickname;
        public string lastName = "";
        public float biologicalAge;
        public Gender gender;
        public BodyTypeDef bodyType;
        public HeadTypeDef headType;
        public HairDef hair;
        public Color hairColor;
        public Color hairColorSecond;
        public string favoriteColor = "LightPurple";
        public SpecialCharacterFacialAnimation facialAnimation;
        public List<int> addonVariants;
        public string xenotype;
        public List<string> endogenes;
        public List<SpecialCharacterSkill> skills;
        public SpecialCharacterItem weapon;
        public List<SpecialCharacterItem> apparel;
        public List<SpecialCharacterItem> inventory;
        [Unsaved(false)] private Texture2D icon;

        public Texture2D Icon => icon ?? (icon = ContentFinder<Texture2D>.Get(iconPath));

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            if (pawnKind == null) yield return "pawnKind is required.";
            if (price <= 0) yield return "price must be positive.";
            if (firstName.NullOrEmpty() || nickname.NullOrEmpty()) yield return "Character name is required.";
            if (iconPath.NullOrEmpty() || association.NullOrEmpty()) yield return "Association and icon are required.";
            if (detailedReport.NullOrEmpty()) yield return "A detailed character report is required.";
            if (biologicalAge <= 0 || bodyType == null || headType == null || hair == null)
                yield return "Fixed age and appearance are required.";
            if (addonVariants == null || addonVariants.Any(i => i < 0)) yield return "Fixed addon variants are required.";
            if (xenotype.NullOrEmpty() || endogenes.NullOrEmpty() || endogenes.Distinct().Count() != endogenes.Count)
                yield return "A xenotype and unique endogenes are required.";
            if (pawnKind != null && (pawnKind.forcedTraits.NullOrEmpty() ||
                pawnKind.forcedTraits.Any(t => t.def == null || !t.degree.HasValue)))
                yield return "Pawn kind fixed traits require a def and degree.";
            if (skills.NullOrEmpty() || skills.Any(s => s == null || s.skill == null || s.level < 0 || s.level > 20) ||
                skills.Select(s => s.skill).Distinct().Count() != skills.Count)
                yield return "Skill targets must be unique and between 0 and 20.";
            if (weapon == null || apparel.NullOrEmpty() || inventory.NullOrEmpty())
                yield return "Fixed equipment and inventory are required.";
            else
                foreach (SpecialCharacterItem item in new[] { weapon }.Concat(apparel).Concat(inventory))
                    if (item == null || item.thing == null || item.count <= 0 || item.hitPoints <= 0)
                        yield return "Fixed items require a definition, count and durability.";
        }
    }

    public class SpecialCharacterSkill
    {
        public SkillDef skill;
        public int level;
        public Passion passion;
    }

    public class SpecialCharacterItem
    {
        public ThingDef thing;
        public ThingDef stuff;
        public int count = 1;
        public QualityCategory quality = QualityCategory.Normal;
        public int hitPoints;
        public Color color = Color.white;
    }
}
