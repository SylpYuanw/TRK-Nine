using System;
using HarmonyLib;
using Verse;
using Shader = UnityEngine.Shader;

namespace XIYUNTE
{
    //在 Def 后处理创建材质前安装 Shader 入口，其余业务补丁仍由原静态启动器安装。
    public sealed class DeathBowRimUnityMod : Mod
    {
        public DeathBowRimUnityMod(ModContentPack content) : base(content)
        {
            var getter = AccessTools.PropertyGetter(typeof(ShaderTypeDef), nameof(ShaderTypeDef.Shader))
                ?? throw new InvalidOperationException("ShaderTypeDef.Shader getter is unavailable.");
            new Harmony("XIYUNTE.DeathBowRimUnity").Patch(getter,
                prefix: new HarmonyMethod(typeof(DeathBowRimUnityShaderPatch), nameof(DeathBowRimUnityShaderPatch.Prefix)));
        }
    }

    //显式安装的 Prefix 只接管死之弓的两个 ShaderTypeDef，不参与后续 PatchAll 扫描。
    internal static class DeathBowRimUnityShaderPatch
    {
        internal static bool Prefix(ShaderTypeDef __instance, ref Shader __result)
        {
            if (!DeathBowRimUnityShaders.TryGetShader(__instance, out Shader shader))
                return true;
            __result = shader;
            return false;
        }
    }
}
