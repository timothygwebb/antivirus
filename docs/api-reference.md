# Python Agent API Reference

## Module: `agents.scan_agent`

### Class `ScanAgent`

Orchestrates ClamAV virus scanning by invoking `clamscan.exe` directly via `core.executor`.

#### `ScanAgent.run(target: str, timeout: int = None) -> dict`

Run a ClamAV scan on the given path.

**Parameters:**
- `target` (`str`): Path to scan (e.g. `"C:\\"` or `"/home/user"`).
- `timeout` (`int`, optional): Maximum seconds to wait. Defaults to `DEFAULT_SCAN_TIMEOUT` (3600).

**Returns:** `dict` with keys:
- `status` (`str`): `"completed"`, `"failed"`, or `"error"`.
- `files_scanned` (`int`): Number of files scanned.
- `threats` (`list[str]`): List of infected file paths found.
- `message` (`str`): Human-readable summary.

---

## Module: `agents.sdk_scan_agent`

### Class `SDKScanAgent`

Scans files using the ClamAV REST API service via `core.clamav_sdk_client`. Requires a
running ClamAV API service. Configure the service URL via the `CLAMAV_API_URL` environment
variable (default: `http://localhost:6000`).

#### `SDKScanAgent.__init__(url: str = None)`

**Parameters:**
- `url` (`str`, optional): Base URL of the ClamAV REST API service. Falls back to `CLAMAV_API_URL` env var, then `http://localhost:6000`.

#### `SDKScanAgent.health() -> dict`

Check whether the ClamAV API service is reachable and healthy.

**Returns:** `{"healthy": bool, "message": str}`

#### `SDKScanAgent.version() -> dict`

Return the ClamAV version reported by the API service.

**Returns:** `{"version": str, "commit": str, "build": str}`

#### `SDKScanAgent.scan_file(path: str) -> dict`

Scan a file on disk via the REST API.

**Parameters:**
- `path` (`str`): Absolute path to the file to scan.

**Returns:** Standard scan result dict (see below).

#### `SDKScanAgent.scan_bytes(data: bytes, filename: str = "") -> dict`

Scan in-memory bytes via the REST API.

**Parameters:**
- `data` (`bytes`): Raw bytes to scan.
- `filename` (`str`, optional): Logical filename for the payload.

**Returns:** Standard scan result dict (see below).

#### `SDKScanAgent.scan_stream(data: bytes) -> dict`

Scan raw bytes via the stream endpoint of the REST API.

**Parameters:**
- `data` (`bytes`): Raw bytes to send to the stream endpoint.

**Returns:** Standard scan result dict (see below).

**Standard scan result dict:**
- `status` (`str`): `"completed"`, `"infected"`, or `"error"`.
- `files_scanned` (`int`): Number of files scanned (always 1 for REST scans).
- `threats` (`list[str]`): Infected filenames / identifiers found.
- `message` (`str`): Human-readable summary.

---

## Module: `agents.update_agent`

### Class `UpdateAgent`

Downloads and updates ClamAV virus definitions using `freshclam.exe`.

#### `UpdateAgent.run(timeout: int = None) -> dict`

**Parameters:**
- `timeout` (`int`, optional): Maximum seconds to wait. Defaults to `DEFAULT_UPDATE_TIMEOUT` (600).

**Returns:** `dict` with keys:
- `status` (`str`): `"updated"`, `"already_current"`, `"unknown"`, or `"error"`.
- `message` (`str`): Human-readable result message.

---

## Module: `agents.repair_agent`

### Class `RepairAgent`

Detects installed browsers (Chrome, Firefox, Edge, Opera) by checking known Windows
installation paths.

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
- `args` (`list`): Command-line arguments to pass (first element is the executable).
- `timeout` (`int`): Maximum seconds to wait (default: 3600). On timeout, returns `(-1, "", error_message)`.

**Returns:** Tuple of `(returncode, stdout, stderr)`.

---

## Module: `core.clamav_sdk_client`

### Class `ClamAVSDKClient`

Thin wrapper around `clamav_sdk.ClamAVClient` (REST transport). Normalises SDK responses
into the standard dict shape used throughout the agent layer.

#### `ClamAVSDKClient.__init__(url: str = None)`

**Parameters:**
- `url` (`str`, optional): Base URL of the ClamAV REST API service. Falls back to `CLAMAV_API_URL` env var, then `http://localhost:6000`.

#### `ClamAVSDKClient.health() -> dict`

**Returns:** `{"healthy": bool, "message": str}`

#### `ClamAVSDKClient.version() -> dict`

**Returns:** `{"version": str, "commit": str, "build": str}`

#### `ClamAVSDKClient.scan_file(path: str) -> dict`

#### `ClamAVSDKClient.scan_bytes(data: bytes, filename: str = "") -> dict`

#### `ClamAVSDKClient.scan_stream(data: bytes) -> dict`

All scan methods return the standard scan result dict (`status`, `files_scanned`, `threats`, `message`).

---

## Module: `core.parser`

### `parse_scan_output(output: str) -> dict`

Parses raw `clamscan` stdout into a structured result dict.

**Parameters:**
- `output` (`str`): Full stdout text from a `clamscan` run.

**Returns:** `dict` with keys `status`, `files_scanned`, `threats`, `message`.

### `parse_update_output(output: str) -> dict`

Parses raw `freshclam` stdout into a structured result dict.

**Parameters:**
- `output` (`str`): Full stdout text from a `freshclam` run.

**Returns:** `dict` with keys `status`, `message`.

---

## Module: `core.config`

Configuration constants resolved relative to the application working directory.
Override the base directory by setting the `ANTIVIRUS_LEGACY_BIN_DIR` environment variable.

| Constant | Default |
|---|---|
| `LEGACY_BIN_DIR` | Current working directory (or `ANTIVIRUS_LEGACY_BIN_DIR`) |
| `ANTIVIRUS_EXE` | `<LEGACY_BIN_DIR>/antivirus.Legacy.exe` |
| `CLAMAV_DIR` | `<LEGACY_BIN_DIR>/ClamAV` |
| `CLAMSCAN_EXE` | `<CLAMAV_DIR>/clamscan.exe` |
| `FRESHCLAM_EXE` | `<CLAMAV_DIR>/freshclam.exe` |
| `DATABASE_DIR` | `<CLAMAV_DIR>/database` |
| `LOG_FILE` | `<LEGACY_BIN_DIR>/antivirus.log` |
| `DEFAULT_SCAN_TIMEOUT` | 3600 (seconds) |
| `DEFAULT_UPDATE_TIMEOUT` | 600 (seconds) |
