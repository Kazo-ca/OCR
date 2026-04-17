# KazoOCR

[![CI](https://github.com/Kazo-ca/OCR/actions/workflows/pr-check.yml/badge.svg)](https://github.com/Kazo-ca/OCR/actions/workflows/pr-check.yml)
[![Release](https://github.com/Kazo-ca/OCR/actions/workflows/auto-release.yml/badge.svg)](https://github.com/Kazo-ca/OCR/actions/workflows/auto-release.yml)
[![Docker](https://github.com/Kazo-ca/OCR/actions/workflows/dockerhub.yml/badge.svg)](https://github.com/Kazo-ca/OCR/actions/workflows/dockerhub.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**KazoOCR** is a cross-platform .NET 10 solution that automates the creation of "Sandwich" PDFs (searchable text layer) by leveraging **OCRmyPDF**. Distribute via CLI (one-shot, batch & watch), Docker, Windows Service, or MAUI (Microsoft Store).

## Features

- **One-shot processing** — Process a single PDF file
- **Batch processing** — Recursively process all PDFs in a folder
- **Watch mode** — Continuously monitor a folder for new PDFs
- **Cross-platform** — Works on Linux natively, Windows via WSL
- **Docker support** — Run as a containerized service
- **Windows Service** — Install as a background service
- **MAUI UI** — Desktop application with drag & drop support

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [OCRmyPDF](https://ocrmypdf.readthedocs.io/) (installed natively on Linux or via WSL on Windows)
- [Tesseract OCR](https://tesseract-ocr.github.io/) with language packs

## Quickstart

### CLI

```bash
# Build the CLI
dotnet build src/KazoOCR.CLI

# Process a single PDF
kazoocr ocr -i document.pdf

# Process all PDFs in a folder
kazoocr ocr -i /path/to/folder/

# Watch a folder for new PDFs
kazoocr watch -i /path/to/folder/

# Install dependencies (requires elevated privileges)
kazoocr install
```

### Docker

```bash
# Using docker-compose
docker-compose -f docker/docker-compose.yml up

# Or directly with docker run
docker run -v /path/to/pdfs:/data kazoocr:latest
```

### MAUI (Windows Desktop)

1. Download from the Microsoft Store (coming soon)
2. Or build locally:
   ```bash
   dotnet build src/KazoOCR.UI -f net10.0-windows10.0.19041.0
   ```

## CLI Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--input` | `-i` | Source file or folder | *required* |
| `--suffix` | `-s` | Suffix for output file | `_OCR` |
| `--languages` | `-l` | Tesseract language codes | `fra+eng` |
| `--deskew` | | Enable deskew correction | `false` |
| `--clean` | | Enable Unpaper cleaning | `false` |
| `--rotate` | | Enable orientation correction | `false` |
| `--optimize` | | Compression level (0-3) | `1` |

## Commands

| Command | Description |
|---------|-------------|
| `ocr` | Process PDF files (one-shot or batch) |
| `watch` | Watch a folder for new PDFs |
| `install` | Install dependencies (OCRmyPDF, Tesseract) |
| `service install` | Install as Windows Service |
| `service uninstall` | Uninstall Windows Service |
| `service status` | Check Windows Service status |

## Project Structure

```
KazoOCR.sln
├── src/
│   ├── KazoOCR.Core/      # Core business logic library
│   ├── KazoOCR.CLI/       # Console application
│   ├── KazoOCR.Docker/    # Worker Service for Docker
│   └── KazoOCR.UI/        # MAUI Desktop application
├── tests/
│   └── KazoOCR.Tests/     # xUnit tests
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
└── docs/                  # Documentation
```

## Documentation

For detailed documentation, see the [/docs](/docs) folder:

- [Architecture](docs/architecture.md) — Solution architecture and design
- [Core](docs/core.md) — Core library documentation
- [CLI](docs/cli.md) — CLI application documentation
- [Docker](docs/docker.md) — Docker deployment guide
- [Service](docs/service.md) — Windows Service documentation
- [UI](docs/ui.md) — MAUI application documentation

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
