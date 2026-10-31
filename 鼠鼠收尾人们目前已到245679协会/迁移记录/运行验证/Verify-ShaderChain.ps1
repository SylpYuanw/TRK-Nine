$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$gameModRoot = 'D:\Steam\steamapps\common\RimWorld\Mods\鼠鼠收尾人们目前已到245679协会'
$dataRoot = Join-Path $modRoot '1.7/Data/RimUnity'
$auditRoot = Join-Path $PSScriptRoot 'ShaderChainAudit'
$utf8 = [Text.UTF8Encoding]::new($false,$true)
$catalog = Get-Content -LiteralPath (Join-Path $dataRoot 'catalog.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$used = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
[void]$used.Add('catalog.json')
$files = @(Get-ChildItem -LiteralPath $dataRoot -Recurse -File)
$records = @(foreach ($file in $files) {
    $relative = $file.FullName.Substring($dataRoot.Length+1).Replace('\','/')
    $decoded = $utf8.GetString([IO.File]::ReadAllBytes($file.FullName))
    if ($file.Extension -eq '.json') { $null = $decoded | ConvertFrom-Json }
    $hash = (Get-FileHash -LiteralPath $file.FullName).Hash
    $gameFile = Join-Path $gameModRoot ('1.7/Data/RimUnity/'+$relative)
    $gameHash = (Get-FileHash -LiteralPath $gameFile).Hash
    if ($hash -ne $gameHash) { throw ('游戏内资源不一致：'+$relative) }
    [pscustomobject]@{path=$relative;bytes=$file.Length;sha256=$hash;gameSHA256=$gameHash;strictUTF8=$true}
})
$chains = @(foreach ($asset in $catalog.assets) {
    [void]$used.Add($asset.file.Replace('/','\'))
    $shader = Get-Content -LiteralPath (Join-Path $dataRoot $asset.file) -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($key in @('vertexFile','fragmentFile','descriptorFile')) {
        if (!(Test-Path -LiteralPath (Join-Path $dataRoot $shader.$key))) { throw ('缺少依赖：'+$shader.$key) }
        [void]$used.Add($shader.$key.Replace('/','\'))
    }
    [pscustomobject]@{name=$asset.name;logicalPath=$asset.path;shaderData=$asset.file;vertexFile=$shader.vertexFile;fragmentFile=$shader.fragmentFile;descriptorFile=$shader.descriptorFile;approximations=$shader.approximations}
})
$unused = @($files | Where-Object { !$used.Contains($_.FullName.Substring($dataRoot.Length+1)) })
if ($unused.Count -ne 0 -or $files.Count -ne 9) { throw '导出文件集合与已核对的引用图不一致。' }
$expectedShaderDefs = @{Trk_BowOfDeathAimWind='Trk/BowOfDeathAimWind';Trk_BowOfDeathAimCharge='Trk/BowOfDeathAimCharge'}
$expectedMotes = @{BowOfDeath_AimVfx='Trk_BowOfDeathAimWind';BowOfDeath_AimChargeVfx='Trk_BowOfDeathAimCharge';BowOfDeath_StageVfx='MoteGlow';BowOfDeath_AimStreakVfx='MoteGlow'}
$relativeFiles = @('LoadFolders.xml','1.7/Defs/ShaderTypeDefs/BowOfDeath_Shaders.xml','1.7/Defs/Misc/Mote_BowOfDeath.xml','1.7/Assemblies/ClassLibrary1.dll')
foreach ($relative in $relativeFiles) {
    if ((Get-FileHash -LiteralPath (Join-Path $modRoot $relative)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $gameModRoot $relative)).Hash) { throw ('实际部署不同步：'+$relative) }
}
[xml]$defs = Get-Content -LiteralPath (Join-Path $gameModRoot '1.7/Defs/ShaderTypeDefs/BowOfDeath_Shaders.xml') -Raw -Encoding UTF8
foreach ($def in $defs.Defs.ShaderTypeDef) {
    $matches = @($catalog.assets | Where-Object { $_.name -ceq $expectedShaderDefs[$def.defName] -and $_.path -ceq $def.shaderPath })
    if ($matches.Count -ne 1) { throw ('ShaderDef 未唯一指向 RimUnity 清单：'+$def.defName) }
}
[xml]$motes = Get-Content -LiteralPath (Join-Path $gameModRoot '1.7/Defs/Misc/Mote_BowOfDeath.xml') -Raw -Encoding UTF8
foreach ($entry in $expectedMotes.GetEnumerator()) {
    $mote = @($motes.Defs.ThingDef | Where-Object defName -ceq $entry.Key)
    if ($mote.Count -ne 1 -or $mote[0].graphicData.shaderType -cne $entry.Value) { throw ('Mote Shader 引用不匹配：'+$entry.Key) }
}
[xml]$folders = Get-Content -LiteralPath (Join-Path $gameModRoot 'LoadFolders.xml') -Raw -Encoding UTF8
if ((@($folders.loadFolders.'v1.7'.li)-join '|') -ne '/|1.7') { throw '1.7 加载目录混入其他版本。' }
foreach ($relative in @('Assemblies','AssetBundles','1.7/AssetBundles')) {
    if (Test-Path -LiteralPath (Join-Path $gameModRoot $relative)) { throw ('发现旧加载入口：'+$relative) }
}
$il = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ModShaderChain-IL.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$build = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Build.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$builtDll = @($build.files | Where-Object path -ceq '1.7/Assemblies/ClassLibrary1.dll')[0]
$actualDllHash = (Get-FileHash -LiteralPath (Join-Path $gameModRoot '1.7/Assemblies/ClassLibrary1.dll')).Hash
if (!$il.passed -or $il.sha256 -ne $actualDllHash -or $builtDll.sha256 -ne $actualDllHash -or $il.targetFramework -ne '.NETCoreApp,Version=v10.0') { throw '部署 DLL 与 net10 构建/二进制审计不一致。' }
foreach ($record in $build.files | Where-Object { $_.path -like '1.7/Source/*' }) {
    if ((Get-FileHash -LiteralPath (Join-Path $modRoot $record.path)).Hash -ne $record.sha256) { throw ('源码与完整构建记录不一致：'+$record.path) }
}
$converted = Get-Content -LiteralPath (Join-Path $auditRoot 'Source-to-GLSL.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if (!$converted.passed) { throw '源 Shader 与发布 GLSL 对照失败。' }
foreach ($shader in $converted.results) {
    if ((Get-FileHash -LiteralPath $shader.source).Hash -ne $shader.sourceSHA256) { throw 'Shader 源文件在离线对照后已改变。' }
    $originalSource = $shader.source.Replace('D:\UnityTest\unity_trk_35_1.7\', 'D:\UnityTest\unity_trk_35\')
    if ($originalSource -eq $shader.source -or (Get-FileHash -LiteralPath $originalSource).Hash -ne $shader.sourceSHA256) { throw '1.7 Shader 源文件与原 1.6 Unity Shader 内容不一致。' }
    foreach ($stage in $shader.stages) {
        if (!$stage.identical -or (Get-FileHash -LiteralPath $stage.published).Hash -ne $stage.publishedSHA256 -or (Get-FileHash -LiteralPath $stage.regenerated).Hash -ne $stage.regeneratedSHA256) { throw '源 Shader 转换产物与当前 GLSL 不符。' }
    }
}
$logPath = 'C:\Users\Administrator\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Logs\Player.log'
$warningLines = @(Select-String -LiteralPath $logPath -Pattern ' WARN .*\[RimUnity\] Trk/BowOfDeathAim(Charge|Wind)：(常量正整数幂|smoothstep)' | ForEach-Object { [pscustomobject]@{line=$_.LineNumber;text=$_.Line} })
$readyLines = @(Select-String -LiteralPath $logPath -Pattern '\[RimUnity\] Shader 准备结束：2 项，失败 0 项' | ForEach-Object { [pscustomobject]@{line=$_.LineNumber;text=$_.Line} })
$fallbackLines = @(Select-String -LiteralPath $logPath -Pattern "ShaderRegistry: shader '.*BowOfDeathAim(Wind|Charge).*not registered, falling back to default")
$prefixCalls = @(Select-String -LiteralPath $logPath -Pattern 'XIYUNTE.DeathBowRimUnityShaderPatch.Prefix')
$startupPassed = $fallbackLines.Count -eq 0 -and $prefixCalls.Count -ge 4 -and $readyLines.Count -gt 0
$result = [ordered]@{checked=[DateTimeOffset]::Now.ToString('o');passed=$startupPassed;resourceCount=$files.Count;resourceBytes=($files|Measure-Object Length -Sum).Sum;unusedCount=$unused.Count;resources=$records;chains=$chains;runtimeDLL=$il.dll;runtimeDLLSHA256=$actualDllHash;targetFramework=$il.targetFramework;earlyPatch=$il.earlyPatch;duplicateScan=$il.duplicateScan;loadFolders17=@($folders.loadFolders.'v1.7'.li);sourceToGLSLPassed=$true;fallbackCount=$fallbackLines.Count;prefixStackCount=$prefixCalls.Count;warningLines=$warningLines;readyLines=$readyLines;limitation='证明实际部署程序集、产物对应及启动材质加载无默认回退；不包含游戏内特效画面或 GPU 抓帧验收。'}
[IO.File]::WriteAllText((Join-Path $auditRoot 'Verification.json'), ($result | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
Write-Output ('PASS 实际部署 net10 DLL 与源码构建哈希一致；Shader 接入 IL、Def→catalog→GLSL、双版加载隔离、9 个发布资源同步及 4 个 GLSL 源文件重新转换对照均通过。')
Write-Output ('当前日志：'+$warningLines.Count+' 条兼容 Warning，Shader 准备成功记录 '+$readyLines.Count+' 条。')
if (!$startupPassed) { throw '实际启动日志仍存在 Shader 默认回退或未证明 Prefix 首次加载。' }
