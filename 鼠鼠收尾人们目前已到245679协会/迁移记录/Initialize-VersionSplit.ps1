$ErrorActionPreference = 'Stop'
$modRoot = Split-Path -Path $PSScriptRoot -Parent
$unity16 = 'D:\UnityTest\unity_trk_35'
$unity17 = 'D:\UnityTest\unity_trk_35_1.7'
$rimUnityRoot = 'D:\UnityTest\RimUnity_c0e241a'
$utf8 = [Text.UTF8Encoding]::new($false)
$evidenceRoot = Join-Path $modRoot '迁移记录'
$runtimeFolders = @('Assemblies', 'AssetBundles', 'Defs', 'Languages', 'Patches', 'Sounds', 'Textures')
$sourceFolders = @('ClassLibrary1', 'WeaponAbility', 'Verification')
$unityFolders = @('Assets', 'Packages', 'ProjectSettings')

# 所有移动目标在执行前解析并限定在指定内容模组内。
foreach ($relative in @('1.6', '1.7', '1.6\Source', '1.7\Source')) {
    $target = [IO.Path]::GetFullPath((Join-Path $modRoot $relative))
    if (!$target.StartsWith($modRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "目标越界：$target" }
    if (Test-Path -LiteralPath $target) { throw "目标已存在，需按任务书核对：$target" }
}
if ([IO.Path]::GetFullPath($unity17) -ne 'D:\UnityTest\unity_trk_35_1.7') { throw '新 Unity 工程目标不符。' }
if (Test-Path -LiteralPath $unity17) { throw "目标已存在，需按任务书核对：$unity17" }
foreach ($folder in $runtimeFolders) {
    if (!(Test-Path -LiteralPath (Join-Path $modRoot $folder) -PathType Container)) { throw "缺少原运行目录：$folder" }
}
foreach ($folder in $sourceFolders) {
    if (!(Test-Path -LiteralPath (Join-Path $modRoot ('Source\' + $folder)) -PathType Container)) { throw "缺少原源码目录：$folder" }
}

function Get-FileRecords([string]$root, [string[]]$folders) {
    foreach ($folder in $folders) {
        foreach ($file in Get-ChildItem -LiteralPath (Join-Path $root $folder) -File -Recurse -Force) {
            [pscustomobject]@{
                path = $file.FullName.Substring($root.Length + 1).Replace('\', '/')
                sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
                bytes = $file.Length
            }
        }
    }
}

$before = [ordered]@{
    created = [DateTimeOffset]::Now.ToString('o')
    modRoot = $modRoot
    unity16 = $unity16
    unity17 = $unity17
    rimUnityCommit = 'c0e241a6848ddedffe5d7e41bd101e9fd140acf6'
    aboutHash = (Get-FileHash -LiteralPath (Join-Path $modRoot 'About\About.xml') -Algorithm SHA256).Hash
    runtime = @(Get-FileRecords $modRoot $runtimeFolders)
    source = @(Get-FileRecords (Join-Path $modRoot 'Source') $sourceFolders)
    unity = @(Get-FileRecords $unity16 $unityFolders)
}
[IO.File]::WriteAllText((Join-Path $evidenceRoot 'Before.json'), ($before | ConvertTo-Json -Depth 8), $utf8)

# 先建立两套源码目录，1.7 不携带 1.6 编译缓存和引用副本。
New-Item -ItemType Directory -Path (Join-Path $modRoot '1.6\Source'), (Join-Path $modRoot '1.7\Source') | Out-Null
foreach ($folder in $sourceFolders) {
    Move-Item -LiteralPath (Join-Path $modRoot ('Source\' + $folder)) -Destination (Join-Path $modRoot ('1.6\Source\' + $folder))
}
foreach ($folder in @('ClassLibrary1', 'WeaponAbility')) {
    $source = Join-Path $modRoot ('1.6\Source\' + $folder)
    $target = Join-Path $modRoot ('1.7\Source\' + $folder)
    foreach ($file in Get-ChildItem -LiteralPath $source -File -Recurse -Force) {
        $relative = $file.FullName.Substring($source.Length + 1)
        if ($relative -match '(^|\\)(bin|obj|\.vs)(\\|$)') { continue }
        $destination = Join-Path $target $relative
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination
    }
}
$project16 = Join-Path $modRoot '1.6\Source\ClassLibrary1\ClassLibrary1.csproj'
$projectText = [IO.File]::ReadAllText($project16, [Text.Encoding]::UTF8)
$projectText = $projectText.Replace('<HintPath>..\..\..\Ratkin\', '<HintPath>..\..\..\..\Ratkin\')
$projectText = $projectText.Replace('<HintPath>..\..\..\本机源码\', '<HintPath>..\..\..\..\本机源码\')
[IO.File]::WriteAllText($project16, $projectText, $utf8)
$project17 = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>ClassLibrary1</AssemblyName>
    <RootNamespace>ClassLibrary1</RootNamespace>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <Deterministic>true</Deterministic>
    <DefineConstants>$(DefineConstants);RIMWORLD_1_7</DefineConstants>
    <OutputPath>../../Assemblies/</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
    <RimWorldDir Condition="'$(RimWorldDir)' == ''">D:/Steam/steamapps/common/RimWorld</RimWorldDir>
    <RimUnityDir Condition="'$(RimUnityDir)' == ''">D:/UnityTest/RimUnity_c0e241a</RimUnityDir>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="Assembly-CSharp" HintPath="$(RimWorldDir)/Assembly-CSharp.dll" Private="false" />
    <Reference Include="UnityStubs" HintPath="$(RimWorldDir)/UnityStubs.dll" Private="false" />
    <Reference Include="Waylith" HintPath="$(RimWorldDir)/Waylith.dll" Private="false" />
    <Reference Include="RimUnity.Core" HintPath="$(RimUnityDir)/1.7/Assemblies/RimUnity.Core.dll" Private="false" />
    <Reference Include="RimUnity" HintPath="$(RimUnityDir)/1.7/Assemblies/RimUnity.dll" Private="false" />
    <Reference Include="0Harmony" HintPath="$(HarmonyPath)" Private="false" />
    <Reference Include="SYS" HintPath="$(RatkinAssemblyPath)" Private="false" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include="StatWorker_MeleeAveragePatches.cs" />
    <Compile Include="Properties/AssemblyInfo.cs" />
    <Compile Include="Weapon/**/*.cs" />
    <Compile Include="Abilities/**/*.cs" />
    <Compile Include="Hediff/**/*.cs" />
    <Compile Include="Combat/**/*.cs" />
    <Compile Include="SpecialCharacter/**/*.cs" />
  </ItemGroup>
  <Target Name="ValidateMigrationReferences" BeforeTargets="ResolveReferences">
    <Error Condition="!Exists('$(RimWorldDir)/Waylith.dll')" Text="RimWorldDir 必须指向本机 RimWorld 1.7 根目录。" />
    <Error Condition="!Exists('$(RimUnityDir)/1.7/Assemblies/RimUnity.dll')" Text="RimUnityDir 必须指向配套 RimUnity 模组根目录。" />
    <Error Condition="'$(HarmonyPath)' == '' Or !Exists('$(HarmonyPath)')" Text="请手动通过 HarmonyPath 配置 1.7 的 0Harmony.dll。" />
    <Error Condition="'$(RatkinAssemblyPath)' == '' Or !Exists('$(RatkinAssemblyPath)')" Text="请手动通过 RatkinAssemblyPath 配置 1.7 的 SYS.dll。" />
  </Target>
</Project>
'@
[IO.File]::WriteAllText((Join-Path $modRoot '1.7\Source\ClassLibrary1\ClassLibrary1.csproj'), $project17, $utf8)

# 运行文件按游戏版本隔离，旧程序集和二进制 AB 仅进入 1.6。
New-Item -ItemType Directory -Path (Join-Path $modRoot '1.7\Assemblies') | Out-Null
foreach ($folder in $runtimeFolders) {
    if ($folder -in @('Textures', 'Sounds')) { continue }
    Move-Item -LiteralPath (Join-Path $modRoot $folder) -Destination (Join-Path $modRoot ('1.6\' + $folder))
    if ($folder -notin @('Assemblies', 'AssetBundles')) {
        Copy-Item -LiteralPath (Join-Path $modRoot ('1.6\' + $folder)) -Destination (Join-Path $modRoot ('1.7\' + $folder)) -Recurse
    }
}
$loadFolders = @'
<?xml version="1.0" encoding="utf-8"?>
<loadFolders>
  <v1.6><li>/</li><li>1.6</li></v1.6>
  <v1.7><li>/</li><li>1.7</li></v1.7>
</loadFolders>
'@
[IO.File]::WriteAllText((Join-Path $modRoot 'LoadFolders.xml'), $loadFolders, $utf8)

# 1.6 自测仍以 1.6 内容目录为工作目录，源码引用使用独立版本路径。
$verificationRoot = Join-Path $modRoot '1.6\Source\Verification'
foreach ($file in Get-ChildItem -LiteralPath $verificationRoot -File | Where-Object { $_.Extension -in @('.ps1', '.cs') }) {
    $text = [IO.File]::ReadAllText($file.FullName, [Text.Encoding]::UTF8)
    $text = $text.Replace("Get-Content -LiteralPath 'pawnslots.txt'", "Get-Content -LiteralPath '..\pawnslots.txt'")
    $text = $text.Replace("('Textures\' +", "('..\Textures\' +")
    $text = $text.Replace('..\Ratkin', '..\..\Ratkin').Replace('../Ratkin', '../../Ratkin')
    $text = $text.Replace('..\本机源码', '..\..\本机源码').Replace('../本机源码', '../../本机源码')
    [IO.File]::WriteAllText($file.FullName, $text, $utf8)
}
foreach ($document in Get-ChildItem -LiteralPath (Join-Path $modRoot 'Source') -File -Force) {
    Copy-Item -LiteralPath $document.FullName -Destination (Join-Path $modRoot ('1.7\Source\' + $document.Name))
    Move-Item -LiteralPath $document.FullName -Destination (Join-Path $modRoot ('1.6\Source\' + $document.Name))
}
Remove-Item -LiteralPath (Join-Path $modRoot 'Source')

# 新 Unity 工程单独接入本地 UPM 源码包。
New-Item -ItemType Directory -Path $unity17 | Out-Null
foreach ($folder in $unityFolders) {
    Copy-Item -LiteralPath (Join-Path $unity16 $folder) -Destination (Join-Path $unity17 $folder) -Recurse
}
$manifestPath = Join-Path $unity17 'Packages\manifest.json'
$manifest = [IO.File]::ReadAllText($manifestPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
$manifest.dependencies | Add-Member -NotePropertyName 'com.chezhou.rimunity' -NotePropertyValue 'file:D:/UnityTest/RimUnity_c0e241a/UnityPackage'
[IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 8), $utf8)
$profile = [ordered]@{
    repository = (Split-Path -Path $modRoot -Parent).Replace('\', '/')
    shaderTool = 'D:/UnityTest/RimUnity_c0e241a/Tools/bin/ShaderTool/RimUnity.ShaderTool.exe'
    gameRoot = 'D:/Steam/steamapps/common/RimWorld'
    auditOnReload = $false
    disableCastShadows = $false
    disableReceiveShadows = $false
    disableLightProbes = $false
    disableReflectionProbes = $false
    mappings = @(@{ bundle = 'trk_bowofdeath_win'; mod = '鼠鼠收尾人们目前已到245679协会' })
    resourceOwners = @()
}
[IO.File]::WriteAllText((Join-Path $unity17 'ProjectSettings\RimUnity.json'), ($profile | ConvertTo-Json -Depth 8), $utf8)
$builder17 = @'
using UnityEditor;

//按工程保存的 RimUnity 配置导出 1.7 明文资源。
public static class BowOfDeathBundleBuilder
{
    [MenuItem("死之弓/导出 1.7 RimUnity 明文资源")]
    public static void BuildAndDeploy()
    {
        RimUnity.Editor.RimUnityWorkflow.ExportAll();
    }
}
'@
[IO.File]::WriteAllText((Join-Path $unity17 'Assets\Editor\BowOfDeathBundleBuilder.cs'), $builder17, $utf8)
foreach ($name in @('BowOfDeathPreviewWindow.cs', 'BowOfDeathVerification.cs')) {
    $path = Join-Path $unity17 ('Assets\Editor\' + $name)
    $text = [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)
    $text = $text.Replace('/1.6/Defs/', '/1.7/Defs/')
    $text = $text.Replace('D:/UnityTest/unity_trk_35/', 'D:/UnityTest/unity_trk_35_1.7/')
    $text = $text.Replace('D:\UnityTest\unity_trk_35 ', 'D:\UnityTest\unity_trk_35_1.7 ')
    [IO.File]::WriteAllText($path, $text, $utf8)
}
'版本目录分隔和新 Unity 工程配置完成；尚未执行 Unity 导入与明文导出。'
