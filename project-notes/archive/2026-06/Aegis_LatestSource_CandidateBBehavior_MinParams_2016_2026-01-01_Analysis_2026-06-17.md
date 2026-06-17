---
id: AEGIS-BT-2026-06-17-LATESTSOURCE-CANDIDATEB-BEHAVIOR-MINPARAMS
type: backtest-analysis
status: reviewed
date: 2026-06-17
topic: AegisGrowthAllocation
tags: [aegis, candidate-b, candidate-c, backtest, execution-model, 2016-2026]
related:
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_Long_2016_2026-01-01_Analysis_2026-06-16.md
  - project-notes/archive/2026-06/Aegis_CandidateB_DateMatched_Control_Attempt_2016_2026-01-01_Analysis_2026-06-16.md
  - project-notes/archive/2026-06/Aegis_CandidateB_2016_2026_Backtest_Analysis_2026-06-12.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-17_215135__AegisGrowthAllocation__ManualGrid_LatestSource_CandidateBBehavior_MinParams_Long_2016_2026-01-01_logs.txt
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_LatestSource_CandidateBBehavior_MinParams_Long_2016_2026-01-01.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_LatestSource_CandidateBBehavior_MinParams_Long_2016_2026-01-01_orders.csv
---

# Latest Source Candidate B Behavior Min-Params Analysis - 2026-06-17

## Agent Summary

The latest source was run with only `backtest-start=2016-01-01` and `backtest-end=2026-01-01`, leaving recovery disabled by default.

This is a valid latest-source Candidate B-behavior run, but it is not identical to the prior Candidate B tag-era baseline. The clearest difference is execution: this run generated `Market On Close` orders, while the 2026-06-12 Candidate B baseline generated regular `Market` orders. The first order batch diverges on 2016-01-04, so performance differences begin immediately.

## Settings

Cloud parameters entered:

- `backtest-start = 2016-01-01`
- `backtest-end = 2026-01-01`

No recovery parameter was entered.

The log confirms:

- `PreWeakGrowthTarget=0.12`
- `PreWeakDefensiveTarget=0.30`
- `PreWeakRecoveryEnabled=False`
- `PreWeakRecoveryGrowthTarget=0.16`
- `PreWeakRecoveryDefensiveTarget=0.30`
- `WeakGrowthTarget=0.10`

## Headline Metrics

| Metric | Latest source, Candidate B behavior | Prior Candidate B baseline | Candidate C long |
|---|---:|---:|---:|
| Final chart timestamp | 2025-12-31 21:15 UTC | 2026-03-13 20:15 UTC | 2025-12-31 21:15 UTC |
| Equity | 125,157.95 | 136,309.55 | 155,912.61 |
| Compounding Annual Return | 15.34% | 15.99% | 17.90% |
| Net Profit | 317.19% | 354.37% | 419.71% |
| Drawdown | 14.00% | 13.70% | 13.70% |
| Sharpe Ratio | 0.842 | 0.885 | 1.011 |
| Probabilistic Sharpe Ratio | 58.54% | 65.17% | 78.12% |
| Portfolio Turnover | 3.04% | 3.07% | 3.02% |
| Fees | 1,741.70 | 1,818.95 | 1,790.57 |
| Order rows | 1,542 | 1,581 | 1,550 |
| Win Rate | 65.71% | 66.24% | 67.25% |
| Profit Factor | 2.479 | 2.429 | 2.793 |

Date-aligned equity check:

- Prior Candidate B baseline at 2025-12-31: `139,608.16`
- Latest-source Candidate B behavior at 2025-12-31: `125,157.95`

The difference is too large to treat as ordinary date-window noise.

## Execution Difference

The first order-level divergence occurs on the initial rebalance, 2016-01-04:

- Prior Candidate B baseline: regular `Market` orders.
- Latest-source Candidate B behavior: `Market On Close` orders.

Example first rows:

- Prior baseline JNJ: `2016-01-04T15:00:00Z`, price `77.5119984`, type `Market`.
- Latest-source JNJ: `2016-01-04T15:00:00Z`, price `75.8669813`, type `Market On Close`.

The algorithm source still uses `SetHoldings(...)` and `SetBrokerageModel(InteractiveBrokersBrokerage, AccountType.Margin)`. The order-type change appears in the QuantConnect-generated orders artifact, not in a direct explicit `MarketOnCloseOrder(...)` call.

## Decision

Do not compare Candidate C only against the older 2026-06-12 Candidate B artifact without noting execution-model drift.

The latest-source Candidate B-behavior run is a cleaner date-aligned comparator for Candidate C, but it raises a separate question: why did the same conceptual Candidate B behavior produce MOC orders and weaker performance?

Candidate C remains stronger than this latest-source Candidate B-behavior run on the same final chart date, but the next validation should use the exact Candidate B tag to isolate whether the difference comes from source version, cloud project settings, or QuantConnect execution behavior.

## Recommended Next Move

Checkout/upload the exact Candidate B tag and rerun with only:

- `backtest-start = 2016-01-01`
- `backtest-end = 2026-01-01`

Tag to use:

- `aegis-growth-allocation-v2026.06.08-candidate-b-paper-live`

This will answer whether the tag itself now produces `Market` or `Market On Close` orders under the current cloud project.

## Workflow Notes

- Downloaded files were moved from the default Downloads folder into `BackTestLogs`.
- The repo-local log normalizer produced the normalized log filename used above.
- The original Downloads files were removed by moving them into the repo artifact folder.
- The log was truncated by QuantConnect's 100 KB backtest log cap, but JSON and order artifacts were complete enough for headline and execution analysis.
