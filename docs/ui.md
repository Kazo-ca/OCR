# KazoOCR.UI

> This document describes the MAUI Desktop application of the KazoOCR solution.

## Overview

`KazoOCR.UI` is a Windows Desktop application built with .NET MAUI. It provides a graphical interface for PDF OCR processing with drag & drop support.

## Features

- **Drag & Drop** — Drop PDF files directly onto the application
- **Batch Processing** — Process multiple files at once
- **Progress Tracking** — Visual progress bar for operations
- **Configuration UI** — Checkboxes and sliders for OCR options

## Requirements

- Windows 10 (build 19041) or later
- .NET 10 Runtime
- WSL with OCRmyPDF installed

## Installation

### Microsoft Store (Coming Soon)

The application will be available on the Microsoft Store.

### Build from Source

```bash
# Build for Windows
dotnet build src/KazoOCR.UI -f net10.0-windows10.0.19041.0

# Publish
dotnet publish src/KazoOCR.UI -f net10.0-windows10.0.19041.0 -c Release
```

## Usage

1. Launch the application
2. Drag and drop PDF files onto the window
3. Configure OCR options:
   - Suffix (output file naming)
   - Languages (Tesseract language codes)
   - Deskew, Clean, Rotate options
   - Optimization level
4. Click "Process" to start OCR
5. Monitor progress in the status bar

## Configuration

Settings are stored in `appsettings.json`:

```json
{
  "KazoOCR": {
    "DefaultSuffix": "_OCR",
    "DefaultLanguages": "fra+eng",
    "DefaultDeskew": false,
    "DefaultClean": false,
    "DefaultRotate": false,
    "DefaultOptimize": 1
  }
}
```

## Build Considerations

The MAUI project is conditionally compiled:
- Builds automatically on Windows
- On other platforms, use `/p:BuildMAUI=true` to include

```bash
# Force MAUI build on CI
dotnet build KazoOCR.sln /p:BuildMAUI=true
```

## Related Documentation

- [Architecture](architecture.md) — Solution overview
- [Core](core.md) — Core library
- [CLI](cli.md) — CLI alternative
