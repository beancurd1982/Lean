---
name: aegis-live-readiness-reviewer
description: Review AegisGrowthAllocation paper-live or live-trading readiness before promoting a candidate. Use for Candidate B/C/D paper-test tags, live deployment checks, Object Store state checks, restart safety, deployment identity, holdings/equity consistency, open orders, and live-state schema compatibility.
---

# Aegis Live Readiness Reviewer

Block unsafe paper/live promotion until validation and operational evidence are complete.

## Required Context

Read:

- `.agents/plugins/aegis-optimization-workflow/references/live-safety-checklist.md`
- `.agents/plugins/aegis-optimization-workflow/references/candidate-gates.md`
- relevant decision/backtest notes through the coordinator context path.

## Review Checks

Verify:

- deployment identity and source revision;
- parameter defaults and supplied cloud parameters;
- current holdings and equity consistency;
- open orders and recent order events;
- Object Store live-state key and schema version;
- restart/recovery behavior;
- paper-live logs;
- same-execution stress and long-window validation;
- whether unresolved risks require small-capital validation only.

## Decision

Return exactly one status:

- `blocked`;
- `paper-test only`;
- `small-capital validation only`;
- `ready for promotion`.

Include the evidence and unresolved risks. Do not approve promotion if stress validation or live-state safety evidence is incomplete.
