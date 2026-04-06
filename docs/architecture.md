# Architecture Overview

## System Components

```
antivirus/
├── antivirus.Legacy/       # .NET Framework 2.0 core application
│   ├── Program.cs          # Entry point, interactive menu, ClamAV lifecycle
│   └── antivirus.Legacy/
│       ├── Scanner.cs      # Real-time ClamAV scan orchestration
│       └── Logger.cs       # File-based logging
│
├── agents/                 # Python AI agent layer
│   ├── scan_agent.py       # Scan orchestration agent
│   ├── update_agent.py     # Virus definition update agent
│   └── repair_agent.py     # Browser repair agent
│
├── core/                   # Python business logic (shared across agents)
│   ├── executor.py         # Subprocess executor for ClamAV binaries
│   ├── parser.py           # Output parsing utilities
│   └── config.py           # Configuration constants
│
├── cli.py                  # Python CLI entry point
├── requirements.txt        # Python dependencies
└── docs/                   # Extended documentation
```

## Data Flow

```
User / AI Agent
      │
      ▼
  cli.py  ──────────────────────────────────────────────────────┐
      │                                                          │
      ▼                                                          ▼
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

## Key Design Decisions

### Portable ClamAV

The application bundles a portable ClamAV installation rather than relying on a system-installed version. This:
- Avoids permission issues (no admin required).
- Ensures a consistent, known-good ClamAV version.
- Supports Windows XP through Windows 11.

### Python Agent Layer

A thin Python layer wraps ClamAV binaries (`clamscan.exe`, `freshclam.exe`) directly via `core.executor`. This provides:
- A programmatic API suitable for AI agent frameworks.
- Structured output (parsed from ClamAV text output).
- Type-annotated interfaces for agent tool calling.

### Asynchronous Output Reading

The scanner reads `clamscan.exe` stdout/stderr asynchronously on background threads, delivering real-time progress updates without blocking the main thread.
