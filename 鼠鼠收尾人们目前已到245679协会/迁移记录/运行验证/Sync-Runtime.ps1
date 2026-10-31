$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$targetRoot = 'D:\Steam\steamapps\common\RimWorld\Mods\鼠鼠收尾人们目前已到245679协会'
if (!(Test-Path -LiteralPath $targetRoot -PathType Container)) { throw '目标 Mod 目录不存在。' }
if (Get-Process -Name 'RimWorld*' -ErrorAction SilentlyContinue) { throw '游戏运行时禁止同步程序集。' }
$folders = @('About', 'Textures', 'Sounds', '1.6/Assemblies', '1.6/AssetBundles', '1.6/Defs', '1.6/Languages', '1.6/Patches', '1.7/Assemblies', '1.7/Defs', '1.7/Languages', '1.7/Patches', '1.7/Data')
$files = @(Get-Item -LiteralPath (Join-Path $modRoot 'LoadFolders.xml')) + @(foreach ($folder in $folders) {
    Get-ChildItem -LiteralPath (Join-Path $modRoot $folder) -File -Recurse
})
$manifest = @(foreach ($file in $files) {
    $relative = $file.FullName.Substring($modRoot.Length + 1).Replace('\', '/')
    $destination = [IO.Path]::GetFullPath((Join-Path $targetRoot $relative))
    if (!$destination.StartsWith($targetRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw '目标路径越界。' }
    $sourceHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    $beforeHash = if (Test-Path -LiteralPath $destination -PathType Leaf) { (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash } else { $null }
    [pscustomobject]@{ path=$relative; sourceSHA256=$sourceHash; beforeSHA256=$beforeHash; changed=$sourceHash -ne $beforeHash }
})
$targetOnly = @(foreach ($folder in $folders) {
    $destination = Join-Path $targetRoot $folder
    if (Test-Path -LiteralPath $destination) {
        Get-ChildItem -LiteralPath $destination -File -Recurse | ForEach-Object {
            $relative = $_.FullName.Substring($targetRoot.Length + 1).Replace('\', '/')
            if ($relative -notin $manifest.path) { $relative }
        }
    }
})
if ($targetOnly.Count -gt 0) { throw ('目标存在额外运行文件，先核对再处理：' + ($targetOnly -join ', ')) }
foreach ($record in $manifest) {
    $destination = Join-Path $targetRoot $record.path
    if ($record.changed) {
        [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))
        Copy-Item -LiteralPath (Join-Path $modRoot $record.path) -Destination $destination -Force
    }
    if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $record.sourceSHA256) { throw ('同步后哈希不一致：' + $record.path) }
}
$result = [ordered]@{ checked=[DateTimeOffset]::Now.ToString('o'); source=$modRoot; target=$targetRoot; passed=$true; fileCount=$manifest.Count; changedFiles=@($manifest | Where-Object changed).Count; targetOnly=$targetOnly; files=$manifest }
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'RuntimeSync.json'), ($result | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
Write-Output ('PASS 运行文件同步和 SHA256：' + $manifest.Count + ' 个；更新 ' + $result.changedFiles + ' 个。')
