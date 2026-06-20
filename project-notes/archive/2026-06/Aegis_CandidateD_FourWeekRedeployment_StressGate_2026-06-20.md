---
id: AEGIS-BT-2026-06-20-CANDIDATE-D-STRESS
type: backtest-analysis
status: accepted
date: 2026-06-20
topic: AegisGrowthAllocation
tags: [aegis, candidate-d, stress-gate, post-stress-redeployment]
related:
  - project-notes/archive/2026-06/Aegis_CandidateD_PostStressRedeployment_Hypothesis_2026-06-20.md
  - project-notes/archive/2026-06/Aegis_CandidateB_Return_Gap_Attribution_Analysis_2026-06-20.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Stress04_2021_2022.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Stress04_2021_2022_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv
---

# Candidate D Four-Week Redeployment Stress Gate

## Agent Summary

Candidate D passed the first stress-preservation gate. With `preweak-rec-confirm-weeks=4`, the 2021-2022 stress run matched Candidate B exactly on measured metrics and produced zero active recovery weeks, which confirms the stricter confirmation gate suppressed Candidate C's single stress-window activation.

## Decision Or Finding

Candidate D may advance to the next validation gate: segmented long `2016-2017`.

This does not approve implementation, default changes, paper trading, or live deployment.

## Parameters

```text
backtest-start=2021-01-01
backtest-end=2022-12-31
preweak-recovery-enabled=true
preweak-rec-confirm-weeks=4
preweak-rec-growth-target=0.16
preweak-rec-def-target=0.30
preweak-rec-dd-improve=0.02
preweak-rec-max-dd=0.08
crisis-diagnostics=true
```

## Stress Metrics

| Metric | Candidate B Baseline | Candidate D | Gate |
|---|---:|---:|---|
| Net profit | 17.613% | 17.613% | PASS |
| CAGR | 8.465% | 8.465% | PASS |
| Drawdown | 12.9% | 12.9% | PASS |
| Sharpe | 0.609 | 0.609 | PASS |
| PSR | 31.356% | 31.356% | PASS |
| Orders | 299 | 299 | PASS |
| Fees | $299.60 | $299.60 | PASS |
| Turnover | 3.03% | 3.03% | PASS |
| Win rate | 60% | 60% | PASS |
| Profit factor | 1.815 | 1.815 | PASS |

## Diagnostic Evidence

Candidate D normalized log:

- `2026-06-20_223822__AegisGrowthAllocation__ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Stress04_2021_2022_logs.txt`

Diagnostic row check:

- weekly rows: `104`;
- first row: `2021-01-04`;
- last row: `2022-12-27`;
- active recovery weeks: `0`;
- max recovery confirmation count: `2`;
- reason counts: `base-regime=80`, `drawdown-signals=24`.

This matches the hypothesis mechanism: the four-week gate blocks Candidate C's brief stress-window recovery activation.

## Next Gate

Run Candidate D segmented long `2016-2017` with the same parameter set except dates:

```text
backtest-start=2016-01-01
backtest-end=2017-12-31
preweak-recovery-enabled=true
preweak-rec-confirm-weeks=4
preweak-rec-growth-target=0.16
preweak-rec-def-target=0.30
preweak-rec-dd-improve=0.02
preweak-rec-max-dd=0.08
crisis-diagnostics=true
```

Expected behavior: Candidate D should avoid Candidate C's weaker 2016 recovery activations. Reject Candidate D if this segment remains worse than Candidate B by more than `0.1` net-profit points.

## Verification

- Compared Candidate D stress JSON against current-source Candidate B stress JSON using `.agents/plugins/aegis-optimization-workflow/scripts/compare_aegis_backtests.py`.
- Extracted Candidate D metrics using `.agents/plugins/aegis-optimization-workflow/scripts/extract_aegis_metrics.py`.
- Confirmed `log-index.csv` remained structurally valid with zero errors and warnings after normalization.
- No trading code was changed.

## Risks And Open Questions

- Passing stress by matching Candidate B means Candidate D is only safe to continue testing; it does not prove long-window improvement.
- The next gates must prove that the four-week confirmation still captures useful 2025 redeployment while avoiding short-lived 2016 and 2022 activations.
