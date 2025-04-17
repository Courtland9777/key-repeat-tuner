@echo off
setlocal
set appDataPath=%APPDATA%\KeyRepeatTuner
set target=%appDataPath%\appsettings.json
set source="%~dp0appsettings.json"

if not exist "%appDataPath%" (
    mkdir "%appDataPath%"
)

if not exist "%target%" (
    echo Copying default config to AppData...
    copy /Y %source% "%target%"
)

echo Opening appsettings.json...
start "" "%target%"
