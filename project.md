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
│   └── ui.md
└── README.md
```

## Dépendances entre projets

```
KazoOCR.CLI ──────► KazoOCR.Core
KazoOCR.Docker ───► KazoOCR.Core
KazoOCR.UI ───────► KazoOCR.Core
KazoOCR.Tests ────► KazoOCR.Core + KazoOCR.CLI
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
| UI | `Microsoft.Maui.*` | Framework UI |
| Tests | `xunit`, `Moq`, `FluentAssertions` | Tests unitaires |

## Modes d'exécution

| Mode | Commande | Usage |
|------|----------|-------|
| **One-shot** | `kazoocr ocr -i fichier.pdf` | Traitement unique |
| **Batch** | `kazoocr ocr -i dossier/` | Traitement récursif d'un dossier |
| **Watch** | `kazoocr watch -i dossier/` | Surveillance continue |
| **Service install** | `kazoocr service install` | Installe le service Windows |
| **Service uninstall** | `kazoocr service uninstall` | Désinstalle le service Windows |
| **Docker** | `docker-compose up` | Conteneur avec volume monté |

## Cross-platform : stratégie WSL

- **Linux/macOS** : appel direct à `ocrmypdf`
- **Windows** : appel via `wsl ocrmypdf` avec conversion des chemins (`wslpath`)
- La détection se fait via `System.Runtime.InteropServices.RuntimeInformation`

## Conventions

- Target framework : `net10.0` (MAUI : `net10.0-windows10.0.19041.0`)
- `Directory.Build.props` : LangVersion=latest, Nullable=enable, ImplicitUsings=enable
- Branches : `main` ← `develop` ← `feature/issue-X.Y`
- CI/CD : GitHub Actions (PR check, auto-release, DockerHub push)

## Itérations

1. **Fondations & Core Logic** : structure solution, services de renommage/validation, wrapper process, README/docs, tests
2. **CLI & Environnement** : CommandDotNet, détection WSL, élévation de privilèges
3. **Watch, Docker & Distribution** : commande watch, Worker Service, Dockerfile, docker-compose
4. **Service Windows & UI MAUI** : installation service Windows, interface MAUI Drag & Drop
