# Architecture Overview

## System Components

```
antivirus/  (repo root)
├── antivirus.Legacy/           # .NET Framework 2.0 interactive application
│   ├── Program.cs              # Entry point, interactive menu, ClamAV lifecycle,
│   │                           # and BrowserRepair class (checks browser exe presence)
│   └── antivirus.Legacy/
│       ├── Scanner.cs          # Real-time ClamAV scan orchestration
│       └── Logger.cs           # File-based logging
│
├── antivirus.csproj            # .NET modern application project file
├── Program.cs                  # Entry point: MBR → ClamAV auto-bootstrap → scan → browser repair
├── Scanner.cs                  # ClamAV integration, auto-download, extraction, and definitions update
├── BrowserRepair.cs            # Attempts reinstall of browsers from local installers
│                               # (browser detection stub — always triggers reinstall attempt)
├── MBRChecker.cs               # Master Boot Record inspection
│
├── agents/                     # Python AI agent layer
│   ├── scan_agent.py           # ScanAgent: invokes clamscan.exe as a subprocess
│   ├── sdk_scan_agent.py       # SDKScanAgent: scans via ClamAV REST API (clamav-sdk)
│   ├── update_agent.py         # Virus definition update agent
│   └── repair_agent.py         # Browser detection agent
│
├── core/                       # Python business logic (shared across agents)
│   ├── executor.py             # Subprocess executor for ClamAV binaries
│   ├── parser.py               # Output parsing utilities (clamscan, freshclam)
│   ├── config.py               # Configuration constants (paths, timeouts)
│   └── clamav_sdk_client.py    # Thin wrapper around clamav-sdk REST client
│
├── cli.py                      # Python CLI entry point
├── requirements.txt            # Python dependencies
└── docs/                       # Extended documentation
```

## Data Flow

### Subprocess Scan (ScanAgent / antivirus.Legacy)

```
User / AI Agent
      │
      ▼
cli.py (scan)  ────────────────────────────────────┐
      │                                            │
      ▼                                            ▼
agents/scan_agent.py         agents/update_agent.py    agents/repair_agent.py
      │                                │                         │
      └──────────────┬─────────────────┘                         │
                     ▼                                           ▼
              core/executor.py  ◄────────────────────── core/executor.py
                     │
         ┌──────────┴──────────┐
         ▼                     ▼
   clamscan.exe          freshclam.exe
   (ClamAV scanner)      (definition updater)
```

### REST Scan (SDKScanAgent)

```
User / AI Agent
      │
      ▼
cli.py (sdk-scan)
      │
      ▼
agents/sdk_scan_agent.py
      │
      ▼
core/clamav_sdk_client.py  (wraps clamav-sdk)
      │
      ▼
ClamAV REST API service  (http://localhost:6000 by default)
```

## Key Design Decisions

### Portable ClamAV

The application bundles a portable ClamAV installation rather than relying on a system-installed version. This:
- Avoids permission issues (no admin required).
- Ensures a consistent, known-good ClamAV version.
- Supports Windows XP through Windows 11.

### Dual Scan Transports (Python Layer)

The Python layer supports two scan transports:
- **Subprocess** (`ScanAgent`): Invokes `clamscan.exe` directly. No external services required; suitable for local use.
- **REST** (`SDKScanAgent`): Communicates with a ClamAV REST API service via the `clamav-sdk` package. Suitable for deployment scenarios where a dedicated scanning service is preferred. Configure the service URL via the `CLAMAV_API_URL` environment variable (default: `http://localhost:6000`).

Both transports return the same structured dict shape, so callers are transport-agnostic.

### Python Agent Layer

A thin Python layer wraps ClamAV binaries (`clamscan.exe`, `freshclam.exe`) directly via `core.executor`, or communicates with a REST API via `core.clamav_sdk_client`. This provides:
- A programmatic API suitable for AI agent frameworks.
- Structured output (parsed from ClamAV text output).
- Type-annotated interfaces for agent tool calling.

### Asynchronous Output Reading

The scanner reads `clamscan.exe` stdout/stderr asynchronously on background threads, delivering real-time progress updates without blocking the main thread.
