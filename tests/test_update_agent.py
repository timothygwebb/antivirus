"""
tests/test_update_agent.py — unit tests for agents.update_agent.
"""

from unittest.mock import patch, mock_open

from agents.update_agent import UpdateAgent


class TestUpdateAgent:
    """Tests for UpdateAgent.run()."""

    def test_run_returns_dict_with_required_keys(self):
        with patch("os.path.exists", return_value=True):
            with patch("os.makedirs"):
                with patch("agents.update_agent.run_antivirus", return_value=(0, "up-to-date\n", "")):
                    agent = UpdateAgent()
                    result = agent.run()
        assert "status" in result
        assert "message" in result

    def test_missing_freshclam_returns_error(self):
        with patch("os.path.exists", return_value=False):
            agent = UpdateAgent()
            result = agent.run()
        assert result["status"] == "error"
        assert "freshclam.exe not found" in result["message"]

    def test_already_current_status(self):
        with patch("os.path.exists", return_value=True):
            with patch("os.makedirs"):
                with patch("agents.update_agent.run_antivirus",
                           return_value=(0, "main.cvd is up-to-date\n", "")):
                    agent = UpdateAgent()
                    result = agent.run()
        assert result["status"] == "already_current"

    def test_nonzero_returncode_reports_error(self):
        with patch("os.path.exists", return_value=True):
            with patch("os.makedirs"):
                with patch("agents.update_agent.run_antivirus",
                           return_value=(1, "", "Network error")):
                    agent = UpdateAgent()
                    result = agent.run()
        assert result["status"] == "error"
        assert "Exit code: 1" in result["message"]

    def test_conf_created_when_missing(self):
        written = {}

        def fake_exists(path):
            # freshclam.exe exists; conf does not
            return "freshclam.exe" in path and "freshclam.conf" not in path

        m = mock_open()
        with patch("os.path.exists", side_effect=fake_exists):
            with patch("os.makedirs"):
                with patch("builtins.open", m):
                    with patch("agents.update_agent.run_antivirus",
                               return_value=(0, "up-to-date\n", "")):
                        agent = UpdateAgent()
                        agent.run()
        # open() should have been called to write the default conf
        m.assert_called_once()
