# Bundling curl.exe with Antivirus Application

## Quick Start

### For Developers/Distributors

1. **Download curl.exe (one-time)**
   ```powershell
   cd antivirus.Legacy
   .\Download-Curl.ps1
   ```

2. **Build the project**
   ```powershell
   dotnet build
   ```

3. **curl.exe is automatically included** in output:
   ```
   bin\Debug\net20\
   ├── antivirus.Legacy.exe
   └── Tools\
       └── curl.exe    ← Bundled (3-4 MB)
   ```

### For End Users

**No action needed!** curl.exe is bundled with the application.

## How It Works

### curl Search Order
```
1. .\Tools\curl.exe       (Bundled - Always checked first)
2. .\curl.exe            (Local app directory)
3. curl (in PATH)         (System curl)
```

### Code Implementation
```csharp
private static string FindCurlExecutable()
{
    // Check bundled curl first
    string bundledCurl = Path.Combine(GetCurrentDirectory(), "Tools", "curl.exe");
    if (File.Exists(bundledCurl))
        return bundledCurl;  // ✓ Use bundled version
    
    // Fall back to system curl
    if (IsExecutableAvailable("curl"))
        return "curl";
    
    return null;  // Not found
}
```

## Why Bundle curl?

### Problem
- Windows ME, XP, Vista, 7, 8 don't have curl pre-installed
- Required for downloading ClamAV (217MB) and virus definitions
- Modern TLS/SSL needed for HTTPS downloads

### Solution
- Include standalone curl.exe (32-bit, ~3-4 MB)
- Works on all Windows versions (ME through 11)
- No user configuration required

## File Details

- **Version**: curl 8.4.0 (or latest)
- **Build**: Win32 static (32-bit)
- **Size**: ~3-4 MB
- **License**: MIT-style
- **TLS**: OpenSSL bundled
- **Source**: https://curl.se/windows/

## Distribution

When distributing the antivirus application:

✅ **Include**: `Tools\curl.exe` (automatically copied by build)
✅ **Works on**: Windows ME through Windows 11
✅ **No dependencies**: Static build with SSL support

## Manual curl.exe Download

If `Download-Curl.ps1` fails:

1. Visit https://curl.se/windows/
2. Download: `curl-8.4.0-win32-mingw.zip` (or latest 32-bit)
3. Extract and copy `curl.exe` to:
   ```
   antivirus.Legacy\Tools\curl.exe
   ```
4. Build project - curl.exe will be copied to output

## Troubleshooting

**curl.exe not found after build**
- Verify `Tools\curl.exe` exists in source
- Check project file includes `<None Include="Tools\**\*" />`
- Rebuild project

**"curl is not available" error at runtime**
- Check `bin\Debug\net20\Tools\curl.exe` exists
- Verify file is executable (~3-4 MB)
- Check antivirus didn't quarantine it

**Download-Curl.ps1 fails**
- Download manually from curl.se
- Copy to `Tools\curl.exe`
- Must be 32-bit version for Windows ME compatibility
