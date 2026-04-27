# Agent Integration Guide

## Overview

The Python agent layer (`agents/`) provides a high-level interface for integrating AI agents with the antivirus tool. Each agent exposes simple, typed functions that can be registered as tools in agent frameworks (LangChain, OpenAI function calling, etc.).

Two scan transports are available:
- **Subprocess** (`ScanAgent`): Invokes `clamscan.exe` directly as a subprocess. No external service required.
- **REST** (`SDKScanAgent`): Communicates with a running ClamAV REST API service via the `clamav-sdk` package.

## Quick Start

```python
from agents.scan_agent import ScanAgent
from agents.update_agent import UpdateAgent

# Update virus definitions first
update = UpdateAgent()
update.run()

# Run a full system scan (subprocess mode)
scan = ScanAgent()
result = scan.run(target="C:\\")
print(result)
```

## Available Agents

### ScanAgent (`agents/scan_agent.py`)

Invokes `clamscan.exe` directly as a subprocess to scan a specified path.

```python
from agents.scan_agent import ScanAgent

agent = ScanAgent()
result = agent.run(target="C:\\Users\\Public")
# Returns: {"status": "completed", "files_scanned": 1234, "threats": []}
```

### SDKScanAgent (`agents/sdk_scan_agent.py`)

Scans files using the ClamAV REST API service (via `clamav-sdk`). Requires a running
ClamAV API service. Configure the service URL via the `CLAMAV_API_URL` environment
variable (default: `http://localhost:6000`).

```python
from agents.sdk_scan_agent import SDKScanAgent

agent = SDKScanAgent()  # uses CLAMAV_API_URL or http://localhost:6000

# Check service health
print(agent.health())   # {"healthy": True, "message": "..."}
print(agent.version())  # {"version": "...", "commit": "...", "build": "..."}

# Scan a file on disk
result = agent.scan_file("/path/to/file.pdf")

# Scan in-memory bytes
with open("/path/to/file.exe", "rb") as fh:
    data = fh.read()
result = agent.scan_bytes(data, filename="file.exe")

# Scan via stream endpoint
result = agent.scan_stream(data)
# All scan methods return: {"status": "completed"|"infected"|"error",
#                           "files_scanned": 1, "threats": [...], "message": "..."}
```

### UpdateAgent (`agents/update_agent.py`)

Downloads and updates virus definitions via `freshclam`.

```python
from agents.update_agent import UpdateAgent

agent = UpdateAgent()
result = agent.run()
# Returns: {"status": "updated", "message": "Definitions up-to-date"}
```

### RepairAgent (`agents/repair_agent.py`)

Checks installed browsers and reports on their status. Checks Chrome, Firefox, Edge,
and Opera at their known Windows installation paths.

```python
from agents.repair_agent import RepairAgent

agent = RepairAgent()
result = agent.run()
# Returns: {"browsers_found": ["Chrome", "Firefox"], "status": "ok", "message": "..."}
```

## CLI Usage

```bash
# Scan a specific path using clamscan.exe (subprocess mode)
python cli.py scan --target "C:\\"

# Scan a file via the ClamAV REST API service
python cli.py sdk-scan --target "/path/to/file.pdf"

# Scan with a custom API URL and using stream mode
python cli.py sdk-scan --target "/path/to/file.pdf" --url "http://myserver:6000" --mode stream

# Available sdk-scan modes: file (default), bytes, stream
python cli.py sdk-scan --target "/path/to/file.pdf" --mode bytes

# Update virus definitions
python cli.py update

# Detect installed browsers
python cli.py repair

# Show help
python cli.py --help
```

## Registering as AI Tool Functions

All agent `run()` methods return plain dicts, making them easy to use as tool functions:

```python
import json
from agents.scan_agent import ScanAgent
from agents.sdk_scan_agent import SDKScanAgent

def antivirus_scan(target: str) -> str:
    """Scan a path for malware using ClamAV (subprocess mode)."""
    result = ScanAgent().run(target=target)
    return json.dumps(result)

def antivirus_scan_file_rest(path: str) -> str:
    """Scan a single file for malware via the ClamAV REST API."""
    result = SDKScanAgent().scan_file(path)
    return json.dumps(result)
```
