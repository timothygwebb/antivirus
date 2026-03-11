# Download curl.exe for bundling with antivirus application
# This script downloads a standalone 32-bit curl.exe for Windows compatibility

param(
    [string]$OutputDir = "Tools"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  curl.exe Download Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Create Tools directory if it doesn't exist
if (!(Test-Path $OutputDir)) {
    Write-Host "[*] Creating $OutputDir directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

$curlExePath = Join-Path $OutputDir "curl.exe"

# NOTE: For Windows ME compatibility with KernelEX, we prefer 32-bit curl
# The bundled curl.exe will be auto-configured with KernelEX by the application

# Check if Windows 10/11 system curl exists (skip on older systems for compatibility)
$systemCurl = "C:\Windows\System32\curl.exe"
$osVersion = [System.Environment]::OSVersion.Version

# Only use system curl on Windows 10+ (version 10.0+)
# Older versions get 32-bit download for maximum compatibility
$useSystemCurl = (Test-Path $systemCurl) -and ($osVersion.Major -ge 10)

if ($useSystemCurl) {
    Write-Host "[*] Found Windows 10/11 system curl.exe" -ForegroundColor Green
    Write-Host "[*] Copying from: $systemCurl" -ForegroundColor Yellow
    Write-Host "    Note: This 64-bit version won't work on Windows ME/XP" -ForegroundColor Gray
    Write-Host "    For legacy OS, download 32-bit version manually" -ForegroundColor Gray

    try {
        Copy-Item -Path $systemCurl -Destination $curlExePath -Force
        Write-Host "[+] curl.exe copied successfully!" -ForegroundColor Green

        # Get file info
        $fileInfo = Get-Item $curlExePath
        $fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
        Write-Host "    File size: $fileSizeMB MB" -ForegroundColor Gray

        # Verify it works
        Write-Host "[*] Verifying curl.exe..." -ForegroundColor Yellow
        $version = & $curlExePath --version | Select-Object -First 1
        Write-Host "[+] $version" -ForegroundColor Green

        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "  SUCCESS - curl.exe is ready!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Location: $curlExePath" -ForegroundColor Cyan
        Write-Host "Size: $fileSizeMB MB" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "For Windows ME/XP/Vista: Download 32-bit version instead" -ForegroundColor Yellow
        Write-Host "  Visit: https://curl.se/windows/ and get win32-mingw build" -ForegroundColor Gray
        Write-Host ""
        exit 0
    } catch {
        Write-Host "[!] Failed to copy system curl: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
    }
}

Write-Host "[*] Downloading 32-bit curl for maximum compatibility..." -ForegroundColor Yellow
Write-Host "    (Works on Windows ME with KernelEX, XP+, and modern Windows)" -ForegroundColor Gray
Write-Host ""

$tempZip = Join-Path $env:TEMP "curl-win32.zip"

# Try download sources with 32-bit builds (Windows ME compatible with KernelEX)
$downloadSources = @(
    @{
        Name = "curl.se (8.6.0 win32) - Windows ME compatible"
        Url = "https://curl.se/windows/dl-8.6.0/curl-8.6.0_3-win32-mingw.zip"
    },
    @{
        Name = "curl.se (8.5.0 win32) - Older stable"
        Url = "https://curl.se/windows/dl-8.5.0/curl-8.5.0_7-win32-mingw.zip"
    },
    @{
        Name = "curl.se (8.4.0 win32) - Fallback"
        Url = "https://curl.se/windows/dl-8.4.0/curl-8.4.0_7-win32-mingw.zip"
    }
)

$downloadSuccess = $false

foreach ($source in $downloadSources) {
    Write-Host "[*] Trying: $($source.Name)" -ForegroundColor Yellow
    Write-Host "    URL: $($source.Url)" -ForegroundColor Gray

    try {
        Invoke-WebRequest -Uri $source.Url -OutFile $tempZip -UseBasicParsing -TimeoutSec 30

        # Verify the file is a valid ZIP
        if ((Get-Item $tempZip).Length -lt 100KB) {
            Write-Host "[!] Downloaded file too small, likely not a valid ZIP" -ForegroundColor Red
            continue
        }

        Write-Host "[+] Download completed!" -ForegroundColor Green
        $downloadSuccess = $true
        break
    } catch {
        Write-Host "[!] Failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
    }
}

if (-not $downloadSuccess) {
    Write-Host ""
    Write-Host "[!] All download attempts failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "  MANUAL DOWNLOAD REQUIRED" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "EASIEST METHOD - Copy from Windows:" -ForegroundColor Cyan
    Write-Host "  Run this command in PowerShell:" -ForegroundColor Gray
    Write-Host "  Copy-Item C:\Windows\System32\curl.exe -Destination .\Tools\curl.exe" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "OR manually download:" -ForegroundColor Cyan
    Write-Host "  1. Visit: https://curl.se/windows/" -ForegroundColor Gray
    Write-Host "  2. Click on any build (win32-mingw or win64-mingw)" -ForegroundColor Gray
    Write-Host "  3. Download the ZIP file" -ForegroundColor Gray
    Write-Host "  4. Extract and find curl.exe inside bin\ folder" -ForegroundColor Gray
    Write-Host "  5. Copy curl.exe to: $((Get-Location).Path)\$OutputDir" -ForegroundColor Gray
    Write-Host ""
    Write-Host "After copying curl.exe, run: dotnet build" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

try {
    # Extract ZIP
    Write-Host "[*] Extracting ZIP file..." -ForegroundColor Yellow
    $tempExtract = Join-Path $env:TEMP "curl-extract"
    if (Test-Path $tempExtract) {
        Remove-Item $tempExtract -Recurse -Force
    }
    Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force

    # Find curl.exe in extracted files
    $curlExeSource = Get-ChildItem -Path $tempExtract -Filter "curl.exe" -Recurse | Select-Object -First 1

    if ($curlExeSource) {
        # Copy to Tools directory
        Copy-Item -Path $curlExeSource.FullName -Destination $curlExePath -Force
        Write-Host "[+] curl.exe copied to: $curlExePath" -ForegroundColor Green

        # Get file info
        $fileInfo = Get-Item $curlExePath
        $fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
        Write-Host "    File size: $fileSizeMB MB" -ForegroundColor Gray

        # Verify it works
        Write-Host "[*] Verifying curl.exe..." -ForegroundColor Yellow
        try {
            $version = & $curlExePath --version | Select-Object -First 1
            Write-Host "[+] $version" -ForegroundColor Green
        } catch {
            Write-Host "[!] Warning: Could not verify curl version, but file exists" -ForegroundColor Yellow
        }
    } else {
        throw "curl.exe not found in extracted files"
    }

    # Cleanup
    Write-Host "[*] Cleaning up temporary files..." -ForegroundColor Yellow
    Remove-Item $tempZip -Force -ErrorAction SilentlyContinue
    Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  SUCCESS - curl.exe is ready!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Location: $curlExePath" -ForegroundColor Cyan
    Write-Host "Size: $fileSizeMB MB" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "The bundled curl.exe will be automatically included" -ForegroundColor Gray
    Write-Host "in the build output when you compile the project." -ForegroundColor Gray
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "[!] ERROR during extraction: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "EASIEST FIX - Copy from Windows:" -ForegroundColor Cyan
    Write-Host "  Copy-Item C:\Windows\System32\curl.exe -Destination .\Tools\curl.exe" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
