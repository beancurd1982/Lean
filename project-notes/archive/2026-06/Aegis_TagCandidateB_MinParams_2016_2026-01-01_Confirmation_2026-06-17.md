---
id: AEGIS-BT-2026-06-17-TAG-CANDIDATEB-MINPARAMS-CONFIRMATION
type: backtest-analysis
status: reviewed
date: 2026-06-17
topic: AegisGrowthAllocation
tags: [aegis, candidate-b, candidate-c, backtest, tag-confirmation, execution-model]
related:
  - project-notes/archive/2026-06/Aegis_LatestSource_CandidateBBehavior_MinParams_2016_2026-01-01_Analysis_2026-06-17.md
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_Long_2016_2026-01-01_Analysis_2026-06-16.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-17_220915__AegisGrowthAllocation__ManualGrid_TagCandidateB_MinParams_Long_2016_2026-01-01_logs.txt
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_TagCandidateB_MinParams_Long_2016_2026-01-01.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_TagCandidateB_MinParams_Long_2016_2026-01-01_orders.csv
---

# Tag Candidate B Min-Params Confirmation - 2026-06-17

## Agent Summary

The exact tag `aegis-growth-allocation-v2026.06.08-candidate-b-paper-live` was uploaded and rerun with only `backtest-start=2016-01-01` and `backtest-end=2026-01-01`.

The result exactly matches the latest-source Candidate B-behavior min-param run. This confirms the weaker current Candidate B read is not caused by Candidate C source changes. However, Candidate C comparison is still not fully clean because Candidate C's saved run used regular `Market` orders while the tag rerun used `Market On Close` orders.

## Headline Comparison

| Metric | Exact tag Candidate B rerun | Latest-source Candidate B behavior | Candidate C saved run |
|---|---:|---:|---:|
| Final chart timestamp | 2025-12-31 21:15 UTC | 2025-12-31 21:15 UTC | 2025-12-31 21:15 UTC |
| First order type | Market On Close | Market On Close | Market |
| Equity | 125,157.95 | 125,157.95 | 155,912.61 |
| Compounding Annual Return | 15.34% | 15.34% | 17.90% |
| Net Profit | 317.19% | 317.19% | 419.71% |
| Drawdown | 14.00% | 14.00% | 13.70% |
| Sharpe Ratio | 0.842 | 0.842 | 1.011 |
| Probabilistic Sharpe Ratio | 58.54% | 58.54% | 78.12% |
| Fees | 1,741.70 | 1,741.70 | 1,790.57 |
| Order rows | 1,542 | 1,542 | 1,550 |
| Win Rate | 65.71% | 65.71% | 67.25% |
| Profit Factor | 2.479 | 2.479 | 2.793 |

## Finding

The exact tag rerun confirms that the current cloud rerun of Candidate B is weaker than Candidate C on the same final chart date.

But this does not yet prove the recovery rule alone caused the improvement because order execution differs:

- Candidate B exact tag rerun: `Market On Close`
- Latest-source Candidate B-behavior run: `Market On Close`
- Candidate C saved long run: `Market`

This makes Candidate C promising, not deployment-ready.

## Interpretation

The research likely has produced a better candidate than the currently validated Candidate B behavior, but the project has not yet completed a clean same-execution validation for Candidate C.

Separately, the paper-live account has not been confirmed as running Candidate C. Candidate C is default-off in the source and requires `preweak-recovery-enabled=true`; if paper live was not redeployed with that setting, it is still effectively running Candidate B behavior.

## Recommended Next Move

Run Candidate C again in the same cloud project immediately after this exact tag run, using only:

- `backtest-start = 2016-01-01`
- `backtest-end = 2026-01-01`
- `preweak-recovery-enabled = true`

Do not add diagnostics or default parameters.

Decision gate:

- If Candidate C again uses `Market On Close` and still materially beats Candidate B, it becomes the leading deployment candidate.
- If Candidate C switches back to `Market` while Candidate B remains `Market On Close`, investigate QuantConnect project/order settings before promoting.
- If Candidate C loses the advantage under `Market On Close`, the saved Candidate C improvement was partly execution-driven and needs redesign or revalidation.

## Workflow Notes

- Downloaded files were moved from the default Downloads folder into `BackTestLogs`.
- The repo-local log normalizer produced the normalized log filename used above.
- The original Downloads files were removed by moving them into the repo artifact folder.
