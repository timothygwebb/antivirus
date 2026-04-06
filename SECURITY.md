# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| Latest  | ✅ Yes    |

## Reporting a Vulnerability

If you discover a security vulnerability in this project, **please do not open a public GitHub issue**.

Instead, report it privately by emailing the maintainer or by using [GitHub's private vulnerability reporting](https://github.com/timothygwebb/antivirus/security/advisories/new).

### What to Include

- A description of the vulnerability and its potential impact.
- Steps to reproduce the issue.
- Any suggested mitigations or patches (optional but appreciated).

### Response Timeline

- **Acknowledgement**: Within 48 hours of receipt.
- **Initial Assessment**: Within 7 days.
- **Fix / Disclosure**: Coordinated with the reporter; typically within 30 days for high-severity issues.

## Security Considerations

### ClamAV Integration

- The application downloads ClamAV binaries and virus definitions from the official ClamAV project (https://www.clamav.net). Always verify downloads against official checksums when possible.
- ClamAV is run as a local portable installation to avoid requiring elevated privileges.

### File Handling

- The application scans files but does not automatically delete or quarantine them without user confirmation.
- Scanned file paths are logged locally to `antivirus.log`.

### Python Agent Layer

- The Python agent layer invokes ClamAV binaries (`clamscan.exe` for scanning, `freshclam.exe` for definition updates) directly as subprocesses via `core.executor`. Ensure inputs to the agent (scan target paths, configuration values) are validated before being passed to these executables.
- Do not pass untrusted user input directly as command-line arguments without sanitization.

## Dependency Security

- Keep ClamAV virus definitions up-to-date by running the definition updater (option 3) regularly.
- Keep Python dependencies up-to-date: `pip install --upgrade -r requirements.txt`.
- Monitor [GitHub Security Advisories](https://github.com/advisories) for any vulnerabilities in dependencies.
