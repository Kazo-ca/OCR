# KazoOCR.Docker

> This document describes the Docker deployment of the KazoOCR solution.

## Overview

`KazoOCR.Docker` is currently a minimal Worker Service template designed to run in a Docker container.
At this stage it starts correctly and writes heartbeat logs; folder watching/OCR processing wiring is not enabled yet.

## Quick Start

```bash
# Using docker-compose
docker compose up --build

# Or build and run manually
docker build -t kazoocr:latest -f docker/Dockerfile .
docker run --rm kazoocr:latest
```

## Current Behavior

- The container starts the worker process.
- The worker currently emits periodic log lines.
- `KAZO_*` configuration variables are reserved for upcoming worker/Core integration.

## Docker Compose

```yaml
services:
  kazoocr:
    build:
      context: .
      dockerfile: docker/Dockerfile
    volumes:
      - ./watch:/watch
    restart: unless-stopped
```

## Dockerfile

The Dockerfile uses a multi-stage build:
1. **Build stage**: .NET SDK for compilation
2. **Runtime stage**: .NET runtime + OCRmyPDF + Tesseract (`fra` and `eng`) + non-root execution user

## Volumes

You can mount a folder to `/watch` now so the compose/runtime paths are ready for upcoming watch-mode wiring:

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
