"""
tests/test_sdk_scan_agent.py — unit tests for agents.sdk_scan_agent.
"""

from unittest.mock import MagicMock, patch

from agents.sdk_scan_agent import SDKScanAgent


def _scan_result(status="completed", threats=None, files_scanned=1, message="OK"):
    return {
        "status": status,
        "files_scanned": files_scanned,
        "threats": threats or [],
        "message": message,
    }


class TestSDKScanAgent:
    """Tests for SDKScanAgent."""

    def test_scan_file_returns_dict_with_required_keys(self):
        with patch("agents.sdk_scan_agent.ClamAVSDKClient") as MockSDK:
            MockSDK.return_value.scan_file.return_value = _scan_result()
            agent = SDKScanAgent(url="http://localhost:6000")
            result = agent.scan_file("/tmp/file.txt")
        assert "status" in result
        assert "files_scanned" in result
        assert "threats" in result
        assert "message" in result

    def test_scan_file_clean(self):
        with patch("agents.sdk_scan_agent.ClamAVSDKClient") as MockSDK:
            MockSDK.return_value.scan_file.return_value = _scan_result(status="completed")
            agent = SDKScanAgent()
            result = agent.scan_file("/tmp/clean.pdf")
        assert result["status"] == "completed"
        assert result["threats"] == []

    def test_scan_file_infected(self):
        with patch("agents.sdk_scan_agent.ClamAVSDKClient") as MockSDK:
            MockSDK.return_value.scan_file.return_value = _scan_result(
                status="infected", threats=["/tmp/bad.exe"]
            )
            agent = SDKScanAgent()
            result = agent.scan_file("/tmp/bad.exe")
        assert result["status"] == "infected"
        assert "/tmp/bad.exe" in result["threats"]

    def test_scan_bytes_returns_dict(self):
        with patch("agents.sdk_scan_agent.ClamAVSDKClient") as MockSDK:
            MockSDK.return_value.scan_bytes.return_value = _scan_result()
            agent = SDKScanAgent()
            result = agent.scan_bytes(b"content", filename="doc.txt")
        assert isinstance(result, dict)
        assert "status" in result

    def test_scan_stream_returns_dict(self):
        with patch("agents.sdk_scan_agent.ClamAVSDKClient") as MockSDK:
            MockSDK.return_value.scan_stream.return_value = _scan_result()
            agent = SDKScanAgent()
            result = agent.scan_stream(b"raw bytes")
        assert isinstance(result, dict)

    def test_health_returns_dict_with_healthy_key(self):
        with patch("agents.sdk_scan_agent.ClamAVSDKClient") as MockSDK:
            MockSDK.return_value.health.return_value = {"healthy": True, "message": "OK"}
            agent = SDKScanAgent()
            result = agent.health()
        assert "healthy" in result
        assert result["healthy"] is True

    def test_version_returns_dict_with_version_key(self):
        with patch("agents.sdk_scan_agent.ClamAVSDKClient") as MockSDK:
            MockSDK.return_value.version.return_value = {
                "version": "0.105.0", "commit": "abc", "build": "rel"
            }
            agent = SDKScanAgent()
            result = agent.version()
        assert "version" in result
        assert result["version"] == "0.105.0"

    def test_default_url_used_when_not_provided(self):
        with patch("agents.sdk_scan_agent.ClamAVSDKClient") as MockSDK:
            SDKScanAgent()
        MockSDK.assert_called_once_with(url=None)

    def test_custom_url_passed_to_client(self):
        with patch("agents.sdk_scan_agent.ClamAVSDKClient") as MockSDK:
            SDKScanAgent(url="http://custom:7000")
        MockSDK.assert_called_once_with(url="http://custom:7000")
