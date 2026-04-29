---
mode: agent
description: "Create a GitHub Pull Request for the current branch. Enforces issue reference, correct title format, and complete checklist."
tools:
  - read_file
  - mcp_io_github_git_issue_read
  - mcp_io_github_git_create_pull_request
  - mcp_io_github_git_list_branches
  - run_in_terminal
---

# Create Pull Request

You are creating a Pull Request for the current branch. The PR will be blocked from merging by the `enforce-issue-ref` workflow if `Closes #<number>` is missing.

## Step 1 — Identify current branch and linked issue

```bash
git branch --show-current
```

Parse the branch name `feature/issue-X.Y-short-description` to extract:
- Iteration number: `X`
- Issue number: `Y`
- Full GitHub issue number: the `#` number from the issue title

If the branch does not follow the naming convention, stop and inform the user.

## Step 2 — Read the linked issue

Fetch the full issue (title, body, labels) via GitHub MCP.

## Step 3 — Verify all acceptance criteria are met

Read the issue's acceptance criteria. For each criterion, confirm:
- [ ] The change implements it
- [ ] A test covers it (if testable)

If any criterion is not met, inform the user and do not create the PR.

## Step 4 — Check branch is up to date

```bash
git fetch origin develop
git log --oneline HEAD..origin/develop
```

If `develop` has commits not in the current branch, rebase first:
```bash
git pull --rebase origin develop
git push --force-with-lease origin feature/issue-X.Y-short-description
```

## Step 5 — Build and test

```bash
dotnet build
dotnet test
```

Only proceed if both commands succeed.

## Step 6 — Create the PR

Use the GitHub MCP to create the PR with:

**Title:** `[X.Y] [Issue title — copied exactly from GitHub issue]`

**Base branch:** `develop`
**Head branch:** `feature/issue-X.Y-short-description`

**Body:**
```markdown
## Description
[2–4 sentence summary of what changed and why]

## Closes
Closes #<issue-number>

## Changes
- [Bullet list of the main changes made]

## Testing
- [How the changes were tested]
- [New tests added: filename/test name]

## Checklist
- [ ] Code follows project conventions (see `.github/instructions/`)
- [ ] All acceptance criteria from issue #<number> are met
- [ ] Tests added/updated and passing
- [ ] Documentation updated if applicable
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes
- [ ] No warnings suppressed with `#pragma warning disable`
```

**Labels:** Same labels as the linked issue.

## Step 7 — Confirm PR URL

Output the PR URL and number to the user.

## Rules

- The `Closes #<number>` line is **mandatory** — the `enforce-issue-ref` workflow will fail without it
- The PR title must match the issue title format exactly: `[X.Y] Description`
- Target `develop`, never `main` directly
- Do not create a PR if `dotnet build` or `dotnet test` fails
