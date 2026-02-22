@echo off
REM Usage: copy-to-usb.bat X:\Antivirus
REM Where X:\Antivirus is the target folder on your USB drive

if "%1"=="" (
    echo Usage: %0 ^<USB_TARGET_FOLDER^>
    exit /b 1
)
set USB_TARGET=%1

REM Create target directory if it doesn't exist
if not exist "%USB_TARGET%" mkdir "%USB_TARGET%"

REM Copy legacy executable and DLLs
copy /Y antivirus.Legacy\bin\Release\antivirus.Legacy.exe "%USB_TARGET%" >nul
copy /Y antivirus.Legacy\bin\Release\*.dll "%USB_TARGET%" >nul

REM Copy BrowserInstallers folder (if exists)
if exist BrowserInstallers xcopy /E /I /Y BrowserInstallers "%USB_TARGET%\BrowserInstallers"

REM Copy ClamAV folder (if exists)
if exist ClamAV xcopy /E /I /Y ClamAV "%USB_TARGET%\ClamAV"

REM Copy definitions and other important files (if needed)
if exist definitions.db copy /Y definitions.db "%USB_TARGET%" >nul

REM Done
echo Files copied to %USB_TARGET%
pause
