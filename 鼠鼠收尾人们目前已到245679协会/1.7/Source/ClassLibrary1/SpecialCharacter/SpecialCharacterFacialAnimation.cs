using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace XIYUNTE.SpecialCharacter
{
    // CE 面部六项类型和两项眼色；字符串配置不建立可选模组的 XML Def 引用。
    public sealed class SpecialCharacterFacialAnimation
    {
        public string head;
        public string eyes;
        public string lids;
        public string brows;
        public string mouth;
        public string skin;
        public Color eyeColor;
        public Color eyeColorSecond;

        // 仅在 FA 与鼠族面部补丁同时加载时解析外部类型及 Def。
        public void Apply(Pawn pawn)
        {
            if (!ModsConfig.IsActive("Nals.FacialAnimation") || !ModsConfig.IsActive("fxz.Ratkin.FacialAnimation"))
                return;

            ApplyConfiguration(pawn);
        }

        private void ApplyConfiguration(Pawn pawn)
        {
            ThingComp[] controllers =
            {
                SetFace(pawn, "HeadControllerComp", head),
                SetFace(pawn, "EyeballControllerComp", eyes),
                SetFace(pawn, "LidControllerComp", lids),
                SetFace(pawn, "BrowControllerComp", brows),
                SetFace(pawn, "MouthControllerComp", mouth),
                SetFace(pawn, "SkinControllerComp", skin)
            };
            ThingComp eyeballs = controllers[1];
            eyeballs.GetType().GetProperty("FaceColor").SetValue(eyeballs, eyeColor, null);
            eyeballs.GetType().GetProperty("FaceSecondColor").SetValue(eyeballs, eyeColorSecond, null);
            // 固定类型和眼色先于初始化写入，FA 不再随机选择未初始化的面部配置。
            foreach (ThingComp comp in controllers)
            {
                comp.GetType().GetMethod("InitializeIfNeed").Invoke(comp, null);
                comp.GetType().GetMethod("SetDirty").Invoke(comp, null);
            }
        }

        private static ThingComp SetFace(Pawn pawn, string controller, string defName)
        {
            Type controllerType = AccessTools.TypeByName("FacialAnimation." + controller);
            ThingComp comp = (ThingComp)typeof(ThingWithComps).GetMethod("GetComp")
                .MakeGenericMethod(controllerType).Invoke(pawn, null);
            Type type = comp.GetType();
            var faceType = type.GetProperty("FaceType");
            Def def = GenDefDatabase.GetDef(faceType.PropertyType, defName, errorOnFail: false) ??
                throw new InvalidOperationException("Missing fixed Facial Animation definition: " + defName);
            faceType.SetValue(comp, def, null);
            return comp;
        }
    }
}
