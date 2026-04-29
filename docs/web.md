# KazoOCR.Web

> This document describes the Blazor web interface of the KazoOCR solution.

## Overview

`KazoOCR.Web` is a Blazor Web App (.NET 10) that provides a browser-based interface for KazoOCR. It communicates with `KazoOCR.Api` via HTTP to submit files, monitor jobs, and configure settings.

**Key Features:**
- Dashboard with live job status
- Drag-and-drop PDF upload
- OCR settings configuration
- First-run password wizard
- Responsive design (Bootstrap)
- No external JavaScript frameworks — pure Blazor components

## Quick Start

```bash
# Using docker-compose
docker compose up

# Access the Web UI
open http://localhost:5001
```

## Pages

| Page | Route | Description |
|------|-------|-------------|
| Dashboard | `/` | Job list with live status polling |
| Upload | `/upload` | Drag-and-drop PDF upload with OCR options |
| Settings | `/settings` | Configure default OCR options and watch path |
| Login | `/login` | Authentication form |
| Setup | `/setup` | First-run password creation wizard |

### Dashboard (`/`)

The main dashboard displays all OCR jobs with their current status.

```
┌──────────────────────────────────────────────────────────────┐
│  KazoOCR                                    [Settings] [⚙]  │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  📊 Dashboard                              [+ Upload]        │
│  ─────────────────────────────────────────────────────────  │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ File              │ Status      │ Created    │ Actions │ │
│  ├────────────────────────────────────────────────────────┤ │
│  │ report.pdf        │ ✓ Completed │ 2 min ago  │ 🗑️      │ │
│  │ invoice.pdf       │ ⏳ Processing│ 1 min ago  │ 🗑️      │ │
│  │ contract.pdf      │ ⏱️ Pending  │ just now   │ 🗑️      │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  Showing 3 jobs • Auto-refresh every 5s                     │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

**Features:**
- Real-time status updates via polling (5-second interval)
- Status indicators: Pending (⏱️), Processing (⏳), Completed (✓), Failed (✗)
- Delete action to remove completed/failed jobs
- Quick access to upload page

### Upload (`/upload`)

Upload PDF files for OCR processing with optional settings override.

```
┌──────────────────────────────────────────────────────────────┐
│  KazoOCR                                    [Settings] [⚙]  │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  📤 Upload PDF                                               │
│  ─────────────────────────────────────────────────────────  │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │                                                        │ │
│  │              📄 Drag & drop PDF here                   │ │
│  │                   or click to browse                   │ │
│  │                                                        │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  OCR Options (optional - uses defaults if not specified)    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Languages: [fra+eng          ▼]                       │ │
│  │  Suffix:    [_OCR             ]                        │ │
│  │  ☑ Deskew   ☐ Clean   ☑ Rotate                        │ │
│  │  Optimize:  [====●========] 1                          │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  [Cancel]                                    [Process PDF]   │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

**Features:**
- Drag-and-drop file selection
- Click-to-browse file picker
- Override default OCR options per file
- Progress indicator during upload
- Automatic redirect to dashboard after submission

### Settings (`/settings`)

Configure default OCR options used for uploaded files and the watch folder.

```
┌──────────────────────────────────────────────────────────────┐
│  KazoOCR                                    [Settings] [⚙]  │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ⚙️ Settings                                                 │
│  ─────────────────────────────────────────────────────────  │
│                                                              │
│  Default OCR Options                                         │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Suffix:       [_OCR             ]                     │ │
│  │  Languages:    [fra+eng          ]                     │ │
│  │  ☑ Deskew      Straighten skewed pages                │ │
│  │  ☐ Clean       Remove noise with Unpaper              │ │
│  │  ☑ Rotate      Auto-rotate pages                      │ │
│  │  Optimize:     [====●========] 1 (0=none, 3=max)      │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  Watch Folder                                                │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Path: /data                                           │ │
│  │  Status: ✓ Watching (3 files processed today)         │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  [Reset to Defaults]                          [Save]         │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

**Features:**
- Modify all OCR options
- View watch folder status
- Reset to factory defaults
- Settings persist across sessions

### Login (`/login`)

Authentication page for protected access.

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│                         KazoOCR                              │
│                                                              │
│              ┌──────────────────────────────┐                │
│              │                              │                │
│              │        🔐 Sign In            │                │
│              │                              │                │
│              │  Password:                   │                │
│              │  [••••••••••••••           ] │                │
│              │                              │                │
│              │          [Sign In]           │                │
│              │                              │                │
│              └──────────────────────────────┘                │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

**Features:**
- Single password field (no username required)
- Session-based authentication
- Redirect to originally requested page after login

### Setup (`/setup`)

First-run wizard for password creation. Only available when no password has been configured.

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│                         KazoOCR                              │
│                      First-Time Setup                        │
│                                                              │
│              ┌──────────────────────────────┐                │
│              │                              │                │
│              │   Welcome to KazoOCR! 🎉     │                │
│              │                              │                │
│              │   Create a password to       │                │
│              │   protect your instance.     │                │
│              │                              │                │
│              │   Password:                  │                │
│              │   [                        ] │                │
│              │                              │                │
│              │   Confirm:                   │                │
│              │   [                        ] │                │
│              │                              │                │
│              │   ☑ Require password for     │                │
│              │     all access               │                │
│              │                              │                │
│              │      [Complete Setup]        │                │
│              │                              │                │
│              └──────────────────────────────┘                │
│                                                              │
│   Note: This page is only shown on first run.               │
│   You can skip setup if running in a trusted environment.   │
│                                                              │
│                         [Skip for now]                       │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

## First-Run Wizard Procedure

When KazoOCR is started for the first time (and `KAZO_DEFAULT_PASSWORD` is not set), the following procedure occurs:

### Step 1: Detect First Run

The Web UI checks with the API whether authentication is configured:

```bash
GET /api/auth/status
# Response: { "configured": false }
```

### Step 2: Redirect to Setup

If not configured, users are automatically redirected to `/setup`.

### Step 3: Create Password

User enters and confirms a password. The password must:
- Be at least 8 characters long
- Contain at least one number or special character

### Step 4: Save Configuration

The Web UI calls the setup endpoint:

```bash
POST /api/auth/setup
Content-Type: application/json

{ "password": "userpassword" }
```

The password is hashed (bcrypt) and stored in `/data/auth.json`.

### Step 5: Auto-Login

After successful setup, the user is automatically logged in and redirected to the dashboard.

### Skipping Setup

Users can click "Skip for now" to bypass password creation. In this mode:
- The Web UI operates without authentication
- API endpoints remain accessible (subject to API key if configured)
- The setup wizard can be completed later from Settings

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `KAZO_WEB_PORT` | `5001` | HTTP port for the Blazor app |
| `KAZO_API_BASE_URL` | `http://api:5000` | Internal URL of KazoOCR.Api (Docker network) |

### Docker Compose Example

```yaml
services:
  web:
    image: kazo/ocr-web:latest
    ports:
      - "${KAZO_WEB_PORT:-5001}:5001"
    environment:
      - KAZO_API_BASE_URL=http://api:5000
    depends_on:
      - api

  api:
    image: kazo/ocr-api:latest
    ports:
      - "${KAZO_API_PORT:-5000}:5000"
    volumes:
      - data:/data
      - auth:/app/auth

volumes:
  data:
  auth:
```

### Accessing from External Networks

When accessing from outside the Docker network, configure the API base URL to use the external hostname:

```yaml
environment:
  - KAZO_API_BASE_URL=http://myserver.example.com:5000
```

## Browser Requirements

KazoOCR.Web supports modern browsers:
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

JavaScript must be enabled for Blazor interactivity.

## Responsive Design

The interface adapts to different screen sizes:

**Desktop (≥1200px):** Full sidebar navigation, multi-column layout

**Tablet (768px-1199px):** Collapsible sidebar, responsive tables

**Mobile (<768px):** Bottom navigation, stacked forms, touch-friendly buttons

## Related Documentation

- [Architecture](architecture.md) — Solution overview
- [API](api.md) — REST API documentation
- [Docker](docker.md) — Docker deployment
- [Core](core.md) — Core library
