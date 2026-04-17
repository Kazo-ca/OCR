# Windows Service

> This document describes the Windows Service deployment of the KazoOCR solution.

## Overview

KazoOCR CLI can be installed as a Windows Service for continuous background processing. The service uses `Microsoft.Extensions.Hosting.WindowsServices` for service lifecycle management.

## Installation

### Install Service

```powershell
# Run as Administrator
kazoocr service install
```

This will:
1. Register the service with Windows Service Control Manager
2. Configure automatic startup
3. Start monitoring the configured folder

### Uninstall Service

```powershell
# Run as Administrator
kazoocr service uninstall
```

### Check Status

```powershell
kazoocr service status
```

## Configuration

Service configuration is stored in `appsettings.service.json`:

```json
{
  "KazoOCR": {
    "WatchPath": "C:\\Users\\Public\\Documents\\OCR",
    "Suffix": "_OCR",
    "Languages": "fra+eng",
    "Deskew": false,
    "Clean": false,
    "Rotate": false,
    "Optimize": 1
  }
}
```

## Windows Service Manager

You can also manage the service using Windows tools:

```powershell
# Using sc.exe
sc query KazoOCR
sc start KazoOCR
sc stop KazoOCR

# Using PowerShell
Get-Service KazoOCR
Start-Service KazoOCR
Stop-Service KazoOCR
```

## Logs

Service logs are written to:
- Windows Event Log (Application)
- `%ProgramData%\KazoOCR\logs\` (file logs)

## Troubleshooting

### Service won't start

1. Check Event Viewer for error messages
2. Verify the watch path exists and is accessible
3. Ensure OCRmyPDF is installed via WSL

### Processing not working

1. Verify WSL is properly configured
2. Check that OCRmyPDF is installed in WSL
3. Review service logs for errors

## Related Documentation

- [Architecture](architecture.md) — Solution overview
- [CLI](cli.md) — CLI commands
- [Core](core.md) — Core library
