# Contributing to Antivirus Solution

Thank you for your interest in contributing! This document describes how to contribute to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Submitting Changes](#submitting-changes)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Reporting Issues](#reporting-issues)

## Code of Conduct

Please be respectful and constructive in all interactions. We are committed to providing a welcoming environment for all contributors.

## Getting Started

1. **Fork** the repository at https://github.com/timothygwebb/antivirus
2. **Clone** your fork locally
3. **Create a branch** for your feature or fix: `git checkout -b feature/my-feature`

## Development Setup

### C# / .NET (antivirus.Legacy)

Requirements:
- .NET Framework 2.0 SDK or Visual Studio 2019+
- Windows OS (XP or later for testing)

```powershell
# Build the legacy project
cd antivirus.Legacy
dotnet build
```

### Python Agent Layer

Requirements:
- Python 3.8+
- pip

```bash
# Install Python dependencies
pip install -r requirements.txt
```

The `clamav-sdk` package (listed in `requirements.txt`) is required for `SDKScanAgent` and the `sdk-scan` CLI command. Tests for those components mock the SDK so no live ClamAV service is needed to run the test suite.

## Submitting Changes

1. Ensure your changes pass all tests.
2. Follow the coding standards below.
3. Write a clear commit message describing your change.
4. Submit a **Pull Request** against the `main` branch.
5. Fill in the PR template with a description of your changes and the motivation.

## Coding Standards

### C# (.NET Framework 2.0 / C# 7.3)

- Target .NET Framework 2.0 for maximum compatibility (Windows XP+).
- Do not use C# language features beyond version 7.3.
- Use relative paths from the application working directory.
- Handle all exceptions gracefully; never surface raw exceptions to the user.
- Log errors to `antivirus.log`.

### Python (Agent Layer)

- Target Python 3.8+.
- Follow [PEP 8](https://peps.python.org/pep-0008/) style guidelines.
- Add type hints to all public functions.
- Write docstrings for all public classes and functions.
- Use specific exception types, not bare `except`.

## Testing

### Running C# Tests

```powershell
dotnet test
```

### Running Python Tests

```bash
pytest
```

All new features should be accompanied by appropriate test coverage.

## Reporting Issues

Please use the [GitHub Issues](https://github.com/timothygwebb/antivirus/issues) page to report bugs or request features.

When reporting a bug, include:
- Operating system and version
- .NET Framework version
- Steps to reproduce
- Expected vs. actual behavior
- Relevant log output from `antivirus.log`
