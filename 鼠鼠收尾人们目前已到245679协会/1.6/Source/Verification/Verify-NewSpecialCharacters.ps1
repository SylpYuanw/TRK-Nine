$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$defs = [Xml.XmlDocument]::new()
[void]$defs.AppendChild($defs.CreateElement('Defs'))
foreach ($filename in @('WuYaGuo.xml','CangYueCao.xml','HuoYanLan.xml','ShanHeYe.xml','JingLingLan.xml')) {
    [xml]$member = Get-Content -LiteralPath (Join-Path $modRoot ('Defs\SpecialCharacter\' + $filename)) -Raw -Encoding UTF8
    foreach ($node in $member.SelectNodes('/Defs/*')) { [void]$defs.DocumentElement.AppendChild($defs.ImportNode($node,$true)) }
}
$records = @(foreach ($snapshot in @('pawnslots_五桠果_苍月草.txt','pawnslots_HuoYanLan.txt','pawnslots_ShanHeYe.txt','pawnslots_JingLingLan.txt')) {
    Get-Content -LiteralPath (Join-Path $modRoot ('Source\ClassLibrary1\SpecialCharacter\' + $snapshot)) -Encoding UTF8 | Where-Object { $_.Trim() }
})
if ($records.Count -ne 5 -or @($records | ForEach-Object { $_.Split('*')[0] } | Sort-Object -Unique).Count -ne 5) { throw '导出应有五条不同人物记录。' }
$clothesFiles = @{ '五桠果' = 'Clothes_Cinq.xml'; '苍月草' = 'Clothes_Seven.xml'; '火焰兰' = 'Clothes_Liu.xml'; '山荷叶' = 'Clothes_Zwei.xml'; '精灵兰' = 'Clothes_Si.xml' }
$faDefs = @{}
foreach ($file in Get-ChildItem -LiteralPath 'D:\Steam\steamapps\workshop\content\294100\2852881266\1.6\Defs\FaceTypeDefs' -Filter '*.xml' -File) {
    [xml]$xml = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    foreach ($node in $xml.SelectNodes('/Defs/*[defName]')) { $faDefs[$node.defName] = $node.Name }
}
[xml]$xenotypes = Get-Content -LiteralPath (Join-Path $modRoot '..\..\Ratkin\Biotech\Defs\GeneDefs\XenotypeDefs.xml') -Raw -Encoding UTF8
$xenotype = $xenotypes.SelectSingleNode('/Defs/XenotypeDef[defName="RK_XenoType_Ratkin"]')
$qualityNames = @('Awful', 'Poor', 'Normal', 'Good', 'Excellent', 'Masterwork', 'Legendary')
foreach ($record in $records) {
    $slot = $record.Split('*')
    $name = $slot[0]
    if ($slot.Count -ne 57) { throw ($name + '导出字段数量不符。') }
    $profile = $defs.SelectSingleNode('/Defs/XIYUNTE.SpecialCharacter.SpecialCharacterDef[label="' + $name + '"]')
    $kind = $defs.SelectSingleNode('/Defs/PawnKindDef[defName="' + $profile.pawnKind + '"]')
    if (!$profile -or !$kind -or [int]$profile.price -ne 2000) { throw ($name + '人物或价格不符。') }
    $expectedHair = switch ($name) { '苍月草' { 'RK_sidePonyA' }; '山荷叶' { 'RK_LongA' }; default { $slot[12] } }
    $age = [Math]::Floor([double]$slot[4] / 3600000)
    $parts = $slot[3].Split('?')
    if ($profile.firstName -cne $parts[0] -or $profile.nickname -cne $parts[1] -or $profile.lastName -cne $parts[2] -or
        [int]$profile.biologicalAge -ne $age -or $profile.gender -ne 'Female' -or
        $profile.bodyType -cne $slot[7] -or $profile.headType -cne $slot[47] -or
        $profile.hair -cne $expectedHair -or $kind.forcedHair -cne $expectedHair -or
        $profile.favoriteColor -cne $slot[56] -or $kind.fixedChildBackstories.li -cne $slot[23] -or
        $kind.fixedAdultBackstories.li -cne $slot[24]) { throw ($name + '身份与外观不符。') }
    foreach ($pair in @(@('hairColor',14), @('hairColorSecond',15))) {
        $bytes = $slot[$pair[1]].Split('-')
        $expected = '(' + ((@($bytes | ForEach-Object { ([Convert]::ToInt32($_,16)/255.0).ToString('0.000',[Globalization.CultureInfo]::InvariantCulture) })) -join ',') + ')'
        if ($profile.($pair[0]) -cne $expected) { throw ($name + '发色不符。') }
    }
    if (@($profile.endogenes.li).Count -ne 14 -or (Compare-Object @($profile.endogenes.li) $slot[50].Split(':')) -or
        @($xenotype.genes.li | Where-Object { $_ -notin $profile.endogenes.li }).Count) { throw ($name + '基因不符。') }
    foreach ($raw in $slot[20].Split(':')) {
        $parts = $raw.Split('|')
        $skill = @($profile.skills.li | Where-Object { $_.skill -ceq $parts[0] })
        if ($skill.Count -ne 1 -or [int]$skill[0].level -ne [int]$parts[1] -or
            $skill[0].passion -cne @('None','Minor','Major')[[int]$parts[2]]) { throw ($name + '技能不符：' + $parts[0]) }
        $aptitude = if ($parts[0] -in @('Mining','Plants')) { 4 } else { 0 }
        if ([int]$parts[1] - $aptitude -lt 0) { throw ($name + '技能目标低于固定资质。') }
    }
    foreach ($raw in $slot[21].Split(':')) {
        $parts = $raw.Split('|')
        $trait = $kind.forcedTraits.SelectSingleNode($parts[0])
        if (!$trait -or [int]$trait.InnerText -ne [int]$parts[1]) { throw ($name + '特质不符。') }
    }
    foreach ($group in @(@('weapon',26), @('apparel',27), @('inventory',28))) {
        $items = if ($group[0] -eq 'weapon') { @($profile.weapon) } else { @($profile.($group[0]).li | Where-Object { !$_.MayRequire }) }
        $rawItems = $slot[$group[1]].Split(':')
        if ($items.Count -ne $rawItems.Count) { throw ($name + '物品数量不符。') }
        foreach ($raw in $rawItems) {
            $parts = $raw.Split('|')
            $item = @($items | Where-Object { $_.thing -ceq $parts[0] })
            $expectedHitPoints = if ($name -eq '精灵兰' -and $parts[0] -eq 'Bow_of_Death_Arrow' -and [int]$parts[5] -eq -1) { 100 } else { [int]$parts[5] }
            if ($item.Count -ne 1 -or [string]$item[0].stuff -cne $parts[1] -or
                [int]$item[0].count -ne [int]$parts[4] -or [int]$item[0].hitPoints -ne $expectedHitPoints) { throw ($name + '物品配置不符。') }
            if ($parts[1] -and $item[0].quality -cne $qualityNames[[int]$parts[3]]) { throw ($name + '品质不符。') }
            if ($group[0] -eq 'apparel') {
                $expected = '(' + ((@($parts[2].Split('-') | ForEach-Object { ([Convert]::ToInt32($_,16)/255.0).ToString('0.000',[Globalization.CultureInfo]::InvariantCulture) })) -join ',') + ')'
                if ($item[0].color -cne $expected) { throw ($name + '衣物颜色不符。') }
                [xml]$equipment = Get-Content -LiteralPath (Join-Path $modRoot ('Defs\ThingDefs_Items\' + $clothesFiles[$name])) -Raw -Encoding UTF8
                $itemDef = $equipment.SelectSingleNode('/Defs/ThingDef[defName="' + $item[0].thing + '"]')
                $maxHp = [double]$itemDef.statBases.MaxHitPoints * 1.3
                $maxHp = if ($maxHp -gt 200) { [Math]::Round($maxHp / 5) * 5 } else { [Math]::Round($maxHp) }
                if ([int]$item[0].hitPoints -gt $maxHp) { throw ($name + '耐久超出当前材料及Def上限。') }
            }
        }
    }
    $fields = @('head','eyes','lids','brows','mouth','skin')
    $types = @('HeadTypeDef','EyeballTypeDef','LidTypeDef','BrowTypeDef','MouthTypeDef','SkinTypeDef')
    $parts = $slot[45].Split('|')
    for ($i=0; $i -lt 6; $i++) {
        if ($profile.facialAnimation.($fields[$i]) -cne $parts[$i] -or $faDefs[$parts[$i]] -cne ('FacialAnimation.'+$types[$i])) { throw ($name + 'FA类型不符。') }
    }
    foreach ($pair in @(@('eyeColor',19),@('eyeColorSecond',53))) {
        $expected = '(' + ((@($slot[$pair[1]].Split('-') | ForEach-Object { ([Convert]::ToInt32($_,16)/255.0).ToString('0.000',[Globalization.CultureInfo]::InvariantCulture) })) -join ',') + ')'
        if ($profile.facialAnimation.($pair[0]) -cne $expected) { throw ($name + '眼色不符。') }
    }
    if ([string]::IsNullOrWhiteSpace($profile.detailedReport)) { throw ($name + '缺少详细报告。') }
    'PASS: ' + $name + '身份、发色、眼色、12项技能、特质、14基因、装备物品、耐久、FA及2000银价格已按导出核对。'
}
