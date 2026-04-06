# Agent Integration Guide

## Overview

The Python agent layer (`agents/`) provides a high-level interface for integrating AI agents with the antivirus tool. Each agent exposes simple, typed functions that can be registered as tools in agent frameworks (LangChain, OpenAI function calling, etc.).

## Quick Start

```python
from agents.scan_agent import ScanAgent
from agents.update_agent import UpdateAgent

# Update virus definitions first
update = UpdateAgent()
update.run()

# Run a full system scan
scan = ScanAgent()
result = scan.run(target="C:\\")
print(result)
```

## Available Agents

### ScanAgent (`agents/scan_agent.py`)

Runs a ClamAV scan on a specified path.

```python
from agents.scan_agent import ScanAgent

agent = ScanAgent()
result = agent.run(target="C:\\Users\\Public")
# Returns: {"status": "completed", "files_scanned": 1234, "threats": []}
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

Checks installed browsers and reports on their status.

```python
from agents.repair_agent import RepairAgent

agent = RepairAgent()
result = agent.run()
# Returns: {"browsers_found": ["Chrome", "Firefox"], "status": "ok"}
```

## CLI Usage

```bash
# Scan a specific path
python cli.py scan --target "C:\\"

# Update virus definitions
python cli.py update

# Check browsers
python cli.py repair

# Show help
python cli.py --help
```

## Registering as AI Tool Functions

All agent `run()` methods return plain dicts, making them easy to use as tool functions:

```python
import json
from agents.scan_agent import ScanAgent

def antivirus_scan(target: str) -> str:
    """Scan a path for malware using ClamAV."""
    result = ScanAgent().run(target=target)
    return json.dumps(result)
```
