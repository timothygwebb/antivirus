# antivirus
Antivirus for all systems.

# Antivirus Recovery Tool

## Bootable CD/USB Usage

1. **Create a Bootable USB or CD:**
   - For USB: Use [Rufus](https://rufus.ie/) or similar to create a bootable Windows PE or minimal Linux environment.
   - For CD: Use your favorite CD burning tool to create a bootable disc with Windows PE or a minimal Linux live CD.

2. **Copy Files:**
   - Copy the entire antivirus tool folder (including ClamAV, browser installers, and all dependencies) to the root of the USB/CD.

3. **Boot the Target System:**
   - Boot the computer from the USB or CD (you may need to change the boot order in BIOS/UEFI).

4. **Run the Antivirus Tool:**
   - If the app does not start automatically, open the USB/CD in Explorer or a terminal and run `antivirus.exe` or use the provided `start-antivirus.bat` script.

5. **What the Tool Does:**
   - Scans for and removes malware, including MBR infections.
   - Attempts to repair browser access by downloading and installing legacy browsers if needed.
   - Logs all actions to `antivirus.log` in the current directory.

6. **No Internet?**
   - All required tools are included on the media. If network access is restored, the tool will attempt to update ClamAV and browsers automatically.

---

## Notes
- This tool is portable and does not require installation.
- For best results, always use the latest version of the tool and virus definitions.
- Use at your own risk. Always back up important data before making system changes.
