$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$testOutput = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $testOutput -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testExe = Join-Path $testOutput 'LiuAuraCinqSimulation.exe'
Push-Location $modRoot
try {
    & $compiler /nologo /utf8output /optimize+ /target:exe "/out:$testExe" /reference:Source\ClassLibrary1\bin\Debug\Assembly-CSharp.dll /reference:Source\ClassLibrary1\bin\Debug\UnityEngine.CoreModule.dll /reference:Source\ClassLibrary1\bin\Debug\Unity.Mathematics.dll /reference:Source\ClassLibrary1\bin\Debug\netstandard.dll /reference:Assemblies\ClassLibrary1.dll /reference:..\本机源码\rimworlddll\0Harmony.dll Source\Verification\LiuAuraCinqSimulation.cs
    if ($LASTEXITCODE -ne 0) { throw '模拟测试编译失败。' }
    & $testExe $modRoot
    if ($LASTEXITCODE -ne 0) { throw '模拟测试失败。' }
} finally {
    Pop-Location
}
