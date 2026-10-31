using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;

// 仅解析本 Mod 业务程序集的加载接入方法体；外部程序集仅用于解析类型和成员签名。
string dll = Path.GetFullPath(args[0]);
if (Path.GetFileName(dll) != "ClassLibrary1.dll" || !dll.Contains("鼠鼠收尾人们目前已到245679协会", StringComparison.Ordinal))
    throw new ArgumentException("审计目标必须是本 Mod 的 ClassLibrary1.dll。");
string[] searchRoots = {
    Path.GetDirectoryName(dll)!,
    @"D:\Steam\steamapps\common\RimWorld",
    @"D:\Steam\steamapps\workshop\content\294100\3819570122\1.7\Assemblies",
    @"D:\Steam\steamapps\workshop\content\294100\2009463077\1.7\Assemblies"
};
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    string path = searchRoots.Select(root => Path.Combine(root, name.Name + ".dll")).FirstOrDefault(File.Exists);
    return path == null ? null : AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
};
Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
    .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null)!)
    .ToDictionary(op => unchecked((ushort)op.Value));
const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
string[] names = { "XIYUNTE.DeathBowRimUnityShaders", "XIYUNTE.DeathBowRimUnityShaderPatch", "XIYUNTE.DeathBowRimUnityMod", "XIYUNTE.DeliveryBoxStatPatchesInit" };
var types = new List<object>();
var methods = new List<(string name, List<string> il)>();
foreach (string name in names)
{
    Type type = assembly.GetType(name, true)!;
    var bodies = type.GetMethods(flags | BindingFlags.DeclaredOnly).Cast<MethodBase>()
        .Concat(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        .Concat(type.TypeInitializer == null ? Array.Empty<MethodBase>() : new[] { type.TypeInitializer });
    var outputMethods = new List<object>();
    foreach (MethodBase method in bodies)
    {
        if (method.Module.Assembly != assembly) throw new InvalidOperationException("禁止读取外部程序集方法体。");
        byte[] il = method.GetMethodBody()?.GetILAsByteArray() ?? Array.Empty<byte>();
        var instructions = new List<string>();
        for (int cursor = 0; cursor < il.Length;)
        {
            int offset = cursor;
            ushort code = il[cursor++];
            if (code == 0xfe) code = (ushort)(0xfe00 | il[cursor++]);
            OpCode op = opcodes[code];
            string operand = "";
            int count;
            switch (op.OperandType)
            {
                case OperandType.InlineNone: count = 0; break;
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineVar: count = 1; operand = il[cursor].ToString(); break;
                case OperandType.InlineVar: count = 2; operand = BitConverter.ToUInt16(il, cursor).ToString(); break;
                case OperandType.InlineI8:
                case OperandType.InlineR: count = 8; operand = BitConverter.ToInt64(il, cursor).ToString(); break;
                case OperandType.InlineSwitch:
                    count = 4 + 4 * BitConverter.ToInt32(il, cursor); operand = "switch"; break;
                case OperandType.InlineString:
                    count = 4; operand = method.Module.ResolveString(BitConverter.ToInt32(il, cursor)); break;
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                    count = 4;
                    MemberInfo member = method.Module.ResolveMember(BitConverter.ToInt32(il, cursor))!;
                    operand = member.DeclaringType + "::" + member;
                    break;
                default: count = 4; operand = BitConverter.ToInt32(il, cursor).ToString(); break;
            }
            cursor += count;
            instructions.Add($"IL_{offset:x4}: {op.Name} {operand}".TrimEnd());
        }
        methods.Add((name + "." + method.Name, instructions));
        outputMethods.Add(new { name = method.Name, instructions });
    }
    types.Add(new { name, attributes = type.GetCustomAttributesData().Select(attribute => attribute.ToString()).ToArray(), methods = outputMethods });
}
string[] helper = methods.Single(method => method.name.EndsWith("DeathBowRimUnityShaders.TryGetShader")).il.ToArray();
string[] prefix = methods.Single(method => method.name.EndsWith("DeathBowRimUnityShaderPatch.Prefix")).il.ToArray();
string[] startup = methods.Single(method => method.name.EndsWith("DeliveryBoxStatPatchesInit..cctor")).il.ToArray();
string[] modConstructor = methods.Single(method => method.name.EndsWith("DeathBowRimUnityMod..ctor")).il.ToArray();
bool assetLoad = helper.Any(line => line.Contains("RimUnity.Assets.AssetLoader::") && line.Contains("Load[Shader]"));
bool hostConversion = helper.Any(line => line.Contains("RimUnity.Assets.HostAssets::") && line.Contains("ToHost"));
bool intercept = prefix.Any(line => line.Contains("DeathBowRimUnityShaders::") && line.Contains("TryGetShader"))
    && prefix.Any(line => line.Contains("stind.ref")) && prefix.Any(line => line.Contains("ldc.i4.0"));
bool patchAll = startup.Any(line => line.Contains("HarmonyLib.Harmony::") && line.Contains("PatchAll"));
bool earlyPatch = modConstructor.Any(line => line.Contains("HarmonyLib.Harmony::") && line.Contains(" Patch("))
    && modConstructor.Any(line => line.Contains("HarmonyLib.AccessTools::") && line.Contains("PropertyGetter"));
bool duplicateScan = assembly.GetType("XIYUNTE.DeathBowRimUnityShaderPatch", true)!.GetCustomAttributesData().Any(attribute => attribute.AttributeType.FullName == "HarmonyLib.HarmonyPatch");
bool hasOldLoader = methods.SelectMany(method => method.il).Any(line => line.Contains("AssetBundle") || line.Contains("LoadFromFile"));
Type hostShader = assembly.GetType("XIYUNTE.DeathBowRimUnityShaderPatch", true)!.GetMethod("Prefix", flags)!.GetParameters()[1].ParameterType.GetElementType()!;
string hostShaderAssembly = hostShader.Assembly.GetName().Name!;
bool passed = assetLoad && hostConversion && intercept && patchAll && earlyPatch && !duplicateScan && !hasOldLoader && hostShaderAssembly == "UnityStubs";
var result = new {
    passed, dll, sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dll))),
    targetFramework = assembly.GetCustomAttributesData().Single(attribute => attribute.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute").ConstructorArguments[0].Value,
    assetLoad, hostConversion, intercept, patchAll, earlyPatch, duplicateScan, hasOldLoader, hostShaderAssembly, types
};
File.WriteAllText(args[1], JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"passed={passed}; AssetLoader={assetLoad}; HostAssets={hostConversion}; Prefix={intercept}; PatchAll={patchAll}; oldAssetBundleLoader={hasOldLoader}");
if (!passed) Environment.ExitCode = 1;
