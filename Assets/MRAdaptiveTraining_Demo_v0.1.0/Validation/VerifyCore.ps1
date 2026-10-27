$ErrorActionPreference = 'Stop'
$deliveryRoot = Split-Path -Parent $PSScriptRoot
$coreRoot = Join-Path $deliveryRoot 'SourceProject\Assets\MRAdaptiveTraining\Runtime\Core'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '需要 Windows .NET Framework C# 编译器，或在 Unity 中验证源码。' }
$output = Join-Path $PSScriptRoot 'CoreTests.exe'
& $compiler /nologo /target:exe ('/out:' + $output) (Join-Path $coreRoot 'TrainingModels.cs') (Join-Path $coreRoot 'TrainingSession.cs') (Join-Path $PSScriptRoot 'CoreTests.cs')
if ($LASTEXITCODE -ne 0) { throw '核心测试编译失败。' }
& $output
if ($LASTEXITCODE -ne 0) { throw '核心测试失败。' }
