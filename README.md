# Antivirus Solution

## Overview
The Antivirus Solution is a multi-project application designed to scan and remove malware, repair browser access, and manage ClamAV virus definitions. The solution contains two .NET projects and a Python agent layer:
- **antivirus.Legacy**: Interactive antivirus application targeting .NET Framework 2.0 with full ClamAV integration, designed for maximum compatibility from Windows XP through Windows 11
- **antivirus**: Modern .NET application that performs an automated scan flow: MBR check, ClamAV verification, path-based scanning, and automatic browser repair
- **Python agent layer** (`cli.py`, `agents/`, `core/`): Programmatic Python interface to ClamAV for use in automation and AI agent frameworks

## Features

### antivirus.Legacy (.NET)
- **Interactive Menu**: User-friendly console menu with Full System Scan, Browser Repair, Update Virus Definitions, and Exit
- **Command-Line Support**: `--scan-all` and `--browser-repair` arguments for automation
- **Full System Scanning**: Scans the C:\ drive using ClamAV's clamscan.exe with real-time progress updates
- **Real-Time Progress Tracking**: Live updates showing files scanned, threats found, elapsed time, and scan speed
- **Virus Definition Updates**: Downloads latest virus signatures via freshclam (option 3 in the menu)
- **Browser Presence Check**: Checks whether common browser executables (Chrome, Firefox, Edge, Opera) are present and reports whether repair appears necessary
- **Comprehensive Logging**: All operations logged to `antivirus.log` in the application directory
- **No Admin Required**: Uses local portable ClamAV installation to avoid permission issues

### antivirus (.NET, modern)
- **Automated Scan Flow**: On launch, checks MBR, verifies ClamAV installation and definitions, prompts for a scan path (defaults to the user's personal folder), runs the scan, and automatically launches browser repair on completion
- **Legacy OS Redirect**: Automatically delegates to `antivirus.Legacy.exe` when running on Windows ME/9x
- **Browser Repair Flag**: Accepts `--browser-repair` to run browser reinstallation only

### Python Agent Layer
- **Programmatic API**: Python classes (`ScanAgent`, `UpdateAgent`, `RepairAgent`, `SDKScanAgent`) with typed interfaces suitable for AI agent frameworks
- **CLI Entry Point**: `cli.py` with `scan`, `sdk-scan`, `update`, and `repair` sub-commands
- **Subprocess Scanner**: `ScanAgent` invokes `clamscan.exe` directly via the `core.executor` module
- **REST Scanner**: `SDKScanAgent` communicates with a running ClamAV REST API service (via `clamav-sdk`) instead of spawning a subprocess
- **Structured Output**: All agents return plain dicts compatible with LangChain, OpenAI function calling, and similar frameworks

## Requirements
- .NET Framework 2.0 or higher
- **Windows XP or later** (or Windows ME with KernelEX - see below)
- **curl** (bundled with application or system-installed)
- **PowerShell** (for ZIP extraction on Windows XP+, not required for Windows ME)
- Internet connection (for first-time ClamAV download and virus definition updates)

### Platform-Specific Requirements

| Platform | .NET 2.0 | curl | PowerShell | ZIP Extraction |
|----------|----------|------|------------|----------------|
| Windows ME | ✓ | Bundled | ❌ Not available | COM (Shell.Application) |
| Windows XP | ✓ | Bundled | ❌ Optional | COM (Shell.Application) |
| Windows Vista+ | ✓ | Bundled | ✓ | PowerShell (faster) |
| Windows 10+ | ✓ | Built-in | ✓ | PowerShell (faster) |

**Note:** The application automatically detects the OS and uses the appropriate extraction method:
- **Windows ME**: Uses COM-based extraction (no PowerShell attempt)
- **Windows XP+**: Tries PowerShell first, falls back to COM if unavailable

## Bundled Tools

The application includes **curl.exe** (32-bit static build) in the `Tools` directory for compatibility with systems that don't have curl pre-installed (Windows ME, XP, etc.).

### First-Time Setup (Developers/Distributors)

Before distributing, download curl.exe:

```powershell
cd antivirus.Legacy
.\Download-Curl.ps1
```

This downloads curl.exe (~3-4MB) to the `Tools` directory, which is automatically copied to the output when building.

### curl Priority Order

The application looks for curl in this order:
1. **Bundled**: `.\Tools\curl.exe` (recommended for portability)
2. **Local**: `.\curl.exe` (application directory)
3. **System**: curl in PATH (Windows 10+ default)

If none are found, downloads will fail with instructions to add curl.

## Platform Support

### ✅ Windows XP and Later
Fully supported out-of-the-box.

### ⚠️ Windows ME with KernelEX
**Windows ME CAN run modern ClamAV with KernelEX installed:**

1. **Install KernelEX**
   - Download from: http://kernelex.sourceforge.net/
   - Install the core package
   - Reboot Windows ME

2. **Configure ClamAV Executables with KernelEX**
   - After downloading ClamAV (option 3), navigate to:
     ```
     antivirus.Legacy\bin\Debug\net20\ClamAV\
     ```
   - Right-click `freshclam.exe` → Properties → Compatibility
   - Set KernelEX mode to **"Windows 2000"** or **"Windows XP"**
   - Apply
   - Repeat for `clamscan.exe`

3. **Run the Antivirus Tool**
   - ClamAV executables will now run using Windows XP API compatibility
   - Full scanning and definition updates supported

### ❌ Windows ME Without KernelEX
Not supported - modern ClamAV requires Windows XP APIs that are not available on stock Windows ME.

**Why KernelEX is needed:**
- Modern ClamAV 1.5.1 is compiled for Windows NT 5.1+ (XP)
- Windows ME is based on Windows 9x kernel (not NT)
- KernelEX provides the missing NT APIs (kernel32, advapi32, etc.)
- Without KernelEX, clamscan.exe will fail with "not a valid Win32 application"

## Project Structure

### antivirus.Legacy Project (.NET)
```
antivirus.Legacy/
├── Program.cs                     # Main entry point, interactive menu, ClamAV management,
│                                  # and BrowserRepair class (checks browser exe presence)
├── antivirus.Legacy/
│   ├── Scanner.cs                 # Real-time ClamAV scanning with progress monitoring
│   ├── Logger.cs                  # Logging implementation
│   ├── BUNDLING-CURL.md           # Guide for bundling curl.exe with the application
│   └── Tools/                     # Bundled tools directory (curl.exe added at build time;
│                                  #   not tracked in source control)
├── ClamAV/                        # Auto-downloaded portable ClamAV installation
│   ├── clamscan.exe               # ClamAV scanner executable
│   ├── freshclam.exe              # Virus definition updater
│   ├── freshclam.conf             # Configuration for definition updates
│   └── database/                  # Virus signature databases (3.6M+ signatures)
│       ├── main.cvd               # Main virus signatures
│       ├── daily.cvd              # Daily virus updates
│       └── bytecode.cvd           # Bytecode signatures
└── antivirus.log                  # Application log file
```

### antivirus Project (.NET, modern)
```
(repo root)
├── antivirus.csproj               # Project file
├── Program.cs                     # Entry point: MBR check → ClamAV bootstrap → scan → browser repair
├── Scanner.cs                     # ClamAV integration, auto-download, extraction, and definitions update
├── BrowserRepair.cs               # Attempts to reinstall browsers from local installers in BrowserInstallers/
│                                  # (browser detection stub always triggers reinstall attempt)
├── Logger.cs / ILogger.cs         # Logging abstractions and implementations
├── Quarantine.cs                  # Quarantine management
├── MBRChecker.cs                  # Master Boot Record inspection
└── ZipExtractor.cs                # ZIP/archive extraction utilities
```

### Python Agent Layer
```
antivirus/                         # Root of the repository
├── cli.py                         # CLI entry point (scan, sdk-scan, update, repair commands)
├── requirements.txt               # Python dependencies (clamav-sdk, pytest)
├── agents/
│   ├── scan_agent.py              # ScanAgent: invokes clamscan.exe as a subprocess
│   ├── sdk_scan_agent.py          # SDKScanAgent: scans via ClamAV REST API (clamav-sdk)
│   ├── update_agent.py            # UpdateAgent: updates definitions via freshclam.exe
│   └── repair_agent.py            # RepairAgent: detects installed browsers
└── core/
    ├── executor.py                # Subprocess wrapper for ClamAV binaries
    ├── parser.py                  # Parses clamscan and freshclam text output
    ├── config.py                  # Path constants (ClamAV dirs, executables, log file)
    └── clamav_sdk_client.py       # Thin wrapper around clamav-sdk REST client
```

## Usage

### antivirus.Legacy — Interactive Menu Mode (Default)
Run without arguments to access the interactive menu:
```cmd
antivirus.Legacy.exe
```

Menu Options:
1. **Full System Scan** - Scans C:\ drive recursively for malware with real-time progress
2. **Browser Repair** - Checks whether common browser executables (Chrome, Firefox, Edge, Opera) are present and reports the result
3. **Update Virus Definitions** - Downloads/updates ClamAV virus signatures
4. **Exit** - Closes the application

### antivirus.Legacy — Command-Line Arguments

**Full System Scan:**
```cmd
antivirus.Legacy.exe --scan-all
```

**Browser Repair Only:**
```cmd
antivirus.Legacy.exe --browser-repair
```

### antivirus (modern) — Automated Scan Flow
Run without arguments to start the automated scan flow:
```cmd
antivirus.exe
```

The application will:
1. Check the Master Boot Record (MBR) for infections
2. Verify ClamAV is installed; if `clamd.exe` is not found, automatically download and extract the portable ClamAV package (~217MB) and create default config files — exit with an error only if this bootstrap step fails
3. Attempt to update virus definitions via `freshclam` (skipped if a recent update was already performed)
4. Prompt for a scan path (press Enter to use the default: your personal folder)
5. Run the ClamAV scan on the specified path
6. Automatically launch browser repair on scan completion

**Browser Repair Only (antivirus modern):**
```cmd
antivirus.exe --browser-repair
```

### Python Agent Layer — CLI
```bash
# Scan a specific path using clamscan.exe
python cli.py scan --target "C:\\"

# Scan a file via the ClamAV REST API service
python cli.py sdk-scan --target "/path/to/file.pdf"
python cli.py sdk-scan --target "/path/to/file.pdf" --url "http://localhost:6000" --mode stream

# Update virus definitions
python cli.py update

# Detect installed browsers
python cli.py repair

# Show help
python cli.py --help
```

## First-Time Setup

### antivirus.Legacy — Download ClamAV and Virus Definitions
On first run, select **Update Virus Definitions** (option 3) in the menu:
1. Detects no local ClamAV installation
2. Downloads portable ClamAV (~217MB) from clamav.net
3. Extracts using PowerShell Expand-Archive (or COM Shell.Application on Windows XP/ME)
4. Configures local database directory
5. Downloads virus signatures (main.cvd, daily.cvd, bytecode.cvd)
6. ✓ Ready to scan!

**Important:** Always run option 3 (Update Virus Definitions) before your first scan!

### antivirus (modern) — Automatic ClamAV Bootstrap
The modern project verifies that ClamAV is available on startup. If ClamAV is missing, it will download and extract the portable ClamAV package automatically and then attempt to update the virus definitions. In other words, the modern application can bootstrap its own local ClamAV setup rather than requiring `antivirus.Legacy.exe` to be run first.

If automatic setup or definition updates fail, you can still use `antivirus.Legacy.exe` option 3 as a fallback (both projects share the same local ClamAV directory), or install ClamAV manually.

### Python Agent Layer — Install Dependencies
```bash
pip install -r requirements.txt
```

The `clamav-sdk` package is required only for `SDKScanAgent` / `sdk-scan` REST scanning (requires a running ClamAV REST API service). The `ScanAgent` subprocess mode works with just `clamscan.exe` on the path defined in `core/config.py`.

## Scanning Progress Display

During a full system scan, you'll see real-time updates:
```
Progress: 5,234 files | 0 threats | Elapsed: 00:00:10 | Speed: 523 files/sec
Progress: 12,891 files | 0 threats | Elapsed: 00:00:20 | Speed: 645 files/sec
⚠ INFECTED: C:\Temp\suspicious.exe
Progress: 18,456 files | 1 threats | Elapsed: 00:00:30 | Speed: 615 files/sec
```

Final results show:
```
========== SCAN RESULTS ==========
Status: COMPLETED
Directories Scanned: 250,961
Files Scanned: 1,168,673
Infections Found: 1
Files Quarantined: 0

⚠ WARNING: 1 threat(s) detected!
==================================
```

## Configuration

All configuration is automatic. The application creates:
- `./ClamAV/` - Portable ClamAV installation
- `./ClamAV/database/` - Virus signature databases
- `./ClamAV/freshclam.conf` - Update configuration
- `./antivirus.log` - Application log

## Troubleshooting

**"freshclam.exe not found"**
- Run option 3 in `antivirus.Legacy.exe` to automatically download portable ClamAV
- Requires internet connection and ~217MB download

**"clamscan.exe not found"**
- Run option 3 in `antivirus.Legacy.exe` first to download and extract ClamAV
- Verify `./ClamAV/clamscan.exe` exists after update

**"Database not found"**
- Run option 3 to download virus signatures
- Check that `./ClamAV/database/` contains .cvd files

**"ClamAV is not fully configured" (antivirus modern)**
- The modern project will attempt to download and extract ClamAV automatically if missing
- If the automatic download fails, check your internet connection and that ~217MB of disk space is available
- As a fallback, use `antivirus.Legacy.exe` option 3 to download ClamAV (both projects share the same ClamAV directory)

**Scan shows 0 infections but you suspect malware**
- Ensure virus definitions are up-to-date (run option 3)
- Check that 3.6M+ signatures loaded during update
- Some rootkits may hide from user-mode scanners

**Permission errors**
- Application uses local portable ClamAV to avoid admin requirements
- If errors persist, try running from a user-writable directory

**Python `sdk-scan` fails to connect**
- Ensure a ClamAV REST API service is running (default: `http://localhost:6000`)
- Set the `CLAMAV_API_URL` environment variable to point to the correct service URL

## Technical Details

- **Scanner Type**: ClamAV clamscan.exe (standalone, no daemon required)
- **Virus Signatures**: 3.6+ million (main.cvd + daily.cvd + bytecode.cvd)
- **Scan Method**: Recursive directory traversal with real-time monitoring
- **Progress Updates**: Every 2 seconds via asynchronous output reading
- **Infection Detection**: Parses "FOUND" keywords from clamscan output
- **.NET Target**: Framework 2.0 for maximum compatibility
- **Python Layer**: Agents use `core.executor` (subprocess) or `core.clamav_sdk_client` (REST via `clamav-sdk`) to invoke ClamAV

## Contributing
1. Fork the repository at https://github.com/timothygwebb/antivirus
2. Create a feature branch
3. Follow existing code style (C# 7.3, .NET Framework 2.0 compatibility)
4. Submit a pull request with clear descriptions

## License
This project is licensed under the MIT License. See the LICENSE file for details.
