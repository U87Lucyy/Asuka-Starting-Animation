$ErrorActionPreference = 'Stop'
$desktop = [Environment]::GetFolderPath('Desktop')
$executable = Join-Path $PSScriptRoot 'AsukaCodex.exe'
if (-not (Test-Path -LiteralPath $executable)) { throw 'Build AsukaCodex.exe first.' }
$shell = New-Object -ComObject WScript.Shell
foreach ($name in '明日香 × Codex','明日香动画预览') {
    $path = Join-Path $desktop "$name.lnk"
    $shortcut = $shell.CreateShortcut($path)
    if ((Test-Path -LiteralPath $path) -and $shortcut.TargetPath -ne $executable) {
        throw "An unrelated shortcut already uses this name: $path"
    }
    $shortcut.TargetPath = $executable
    $shortcut.WorkingDirectory = $PSScriptRoot
    $shortcut.IconLocation = $executable + ',0'
    $shortcut.Description = '明日香渐变线条登场与睁眼动画，与 Codex 窗口尺寸一致'
    if ($name -eq '明日香动画预览') { $shortcut.Arguments = '--preview' }
    else { $shortcut.Arguments = '' }
    $shortcut.Save()
    Write-Host "Created $path"
}
