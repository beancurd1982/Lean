---
id: AEGIS-BT-2026-06-16-CANDIDATEB-DATEMATCH-CONTROL-ATTEMPT
type: backtest-analysis
status: reviewed
date: 2026-06-16
topic: AegisGrowthAllocation
tags: [aegis, candidate-b, candidate-c, preweak-recovery, backtest, control-run, 2016-2026]
related:
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_Long_2016_2026-01-01_Analysis_2026-06-16.md
  - project-notes/archive/2026-06/Aegis_CandidateB_2016_2026_Backtest_Analysis_2026-06-12.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-16_235346__AegisGrowthAllocation__ManualGrid_CandidateB_DateMatched_Long_2016_2026-01-01_logs.txt
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateB_DateMatched_Long_2016_2026-01-01.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateB_DateMatched_Long_2016_2026-01-01_orders.csv
---

# Candidate B Date-Matched Control Attempt - 2026-06-16

## Agent Summary

This run was intended to be the date-matched Candidate B control for Candidate C PreWeak Recovery v2, using `backtest-end=2026-01-01`.

It is not a valid date-matched control. The Candidate B JSON chart runs through 2026-03-18, while the Candidate C comparison artifact ends on 2025-12-31. Treat this run as reviewed-but-not-decisive.

## Intended Settings

Requested control settings:

- `backtest-start = 2016-01-01`
- `backtest-end = 2026-01-01`
- `pre-weak-guard-enabled = true`
- recovery disabled or omitted

The log confirms the recovery feature was disabled:

- `PreWeakRecoveryEnabled=False`
- `PreWeakRecoveryGrowthTarget=0.16`
- `PreWeakRecoveryDefensiveTarget=0.30`

## Actual Calendar Coverage

Final JSON chart timestamps:

| Run | Final chart timestamp | Last order timestamp |
|---|---:|---:|
| Candidate B control attempt | 2026-03-18 20:15 UTC | 2026-03-09 14:00 UTC |
| Candidate C long run | 2025-12-31 21:15 UTC | 2025-12-29 15:00 UTC |

This means the two artifacts are not date-matched even though the intended `backtest-end` was 2026-01-01.

Probable cause: the cloud run did not apply the `backtest-end` parameter for this Candidate B attempt, or the downloaded artifact was not from the intended run. The local algorithm code still parses `backtest-end` and calls `SetEndDate(...)` when the parameter is present.

## Headline Metrics

The metrics are still useful as a latest-available Candidate B read, but not as the final Candidate C control.

| Metric | Candidate B control attempt | Candidate C long run |
|---|---:|---:|
| Final chart timestamp | 2026-03-18 | 2025-12-31 |
| Compounding Annual Return | 16.36% | 17.90% |
| Net Profit | 370.30% | 419.71% |
| Drawdown | 13.80% | 13.70% |
| Sharpe Ratio | 0.906 | 1.011 |
| Probabilistic Sharpe Ratio | 67.59% | 78.12% |
| Portfolio Turnover | 3.06% | 3.02% |
| Fees | 1,844.20 | 1,790.57 |
| Order rows | 1,594 | 1,550 |
| Win Rate | 66.04% | 67.25% |
| Profit Factor | 2.446 | 2.793 |

Even with the mismatched period, Candidate C remains directionally stronger. The evidence is not clean enough to promote Candidate C or start optimization.

## Decision

Do not use this run as the date-matched Candidate B control.

Candidate C remains promising, but the promotion gate remains open:

- Need an exact Candidate B control ending 2026-01-01.
- If QuantConnect keeps ignoring `backtest-end`, add or temporarily enable explicit date configuration logging so the cloud log prints the raw parameter value and applied end date.

## Recommended Next Move

Rerun Candidate B with only the required date parameters:

- `backtest-start = 2016-01-01`
- `backtest-end = 2026-01-01`

Do not add default parameters. Do not add recovery parameters. Leave `crisis-diagnostics` off for this control unless diagnostics are specifically needed.

Before launching, confirm that the parameter name is exactly `backtest-end` and the value is exactly `2026-01-01`.

## Workflow Notes

- Downloaded files were moved from the default Downloads folder into `BackTestLogs`.
- The repo-local log normalizer produced the normalized log filename used above.
- The original Downloads files were removed by moving them into the repo artifact folder.
- The normalizer shell command timed out, but inspection confirmed the rename and `log-index.csv` update completed.
