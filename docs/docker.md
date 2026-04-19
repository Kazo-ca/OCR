# KazoOCR Docker Deployment

> This document describes the Docker deployment of the KazoOCR solution.

## Overview

KazoOCR provides a multi-service Docker deployment with three components:

| Service | Description | Port |
|---------|-------------|------|
| `api` | REST API + embedded worker | 5000 |
| `web` | Blazor Web UI | 5001 |
| `worker` | Standalone watch-mode worker (legacy) | — |

The recommended deployment uses `api` + `web` services. The `worker` service is optional for standalone folder-watching scenarios.

## Quick Start

```bash
# Start all services
docker compose up

# Access the services
# API + Swagger:  http://localhost:5000/swagger
# Web UI:         http://localhost:5001
```

## Docker Compose

The `docker-compose.yml` configures all services:

```yaml
services:
  # REST API with embedded background worker
  api:
    build:
      context: .
      dockerfile: docker/Dockerfile
      target: api
    ports:
      - "${KAZO_API_PORT:-5000}:5000"
    environment:
      - KAZO_API_KEY=${KAZO_API_KEY:-}
      - KAZO_DEFAULT_PASSWORD=${KAZO_DEFAULT_PASSWORD:-}
      - KAZO_WATCH_PATH=/data
      - KAZO_SUFFIX=${KAZO_SUFFIX:-_OCR}
      - KAZO_LANGUAGES=${KAZO_LANGUAGES:-fra+eng}
    volumes:
      - data:/data
      - auth:/app/auth
    restart: unless-stopped

  # Blazor Web UI
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

  # Standalone worker (optional, for watch-only scenarios)
  worker:
    build:
      context: .
      dockerfile: docker/Dockerfile
      target: worker
    environment:
      - KAZO_WATCH_PATH=/watch
    volumes:
      - ./watch:/watch
    profiles:
      - worker-only
    restart: unless-stopped

volumes:
  data:
    # Shared volume for PDF processing (input/output)
  auth:
    # Persistent volume for authentication data (auth.json)
```

### Starting Specific Services

```bash
# Start API and Web UI (default)
docker compose up api web

# Start only the standalone worker
docker compose --profile worker-only up worker

# Start all services including worker
docker compose --profile worker-only up
```

## Environment Variables

### API Service

| Variable | Default | Description |
|----------|---------|-------------|
| `KAZO_API_PORT` | `5000` | External port mapping for API |
| `KAZO_API_KEY` | *(empty)* | API key for authentication; open access when empty |
| `KAZO_DEFAULT_PASSWORD` | *(empty)* | Web UI password; first-run wizard when empty |
| `KAZO_WATCH_PATH` | `/data` | Folder watched by embedded worker |
| `KAZO_SUFFIX` | `_OCR` | Suffix for processed files |
| `KAZO_LANGUAGES` | `fra+eng` | Tesseract language codes |
| `KAZO_DESKEW` | `true` | Enable deskew correction |
| `KAZO_CLEAN` | `false` | Enable Unpaper cleaning |
| `KAZO_ROTATE` | `true` | Enable orientation correction |
| `KAZO_OPTIMIZE` | `1` | Compression level (0-3) |

### Web Service

| Variable | Default | Description |
|----------|---------|-------------|
| `KAZO_WEB_PORT` | `5001` | External port mapping for Web UI |
| `KAZO_API_BASE_URL` | `http://api:5000` | Internal API URL (Docker network) |

### Worker Service

| Variable | Default | Description |
|----------|---------|-------------|
| `KAZO_WATCH_PATH` | `/watch` | Folder to monitor for PDFs |
| `KAZO_SUFFIX` | `_OCR` | Suffix for processed files |
| `KAZO_LANGUAGES` | `fra+eng` | Tesseract language codes |

## Volumes

### `data` Volume

Shared storage for PDF files:
- Upload destination for API
- Output location for processed files
- Can be mounted to a host directory for easy access:

```yaml
volumes:
  - /path/to/documents:/data
```

### `auth` Volume

Persistent storage for authentication:
- Contains `auth.json` with hashed passwords
- Preserves authentication across container restarts
- Should be backed up for production deployments

```yaml
volumes:
  - /path/to/auth:/app/auth
```

## Dockerfile

The Dockerfile uses a multi-stage build with separate targets:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/KazoOCR.Api -c Release -o /app/api
RUN dotnet publish src/KazoOCR.Web -c Release -o /app/web
RUN dotnet publish src/KazoOCR.Docker -c Release -o /app/worker

# API runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
RUN apt-get update && apt-get install -y ocrmypdf tesseract-ocr-fra tesseract-ocr-eng
WORKDIR /app
COPY --from=build /app/api .
EXPOSE 5000
ENTRYPOINT ["dotnet", "KazoOCR.Api.dll"]

# Web runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS web
WORKDIR /app
COPY --from=build /app/web .
EXPOSE 5001
ENTRYPOINT ["dotnet", "KazoOCR.Web.dll"]

# Worker runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS worker
RUN apt-get update && apt-get install -y ocrmypdf tesseract-ocr-fra tesseract-ocr-eng
WORKDIR /app
COPY --from=build /app/worker .
ENTRYPOINT ["dotnet", "KazoOCR.Docker.dll"]
```

## Building Images

```bash
# Build all targets
docker compose build

# Build specific target
docker build -t kazoocr-api:latest --target api -f docker/Dockerfile .
docker build -t kazoocr-web:latest --target web -f docker/Dockerfile .
docker build -t kazoocr-worker:latest --target worker -f docker/Dockerfile .
```

## Production Deployment

For production environments:

1. **Set secure passwords:**
   ```bash
   export KAZO_API_KEY="your-secure-api-key"
   export KAZO_DEFAULT_PASSWORD="your-secure-password"
   ```

2. **Use external volumes:**
   ```yaml
   volumes:
     - /mnt/storage/kazoocr/data:/data
     - /mnt/storage/kazoocr/auth:/app/auth
   ```

3. **Configure reverse proxy** (nginx/traefik) for HTTPS

4. **Set resource limits:**
   ```yaml
   services:
     api:
       deploy:
         resources:
           limits:
             cpus: '2'
             memory: 2G
   ```

## Logs

```bash
# View all service logs
docker compose logs -f

# View specific service logs
docker compose logs -f api
docker compose logs -f web
```

## Health Checks

The API service provides a health endpoint:

```bash
curl http://localhost:5000/health
```

Add health checks to docker-compose:

```yaml
services:
  api:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

## Related Documentation

- [Architecture](architecture.md) — Solution overview
- [API](api.md) — REST API documentation
- [Web](web.md) — Blazor Web UI
- [Core](core.md) — Core library
- [CLI](cli.md) — CLI application
