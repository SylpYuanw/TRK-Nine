using System;
using System.Collections.Generic;
using RimUnity.Assets;
using Verse;
using Shader = UnityEngine.Shader;

namespace XIYUNTE
{
    //按所属模组的明文清单加载死之弓 Shader，并缓存宿主资源包装。
    internal static class DeathBowRimUnityShaders
    {
        private static readonly Dictionary<ShaderTypeDef, Shader> Shaders = new Dictionary<ShaderTypeDef, Shader>(2);

        public static bool TryGetShader(ShaderTypeDef definition, out Shader shader)
        {
            shader = null;
            if (definition.defName != "Trk_BowOfDeathAimWind" && definition.defName != "Trk_BowOfDeathAimCharge")
                return false;
            if (Shaders.TryGetValue(definition, out shader))
                return true;

            ModContentPack contentMod = definition.modContentPack
                ?? throw new InvalidOperationException("死之弓 Shader 缺少资源所属模组：" + definition.defName);
            RimUnity.Shader resource = AssetLoader.Load<RimUnity.Shader>(contentMod, definition.shaderPath);
            shader = HostAssets.ToHost(resource);
            Shaders.Add(definition, shader);
            return true;
        }
    }
}
