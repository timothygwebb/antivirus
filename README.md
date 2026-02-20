# Antivirus Recovery Tool

## Overview
The Antivirus Recovery Tool is a cross-platform application designed to scan and remove malware, repair browser access, and manage ClamAV virus definitions. The tool is compatible with both modern operating systems (e.g., Windows 10/11) and older systems (e.g., Windows Me) through a dual OS compatibility mode.

## Features
- **Dual OS Compatibility**: Automatically detects the operating system and runs in compatibility mode for older systems.
- **Malware Scanning**: Scans for and removes malware, including MBR infections.
- **Browser Repair**: Repairs browser access by downloading and installing legacy browsers if needed. This process is now decoupled and runs as a separate process after a successful scan.
- **ClamAV Integration**: Ensures virus definitions are up-to-date and performs scans using ClamAV.
- **Logging**: Logs all actions to `antivirus.log` in the current directory.

## Usage

### Bootable CD/USB Usage

1. **Create a Bootable USB or CD:**
   - For USB: Use [Rufus](https://rufus.ie/) or similar to create a bootable Windows PE or minimal Linux environment.
   - For CD: Use your favorite CD burning tool to create a bootable disc with Windows PE or a minimal Linux live CD.

2. **Copy Files:**
   - Copy the entire antivirus tool folder (including ClamAV, browser installers, .NET Framework 2.0 installer, and all dependencies) to the root of the USB/CD.

3. **Boot the Target System:**
   - Boot the computer from the USB or CD (you may need to change the boot order in BIOS/UEFI).

4. **Run the Antivirus Tool:**
   - If the app does not start automatically, open the USB/CD in Explorer or a terminal and run `antivirus.exe` or use the provided `start-antivirus.bat` script.

5. **What the Tool Does:**
   - Scans for and removes malware, including MBR infections.
   - Attempts to repair browser access by downloading and installing legacy browsers if needed. This step is now performed as a separate process after the scan completes.
   - Logs all actions to `antivirus.log` in the current directory.

6. **No Internet?**
   - All required tools are included on the media. If network access is restored, the tool will attempt to update ClamAV and browsers automatically.

---

## Development

### Project Structure
- **Program.cs**: Entry point of the application. Implements dual OS compatibility logic and launches browser repair as a separate process after a successful scan.
- **Scanner.cs**: Handles malware scanning and ClamAV integration. Ensures ClamAV daemon readiness and virus definitions.
- **MBRChecker.cs**: Checks and cleanses the Master Boot Record (MBR).
- **DefaultLogger.cs**: Provides logging functionality.
- **ClamAVDefinitionsManager.cs**: Manages ClamAV virus definitions.
- **Definitions.cs**: Handles virus definitions database.
- **BrowserRepair.cs**: Repairs browser access and installs legacy browsers. Now executed as a separate process.
- **Quarantine.cs**: Manages quarantining of infected files.
- **ILogger.cs**: Interface for logging.
- **Logger.cs**: Implements logging functionality.

### Compatibility Mode
- The application uses `_Backup` files for compatibility with older operating systems (e.g., Windows Me).
- The `Program.cs` file dynamically determines the OS version and loads the appropriate files.

### Building the Project
- The project targets `.NET 8` for modern systems.
- For compatibility with older systems, the `_Backup` files are designed to work with `.NET Framework 1.1`.

---

## Notes
- This tool is portable and does not require installation.
- For best results, always use the latest version of the tool and virus definitions.
- Use at your own risk. Always back up important data before making system changes.
