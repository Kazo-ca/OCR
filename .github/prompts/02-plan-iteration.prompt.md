---
mode: agent
description: "Plan a new iteration: add it to plan.md with goals, issues, and acceptance criteria."
tools:
  - read_file
  - replace_string_in_file
---

# Plan a New Iteration

You are adding a new iteration to `plan.md`. The iteration must be fully specified before any issues are created.

## Step 1 — Read current plan

Read `plan.md` to understand:
- What iterations already exist
- What the highest iteration number is
- What the overall project vision is

Also read `project.md` to understand the current architecture and what components exist.

## Step 2 — Gather iteration details

Ask the user for:

1. **Iteration number** (next after the current highest)
2. **Iteration title** (e.g., "Web API & Web UI")
3. **Goal statement** — what will be true when this iteration is done?
4. **List of features/tasks** — what needs to be built? (free-form, will be structured into issues)
5. **Hard dependencies** — which tasks require code or behavior that will only exist after another new issue is merged?

## Step 3 — Structure the issues

For each feature/task, create an issue entry following this format:

```markdown
#### Issue X.Y — [Title]
- **Labels:** `iteration-X`, `[domain]`
- **Estimation:** [N] min
- **Description:**
  - [Detailed description of what needs to be implemented]
  - [List of sub-tasks]
- **Acceptance Criteria:**
  - [ ] [Testable criterion 1]
  - [ ] [Testable criterion 2]
```

Rules for structuring:
- Each issue must be independently implementable (no hidden coupling)
- Each issue must have at least 2 acceptance criteria
- Issues that depend on each other must note the dependency explicitly
- Hard dependencies on code that is not already in `main` or `develop` must not stay in the same iteration
- If issue X.Y requires a new module, API, UI, or behavior introduced by issue X.Z, move the dependent issue to the next iteration instead of planning both in parallel
- Only keep issues in the same iteration when they can be implemented safely from the current branch state without waiting for another new PR to merge
- When in doubt, split producer work and consumer work into separate iterations
- Estimation in multiples of 30 min

## Step 4 — Update plan.md

Add the new iteration section to `plan.md`:

1. Add the iteration to the `## Iterations` summary section:
   ```
   X. **[Iteration Title]**: [one-line summary of what will be built]
   ```

2. Add the full iteration section with all issues to the `## Product Backlog` section.

3. Update the `## Issue Summary Table` at the bottom with the new issue rows.

4. Update the `Labels` table if new domain labels are needed.

5. Validate the iteration before finishing:
  - No issue in the iteration is blocked on another new issue from the same iteration
  - Any blocked follow-up work is moved to the next iteration with an explicit dependency note

## Step 5 — Update project.md

If new components or modules are being added in this iteration:
1. Add them to the architecture diagram in `project.md`
2. Add them to the dependencies section
3. Add a component description section
4. Update the NuGet packages table if new packages are being added
5. Mark new items with `[iteration-X]` so they are clearly identified as future work

## Step 6 — Commit the plan

```bash
git add plan.md project.md
git commit -m "docs: add iteration X plan — [iteration title]"
git push origin develop
```

## Done

The iteration is now planned. Use `03-create-issues.prompt.md` to create the GitHub issues for this iteration.
