using UnityEngine;
using Verse;

namespace XIYUNTE
{
    // 逐实例传递朝向、随机种子及最终透明度，不修改MaterialPool中的共享材质。
    public class Graphic_DeathBowWindMote : Graphic_Mote
    {
        private static readonly MaterialPropertyBlock InstanceProperties = new MaterialPropertyBlock();
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SeedId = Shader.PropertyToID("_FlowSeed");
        private static readonly int SideId = Shader.PropertyToID("_BodyAimSide");

        public static void FillProperties(MaterialPropertyBlock properties, Mote_DeathBowAimVfxBase mote, Color tint)
        {
            properties.Clear();
            Color color = tint * mote.instanceColor;
            color.a *= mote.Alpha;
            properties.SetColor(ColorId, color);
            properties.SetFloat(SeedId, mote.EffectSeed);
            if (mote is Mote_DeathBowAimVfx body)
                properties.SetFloat(SideId, body.BodyAimSide);
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            Mote_DeathBowAimVfxBase mote = (Mote_DeathBowAimVfxBase)thing;
            FillProperties(InstanceProperties, mote, Color);
            DrawMote(data, MatSingle, Color, loc, rot, thingDef, thing, 0, true, InstanceProperties);
        }
    }
}
