param([Parameter(Mandatory=$true)][string]$LogPath, [Parameter(Mandatory=$true)][string]$OutputPath, [string]$DllPath)
$ErrorActionPreference = 'Stop'
$fallbacks = @(Select-String -LiteralPath $LogPath -Pattern "ShaderRegistry: shader '.*BowOfDeathAim(Wind|Charge).*not registered, falling back to default" | ForEach-Object { [pscustomobject]@{line=$_.LineNumber;text=$_.Line} })
$preparation = @(Select-String -LiteralPath $LogPath -Pattern '\[RimUnity\] Shader 准备结束：2 项，失败 0 项')
$prefixCalls = @(Select-String -LiteralPath $LogPath -Pattern 'XIYUNTE.DeathBowRimUnityShaderPatch.Prefix')
$result = [ordered]@{checked=[DateTimeOffset]::Now.ToString('o');log=[IO.Path]::GetFullPath($LogPath);logSHA256=(Get-FileHash -LiteralPath $LogPath).Hash;passed=$fallbacks.Count -eq 0 -and $preparation.Count -gt 0 -and $prefixCalls.Count -ge 4;fallbacks=$fallbacks;preparationCount=$preparation.Count;prefixStackCount=$prefixCalls.Count;limitation='检查启动材质初始化的默认回退与两个 Shader 经 Prefix 首次加载的四条兼容诊断栈，不代替特效画面验收。'}
if ($DllPath) { $result.dll=[IO.Path]::GetFullPath($DllPath); $result.dllSHA256=(Get-FileHash -LiteralPath $DllPath).Hash }
[IO.File]::WriteAllText([IO.Path]::GetFullPath($OutputPath), ($result | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
Write-Output ('Shader 默认回退='+$fallbacks.Count+'；准备结束记录='+$preparation.Count+'；Prefix 调用栈='+$prefixCalls.Count)
if (!$result.passed) { throw 'Shader 启动加载回归未通过。' }
