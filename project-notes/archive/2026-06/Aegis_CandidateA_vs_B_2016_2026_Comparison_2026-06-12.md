---
id: AEGIS-BT-2026-06-12-CANDIDATE-A-VS-B
type: backtest-analysis
status: reviewed
date: 2026-06-12
topic: AegisGrowthAllocation
tags: [aegis, candidate-a, candidate-b, backtest, validation]
related:
  - project-notes/archive/2026-06/Aegis_CandidateB_2016_2026_Backtest_Analysis_2026-06-12.md
  - project-notes/archive/2026-06/Aegis_CandidateB_Next_Optimization_Proposal_2026-06-12.md
  - project-notes/archive/2026-06/Aegis_Weak_Sleeve_Target_Parameterization_2026-06-08.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_CandidateA_Long_2016_2026.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_CandidateA_Long_2016_2026_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-12_223003__AegisGrowthAllocation__ManualGrid_WeakSleeve_CandidateA_Long_2016_2026_logs.txt
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_CandidateB_Long_2016_2026.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_CandidateB_Long_2016_2026_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-12_222332__AegisGrowthAllocation__ManualGrid_WeakSleeve_CandidateB_Long_2016_2026_logs.txt
---

# Aegis Candidate A vs B 2016-2026 Comparison - 2026-06-12

## Agent Summary

Candidate B remains the better long-window baseline. Candidate A slightly reduces PreWeak activations but underperforms Candidate B on net profit, CAGR, Sharpe, Sortino, PSR, expectancy, order count, and fees, with no drawdown improvement. This comparison supports keeping Candidate B defaults and does not justify reverting `pre-weak-dd-threshold` from `0.04` to `0.05`.

## Run Setup

Both runs used:

- Configured period: `2016-01-01` to `2026-06-11`
- Actual metadata period: `2016-01-01` to `2026-03-14`
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `pre-weak-growth-target=0.12`
- `pre-weak-def-target=0.30`
- `weak-growth-target=0.10`

Only threshold differed:

- Candidate A: `pre-weak-dd-threshold=0.05`
- Candidate B: `pre-weak-dd-threshold=0.04`

## Headline Comparison

| Metric | Candidate A | Candidate B | Read |
| --- | ---: | ---: | --- |
| Net Profit | `350.119%` | `354.365%` | B better by `4.246` points |
| CAGR | `15.884%` | `15.991%` | B slightly better |
| End Equity | `$135,035.59` | `$136,309.55` | B better by `$1,273.96` |
| Drawdown | `13.700%` | `13.700%` | Tie |
| Sharpe | `0.861` | `0.885` | B better |
| Sortino | `0.904` | `0.934` | B better |
| PSR | `61.128%` | `65.167%` | B better |
| Orders | `1598` | `1581` | B has `17` fewer orders |
| Fees | `$1,864.37` | `$1,821.95` | B has `$42.42` lower fees |
| Expectancy | `0.507` | `0.520` | B better |

## Annual Difference

| Year | Candidate A | Candidate B | B - A |
| --- | ---: | ---: | ---: |
| 2016 | `8.37%` | `9.86%` | `+1.49` |
| 2017 | `25.33%` | `27.02%` | `+1.69` |
| 2018 | `10.67%` | `7.25%` | `-3.42` |
| 2019 | `18.40%` | `18.38%` | `-0.02` |
| 2020 | `30.55%` | `30.49%` | `-0.06` |
| 2021 | `35.52%` | `35.64%` | `+0.12` |
| 2022 | `-10.22%` | `-9.91%` | `+0.31` |
| 2023 | `29.05%` | `29.00%` | `-0.05` |
| 2024 | `19.27%` | `20.11%` | `+0.84` |
| 2025 | `6.12%` | `6.37%` | `+0.25` |
| 2026 partial | `-2.50%` | `-2.40%` | `+0.10` |

## Diagnostics

Candidate A:

- PreWeak weeks: `114`
- NonPreWeak weeks: `418`
- Weak regime weeks: `45`
- Severe crash weeks: `0`
- PreWeak average drawdown: `7.20%`
- PreWeak forward 4-week average: `1.08%`
- PreWeak forward 4-week win rate: `70.27%`

Candidate B:

- PreWeak weeks: `127`
- NonPreWeak weeks: `405`
- Weak regime weeks: `45`
- Severe crash weeks: `0`
- PreWeak average drawdown: `6.69%`
- PreWeak forward 4-week average: `1.20%`
- PreWeak forward 4-week win rate: `71.54%`

## Decision Read

Candidate B's lower threshold activates PreWeak more often, but the extra activations are net helpful in this long-span comparison. B improves aggregate performance and risk-adjusted metrics without increasing drawdown, orders, or fees. The earlier concern that Candidate B may add unnecessary sensitivity is not supported by this full-span comparison.

## Recommendation

Keep Candidate B defaults:

- `pre-weak-dd-threshold=0.04`
- `pre-weak-growth-target=0.12`
- `pre-weak-def-target=0.30`
- `weak-growth-target=0.10`

Do not revert to Candidate A. Do not start severe-crash code work from this result alone. The next useful comparison is either:

- an all-defensive-off long-span control, or
- a focused review of benchmark-relative underperformance and 2022/partial-2026 weakness.

## File Hygiene

The downloaded long-run files were moved from `C:/Users/douya/Downloads` into `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs` with normalized descriptive names. The corresponding source files were removed from Downloads by the move operation.
