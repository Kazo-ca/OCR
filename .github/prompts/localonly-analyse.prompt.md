---
description: "Create a local-only analysis document (.localonly.md) about a specific aspect of the code, project, or solution — code review, problem investigation, or feature analysis."
name: "localonly-analyse"
argument-hint: "Describe what to analyse (e.g. 'OcrProcessRunner error handling', 'feature: add retry logic', 'review WatcherService')"
agent: "agent"
---

Create a local-only analysis document for the topic described by the user.

## Output File

Save the document as `{argument}.localonly.md` in the root of the repository (or in `docs/` if it fits better — use judgement based on the topic).
Use a short, lowercase, hyphenated ASCII name derived from the topic (e.g. `ocr-process-runner-review.localonly.md`).

## Document Structure

The document must contain:

```
# <Title>

**Date:** <today's date>
**Scope:** <affected files / components>
**Type:** <Code Review | Problem Analysis | Feature Analysis>

## Summary

Brief overview of what was analysed and the key finding or recommendation.

## Context

What triggered this analysis. Which part of the codebase is involved and why it matters.

## Analysis

Detailed findings. For a code review: observations on correctness, design, security, and test coverage.
For a problem: root cause, impact, reproduction steps.
For a feature: requirements, design options, trade-offs.

## Recommendations / Proposed Approach

Concrete, actionable next steps. Reference specific files and symbols using markdown links.

## Open Questions

Anything that needs further investigation or stakeholder input.
```

## Guidelines

- Follow the project conventions from [copilot-instructions.md](../../.github/copilot-instructions.md).
- Reference source files as relative markdown links.
- Keep the language concise and technical.
- Do **not** commit the file — it ends in `.localonly.md` and is git-ignored.
