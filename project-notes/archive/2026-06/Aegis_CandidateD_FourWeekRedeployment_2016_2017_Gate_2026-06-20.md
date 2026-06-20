---
id: AEGIS-BT-2026-06-20-CANDIDATE-D-2016-2017
type: backtest-analysis
status: accepted
date: 2026-06-20
topic: AegisGrowthAllocation
tags: [aegis, candidate-d, long-segment, post-stress-redeployment]
related:
  - project-notes/archive/2026-06/Aegis_CandidateD_PostStressRedeployment_Hypothesis_2026-06-20.md
  - project-notes/archive/2026-06/Aegis_CandidateD_FourWeekRedeployment_StressGate_2026-06-20.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2016_2017.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2016_2017_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv
---

# Candidate D Four-Week Redeployment 2016-2017 Gate

## Agent Summary

Candidate D passed the segmented long `2016-2017` gate. The four-week confirmation variant improved net profit versus Candidate B and Candidate C, while keeping drawdown unchanged at `9.4%`. This clears the hypothesis rejection rule for the 2016 path and allows the next gate: segmented long `2022-2023`.

## Decision Or Finding

Candidate D may advance to the `2022-2023` segmented long gate.

This does not approve implementation, default changes, paper trading, or live deployment.

## Parameters

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

## Segment Metrics

| Metric | Candidate B | Candidate C | Candidate D | D vs B |
|---|---:|---:|---:|---:|
| Net profit | 35.679% | 35.359% | 36.728% | +1.049 pts |
| CAGR | 16.513% | 16.376% | 16.964% | +0.451 pts |
| Drawdown | 9.4% | 9.4% | 9.4% | 0.0 pts |
| Sharpe | 1.205 | 1.182 | 1.212 | +0.007 |
| PSR | 68.210% | 66.951% | 68.408% | +0.198 pts |
| Orders | 277 | 269 | 263 | -14 |
| Fees | $391.22 | $376.27 | $377.45 | -$13.77 |
| Turnover | 2.62% | 2.50% | 2.53% | -0.09 pts |
| Win rate | 63% | 61% | 62% | -1 pt |
| Profit factor | 2.6731 | 2.5044 | 2.8880 | +0.2149 |

The helper comparison script reported a strict win-rate failure versus Candidate B, but the pre-registered rejection criterion for this gate was net-profit degradation versus Candidate B by more than `0.1` points. Candidate D passed that gate.

## Diagnostic Evidence

Candidate D normalized log:

- `2026-06-20_224645__AegisGrowthAllocation__ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2016_2017_logs.txt`

Diagnostic row check:

- weekly rows: `104`;
- first row: `2016-01-04`;
- last row: `2017-12-26`;
- active recovery weeks: `2`;
- max recovery confirmation count: `5`;
- reason counts: `base-regime=81`, `drawdown-signals=21`, `pre-weak-recovery-confirmed=2`.

Recovery-active rows:

| Date | DD | Confirm | Target | Growth |
|---|---:|---:|---|---|
| 2016-06-27 | 0.0604 | 4 | `G0.1600/D0.3000/C0.5400` | `AMZN,NVDA` |
| 2016-07-05 | 0.0472 | 5 | `G0.1600/D0.3000/C0.5400` | `AMZN,META,NVDA` |

This confirms the four-week gate reduced the Candidate C activation set while improving the segment outcome.

## Next Gate

Run Candidate D segmented long `2022-2023`:

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

Expected behavior: avoid non-beneficial Candidate C recovery activations in 2022/2023 and avoid degradation versus Candidate B.

## Verification

- Compared Candidate D `2016-2017` JSON against Candidate B and Candidate C segment JSONs using `.agents/plugins/aegis-optimization-workflow/scripts/compare_aegis_backtests.py`.
- Extracted Candidate D metrics using `.agents/plugins/aegis-optimization-workflow/scripts/extract_aegis_metrics.py`.
- Confirmed normalized diagnostic log row coverage and recovery activation count.
- No trading code was changed.

## Risks And Open Questions

- This is still segmented diagnostic evidence; it does not replace a full uninterrupted long-window backtest.
- Candidate D still needs to pass `2022-2023`, retain the `2024-2026` redeployment gain, and then pass the full long-window comparison before it can become promotion evidence.
