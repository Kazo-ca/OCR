# KazoOCR Docker

> This document describes the Docker deployment of the KazoOCR solution.

## Overview

KazoOCR uses Docker Compose to run two services:

- **api** — ASP.NET Core Web API with embedded background worker for folder watching
- **web** — Blazor Web App providing a browser-based dashboard

The legacy `KazoOCR.Docker` worker service remains available as a standalone container target.

## Quick Start

```bash
# Start both services
docker compose up --build

# Or start in detached mode
docker compose up -d --build
```

Once running:
- **API + Swagger**: http://localhost:5000/swagger
- **Web Dashboard**: http://localhost:5001

## Services

### API Service

The API service runs `KazoOCR.Api` and provides:
- REST endpoints for OCR job management
- Swagger documentation at `/swagger`
- Health check at `/health`
- Embedded background worker watching `/data` for new PDFs

### Web Service

The Web service runs `KazoOCR.Web` (Blazor) and provides:
- Browser-based dashboard for job management
- File upload interface
- Settings management
- Communicates with API service internally via `http://api:5000`

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `KAZO_API_PORT` | `5000` | Host port mapped to the API |
| `KAZO_WEB_PORT` | `5001` | Host port mapped to the Web UI |
| `KAZO_API_KEY` | *(empty)* | Static API key; no auth if unset |
| `KAZO_DEFAULT_PASSWORD` | *(empty)* | Admin password; first-run wizard if unset |
| `KAZO_WATCH_PATH` | `/data` | Folder watched by the API worker |
| `KAZO_SUFFIX` | `_OCR` | Output file suffix |
| `KAZO_LANGUAGES` | `fra+eng` | Tesseract language codes |
| `KAZO_API_BASE_URL` | `http://api:5000` | URL used by Web to reach API |

### Customizing Ports

```bash
# Use custom ports
KAZO_API_PORT=8080 KAZO_WEB_PORT=8081 docker compose up
```

## Docker Compose

```yaml
services:
  api:
    build:
      context: .
      dockerfile: docker/Dockerfile
      target: api
    ports:
      - "${KAZO_API_PORT:-5000}:5000"
    volumes:
      - data:/data
      - auth:/app/data
    environment:
      - KAZO_WATCH_PATH=/data
      - KAZO_SUFFIX=${KAZO_SUFFIX:-_OCR}
      - KAZO_LANGUAGES=${KAZO_LANGUAGES:-fra+eng}
      - KAZO_API_KEY=${KAZO_API_KEY:-}
      - KAZO_DEFAULT_PASSWORD=${KAZO_DEFAULT_PASSWORD:-}
    restart: unless-stopped

  web:
    build:
      context: .
      dockerfile: docker/Dockerfile
      target: web
    ports:
      - "${KAZO_WEB_PORT:-5001}:5001"
    environment:
      - KAZO_API_BASE_URL=http://api:5000
    depends_on:
      - api
    restart: unless-stopped

volumes:
  data:
  auth:
```

## Dockerfile

The Dockerfile uses a multi-stage build with four targets:

1. **build** — .NET SDK for compilation (all projects)
2. **api** — ASP.NET runtime + OCRmyPDF + Tesseract for the API service
3. **web** — ASP.NET runtime for the Web UI
4. **worker** — .NET runtime + OCRmyPDF for legacy standalone worker

### Building Specific Targets

```bash
# Build API image only
docker build -t kazoocr-api:latest -f docker/Dockerfile --target api .

# Build Web image only
docker build -t kazoocr-web:latest -f docker/Dockerfile --target web .

# Build legacy worker image
docker build -t kazoocr-worker:latest -f docker/Dockerfile --target worker .
```

## Volumes

### data

Shared volume for PDF files. The API worker monitors this folder and processes new PDFs.

```bash
# Mount a host directory for PDF processing
docker run -v /home/user/documents:/data kazoocr-api:latest
```

### auth

Persists `auth.json` containing user credentials and API keys across container restarts.

## Logs

View container logs:

```bash
# All services
docker compose logs -f

# API only
docker compose logs -f api

# Web only
docker compose logs -f web
```

## Legacy Worker Service

The standalone `KazoOCR.Docker` worker service can be used independently:

```bash
# Build and run the legacy worker
docker build -t kazoocr-worker:latest -f docker/Dockerfile --target worker .
docker run -v /home/user/documents:/watch kazoocr-worker:latest
```

## Related Documentation

- [Architecture](architecture.md) — Solution overview
- [Core](core.md) — Core library
- [CLI](cli.md) — CLI application
