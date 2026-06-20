---
id: AEGIS-BT-2026-06-20-CANDIDATE-D-FULL-LONG
type: backtest-analysis
status: rejected
date: 2026-06-20
topic: AegisGrowthAllocation
tags: [aegis, candidate-d, full-long, rejected, post-stress-redeployment]
related:
  - project-notes/archive/2026-06/Aegis_CandidateD_PostStressRedeployment_Hypothesis_2026-06-20.md
  - project-notes/archive/2026-06/Aegis_CandidateD_FourWeekRedeployment_2024_2026_Gate_2026-06-20.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2016_2026-01-01.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2016_2026-01-01_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv
---

# Candidate D Four-Week Redeployment Full Long Rejection

## Agent Summary

Candidate D failed the full same-execution long-window gate. Despite passing stress and selected segmented gates, the full `2016-01-01` through `2026-01-01` result underperformed both Candidate B and Candidate C. The current four-week post-stress redeployment parameter set is rejected as a replacement candidate.

## Decision Or Finding

Reject current Candidate D:

```text
preweak-recovery-enabled=true
preweak-rec-confirm-weeks=4
preweak-rec-growth-target=0.16
preweak-rec-def-target=0.30
preweak-rec-dd-improve=0.02
preweak-rec-max-dd=0.08
```

Do not promote, implement, or paper-test this parameter set.

## Full Long Metrics

| Metric | Candidate B | Candidate C | Candidate D | D vs B | D vs C |
|---|---:|---:|---:|---:|---:|
| Net profit | 317.193% | 330.470% | 311.574% | -5.619 pts | -18.896 pts |
| CAGR | 15.342% | 15.704% | 15.186% | -0.156 pts | -0.518 pts |
| Drawdown | 14.0% | 14.0% | 14.0% | 0.0 pts | 0.0 pts |
| Sharpe | 0.842 | 0.859 | 0.827 | -0.015 | -0.032 |
| PSR | 58.542% | 60.400% | 56.353% | -2.189 pts | -4.047 pts |
| Orders | 1542 | 1535 | 1556 | +14 | +21 |
| Fees | $1747.99 | $1740.63 | $1759.93 | +$11.94 | +$19.30 |
| Turnover | 3.04% | 3.01% | 3.06% | +0.02 pts | +0.05 pts |
| Win rate | 68% | 69% | 68% | 0 pts | -1 pt |
| Profit factor | 2.4788 | 2.5362 | 2.4085 | -0.0703 | -0.1277 |

Candidate D failed the pre-registered long-window gate because it did not improve over Candidate C and was also worse than Candidate B.

## Diagnostic Limitation

Candidate D normalized log:

- `2026-06-20_231040__AegisGrowthAllocation__ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2016_2026-01-01_logs.txt`

The diagnostic log contained only:

- weekly rows: `147`;
- first row: `2016-01-04`;
- last row: `2018-10-22`;
- active recovery weeks in available rows: `2`;
- max recovery confirmation in available rows: `5`.

This log is truncated and is not sufficient for full-window attribution. The rejection is based on complete result JSON metrics, not the partial diagnostic stream.

## Interpretation

The segmented tests were useful but misleading as promotion evidence:

- stress passed by suppressing recovery activation;
- `2016-2017` improved versus B/C;
- `2022-2023` matched B/C;
- `2024-2026` retained Candidate C's 2025 gain;
- but the uninterrupted full long run lost enough compounding elsewhere to trail both B and C.

This confirms the workflow rule that segmented diagnostics can select what to test next, but cannot replace the full same-execution long-window gate.

## Next Step

Do not tune Candidate D by small parameter nudges yet. The next research step should be another hypothesis-design pass using the failed full long result:

- compare Candidate C vs Candidate D full-run metrics and orders;
- identify which periods in the uninterrupted run created the gap;
- require complete diagnostics or a less verbose diagnostic mode if full-window weekly logs are truncated by QuantConnect;
- design Candidate E only after the full-run gap is explained.

## Verification

- Compared Candidate D full long JSON against Candidate B and Candidate C same-execution long JSONs using `.agents/plugins/aegis-optimization-workflow/scripts/compare_aegis_backtests.py`.
- Extracted Candidate D metrics using `.agents/plugins/aegis-optimization-workflow/scripts/extract_aegis_metrics.py`.
- Checked the normalized diagnostic log and confirmed it is truncated.
- No trading code was changed.

## Risks And Open Questions

- Full-run diagnostic attribution is incomplete because the log stream ended in 2018.
- Candidate C remains the stronger long-window research reference, but still is not promotion-ready because it misses the Candidate B stress gate.
- Candidate B remains the stress baseline.
