---
mode: agent
description: "First-time setup: interview the user and fill all [PROJECT_NAME] placeholders in project.md and copilot-instructions.md."
tools:
  - read_file
  - replace_string_in_file
---

# Initialize project.md

You are filling in `project.md` for a brand-new repository created from `CopilotProjectTemplate`.

## Step 1 — Read the template

Read `project.md` in full so you know every section and placeholder that needs to be replaced.

Also read `.github/copilot-instructions.md`.

## Step 2 — Interview the user

Ask **all** of the following questions at once (do not ask one at a time):

1. **Project name** — e.g. `MyApp` (replaces every `[PROJECT_NAME]`)
2. **GitHub owner/repo** — e.g. `myuser/myapp`
3. **Short description** — 1–2 sentences: what does it do and for whom?
4. **Technology stack** — e.g. `.NET 10`, `Node.js 22`, `Python 3.12`
5. **Target framework / runtime** — e.g. `net10.0`, `node22`, `python3.12`
6. **Components / modules** — list each project or module with a one-line purpose
   (e.g. `MyApp.Core` — business logic, `MyApp.CLI` — console entry point)
7. **External packages** already known — NuGet / npm / pip (name + purpose)
8. **Execution modes** — which apply: CLI, Docker Watch, Web API, Web UI, Desktop, Worker?
9. **Deployment** — how is this project released or distributed?

## Step 3 — Fill in project.md

Using the answers, replace every `[PROJECT_NAME]`, `[PLACEHOLDER]`, and similar tokens in
`project.md`. Apply these rules:

- Replace every occurrence, including inside headings, code blocks, tables, and comments.
- Keep all sections that apply to this project; delete sections marked "*(planned/optional)*"
  that do not apply.
- Update the Component Details section: keep only the components listed by the user; adapt
  the description, key files, and dependencies for each.
- Update the NuGet / npm / pip packages table with the packages provided.
- Update the Execution Modes table to show only the modes that apply.
- Update the Configuration Reference table with the real settings (remove rows that don't exist).
- Update the Cross-Platform Strategy table to match the actual components.
- Remove the `PLACEHOLDER CHECKLIST` section at the very bottom of the file.

After editing, the file must be a complete and accurate description of the project with no
remaining placeholder tokens.

## Step 4 — Update copilot-instructions.md

Open `.github/copilot-instructions.md` and replace every `[PROJECT_NAME]` occurrence with
the project name collected above.

## Step 5 — Confirm

Show the user a one-paragraph summary of the key values that were replaced, then tell them
the next step is to run `00-init-plan-md` to generate `plan.md` from this file.
