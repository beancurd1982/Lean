---
name: aegis-optimization-coordinator
description: Coordinate AegisGrowthAllocation optimization research for Candidate B, Candidate C, Candidate D/E proposals, and next-candidate decisions. Use when Codex is asked to optimize Candidate B, find a candidate better than Candidate C, compare Aegis candidates, decide the next Aegis research step, or route Aegis backtest/live-readiness work.
---

# Aegis Optimization Coordinator

Coordinate Aegis candidate research without changing trading code unless a hypothesis has been approved.

## Required Context Path

1. Read `project-notes/README.md`.
2. Read `project-notes/_indexes/backtest-index.md`.
3. Read `project-notes/_indexes/decision-index.md`.
4. Read `.agents/plugins/aegis-optimization-workflow/references/candidate-gates.md`.
5. Read `.agents/plugins/aegis-optimization-workflow/references/aegis-note-map.md`.
6. Open only linked detailed notes needed for the current decision.

Do not scan `project-notes/archive/` broadly.

## Routing

- Use `aegis-hypothesis-designer` before any candidate implementation or optimization run.
- Use `aegis-source-risk-auditor` when the next lever is unclear or could affect live-trading behavior.
- Use `aegis-candidate-implementer` only after an approved hypothesis exists.
- Use `aegis-backtest-evaluator` for new logs, result JSON, orders CSV, and Candidate B/C comparisons.
- Use `aegis-live-readiness-reviewer` before any paper/live promotion.

## Standing Rules

- Treat Candidate B as the stress baseline.
- Treat Candidate C as useful research evidence, not promotion-ready.
- Compare only same-execution runs unless the user explicitly accepts an execution-model change.
- Prefer source-level hypotheses and attribution over broad optimizer sweeps.
- Stop and ask before live-state, Object Store, order-handling, deployment identity, or default-behavior changes.

## Output

End with one of:

- next skill to use and why;
- a bounded hypothesis request;
- a comparison decision;
- a live-readiness decision;
- a blocker requiring user clarification.
