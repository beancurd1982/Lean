# Project Notes Maintenance

## Purpose

Keep `project-notes/` useful as a low-token knowledge base. The goal is fast retrieval of durable decisions and reusable workflows, not a transcript of every session.

## Retrieval Order

1. Read `project-notes/README.md`.
2. Read exactly one relevant file in `project-notes/_indexes/` when possible.
3. Open detailed notes only through index links or when the user asks for historical investigation.
4. Avoid scanning `project-notes/archive/` directly unless rebuilding an index.

## Durable Note Threshold

Create a markdown note only when at least one of these is true:

- The work affects live trading behavior, persistence/Object Store, order handling, deployment safety, parameter selection, or algorithm decisions derived from backtests.
- The note records a reusable workflow lesson likely to prevent repeated future failures.
- The work contains multi-agent debate or independent review that should remain auditable.
- The user explicitly asks for a durable record.

Do not create markdown notes for routine exploration, small edits, formatting, minor tooling work, or ordinary low-risk reviews.

## Backtest And Log Reviews

Use `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv` as the source of truth for routine Aegis log reviews.

- Use `review_ref=inline:<short-summary>` for routine reviewed logs.
- Use `review_ref=<project-note-path>` only when the analysis is decision-grade.
- Create markdown only when the analysis affects a decision, exposes risk, compares candidates, or recommends an algorithm/configuration change.

## New Note Template

```markdown
---
id: AEGIS-DEC-0000
type: decision
status: accepted
date: YYYY-MM-DD
topic: AegisGrowthAllocation
tags: [aegis, safety]
related: []
files: []
---

# Short Title

## Agent Summary
One short paragraph explaining why this note matters and when to read it.

## Summary

## Files Touched

## Decision Or Finding

## Evidence

## Verification

## Risks And Open Questions
```

## Index Maintenance

When adding a durable note:

1. Put it in the narrowest useful folder, or in `archive/YYYY-MM/` if it is historical/imported material.
2. Add a short entry to the relevant `_indexes/*.md` file.
3. Keep the index entry compact: date, topic, impact/use case, link.
4. Prefer stable IDs in note frontmatter for future cross-reference.

## Token-Saving Rules

- Put an `Agent Summary` near the top of durable notes.
- Keep indexes short enough to read in one pass.
- Link to details instead of copying large excerpts.
- Preserve exact file paths, parameter values, error messages, and decisions when they are needed to avoid rediscovery.