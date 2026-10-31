using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;

// 仅读取程序集元数据和成员签名，不读取或反编译方法体。
string gameRoot = @"D:\Steam\steamapps\common\RimWorld";
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    string path = Path.Combine(gameRoot, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
string assemblyPath = Path.GetFullPath(args[0]);
using var stream = File.OpenRead(assemblyPath);
using var pe = new PEReader(stream);
MetadataReader metadata = pe.GetMetadataReader();
if (args.Contains("--names"))
{
    foreach (var handle in metadata.TypeDefinitions)
    {
        var type = metadata.GetTypeDefinition(handle);
        Console.WriteLine(metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name));
    }
    return;
}
var matches = metadata.TypeDefinitions.Select(handle => metadata.GetTypeDefinition(handle))
    .Where(type => args.Skip(1).Any(pattern => metadata.GetString(type.Name).Contains(pattern, StringComparison.OrdinalIgnoreCase)))
    .Select(type => metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name)).ToArray();
Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
Console.WriteLine(assembly.FullName);
if (args.Contains("--assembly"))
{
    foreach (var attribute in assembly.GetCustomAttributesData().Where(attribute => attribute.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute"))
        Console.WriteLine("TargetFramework=" + attribute.ConstructorArguments[0].Value);
    foreach (var reference in assembly.GetReferencedAssemblies())
        Console.WriteLine("REFERENCE " + reference.FullName);
    return;
}
const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
foreach (string name in matches)
{
    Type type = assembly.GetType(name, true)!;
    Console.WriteLine($"TYPE {type}; base={type.BaseType}");
    foreach (FieldInfo field in type.GetFields(flags))
        Console.WriteLine($"FIELD {field.Attributes} {field.FieldType} {field.Name}");
    foreach (PropertyInfo property in type.GetProperties(flags))
        Console.WriteLine($"PROPERTY {property.PropertyType} {property.Name}; get={property.GetMethod?.Attributes}; set={property.SetMethod?.Attributes}");
    foreach (MethodInfo method in type.GetMethods(flags).Where(method => !method.IsSpecialName))
        Console.WriteLine($"METHOD {method.Attributes} {method}");
}
