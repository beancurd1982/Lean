---
name: aegis-backtest-evaluator
description: Evaluate AegisGrowthAllocation backtests, logs, result JSON, and order CSV artifacts for Candidate B/C/D comparisons. Use when new Aegis backtest logs are added, QuantConnect result artifacts need comparison, log-index.csv must be updated, or a candidate must be accepted/rejected against stress and long-window gates.
---

# Aegis Backtest Evaluator

Evaluate backtest artifacts consistently and keep the log index auditable.

## Required Context

Read:

- `.agents/plugins/aegis-optimization-workflow/references/candidate-gates.md`
- `.agents/plugins/aegis-optimization-workflow/references/aegis-note-map.md`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv`

## Log Intake

When new Aegis logs are added, run the repo-local renamer:

```powershell
Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Invoke-BackTestLogRename.ps1
```

Keep `log-index.csv` as the source of truth:

- add new normalized rows;
- set `status=reviewed` after analysis;
- set `review_ref=inline:<short-summary>` for routine reviews;
- use a project note path for decision-grade analysis.

## Comparison Rules

- Compare only same-execution runs unless the user explicitly accepts an execution-model change.
- Check at least net profit, CAGR, drawdown, Sharpe, PSR, turnover, fees, order count, win rate, and profit factor.
- Use `scripts/extract_aegis_metrics.py` for normalized metrics when result JSON is available.
- Use `scripts/compare_aegis_backtests.py` for baseline/candidate comparisons.
- Use `scripts/check_log_index.py` before trusting `log-index.csv`.

## Documentation Threshold

Create a durable markdown note only when the result affects a decision, exposes risk, compares candidates, recommends an algorithm/configuration change, or supports promotion/rejection.
