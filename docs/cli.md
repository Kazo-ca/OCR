# KazoOCR.CLI

> This document describes the CLI application of the KazoOCR solution.

## Overview

`KazoOCR.CLI` is a console application that provides command-line access to KazoOCR functionality. It uses `CommandDotNet` for argument mapping and `Microsoft.Extensions.Hosting` for background services.

## Commands

### ocr

Process PDF files (one-shot or batch mode).

```bash
# Single file
kazoocr ocr -i document.pdf

# Batch processing
kazoocr ocr -i /path/to/folder/
```

### watch

Watch a folder for new PDF files and process them automatically.

```bash
kazoocr watch -i /path/to/folder/
```

### install

Install dependencies (OCRmyPDF, Tesseract, Unpaper).

```bash
kazoocr install
```

### service

Manage Windows Service installation (Windows only).

```bash
# Install the Windows Service
kazoocr service install

# Install with custom configuration path
kazoocr service install --config "C:\MyConfig\service.json"

# Uninstall the Windows Service
kazoocr service uninstall

# Check service status
kazoocr service status
```

The service commands require Administrator privileges. See [Service Documentation](service.md) for details.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--input` | `-i` | Source file or folder | *required* |
| `--suffix` | `-s` | Suffix for output file | `_OCR` |
| `--languages` | `-l` | Tesseract language codes | `fra+eng` |
| `--deskew` | | Enable deskew correction | `true` |
| `--clean` | | Enable Unpaper cleaning | `false` |
| `--rotate` | | Enable orientation correction | `true` |
| `--optimize` | | Compression level (0-3) | `1` |

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | General error |
| 2 | Invalid arguments |
| 3 | File not found |
| 4 | OCR processing failed |

## Configuration

The CLI can be configured via:
- Command-line arguments
- Environment variables
- Configuration file (`appsettings.json`)
- Service configuration (`appsettings.service.json`) for Windows Service mode

## Examples

```bash
# Process with custom suffix
kazoocr ocr -i document.pdf -s "_searchable"

# Process with French and German
kazoocr ocr -i document.pdf -l "fra+deu"

# Process with all corrections enabled
kazoocr ocr -i document.pdf --deskew --clean --rotate

# Watch a folder continuously
kazoocr watch -i /path/to/folder/ --clean

# Install Windows Service with custom config
kazoocr service install --config "D:\Config\kazoocr.json"
```

## Related Documentation

- [Architecture](architecture.md) — Solution overview
- [Core](core.md) — Core library
- [Service](service.md) — Windows Service
