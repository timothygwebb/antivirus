"""
agents.repair_agent — AI agent for detecting installed browsers.

Checks common installation paths for Chrome, Firefox, Edge, and Opera and
returns a structured report suitable for use as an AI tool function.
"""

import os
from typing import List


# Known browser executable locations (Windows paths)
_BROWSER_LOCATIONS = {
    "Chrome": [
        r"C:\Program Files\Google\Chrome\Application\chrome.exe",
        r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
    ],
    "Firefox": [
        r"C:\Program Files\Mozilla Firefox\firefox.exe",
        r"C:\Program Files (x86)\Mozilla Firefox\firefox.exe",
    ],
    "Edge": [
        r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    ],
    "Opera": [
        r"C:\Program Files\Opera\opera.exe",
        r"C:\Program Files (x86)\Opera\opera.exe",
    ],
}


class RepairAgent:
    """Agent that detects installed browsers and reports their status.

    Example:
        agent = RepairAgent()
        result = agent.run()
    """

    def run(self) -> dict:
        """Detect installed browsers on the system.

        Returns:
            A dict with keys:
                status         (str)  — "ok" or "error"
                browsers_found (list) — names of detected browsers
                message        (str)  — human-readable summary
        """
        try:
            found: List[str] = []
            for browser, paths in _BROWSER_LOCATIONS.items():
                for path in paths:
                    if os.path.exists(path):
                        found.append(browser)
                        break

            if found:
                message = "Detected {} browser(s): {}.".format(len(found), ", ".join(found))
            else:
                message = "No supported browsers detected at known installation paths."

            return {
                "status": "ok",
                "browsers_found": found,
                "message": message,
            }
        except Exception as exc:
            return {
                "status": "error",
                "browsers_found": [],
                "message": "Unexpected error during browser detection: {}".format(exc),
            }
