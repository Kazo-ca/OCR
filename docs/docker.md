# KazoOCR.Docker

> This document describes the Docker deployment of the KazoOCR solution.

## Overview

`KazoOCR.Docker` is a Worker Service designed to run in a Docker container. It uses the Core library's `WatcherService` to monitor a mounted volume for PDF files.

## Quick Start

```bash
# Using docker-compose
docker compose up --build

# Or build and run manually
docker build -t kazoocr:latest -f docker/Dockerfile .
docker run -v /path/to/pdfs:/watch kazoocr:latest
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `KAZO_WATCH_PATH` | Path to watch for PDFs | `/watch` |
| `KAZO_SUFFIX` | Output file suffix | `_OCR` |
| `KAZO_LANGUAGES` | Tesseract language codes | `fra+eng` |
| `KAZO_DESKEW` | Enable deskew correction | `false` |
| `KAZO_CLEAN` | Enable Unpaper cleaning | `false` |
| `KAZO_ROTATE` | Enable orientation correction | `false` |
| `KAZO_OPTIMIZE` | Compression level (0-3) | `1` |

## Docker Compose

```yaml
services:
  kazoocr:
    build:
      context: .
      dockerfile: docker/Dockerfile
    volumes:
      - ./watch:/watch
    environment:
      - KAZO_WATCH_PATH=/watch
      - KAZO_SUFFIX=_OCR
      - KAZO_LANGUAGES=fra+eng
      - KAZO_DESKEW=false
      - KAZO_CLEAN=false
      - KAZO_ROTATE=false
      - KAZO_OPTIMIZE=1
    restart: unless-stopped
```

## Dockerfile

The Dockerfile uses a multi-stage build:
1. **Build stage**: .NET SDK for compilation
2. **Runtime stage**: .NET runtime + OCRmyPDF + Tesseract

## Volumes

Mount your PDF folder to `/watch` in the container:

```bash
docker run -v /home/user/documents:/watch kazoocr:latest
```

## Logs

View container logs:

```bash
docker logs -f kazoocr
```

## Related Documentation

- [Architecture](architecture.md) — Solution overview
- [Core](core.md) — Core library
- [CLI](cli.md) — CLI application
