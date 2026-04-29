---
description: "Use when creating, renaming, or suggesting file names. Enforces ASCII-only naming."
applyTo: "**"
---

# File Naming: No Accents, No Non-ASCII Characters

## Rules

- **Never** use accented or non-ASCII characters in file or directory names.
- **Never** use non-English words in file names. Always use English equivalents.
- Use only ASCII letters (`a-z`, `A-Z`), digits (`0-9`), hyphens (`-`), underscores (`_`), and dots (`.`).
- These rules apply to file names **only** — file *content* may contain any Unicode as needed.

## Why

Git and many tools (CI runners, Docker, Windows, cross-platform shells) handle non-ASCII file paths inconsistently. Accented characters in file names cause:
- Encoding errors in git output and terminal commands
- Broken CI pipelines on Linux runners
- Unreadable diffs and logs

## Examples

| Wrong | Correct |
|-------|---------|
| `données.csv` | `data.csv` |
| `résumé-modèle.py` | `summary-model.py` |
| `contrôleur.ts` | `controller.ts` |
| `paramètres/` | `settings/` |
| `schéma-base.sql` | `schema-base.sql` |
| `MéthodesPaiement.cs` | `PaymentMethods.cs` |

## Enforcement

If asked to create or rename a file, silently apply the correct ASCII name.  
Do not ask for confirmation unless the correction is ambiguous.
