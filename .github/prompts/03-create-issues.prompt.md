---
mode: agent
description: "Create GitHub issues for a specific iteration from plan.md. Reads plan.md, creates all issues via MCP with proper labels and plan.md references."
tools:
  - read_file
  - mcp_io_github_git_issue_write
  - mcp_io_github_git_get_label
---

# Create GitHub Issues from plan.md

You are creating GitHub issues for **iteration $ITERATION_NUMBER** (specify in your message, e.g., "Create issues for iteration 5").

## Step 1 — Read and validate plan.md

Read `plan.md` in full. Locate the section for the requested iteration.

If the iteration section is missing or incomplete, stop and ask the user to run `02-plan-iteration.prompt.md` first.

## Step 2 — Read project.md for context

Read `project.md` to understand the component architecture and ensure issue descriptions reference the correct components.

## Step 2.5 — Validate dependency safety before creating issues

Before creating any issue, inspect the requested iteration for hard dependencies.

If an issue depends on another issue from the same iteration, or depends on a component that will only exist after another issue in the same iteration merges, stop and tell the user to re-plan the work into separate iterations with `02-plan-iteration.prompt.md`.

Do not create a batch of issues that is expected to run in parallel when some of those issues are blocked on unmerged work from the same iteration.

## Step 3 — Check required labels exist

Before creating issues, verify that the following labels exist on GitHub:
- `iteration-X` (where X is the iteration number)
- All domain labels referenced in the iteration (e.g., `api`, `web`, `docker`, `tests`, `documentation`)

If any label is missing, create it using `mcp_io_github_git_issue_write` with the label information from the `Labels` table in `plan.md`.

## Step 4 — Create issues

For each issue in the iteration, create a GitHub issue with:

**Title format:** `[X.Y] Description matching plan.md`

**Body format:**
```markdown
## Context

> See [plan.md §Issue X.Y](../blob/main/plan.md#issue-xy---title) for full specification.

Part of **Iteration X — [Iteration Title]** (see [`plan.md`](../blob/main/plan.md)).

**Depends on:** #XX (if applicable — omit if no dependencies)

---

## Description

[Copy the full description from plan.md, including sub-tasks as a bullet list]

[Include any relevant code snippets or configuration tables from plan.md]

## Acceptance Criteria

[Copy the acceptance criteria from plan.md as an unchecked checklist]
- [ ] Criterion 1
- [ ] Criterion 2
```

**Labels:** Apply all labels listed in the issue's `Labels` field in plan.md.

## Step 5 — Output summary

After all issues are created, output a summary table:

| Issue # | Title | Labels | URL |
|---------|-------|--------|-----|
| #XX | [X.Y] Title | `iteration-X`, `domain` | link |

## Rules

- Issue bodies MUST reference `plan.md` — this is how Copilot reconstructs context in future sessions
- Dependency references (`Depends on: #XX`) must use the actual GitHub issue numbers (not plan.md numbers)
- Create issues in dependency order (dependencies first)
- Do not create issues for an iteration that still contains hard same-iteration dependencies; stop and ask the user to split the blocked work into a later iteration
- Issues created from the same iteration must all be startable from the current `main`/`develop` state without waiting for another new PR from that iteration to merge
- Never create issues for iterations that are not yet in `plan.md`
