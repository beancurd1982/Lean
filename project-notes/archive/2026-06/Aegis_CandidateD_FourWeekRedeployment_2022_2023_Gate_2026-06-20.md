---
id: AEGIS-BT-2026-06-20-CANDIDATE-D-2022-2023
type: backtest-analysis
status: accepted
date: 2026-06-20
topic: AegisGrowthAllocation
tags: [aegis, candidate-d, long-segment, post-stress-redeployment]
related:
  - project-notes/archive/2026-06/Aegis_CandidateD_PostStressRedeployment_Hypothesis_2026-06-20.md
  - project-notes/archive/2026-06/Aegis_CandidateD_FourWeekRedeployment_2016_2017_Gate_2026-06-20.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2022_2023.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2022_2023_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv
---

# Candidate D Four-Week Redeployment 2022-2023 Gate

## Agent Summary

Candidate D passed the segmented long `2022-2023` no-degradation gate. Metrics matched Candidate B and Candidate C exactly, and diagnostics confirmed the four-week gate suppressed all Candidate C-style short recovery activations in this segment.

## Decision Or Finding

Candidate D may advance to the `2024-2026-01-01` segmented long gate.

This does not approve implementation, default changes, paper trading, or live deployment.

## Parameters

```text
backtest-start=2022-01-01
backtest-end=2023-12-31
preweak-recovery-enabled=true
preweak-rec-confirm-weeks=4
preweak-rec-growth-target=0.16
preweak-rec-def-target=0.30
preweak-rec-dd-improve=0.02
preweak-rec-max-dd=0.08
crisis-diagnostics=true
```

## Segment Metrics

| Metric | Candidate B | Candidate C | Candidate D | Gate |
|---|---:|---:|---:|---|
| Net profit | 16.300% | 16.300% | 16.300% | PASS |
| CAGR | 7.868% | 7.868% | 7.868% | PASS |
| Drawdown | 12.0% | 12.0% | 12.0% | PASS |
| Sharpe | 0.269 | 0.269 | 0.269 | PASS |
| PSR | 29.035% | 29.035% | 29.035% | PASS |
| Orders | 310 | 310 | 310 | PASS |
| Fees | $310.00 | $310.00 | $310.00 | PASS |
| Turnover | 3.36% | 3.36% | 3.36% | PASS |
| Win rate | 61% | 61% | 61% | PASS |
| Profit factor | 1.4358 | 1.4358 | 1.4358 | PASS |

## Diagnostic Evidence

Candidate D normalized log:

- `2026-06-20_225509__AegisGrowthAllocation__ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2022_2023_logs.txt`

Diagnostic row check:

- weekly rows: `104`;
- first row: `2022-01-03`;
- last row: `2023-12-26`;
- active recovery weeks: `0`;
- max recovery confirmation count: `3`;
- reason counts: `base-regime=63`, `drawdown-signals=41`.

This confirms the four-week gate blocked the short-lived recovery activations that appeared in Candidate C's 2022-2023 diagnostics.

## Next Gate

Run Candidate D segmented long `2024-2026-01-01`:

```text
backtest-start=2024-01-01
backtest-end=2026-01-01
preweak-recovery-enabled=true
preweak-rec-confirm-weeks=4
preweak-rec-growth-target=0.16
preweak-rec-def-target=0.30
preweak-rec-dd-improve=0.02
preweak-rec-max-dd=0.08
crisis-diagnostics=true
```

Expected behavior: retain enough of Candidate C's 2025 redeployment benefit to justify a full long-window test.

## Verification

- Compared Candidate D `2022-2023` JSON against Candidate B and Candidate C segment JSONs using `.agents/plugins/aegis-optimization-workflow/scripts/compare_aegis_backtests.py`.
- Extracted Candidate D metrics using `.agents/plugins/aegis-optimization-workflow/scripts/extract_aegis_metrics.py`.
- Confirmed normalized diagnostic log row coverage and recovery activation count.
- No trading code was changed.

## Risks And Open Questions

- This gate only proves no degradation in 2022-2023.
- Candidate D still needs to prove it retains the 2025 redeployment gain before a full long-window test is worthwhile.
