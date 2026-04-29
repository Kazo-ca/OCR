---
description: "Git workflow, branch naming, commit messages, and PR conventions."
applyTo: "**"
---

# Git Workflow Conventions

## Branch naming

```
feature/issue-X.Y-short-description
```

- `X` = iteration number, `Y` = issue number within the iteration
- `short-description` = 2–5 words, lowercase, hyphen-separated, ASCII only
- One branch per issue — never bundle multiple issues on one branch

Examples:
```
feature/issue-3.2-docker-worker-service
feature/issue-5.1-web-api-project
```

## Commit messages

Use Conventional Commits format:

```
type(scope): short description
```

Common types: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `ci`

**Rules:**
- Use only ASCII characters in commit messages (avoid Unicode dashes —, curly quotes "", accented letters)
- Keep the subject line ≤ 72 characters
- Use the imperative mood: "add feature" not "added feature"

Examples:
```
feat(api): add OCR process endpoint with job queue
fix(core): prevent double-processing of already-suffixed files
docs(readme): add web UI quickstart section
test(api): add integration tests for auth endpoints
ci: add enforce-issue-ref workflow
```

## Before pushing

```bash
git pull --rebase origin <branch>
git push origin feature/issue-X.Y-short-description
```

Always rebase (not merge) to keep a clean linear history. Never force-push to `main` or `develop`.

## Pull Requests

**Every PR must:**
1. Target `develop` (not `main` directly)
2. Have a title matching: `[X.Y] Short description matching the issue title`
3. Include in the body: `Closes #<issue-number>` — this is enforced by the `enforce-issue-ref` workflow
4. Pass the `pr-check` workflow (build + tests) before merging

**PR body template:**

```markdown
## Description
Brief description of the changes.

## Closes
Closes #<issue-number>

## Changes
- 

## Checklist
- [ ] Code follows project conventions
- [ ] Tests added/updated
- [ ] Documentation updated if needed
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes
```

## Labels

All PRs and issues must have the appropriate labels:
- `iteration-X` — which iteration this belongs to
- Domain label: `core`, `api`, `web`, `cli`, `docker`, `tests`, `documentation`, etc.

Labels are created by the setup script (`.github/scripts/setup-github.sh`).
