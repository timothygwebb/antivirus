"""
agents.scan_agent — AI agent for running ClamAV virus scans.

Wraps the core executor and parser to provide a clean `run()` interface
suitable for use as a tool function in AI agent frameworks.
"""

import os
from typing import Optional

from core.config import CLAMSCAN_EXE, DATABASE_DIR, DEFAULT_SCAN_TIMEOUT
from core.executor import run_antivirus
from core.parser import parse_scan_output


class ScanAgent:
    """Agent that performs a ClamAV virus scan on a target path.

    Example:
        agent = ScanAgent()
        result = agent.run(target="C:\\\\Users\\\\Public")
    """

    def run(self, target: str, timeout: Optional[int] = None) -> dict:
        """Run a ClamAV scan on the specified target path.

        Args:
            target: The file system path to scan recursively.
            timeout: Maximum seconds to wait. Defaults to DEFAULT_SCAN_TIMEOUT.

        Returns:
            A dict with keys: status, files_scanned, threats, message.
        """
        if timeout is None:
            timeout = DEFAULT_SCAN_TIMEOUT

        try:
            if not os.path.exists(CLAMSCAN_EXE):
                return {
                    "status": "error",
                    "files_scanned": 0,
                    "threats": [],
                    "message": (
                        "clamscan.exe not found at '{}'. "
                        "Please run the definition updater first.".format(CLAMSCAN_EXE)
                    ),
                }

            args = [
                CLAMSCAN_EXE,
                "--recursive",
                "--database={}".format(DATABASE_DIR),
                target,
            ]
            returncode, stdout, stderr = run_antivirus(args, timeout=timeout)

            result = parse_scan_output(stdout)

            # clamscan exits with 1 when infections are found — treat as completed
            if returncode not in (0, 1) and result["status"] != "error":
                result["status"] = "failed"
                result["message"] += " Exit code: {}.".format(returncode)
                if stderr:
                    result["message"] += " stderr: {}".format(stderr.strip())

            return result

        except Exception as exc:
            return {
                "status": "error",
                "files_scanned": 0,
                "threats": [],
                "message": "Unexpected error during scan: {}".format(exc),
            }
