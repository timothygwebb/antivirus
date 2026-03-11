# Antivirus Solution

## Overview
The Antivirus Solution is a .NET Framework 2.0 application designed to scan and remove malware, repair browser access, and manage ClamAV virus definitions. The solution contains two projects:
- **antivirus.Legacy**: Main legacy antivirus application targeting .NET Framework 2.0 with full ClamAV integration
- **antivirus**: Modern antivirus implementation with enhanced features

## Features
- **Full System Scanning**: Scans entire drives using ClamAV's clamscan.exe with real-time progress updates
- **Real-Time Progress Tracking**: Live updates showing files scanned, threats found, elapsed time, and scan speed
- **Automatic ClamAV Download**: Automatically downloads and extracts portable ClamAV if not found locally
- **Virus Definition Updates**: Downloads latest virus signatures (3.6M+ signatures) via freshclam
- **Browser Repair**: Detects and reports on installed browsers (Chrome, Firefox, Edge, Opera)
- **Interactive Menu**: User-friendly console menu for all operations
- **Command-Line Support**: `--scan-all` and `--browser-repair` arguments for automation
- **Comprehensive Logging**: All operations logged to `antivirus.log` in the application directory
- **No Admin Required**: Uses local portable ClamAV installation to avoid permission issues

## Requirements
- .NET Framework 2.0 or higher
- **Windows XP or later** (or Windows ME with KernelEX - see below)
- `curl` command-line tool (typically pre-installed on Windows 10+)
- PowerShell (for ZIP extraction of portable ClamAV)
- Internet connection (for first-time ClamAV download and virus definition updates)

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

### antivirus.Legacy Project
```
antivirus.Legacy/
├── Program.cs                     # Main entry point with menu system and ClamAV management
├── antivirus.Legacy/
│   ├── Scanner.cs                 # Real-time ClamAV scanning with progress monitoring
│   └── Logger.cs                  # Logging implementation
├── ClamAV/                        # Auto-downloaded portable ClamAV installation
│   ├── clamscan.exe              # ClamAV scanner executable
│   ├── freshclam.exe             # Virus definition updater
│   ├── freshclam.conf            # Configuration for definition updates
│   └── database/                 # Virus signature databases (3.6M+ signatures)
│       ├── main.cvd              # Main virus signatures
│       ├── daily.cvd             # Daily virus updates
│       └── bytecode.cvd          # Bytecode signatures
└── antivirus.log                 # Application log file
```

## Usage

### Interactive Menu Mode (Default)
Run without arguments to access the interactive menu:
```cmd
antivirus.exe
```

Menu Options:
1. **Full System Scan** - Scans C:\ drive recursively for malware with real-time progress
2. **Browser Repair** - Checks for and reports on installed browsers
3. **Update Virus Definitions** - Downloads/updates ClamAV virus signatures
4. **Exit** - Closes the application

### Command-Line Arguments

**Full System Scan:**
```cmd
antivirus.exe --scan-all
```

**Browser Repair Only:**
```cmd
antivirus.exe --browser-repair
```

## First-Time Setup

On first run, when selecting "Update Virus Definitions" (option 3):
1. Detects no local ClamAV installation
2. Downloads portable ClamAV (~217MB) from clamav.net
3. Extracts using PowerShell Expand-Archive
4. Configures local database directory
5. Downloads virus signatures (main.cvd, daily.cvd, bytecode.cvd)
6. ✓ Ready to scan!

**Important:** Always run option 3 (Update Virus Definitions) before your first scan!

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
- Run option 3 to automatically download portable ClamAV
- Requires internet connection and ~217MB download

**"clamscan.exe not found"**
- Run option 3 first to download and extract ClamAV
- Verify `./ClamAV/clamscan.exe` exists after update

**"Database not found"**
- Run option 3 to download virus signatures
- Check that `./ClamAV/database/` contains .cvd files

**Scan shows 0 infections but you suspect malware**
- Ensure virus definitions are up-to-date (run option 3)
- Check that 3.6M+ signatures loaded during update
- Some rootkits may hide from user-mode scanners

**Permission errors**
- Application uses local portable ClamAV to avoid admin requirements
- If errors persist, try running from a user-writable directory

## Technical Details

- **Scanner Type**: ClamAV clamscan.exe (standalone, no daemon required)
- **Virus Signatures**: 3.6+ million (main.cvd + daily.cvd + bytecode.cvd)
- **Scan Method**: Recursive directory traversal with real-time monitoring
- **Progress Updates**: Every 2 seconds via asynchronous output reading
- **Infection Detection**: Parses "FOUND" keywords from clamscan output
- **.NET Target**: Framework 2.0 for maximum compatibility

## Contributing
1. Fork the repository at https://github.com/timothygwebb/antivirus
2. Create a feature branch
3. Follow existing code style (C# 7.3, .NET Framework 2.0 compatibility)
4. Submit a pull request with clear descriptions

## License
This project is licensed under the MIT License. See the LICENSE file for details.
