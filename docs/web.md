# KazoOCR.Web

Blazor Web App providing a browser-based interface for KazoOCR.

## Overview

KazoOCR.Web is a .NET 10 Blazor Server application that communicates with `KazoOCR.Api` via HTTP. It provides a responsive web interface for:

- Uploading PDF files for OCR processing
- Monitoring job status in real-time
- Configuring OCR settings
- Managing authentication

## Project Structure

```
src/KazoOCR.Web/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor      # Main layout with sidebar
│   │   ├── AuthLayout.razor      # Layout for login/setup pages
│   │   └── NavMenu.razor         # Navigation menu
│   ├── Pages/
│   │   ├── Home.razor            # Dashboard (job list)
│   │   ├── Upload.razor          # Drag-drop upload
│   │   ├── Settings.razor        # OCR settings
│   │   ├── Login.razor           # Authentication
│   │   └── Setup.razor           # First-run wizard
│   └── AuthGuard.razor           # Auth redirect component
├── Models/                       # DTOs for API communication
├── Services/
│   ├── IKazoApiClient.cs         # API client interface
│   ├── KazoApiClient.cs          # Typed HttpClient
│   └── AuthStateService.cs       # Browser auth state
└── Program.cs                    # App configuration
```

## Pages

| Route | Page | Description |
|-------|------|-------------|
| `/` | Dashboard | Lists all OCR jobs with live status polling (every 2 seconds) |
| `/upload` | Upload | Drag-and-drop PDF upload with OCR options form |
| `/settings` | Settings | Configure default OCR options, suffix, and watch path |
| `/login` | Login | Password authentication form |
| `/setup` | Setup | First-run password creation wizard |

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `KAZO_WEB_PORT` | `5001` | HTTP port for the Blazor app |
| `KAZO_API_BASE_URL` | `http://api:5000` | URL of the KazoOCR.Api service |

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "KazoOCR": {
    "ApiBaseUrl": "http://api:5000",
    "WebPort": 5001
  }
}
```

## Authentication Flow

1. On page load, `AuthGuard` checks authentication status via `GET /api/auth/status`
2. If `configured: false`, redirect to `/setup` for first-run password creation
3. If `authenticated: false`, redirect to `/login`
4. If authenticated, render the protected content

## Upload Page Features

- **Drag-and-drop zone** for PDF files
- **File selection** via browse button
- **OCR options form**:
  - Languages (text field, e.g., "fra+eng")
  - Deskew (checkbox)
  - Clean (checkbox)
  - Rotate (checkbox)
  - Optimize (slider 0-3)
- **Progress indicator** during upload
- **Navigation** to dashboard on success

## Dashboard Features

- **Job list table** showing:
  - File name
  - Status (Pending, Processing, Completed, Failed)
  - Created timestamp
  - Completed timestamp
  - Actions (Download, Delete)
- **Live polling** every 2 seconds
- **Status badges** with color coding

## API Communication

The application uses a typed `HttpClient` configured in `Program.cs`:

```csharp
var apiBaseUrl = Environment.GetEnvironmentVariable("KAZO_API_BASE_URL") ?? "http://api:5000";
builder.Services.AddHttpClient<IKazoApiClient, KazoApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### API Endpoints Used

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/auth/status` | Check auth configuration |
| POST | `/api/auth/login` | Authenticate user |
| POST | `/api/auth/logout` | End session |
| POST | `/api/auth/setup` | Create initial password |
| GET | `/api/ocr/jobs` | List all jobs |
| GET | `/api/ocr/jobs/{id}` | Get job details |
| POST | `/api/ocr/process` | Submit PDF for processing |
| DELETE | `/api/ocr/jobs/{id}` | Remove/cancel job |
| GET | `/api/settings` | Get OCR settings |
| PUT | `/api/settings` | Update OCR settings |

## Running Locally

```bash
# Set environment variables (optional)
export KAZO_WEB_PORT=5001
export KAZO_API_BASE_URL=http://localhost:5000

# Run the application
cd src/KazoOCR.Web
dotnet run
```

The application will be available at `http://localhost:5001`.

## Docker Integration

In `docker-compose.yml`, the web service is configured to:
- Expose port 5001
- Connect to the API service via internal Docker network
- Use environment variables for configuration

```yaml
web:
  build:
    context: .
    dockerfile: docker/Dockerfile.web
  ports:
    - "5001:5001"
  environment:
    - KAZO_WEB_PORT=5001
    - KAZO_API_BASE_URL=http://api:5000
  depends_on:
    - api
```

## Technology Stack

- **Framework**: .NET 10 / Blazor Server
- **UI**: Bootstrap 5 (included via template)
- **No external JavaScript** - pure Blazor components
- **Responsive design** for mobile and desktop
