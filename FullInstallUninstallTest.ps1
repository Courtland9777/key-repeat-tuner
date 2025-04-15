$ErrorActionPreference = "Stop"
$ProductCode = "{9ED8FC20-EB1C-41EE-B7FC-A1ABEE87454A}"
$MsiPath = "C:\Users\Court\source\repos\KeyRepeatTuner\KeyRepeatTuner.Setup\bin\x64\Release\en-US\KeyRepeatTuner.Setup.msi"
$InstallDir = "C:\Program Files\KeyRepeatTuner"
$StartMenu = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\KeyRepeatTuner"
$RegistryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$AppRegKey = "KeyRepeatTuner"

function Test-KeyRepeatTunerInstall {
    Write-Host "`n[INSTALL CHECK]" -ForegroundColor Cyan

    if (!(Test-Path "$InstallDir\KeyRepeatTuner.exe")) {
        Write-Error "❌ EXE missing"
    } else {
        Write-Host "✔️ EXE present"
    }

    if (!(Test-Path "$InstallDir\appsettings.json")) {
        Write-Error "❌ appsettings.json missing"
    } else {
        Write-Host "✔️ appsettings.json present"
    }

    if (!(Test-Path "$StartMenu\KeyRepeatTuner.lnk")) {
        Write-Error "❌ App shortcut missing"
    } else {
        Write-Host "✔️ App shortcut present"
    }

    if (!(Test-Path "$StartMenu\Uninstall KeyRepeatTuner.lnk")) {
        Write-Error "❌ Uninstall shortcut missing"
    } else {
        Write-Host "✔️ Uninstall shortcut present"
    }

    $reg = Get-ItemProperty -Path $RegistryPath -Name $AppRegKey -ErrorAction SilentlyContinue
    if ($null -eq $reg) {
        Write-Error "❌ Registry auto-start missing"
    } else {
        Write-Host "✔️ Registry auto-start found"
    }
}

function Test-KeyRepeatTunerUninstall {
    Write-Host "`n[UNINSTALL CHECK]" -ForegroundColor Cyan

    if (Test-Path $InstallDir) {
        Write-Error "❌ Install directory still exists"
    } else {
        Write-Host "✔️ Install directory removed"
    }

    if (Test-Path $StartMenu) {
        Write-Error "❌ Start Menu folder still exists"
    } else {
        Write-Host "✔️ Start Menu folder removed"
    }

    $reg = Get-ItemProperty -Path $RegistryPath -Name $AppRegKey -ErrorAction SilentlyContinue
    if ($null -ne $reg) {
        Write-Error "❌ Registry key still exists"
    } else {
        Write-Host "✔️ Registry key removed"
    }
}

Write-Host "[INSTALLING] MSI..." -ForegroundColor Yellow
Start-Process "msiexec.exe" "/i `"$MsiPath`" /qn" -Wait

Test-KeyRepeatTunerInstall

Write-Host "`n[UNINSTALLING] MSI..." -ForegroundColor Yellow
Start-Process "msiexec.exe" "/x $ProductCode /qn" -Wait

Test-KeyRepeatTunerUninstall

Write-Host "`n✅ Install/Uninstall Test Completed" -ForegroundColor Green
