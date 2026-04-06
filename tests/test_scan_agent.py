"""
tests/test_scan_agent.py — unit tests for agents.scan_agent.
"""

from unittest.mock import patch

from agents.scan_agent import ScanAgent


class TestScanAgent:
    """Tests for ScanAgent.run()."""

    def test_run_returns_dict_with_required_keys(self):
        with patch("os.path.exists", return_value=True):
            with patch("agents.scan_agent.run_antivirus", return_value=(0, "Scanned files: 5\n", "")):
                agent = ScanAgent()
                result = agent.run(target="C:\\")
        assert "status" in result
        assert "files_scanned" in result
        assert "threats" in result
        assert "message" in result

    def test_missing_clamscan_returns_error(self):
        with patch("os.path.exists", return_value=False):
            agent = ScanAgent()
            result = agent.run(target="C:\\")
        assert result["status"] == "error"
        assert "clamscan.exe not found" in result["message"]

    def test_infected_file_appears_in_threats(self):
        output = "/tmp/bad.exe: Eicar-Test-Sig FOUND\nScanned files: 1\n"
        with patch("os.path.exists", return_value=True):
            with patch("agents.scan_agent.run_antivirus", return_value=(1, output, "")):
                agent = ScanAgent()
                result = agent.run(target="/tmp")
        assert "/tmp/bad.exe" in result["threats"]

    def test_clean_scan_has_no_threats(self):
        output = "Scanned files: 100\nInfected files: 0\n"
        with patch("os.path.exists", return_value=True):
            with patch("agents.scan_agent.run_antivirus", return_value=(0, output, "")):
                agent = ScanAgent()
                result = agent.run(target="/tmp")
        assert result["threats"] == []
        assert result["status"] == "completed"

    def test_executor_error_returns_failed_status(self):
        with patch("os.path.exists", return_value=True):
            with patch("agents.scan_agent.run_antivirus", return_value=(-1, "", "timed out")):
                agent = ScanAgent()
                result = agent.run(target="/tmp")
        assert result["status"] in ("failed", "error")
