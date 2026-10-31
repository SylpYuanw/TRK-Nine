param([string]$FacialAnimationAssembly)

$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$testOutput = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $testOutput -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testExe = Join-Path $testOutput 'SpecialCharacterVerification.exe'
Push-Location $modRoot
try {
    & $compiler /nologo /utf8output /target:exe "/out:$testExe" /reference:Assemblies\ClassLibrary1.dll /reference:Source\ClassLibrary1\bin\Debug\Assembly-CSharp.dll /reference:Source\ClassLibrary1\bin\Debug\UnityEngine.CoreModule.dll Source\Verification\SpecialCharacterVerification.cs
    if ($LASTEXITCODE -ne 0) { throw '特殊人物验证程序编译失败。' }
    # 游戏解析器使用新版 BCL 接口，独立验证在 .NET 8 运行，不使用旧 Framework CLR。
    @{ runtimeOptions = @{ tfm = 'net8.0'; framework = @{ name = 'Microsoft.NETCore.App'; version = '8.0.0' } } } |
        ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $testOutput 'SpecialCharacterVerification.runtimeconfig.json') -Encoding UTF8
    & dotnet $testExe $modRoot $FacialAnimationAssembly
    if ($LASTEXITCODE -ne 0) { throw '特殊人物状态快照与读档初始化验证失败。' }

    [xml]$defs = Get-Content -LiteralPath 'Defs\SpecialCharacter\ManTianXing.xml' -Raw -Encoding UTF8
    $profile = $defs.Defs.'XIYUNTE.SpecialCharacter.SpecialCharacterDef'
    $kind = $defs.Defs.PawnKindDef
    $slot = (Get-Content -LiteralPath '..\pawnslots.txt' -Encoding UTF8 | Where-Object { $_.Trim() } | Select-Object -First 1).Split('*')
    if ([int]$profile.price -ne 2000) { throw '购买价格不符。' }
    if ([double]$profile.biologicalAge * 3600000 -ne [double]$slot[4] -or $profile.gender -ne 'Female') { throw '年龄或性别不符。' }
    if ($profile.headType -ne $slot[47] -or $profile.hair -ne $slot[12] -or $profile.bodyType -ne $slot[7]) { throw '外观 Def 不符。' }
    if ($kind.fixedChildBackstories.li -ne $slot[23] -or $kind.fixedAdultBackstories.li -ne $slot[24]) { throw '经历不符。' }
    if (@($profile.endogenes.li).Count -ne 14 -or (Compare-Object @($profile.endogenes.li) $slot[50].Split(':'))) { throw '内源基因清单不符。' }
    [xml]$ratkinXenotypes = Get-Content -LiteralPath (Join-Path $modRoot '..\..\Ratkin\Biotech\Defs\GeneDefs\XenotypeDefs.xml') -Raw -Encoding UTF8
    $ratkinXenotype = $ratkinXenotypes.SelectSingleNode('/Defs/XenotypeDef[defName="' + $profile.xenotype + '"]')
    if (!$ratkinXenotype -or $ratkinXenotype.inheritable -ne 'true') { throw '固定鼠族异种未定义或不是内源异种。' }
    $appearanceGenes = @($profile.endogenes.li | Where-Object { $_ -notin $ratkinXenotype.genes.li })
    if ((Compare-Object $appearanceGenes @('Skin_Melanin3', 'Hair_DarkReddish')) -or
        @($ratkinXenotype.genes.li | Where-Object { $_ -notin $profile.endogenes.li }).Count) { throw '人物清单与鼠族异种基因不一致。' }
    foreach ($rawSkill in $slot[20].Split(':')) {
        $parts = $rawSkill.Split('|')
        $skill = @($profile.skills.li | Where-Object { $_.skill -eq $parts[0] })
        $passion = @('None', 'Minor', 'Major')[[int]$parts[2]]
        if ($skill.Count -ne 1 -or [int]$skill[0].level -ne [int]$parts[1] -or $skill[0].passion -ne $passion) { throw ('技能不符：' + $parts[0]) }
    }
    foreach ($rawTrait in $slot[21].Split(':')) {
        $parts = $rawTrait.Split('|')
        $trait = @($kind.forcedTraits.SelectNodes('*') | Where-Object { $_.Name -eq $parts[0] })
        if ($trait.Count -ne 1 -or [int]$trait[0].InnerText -ne [int]$parts[1]) { throw ('特质不符：' + $parts[0]) }
    }
    $qualityNames = @('Awful', 'Poor', 'Normal', 'Good', 'Excellent', 'Masterwork', 'Legendary')
    $items = @($profile.weapon) + @($profile.apparel.li) + @($profile.inventory.li)
    $rawItems = @($slot[26].Split(':')) + @($slot[27].Split(':')) + @($slot[28].Split(':'))
    if ($items.Count -ne $rawItems.Count) { throw '物品数量不符。' }
    foreach ($rawItem in $rawItems) {
        $parts = $rawItem.Split('|')
        $item = @($items | Where-Object { $_.thing -eq $parts[0] })
        $count = if ($item[0].count) { [int]$item[0].count } else { 1 }
        if ($item.Count -ne 1 -or [string]$item[0].stuff -ne $parts[1] -or $count -ne [int]$parts[4] -or [int]$item[0].hitPoints -ne [int]$parts[5]) { throw ('装备或库存不符：' + $parts[0]) }
        if ($parts[1] -and $item[0].quality -ne $qualityNames[[int]$parts[3]]) { throw ('装备品质不符：' + $parts[0]) }
    }
    [xml]$language = Get-Content -LiteralPath 'Languages\ChineseSimplified (简体中文)\Keyed\SpecialCharacter.xml' -Raw -Encoding UTF8
    $keys = @($language.LanguageData.ChildNodes | Where-Object { $_.NodeType -eq 'Element' } | ForEach-Object { $_.Name })
    $source = (Get-ChildItem -LiteralPath 'Source\ClassLibrary1\SpecialCharacter' -Filter '*.cs' | Get-Content -Raw -Encoding UTF8) -join "`n"
    foreach ($match in [regex]::Matches($source, '"(TRK_SC_[^"]+)"\.Translate')) {
        if ($match.Groups[1].Value -notin $keys) { throw ('缺少文本：' + $match.Groups[1].Value) }
    }

    # 原版名称由 RIMSAGE 证据清单提供，模组名称从实际加载目录建立索引。
    $coreEvidence = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SpecialCharacter-CoreDefEvidence.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $knownDefs = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $templates = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($entry in $coreEvidence.defs) {
        [void]$knownDefs.Add($entry.type + "`t" + $entry.name)
        if ($entry.namedTemplate) { [void]$templates.Add($entry.namedTemplate) }
    }
    $ratkinRoot = Join-Path $modRoot '..\..\Ratkin'
    $defDirectories = @((Join-Path $modRoot 'Defs')) + @('Defs', '1.6\Defs', 'Contents\Defs', 'Biotech\Defs' | ForEach-Object { Join-Path $ratkinRoot $_ })
    $localDefs = @{}
    foreach ($directory in $defDirectories) {
        if (!(Test-Path -LiteralPath $directory)) { continue }
        foreach ($file in Get-ChildItem -LiteralPath $directory -Filter '*.xml' -File -Recurse) {
            [xml]$document = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
            foreach ($node in $document.SelectNodes('/Defs/*')) {
                if ($node.GetAttribute('Name')) { [void]$templates.Add($node.GetAttribute('Name')) }
                $nameNode = $node.SelectSingleNode('defName')
                if (!$nameNode) { continue }
                $typeName = switch ($node.LocalName) {
                    'AlienRace.ThingDef_AlienRace' { 'ThingDef' }
                    'AlienRace.AlienBackstoryDef' { 'BackstoryDef' }
                    default { $node.LocalName.Split('.')[-1] }
                }
                $identifier = $typeName + "`t" + $nameNode.InnerText
                [void]$knownDefs.Add($identifier)
                $localDefs[$identifier] = 1 + [int]$localDefs[$identifier]
            }
        }
    }
    $references = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($reference in Get-Content -LiteralPath (Join-Path $testOutput 'SpecialCharacter-def-references.tsv') -Encoding UTF8) {
        [void]$references.Add($reference)
    }
    $allProfiles = @(foreach ($file in Get-ChildItem -LiteralPath 'Defs\SpecialCharacter' -Filter '*.xml' -File) {
        [xml]$definitions = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        $definitions.SelectNodes('/Defs/XIYUNTE.SpecialCharacter.SpecialCharacterDef')
    })
    foreach ($character in $allProfiles) {
        foreach ($gene in $character.endogenes.li) { [void]$references.Add("GeneDef`t" + $gene) }
        [void]$references.Add("XenotypeDef`t" + $character.xenotype)
        if ($character.favoriteColor) { [void]$references.Add("ColorDef`t" + $character.favoriteColor) }
    }
    foreach ($reference in @(
        ("XenotypeDef`t" + $profile.xenotype), "XenotypeDef`tBaseliner", "FactionDef`tRakinia",
        "ThingDef`tRatkin", "ThingDef`tTRK_SpecialCharacterPod", "ThingDef`tDropPodIncoming", "BeardDef`tNoBeard",
        "TattooDef`tNoTattoo_Face", "TattooDef`tNoTattoo_Body", "ColorDef`tLightPurple")) {
        [void]$references.Add($reference)
    }
    foreach ($skillName in @('Mining', 'Plants')) {
        if (!$knownDefs.Contains("GeneTemplateDef`tAptitudeStrong") -or !$knownDefs.Contains("SkillDef`t" + $skillName)) {
            throw '动态资质基因缺少原版模板或技能 Def。'
        }
        [void]$knownDefs.Add("GeneDef`tAptitudeStrong_" + $skillName)
    }
    foreach ($reference in $references) {
        if (!$knownDefs.Contains($reference)) { throw ('Def 引用不存在或类型不符：' + $reference) }
        if ([int]$localDefs[$reference] -gt 1) { throw ('Def 引用对应重复定义：' + $reference) }
    }
    foreach ($node in $defs.SelectNodes('/Defs/*[@ParentName]')) {
        if (!$templates.Contains($node.GetAttribute('ParentName'))) { throw ('继承模板不存在：' + $node.GetAttribute('ParentName')) }
    }
    foreach ($iconPath in @($allProfiles.iconPath) + @('SpecialCharacter/Hana_Association')) {
        if (!(Test-Path -LiteralPath (Join-Path $modRoot ('..\Textures\' + $iconPath + '.png')))) { throw ('缺少图标：' + $iconPath) }
    }
    ('PASS: ' + $references.Count + ' 项唯一 Def 引用、继承模板和图标路径已核对；动态资质基因按原版模板验证。')
    'PASS: 人物身份、技能和装备按原始槽位核对；2000 白银、12 项技能、3 项特质、14 基因、6 件装备/库存及中文文本正确。肤色按最新规则保留基因结果。'
    & (Join-Path $PSScriptRoot 'Verify-NewSpecialCharacters.ps1')
} finally {
    Pop-Location
}
