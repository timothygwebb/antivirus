# Python Agent API Reference

## Module: `agents.scan_agent`

### Class `ScanAgent`

Orchestrates ClamAV virus scanning by invoking `clamscan.exe` directly via `core.executor`.

#### `ScanAgent.run(target: str) -> dict`

Run a ClamAV scan on the given path.

**Parameters:**
- `target` (`str`): Path to scan (e.g. `"C:\\"` or `"/home/user"`).

**Returns:** `dict` with keys:
- `status` (`str`): `"completed"`, `"failed"`, or `"error"`.
- `files_scanned` (`int`): Number of files scanned.
- `threats` (`list[str]`): List of infected file paths found.
- `message` (`str`): Human-readable summary.

---

## Module: `agents.update_agent`

### Class `UpdateAgent`

Downloads and updates ClamAV virus definitions.

#### `UpdateAgent.run() -> dict`

**Returns:** `dict` with keys:
- `status` (`str`): `"updated"`, `"already_current"`, `"unknown"`, or `"error"`.
- `message` (`str`): Human-readable result message.

---

## Module: `agents.repair_agent`

### Class `RepairAgent`

Detects installed browsers and reports their status.

#### `RepairAgent.run() -> dict`

**Returns:** `dict` with keys:
- `status` (`str`): `"ok"` or `"error"`.
- `browsers_found` (`list[str]`): Names of detected browsers.
- `message` (`str`): Human-readable summary.

---

## Module: `core.executor`

### `run_antivirus(args: list, timeout: int = 3600) -> tuple`

Low-level helper that invokes a ClamAV binary (`clamscan.exe`, `freshclam.exe`, etc.) as a subprocess.

**Parameters:**
- `args` (`list`): Command-line arguments to pass.
- `timeout` (`int`): Maximum seconds to wait (default: 3600).

**Returns:** Tuple of `(returncode, stdout, stderr)`.

---

## Module: `core.parser`

### `parse_scan_output(output: str) -> dict`

Parses raw `clamscan` stdout into a structured result dict.

### `parse_update_output(output: str) -> dict`

Parses raw `freshclam` stdout into a structured result dict.
