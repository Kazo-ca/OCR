# KazoOCR.Docker

> This document describes the Docker deployment of the KazoOCR solution.

## Overview

`KazoOCR.Docker` is a Worker Service designed to run in a Docker container. It uses the Core library's `WatcherService` to monitor a mounted volume for PDF files.

## Quick Start

```bash
# Using docker-compose
docker-compose -f docker/docker-compose.yml up

# Or build and run manually
docker build -t kazoocr:latest -f docker/Dockerfile .
docker run -v /path/to/pdfs:/data kazoocr:latest
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `KAZO_WATCH_PATH` | Path to watch for PDFs | `/data` |
| `KAZO_SUFFIX` | Output file suffix | `_OCR` |
| `KAZO_LANGUAGES` | Tesseract language codes | `fra+eng` |
| `KAZO_DESKEW` | Enable deskew correction | `false` |
| `KAZO_CLEAN` | Enable Unpaper cleaning | `false` |
| `KAZO_ROTATE` | Enable orientation correction | `false` |
| `KAZO_OPTIMIZE` | Compression level (0-3) | `1` |

## Docker Compose

```yaml
version: '3.8'
services:
  kazoocr:
    build:
      context: ..
      dockerfile: docker/Dockerfile
    volumes:
      - /path/to/pdfs:/data
    environment:
      - KAZO_WATCH_PATH=/data
      - KAZO_LANGUAGES=fra+eng
    restart: unless-stopped
```

## Dockerfile

The Dockerfile uses a multi-stage build:
1. **Build stage**: .NET SDK for compilation
2. **Runtime stage**: .NET runtime + OCRmyPDF + Tesseract

## Volumes

Mount your PDF folder to `/data` in the container:

```bash
docker run -v /home/user/documents:/data kazoocr:latest
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
