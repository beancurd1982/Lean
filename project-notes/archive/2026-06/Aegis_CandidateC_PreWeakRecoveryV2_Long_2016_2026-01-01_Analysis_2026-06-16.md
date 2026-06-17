---
id: AEGIS-BT-2026-06-16-CANDIDATEC-PREWEAK-RECOVERY-V2-LONG
type: backtest-analysis
status: reviewed
date: 2026-06-16
topic: AegisGrowthAllocation
tags: [aegis, candidate-c, preweak-recovery, backtest, long-window, 2016-2026]
related:
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_Stress04_2021_2022_Analysis_2026-06-16.md
  - project-notes/archive/2026-06/Aegis_CandidateB_2016_2026_Backtest_Analysis_2026-06-12.md
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeak_Recovery_v2_Implementation_Plan_2026-06-15.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-16_233944__AegisGrowthAllocation__ManualGrid_CandidateC_PreWeakRecoveryV2_Long_2016_2026-01-01_logs.txt
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_Long_2016_2026-01-01.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_Long_2016_2026-01-01_orders.csv
---

# Candidate C PreWeak Recovery v2 Long Analysis - 2026-06-16

## Agent Summary

Candidate C v2 was run on the long window with `preweak-recovery-enabled=true` and `backtest-end=2026-01-01`.

The result is promising: versus the existing Candidate B long baseline, Candidate C improved CAR, net profit, Sharpe, PSR, fees, turnover, and order count while keeping drawdown unchanged. It is not yet promotion-ready because the available Candidate B comparison file ends later, on 2026-03-09, so the next required move is a date-matched Candidate B control ending 2026-01-01.

## Settings

Cloud parameters entered:

- `backtest-start = 2016-01-01`
- `backtest-end = 2026-01-01`
- `crisis-diagnostics = true`
- `preweak-recovery-enabled = true`

Source defaults supplied the rest:

- Normal PreWeak: `G0.12/D0.30/C0.58`
- Recovery PreWeak: `G0.16/D0.30/C0.54`
- Weak: `G0.10/D0.40/C0.50`
- Recovery gates: `dd-improve=0.02`, `max-dd=0.08`, `confirm-weeks=2`

The `2026-01-01` end date is intentional. Prior QuantConnect cloud runs showed inconsistent availability for later 2026 data, so 2026-01-01 is currently the safer validation endpoint.

## Headline Comparison

Comparison is against the existing Candidate B long baseline file. This is useful directional evidence but not an exact control because the Candidate B artifact ends on 2026-03-09 while this Candidate C run ends on 2025-12-29.

| Metric | Candidate B existing long baseline | Candidate C v2 long |
|---|---:|---:|
| Trade-stat start | 2016-01-04 | 2016-01-04 |
| Trade-stat end | 2026-03-09 | 2025-12-29 |
| Compounding Annual Return | 15.99% | 17.90% |
| Net Profit | 354.37% | 419.71% |
| Drawdown | 13.70% | 13.70% |
| Sharpe Ratio | 0.885 | 1.011 |
| Probabilistic Sharpe Ratio | 65.17% | 78.12% |
| Portfolio Turnover | 3.07% | 3.02% |
| Fees | 1,818.95 | 1,790.57 |
| Order rows | 1,581 | 1,550 |
| Win Rate | 66.24% | 67.25% |
| Profit Factor | 2.429 | 2.793 |

Interpretation: Candidate C is the best long-run signal from the recovery-gated branch so far. The improvement is large enough to justify another control run, but not enough to skip the date-match requirement.

## Recovery Diagnostics

Visible log evidence shows four recovery activations:

- 2016-06-06
- 2016-06-13
- 2016-06-20
- 2016-06-27

All four were `Override=pre-weak-recovery` and `PreWeakRecovery=True`.

The visible activation conditions were consistent with the intended gate:

- Recovery segment: `2`
- Local trough drawdown: `0.0746`
- Active drawdown range: about `0.0446` to `0.0518`
- Recovery amount range: about `0.0228` to `0.0301`
- Confirmation weeks: `2` through `5`

QuantConnect truncated the diagnostic log with the 100 KB backtest log limit, so later diagnostic summary coverage is unavailable from this artifact. JSON and order metrics remain usable.

## Decision

Candidate C v2 should remain under active evaluation and should not be promoted yet.

Reasons:

- It preserved the 2021-2022 stress-window behavior in the prior run.
- It improved the long-run headline metrics in this run.
- It activated only under the recovery gate, not under arbitrary fixed PreWeak growth.
- The comparison is not date-matched, so the evidence is not final.

## Recommended Next Move

Run a date-matched Candidate B control:

- `backtest-start = 2016-01-01`
- `backtest-end = 2026-01-01`
- `pre-weak-guard-enabled = true`
- `preweak-recovery-enabled = false` or omit it if the default is false
- Omit other default parameters unless the cloud project requires them.

`crisis-diagnostics` is optional for this control. If the goal is only headline metric comparison, omit it or leave it false to avoid the QuantConnect log cap. If diagnostics are needed, enable it knowing that the long-run log may truncate.

Promotion gate:

- If the date-matched Candidate B control remains close to the old Candidate B baseline and Candidate C keeps the same advantage, then Candidate C deserves focused validation or a narrow optimization around the recovery gate.
- If the advantage disappears after date matching, do not optimize Candidate C further until the root cause is understood.

## Workflow Notes

- Downloaded files were moved from the default Downloads folder into `BackTestLogs`.
- The repo-local log normalizer produced the normalized log filename used above.
- The original Downloads files were removed by moving them into the repo artifact folder.
- Keep using `2026-01-01` as the current safe long-run cloud endpoint unless QuantConnect data availability is confirmed for a later date.
