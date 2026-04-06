"""
core.executor — Subprocess wrapper for invoking the antivirus executable.

Provides a thin, tested wrapper around subprocess so that agents do not need
to deal with process management directly.
"""

import subprocess
from typing import List, Tuple


def run_antivirus(args: List[str], timeout: int = 3600) -> Tuple[int, str, str]:
    """Invoke the antivirus executable (or clamscan directly) as a subprocess.

    Args:
        args: Command and arguments to execute, e.g.
              ["/path/to/clamscan.exe", "--recursive", "C:\\"]
        timeout: Maximum seconds to wait for the process to finish.
                 Defaults to 3600 (1 hour).

    Returns:
        A tuple of (returncode, stdout, stderr).
        On timeout or OS error, returncode is -1 and the error is in stderr.
    """
    try:
        result = subprocess.run(
            args,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=timeout,
        )
        stdout = result.stdout.decode("utf-8", errors="replace")
        stderr = result.stderr.decode("utf-8", errors="replace")
        return result.returncode, stdout, stderr
    except subprocess.TimeoutExpired as exc:
        stderr = "Process timed out after {} seconds: {}".format(timeout, exc)
        return -1, "", stderr
    except OSError as exc:
        stderr = "Failed to launch process {!r}: {}".format(args[0] if args else "", exc)
        return -1, "", stderr
