---
id: AEGIS-BT-2026-06-13-OPTS09A-LONG
type: backtest-analysis
status: reviewed
date: 2026-06-13
topic: AegisGrowthAllocation
tags: [aegis, opts09a, optimization-rerun, pre-weak, backtest, validation]
related:
  - project-notes/archive/2026-06/Aegis_OptS07A_2016_2026_Backtest_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_Recent_Optimization_Review_2026-06-13.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_OptS09A_Long_2016_2026.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_OptS09A_Long_2016_2026_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-13_222957__AegisGrowthAllocation__ManualGrid_WeakSleeve_OptS09A_Long_2016_2026_logs.txt
---

# Aegis OptS09A 2016-2026 Backtest Analysis - 2026-06-13

## Agent Summary

OptS09A (`pre-weak-dd-threshold=0.09`, `pre-weak-growth-target=0.24`, `pre-weak-def-target=0.25`) is the best long-span headline result so far: highest CAGR, Sharpe, PSR, and expectancy among the rerun candidates. It is almost all-off on return while reducing all-off drawdown. It still fails the 2022 protection gate and is worse than OptS07A in 2022 and partial 2026, so it is not promotable without stress-window validation.

## Run Setup

- Run name: `Logical Orange Fox`
- Configured period: `2016-01-01` to `2026-06-11`
- Actual metadata period: `2016-01-01` to `2026-03-15`
- QuantConnect cloud export note: the cloud platform currently reports available data only through about `2026-03-14/15` even when `backtest-end=2026-06-11`. Treat this as the expected effective end date for this batch of uploaded cloud results, not as a run-specific defect.
- Algorithm version: `AegisGrowthAllocation-2026-06-08-candidate-b-defaults`
- Source revision: `4499311dc`
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `pre-weak-dd-threshold=0.09`
- `pre-weak-growth-target=0.24`
- `pre-weak-def-target=0.25`
- `weak-growth-target=0.10`

## Headline Comparison

| Metric | OptS09A | OptS07A | Selective09 | Candidate B | All-Off |
| --- | ---: | ---: | ---: | ---: | ---: |
| Net Profit | `577.969%` | `563.383%` | `552.974%` | `354.365%` | `580.604%` |
| CAGR | `20.630%` | `20.373%` | `20.187%` | `15.991%` | `20.676%` |
| Drawdown | `15.500%` | `15.300%` | `15.600%` | `13.700%` | `16.200%` |
| Sharpe | `1.113` | `1.105` | `1.090` | `0.885` | `1.102` |
| Sortino | `1.235` | `1.215` | `1.197` | `0.934` | `1.224` |
| PSR | `85.639%` | `85.128%` | `83.965%` | `65.167%` | `84.417%` |
| Expectancy | `0.656` | `0.628` | `0.632` | `0.520` | `0.615` |
| Orders | `1508` | `1510` | `1507` | `1581` | `1493` |
| Fees | `$1,862.35` | `$1,852.54` | `$1,859.45` | `$1,821.95` | `$1,860.48` |

## Diagnostics

- PreWeak weeks: `36`
- NonPreWeak weeks: `496`
- Weak regime weeks: `45`
- Severe crash weeks: `0`
- PreWeak average drawdown: `11.09%`
- PreWeak forward 4-week average: `1.03%`
- PreWeak forward 8-week average: `2.40%`
- PreWeak forward 12-week average: `4.60%`
- PreWeak forward 4-week win rate: `75.00%`
- PreWeak average target: `G0.2400/D0.2500/C0.5100`

## Annual Read

| Year | OptS09A | OptS07A | Candidate B | All-Off | Read |
| --- | ---: | ---: | ---: | ---: | --- |
| 2020 | `48.14%` | `45.38%` | `31.37%` | `48.14%` | Captures rebound like all-off |
| 2021 | `35.37%` | `36.10%` | `35.56%` | `35.37%` | Similar |
| 2022 | `-13.35%` | `-13.10%` | `-9.91%` | `-13.96%` | Fails protection gate |
| 2023 | `31.87%` | `31.87%` | `29.00%` | `35.05%` | Beats B, trails all-off |
| 2024 | `28.13%` | `27.03%` | `19.00%` | `27.03%` | Strong |
| 2025 | `17.92%` | `16.88%` | `5.96%` | `16.68%` | Strong |
| 2026 partial | `-3.87%` | `-3.26%` | `-2.36%` | `-3.25%` | Worse than OptS07A, B, and all-off |

Annual values are chart-derived from `Strategy Equity` series points and should be treated as supporting evidence.

## Gate Check

| Gate | Result | Pass |
| --- | --- | --- |
| Long-span CAGR at least `18.0%` | `20.630%` | Yes |
| Long-span drawdown no worse than `15.5%` | `15.500%` | Borderline yes |
| 2022 no worse than `-11.9%` | `-13.35%` | No |
| Partial 2026 no worse than all-off and near Candidate B | Worse than both | No |
| Sharpe and PSR improve versus B | Sharpe `1.113`; PSR `85.639%` | Yes |
| Orders no more than B + `5%` | `1508`, below B | Yes |
| Fees no more than B + `10%` | `$1,862.35`, within gate | Yes |

## Decision Read

OptS09A is the raw optimizer winner and the best return/risk result in the long-span headline metrics. The cost is weaker stress behavior: the 2022 result remains materially worse than Candidate B and the partial 2026 result is also worse than OptS07A and all-off.

Do not promote OptS09A directly. Stop broad long-span tuning and validate the two finalists on standard stress windows, starting with 2021-2022 because that is the known failure case.

## Recommended Next Move

Run a normal backtest for OptS09A on the 2021-2022 stress window:

- `backtest-start=2021-01-01`
- `backtest-end=2022-12-31`
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `pre-weak-dd-threshold=0.09`
- `pre-weak-growth-target=0.24`
- `pre-weak-def-target=0.25`
- `weak-growth-target=0.10`

If OptS09A is materially worse than OptS07A or Candidate B in this window, prefer OptS07A as the finalist despite OptS09A's better long-span headline metrics.
