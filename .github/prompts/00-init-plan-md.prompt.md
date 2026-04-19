---
mode: agent
description: "Generate the initial plan.md from the completed project.md. Also patches the setup scripts with project-specific labels and invites the user to run them."
tools:
  - read_file
  - replace_string_in_file
---

# Initialize plan.md from project.md

You are generating the initial content of `plan.md` based on the already-completed `project.md`.

> **Pre-condition:** `project.md` must already be filled in (no remaining `[PLACEHOLDER]` tokens).
> If it still contains placeholders, stop and ask the user to run `00-init-project-md` first.

## Step 1 — Read both files

Read `project.md` in full. Extract:

- Project name
- Vision / description (1–2 sentences)
- Solution structure (components with their purposes)
- Technology stack and target framework
- Key coding conventions
- GitHub owner/repo

Read `plan.md` in full to understand its current structure.

## Step 2 — Ask one question

Ask only this: **"What is the goal of Iteration 1?"**
(1–2 sentences describing what will be true when iteration 1 is complete — e.g. "The Core
library and CLI are functional and tested on Windows and Linux.")

Also ask: **"What domain labels do you want for Iteration 1?"**
(comma-separated, e.g. `core, cli, tests` — these will be added to the labels table)

Wait for the user's answer before continuing.

## Step 3 — Fill in plan.md

Replace the placeholder content in `plan.md` using the information extracted from
`project.md` and the user's answers. Follow the existing structure of `plan.md` exactly.

### Vision section
Copy the project description from `project.md`. Keep it to 2–4 sentences.

### Architecture section
Copy the solution structure diagram from `project.md` verbatim. Add `[planned]` next to
components that are not yet implemented.

### Conventions section
Extract the 4–6 most important conventions from `project.md` (branching, formatting, path
handling, testing, file naming). Write them as concise bullet points.

### Labels table
Start with the standard labels already in the template, then add:
- `iteration-1` with an appropriate color
- One label per domain provided by the user (e.g. `core`, `cli`, `tests`)

### Iteration 1 section
- Use the goal statement provided by the user.
- Leave issue entries **empty** — add one placeholder entry with the comment:
  ```
  <!-- Run 02-plan-iteration to add issues for this iteration -->
  ```
- Leave the Issue Summary Table with a single placeholder row.

### Changelog
```
| — | 1 — [Iteration Title] | Planned |
```

## Step 4 — Patch the setup scripts

Both scripts contain a "domain labels" section that must reflect the actual labels for this
project. Update **both files** now:

- `.github/scripts/setup-github.sh`
- `.github/scripts/setup-github.ps1`

In each file, locate the block between the two comments:
```
# --- iteration labels ---...
# --- domain labels ---...
```

Replace the domain label lines (the block between `# --- domain labels ---` and the next
blank line / section comment) with one `create_label` / `New-Label` call per label that
was added to the Labels table in `plan.md`.

Rules:
- Keep the iteration label lines as-is.
- Keep the standard labels that are still relevant (`documentation`, `bug`, `ci-cd`).
- Add every domain label the user provided (e.g. `core`, `cli`, `api`, `tests`).
- Remove any default domain labels that do not apply to this project.
- Color codes: use the same hex values as in the `plan.md` Labels table (without `#`).

## Step 5 — Confirm and invite to run

Tell the user:

1. `plan.md` is initialized.
2. Both setup scripts have been updated with the project-specific labels.
3. Ask which OS they are on, then show the exact command to run:

**Linux / macOS:**
```bash
bash .github/scripts/setup-github.sh
```

**Windows (PowerShell):**
```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\.github\scripts\setup-github.ps1
```

4. After running the script, the next step is `02-plan-iteration` to add issues to `plan.md`.
