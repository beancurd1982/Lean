---
id: AEGIS-BT-2026-06-20-CANDIDATE-D-2024-2026
type: backtest-analysis
status: accepted
date: 2026-06-20
topic: AegisGrowthAllocation
tags: [aegis, candidate-d, long-segment, post-stress-redeployment]
related:
  - project-notes/archive/2026-06/Aegis_CandidateD_PostStressRedeployment_Hypothesis_2026-06-20.md
  - project-notes/archive/2026-06/Aegis_CandidateD_FourWeekRedeployment_2022_2023_Gate_2026-06-20.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2024_2026-01-01.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2024_2026-01-01_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv
---

# Candidate D Four-Week Redeployment 2024-2026 Gate

## Agent Summary

Candidate D passed the segmented long `2024-2026-01-01` retention gate. It retained essentially all of Candidate C's 2025 redeployment gain while using one fewer order and one fewer dollar of fees than Candidate C. Candidate D should advance to the full same-execution long-window test.

## Decision Or Finding

Candidate D may advance to the full long-window gate: `2016-01-01` through `2026-01-01`.

This does not approve implementation, default changes, paper trading, or live deployment.

## Parameters

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

## Segment Metrics

| Metric | Candidate B | Candidate C | Candidate D | D vs B | D vs C |
|---|---:|---:|---:|---:|---:|
| Net profit | 27.803% | 30.452% | 30.447% | +2.644 pts | -0.005 pts |
| CAGR | 13.037% | 14.202% | 14.199% | +1.162 pts | -0.003 pts |
| Drawdown | 10.7% | 10.7% | 10.7% | 0.0 pts | 0.0 pts |
| Sharpe | 0.398 | 0.478 | 0.478 | +0.080 | 0.000 |
| PSR | 44.997% | 49.830% | 49.824% | +4.827 pts | -0.006 pts |
| Orders | 373 | 380 | 379 | +6 | -1 |
| Fees | $373.00 | $380.00 | $379.00 | +$6.00 | -$1.00 |
| Turnover | 3.74% | 3.75% | 3.74% | 0.00 pts | -0.01 pts |
| Win rate | 69% | 70% | 70% | +1 pt | 0 pts |
| Profit factor | 1.9796 | 2.0643 | 2.0657 | +0.0861 | +0.0014 |

The helper comparison script reports strict failures versus Candidate C on tiny net-profit and PSR deltas, but the hypothesis gate was to retain enough of the 2025 redeployment gain to justify a full long-window test. Candidate D retained essentially all of Candidate C's gain in this segment.

## Diagnostic Evidence

Candidate D normalized log:

- `2026-06-20_230229__AegisGrowthAllocation__ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2024_2026-01-01_logs.txt`

Diagnostic row check:

- weekly rows: `105`;
- first row: `2024-01-02`;
- last row: `2025-12-29`;
- active recovery weeks: `2`;
- max recovery confirmation count: `5`;
- reason counts: `base-regime=75`, `drawdown-signals=28`, `pre-weak-recovery-confirmed=2`.

Recovery-active rows:

| Date | DD | Confirm | Target | Growth |
|---|---:|---:|---|---|
| 2025-07-21 | 0.0467 | 4 | `G0.1600/D0.3000/C0.5400` | `AVGO,META,MSFT,NVDA` |
| 2025-07-28 | 0.0416 | 5 | `G0.1600/D0.3000/C0.5400` | `AVGO,META,MSFT,NVDA` |

The 2025 redeployment path was retained:

- Candidate D returned to base-regime target `G0.4500/D0.3000/C0.2500` by `2025-09-02`.
- Candidate D then captured the large September weeks:
  - `2025-09-08`: `1.966%`;
  - `2025-09-15`: `2.533%`.

## Next Gate

Run Candidate D full same-execution long-window validation:

```text
backtest-start=2016-01-01
backtest-end=2026-01-01
preweak-recovery-enabled=true
preweak-rec-confirm-weeks=4
preweak-rec-growth-target=0.16
preweak-rec-def-target=0.30
preweak-rec-dd-improve=0.02
preweak-rec-max-dd=0.08
crisis-diagnostics=true
```

Expected behavior: preserve Candidate B stress behavior, beat Candidate C long-window performance meaningfully, and avoid material increases in drawdown, orders, fees, and turnover.

## Verification

- Compared Candidate D `2024-2026` JSON against Candidate B and Candidate C segment JSONs using `.agents/plugins/aegis-optimization-workflow/scripts/compare_aegis_backtests.py`.
- Extracted Candidate D metrics using `.agents/plugins/aegis-optimization-workflow/scripts/extract_aegis_metrics.py`.
- Confirmed normalized diagnostic log row coverage and recovery activation count.
- No trading code was changed.

## Risks And Open Questions

- Segment evidence is now favorable, but full long-window compounding can differ from isolated two-year segments.
- Candidate D remains a parameter-only research candidate until it passes the full same-execution long-window comparison.
