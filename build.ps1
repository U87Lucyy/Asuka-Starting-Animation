$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$wpf = Join-Path $framework 'WPF'
New-Item -ItemType Directory -Path build -Force | Out-Null
& $compiler /nologo /target:exe /out:build\LaunchFlowTests.exe tests\LaunchFlowTests.cs src\LaunchFlow.cs
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& '.\build\LaunchFlowTests.exe'
if ($LASTEXITCODE -ne 0) { throw 'Launch flow tests failed.' }
& $compiler /nologo /target:exe /out:build\ActivationTests.exe tests\ActivationTests.cs src\CodexActivation.cs
if ($LASTEXITCODE -ne 0) { throw 'Activation test compilation failed.' }
& '.\build\ActivationTests.exe'
if ($LASTEXITCODE -ne 0) { throw 'Activation tests failed.' }
& $compiler /nologo /reference:System.Drawing.dll /out:build\WindowTargetTests.exe tests\WindowTargetTests.cs src\WindowTarget.cs
if ($LASTEXITCODE -ne 0) { throw 'Window geometry test compilation failed.' }
& $compiler /nologo /utf8output /target:winexe /platform:x64 /optimize+ /out:AsukaCodex.exe /win32manifest:src\app.manifest /resource:src\Scene.xaml,Scene.xaml /reference:System.Xaml.dll /reference:System.Drawing.dll /reference:"$wpf\WindowsBase.dll" /reference:"$wpf\PresentationCore.dll" /reference:"$wpf\PresentationFramework.dll" src\Launcher.cs src\LaunchFlow.cs src\CodexActivation.cs src\WindowTarget.cs
if ($LASTEXITCODE -ne 0) { throw 'Launcher compilation failed.' }
Write-Host 'Built AsukaCodex.exe'
