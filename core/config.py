"""
core.config — Configuration constants for the antivirus agent layer.

All paths are relative to the application working directory, consistent with
the .NET project conventions.
"""

import os

# Base directory for the .NET executable (antivirus.Legacy output)
LEGACY_BIN_DIR = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "antivirus.Legacy", "bin", "Debug", "net20",
)

# Antivirus executable name
ANTIVIRUS_EXE = os.path.join(LEGACY_BIN_DIR, "antivirus.Legacy.exe")

# ClamAV installation directory (relative to the .NET exe working directory)
CLAMAV_DIR = os.path.join(LEGACY_BIN_DIR, "ClamAV")

# clamscan / freshclam executables
CLAMSCAN_EXE = os.path.join(CLAMAV_DIR, "clamscan.exe")
FRESHCLAM_EXE = os.path.join(CLAMAV_DIR, "freshclam.exe")

# Virus definition database directory
DATABASE_DIR = os.path.join(CLAMAV_DIR, "database")

# Log file path
LOG_FILE = os.path.join(LEGACY_BIN_DIR, "antivirus.log")

# Default scan timeout in seconds (1 hour)
DEFAULT_SCAN_TIMEOUT = 3600

# Default update timeout in seconds (10 minutes)
DEFAULT_UPDATE_TIMEOUT = 600
