"""
tests/test_clamav_sdk_client.py — unit tests for core.clamav_sdk_client.
"""

from unittest.mock import MagicMock, patch

import pytest

from core.clamav_sdk_client import ClamAVSDKClient, _error_result


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_sdk_result(status="clean", message="OK", filename="test.txt"):
    r = MagicMock()
    r.status = status
    r.message = message
    r.filename = filename
    r.scan_time = 0.01
    return r


def _make_health_result(healthy=True, message="OK"):
    r = MagicMock()
    r.healthy = healthy
    r.message = message
    return r


def _make_version_result(version="1.0.0", commit="abc", build="build1"):
    r = MagicMock()
    r.version = version
    r.commit = commit
    r.build = build
    return r


# ---------------------------------------------------------------------------
# _error_result helper
# ---------------------------------------------------------------------------

class TestErrorResult:
    def test_structure(self):
        result = _error_result("something went wrong")
        assert result["status"] == "error"
        assert result["files_scanned"] == 0
        assert result["threats"] == []
        assert "something went wrong" in result["message"]


# ---------------------------------------------------------------------------
# ClamAVSDKClient.health
# ---------------------------------------------------------------------------

class TestHealth:
    def test_healthy_true(self):
        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.health_check.return_value = _make_health_result(healthy=True, message="all good")
            client = ClamAVSDKClient("http://localhost:6000")
            result = client.health()
        assert result["healthy"] is True
        assert "all good" in result["message"]

    def test_connection_error_returns_unhealthy(self):
        from clamav_sdk.exceptions import ClamAVConnectionError

        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.health_check.side_effect = ClamAVConnectionError("refused")
            client = ClamAVSDKClient("http://localhost:6000")
            result = client.health()
        assert result["healthy"] is False
        assert "Connection error" in result["message"]

    def test_generic_error_returns_unhealthy(self):
        from clamav_sdk.exceptions import ClamAVError

        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.health_check.side_effect = ClamAVError("boom")
            client = ClamAVSDKClient()
            result = client.health()
        assert result["healthy"] is False


# ---------------------------------------------------------------------------
# ClamAVSDKClient.version
# ---------------------------------------------------------------------------

class TestVersion:
    def test_returns_version_fields(self):
        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.version.return_value = _make_version_result("0.105.0", "deadbeef", "rel")
            client = ClamAVSDKClient()
            result = client.version()
        assert result["version"] == "0.105.0"
        assert result["commit"] == "deadbeef"
        assert result["build"] == "rel"

    def test_error_includes_error_key(self):
        from clamav_sdk.exceptions import ClamAVError

        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.version.side_effect = ClamAVError("nope")
            client = ClamAVSDKClient()
            result = client.version()
        assert "error" in result
        assert result["version"] == ""


# ---------------------------------------------------------------------------
# ClamAVSDKClient.scan_file
# ---------------------------------------------------------------------------

class TestScanFile:
    def test_clean_file(self):
        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.scan_file.return_value = _make_sdk_result(status="clean")
            client = ClamAVSDKClient()
            result = client.scan_file("/tmp/safe.pdf")
        assert result["status"] == "completed"
        assert result["threats"] == []
        assert result["files_scanned"] == 1

    def test_infected_file_appears_in_threats(self):
        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.scan_file.return_value = _make_sdk_result(status="infected")
            client = ClamAVSDKClient()
            result = client.scan_file("/tmp/bad.exe")
        assert result["status"] == "infected"
        assert "/tmp/bad.exe" in result["threats"]

    def test_connection_error(self):
        from clamav_sdk.exceptions import ClamAVConnectionError

        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.scan_file.side_effect = ClamAVConnectionError("refused")
            client = ClamAVSDKClient()
            result = client.scan_file("/tmp/file.txt")
        assert result["status"] == "error"
        assert "Connection error" in result["message"]

    def test_timeout_error(self):
        from clamav_sdk.exceptions import ClamAVTimeoutError

        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.scan_file.side_effect = ClamAVTimeoutError("timeout")
            client = ClamAVSDKClient()
            result = client.scan_file("/tmp/file.txt")
        assert result["status"] == "error"
        assert "timed out" in result["message"].lower()

    def test_file_too_large_error(self):
        from clamav_sdk.exceptions import ClamAVFileTooLargeError

        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.scan_file.side_effect = ClamAVFileTooLargeError("too big")
            client = ClamAVSDKClient()
            result = client.scan_file("/tmp/huge.iso")
        assert result["status"] == "error"
        assert "size limit" in result["message"].lower()

    def test_service_unavailable_error(self):
        from clamav_sdk.exceptions import ClamAVServiceUnavailableError

        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.scan_file.side_effect = ClamAVServiceUnavailableError("down")
            client = ClamAVSDKClient()
            result = client.scan_file("/tmp/file.txt")
        assert result["status"] == "error"
        assert "unavailable" in result["message"].lower()

    def test_bad_request_error(self):
        from clamav_sdk.exceptions import ClamAVBadRequestError

        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.scan_file.side_effect = ClamAVBadRequestError("bad")
            client = ClamAVSDKClient()
            result = client.scan_file("/tmp/file.txt")
        assert result["status"] == "error"
        assert "Bad request" in result["message"]


# ---------------------------------------------------------------------------
# ClamAVSDKClient.scan_bytes
# ---------------------------------------------------------------------------

class TestScanBytes:
    def test_clean_bytes(self):
        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.scan_bytes.return_value = _make_sdk_result(status="clean")
            client = ClamAVSDKClient()
            result = client.scan_bytes(b"hello", filename="hello.txt")
        assert result["status"] == "completed"
        assert result["threats"] == []

    def test_infected_bytes(self):
        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.scan_bytes.return_value = _make_sdk_result(
                status="infected", filename="evil.bin"
            )
            client = ClamAVSDKClient()
            result = client.scan_bytes(b"\x00\x01", filename="evil.bin")
        assert result["status"] == "infected"
        assert "evil.bin" in result["threats"]

    def test_connection_error(self):
        from clamav_sdk.exceptions import ClamAVConnectionError

        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.scan_bytes.side_effect = ClamAVConnectionError("refused")
            client = ClamAVSDKClient()
            result = client.scan_bytes(b"data")
        assert result["status"] == "error"


# ---------------------------------------------------------------------------
# ClamAVSDKClient.scan_stream
# ---------------------------------------------------------------------------

class TestScanStream:
    def test_clean_stream(self):
        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.scan_stream.return_value = _make_sdk_result(status="clean")
            client = ClamAVSDKClient()
            result = client.scan_stream(b"raw data")
        assert result["status"] == "completed"

    def test_infected_stream(self):
        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.scan_stream.return_value = _make_sdk_result(status="infected")
            client = ClamAVSDKClient()
            result = client.scan_stream(b"virus")
        assert result["status"] == "infected"

    def test_connection_error(self):
        from clamav_sdk.exceptions import ClamAVConnectionError

        with patch("core.clamav_sdk_client.ClamAVClient") as MockClient:
            MockClient.return_value.scan_stream.side_effect = ClamAVConnectionError("refused")
            client = ClamAVSDKClient()
            result = client.scan_stream(b"data")
        assert result["status"] == "error"
