# Bundled Tools

This directory contains standalone tools bundled with the antivirus application for compatibility with older Windows systems.

## curl.exe

**Version**: curl 8.4.0 (Win32 static build)
**Source**: https://curl.se/windows/
**License**: MIT-style (see curl LICENSE file)

### Why Bundled?
- Windows ME and older systems don't have curl pre-installed
- Required for downloading ClamAV and virus definitions
- Ensures consistent TLS/SSL support across all platforms

### Download Instructions
To add curl.exe to this directory:

1. Download curl for Windows from: https://curl.se/windows/
2. Choose the 32-bit static build (curl-X.XX.X-win32-mingw.zip)
3. Extract and copy `curl.exe` to this `Tools` directory
4. The application will automatically use the bundled version if system curl is not found

### File Size
Approximately 3-4 MB for standalone curl.exe with OpenSSL support.

### Alternative Sources
- https://github.com/curl/curl/releases
- https://curl.se/download.html
