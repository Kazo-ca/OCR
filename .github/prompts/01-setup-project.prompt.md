---
mode: agent
description: "Initialize a new repository from this template: customize project.md, plan.md, create GitHub labels, and configure branch protection."
tools:
  - mcp_io_github_git_issue_write
  - mcp_io_github_git_create_branch
  - mcp_io_github_git_list_branches
  - read_file
  - run_in_terminal
---

# Setup New Project from Template

You are setting up a brand-new GitHub repository that was created from the `CopilotProjectTemplate`.

## Step 1 — Gather project information

Ask the user for the following if not already provided:

1. **Project name** (e.g., `MyApp`)
2. **Short description** (1–2 sentences)
3. **Technology stack** (e.g., `.NET 10`, `Node.js 22`, `Python 3.12`)
4. **Solution structure** — how many projects/modules? List them.
5. **GitHub owner/repo** (e.g., `MyOrg/MyApp`)

## Step 2 — Customize `project.md`

Open `project.md` and replace all `[PLACEHOLDER]` values with the information gathered above. Keep all structural sections; only update the content.

The file must remain comprehensive enough that Copilot can reconstruct the full project context from it alone.

## Step 3 — Initialize `plan.md`

Open `plan.md` and:
1. Set the project name and vision
2. Add the first iteration title and goal
3. Leave issue sections empty — they will be filled via the `03-create-issues.prompt.md` prompt

## Step 4 — Configure git

In the terminal:
```bash
git config core.quotepath false
git config commit.gpgsign false
```

Ensure `.gitattributes` is committed first:
```bash
git add .gitattributes
git commit -m "chore: add gitattributes for consistent line endings"
```

## Step 5 — Create GitHub labels

Run the setup script:
```bash
bash .github/scripts/setup-github.sh
```

If the script is not executable on Windows, run:
```powershell
bash .github/scripts/setup-github.sh
# or
gh label create ...
```

The script creates all standard labels. Add custom labels for your domain.

## Step 6 — Create the `develop` branch

```bash
git checkout -b develop
git push -u origin develop
```

## Step 7 — Set branch protection (via GitHub UI or CLI)

Protect the `main` branch:
- Require PR before merging
- Require status checks: `pr-check / build`, `pr-check / test`
- Require the `Enforce Issue Reference` check to pass

## Step 8 — Commit initial setup

```bash
git add project.md plan.md
git commit -m "docs: initialize project.md and plan.md"
git push origin develop
```

## Done

The repository is ready. Next steps:
- Use `02-plan-iteration.prompt.md` to add iterations to `plan.md`
- Use `03-create-issues.prompt.md` to create GitHub issues from `plan.md`
- Use `04-start-issue.prompt.md` to begin work on an issue
