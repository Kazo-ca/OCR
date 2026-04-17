# Architecture

> This document describes the overall architecture of the KazoOCR solution.

## Overview

KazoOCR is a .NET 10 solution designed for cross-platform PDF OCR processing. The architecture follows a modular approach with a shared Core library consumed by multiple front-end applications.

## Solution Structure

```
KazoOCR.sln
├── src/
│   ├── KazoOCR.Core/          # Business logic library (.NET 10)
│   ├── KazoOCR.CLI/           # Console application (.NET 10)
│   ├── KazoOCR.Docker/        # Worker Service (.NET 10)
│   └── KazoOCR.UI/            # MAUI Desktop application
├── tests/
│   └── KazoOCR.Tests/         # xUnit tests
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
└── docs/
```

## Project Dependencies

```
KazoOCR.CLI ──────► KazoOCR.Core
KazoOCR.Docker ───► KazoOCR.Core
KazoOCR.UI ───────► KazoOCR.Core
KazoOCR.Tests ────► KazoOCR.Core + KazoOCR.CLI
```

## Key Design Decisions

### Cross-Platform Strategy

- **Linux/macOS**: Direct invocation of `ocrmypdf` command
- **Windows**: Invocation via WSL (`wsl ocrmypdf`) with path conversion using `wslpath`

### Watch Architecture

The watch functionality (folder monitoring) is implemented in `KazoOCR.Core` using:
- `FileSystemWatcher` for file system events
- `Channel<string>` for asynchronous processing queue

This allows both CLI and Docker projects to share the same watch implementation.

## NuGet Packages

| Project | Package | Purpose |
|---------|---------|---------|
| Core | — | No external dependencies |
| CLI | `CommandDotNet` | Argument mapping via attributes |
| CLI | `Microsoft.Extensions.Hosting` | Host for watch/service modes |
| CLI | `Microsoft.Extensions.Hosting.WindowsServices` | Windows Service support |
| Docker | `Microsoft.Extensions.Hosting` | Worker Service |
| UI | `Microsoft.Maui.*` | UI framework |
| Tests | `xunit`, `Moq`, `FluentAssertions` | Testing |

## Related Documentation

- [Core](core.md) — Core library details
- [CLI](cli.md) — CLI application
- [Docker](docker.md) — Docker deployment
- [Service](service.md) — Windows Service
- [UI](ui.md) — MAUI application
