using System.Reflection;
using System.Runtime.Loader;

string gameRoot = args[0];
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    string path = Path.Combine(gameRoot, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
Assembly game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(gameRoot, "Assembly-CSharp.dll"));
if (args.Length == 3 && args[1] == "loadfolders")
{
    string modRoot = Path.GetFullPath(args[2]);
    var document = new System.Xml.XmlDocument();
    document.Load(Path.Combine(modRoot, "LoadFolders.xml"));
    Type loadFoldersType = game.GetType("Verse.ModLoadFolders", throwOnError: true)!;
    object loadFolders = Activator.CreateInstance(loadFoldersType)!;
    loadFoldersType.GetMethod("LoadDataFromXmlCustom")!.Invoke(loadFolders, new object[] { document.DocumentElement! });
    Type modType = game.GetType("Verse.ModContentPack", throwOnError: true)!;
    MethodInfo fileIndex = modType.GetMethod("GetAllFilesForMod", BindingFlags.Public | BindingFlags.Static)!;
    foreach (string version in new[] { "1.6", "1.7" })
    {
        var folderObjects = (System.Collections.IEnumerable)loadFoldersType.GetMethod("FoldersForVersion")!.Invoke(loadFolders, new object[] { version })!;
        string[] names = folderObjects.Cast<object>().Select(folder => (string)folder.GetType().GetField("folderName")!.GetValue(folder)!).ToArray();
        if (!names.SequenceEqual(new[] { "", version }))
            throw new InvalidOperationException("实际游戏解析器返回的加载目录不符：" + version);
        var directories = names.Reverse().Select(name => Path.GetFullPath(Path.Combine(modRoot, name))).ToList();
        Console.WriteLine($"PASS 实际 1.7 解析器：{version} 加载根目录与 {version}；优先级：{string.Join(" > ", directories)}");
        foreach (string category in new[] { "Textures", "Sounds", "Defs" })
        {
            var index = (System.Collections.IDictionary)fileIndex.Invoke(null, new object[] { null, category, null, directories })!;
            string expectedDirectory = Path.GetFullPath(Path.Combine(modRoot, category is "Textures" or "Sounds" ? category : version + "/" + category));
            FileInfo[] files = index.Values.Cast<FileInfo>().ToArray();
            int expectedCount = Directory.GetFiles(expectedDirectory, "*", SearchOption.AllDirectories).Length;
            if (files.Length != expectedCount || files.Any(file => !file.FullName.StartsWith(expectedDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("实际游戏资源索引的来源或数量不符：" + version + "/" + category);
            Console.WriteLine($"PASS 实际游戏文件索引：{version} / {category} = {files.Length} 个，来源 {expectedDirectory}");
        }
    }
    return;
}
foreach (string name in new[] { "Verse.ShaderTypeDef", "Verse.Def" })
{
    Type type = game.GetType(name, throwOnError: true)!;
    Console.WriteLine($"TYPE {type.FullName}; base={type.BaseType}; sealed={type.IsSealed}");
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    foreach (FieldInfo field in type.GetFields(flags))
        Console.WriteLine($"FIELD {field.Attributes} {field.FieldType} {field.Name}");
    foreach (PropertyInfo property in type.GetProperties(flags))
        Console.WriteLine($"PROPERTY {property.PropertyType} {property.Name}; getter={property.GetMethod?.Attributes}; setter={property.SetMethod?.Attributes}");
    foreach (MethodInfo method in type.GetMethods(flags).Where(method => method.Name is "ResolveReferences" or "PostLoad"))
        Console.WriteLine($"METHOD {method.Attributes} {method}");
}
