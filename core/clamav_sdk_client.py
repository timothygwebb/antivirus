"""
core.clamav_sdk_client — Thin wrapper around the clamav-sdk REST client.

Normalises SDK responses into the same structured dict shape used throughout
the rest of the agent layer so that callers are transport-agnostic.

Requires a running ClamAV API service (https://github.com/DevHatRo/ClamAV-API).
Configure the service URL via the CLAMAV_API_URL environment variable or pass
it explicitly to the client constructor.

Default URL: http://localhost:6000
"""

import os
from typing import Optional

from clamav_sdk import ClamAVClient
from clamav_sdk.exceptions import (
    ClamAVError,
    ClamAVConnectionError,
    ClamAVTimeoutError,
    ClamAVServiceUnavailableError,
    ClamAVFileTooLargeError,
    ClamAVBadRequestError,
)

# Default ClamAV API service URL.  Override with the CLAMAV_API_URL env var.
DEFAULT_API_URL = os.environ.get("CLAMAV_API_URL", "http://localhost:6000")


class ClamAVSDKClient:
    """Wrapper around :class:`clamav_sdk.ClamAVClient` (REST transport).

    All public methods return the same dict shape used by :class:`ScanAgent`
    so that callers need not know which transport is in use::

        {
            "status":        str,   # "completed" | "infected" | "error"
            "files_scanned": int,
            "threats":       list,  # infected filenames / identifiers
            "message":       str,
        }

    Example::

        client = ClamAVSDKClient()
        print(client.health())        # {"healthy": True, "message": "..."}
        print(client.version())       # {"version": "...", "commit": "...", "build": "..."}
        print(client.scan_file("/path/to/file.pdf"))
    """

    def __init__(self, url: Optional[str] = None) -> None:
        self._url = url or DEFAULT_API_URL
        self._client = ClamAVClient(self._url)

    # ------------------------------------------------------------------
    # Utility / diagnostics
    # ------------------------------------------------------------------

    def health(self) -> dict:
        """Return a health-check result dict.

        Returns:
            ``{"healthy": bool, "message": str}``
        """
        try:
            result = self._client.health_check()
            return {"healthy": result.healthy, "message": result.message}
        except ClamAVConnectionError as exc:
            return {"healthy": False, "message": "Connection error: {}".format(exc)}
        except ClamAVError as exc:
            return {"healthy": False, "message": "Health check failed: {}".format(exc)}

    def version(self) -> dict:
        """Return server version information.

        Returns:
            ``{"version": str, "commit": str, "build": str}``
        """
        try:
            info = self._client.version()
            return {
                "version": info.version,
                "commit": info.commit,
                "build": info.build,
            }
        except ClamAVError as exc:
            return {"version": "", "commit": "", "build": "", "error": str(exc)}

    # ------------------------------------------------------------------
    # Scanning helpers — all return the standard scan result dict
    # ------------------------------------------------------------------

    def _make_scan_result(self, sdk_result, filename: str = "") -> dict:
        """Convert a :class:`clamav_sdk.models.ScanResult` to a standard dict."""
        infected = sdk_result.status.lower() == "infected"
        threats = [filename or sdk_result.filename] if infected else []
        status = "infected" if infected else "completed"
        message = sdk_result.message or (
            "File is clean." if not infected else "Threat detected in '{}'.".format(
                filename or sdk_result.filename
            )
        )
        return {
            "status": status,
            "files_scanned": 1,
            "threats": threats,
            "message": message,
        }

    def scan_file(self, path: str) -> dict:
        """Scan a file on disk via the REST API.

        Args:
            path: Absolute path to the file to scan.

        Returns:
            Standard scan result dict.
        """
        try:
            result = self._client.scan_file(path)
            return self._make_scan_result(result, filename=path)
        except ClamAVFileTooLargeError as exc:
            return _error_result("File exceeds server size limit: {}".format(exc))
        except ClamAVTimeoutError as exc:
            return _error_result("Scan timed out: {}".format(exc))
        except ClamAVServiceUnavailableError as exc:
            return _error_result("ClamAV service unavailable: {}".format(exc))
        except ClamAVConnectionError as exc:
            return _error_result("Connection error: {}".format(exc))
        except ClamAVBadRequestError as exc:
            return _error_result("Bad request: {}".format(exc))
        except ClamAVError as exc:
            return _error_result("Scan error: {}".format(exc))

    def scan_bytes(self, data: bytes, filename: str = "") -> dict:
        """Scan in-memory bytes via the REST API.

        Args:
            data:     Raw bytes to scan.
            filename: Optional logical filename attached to the payload.

        Returns:
            Standard scan result dict.
        """
        try:
            result = self._client.scan_bytes(data, filename=filename)
            return self._make_scan_result(result, filename=filename)
        except ClamAVFileTooLargeError as exc:
            return _error_result("Payload exceeds server size limit: {}".format(exc))
        except ClamAVTimeoutError as exc:
            return _error_result("Scan timed out: {}".format(exc))
        except ClamAVServiceUnavailableError as exc:
            return _error_result("ClamAV service unavailable: {}".format(exc))
        except ClamAVConnectionError as exc:
            return _error_result("Connection error: {}".format(exc))
        except ClamAVBadRequestError as exc:
            return _error_result("Bad request: {}".format(exc))
        except ClamAVError as exc:
            return _error_result("Scan error: {}".format(exc))

    def scan_stream(self, data: bytes) -> dict:
        """Scan raw bytes via the stream endpoint of the REST API.

        Args:
            data: Raw bytes to scan.

        Returns:
            Standard scan result dict.
        """
        try:
            result = self._client.scan_stream(data)
            return self._make_scan_result(result)
        except ClamAVFileTooLargeError as exc:
            return _error_result("Stream exceeds server size limit: {}".format(exc))
        except ClamAVTimeoutError as exc:
            return _error_result("Scan timed out: {}".format(exc))
        except ClamAVServiceUnavailableError as exc:
            return _error_result("ClamAV service unavailable: {}".format(exc))
        except ClamAVConnectionError as exc:
            return _error_result("Connection error: {}".format(exc))
        except ClamAVBadRequestError as exc:
            return _error_result("Bad request: {}".format(exc))
        except ClamAVError as exc:
            return _error_result("Scan error: {}".format(exc))


def _error_result(message: str) -> dict:
    """Return a standard error scan result dict."""
    return {
        "status": "error",
        "files_scanned": 0,
        "threats": [],
        "message": message,
    }
