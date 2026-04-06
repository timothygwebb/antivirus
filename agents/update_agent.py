"""
agents.update_agent — AI agent for updating ClamAV virus definitions.

Wraps freshclam to download the latest virus signatures and returns a
structured result dict.
"""

import os
from typing import Optional

from core.config import FRESHCLAM_EXE, DATABASE_DIR, DEFAULT_UPDATE_TIMEOUT
from core.executor import run_antivirus
from core.parser import parse_update_output

# Minimal freshclam configuration written when no conf file is present.
_DEFAULT_CONF_TEMPLATE = (
    "DatabaseDirectory {db_dir}\n"
    "UpdateLogFile /dev/null\n"
    "DatabaseMirror database.clamav.net\n"
)


def _ensure_freshclam_conf(conf_path: str) -> None:
    """Create a minimal freshclam.conf at *conf_path* if it does not exist."""
    if not os.path.exists(conf_path):
        os.makedirs(os.path.dirname(conf_path), exist_ok=True)
        with open(conf_path, "w") as fh:
            fh.write(_DEFAULT_CONF_TEMPLATE.format(db_dir=DATABASE_DIR))


class UpdateAgent:
    """Agent that updates ClamAV virus definitions using freshclam.

    Example:
        agent = UpdateAgent()
        result = agent.run()
    """

    def run(self, timeout: Optional[int] = None) -> dict:
        """Download and update virus definitions.

        Args:
            timeout: Maximum seconds to wait. Defaults to DEFAULT_UPDATE_TIMEOUT.

        Returns:
            A dict with keys: status, message.
        """
        if timeout is None:
            timeout = DEFAULT_UPDATE_TIMEOUT

        try:
            if not os.path.exists(FRESHCLAM_EXE):
                return {
                    "status": "error",
                    "message": (
                        "freshclam.exe not found at '{}'. "
                        "Please download ClamAV first.".format(FRESHCLAM_EXE)
                    ),
                }

            os.makedirs(DATABASE_DIR, exist_ok=True)

            conf_path = os.path.join(os.path.dirname(FRESHCLAM_EXE), "freshclam.conf")
            _ensure_freshclam_conf(conf_path)

            args = [FRESHCLAM_EXE, "--config-file={}".format(conf_path)]

            returncode, stdout, stderr = run_antivirus(args, timeout=timeout)
            combined = stdout + stderr

            result = parse_update_output(combined)

            if returncode != 0:
                result["status"] = "error"
                message = result.get("message", "")
                details = ["Exit code: {}.".format(returncode)]
                if stderr:
                    details.append("stderr: {}".format(stderr.strip()))
                result["message"] = (message + " " + " ".join(details)).strip()

            return result

        except Exception as exc:
            return {
                "status": "error",
                "message": "Unexpected error during update: {}".format(exc),
            }
