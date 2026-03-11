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

# curl for Windows download URL (32-bit static build)
$curlVersion = "8.4.0"
$curlUrl = "https://curl.se/windows/dl-$curlVersion/curl-$curlVersion-win32-mingw.zip"
$tempZip = Join-Path $env:TEMP "curl-win32.zip"
$curlExePath = Join-Path $OutputDir "curl.exe"

Write-Host "[*] Downloading curl $curlVersion (32-bit) from curl.se..." -ForegroundColor Yellow
Write-Host "    URL: $curlUrl" -ForegroundColor Gray

try {
    # Download curl ZIP
    Invoke-WebRequest -Uri $curlUrl -OutFile $tempZip -UseBasicParsing
    Write-Host "[+] Download completed: $tempZip" -ForegroundColor Green
    
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
        $version = & $curlExePath --version | Select-Object -First 1
        Write-Host "[+] $version" -ForegroundColor Green
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
    Write-Host "The bundled curl.exe will be automatically used" -ForegroundColor Gray
    Write-Host "when system curl is not available." -ForegroundColor Gray
    
} catch {
    Write-Host ""
    Write-Host "[!] ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Alternative: Manually download curl.exe" -ForegroundColor Yellow
    Write-Host "  1. Visit: https://curl.se/windows/" -ForegroundColor Yellow
    Write-Host "  2. Download 32-bit static build" -ForegroundColor Yellow
    Write-Host "  3. Extract and copy curl.exe to: $OutputDir" -ForegroundColor Yellow
    exit 1
}
