# 引用一致性自检:防止"XML 定义的 Def / C# 类型"与"C# 里写的名字"对不上导致运行时才报错。
# 检查两件事:
#   1) DeathBowDefOf 里每个字段(字段名=defName,字段类型=Def 种类)在本 MOD 的 Defs 里都能找到同名且同种类的定义;
#   2) Defs 里以 XIYUNTE. 开头的 Class/thingClass/compClass 在已编译的程序集里真实存在。
# 用法(在工作区根目录执行): powershell -File "鼠鼠收尾人们目前已到4579协会\Source\自检_引用一致性.ps1"
# 退出码:0=通过,1=发现问题。

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$defsRoot = Join-Path $modRoot "Defs"
$dll = Join-Path $modRoot "Assemblies\ClassLibrary1.dll"
$defOfCs = Join-Path $modRoot "Source\ClassLibrary1\Weapon\BowOfDeath\DeathBowDefOf.cs"
$problems = New-Object System.Collections.Generic.List[string]

# ---------- 1) 收集 XML 里的 defName 及其所在元素类型 ----------
$xmlDefs = @{}
$dupDefs = New-Object System.Collections.Generic.List[string]
foreach ($file in Get-ChildItem $defsRoot -Recurse -Filter *.xml) {
    $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    $clean = [regex]::Replace($text, '(?s)<!--.*?-->', '')
    foreach ($m in [regex]::Matches($clean, '(?s)<defName>\s*([^<>]+?)\s*</defName>')) {
        $name = $m.Groups[1].Value
        $before = $clean.Substring(0, $m.Index)
        $tags = [regex]::Matches($before, '<\s*([A-Za-z_][\w]*)')
        $tag = if ($tags.Count -gt 0) { $tags[$tags.Count - 1].Groups[1].Value } else { "?" }
        # 同名不同类型的 Def(RimWorld 允许,例如 AbilityDef 与 HediffDef 同名)不算重复,因此按"类型+名字"判重。
        $key = "$tag|$name"
        if ($xmlDefs.ContainsKey($key)) {
            $dupDefs.Add("重复定义 <$tag> defName $name :$($xmlDefs[$key].File) 与 $($file.Name)")
        }
        $xmlDefs[$key] = [pscustomobject]@{ Tag = $tag; Name = $name; File = $file.Name }
    }
}
foreach ($d in $dupDefs) { $problems.Add($d) }

# ---------- 2) 校验 DeathBowDefOf 的每个字段 ----------
$cs = [System.IO.File]::ReadAllText($defOfCs, [System.Text.Encoding]::UTF8)
$fields = [regex]::Matches($cs, 'public\s+static\s+([A-Za-z_][\w]*)\s+([A-Za-z_][\w]*)\s*;')
if ($fields.Count -eq 0) { $problems.Add("DeathBowDefOf.cs 里没有解析到任何字段,检查脚本或文件路径") }
foreach ($f in $fields) {
    $type = $f.Groups[1].Value
    $name = $f.Groups[2].Value
    $sameName = @($xmlDefs.Values | Where-Object { $_.Name -eq $name })
    if ($sameName.Count -eq 0) {
        $problems.Add("DefOf 字段 $type $name : Defs 里找不到 <defName>$name</defName>")
        continue
    }
    if (@($sameName | Where-Object { $_.Tag -eq $type }).Count -eq 0) {
        $tags = ($sameName | ForEach-Object { "<$($_.Tag)>" }) -join "、"
        $problems.Add("DefOf 字段 $type $name : 只找到 $tags 定义,种类不匹配")
    }
}
Write-Host ("DefOf 字段数: {0},XML defName 数: {1}" -f $fields.Count, $xmlDefs.Count)

# ---------- 3) 校验 XML 里以 XIYUNTE. 开头的类引用 ----------
if (-not (Test-Path $dll)) {
    $problems.Add("找不到程序集:$dll")
}
else {
    $ip = Join-Path $env:USERPROFILE ".dotnet\tools\ilspycmd.exe"
    if (-not (Test-Path $ip)) {
        Write-Host "跳过 C# 类型检查:未找到 ilspycmd"
    }
    else {
        $types = & $ip -l c $dll 2>$null | ForEach-Object { ($_ -replace '^\s*Class\s+', '').Trim() }
        foreach ($file in Get-ChildItem $defsRoot -Recurse -Filter *.xml) {
            $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
            $refs = [regex]::Matches($text, '(?:Class|thingClass|compClass)>\s*(XIYUNTE\.[A-Za-z_][\w\.]*)|Class="(XIYUNTE\.[A-Za-z_][\w\.]*)"')
            foreach ($r in $refs) {
                $typeName = if ($r.Groups[1].Success) { $r.Groups[1].Value } else { $r.Groups[2].Value }
                if ($types -notcontains $typeName) {
                    $problems.Add("XML 引用了不存在的类型 $typeName($($file.Name))")
                }
            }
        }
    }
}

# ---------- 结果 ----------
# ---------- 4) 校验 texPath 指向的贴图存在(本 MOD 贴图目录内必须存在;外部路径只做提示) ----------
$texRoot = Join-Path $modRoot "Textures"
$ownRoots = @(Get-ChildItem $texRoot -Directory -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
$external = New-Object System.Collections.Generic.List[string]
foreach ($file in Get-ChildItem $defsRoot -Recurse -Filter *.xml) {
    $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    foreach ($m in [regex]::Matches($text, '<texPath>\s*([^<]+?)\s*</texPath>')) {
        $p = $m.Groups[1].Value.Trim()
        $found = (Test-Path (Join-Path $texRoot $p))
        if (-not $found) {
            foreach ($ext in @('.png', '.jpg', '.jpeg', '.dds', '.tga')) {
                if (Test-Path (Join-Path $texRoot ($p + $ext))) { $found = $true; break }
            }
        }
        if (-not $found) {
            foreach ($dir in @('_east', '_north', '_south', '_west')) {
                if (Test-Path (Join-Path $texRoot ($p + $dir + '.png'))) { $found = $true; break }
            }
        }
        if (-not $found) {
            $first = ($p -split '/')[0]
            if ($ownRoots -contains $first) {
                $problems.Add("texPath 指向本 MOD 贴图目录却找不到文件:$p($($file.Name))")
            }
            else {
                $external.Add("$p($($file.Name))")
            }
        }
    }
}
if ($external.Count -gt 0) {
    Write-Host "提示:以下 texPath 不在本 MOD 贴图内(来自原版或其他 MOD,未做存在性校验):" -ForegroundColor Yellow
    foreach ($e in ($external | Sort-Object -Unique)) { Write-Host (" - " + $e) -ForegroundColor Yellow }
}

# ---------- 结果 ----------
if ($problems.Count -eq 0) {
    Write-Host "自检通过:DefOf 与 Defs、XML 与程序集引用一致。" -ForegroundColor Green
    exit 0
}
Write-Host "自检发现问题:" -ForegroundColor Red
foreach ($p in $problems) { Write-Host (" - " + $p) -ForegroundColor Red }
exit 1
