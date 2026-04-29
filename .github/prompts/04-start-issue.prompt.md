---
mode: agent
description: "Start working on a specific GitHub issue: read the issue, read plan.md, create the branch, and summarize the work to be done."
tools:
  - read_file
  - mcp_io_github_git_issue_read
  - mcp_io_github_git_create_branch
  - mcp_io_github_git_list_branches
  - run_in_terminal
---

# Start Work on an Issue

You are starting work on **issue #$ISSUE_NUMBER** (specify in your message, e.g., "Start work on issue #12").

## Step 1 — Read the issue

Fetch the full issue content (title, body, labels, comments) using the GitHub MCP.

## Step 2 — Read plan.md for the full specification

The issue body contains a link to `plan.md`. Read `plan.md` and locate the corresponding section identified in the issue's "Context" block.

Also read `project.md` to understand the current architecture and where the new code fits.

## Step 2.5 — Check dependency readiness

Inspect the issue body and the `plan.md` entry for dependency markers such as `Depends on: #XX`.

If an upstream dependency is still open, unmerged, or not yet available in `main`/`develop`, stop here and tell the user the issue is blocked.

Do not create or switch to a feature branch for blocked work unless the user explicitly asks for a non-coding preparation step.

## Step 3 — Parse the issue number for branch naming

- Extract the iteration number `X` and issue number `Y` from the issue title format `[X.Y] ...`
- Extract a short description (2–5 words, lowercase, hyphen-separated, ASCII only)

Branch name: `feature/issue-X.Y-short-description`

## Step 4 — Create the branch

Check if the branch already exists:
```bash
git branch -a | grep "feature/issue-X.Y"
```

If it does not exist, create it from `develop`:
```bash
git checkout develop
git pull --rebase origin develop
git checkout -b feature/issue-X.Y-short-description
git push -u origin feature/issue-X.Y-short-description
```

If it already exists, switch to it:
```bash
git checkout feature/issue-X.Y-short-description
git pull --rebase origin feature/issue-X.Y-short-description
```

## Step 5 — Summarize the work

Output a clear summary to the user:

```
Branch: feature/issue-X.Y-short-description
Issue: #XX — [Title]
Iteration: X — [Iteration Name]

== Work to do ==
[Bullet list of sub-tasks from the issue description, in implementation order]

== Acceptance criteria ==
[Checklist from the issue]

== Files likely to be modified ==
[Based on plan.md and project.md, list the files/projects that will be touched]

== Dependencies ==
[List any upstream issues and their status]
```

## Step 6 — Confirm before coding

Ask the user to confirm before starting any code changes:

"I've read issue #XX and plan.md. Here is the work plan [summary above]. Should I start implementing?"

Only begin implementation after explicit confirmation.

## Rules

- Never start work directly on `main` or `develop`
- The branch must follow the naming convention exactly
- Always sync with `develop` before creating the branch (pull --rebase)
- If upstream dependencies are not yet merged, treat the issue as blocked and do not start implementation in parallel
