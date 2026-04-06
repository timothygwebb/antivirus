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
            args = [FRESHCLAM_EXE, "--config-file={}".format(conf_path)]

            returncode, stdout, stderr = run_antivirus(args, timeout=timeout)
            combined = stdout + stderr

            result = parse_update_output(combined)

            if returncode not in (0,) and result["status"] not in ("updated", "already_current"):
                result["status"] = "error"
                result["message"] += " Exit code: {}.".format(returncode)

            return result

        except Exception as exc:
            return {
                "status": "error",
                "message": "Unexpected error during update: {}".format(exc),
            }
