# KazoOCR.Core

> This document describes the Core library of the KazoOCR solution.

## Overview

`KazoOCR.Core` is a pure .NET library with no external dependencies. It contains all business logic for PDF OCR processing.

## Key Components

### IOcrFileService / OcrFileService

File handling services:
- Rename files with configurable suffix (default: `_OCR`)
- Validate PDF files
- Detect already processed files

### IOcrProcessRunner / OcrProcessRunner

Cross-platform wrapper around `ocrmypdf`:
- Linux native invocation
- Windows invocation via WSL with path conversion

### OcrSettings

Configuration POCO for OCR operations:
- `Suffix` — Output file suffix
- `Languages` — Tesseract language codes
- `Deskew` — Enable deskew correction
- `Clean` — Enable Unpaper cleaning
- `Rotate` — Enable orientation correction
- `Optimize` — Compression level (0-3)

### IEnvironmentDetector / EnvironmentDetector

Environment detection services:
- `IsWslAvailable()` — Check WSL availability on Windows
- `IsOcrMyPdfInstalled()` — Check OCRmyPDF installation
- `IsTesseractLangInstalled(string lang)` — Check Tesseract language pack
- `IsUnpaperInstalled()` — Check Unpaper installation

### IEnvironmentInstaller / EnvironmentInstaller

Automated dependency installation:
- Install via `apt-get` on Linux or WSL

### IPrivilegeElevator / PrivilegeElevator

Privilege management:
- `IsElevated()` — Check admin privileges
- `RelaunchElevatedAsync(args)` — Relaunch with elevation

### WatcherService

Folder monitoring service:
- `FileSystemWatcher` for file events
- `Channel<string>` for async processing queue

### Result Types

- `ValidationResult` — File validation results
- `ProcessResult` — OCR processing results

## Usage

```csharp
// Create services
var fileService = new OcrFileService();
var processRunner = new OcrProcessRunner();

// Configure settings
var settings = new OcrSettings
{
    Suffix = "_OCR",
    Languages = "fra+eng",
    Deskew = true
};

// Process a file
var result = await processRunner.RunAsync("document.pdf", settings);
```

## Related Documentation

- [Architecture](architecture.md) — Solution overview
- [CLI](cli.md) — CLI integration
- [Docker](docker.md) — Docker integration
