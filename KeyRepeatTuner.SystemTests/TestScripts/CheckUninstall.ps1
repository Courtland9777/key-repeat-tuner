$InstallDir = "C:\Program Files\Key Repeat Tuner"
$StartMenu = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Key Repeat Tuner"
$RegistryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$AppRegKey = "KeyRepeatTuner"

Write-Host "`n🧹 Verifying uninstallation cleanup..." -ForegroundColor Cyan

if (Test-Path $InstallDir) {
    Write-Error "❌ Install directory still exists: $InstallDir"
} else {
    Write-Host "✔️ Install folder removed"
}

if (Test-Path $StartMenu) {
    Write-Error "❌ Start Menu folder still exists: $StartMenu"
} else {
    Write-Host "✔️ Start Menu folder removed"
}

$reg = Get-ItemProperty -Path $RegistryPath -Name $AppRegKey -ErrorAction SilentlyContinue
if ($null -ne $reg) {
    Write-Error "❌ Registry key still present"
} else {
    Write-Host "✔️ Auto-start registry key removed"
}
