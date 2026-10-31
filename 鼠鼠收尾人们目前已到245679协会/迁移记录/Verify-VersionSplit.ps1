$ErrorActionPreference = 'Stop'
$modRoot = Split-Path -Path $PSScriptRoot -Parent
$before = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Before.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$checks = [Collections.Generic.List[object]]::new()
$failures = [Collections.Generic.List[string]]::new()

function Check([string]$name, [bool]$passed, [string]$detail) {
    $checks.Add([pscustomobject]@{ name = $name; passed = $passed; detail = $detail })
    if (!$passed) { $failures.Add($name + '：' + $detail) }
}
function Same-File([string]$path, [string]$hash) {
    (Test-Path -LiteralPath $path -PathType Leaf) -and ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -eq $hash)
}

$shared = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SharedAssetsUpdate.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$runtime16Errors = @($before.runtime | Where-Object {
    $relative = if ($_.path -match '^(Textures|Sounds)/') { $_.path } else { '1.6/' + $_.path }
    !(Same-File (Join-Path $modRoot $relative) $_.sha256)
})
Check '1.6 原运行文件逐项 SHA256' ($runtime16Errors.Count -eq 0) ($before.runtime.Count.ToString() + ' 个文件；通用资源从根目录核对；差异：' + ($runtime16Errors.path -join ', '))
$sharedErrors = @($shared.files | Where-Object { !(Same-File (Join-Path $modRoot $_.path) $_.sha256) })
$actualSharedCount = @(foreach ($folder in @('Textures', 'Sounds')) { Get-ChildItem -LiteralPath (Join-Path $modRoot $folder) -File -Recurse -Force }).Count
Check '通用贴图与音效在根目录且内容不变' ($shared.modRoot -eq $modRoot -and $shared.files.Count -eq 145 -and $actualSharedCount -eq 145 -and $sharedErrors.Count -eq 0) ($shared.files.Count.ToString() + ' 个文件；差异：' + ($sharedErrors.path -join ', '))
$duplicatedShared = @(foreach ($version in @('1.6', '1.7')) { foreach ($folder in @('Textures', 'Sounds')) {
    $path = Join-Path $modRoot ($version + '/' + $folder)
    if (Test-Path -LiteralPath $path) { $path }
}})
Check '两个版本目录无重复通用资源' ($duplicatedShared.Count -eq 0) ($duplicatedShared -join ', ')
$content17 = @($before.runtime | Where-Object { $_.path -notmatch '^(Assemblies|AssetBundles|Textures|Sounds)/' -and $_.path -ne 'Defs/ShaderTypeDefs/BowOfDeath_Shaders.xml' })
$runtime17Errors = @($content17 | Where-Object { !(Same-File (Join-Path $modRoot ('1.7/' + $_.path)) $_.sha256) })
Check '1.7 基础资源独立复制' ($runtime17Errors.Count -eq 0) ($content17.Count.ToString() + ' 个文件；差异：' + ($runtime17Errors.path -join ', '))
$sourceFiles = @($before.source | Where-Object { $_.path -match '^(ClassLibrary1|WeaponAbility)/.*\.cs$' -and $_.path -notmatch '/(obj|bin|\.vs)/' })
foreach ($version in @('1.6', '1.7')) {
    $errors = @($sourceFiles | Where-Object { !(Same-File (Join-Path $modRoot ($version + '/Source/' + $_.path)) $_.sha256) })
    Check ($version + ' 业务 C# 与原始内容一致') ($errors.Count -eq 0) ($sourceFiles.Count.ToString() + ' 个文件；差异：' + ($errors.path -join ', '))
}
$source17Root = Join-Path $modRoot '1.7/Source'
$actualSource17 = @(foreach ($folder in @('ClassLibrary1', 'WeaponAbility')) {
    Get-ChildItem -LiteralPath (Join-Path $source17Root $folder) -Filter '*.cs' -File -Recurse | Where-Object { $_.FullName -notmatch '\\(obj|bin|\.vs)\\' } |
        ForEach-Object { $_.FullName.Substring($source17Root.Length + 1).Replace('\', '/') }
})
$expectedNewSource = @('ClassLibrary1/Weapon/BowOfDeath/DeathBowRimUnityShaders.cs', 'ClassLibrary1/Weapon/BowOfDeath/DeathBowRimUnityShaderPatch.cs')
$actualNewSource = @($actualSource17 | Where-Object { $_ -notin $sourceFiles.path })
Check '1.7 新增业务代码仅两个 Shader 接入文件' (@(Compare-Object $expectedNewSource $actualNewSource).Count -eq 0) ($actualNewSource -join ', ')
Check '两个版本各自包含 Source 和 Defs' ((Test-Path -LiteralPath (Join-Path $modRoot '1.6/Source/ClassLibrary1/ClassLibrary1.csproj')) -and (Test-Path -LiteralPath (Join-Path $modRoot '1.7/Source/ClassLibrary1/ClassLibrary1.csproj')) -and (Test-Path -LiteralPath (Join-Path $modRoot '1.6/Defs')) -and (Test-Path -LiteralPath (Join-Path $modRoot '1.7/Defs'))) '1.6/Source、1.6/Defs、1.7/Source、1.7/Defs'
$routing = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'UnityRoutingUpdate.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$routing16 = @($routing.changes | Where-Object version -eq '1.6')
$unityErrors = @($before.unity | Where-Object {
    $record = $_
    $approved = @($routing16 | Where-Object path -eq $record.path)
    if ($approved.Count -eq 0) { !(Same-File (Join-Path $before.unity16 $record.path) $record.sha256) }
    else {
        $sharedChange = @($shared.unityChanges | Where-Object { $_.version -eq '1.6' -and $_.path -eq $record.path })
        $expectedHash = if ($sharedChange.Count -eq 1) { $sharedChange[0].afterSHA256 } else { $approved[0].afterSHA256 }
        $approved.Count -ne 1 -or $approved[0].beforeSHA256 -ne $record.sha256 -or ($sharedChange.Count -eq 1 -and $sharedChange[0].beforeSHA256 -ne $approved[0].afterSHA256) -or !(Same-File (Join-Path $before.unity16 $record.path) $expectedHash)
    }
})
$originalUnityFiles = @(foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) { Get-ChildItem -LiteralPath (Join-Path $before.unity16 $folder) -File -Recurse -Force })
Check '1.6 Unity 仅保留获准的路径修改' ($unityErrors.Count -eq 0 -and $originalUnityFiles.Count -eq $before.unity.Count -and $routing16.Count -eq 3) ($before.unity.Count.ToString() + ' 个文件；三个 Editor 脚本仅改路径，其余按原始哈希核对；差异：' + ($unityErrors.path -join ', '))
$routingErrors = @($routing.changes | Where-Object {
    $record = $_
    $sharedChange = @($shared.unityChanges | Where-Object { $_.version -eq $record.version -and $_.path -eq $record.path })
    if ($sharedChange.Count -eq 1) { $sharedChange[0].beforeSHA256 -ne $record.afterSHA256 -or !(Same-File (Join-Path $record.root $record.path) $sharedChange[0].afterSHA256) }
    else { !(Same-File (Join-Path $record.root $record.path) $record.afterSHA256) }
})
Check '两套 Unity 当前路径文件与修正记录一致' ($routing.modRoot -eq $modRoot -and $routingErrors.Count -eq 0) ($routingErrors.path -join ', ')
$shaderFiles = @($before.unity | Where-Object { $_.path -like '*.shader' })
$shaderErrors = @($shaderFiles | Where-Object { !(Same-File (Join-Path $before.unity17 $_.path) $_.sha256) })
Check '1.7 Unity 的 Shader 源码未改公式或参数' ($shaderErrors.Count -eq 0) ($shaderFiles.Count.ToString() + ' 个源文件；差异：' + ($shaderErrors.path -join ', '))
[xml]$about = Get-Content -LiteralPath (Join-Path $modRoot 'About/About.xml') -Raw -Encoding UTF8
$dependencies16 = @($about.ModMetaData.modDependenciesByVersion.'v1.6'.li.packageId)
$dependencies17 = @($about.ModMetaData.modDependenciesByVersion.'v1.7'.li.packageId)
Check 'About 双版支持和分版依赖' ((@($about.ModMetaData.supportedVersions.li) -join '|') -eq '1.6|1.7' -and ($dependencies16 -join '|') -eq 'brrainz.harmony|Solaris.RatkinRaceMod' -and ($dependencies17 -join '|') -eq 'brrainz.harmony|Solaris.RatkinRaceMod|Chezhou.RimUnity' -and 'Chezhou.RimUnity' -in @($about.ModMetaData.loadAfter.li)) '1.6 保留原前置；1.7 增加 RimUnity 依赖和加载顺序。'

[xml]$loadFolders = Get-Content -LiteralPath (Join-Path $modRoot 'LoadFolders.xml') -Raw -Encoding UTF8
Check 'LoadFolders 根目录共享与版本分隔' ((@($loadFolders.loadFolders.'v1.6'.li) -join '|') -eq '/|1.6' -and (@($loadFolders.loadFolders.'v1.7'.li) -join '|') -eq '/|1.7' -and @($loadFolders.SelectNodes('/loadFolders/*')).Count -eq 2) '每版加载根目录共享资源与自己的版本数据。'
$remainingRootFolders = @('Assemblies', 'AssetBundles', 'Defs', 'Languages', 'Patches', 'Source') | Where-Object { Test-Path -LiteralPath (Join-Path $modRoot $_) }
Check '根目录无旧加载内容或旧源码入口' (@($remainingRootFolders).Count -eq 0) ($remainingRootFolders -join ', ')
$runtimeBinaries17 = @(Get-ChildItem -LiteralPath (Join-Path $modRoot '1.7/Assemblies') -File)
Check '1.7 仅输出自身业务产物且无旧 AB' (@(Compare-Object @('ClassLibrary1.dll', 'ClassLibrary1.pdb', 'ClassLibrary1.deps.json') @($runtimeBinaries17.Name)).Count -eq 0 -and !(Test-Path -LiteralPath (Join-Path $modRoot '1.7/AssetBundles'))) ($runtimeBinaries17.Name -join ', ')
$oldCaches17 = @(Get-ChildItem -LiteralPath (Join-Path $modRoot '1.7/Source') -Directory -Recurse -Force | Where-Object { $_.Name -eq '.vs' -or $_.Name -like 'net4*' })
Check '1.7 未混入旧框架构建缓存' ($oldCaches17.Count -eq 0) ($oldCaches17.FullName -join ', ')

$project16Path = Join-Path $modRoot '1.6/Source/ClassLibrary1/ClassLibrary1.csproj'
$project17Path = Join-Path $modRoot '1.7/Source/ClassLibrary1/ClassLibrary1.csproj'
[xml]$project16 = Get-Content -LiteralPath $project16Path -Raw -Encoding UTF8
[xml]$project17 = Get-Content -LiteralPath $project17Path -Raw -Encoding UTF8
Check '1.6 目标框架' ($project16.SelectSingleNode('//*[local-name()="TargetFrameworkVersion"]').InnerText -eq 'v4.7.2') '.NET Framework 4.7.2'
Check '1.7 目标框架' ($project17.Project.PropertyGroup.TargetFramework -eq 'net10.0') '.NET 10'
$outputs16 = @($project16.SelectNodes('//*[local-name()="OutputPath"]') | ForEach-Object { [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($project16Path)) $_.InnerText)) })
$output17 = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($project17Path)) $project17.Project.PropertyGroup.OutputPath))
Check '两套程序集输出不交叉' (@($outputs16 | Where-Object { $_.TrimEnd('\') -ne (Join-Path $modRoot '1.6\Assemblies') }).Count -eq 0 -and $output17.TrimEnd('\') -eq (Join-Path $modRoot '1.7\Assemblies')) (($outputs16 -join ', ') + '; ' + $output17)
$references17 = @($project17.Project.ItemGroup.Reference | Where-Object { $_ })
Check '1.7 外部程序集不随业务输出复制' (@($references17 | Where-Object { $_.Private -ne 'false' }).Count -eq 0) ($references17.Count.ToString() + ' 项 Private=false；默认路径对准实际加载依赖，可通过属性覆盖。')

$profile = Get-Content -LiteralPath (Join-Path $before.unity17 'ProjectSettings/RimUnity.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$configuredMod = [IO.Path]::GetFullPath((Join-Path $profile.repository $profile.mappings[0].mod))
Check 'RimUnity 输出归属与目标游戏' ($configuredMod -eq $modRoot -and $profile.gameRoot -eq 'D:/Steam/steamapps/common/RimWorld' -and $profile.mappings[0].bundle -eq 'trk_bowofdeath_win') ($configuredMod + '; ' + $profile.gameRoot)
$manifest = Get-Content -LiteralPath (Join-Path $before.unity17 'Packages/manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Check 'UPM 使用固定仓库的本地源码包' ($manifest.dependencies.'com.chezhou.rimunity' -eq 'file:D:/UnityTest/RimUnity_c0e241a/UnityPackage') $manifest.dependencies.'com.chezhou.rimunity'
$newEditorFiles = @(Get-ChildItem -LiteralPath (Join-Path $before.unity17 'Assets/Editor') -Filter '*.cs' -File)
$wrongTargets = @($newEditorFiles | Select-String -Pattern 'Desktop/鼠鼠收尾人们目前已到245679协会|unity_trk_35/|BuildAssetBundles')
Check '新 Unity Editor 不指向桌面副本或二进制打包' ($wrongTargets.Count -eq 0) (($wrongTargets | ForEach-Object { $_.Path + ':' + $_.LineNumber }) -join ', ')
$workspaceForward = $modRoot.Replace('\', '/')
foreach ($version in @('1.6', '1.7')) {
    $unityRoot = if ($version -eq '1.6') { $before.unity16 } else { $before.unity17 }
    $previewText = [IO.File]::ReadAllText((Join-Path $unityRoot 'Assets/Editor/BowOfDeathPreviewWindow.cs'), [Text.Encoding]::UTF8)
    $verifyText = [IO.File]::ReadAllText((Join-Path $unityRoot 'Assets/Editor/BowOfDeathVerification.cs'), [Text.Encoding]::UTF8)
    $previewRoot = [regex]::Match($previewText, 'private const string ModRoot = "([^"]+)";').Groups[1].Value
    $previewXmlSuffix = [regex]::Match($previewText, 'private const string XmlPath = ModRoot \+ "([^"]+)";').Groups[1].Value
    $verifyParent = [regex]::Match($verifyText, 'private const string Root = "([^"]+)";').Groups[1].Value
    $verifySuffix = [regex]::Match($verifyText, 'private const string ModRoot = Root \+ "([^"]+)";').Groups[1].Value
    $verifyXmlSuffix = [regex]::Match($verifyText, 'private const string XmlPath = ModRoot \+ "([^"]+)";').Groups[1].Value
    $expectedXml = '/' + $version + '/Defs/Misc/Mote_BowOfDeath.xml'
    Check ($version + ' Unity 参数分版且贴图读取根目录') ($previewRoot -eq $workspaceForward -and $verifyParent + $verifySuffix -eq $workspaceForward -and $previewXmlSuffix -eq $expectedXml -and $verifyXmlSuffix -eq $expectedXml -and (Test-Path -LiteralPath ($workspaceForward + $expectedXml)) -and (Test-Path -LiteralPath (Join-Path $modRoot 'Textures/VFX/Bow_of_Death_Aim_Charge_Start_Vfx.png'))) ($workspaceForward + $expectedXml + '; 贴图：ModRoot/Textures')
}
$builder16Text = [IO.File]::ReadAllText((Join-Path $before.unity16 'Assets/Editor/BowOfDeathBundleBuilder.cs'), [Text.Encoding]::UTF8)
$builder16Literal = [regex]::Match($builder16Text, 'public const string ModBundleDir = "([^"]+)";').Groups[1].Value
$builder16Destination = $builder16Literal.Replace('\\', '\')
Check '1.6 二进制打包只部署到工作区 1.6/AssetBundles' ($builder16Destination -eq (Join-Path $modRoot '1.6/AssetBundles') -and (Test-Path -LiteralPath $builder16Destination)) $builder16Destination

$exportReportPath = Join-Path $before.unity17 'Library/RimUnity/Reports/export.json'
$exportReport = Get-Content -LiteralPath $exportReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
$expectedShaderPaths = @('Assets/Data/Trk.Nine/Materials/Trk/BowOfDeathAimCharge.shader', 'Assets/Data/Trk.Nine/Materials/Trk/BowOfDeathAimWind.shader')
$entryDifferences = @(Compare-Object $expectedShaderPaths @($exportReport.entries.path))
Check '官方导出报告两个 Shader 全部成功' ($exportReport.entries.Count -eq 2 -and @($exportReport.entries | Where-Object { $_.status -ne 'exported' -or $_.error }).Count -eq 0 -and $entryDifferences.Count -eq 0) $exportReportPath
$dataRoot = Join-Path $modRoot '1.7/Data/RimUnity'
$catalog = Get-Content -LiteralPath (Join-Path $dataRoot 'catalog.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Check '资源清单版本和 owner' ($catalog.formatVersion -eq 2 -and $catalog.owner -ceq 'Trk.Nine' -and $catalog.assets.Count -eq 2) ('formatVersion=' + $catalog.formatVersion + '; owner=' + $catalog.owner)
[xml]$shaderDefinitions = Get-Content -LiteralPath (Join-Path $modRoot '1.7/Defs/ShaderTypeDefs/BowOfDeath_Shaders.xml') -Raw -Encoding UTF8
$expectedShaderNames = @{ Trk_BowOfDeathAimWind = 'Trk/BowOfDeathAimWind'; Trk_BowOfDeathAimCharge = 'Trk/BowOfDeathAimCharge' }
$shaderDefinitionErrors = @($shaderDefinitions.Defs.ShaderTypeDef | Where-Object {
    $definition = $_
    $matches = @($catalog.assets | Where-Object { $_.path -ceq $definition.shaderPath -and $_.name -ceq $expectedShaderNames[$definition.defName] })
    $matches.Count -ne 1
})
Check '1.7 ShaderDef 与实际导出清单一致' ($shaderDefinitions.Defs.ShaderTypeDef.Count -eq 2 -and $shaderDefinitionErrors.Count -eq 0) '两个 Def 使用对应 Shader 的真实逻辑路径。'
$resourceErrors = [Collections.Generic.List[string]]::new()
$glslFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($asset in $catalog.assets) {
    $assetFile = [IO.Path]::GetFullPath((Join-Path $dataRoot $asset.file))
    if (!$assetFile.StartsWith($dataRoot + '\') -or !(Test-Path -LiteralPath $assetFile -PathType Leaf)) { $resourceErrors.Add($asset.file); continue }
    $shader = Get-Content -LiteralPath $assetFile -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($property in @('vertexFile', 'fragmentFile', 'descriptorFile')) {
        $relative = $shader.$property
        if (!$relative) { $resourceErrors.Add($asset.path + ':' + $property); continue }
        $file = [IO.Path]::GetFullPath((Join-Path $dataRoot $relative))
        if (!$file.StartsWith($dataRoot + '\') -or !(Test-Path -LiteralPath $file -PathType Leaf)) { $resourceErrors.Add($relative) }
        if ($property -in @('vertexFile', 'fragmentFile')) { [void]$glslFiles.Add($file) }
    }
}
Check 'catalog 的 Shader 描述及 GLSL 引用完整' ($resourceErrors.Count -eq 0) ($resourceErrors -join ', ')

$syntaxErrors = [Collections.Generic.List[string]]::new()
foreach ($file in Get-ChildItem -LiteralPath $modRoot -Filter '*.ps1' -Recurse -File) {
    $tokens = $null
    $parseErrors = $null
    [void][Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$parseErrors)
    foreach ($error in $parseErrors) { $syntaxErrors.Add($file.FullName + ':' + $error.Message) }
}
Check '迁移与原自测 PowerShell 语法' ($syntaxErrors.Count -eq 0) ($syntaxErrors -join ', ')
$unityNotePath = Join-Path $before.unity17 'Assets/RimUnity迁移说明.txt'
$unityNote = Get-Content -LiteralPath $unityNotePath -Raw -Encoding UTF8
Check 'Unity Assets 内有实际路径说明' ($unityNote.Contains($modRoot) -and $unityNote.Contains('D:\UnityTest\RimUnity_c0e241a\UnityPackage') -and (Test-Path -LiteralPath ($unityNotePath + '.meta'))) $unityNotePath
$gameLoaderEvidence = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SharedAssets-GameLoader.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Check '实际 1.7 加载器与文件索引核验' ($gameLoaderEvidence.passed -and $gameLoaderEvidence.passedCalls -eq 8 -and (Same-File (Join-Path $modRoot $gameLoaderEvidence.programSource) $gameLoaderEvidence.programSHA256) -and (Same-File (Join-Path $modRoot 'LoadFolders.xml') $gameLoaderEvidence.loadFoldersSHA256) -and (Same-File $gameLoaderEvidence.gameAssembly $gameLoaderEvidence.gameAssemblySHA256)) $gameLoaderEvidence.scope
$originalComparison = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Original16-Comparison.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Check '与用户指定原 1.6 备份核对记录一致' ($originalComparison.runtimeFiles -eq 207 -and $originalComparison.businessSourceFiles -eq 34 -and $originalComparison.runtimeDifferences.Count -eq 0 -and $originalComparison.businessSourceDifferences.Count -eq 0) '207 个原运行文件及 34 个业务源文件内容一致。'
$compileEvidence = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ShaderBridge-Compile.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Check 'RimUnity 加载接口独立 .NET 10 编译证据' ($compileEvidence.exitCode -eq 0 -and (Same-File (Join-Path $modRoot $compileEvidence.source) $compileEvidence.sourceSHA256) -and (Same-File (Join-Path $modRoot $compileEvidence.output) $compileEvidence.outputSHA256)) $compileEvidence.limitation
$buildEvidence = Get-Content -LiteralPath (Join-Path $PSScriptRoot '运行验证/Build.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$buildFileErrors = @($buildEvidence.files | Where-Object { !(Same-File (Join-Path $modRoot $_.path) $_.sha256) })
$buildReferenceErrors = @($buildEvidence.references | Where-Object { !(Same-File $_.path $_.sha256) })
Check '1.7 完整 .NET 10 构建与当前源码依赖一致' ($buildEvidence.exitCode -eq 0 -and $buildEvidence.targetFramework -eq '.NETCoreApp,Version=v10.0' -and $buildFileErrors.Count -eq 0 -and $buildReferenceErrors.Count -eq 0) '完整业务及 Harmony Prefix 编译成功；Ratkin 的 SYS 来自其 1.7 加载清单选取的 1.6 目录，不宣称前置 DLL 已纯 1.7 化。'
$syncEvidence = Get-Content -LiteralPath (Join-Path $PSScriptRoot '运行验证/RuntimeSync.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$syncErrors = @($syncEvidence.files | Where-Object { !(Same-File (Join-Path $modRoot $_.path) $_.sourceSHA256) -or !(Same-File (Join-Path $syncEvidence.target $_.path) $_.sourceSHA256) })
Check '工作区与游戏内本 Mod 运行文件同步' ($syncEvidence.passed -and $syncErrors.Count -eq 0) ($syncEvidence.fileCount.ToString() + ' 个文件逐项 SHA256 一致。')
$startupEvidence = Get-Content -LiteralPath (Join-Path $PSScriptRoot '运行验证/EarlyShaderPatch/After.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Check '1.7 实际材质初始化无默认 Shader 回退' ($startupEvidence.passed -and $startupEvidence.fallbacks.Count -eq 0 -and $startupEvidence.prefixStackCount -ge 4 -and (Same-File $startupEvidence.log $startupEvidence.logSHA256) -and (Same-File $startupEvidence.dll $startupEvidence.dllSHA256) -and (Same-File (Join-Path $modRoot '1.7/Assemblies/ClassLibrary1.dll') $startupEvidence.dllSHA256)) '修复后完整启动：Wind/Charge 默认回退零条；Def 图形初始化经 Prefix 加载 RimUnity Shader。画面仍需实测。'

if ($resourceErrors.Count -eq 0 -and $glslFiles.Count -gt 0) {
    $arguments = @($profile.gameRoot, 'verify') + @($glslFiles)
    $verificationOutput = & $profile.shaderTool @arguments 2>&1
    $shaderExitCode = $LASTEXITCODE
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'GLSL-Verify.log'), ($verificationOutput -join [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    Check '目标游戏 ShaderTool 复验实际导出 GLSL' ($shaderExitCode -eq 0) ($glslFiles.Count.ToString() + ' 个唯一程序文件；exit=' + $shaderExitCode)
}

$result = [ordered]@{
    checked = [DateTimeOffset]::Now.ToString('o')
    passed = $failures.Count -eq 0
    checks = @($checks.ToArray())
    notExecuted = @('1.6 原运行 DLL 未替换；仅重定向到运行验证/Build16 进行了 net472 构建核验。', '依用户最新指示，特效画面由用户检查。Ratkin 1.7 目录未提供 SYS.CompSheath，本 Mod 1.7 引用其加载清单共享的 SYS.dll，不能宣称仅引用 Ratkin 1.7 目录。')
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'Verification.json'), ($result | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
foreach ($check in $checks) { Write-Output ((@{ $true = 'PASS'; $false = 'FAIL' }[$check.passed]) + ' ' + $check.name + ' ' + $check.detail) }
if ($failures.Count -gt 0) { throw ($failures -join [Environment]::NewLine) }
