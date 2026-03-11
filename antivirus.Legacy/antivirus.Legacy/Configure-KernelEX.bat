@echo off
REM Configure ClamAV executables to run with KernelEX on Windows ME
REM This script adds KernelEX compatibility settings to the registry

echo ========================================
echo  KernelEX Configuration for ClamAV
echo ========================================
echo.
echo This script configures clamscan.exe and freshclam.exe
echo to run with Windows 2000 compatibility via KernelEX.
echo.
echo Requirements:
echo   - KernelEX must be installed first
echo   - Download from: http://kernelex.sourceforge.net/
echo.
pause

REM Check if ClamAV directory exists
if not exist "ClamAV" (
    echo ERROR: ClamAV directory not found!
    echo Please run option 3 to download ClamAV first.
    pause
    exit /b 1
)

echo.
echo Configuring KernelEX compatibility settings...
echo.

REM Get current directory
set CLAMAV_DIR=%CD%\ClamAV

REM Configure freshclam.exe for Windows 2000 mode
echo [*] Configuring freshclam.exe...
reg add "HKCU\Software\KernelEx" /v "%CLAMAV_DIR%\freshclam.exe" /t REG_SZ /d "Windows2000" /f >nul 2>&1
if errorlevel 1 (
    echo     WARNING: Could not set registry key. KernelEX may not be installed.
) else (
    echo     SUCCESS: freshclam.exe configured for Windows 2000 mode
)

REM Configure clamscan.exe for Windows 2000 mode
echo [*] Configuring clamscan.exe...
reg add "HKCU\Software\KernelEx" /v "%CLAMAV_DIR%\clamscan.exe" /t REG_SZ /d "Windows2000" /f >nul 2>&1
if errorlevel 1 (
    echo     WARNING: Could not set registry key. KernelEX may not be installed.
) else (
    echo     SUCCESS: clamscan.exe configured for Windows 2000 mode
)

echo.
echo ========================================
echo  Configuration Complete!
echo ========================================
echo.
echo ClamAV executables are now configured to run with KernelEX.
echo You can now use options 1 and 3 in the antivirus menu.
echo.
echo If ClamAV still fails to run:
echo   1. Verify KernelEX is installed and working
echo   2. Try manually right-clicking the .exe files
echo   3. Select Properties -^> Compatibility -^> Windows 2000
echo.
pause
