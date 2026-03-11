# Copilot Instructions

## Project Guidelines
- When encountering repeated issues or loops in resolving a problem, take decisive action to fix the issue without asking for further confirmation.
- When repeating steps or encountering multiple iterations without resolution, delete and recreate the problematic configuration or code to resolve the issue.

## Solution Structure
This solution contains two projects:
- **antivirus.Legacy** (primary): .NET Framework 2.0 legacy antivirus with full ClamAV integration
- **antivirus**: Modern implementation

## Code Standards for antivirus.Legacy
- **Target**: .NET Framework 2.0
- **C# Version**: 7.3
- **Compatibility**: Windows XP+ support
- **Dependencies**: Avoid external packages; use built-in .NET 2.0 APIs
- **File Paths**: Use relative paths from `antivirus.Legacy\bin\Debug\net20\` working directory
- **ClamAV Location**: `.\ClamAV\` (relative to working directory)
- **Database Location**: `.\ClamAV\database\` (relative to working directory)
- **Log File**: `.\antivirus.log` (relative to working directory)

## Path Conventions
All file paths in code should be relative to the application's current directory:
```csharp
// Correct - Relative paths
Path.Combine(Directory.GetCurrentDirectory(), "ClamAV")
Path.Combine(Directory.GetCurrentDirectory(), "ClamAV", "database")

// Avoid - Hardcoded absolute paths
"C:\\Users\\marye\\source\\repos\\antivirus\\..."
```

## ClamAV Integration Guidelines
- Use **portable ClamAV** (auto-downloaded to `.\ClamAV\`)
- Never use system-installed ClamAV (permission issues)
- Use `clamscan.exe` (standalone scanner, no daemon)
- Use `freshclam.exe` with `--config-file` for updates
- Database path: `.\ClamAV\database\`

## Scanner Implementation
- Use asynchronous output reading for real-time progress
- Update progress every 2 seconds
- Count files as they're scanned
- Detect infections via "FOUND" keyword in output
- Parse final statistics from clamscan summary

## Error Handling
- Gracefully handle missing ClamAV (prompt user to run option 3)
- Gracefully handle missing database (warn but continue)
- Log all errors to `antivirus.log`
- Never throw exceptions to user; show friendly messages