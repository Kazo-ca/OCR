# KazoOCR — Plan d'Exécution Complet

## 🎯 Vision

**KazoOCR** est une solution .NET 10 multi-plateforme qui automatise la création de PDF "Sandwich" (texte sélectionnable) en pilotant **OCRmyPDF**. Distribuable via CLI (one-shot & watch), Docker, Service Windows et MAUI (Microsoft Store).

---

## 📐 Architecture de la Solution

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

> **Architecture Watch :** La logique de **watch** (FileSystemWatcher + Channel) est dans `KazoOCR.Core`. Le CLI expose la commande `watch`. Le projet `KazoOCR.Docker` est un **Worker Service** minimal qui référence Core et lance la surveillance au démarrage — c'est le point d'entrée du conteneur Docker. Le CLI supporte aussi l'installation en tant que **Service Windows** via `sc.exe create` (itération 4).

### Dépendances entre projets

```
KazoOCR.CLI ──────► KazoOCR.Core
KazoOCR.Docker ───► KazoOCR.Core
KazoOCR.UI ───────► KazoOCR.Core
KazoOCR.Tests ────► KazoOCR.Core + KazoOCR.CLI
```

### Packages NuGet envisagés

| Projet | Package | Rôle |
|--------|---------|------|
| Core | — | Aucune dépendance externe |
| CLI | `CommandDotNet` (≥8.0) | Mapping d'arguments par attributs |
| CLI | `Microsoft.Extensions.Hosting` | Host pour le mode Watch |
| Docker | `Microsoft.Extensions.Hosting` | Worker Service (point d'entrée conteneur) |
| UI | `Microsoft.Maui.*` | Framework UI multi-plateforme |
| Tests | `xunit`, `Moq`, `FluentAssertions` | Tests unitaires |

### Modes d'exécution du CLI

| Mode | Commande | Usage |
|------|----------|-------|
| **One-shot** | `kazoocr ocr -i fichier.pdf` | Traitement unique |
| **Batch** | `kazoocr ocr -i dossier/` | Traitement récursif d'un dossier |
| **Watch** | `kazoocr watch -i dossier/` | Surveillance continue (Docker, terminal) |
| **Service install** | `kazoocr service install` | Installe le service Windows (appsettings.service.json) |
| **Service uninstall** | `kazoocr service uninstall` | Désinstalle le service Windows |

---

## 📋 Product Backlog — GitHub Issues

### Labels

| Label | Couleur | Description |
|-------|---------|-------------|
| `iteration-1` | `#0E8A16` | Fondations & Core Logic |
| `iteration-2` | `#1D76DB` | CLI & Environnement |
| `iteration-3` | `#D93F0B` | Watch, Docker & Distribution |
| `iteration-4` | `#7057FF` | Service Windows & UI MAUI |
| `iteration-5` | `#E4B429` | Web API & Web UI |
| `ci-cd` | `#FBCA04` | Workflows GitHub Actions |
| `documentation` | `#C5DEF5` | Documentation |
| `core` | `#B60205` | KazoOCR.Core |
| `cli` | `#5319E7` | KazoOCR.CLI |
| `docker` | `#006B75` | Docker |
| `service` | `#0075CA` | Service Windows |
| `api` | `#0052CC` | KazoOCR.Api |
| `web` | `#00B4D8` | KazoOCR.Web |
| `ui` | `#F9D0C4` | KazoOCR.UI |
| `tests` | `#E99695` | KazoOCR.Tests |

---

### Itération 1 — Fondations & Core Logic

> **Objectif :** Structure du repo, logique métier de base, prêt à compiler.

#### Issue 1.1 — Initialisation de la solution .NET 10
- **Labels :** `iteration-1`
- **Estimation :** 30 min
- **Description :**
  - Créer `KazoOCR.sln` à la racine
  - Créer les 5 projets dans `src/` et `tests/` :
    - `KazoOCR.Core` (classlib, net10.0)
    - `KazoOCR.CLI` (console, net10.0)
    - `KazoOCR.Docker` (worker, net10.0)
    - `KazoOCR.UI` (MAUI, net10.0-windows10.0.19041.0) — build conditionnel
    - `KazoOCR.Tests` (xunit, net10.0)
  - Configurer les `ProjectReference` entre projets
  - Ajouter `Directory.Build.props` avec les propriétés communes (LangVersion, Nullable, ImplicitUsings)
  - Condition MAUI : compile uniquement sur Windows ou via `/p:BuildMAUI=true`
- **Critères d'acceptation :**
  - [ ] `dotnet build KazoOCR.sln` réussit (hors MAUI sur Linux)
  - [ ] Structure des dossiers conforme au schéma d'architecture

#### Issue 1.2 — Core : Service de renommage et validation
- **Labels :** `iteration-1`, `core`
- **Estimation :** 30 min
- **Description :**
  - Créer `IOcrFileService` et `OcrFileService` dans Core :
    - `string ComputeOutputPath(string inputPath, string suffix)` — Calcule le chemin de sortie (`input_OCR.pdf`)
    - `bool IsAlreadyProcessed(string filePath, string suffix)` — Détecte si le fichier contient déjà le suffixe
    - `ValidationResult ValidateInput(string path)` — Vérifie existence, extension (.pdf), permissions
  - Créer `OcrSettings` (POCO) avec les propriétés : `Suffix`, `Languages`, `Deskew`, `Clean`, `Rotate`, `Optimize`
  - Créer `ValidationResult` avec `IsValid`, `Errors`
- **Critères d'acceptation :**
  - [x] Tests unitaires pour chaque méthode
  - [x] Gestion des cas limites (chemin null, fichier inexistant, extension invalide)

#### Issue 1.3 — Core : Wrapper ProcessStartInfo cross-platform
- **Labels :** `iteration-1`, `core`
- **Estimation :** 30 min
- **Description :**
  - Créer `IOcrProcessRunner` et `OcrProcessRunner` :
    - `Task<ProcessResult> RunAsync(OcrSettings settings, string inputPath, string outputPath, CancellationToken ct)`
  - Logique cross-platform :
    - **Linux/macOS :** Appel direct à `ocrmypdf`
    - **Windows :** Appel via `wsl ocrmypdf` avec conversion des chemins (`wslpath`)
  - Créer `ProcessResult` avec `ExitCode`, `StandardOutput`, `StandardError`
  - Construction de la ligne de commande ocrmypdf à partir de `OcrSettings` :
    ```
    ocrmypdf --deskew --clean --rotate-pages --optimize 1 -l fra+eng input.pdf output.pdf
    ```
- **Critères d'acceptation :**
  - [x] Détection correcte de l'OS via `RuntimeInformation`
  - [x] Conversion de chemins Windows → WSL fonctionnelle
  - [x] Tests unitaires avec mock du Process

#### Issue 1.4 — README.md et structure /docs
- **Labels :** `iteration-1`, `documentation`
- **Estimation :** 30 min
- **Description :**
  - Rédiger `README.md` racine avec :
    - Badges CI/CD (placeholder)
    - Description du projet, features
    - Quickstart (CLI, Docker, MAUI)
    - Table des paramètres CLI
    - Lien vers `/docs`
  - Créer la structure `/docs` avec fichiers stub :
    - `architecture.md` — Diagramme et description des projets
    - `core.md`, `cli.md`, `docker.md`, `ui.md` — Un fichier par module
- **Critères d'acceptation :**
  - [x] README fonctionnel avec instructions d'installation
  - [x] Fichiers /docs créés avec headers et structure

---

### Itération 2 — CLI & Environnement

> **Objectif :** CLI fonctionnelle avec aide auto-générée, détection WSL et élévation de privilèges.

#### Issue 2.1 — CLI : Mapping d'arguments par attributs
- **Labels :** `iteration-2`, `cli`
- **Estimation :** 30 min
- **Description :**
  - Installer `CommandDotNet` dans KazoOCR.CLI
  - Créer la classe `OcrCommand` avec les attributs CommandDotNet :
    ```csharp
    public class OcrCommand
    {
        public Task<ExitCodes> Ocr(
            [Option('i', Description = "Fichier ou dossier source")] string input,
            [Option('s', Description = "Suffixe du fichier produit")] string suffix = "_OCR",
            [Option('l', Description = "Codes langues Tesseract")] string languages = "fra+eng",
            [Option(Description = "Correction de l'inclinaison")] bool deskew = false,
            [Option(Description = "Nettoyage via Unpaper")] bool clean = false,
            [Option(Description = "Correction d'orientation")] bool rotate = false,
            [Option(Description = "Niveau de compression (0-3)")] int optimize = 1
        ) { ... }
    }
    ```
  - Configurer `AppRunner` dans `Program.cs` avec middleware (help, version, error handling)
  - Traitement par lot si `input` est un dossier (parcours récursif des `.pdf`)
- **Critères d'acceptation :**
  - [x] `kazoocr --help` affiche l'aide formatée
  - [x] `kazoocr ocr -i fichier.pdf` lance le traitement
  - [x] `kazoocr ocr -i dossier/` traite tous les PDF récursivement

#### Issue 2.2 — Détecteur WSL et auto-installation
- **Labels :** `iteration-2`, `core`
- **Estimation :** 30 min
- **Description :**
  - Créer `IEnvironmentDetector` et `EnvironmentDetector` dans Core :
    - `bool IsWslAvailable()` — Vérifie `wsl --status` (exit code 0)
    - `bool IsOcrMyPdfInstalled()` — Vérifie `(wsl) which ocrmypdf`
    - `bool IsTesseractLangInstalled(string lang)` — Vérifie le pack de langue
    - `bool IsUnpaperInstalled()` — Vérifie `(wsl) which unpaper`
  - Créer `IEnvironmentInstaller` et `EnvironmentInstaller` :
    - `Task InstallDependenciesAsync()` — Exécute les commandes apt-get via WSL
    - Script : `sudo apt-get update && sudo apt-get install -y ocrmypdf tesseract-ocr-fra unpaper`
  - Ajouter commande CLI `--install` / `install` pour lancer la vérification/installation
- **Critères d'acceptation :**
  - [x] Détection correcte sur Windows (WSL) et Linux (natif)
  - [x] `kazoocr install` installe les dépendances manquantes
  - [x] Messages clairs à l'utilisateur sur l'état de chaque dépendance

#### Issue 2.3 — Élévation de privilèges Windows
- **Labels :** `iteration-2`, `cli`
- **Estimation :** 30 min
- **Description :**
  - Créer `IPrivilegeElevator` et `PrivilegeElevator` dans Core :
    - `bool IsElevated()` — Vérifie si le process actuel a les droits admin
    - `Task<bool> RelaunchElevatedAsync(string[] args)` — Relance via `runas`
  - Intercepter `UnauthorizedAccessException` dans le pipeline CLI
  - Sur Windows uniquement : proposer à l'utilisateur de relancer en admin
    ```csharp
    new ProcessStartInfo
    {
        FileName = Environment.ProcessPath,
        Arguments = string.Join(" ", args),
        Verb = "runas",
        UseShellExecute = true
    }
    ```
  - Sur Linux : afficher un message suggérant `sudo`
- **Critères d'acceptation :**
  - [x] L'élévation est proposée, jamais forcée
  - [x] Les arguments originaux sont préservés lors de la relance
  - [x] Le mécanisme est conditionnel à Windows (`RuntimeInformation`)

> **🔄 Tâche Pivot** — À la fin de l'itération 2, évaluer :
> - Le bridge WSL fonctionne-t-il de manière fiable ?
> - Les chemins Windows ↔ WSL sont-ils convertis correctement ?
> - Si problème : ajuster l'itération 3 (prioriser Docker, simplifier UI)

---

### Itération 3 — Watch, Docker & Distribution

> **Objectif :** Mode watch dans le CLI, projet Docker Worker, image Docker fonctionnelle.

#### Issue 3.1 — CLI : Commande Watch avec FileSystemWatcher
- **Labels :** `iteration-3`, `cli`
- **Estimation :** 30 min
- **Description :**
  - Implémenter la commande `watch` dans le CLI :
    ```csharp
    public Task<ExitCodes> Watch(
        [Option('i', Description = "Dossier à surveiller")] string input,
        [Option('s')] string suffix = "_OCR",
        [Option('l')] string languages = "fra+eng",
        [Option] bool deskew = false,
        [Option] bool clean = false,
        [Option] bool rotate = false,
        [Option] int optimize = 1
    )
    ```
  - Logique dans Core : `WatcherService` (FileSystemWatcher + `Channel<string>`)
    - Filtre : `*.pdf` uniquement
    - Ignore les fichiers contenant le suffixe → évite les boucles
  - Consumer asynchrone qui appelle `IOcrProcessRunner` pour chaque fichier
  - Logging structuré via `ILogger`
  - Supporte `CancellationToken` pour arrêt propre (Ctrl+C)
- **Critères d'acceptation :**
  - [ ] `kazoocr watch -i dossier/` surveille et traite les nouveaux PDF
  - [ ] Les fichiers `*_OCR.pdf` ne déclenchent pas de retraitement
  - [ ] Arrêt propre via Ctrl+C ou signal SIGTERM

#### Issue 3.2 — Docker : Projet Worker Service
- **Labels :** `iteration-3`, `docker`
- **Estimation :** 30 min
- **Description :**
  - Implémenter `KazoOCR.Docker` comme Worker Service (`BackgroundService`) :
    - Référence `KazoOCR.Core`
    - Lit la configuration via variables d'environnement
    - Utilise le `WatcherService` de Core pour surveiller `KAZO_WATCH_PATH`
  - Configuration via variables d'environnement :
    | Variable | Défaut | Description |
    |----------|--------|-------------|
    | `KAZO_WATCH_PATH` | `/watch` | Dossier surveillé |
    | `KAZO_SUFFIX` | `_OCR` | Suffixe de sortie |
    | `KAZO_LANGUAGES` | `fra+eng` | Langues Tesseract |
    | `KAZO_DESKEW` | `true` | Correction inclinaison |
    | `KAZO_CLEAN` | `false` | Nettoyage Unpaper |
    | `KAZO_ROTATE` | `true` | Correction orientation |
    | `KAZO_OPTIMIZE` | `1` | Niveau compression |
  - `Program.cs` minimal : configure le Host, enregistre les services Core, lance le worker
- **Critères d'acceptation :**
  - [ ] Le Worker démarre et surveille le dossier configuré
  - [ ] Les variables d'environnement surchargent les défauts
  - [ ] Logging structuré fonctionnel

#### Issue 3.3 — Dockerfile multi-stage
- **Labels :** `iteration-3`, `docker`, `ci-cd`
- **Estimation :** 30 min
- **Description :**
  - Créer `docker/Dockerfile` multi-stage :
    - **Stage 1 (build):** `mcr.microsoft.com/dotnet/sdk:10.0` — Compile KazoOCR.Docker
    - **Stage 2 (runtime):** `mcr.microsoft.com/dotnet/runtime:10.0` + installation OCRmyPDF
      ```dockerfile
      RUN apt-get update && apt-get install -y --no-install-recommends \
          ocrmypdf tesseract-ocr-fra unpaper \
          && rm -rf /var/lib/apt/lists/*
      ```
  - Créer `docker-compose.yml` pour usage simplifié :
    ```yaml
    services:
      kazoocr:
        build: .
        volumes:
          - ./watch:/watch
          - ./output:/output
        environment:
          - KAZO_WATCH_PATH=/watch
          - KAZO_SUFFIX=_OCR
    ```
  - Documenter dans `docs/docker.md`
- **Critères d'acceptation :**
  - [ ] `docker build` réussit
  - [ ] Le conteneur surveille le dossier monté et traite les PDF
  - [ ] Image optimisée (taille minimale)

---

### Itération 4 — Service Windows & UI MAUI

> **Objectif :** Installation/désinstallation du CLI en service Windows, et UI MAUI.

#### Issue 4.1 — CLI : Installation en Service Windows
- **Labels :** `iteration-4`, `cli`, `service`
- **Estimation :** 30 min
- **Description :**
  - Ajouter les commandes `service install` et `service uninstall` au CLI :
    ```
    kazoocr service install    # Enregistre le service Windows
    kazoocr service uninstall  # Supprime le service Windows
    kazoocr service status     # Affiche l'état du service
    ```
  - Utiliser `sc.exe create` / `sc.exe delete` pour gérer le service
  - Le service exécute le CLI en mode watch avec la config de `appsettings.service.json`
  - Fichier `appsettings.service.json` :
    ```json
    {
      "WatchFolders": [
        {
          "Path": "C:\\Users\\Public\\Documents\\OCR\\Input",
          "Suffix": "_OCR",
          "Languages": "fra+eng",
          "Deskew": true,
          "Clean": false,
          "Rotate": true,
          "Optimize": 1
        },
        {
          "Path": "D:\\Scans",
          "Suffix": "_OCR",
          "Languages": "fra",
          "Deskew": true
        }
      ]
    }
    ```
  - Support de **plusieurs dossiers** surveillés simultanément
  - Élévation automatique requise (admin) pour install/uninstall
  - Ajouter `Microsoft.Extensions.Hosting.WindowsServices` au CLI pour le support `UseWindowsService()`
- **Critères d'acceptation :**
  - [ ] `kazoocr service install` crée un service Windows visible dans `services.msc`
  - [ ] Le service surveille tous les dossiers configurés dans `appsettings.service.json`
  - [ ] `kazoocr service uninstall` supprime proprement le service
  - [ ] Documentation dans `docs/service.md`

#### Issue 4.2 — UI MAUI : Vue Drag & Drop
- **Labels :** `iteration-4`, `ui`
- **Estimation :** 30 min
- **Description :**
  - Créer la vue principale `MainPage.xaml` :
    - Zone de Drag & Drop pour fichiers PDF
    - Sélection de fichier/dossier via FilePicker
    - Options OCR (checkboxes : Deskew, Clean, Rotate ; slider Optimize)
    - Barre de progression et log d'exécution
  - Intégration avec `KazoOCR.Core` (mêmes services que la CLI)
  - Build conditionnel dans le `.csproj` :
    ```xml
    <PropertyGroup Condition="$([MSBuild]::IsOSPlatform('Windows')) OR '$(BuildMAUI)' == 'true'">
      <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
    </PropertyGroup>
    ```
- **Critères d'acceptation :**
  - [ ] Drag & Drop fonctionnel
  - [ ] Le traitement OCR est lancé via Core
  - [ ] Le build n'échoue pas sur Linux (projet exclu)

---

### CI/CD — GitHub Actions Workflows

#### Issue CI.1 — Workflow PR Check
- **Labels :** `ci-cd`
- **Estimation :** 30 min
- **Description :**
  - Fichier : `.github/workflows/pr-check.yml`
  - Déclencheur : `pull_request` vers `main` (excl. draft)
  - Jobs :
    1. **build** : `dotnet build` (Ubuntu + Windows)
    2. **test** : `dotnet test` avec couverture
  - Matrice : `os: [ubuntu-latest, windows-latest]`
  - Condition de skip MAUI sur Ubuntu
- **Critères d'acceptation :**
  - [x] Le workflow se déclenche sur PR non-draft
  - [x] Build et tests passent sur les deux OS

#### Issue CI.2 — Workflow Auto-Release
- **Labels :** `ci-cd`
- **Estimation :** 30 min
- **Description :**
  - Fichier : `.github/workflows/auto-release.yml`
  - Déclencheur : push de tag `v*.*.*`
  - Jobs :
    1. Build en mode Release
    2. `dotnet publish` pour CLI (linux-x64, win-x64, osx-x64)
    3. Création de la Release GitHub avec les artifacts
  - Utiliser `actions/create-release` et `actions/upload-release-asset`
- **Critères d'acceptation :**
  - [x] Tag `v1.0.0` → Release GitHub automatique
  - [x] Binaires CLI joints à la release

#### Issue CI.3 — Workflow DockerHub
- **Labels :** `ci-cd`
- **Estimation :** 30 min
- **Description :**
  - Fichier : `.github/workflows/dockerhub.yml`
  - Déclencheur : publication d'une release GitHub
  - Jobs :
    1. Build de l'image Docker
    2. Tag avec la version de la release
    3. Push vers DockerHub (`kazoca/kazoocr:latest`, `kazoca/kazoocr:v1.0.0`)
  - Secrets requis : `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN`
- **Critères d'acceptation :**
  - [x] Image poussée sur DockerHub à chaque release
  - [x] Tags `latest` et version spécifique

---

### Tests

#### Issue T.1 — Tests unitaires Core & CLI
- **Labels :** `iteration-1`, `tests`
- **Estimation :** 30 min (évolue avec chaque itération)
- **Description :**
  - Tests pour `OcrFileService` :
    - `ComputeOutputPath` : cas normaux, chemins avec espaces, caractères spéciaux
    - `IsAlreadyProcessed` : détection du suffixe
    - `ValidateInput` : fichier inexistant, mauvaise extension, null
  - Tests pour `OcrProcessRunner` :
    - Construction correcte de la ligne de commande
    - Détection OS et choix du mode (WSL vs natif)
  - Tests pour `EnvironmentDetector` (avec mocks)
  - Tests pour le mapping CLI (CommandDotNet integration tests)
- **Critères d'acceptation :**
  - [x] Couverture > 80% sur Core
  - [x] Tous les tests passent sur Ubuntu et Windows

---

## 📊 Récapitulatif des Issues

| # | Titre | Labels | Itération |
|---|-------|--------|-----------|
| 1.1 | Initialisation solution .NET 10 et 5 projets | `iteration-1` | 1 |
| 1.2 | Core : Service de renommage et validation | `iteration-1`, `core` | 1 |
| 1.3 | Core : Wrapper ProcessStartInfo cross-platform | `iteration-1`, `core` | 1 |
| 1.4 | README.md et structure /docs | `iteration-1`, `documentation` | 1 |
| T.1 | Tests unitaires Core & CLI | `iteration-1`, `tests` | 1+ |
| 2.1 | CLI : Mapping d'arguments (CommandDotNet) | `iteration-2`, `cli` | 2 |
| 2.2 | Détecteur WSL et auto-installation | `iteration-2`, `core` | 2 |
| 2.3 | Élévation de privilèges Windows | `iteration-2`, `cli` | 2 |
| 3.1 | CLI : Commande Watch (FileSystemWatcher) | `iteration-3`, `cli` | 3 |
| 3.2 | Docker : Projet Worker Service | `iteration-3`, `docker` | 3 |
| 3.3 | Dockerfile multi-stage | `iteration-3`, `docker`, `ci-cd` | 3 |
| 4.1 | CLI : Installation en Service Windows | `iteration-4`, `cli`, `service` | 4 |
| 4.2 | UI MAUI : Vue Drag & Drop | `iteration-4`, `ui` | 4 |
| 5.1 | Web API : Projet KazoOCR.Api | `iteration-5`, `api` | 5 |
| 5.2 | Web API : Swagger / OpenAPI | `iteration-5`, `api`, `documentation` | 5 |
| 5.3 | Web API : Authentication (API Key + Password) | `iteration-5`, `api` | 5 |
| 5.4 | Web UI : Projet KazoOCR.Web (Blazor) | `iteration-5`, `web` | 5 |
| 5.5 | Web UI : First-run wizard (password creation) | `iteration-5`, `web` | 5 |
| 5.6 | Docker : Reconfiguration multi-service | `iteration-5`, `docker` | 5 |
| 5.7 | Tests : Integration tests Web API & Web UI | `iteration-5`, `tests` | 5 |
| 5.8 | Documentation : docs/api.md + docs/web.md | `iteration-5`, `documentation` | 5 |
| CI.1 | Workflow PR Check | `ci-cd` | 1 |
| CI.2 | Workflow Auto-Release | `ci-cd` | 2 |
| CI.3 | Workflow DockerHub | `ci-cd` | 3 |

**Total : 24 issues — ~12h de travail estimé**

---

### Itération 5 — Web API & Web UI

> **Objectif :** Exposer KazoOCR via une API REST documentée et une interface web Blazor hébergées dans le conteneur Docker.

#### Issue 5.1 — Web API : Projet KazoOCR.Api
- **Labels :** `iteration-5`, `api`
- **Estimation :** 45 min
- **Référence :** See [project.md §KazoOCR.Api](../project.md#kazoocrapiasp-net-core-web-api--iteration-5) for component details and environment variables.
- **Description :**
  - Créer `src/KazoOCR.Api/` — ASP.NET Core Web API (`net10.0`)
  - Ajouter `ProjectReference` vers `KazoOCR.Core`
  - Ajouter `KazoOCR.Api` à `KazoOCR.sln`
  - Enregistrer les services Core via DI (IOcrFileService, IOcrProcessRunner, WatcherService)
  - Embarquer un `BackgroundService` (`OcrWorkerBackgroundService`) qui exécute `WatcherService`
  - Endpoints REST :
    | Method | Route | Description |
    |--------|-------|-------------|
    | POST | `/api/ocr/process` | Submit a PDF for OCR (multipart/form-data) |
    | GET | `/api/ocr/jobs` | List all jobs with status |
    | GET | `/api/ocr/jobs/{id}` | Get status/result of a specific job |
    | DELETE | `/api/ocr/jobs/{id}` | Cancel / remove a job |
    | GET | `/health` | Health check endpoint |
  - Configuration via `appsettings.json` + environment variables :
    | Variable | Default | Description |
    |----------|---------|-------------|
    | `KAZO_API_PORT` | `5000` | HTTP port |
    | `KAZO_WATCH_PATH` | `/data` | Folder watched by embedded worker |
    | `KAZO_SUFFIX` | `_OCR` | Output file suffix |
    | `KAZO_LANGUAGES` | `fra+eng` | Tesseract language codes |
- **Critères d'acceptation :**
  - [ ] `dotnet run` démarre le serveur sur le port configuré
  - [ ] `POST /api/ocr/process` accepte un PDF et retourne un `jobId`
  - [ ] `GET /api/ocr/jobs/{id}` retourne le statut avec `Pending`, `Processing`, `Completed`, `Failed`
  - [ ] Le worker en arrière-plan surveille `KAZO_WATCH_PATH`

#### Issue 5.2 — Web API : Swagger / OpenAPI
- **Labels :** `iteration-5`, `api`, `documentation`
- **Estimation :** 30 min
- **Référence :** See [project.md §KazoOCR.Api](../project.md#kazoocrapiasp-net-core-web-api--iteration-5) for Swagger package details.
- **Description :**
  - Ajouter `Swashbuckle.AspNetCore` au projet `KazoOCR.Api`
  - Configurer Swagger UI à `/swagger`
  - Documenter tous les endpoints avec `[ProducesResponseType]`, `[SwaggerOperation]`, schémas XML
  - Activer la génération du fichier `openapi.json` dans le build Release
  - Configurer Swagger pour inclure l'en-tête `X-Api-Key` dans le SecurityDefinition
- **Critères d'acceptation :**
  - [ ] `http://localhost:5000/swagger` affiche l'UI Swagger complète
  - [ ] Tous les endpoints sont documentés avec exemples de réponse
  - [ ] Le fichier `openapi.json` est généré dans `wwwroot`

#### Issue 5.3 — Web API : Authentication (API Key + Password)
- **Labels :** `iteration-5`, `api`
- **Estimation :** 45 min
- **Référence :** See [project.md §KazoOCR.Api](../project.md#kazoocrapiasp-net-core-web-api--iteration-5) for authentication configuration details.
- **Description :**
  - **API Key** :
    - Si `KAZO_API_KEY` est défini, tous les appels API doivent inclure `X-Api-Key: <value>`
    - Retourner `401 Unauthorized` si la clé est absente ou incorrecte
    - Si `KAZO_API_KEY` est vide/absent → pas d'authentification (mode ouvert)
    - Implémenter via un `IAuthorizationFilter` ou middleware ASP.NET Core
  - **Password (Web UI)** :
    - Si `KAZO_DEFAULT_PASSWORD` est défini → utilisé comme mot de passe administrateur
    - Si `KAZO_DEFAULT_PASSWORD` est vide/absent → première connexion déclenche le wizard de création de mot de passe
    - Stocker le hash du mot de passe dans un fichier `data/auth.json` (bcrypt)
    - Endpoint `POST /api/auth/login` → retourne un JWT ou cookie de session
    - Endpoint `POST /api/auth/setup` → disponible uniquement si aucun mot de passe n'est configuré
  - Ne jamais stocker de mots de passe en clair
- **Critères d'acceptation :**
  - [ ] Sans `KAZO_API_KEY` → API accessible sans en-tête
  - [ ] Avec `KAZO_API_KEY` → `401` si header absent ou incorrect
  - [ ] Sans `KAZO_DEFAULT_PASSWORD` → `POST /api/auth/setup` accessible, wizard déclenché
  - [ ] Avec `KAZO_DEFAULT_PASSWORD` → wizard désactivé, connexion directe
  - [ ] Mots de passe hashés (bcrypt, salt unique)

#### Issue 5.4 — Web UI : Projet KazoOCR.Web (Blazor)
- **Labels :** `iteration-5`, `web`
- **Estimation :** 60 min
- **Référence :** See [project.md §KazoOCR.Web](../project.md#kazoocr-web-blazor-web-app--iteration-5) for component details and port configuration.
- **Description :**
  - Créer `src/KazoOCR.Web/` — Blazor Web App (`net10.0`)
  - Ajouter `KazoOCR.Web` à `KazoOCR.sln`
  - Configurer un `HttpClient` typé pointant vers `KazoOCR.Api` (URL via `KAZO_API_BASE_URL`)
  - Pages Blazor :
    | Page | Route | Description |
    |------|-------|-------------|
    | `Dashboard` | `/` | Job list with live status polling |
    | `Upload` | `/upload` | Drag-and-drop PDF upload + OCR options |
    | `Settings` | `/settings` | OCR defaults, watch path, suffix |
    | `Login` | `/login` | Authentication form |
    | `Setup` | `/setup` | First-run password wizard |
  - Configuration :
    | Variable | Default | Description |
    |----------|---------|-------------|
    | `KAZO_WEB_PORT` | `5001` | HTTP port for the Blazor app |
    | `KAZO_API_BASE_URL` | `http://api:5000` | Internal URL of KazoOCR.Api |
  - Responsive layout using Bootstrap (included with Blazor template)
  - No external JavaScript framework — pure Blazor components
- **Critères d'acceptation :**
  - [ ] `http://localhost:5001` affiche le dashboard
  - [ ] Upload d'un PDF via drag-and-drop déclenche un job via l'API
  - [ ] Le dashboard se met à jour automatiquement (polling ou SignalR)
  - [ ] La page Settings permet de modifier les options OCR par défaut

#### Issue 5.5 — Web UI : First-run wizard (password creation)
- **Labels :** `iteration-5`, `web`
- **Estimation :** 30 min
- **Référence :** See [project.md §KazoOCR.Api](../project.md#kazoocrapiasp-net-core-web-api--iteration-5) for first-run password behavior.
- **Description :**
  - Détecter l'état "non configuré" via `GET /api/auth/status` → retourne `{ "configured": false }`
  - Si non configuré → rediriger automatiquement vers `/setup` depuis le middleware Blazor
  - Page `/setup` :
    - Formulaire : nouveau mot de passe + confirmation
    - Validation côté client (longueur ≥ 8, complexité)
    - Appel `POST /api/auth/setup` → stocke le hash
    - Après succès → redirection vers `/login`
  - La page `/setup` est inaccessible si un mot de passe est déjà configuré (retourne 404 via l'API)
- **Critères d'acceptation :**
  - [ ] Premier démarrage sans `KAZO_DEFAULT_PASSWORD` → redirection automatique vers `/setup`
  - [ ] `/setup` inaccessible si déjà configuré
  - [ ] Validation du formulaire (complexité, confirmation)
  - [ ] Après setup → connexion fonctionnelle

#### Issue 5.6 — Docker : Reconfiguration multi-service
- **Labels :** `iteration-5`, `docker`
- **Estimation :** 45 min
- **Référence :** See [project.md §KazoOCR.Docker](../project.md#kazoocrdocker-worker-service) for Docker reconfiguration scope.
- **Description :**
  - Mettre à jour `docker/Dockerfile` pour un build multi-stage incluant `KazoOCR.Api` et `KazoOCR.Web` :
    - Stage build : compile les deux projets
    - Stage runtime api : `mcr.microsoft.com/dotnet/aspnet:10.0` + ocrmypdf + KazoOCR.Api
    - Stage runtime web : `mcr.microsoft.com/dotnet/aspnet:10.0` + KazoOCR.Web
  - Mettre à jour `docker/docker-compose.yml` avec 3 services :
    ```yaml
    services:
      worker:          # KazoOCR.Docker (watch-only mode, legacy)
      api:             # KazoOCR.Api (REST API + embedded worker)
        ports: ["${KAZO_API_PORT:-5000}:5000"]
      web:             # KazoOCR.Web (Blazor UI)
        ports: ["${KAZO_WEB_PORT:-5001}:5001"]
    volumes:
      data:            # Shared volume for PDF processing
      auth:            # Volume for auth.json persistence
    ```
  - Exposer les ports via variables d'environnement (`KAZO_API_PORT`, `KAZO_WEB_PORT`)
  - Le `worker` service est optionnel (peut être désactivé si `api` est utilisé — il embarque son propre worker)
  - Mettre à jour `docs/docker.md`
- **Critères d'acceptation :**
  - [ ] `docker-compose up` démarre les 3 services
  - [ ] `http://localhost:5000/swagger` accessible depuis l'hôte
  - [ ] `http://localhost:5001` accessible depuis l'hôte
  - [ ] Les ports sont configurables via variables d'environnement
  - [ ] Le volume `auth` persiste les données d'authentification entre les redémarrages

#### Issue 5.7 — Tests : Integration tests Web API & Web UI
- **Labels :** `iteration-5`, `tests`
- **Estimation :** 60 min
- **Référence :** See [project.md §KazoOCR.Tests](../project.md#kazoocrtests-xunit) for testing conventions.
- **Description :**
  - Ajouter `ProjectReference` vers `KazoOCR.Api` et `KazoOCR.Web` dans `KazoOCR.Tests`
  - Ajouter `Microsoft.AspNetCore.Mvc.Testing` au projet tests
  - Tests d'intégration pour `KazoOCR.Api` :
    - `POST /api/ocr/process` sans API key → `200` si non configuré, `401` si configuré
    - `POST /api/ocr/process` avec mauvaise clé → `401`
    - `POST /api/ocr/process` avec PDF valide + bonne clé → `202 Accepted` + jobId
    - `GET /api/ocr/jobs/{id}` avec id inconnu → `404`
    - `GET /health` → `200`
    - `POST /api/auth/setup` si déjà configuré → `409 Conflict`
    - Swagger endpoint `/swagger/v1/swagger.json` → `200` avec contenu JSON valide
  - Tests d'intégration pour `KazoOCR.Web` :
    - `GET /` sans auth → redirection vers `/login`
    - `GET /setup` si configuré → `404` ou redirection
    - `GET /upload` après login → `200`
  - Utiliser `WebApplicationFactory<Program>` avec mocks des services Core
- **Critères d'acceptation :**
  - [ ] Couverture > 80% sur KazoOCR.Api
  - [ ] Tous les scénarios d'authentification couverts
  - [ ] Tests passent sur Ubuntu et Windows (CI matrix)

#### Issue 5.8 — Documentation : docs/api.md + docs/web.md
- **Labels :** `iteration-5`, `documentation`
- **Estimation :** 30 min
- **Référence :** See [project.md §Itérations](../project.md#itérations) for iteration 5 scope summary.
- **Description :**
  - Créer `docs/api.md` :
    - Overview de l'API REST
    - Table des endpoints avec exemples curl
    - Configuration (env vars, ports, auth)
    - Lien vers Swagger UI
  - Créer `docs/web.md` :
    - Description de l'interface Blazor
    - Captures d'écran / wireframes ASCII
    - Configuration (ports, API URL)
    - Procédure first-run
  - Mettre à jour `docs/architecture.md` pour inclure KazoOCR.Api et KazoOCR.Web
  - Mettre à jour `docs/docker.md` pour documenter le nouveau docker-compose multi-service
  - Mettre à jour `README.md` pour ajouter le quickstart Web (`http://localhost:5001`)
- **Critères d'acceptation :**
  - [ ] `docs/api.md` complet avec exemples
  - [ ] `docs/web.md` complet avec procédure first-run
  - [ ] README mis à jour avec accès Web UI

---

## 🔄 Stratégie de Branches

```
main ← develop ← feature/issue-X.Y
```

- `main` : stable, protégée, releases uniquement
- `develop` : intégration des features
- `feature/issue-X.Y` : une branche par issue

---

## ✅ Prochaine Étape

Une fois ce plan validé, je créerai via le MCP GitHub :
1. Les **labels** sur le repo `Kazo-ca/OCR`
2. Les **16 issues** avec descriptions complètes, labels et assignation
3. Un **GitHub Project** (board Kanban) avec les colonnes : Backlog → In Progress → Review → Done
4. Liaison des issues au projet avec placement dans les bonnes colonnes/itérations
