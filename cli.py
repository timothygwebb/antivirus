"""
cli.py — Command-line entry point for the antivirus Python agent layer.

Usage:
    python cli.py scan --target <path>
    python cli.py update
    python cli.py repair
    python cli.py --help
"""

import argparse
import json
import sys


def cmd_scan(args):
    """Run a virus scan on the specified target path."""
    from agents.scan_agent import ScanAgent

    agent = ScanAgent()
    result = agent.run(target=args.target)
    print(json.dumps(result, indent=2))
    # Exit with non-zero code if threats were found or scan failed
    if result.get("threats") or result.get("status") not in ("completed",):
        return 1
    return 0


def _read_file_bytes(path):
    """Read a file as bytes, returning (data, error_result) tuple.

    Returns ``(bytes, None)`` on success or ``(None, dict)`` on failure.
    """
    try:
        with open(path, "rb") as fh:
            return fh.read(), None
    except OSError as exc:
        return None, {"status": "error", "files_scanned": 0, "threats": [], "message": str(exc)}


def cmd_sdk_scan(args):
    """Scan a file using the clamav-sdk REST client."""
    from agents.sdk_scan_agent import SDKScanAgent

    agent = SDKScanAgent(url=args.url or None)

    if args.mode in ("bytes", "stream"):
        data, err = _read_file_bytes(args.target)
        if err is not None:
            print(json.dumps(err, indent=2))
            return 1
        if args.mode == "bytes":
            result = agent.scan_bytes(data, filename=args.target)
        else:
            result = agent.scan_stream(data)
    else:
        result = agent.scan_file(args.target)

    print(json.dumps(result, indent=2))
    # Exit 1 when threats are found or an error occurred; 0 on clean scan
    if result.get("threats") or result.get("status") not in ("completed",):
        return 1
    return 0


def cmd_update(args):
    """Update virus definitions."""
    from agents.update_agent import UpdateAgent

    agent = UpdateAgent()
    result = agent.run()
    print(json.dumps(result, indent=2))
    if result.get("status") == "error":
        return 1
    return 0


def cmd_repair(args):
    """Detect installed browsers."""
    from agents.repair_agent import RepairAgent

    agent = RepairAgent()
    result = agent.run()
    print(json.dumps(result, indent=2))
    if result.get("status") == "error":
        return 1
    return 0


def build_parser():
    """Build and return the argument parser."""
    parser = argparse.ArgumentParser(
        prog="cli.py",
        description="Antivirus agent CLI — interact with the antivirus tool programmatically.",
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    # scan sub-command
    scan_parser = subparsers.add_parser("scan", help="Scan a path for malware.")
    scan_parser.add_argument(
        "--target",
        required=True,
        help="File system path to scan recursively (e.g. C:\\ or /home/user).",
    )

    # sdk-scan sub-command
    sdk_scan_parser = subparsers.add_parser(
        "sdk-scan",
        help="Scan a file using the clamav-sdk REST client (requires a running ClamAV API service).",
    )
    sdk_scan_parser.add_argument(
        "--target",
        required=True,
        help="File path to scan (e.g. /path/to/file.pdf).",
    )
    sdk_scan_parser.add_argument(
        "--url",
        default="",
        help="ClamAV API service base URL (default: value of CLAMAV_API_URL env var or http://localhost:6000).",
    )
    sdk_scan_parser.add_argument(
        "--mode",
        choices=["file", "bytes", "stream"],
        default="file",
        help="Scan mode: file (default), bytes (in-memory), or stream.",
    )

    # update sub-command
    subparsers.add_parser("update", help="Update ClamAV virus definitions.")

    # repair sub-command
    subparsers.add_parser("repair", help="Detect installed browsers.")

    return parser


def main():
    """Entry point for the CLI."""
    parser = build_parser()
    args = parser.parse_args()

    dispatch = {
        "scan": cmd_scan,
        "sdk-scan": cmd_sdk_scan,
        "update": cmd_update,
        "repair": cmd_repair,
    }

    handler = dispatch.get(args.command)
    if handler is None:
        parser.print_help()
        sys.exit(1)

    sys.exit(handler(args))


if __name__ == "__main__":
    main()
