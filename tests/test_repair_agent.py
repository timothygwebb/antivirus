"""
tests/test_repair_agent.py — unit tests for agents.repair_agent.
"""

from unittest.mock import patch

from agents.repair_agent import RepairAgent


class TestRepairAgent:
    """Tests for RepairAgent.run()."""

    def test_run_returns_dict_with_required_keys(self):
        agent = RepairAgent()
        result = agent.run()
        assert isinstance(result, dict)
        assert "status" in result
        assert "browsers_found" in result
        assert "message" in result

    def test_status_is_ok_on_success(self):
        agent = RepairAgent()
        result = agent.run()
        assert result["status"] == "ok"

    def test_browsers_found_is_list(self):
        agent = RepairAgent()
        result = agent.run()
        assert isinstance(result["browsers_found"], list)

    def test_browsers_detected_when_exe_exists(self):
        fake_path = "/fake/chrome.exe"
        patched_locations = {"Chrome": [fake_path]}
        with patch("agents.repair_agent._BROWSER_LOCATIONS", patched_locations):
            with patch("os.path.exists", return_value=True):
                agent = RepairAgent()
                result = agent.run()
        assert "Chrome" in result["browsers_found"]

    def test_no_browsers_when_none_exist(self):
        with patch("os.path.exists", return_value=False):
            agent = RepairAgent()
            result = agent.run()
        assert result["browsers_found"] == []
        assert result["status"] == "ok"
