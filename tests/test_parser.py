"""
tests/test_parser.py — unit tests for core.parser.
"""

import sys
import os

# Ensure the project root is on the path
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from core.parser import parse_scan_output, parse_update_output


class TestParseScanOutput:
    """Tests for parse_scan_output."""

    def test_no_threats(self):
        output = (
            "----------- SCAN SUMMARY -----------\n"
            "Known viruses: 8686187\n"
            "Scanned files: 1000\n"
            "Infected files: 0\n"
        )
        result = parse_scan_output(output)
        assert result["status"] == "completed"
        assert result["files_scanned"] == 1000
        assert result["threats"] == []

    def test_with_threats(self):
        output = (
            "/tmp/eicar.txt: Eicar-Test-Sig FOUND\n"
            "----------- SCAN SUMMARY -----------\n"
            "Scanned files: 500\n"
            "Infected files: 1\n"
        )
        result = parse_scan_output(output)
        assert result["status"] == "completed"
        assert "/tmp/eicar.txt" in result["threats"]
        assert result["files_scanned"] == 500

    def test_multiple_threats(self):
        output = (
            "/a/file1.exe: Win.Trojan.Generic FOUND\n"
            "/b/file2.dll: Win.Trojan.Other FOUND\n"
            "Scanned files: 200\n"
        )
        result = parse_scan_output(output)
        assert len(result["threats"]) == 2
        assert "/a/file1.exe" in result["threats"]
        assert "/b/file2.dll" in result["threats"]

    def test_empty_output(self):
        result = parse_scan_output("")
        assert result["status"] == "completed"
        assert result["files_scanned"] == 0
        assert result["threats"] == []

    def test_message_included(self):
        result = parse_scan_output("Scanned files: 42\n")
        assert "42" in result["message"]


class TestParseUpdateOutput:
    """Tests for parse_update_output."""

    def test_already_up_to_date(self):
        output = "main.cvd is up-to-date\ndaily.cvd is up-to-date\n"
        result = parse_update_output(output)
        assert result["status"] == "already_current"

    def test_updated(self):
        output = "Downloading daily.cvd [100%]\nDatabase updated successfully.\n"
        result = parse_update_output(output)
        assert result["status"] in ("updated",)

    def test_empty_output(self):
        result = parse_update_output("")
        assert result["status"] in ("updated", "already_current", "error")
        assert "message" in result
