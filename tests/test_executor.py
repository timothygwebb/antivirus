"""
tests/test_executor.py — unit tests for core.executor.
"""

from unittest.mock import patch, MagicMock

from core.executor import run_antivirus


class TestRunAntivirus:
    """Tests for run_antivirus()."""

    def test_returns_tuple_of_three(self):
        mock_result = MagicMock()
        mock_result.returncode = 0
        mock_result.stdout = b"hello"
        mock_result.stderr = b""
        with patch("subprocess.run", return_value=mock_result):
            rc, stdout, stderr = run_antivirus(["echo", "hello"])
        assert isinstance(rc, int)
        assert isinstance(stdout, str)
        assert isinstance(stderr, str)

    def test_returncode_passed_through(self):
        mock_result = MagicMock()
        mock_result.returncode = 42
        mock_result.stdout = b""
        mock_result.stderr = b""
        with patch("subprocess.run", return_value=mock_result):
            rc, _, _ = run_antivirus(["some_exe"])
        assert rc == 42

    def test_timeout_returns_negative_one(self):
        import subprocess

        with patch("subprocess.run", side_effect=subprocess.TimeoutExpired(cmd="x", timeout=1)):
            rc, stdout, stderr = run_antivirus(["slow_exe"], timeout=1)
        assert rc == -1
        assert "timed out" in stderr.lower()

    def test_os_error_returns_negative_one(self):
        with patch("subprocess.run", side_effect=OSError("not found")):
            rc, stdout, stderr = run_antivirus(["missing_exe"])
        assert rc == -1
        assert stderr != ""

    def test_stdout_decoded_to_string(self):
        mock_result = MagicMock()
        mock_result.returncode = 0
        mock_result.stdout = "scanned files: 5\n".encode("utf-8")
        mock_result.stderr = b""
        with patch("subprocess.run", return_value=mock_result):
            _, stdout, _ = run_antivirus(["exe"])
        assert isinstance(stdout, str)
        assert "scanned" in stdout
