"""
agents.sdk_scan_agent — AI agent for running ClamAV scans via the clamav-sdk.

Uses the clamav-sdk REST client to communicate with a running ClamAV API
service instead of invoking clamscan.exe directly as a subprocess.  This makes
the scanning layer transport-agnostic and suitable for deployment scenarios
where a dedicated scanning service is preferred.

Configure the ClamAV API service URL via the CLAMAV_API_URL environment
variable (default: http://localhost:6000).
"""

from typing import Optional

from core.clamav_sdk_client import ClamAVSDKClient


class SDKScanAgent:
    """Agent that scans files using the clamav-sdk REST client.

    Unlike :class:`agents.scan_agent.ScanAgent`, this agent does not invoke
    clamscan.exe as a subprocess.  Instead it talks to a ClamAV REST API
    service via the ``clamav-sdk`` package.

    Example::

        agent = SDKScanAgent()
        result = agent.scan_file("/path/to/file.pdf")
        print(result["status"], result["threats"])
    """

    def __init__(self, url: Optional[str] = None) -> None:
        """Initialise the agent.

        Args:
            url: Base URL of the ClamAV REST API service.
                 Falls back to the ``CLAMAV_API_URL`` environment variable, or
                 ``http://localhost:6000`` if neither is set.
        """
        self._client = ClamAVSDKClient(url=url)

    # ------------------------------------------------------------------
    # Diagnostics
    # ------------------------------------------------------------------

    def health(self) -> dict:
        """Check whether the ClamAV API service is reachable and healthy.

        Returns:
            ``{"healthy": bool, "message": str}``
        """
        return self._client.health()

    def version(self) -> dict:
        """Return the ClamAV version reported by the API service.

        Returns:
            ``{"version": str, "commit": str, "build": str}``
        """
        return self._client.version()

    # ------------------------------------------------------------------
    # Scanning
    # ------------------------------------------------------------------

    def scan_file(self, path: str) -> dict:
        """Scan a file on disk.

        Args:
            path: Absolute path to the file to scan.

        Returns:
            A dict with keys: status, files_scanned, threats, message.
        """
        return self._client.scan_file(path)

    def scan_bytes(self, data: bytes, filename: str = "") -> dict:
        """Scan in-memory bytes.

        Args:
            data:     Raw bytes to scan.
            filename: Optional logical filename for the payload.

        Returns:
            A dict with keys: status, files_scanned, threats, message.
        """
        return self._client.scan_bytes(data, filename=filename)

    def scan_stream(self, data: bytes) -> dict:
        """Scan raw bytes via the stream endpoint.

        Args:
            data: Raw bytes to send to the stream endpoint.

        Returns:
            A dict with keys: status, files_scanned, threats, message.
        """
        return self._client.scan_stream(data)
