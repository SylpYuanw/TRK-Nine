$ErrorActionPreference = 'Stop'
$modRoot = Split-Path -Path $PSScriptRoot -Parent
$evidencePath = Join-Path $PSScriptRoot 'SharedAssetsUpdate.json'
$evidence = Get-Content -LiteralPath $evidencePath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($evidence.modRoot -ne $modRoot) { throw '资源核验记录不属于当前 Mod。' }

foreach ($folder in @('Textures', 'Sounds')) {
    $source = [IO.Path]::GetFullPath((Join-Path $modRoot ('1.6\' + $folder)))
    $duplicate = [IO.Path]::GetFullPath((Join-Path $modRoot ('1.7\' + $folder)))
    $destination = [IO.Path]::GetFullPath((Join-Path $modRoot $folder))
    if (!$source.StartsWith($modRoot + '\1.6\') -or !$duplicate.StartsWith($modRoot + '\1.7\') -or [IO.Path]::GetFileName($duplicate) -ne $folder) {
        throw '资源目录不在当前版本范围。'
    }
    foreach ($root in @($source, $duplicate)) {
        if ((Get-Item -LiteralPath $root -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw '资源根目录包含重解析点。' }
        if (@(Get-ChildItem -LiteralPath $root -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count -gt 0) {
            throw '资源子目录包含重解析点。'
        }
    }
    if (Test-Path -LiteralPath $destination) { throw '共享资源目标已存在，需重新核对。' }
    $records = @($evidence.files | Where-Object { $_.path.StartsWith($folder + '/') })
    foreach ($record in $records) {
        $relative = $record.path.Substring($folder.Length + 1)
        foreach ($file in @((Join-Path $source $relative), (Join-Path $duplicate $relative))) {
            if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $record.sha256) { throw ('共享资源内容有变化：' + $file) }
        }
    }
    if (@(Get-ChildItem -LiteralPath $source -File -Recurse -Force).Count -ne $records.Count -or @(Get-ChildItem -LiteralPath $duplicate -File -Recurse -Force).Count -ne $records.Count) {
        throw '资源目录含核验记录之外的文件。'
    }
    Move-Item -LiteralPath $source -Destination $destination

    # 逐文件确认根目录副本后删除重复文件，仅移除已空的重复目录。
    foreach ($record in $records) {
        $relative = $record.path.Substring($folder.Length + 1)
        $canonicalFile = [IO.Path]::GetFullPath((Join-Path $destination $relative))
        $duplicateFile = [IO.Path]::GetFullPath((Join-Path $duplicate $relative))
        if (!$canonicalFile.StartsWith($destination + '\') -or !$duplicateFile.StartsWith($duplicate + '\')) { throw '文件路径越界。' }
        if ((Get-FileHash -LiteralPath $canonicalFile -Algorithm SHA256).Hash -ne $record.sha256 -or (Get-FileHash -LiteralPath $duplicateFile -Algorithm SHA256).Hash -ne $record.sha256) {
            throw '共享资源副本不一致，中止去重。'
        }
        Remove-Item -LiteralPath $duplicateFile
    }
    $emptyDirectories = @(Get-ChildItem -LiteralPath $duplicate -Directory -Recurse -Force | Sort-Object { $_.FullName.Length } -Descending)
    foreach ($directory in $emptyDirectories) {
        if (@(Get-ChildItem -LiteralPath $directory.FullName -Force).Count -ne 0) { throw '重复资源子目录不为空。' }
        Remove-Item -LiteralPath $directory.FullName
    }
    if (@(Get-ChildItem -LiteralPath $duplicate -Force).Count -ne 0) { throw '重复资源根目录不为空。' }
    Remove-Item -LiteralPath $duplicate
    Write-Output ($folder + ' 已合并到 ' + $destination + '；保留 ' + $records.Count + ' 个文件。')
}
foreach ($change in $evidence.unityChanges) {
    $change.afterSHA256 = (Get-FileHash -LiteralPath (Join-Path $change.root $change.path) -Algorithm SHA256).Hash
}
$evidence | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $evidencePath -Encoding UTF8
