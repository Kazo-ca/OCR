# Iteration Promotion Workflow

## Purpose

The `iteration-promote` workflow automates the transition between iterations.  
When all issues in the current iteration are closed (via merged PRs), it applies the `Ready` label to every open, non-`OnHold` issue in the next iteration.

This makes it immediately visible in GitHub's issue views and project boards which issues are now actionable, without any manual triage.

---

## Labels

| Label | Color | Meaning |
|---|---|---|
| `Ready` | green `#0E8A16` | Issue is in the active iteration — ready to be worked on |
| `OnHold` | light green `#C2E0C6` | Excluded from promotion — intentionally deferred |
| `iteration-N` | varies | Groups issues by iteration number |

---

## Triggers

The workflow has **three triggers**, each catching a different timing window:

| Trigger | Why |
|---|---|
| `pull_request: [closed]` | Fires when a PR merges. Earliest possible signal, before linked issues close. |
| `issues: [closed]` | Fires when GitHub closes the linked issue (may lag behind the PR event). |
| `workflow_dispatch` | Manual re-run — safety net after corrections or label fixes. |

For `pull_request` events, the job only runs when `merged == true` (not when a PR is simply abandoned/closed without merging).

---

## Logic

The workflow is **stateless** — it reads the current label state at runtime rather than relying on event payload data. This makes it safe to re-run at any time.

```
1. Are there any open issues with the `Ready` label (excluding `OnHold`)?
   └─ YES → current iteration still active. Stop.
   └─ NO  → continue.

2. Scan all open issues for `iteration-N` labels (excluding `OnHold`).
   Find the lowest N that has at least one open issue.

3. Ensure the `Ready` label exists in the repo (create it if missing).

4. Apply `Ready` to every open, non-`OnHold` issue in that iteration.
```

---

## Why Stateless?

The original implementation relied on `context.payload.issue` (the event payload) to determine which iteration the closed issue belonged to. This caused problems:

- Re-opening and re-closing an issue would not reliably re-trigger the right logic.
- The `issues: [closed]` event could fire before the PR merge event updated labels.
- The payload approach required the closed issue itself to carry an `iteration-N` label — if it was missing, the whole run silently skipped.

The stateless approach has none of these problems: every run asks "what is the current state?" and acts accordingly.

---

## Why Three Triggers Instead of One?

A single `issues: [closed]` trigger misses the case where a PR merges but the linked issue closes slightly later (GitHub processes them asynchronously). A single `pull_request: [closed]` trigger misses direct issue closures. Using both — plus `workflow_dispatch` — ensures coverage regardless of the order events arrive.

Because the logic is idempotent (applying `Ready` to issues that already have it is a no-op), running twice has no side effects.

---

## Original Implementation vs. Current

| | Before | After |
|---|---|---|
| **Triggers** | `issues: [closed]` only | `pull_request`, `issues`, `workflow_dispatch` |
| **Auth** | `PROJECT_TOKEN` secret (fine-grained PAT) | built-in `GITHUB_TOKEN` |
| **API** | GitHub Projects GraphQL API | REST API only |
| **Promotion signal** | Move item status Backlog → Todo in the Project board | Apply `Ready` label to the issue |
| **OnHold exclusion** | No | Yes |
| **Pagination** | `per_page: 100` (hard cap) | `github.paginate()` (all pages) |
| **Stateless** | No — payload-dependent | Yes |
| **Lines** | 227 | ~120 |

---

## Pagination

All `listForRepo` calls use `github.paginate()`, which automatically fetches every page and returns a flat array. This ensures correctness for repositories with more than 100 issues.

---

## Permissions

```yaml
permissions:
  issues: write
```

Only `issues: write` is required. No secrets beyond `GITHUB_TOKEN` are needed.

---

## File

`.github/workflows/iteration-promote.yml`
