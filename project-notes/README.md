# Project Notes

This folder is the durable knowledge base for project history, decisions, reviews, and reusable workflows. It is not a running diary.

## Fast Retrieval Path

Read in this order to minimize token usage:

1. Start with this file.
2. Open one relevant index in `_indexes/`.
3. Open a linked detailed note only when the index summary is not enough.
4. Avoid `archive/` unless an index points there or the task is historical investigation.

## Indexes

- [Decision Index](_indexes/decision-index.md) - durable trading, deployment, persistence, parameter, and algorithm decisions.
- [Backtest Index](_indexes/backtest-index.md) - decision-relevant backtest and optimization records.
- [Workflow Index](_indexes/workflow-index.md) - reusable workflows, troubleshooting, and automation notes.
- [Archive Index](_indexes/archive-index.md) - compact list of preserved legacy notes.

## Active Runbooks

- [Project Notes Maintenance](runbooks/project-notes-maintenance.md)

## Note Creation Policy

Create markdown notes only for decision-grade, safety-critical, reusable workflow, multi-agent debate, or user-requested records.

Do not create markdown notes for routine exploration, small edits, formatting, minor tooling work, or ordinary low-risk reviews. Use the final response for those.

Routine Aegis backtest/log reviews belong in `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv` with `review_ref=inline:<short-summary>` unless the analysis affects a decision, exposes risk, compares candidates, or recommends an algorithm/configuration change.

## Durable Note Shape

New durable notes should start with a short summary and metadata so agents can decide whether to keep reading without loading the full file.

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

## Decision Or Finding

## Evidence

## Verification

## Risks And Open Questions
```