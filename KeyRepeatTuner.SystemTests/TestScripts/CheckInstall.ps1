$InstallDir = "C:\Program Files\Key Repeat Tuner"
$StartMenu = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Key Repeat Tuner"
$RegistryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$AppRegKey = "KeyRepeatTuner"

Write-Host "`n🔍 Verifying installation..." -ForegroundColor Cyan

if (!(Test-Path "$InstallDir\KeyRepeatTuner.exe")) {
    Write-Error "❌ KeyRepeatTuner.exe not found"
} else {
    Write-Host "✔️ EXE found"
}

if (!(Test-Path "$InstallDir\appsettings.json")) {
    Write-Error "❌ appsettings.json missing"
} else {
    Write-Host "✔️ appsettings.json present"
}

if (!(Test-Path "$StartMenu\Key Repeat Tuner.lnk")) {
    Write-Error "❌ App shortcut missing"
} else {
    Write-Host "✔️ Start Menu shortcut present"
}

if (!(Test-Path "$StartMenu\Uninstall Key Repeat Tuner.lnk")) {
    Write-Error "❌ Uninstall shortcut missing"
} else {
    Write-Host "✔️ Uninstall shortcut present"
}

if (!(Test-Path "$StartMenu\Edit User Settings.lnk")) {
    Write-Error "❌ Edit User Settings shortcut missing"
} else {
    Write-Host "✔️ Settings shortcut present"
}

$reg = Get-ItemProperty -Path $RegistryPath -Name $AppRegKey -ErrorAction SilentlyContinue
if ($null -eq $reg) {
    Write-Error "❌ Auto-start registry key not found"
} else {
    Write-Host "✔️ Auto-start registry key set"
}
