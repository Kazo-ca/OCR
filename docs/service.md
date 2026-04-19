# Windows Service

> This document describes the Windows Service deployment of the KazoOCR solution.

## Overview

KazoOCR CLI can be installed as a Windows Service for continuous background processing. The service uses `Microsoft.Extensions.Hosting.WindowsServices` for service lifecycle management and supports monitoring **multiple folders** simultaneously.

## Installation

### Install Service

```powershell
# Run as Administrator
kazoocr service install
```

This will:
1. Register the service with Windows Service Control Manager
2. Configure automatic startup
3. Start monitoring all folders configured in `appsettings.service.json`

### Custom Configuration Path

```powershell
# Run as Administrator
kazoocr service install --config "C:\MyConfig\service.json"
```

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

Service configuration is stored in `appsettings.service.json`. The service supports monitoring **multiple folders**, each with their own OCR settings:

```json
{
  "WatchFolders": [
    {
      "Path": "C:\\Users\\Public\\Documents\\OCR",
      "Suffix": "_OCR",
      "Languages": "fra+eng",
      "Deskew": true,
      "Clean": false,
      "Rotate": true,
      "Optimize": 1
    },
    {
      "Path": "D:\\Scans\\Incoming",
      "Suffix": "_processed",
      "Languages": "eng",
      "Deskew": true,
      "Clean": true,
      "Rotate": true,
      "Optimize": 2
    }
  ]
}
```

### Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Path` | string | — | The folder path to watch for PDF files |
| `Suffix` | string | `_OCR` | Suffix appended to processed files |
| `Languages` | string | `fra+eng` | Tesseract language codes (e.g., `fra+eng`, `eng`, `deu`) |
| `Deskew` | bool | `true` | Enable automatic deskew correction |
| `Clean` | bool | `false` | Enable Unpaper cleaning (removes artifacts) |
| `Rotate` | bool | `true` | Enable automatic orientation correction |
| `Optimize` | int | `1` | PDF optimization level (0-3) |

## Administrator Privileges

Installing and uninstalling the Windows Service requires **Administrator privileges**. The CLI will:

1. Check if running as Administrator
2. If not, attempt to elevate privileges via UAC
3. A new elevated window will open to complete the operation

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
- Console output (when running interactively)

To view Event Log entries:
```powershell
Get-EventLog -LogName Application -Source KazoOCR -Newest 10
```

## Troubleshooting

### Service won't start

1. Check Event Viewer for error messages
2. Verify all watch paths exist and are accessible
3. Ensure OCRmyPDF is installed via WSL
4. Check the configuration file is valid JSON

### Processing not working

1. Verify WSL is properly configured
2. Check that OCRmyPDF is installed in WSL
3. Review service logs for errors
4. Ensure the service account has read/write access to watch folders

### Service not visible in Services Manager

1. Verify installation completed successfully
2. Check the Event Log for errors during installation
3. Try uninstalling and reinstalling the service

## Related Documentation

- [Architecture](architecture.md) — Solution overview
- [CLI](cli.md) — CLI commands
- [Core](core.md) — Core library
- [Docker](docker.md) — Docker deployment
