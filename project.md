# KazoOCR — Contexte Projet

## Vision

**KazoOCR** est une solution .NET 10 multi-plateforme qui automatise la création de PDF "Sandwich" (texte sélectionnable) en pilotant **OCRmyPDF**. Distribuable via CLI (one-shot, batch & watch), Docker, Service Windows et MAUI (Microsoft Store).

## Architecture de la Solution

```
KazoOCR.sln
├── src/
│   ├── KazoOCR.Core/          # Bibliothèque métier (.NET 10 classlib)
│   ├── KazoOCR.CLI/           # Application console + Watch (.NET 10)
│   ├── KazoOCR.Docker/        # Worker Service — lance Core en mode watch (.NET 10)
│   ├── KazoOCR.Api/           # ASP.NET Core Web API + Worker intégré (.NET 10)  [iteration-5]
│   ├── KazoOCR.Web/           # Blazor Web App — interface web (.NET 10)          [iteration-5]
│   └── KazoOCR.UI/            # Application MAUI (Windows Desktop)
├── tests/
│   └── KazoOCR.Tests/         # Tests xUnit
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
├── .github/
│   └── workflows/
│       ├── pr-check.yml
│       ├── auto-release.yml
│       └── dockerhub.yml
├── docs/
│   ├── architecture.md
│   ├── core.md
│   ├── cli.md
│   ├── docker.md
│   ├── service.md
│   ├── ui.md
│   ├── api.md                 # Documentation Web API          [iteration-5]
│   └── web.md                 # Documentation Web UI           [iteration-5]
└── README.md
```

## Dépendances entre projets

```
KazoOCR.CLI ──────► KazoOCR.Core
KazoOCR.Docker ───► KazoOCR.Core
KazoOCR.Api ──────► KazoOCR.Core                              [iteration-5]
KazoOCR.Web ──────► KazoOCR.Api (HTTP)                        [iteration-5]
KazoOCR.UI ───────► KazoOCR.Core
KazoOCR.Tests ────► KazoOCR.Core + KazoOCR.CLI + KazoOCR.Api + KazoOCR.Web
```

## Composants clés

### KazoOCR.Core (classlib)
- **Aucune dépendance externe** — bibliothèque métier pure
- `IOcrFileService` / `OcrFileService` : renommage (`_OCR` suffix), validation des fichiers PDF, détection de fichiers déjà traités
- `IOcrProcessRunner` / `OcrProcessRunner` : wrapper cross-platform autour de `ocrmypdf` (Linux natif / Windows via WSL)
- `OcrSettings` : POCO de configuration (Suffix, Languages, Deskew, Clean, Rotate, Optimize)
- `IEnvironmentDetector` / `EnvironmentDetector` : détection WSL, ocrmypdf, Tesseract, unpaper
- `IEnvironmentInstaller` / `EnvironmentInstaller` : installation automatique des dépendances via apt-get (WSL ou natif)
- `IPrivilegeElevator` / `PrivilegeElevator` : vérification et élévation de droits admin (Windows `runas` / Linux `sudo`)
- `WatcherService` : surveillance de dossier via `FileSystemWatcher` + `Channel<string>` — consommation asynchrone des fichiers PDF détectés
- `ValidationResult`, `ProcessResult` : types de retour

### KazoOCR.CLI (console)
- Package `CommandDotNet` (≥8.0) pour le mapping d'arguments par attributs
- Package `Microsoft.Extensions.Hosting` pour le mode Watch et le support Service Windows
- Commandes : `ocr` (one-shot/batch), `watch` (surveillance), `install` (dépendances), `service install/uninstall/status`
- Appel direct aux services de Core via injection de dépendances

### KazoOCR.Docker (Worker Service)
- `BackgroundService` minimal qui référence Core et lance `WatcherService`
- Configuration via variables d'environnement (`KAZO_WATCH_PATH`, `KAZO_SUFFIX`, `KAZO_LANGUAGES`, etc.)
- Image Docker multi-stage : SDK pour build, runtime + ocrmypdf pour exécution
- **[iteration-5]** Reconfigured to host both `KazoOCR.Api` and `KazoOCR.Web` alongside the worker

### KazoOCR.Api (ASP.NET Core Web API) — [iteration-5]
- ASP.NET Core 10 project exposing a REST API consumed by `KazoOCR.Web`
- **Embedded `BackgroundService`** — runs `WatcherService` from Core in-process
- **Swagger / OpenAPI** documentation via `Swashbuckle.AspNetCore`
- **Authentication**:
  - `KAZO_API_KEY` env var — all requests require `X-Api-Key` header when set; no auth enforced if unset
  - `KAZO_DEFAULT_PASSWORD` env var — used for web-UI login; if unset, password creation is prompted on first run
- **Configuration**:
  | Variable | Default | Description |
  |----------|---------|-------------|
  | `KAZO_API_PORT` | `5000` | HTTP port exposed by the API |
  | `KAZO_API_KEY` | *(none)* | Static API key; authentication disabled when empty |
  | `KAZO_DEFAULT_PASSWORD` | *(none)* | Web-UI password; first-run wizard when empty |
  | `KAZO_WATCH_PATH` | `/data` | Folder watched by the embedded worker |
- Endpoints: `POST /ocr/process`, `GET /ocr/status/{id}`, `GET /ocr/jobs`, `DELETE /ocr/jobs/{id}`, `GET /health`

### KazoOCR.Web (Blazor Web App) — [iteration-5]
- Blazor Server or Blazor Web App (.NET 10) served alongside `KazoOCR.Api` (separate service in docker-compose)
- **Pages**: Dashboard (job list + status), Upload (drag-and-drop PDF), Settings (OCR options), First-run wizard (password creation)
- Communicates with `KazoOCR.Api` via typed `HttpClient`
- **Port** configurable via `KAZO_WEB_PORT` (default `5001`)
- Responsive layout; no JavaScript framework — pure Blazor components

### KazoOCR.UI (MAUI)
- Vue principale avec Drag & Drop de fichiers PDF
- Options OCR (checkboxes, slider) + barre de progression
- Build conditionnel : compilé uniquement sur Windows ou via `/p:BuildMAUI=true`

### KazoOCR.Tests (xUnit)
- Packages : `xunit`, `Moq`, `FluentAssertions`
- Couverture cible > 80% sur Core
- Tests sur Ubuntu et Windows (CI matrix)

## Packages NuGet

| Projet | Package | Rôle |
|--------|---------|------|
| Core | — | Aucune dépendance externe |
| CLI | `CommandDotNet` (≥8.0) | Mapping d'arguments par attributs |
| CLI | `Microsoft.Extensions.Hosting` | Host pour le mode Watch / Service Windows |
| CLI | `Microsoft.Extensions.Hosting.WindowsServices` | Support `UseWindowsService()` |
| Docker | `Microsoft.Extensions.Hosting` | Worker Service |
| Api | `Microsoft.AspNetCore.OpenApi` | OpenAPI / Swagger support |
| Api | `Swashbuckle.AspNetCore` | Swagger UI |
| Api | `Microsoft.Extensions.Hosting` | Host + BackgroundService |
| Web | `Microsoft.AspNetCore.Components.WebAssembly` | Blazor Web App |
| UI | `Microsoft.Maui.*` | Framework UI |
| Tests | `xunit`, `Moq`, `FluentAssertions` | Tests unitaires |
| Tests | `Microsoft.AspNetCore.Mvc.Testing` | Integration tests (WebApplicationFactory) |

## Modes d'exécution

| Mode | Commande | Usage |
|------|----------|-------|
| **One-shot** | `kazoocr ocr -i fichier.pdf` | Traitement unique |
| **Batch** | `kazoocr ocr -i dossier/` | Traitement récursif d'un dossier |
| **Watch** | `kazoocr watch -i dossier/` | Surveillance continue |
| **Service install** | `kazoocr service install` | Installe le service Windows |
| **Service uninstall** | `kazoocr service uninstall` | Désinstalle le service Windows |
| **Docker** | `docker-compose up` | Conteneur avec volume monté |
| **API** | `http://localhost:5000/swagger` | Swagger UI |
| **Web UI** | `http://localhost:5001` | Interface web Blazor |

## Cross-platform : stratégie WSL

- **Linux/macOS** : appel direct à `ocrmypdf`
- **Windows** : appel via `wsl ocrmypdf` avec conversion des chemins (`wslpath`)
- La détection se fait via `System.Runtime.InteropServices.RuntimeInformation`

## Conventions

- Target framework : `net10.0` (MAUI : `net10.0-windows10.0.19041.0`)
- `Directory.Build.props` : LangVersion=latest, Nullable=enable, ImplicitUsings=enable
- `.editorconfig` : analyzer rules and code style configuration
- Branches : `main` ← `develop` ← `feature/issue-X.Y`
- CI/CD : GitHub Actions (PR check, auto-release, DockerHub push)

## Coding Guidelines

### Code Quality Rules

The project uses `.editorconfig` to enforce code quality rules. Key guidelines:

1. **Avoid unused variable assignments** — If a method return value is not used, call the method without assigning to a variable:
   ```csharp
   // Bad: Unused assignment triggers IDE0059
   var result = await SomeMethodAsync();
   
   // Good: Just await without assignment if result not used
   await SomeMethodAsync();
   ```

2. **Use conditional expressions over if-else for assignments** — When both branches assign to the same variable, use ternary:
   ```csharp
   // Bad: Explicit if-else for same variable
   ProcessResult result;
   if (IsWindows())
       result = await RunWindowsAsync();
   else
       result = await RunLinuxAsync();
   
   // Good: Conditional expression
   var result = IsWindows()
       ? await RunWindowsAsync()
       : await RunLinuxAsync();
   ```

3. **Use LINQ over foreach for filtering** — Prefer `Any()`, `Where()`, `FirstOrDefault()`:
   ```csharp
   // Bad: Foreach with if filter
   foreach (var line in lines)
   {
       if (line.Equals(target))
           return true;
   }
   return false;
   
   // Good: LINQ Any()
   return lines.Any(line => line.Equals(target));
   ```

### IDisposable Pattern
- Always dispose `IDisposable` objects using `using` or `using var` statements
- Example: `using var cts = new CancellationTokenSource();`

### Path Handling
- Prefer `Path.Join()` over `Path.Combine()` to avoid silent argument dropping
- `Path.Combine()` resets to root if any argument is rooted; `Path.Join()` simply concatenates
- When combining paths with potentially untrusted input, sanitize with `Path.GetFileName()`

```csharp
// GOOD: Use Path.Join for safe path concatenation
var fullPath = Path.Join(tempDir, "file.pdf");
var directoryName = $"folder-{id}";
var result = Path.Join(basePath, directoryName);

// BAD: Path.Combine may drop earlier arguments
var fullPath = Path.Combine(tempDir, untrustedInput); // Could be rooted!
```

### Analyzer Rules

Key diagnostics enabled as warnings in `.editorconfig`:
- `CS0219` — Variable assigned but never used
- `IDE0059` — Unnecessary assignment
- `CA1508` — Dead conditional code
- `CA2000` — Dispose IDisposable before losing scope
- `CA1860` — Prefer `Any()` over `Count() > 0`

### Code Analysis
- Configure analyzer rules in `.editorconfig`
- Run `dotnet build` with warnings treated as errors (`TreatWarningsAsErrors=true`)
- Review and fix all github-code-quality comments before merging

## Iterations

1. **Fondations & Core Logic** : structure solution, services de renommage/validation, wrapper process, README/docs, tests
2. **CLI & Environnement** : CommandDotNet, détection WSL, élévation de privilèges
3. **Watch, Docker & Distribution** : commande watch, Worker Service, Dockerfile, docker-compose
4. **Service Windows & UI MAUI** : installation service Windows, interface MAUI Drag & Drop
5. **Web API & Web UI** : `KazoOCR.Api` (ASP.NET Core REST API + Swagger + auth + embedded worker), `KazoOCR.Web` (Blazor dashboard + upload + first-run wizard), reconfiguration Docker pour multi-service, tests d'intégration Web
