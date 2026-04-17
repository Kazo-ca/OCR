# Copilot Instructions — KazoOCR

## Context

KazoOCR is a .NET 10 multi-platform solution that automates PDF "Sandwich" creation (selectable text) by driving **OCRmyPDF**. See [`project.md`](../project.md) for full architecture and conventions.

## Solution Structure

```
src/KazoOCR.Core/       # Business logic (no external deps)
src/KazoOCR.CLI/        # Console app + Watch mode (CommandDotNet)
src/KazoOCR.Docker/     # Worker Service — Watch mode in container
src/KazoOCR.UI/         # MAUI Desktop (Windows only, conditional build)
tests/KazoOCR.Tests/    # xUnit tests
```

## Coding Conventions

- Target framework: `net10.0` (MAUI: `net10.0-windows10.0.19041.0`)
- `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`
- Use interfaces for all services (`IOcrFileService`, `IOcrProcessRunner`, etc.)
- Dependency injection throughout — no static service locators
- `CancellationToken` on all async methods
- Structured logging via `ILogger<T>` — no `Console.WriteLine` in Core

## Branching Model

- `main` — protected, releases only
- `develop` — integration branch
- `feature/issue-X.Y-short-description` — one branch per issue

## Pull Requests

- **Always include `Closes #<issue-number>`** in the PR description to link the issue
- The issue number comes from the GitHub issue that triggered the work
- Title format: `[X.Y] Short description matching the issue title`
- Keep PRs focused on one issue — no bundling multiple issues in one PR

## Testing

- Target >80% coverage on `KazoOCR.Core`
- Use `Moq` for mocks and `FluentAssertions` for assertions
- Tests must pass on both Ubuntu and Windows

## File Naming

- No accented or non-ASCII characters in file or directory names
- No French words in file names — use English equivalents
- ASCII only: `a-z`, `A-Z`, `0-9`, `-`, `_`, `.`
