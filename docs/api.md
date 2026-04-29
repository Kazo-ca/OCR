# KazoOCR.Api

> This document describes the REST API of the KazoOCR solution.

## Overview

`KazoOCR.Api` is an ASP.NET Core 10 Web API that exposes OCR processing capabilities via HTTP endpoints. It includes:

- **REST API** — Submit PDF files for OCR processing and track job status
- **Embedded Worker** — Background service that monitors a watch folder for automatic processing
- **Swagger UI** — Interactive API documentation and testing

## Quick Start

```bash
# Using docker-compose
docker compose up

# Access the API
curl http://localhost:5000/health

# View Swagger documentation
open http://localhost:5000/swagger
```

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/ocr/process` | Submit a PDF for OCR processing (multipart/form-data) |
| `GET` | `/api/ocr/jobs` | List all jobs with status |
| `GET` | `/api/ocr/jobs/{id}` | Get status/result of a specific job |
| `DELETE` | `/api/ocr/jobs/{id}` | Cancel or remove a job |
| `GET` | `/health` | Health check endpoint |
| `POST` | `/api/auth/login` | Authenticate and obtain a session |
| `POST` | `/api/auth/setup` | First-run password setup (only available if no password configured) |

### POST /api/ocr/process

Submit a PDF file for OCR processing.

**Request:**
- Content-Type: `multipart/form-data`
- Body: PDF file in the `file` field

**Response:**
- `202 Accepted` — Job accepted, returns `jobId`
- `400 Bad Request` — Invalid file or missing file
- `401 Unauthorized` — API key required but missing/invalid

```bash
# Submit a PDF for processing
curl -X POST http://localhost:5000/api/ocr/process \
  -F "file=@document.pdf"

# Response
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Pending",
  "createdAt": "2026-04-19T14:30:00Z"
}
```

### GET /api/ocr/jobs

List all OCR jobs with their current status.

**Response:**
- `200 OK` — Returns array of jobs

```bash
curl http://localhost:5000/api/ocr/jobs

# Response
[
  {
    "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fileName": "document.pdf",
    "status": "Completed",
    "createdAt": "2026-04-19T14:30:00Z",
    "completedAt": "2026-04-19T14:30:45Z"
  }
]
```

### GET /api/ocr/jobs/{id}

Get the status and details of a specific job.

**Response:**
- `200 OK` — Returns job details
- `404 Not Found` — Job not found

```bash
curl http://localhost:5000/api/ocr/jobs/3fa85f64-5717-4562-b3fc-2c963f66afa6

# Response
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "document.pdf",
  "status": "Completed",
  "createdAt": "2026-04-19T14:30:00Z",
  "completedAt": "2026-04-19T14:30:45Z",
  "outputPath": "/data/document_OCR.pdf"
}
```

**Job Status Values:**

| Status | Description |
|--------|-------------|
| `Pending` | Job is queued for processing |
| `Processing` | Job is currently being processed |
| `Completed` | Job finished successfully |
| `Failed` | Job encountered an error |

### DELETE /api/ocr/jobs/{id}

Cancel a pending job or remove a completed job from the list.

**Response:**
- `204 No Content` — Job deleted successfully
- `404 Not Found` — Job not found

```bash
curl -X DELETE http://localhost:5000/api/ocr/jobs/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

### GET /health

Health check endpoint for container orchestration and load balancers.

**Response:**
- `200 OK` — Service is healthy

```bash
curl http://localhost:5000/health

# Response
{
  "status": "Healthy"
}
```

## Authentication

The API supports two authentication mechanisms:

### API Key Authentication

When `KAZO_API_KEY` environment variable is set, all API requests must include the `X-Api-Key` header.

```bash
# Without API key configured (open access)
curl -X POST http://localhost:5000/api/ocr/process \
  -F "file=@document.pdf"

# With API key configured
curl -X POST http://localhost:5000/api/ocr/process \
  -H "X-Api-Key: mysecretkey" \
  -F "file=@document.pdf"
```

If `KAZO_API_KEY` is not set or empty, the API operates in open mode with no authentication required.

### Password Authentication (Web UI)

The Web UI uses session-based authentication with passwords:

- If `KAZO_DEFAULT_PASSWORD` is set, it's used as the admin password
- If not set, the first-run wizard prompts for password creation
- Passwords are stored as bcrypt hashes in `/data/auth.json`

```bash
# First-run setup (only available if no password configured)
curl -X POST http://localhost:5000/api/auth/setup \
  -H "Content-Type: application/json" \
  -d '{"password": "mysecurepassword"}'

# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"password": "mysecurepassword"}'
```

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `KAZO_API_PORT` | `5000` | HTTP port exposed by the API |
| `KAZO_API_KEY` | *(empty)* | Static API key; authentication disabled when empty |
| `KAZO_DEFAULT_PASSWORD` | *(empty)* | Web UI password; first-run wizard when empty |
| `KAZO_WATCH_PATH` | `/data` | Folder watched by the embedded worker |
| `KAZO_SUFFIX` | `_OCR` | Suffix added to processed files |
| `KAZO_LANGUAGES` | `fra+eng` | Tesseract language codes |
| `KAZO_DESKEW` | `true` | Enable deskew correction |
| `KAZO_CLEAN` | `false` | Enable Unpaper cleaning |
| `KAZO_ROTATE` | `true` | Enable orientation correction |
| `KAZO_OPTIMIZE` | `1` | Compression level (0-3) |

### Docker Compose Example

```yaml
services:
  api:
    image: kazo/ocr:latest
    ports:
      - "${KAZO_API_PORT:-5000}:5000"
    environment:
      - KAZO_API_KEY=mysecretkey
      - KAZO_WATCH_PATH=/data
      - KAZO_LANGUAGES=fra+eng
    volumes:
      - data:/data
      - auth:/app/auth

volumes:
  data:
  auth:
```

## Swagger UI

Interactive API documentation is available at:

```
http://localhost:5000/swagger
```

Swagger UI provides:
- Interactive endpoint testing
- Request/response schema documentation
- Authentication header configuration
- OpenAPI specification download

The OpenAPI specification is also available at:
```
http://localhost:5000/swagger/v1/swagger.json
```

## Embedded Worker

The API includes an embedded `BackgroundService` that monitors the configured watch folder (`KAZO_WATCH_PATH`) for new PDF files:

1. When a PDF file is detected, it's automatically queued for processing
2. Jobs from the watch folder appear in the job list alongside uploaded files
3. Processed files are saved with the configured suffix (e.g., `document_OCR.pdf`)

This allows the API to function as both:
- An on-demand OCR service via the REST API
- An automatic folder-watching processor

## Error Handling

All API errors return a consistent JSON format:

```json
{
  "error": "Error message description",
  "code": "ERROR_CODE",
  "details": {}
}
```

| HTTP Status | Code | Description |
|-------------|------|-------------|
| `400` | `INVALID_FILE` | File is not a valid PDF |
| `400` | `MISSING_FILE` | No file provided in request |
| `401` | `UNAUTHORIZED` | API key missing or invalid |
| `404` | `JOB_NOT_FOUND` | Requested job does not exist |
| `409` | `ALREADY_CONFIGURED` | Password already set (setup endpoint) |
| `500` | `INTERNAL_ERROR` | Unexpected server error |

## Related Documentation

- [Architecture](architecture.md) — Solution overview
- [Web](web.md) — Blazor Web UI
- [Docker](docker.md) — Docker deployment
- [Core](core.md) — Core library
