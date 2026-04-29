---
description: "General coding conventions for this project. Applied to all source files."
applyTo: "**/*.{cs,fs,vb,ts,js,py}"
---

# Coding Conventions

## Path handling

Prefer `Path.Join()` over `Path.Combine()`. `Path.Combine()` silently drops earlier arguments when any segment is rooted — this is a frequent source of path-traversal bugs.

```csharp
// CORRECT
var output = Path.Join(baseDir, "processed", "file.pdf");

// WRONG — if fileName is absolute, baseDir is silently discarded
var output = Path.Combine(baseDir, fileName);
```

When combining paths with user-supplied or external input, always sanitize:

```csharp
var safeFileName = Path.GetFileName(userInput); // strip path components
var fullPath = Path.Join(baseDir, safeFileName);
```

## IDisposable pattern

Always dispose `IDisposable` objects using `using` or `using var`. Never rely on the GC for deterministic cleanup.

```csharp
// CORRECT
using var cts = new CancellationTokenSource(timeout);
using var stream = File.OpenRead(path);

// WRONG
var cts = new CancellationTokenSource(timeout); // leaked
```

## Async methods

- Every async method must accept a `CancellationToken ct` parameter.
- Propagate the token to all internal awaits.
- Never use `.Wait()` or `.Result` on a `Task` — deadlock risk.

```csharp
public async Task<Result> ProcessAsync(string input, CancellationToken ct)
{
    await SomeOperationAsync(input, ct);
}
```

## Logging

- Use `ILogger<T>` — never `Console.WriteLine` in library/service projects.
- Use structured logging (message templates with named placeholders):

```csharp
_logger.LogInformation("Processing file {FileName} with suffix {Suffix}", fileName, suffix);
// NOT: _logger.LogInformation($"Processing {fileName}");
```

- Log at appropriate levels: `Debug` for trace details, `Information` for business events, `Warning` for recoverable issues, `Error` for failures.

## Nullable reference types

- `Nullable=enable` is enforced project-wide.
- Do not use the `!` null-forgiving operator without an explanatory comment.
- Prefer `is not null` / `is null` guard clauses over `?.` chains in critical paths.

```csharp
if (result is null)
    throw new InvalidOperationException("Expected a non-null result.");
```

## Dependency Injection

- Use interfaces for all services (`IMyService` / `MyService`).
- Register dependencies in DI — no static service locators (`ServiceLocator`, `IServiceProvider` resolved manually).
- Constructor injection only. No property injection.

## Error handling

- Validate at system boundaries (user input, file I/O, external process output).
- Do not add error handling for scenarios that cannot happen (no defensive `try/catch` around pure code).
- Return typed result objects (`Result<T>`, `ValidationResult`) instead of throwing for expected failures.

## Code analysis

- All analyzer warnings are treated as errors (`TreatWarningsAsErrors=true`).
- Fix all warnings before opening a PR.
- Configure suppressions in `.editorconfig`, never with `#pragma warning disable`.
