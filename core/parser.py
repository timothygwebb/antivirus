"""
core.parser — Output parsers for clamscan and freshclam text output.

Converts the raw text output from ClamAV tools into structured dicts that
agents can return directly as their results.
"""

import re
from typing import List


def parse_scan_output(output: str) -> dict:
    """Parse raw clamscan stdout into a structured result dict.

    Args:
        output: The full stdout text from a clamscan run.

    Returns:
        A dict with keys:
            status        (str)  — "completed" or "error"
            files_scanned (int)  — number of files scanned
            threats       (list) — list of infected file paths
            message       (str)  — human-readable summary
    """
    threats: List[str] = []
    files_scanned = 0
    status = "completed"

    try:
        for line in output.splitlines():
            # Infected file lines look like: "/path/to/file: Eicar-Test-Sig FOUND"
            if line.strip().endswith("FOUND"):
                match = re.match(r"^(.+?):\s+\S+\s+FOUND$", line.strip())
                if match:
                    threats.append(match.group(1))

            # Summary line: "Scanned files: 12345"
            scanned_match = re.search(r"Scanned files:\s*(\d+)", line)
            if scanned_match:
                files_scanned = int(scanned_match.group(1))

        message = "Scan completed. Files scanned: {}. Threats found: {}.".format(
            files_scanned, len(threats)
        )
    except Exception as exc:
        status = "error"
        message = "Error parsing scan output: {}".format(exc)

    return {
        "status": status,
        "files_scanned": files_scanned,
        "threats": threats,
        "message": message,
    }


def parse_update_output(output: str) -> dict:
    """Parse raw freshclam stdout into a structured result dict.

    Args:
        output: The full stdout text from a freshclam run.

    Returns:
        A dict with keys:
            status  (str) — "updated", "already_current", or "error"
            message (str) — human-readable summary
    """
    try:
        lower = output.lower()
        if "up-to-date" in lower or "already up to date" in lower:
            return {"status": "already_current", "message": "Virus definitions are already up-to-date."}
        if "updated" in lower or "database updated" in lower:
            return {"status": "updated", "message": "Virus definitions updated successfully."}
        return {"status": "updated", "message": output.strip() or "Update completed."}
    except Exception as exc:
        return {"status": "error", "message": "Error parsing update output: {}".format(exc)}
