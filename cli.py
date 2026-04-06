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
