---
id: AEGIS-BT-2026-06-12-CANDIDATE-B-LONG
type: backtest-analysis
status: reviewed
date: 2026-06-12
topic: AegisGrowthAllocation
tags: [aegis, candidate-b, backtest, validation]
related:
  - project-notes/archive/2026-06/Aegis_CandidateB_Next_Optimization_Proposal_2026-06-12.md
  - project-notes/archive/2026-06/Aegis_Weak_Sleeve_Target_Parameterization_2026-06-08.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_CandidateB_Long_2016_2026.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_CandidateB_Long_2016_2026_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-12_222332__AegisGrowthAllocation__ManualGrid_WeakSleeve_CandidateB_Long_2016_2026_logs.txt
---

# Aegis Candidate B 2016-2026 Backtest Analysis - 2026-06-12

## Agent Summary

Candidate B ran correctly over the intended long validation span, with actual available data ending on 2026-03-14. The result supports keeping Candidate B as the current baseline, but it does not justify new severe-crash work yet. The main concerns are benchmark-relative warnings, weak 2022 and partial 2026 behavior, and continued overfitting risk from many parameters.

## Run Metadata

- Run name: `Retrospective Violet Cobra`
- Configured start/end: `2016-01-01` to `2026-06-11`
- Actual metadata start/end: `2016-01-01` to `2026-03-14`
- Deployment identity: `AegisGrowthAllocation-2026-06-08-candidate-b-defaults`
- Source revision: `4499311dc`
- Files were later moved from Downloads into `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs` with normalized descriptive names.
- Candidate B parameters confirmed:
  - `pre-weak-dd-threshold=0.04`
  - `pre-weak-growth-target=0.12`
  - `pre-weak-def-target=0.30`
  - `weak-growth-target=0.10`
  - `pre-weak-guard-enabled=true`
  - `weak-stress-overlay-enabled=false`
  - `severe-crash-override-enabled=false`

## Headline Results

| Metric | Value |
| --- | ---: |
| Net Profit | `354.365%` |
| CAGR | `15.991%` |
| End Equity | `$136,309.55` |
| Drawdown | `13.700%` |
| Sharpe | `0.885` |
| Sortino | `0.934` |
| PSR | `65.167%` |
| Total Orders | `1581` |
| Fees | `$1,821.95` |
| Portfolio Turnover | `3.07%` |

## Annual Return Read

| Year | Return |
| --- | ---: |
| 2016 | `9.86%` |
| 2017 | `27.02%` |
| 2018 | `7.25%` |
| 2019 | `18.38%` |
| 2020 | `30.49%` |
| 2021 | `35.64%` |
| 2022 | `-9.91%` |
| 2023 | `29.00%` |
| 2024 | `20.11%` |
| 2025 | `6.37%` |
| 2026 partial | `-2.40%` |

## Diagnostics

- Diagnostic weeks: `532`
- PreWeak weeks: `127`
- NonPreWeak weeks: `405`
- Weak regime weeks: `45`
- Severe crash weeks: `0`
- PreWeak average drawdown: `6.69%`
- NonPreWeak average drawdown: `2.35%`
- PreWeak forward averages:
  - 4-week: `1.20%`
  - 8-week: `2.82%`
  - 12-week: `4.38%`
- PreWeak 4-week win rate: `71.54%`

## Rolling-Window Risks

- Worst rolling 12-month net result: `M12_20221231`, net `-9.91%`, drawdown `11.80%`, Sharpe `-1.561`.
- Worst full-run drawdown point: `2020-03-13`, drawdown about `13.72%`.
- Partial 2026 remains weak, matching the earlier short-run sanity check directionally.

## QuantConnect Analysis Warnings

- Low margin utilization on many days.
- Parameter-count overfitting risk: `17` parameters detected.
- Strategy Sharpe was lower than benchmark Sharpe in the exported analysis.
- COVID-19 crisis event underperformed benchmark on risk-adjusted return.
- Daily excess return significance test did not reject the null; p-value `0.3345`.

## Decision Read

The long run supports keeping Candidate B as the working baseline, but not because it is flawless. It has acceptable absolute performance and strong post-2023 behavior, while 2022 and partial 2026 remain weak. The diagnostics do not support adding a new strict severe-crash experiment yet; the cleaner next step is a long-window control comparison.

## Recommended Next Step

Run Candidate A over the same long span:

- `backtest-start=2016-01-01`
- `backtest-end=2026-06-11`
- same parameters as Candidate B except `pre-weak-dd-threshold=0.05`

Compare Candidate A vs Candidate B on CAGR, drawdown, Sharpe, PSR, 2022, partial 2026, orders, fees, PreWeak weeks, and rolling 12-month worst windows.

## Open Questions

- Is Candidate B still superior to Candidate A over the full `2016-01-01` to actual `2026-03-14` span?
- Does the `0.04` threshold add useful protection or just more PreWeak activations?
- Are benchmark-relative warnings acceptable for the strategy objective, or should the next optimization target benchmark-relative risk-adjusted return?
