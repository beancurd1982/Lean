---
id: AEGIS-BT-2026-06-13-SELECTIVE07-LONG
type: backtest-analysis
status: reviewed
date: 2026-06-13
topic: AegisGrowthAllocation
tags: [aegis, selective07, pre-weak, candidate-b, backtest, validation]
related:
  - project-notes/archive/2026-06/Aegis_Recent_Optimization_Review_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_AllDefensiveOff_2016_2026_Control_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_CandidateA_vs_B_2016_2026_Comparison_2026-06-12.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_Selective07_Long_2016_2026.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_Selective07_Long_2016_2026_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-13_212636__AegisGrowthAllocation__ManualGrid_WeakSleeve_Selective07_Long_2016_2026_logs.txt
---

# Aegis Selective 07 2016-2026 Backtest Analysis - 2026-06-13

## Agent Summary

Selective 07 is a strong improvement over Candidate B on long-span return and risk-adjusted metrics, while preserving some but not enough of Candidate B's downside protection. It passes the long-span CAGR, drawdown, Sharpe/PSR, order, and fee gates from the reviewed proposal, but fails the 2022 protection gate. This makes it promising evidence for pre-weak selectivity, not a promotion candidate.

## Run Setup

- Run name: `Sleepy Blue Camel`
- Configured period: `2016-01-01` to `2026-06-11`
- Actual metadata period: `2016-01-01` to `2026-03-15`
- Actual diagnostic weeks: `2016-01-04` to `2026-03-09`
- Algorithm version: `AegisGrowthAllocation-2026-06-08-candidate-b-defaults`
- Source revision: `4499311dc`
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `pre-weak-dd-threshold=0.07`
- `pre-weak-growth-target=0.12`
- `pre-weak-def-target=0.30`
- `weak-growth-target=0.10`

## Headline Comparison

| Metric | Selective 07 | Candidate B | All Defensive Off | Read |
| --- | ---: | ---: | ---: | --- |
| Net Profit | `477.602%` | `354.365%` | `580.604%` | S07 recovers `123.237` points vs B, still trails all-off |
| CAGR | `18.751%` | `15.991%` | `20.676%` | Passes `18.0%` gate |
| End Equity | `$173,280.52` | `$136,309.55` | `$204,181.21` | Strong middle result |
| Drawdown | `14.800%` | `13.700%` | `16.200%` | Passes `15.5%` gate, but worse than B |
| Sharpe | `1.021` | `0.885` | `1.102` | Passes vs B |
| Sortino | `1.108` | `0.934` | `1.224` | Passes vs B |
| PSR | `78.387%` | `65.167%` | `84.417%` | Passes vs B |
| Orders | `1525` | `1581` | `1493` | Better than B |
| Fees | `$1,834.94` | `$1,821.95` | `$1,860.48` | Within fee gate |
| Expectancy | `0.599` | `0.520` | `0.615` | Better than B |

## Diagnostics

Selective 07:

- PreWeak weeks: `63`
- NonPreWeak weeks: `469`
- Weak regime weeks: `45`
- Severe crash weeks: `0`
- PreWeak average drawdown: `9.62%`
- NonPreWeak average drawdown: `2.53%`
- PreWeak next-return average: `0.13%`
- PreWeak forward 4-week average: `0.95%`
- PreWeak forward 8-week average: `2.72%`
- PreWeak forward 12-week average: `4.55%`
- PreWeak forward 4-week win rate: `63.49%`
- PreWeak average target: `G0.1200/D0.3000/C0.5800`
- NonPreWeak average target: `G0.4872/D0.2742/C0.2386`

Candidate B comparison:

- Candidate B PreWeak weeks: `127`
- Selective 07 reduces PreWeak activations by `64` weeks, about half.
- Candidate B PreWeak forward 4-week average was `1.20%` with win rate `71.54%`; Selective 07's remaining activations are less favorable by this simple summary.

## Chart-Derived Annual Read

| Year | Selective 07 | Candidate B | All-Off | S07 - B | S07 - All-Off |
| --- | ---: | ---: | ---: | ---: | ---: |
| 2016 | `13.61%` | `9.86%` | `14.23%` | `+3.75` | `-0.62` |
| 2017 | `24.42%` | `27.02%` | `28.54%` | `-2.60` | `-4.12` |
| 2018 | `12.70%` | `7.56%` | `15.01%` | `+5.14` | `-2.30` |
| 2019 | `20.86%` | `18.81%` | `20.62%` | `+2.05` | `+0.24` |
| 2020 | `38.82%` | `31.37%` | `48.14%` | `+7.46` | `-9.32` |
| 2021 | `35.66%` | `35.56%` | `35.37%` | `+0.10` | `+0.29` |
| 2022 | `-12.55%` | `-9.91%` | `-13.96%` | `-2.64` | `+1.41` |
| 2023 | `29.14%` | `29.00%` | `35.05%` | `+0.13` | `-5.91` |
| 2024 | `28.30%` | `19.00%` | `27.03%` | `+9.30` | `+1.27` |
| 2025 | `13.60%` | `5.96%` | `16.68%` | `+7.64` | `-3.08` |
| 2026 partial | `-3.22%` | `-2.36%` | `-3.25%` | `-0.86` | `+0.03` |

Annual values are chart-derived from `Strategy Equity` series points and should be treated as supporting evidence rather than the primary metric source.

## Gate Check

| Gate | Result | Pass |
| --- | --- | --- |
| Long-span CAGR at least `18.0%` | `18.751%` | Yes |
| Long-span max drawdown no worse than `15.5%` | `14.800%` | Yes |
| 2022 no worse than `-11.9%` | `-12.55%` | No |
| Partial 2026 no worse than all-off and preferably within `0.5` points of B | Near all-off, `0.86` points worse than B | Partial |
| Sharpe and PSR improve versus B | Sharpe `1.021`; PSR `78.387%` | Yes |
| Orders no more than B + `5%` | `1525`, below B | Yes |
| Fees no more than B + `10%` | `$1,834.94`, within gate | Yes |

## Decision Read

Selective 07 proves the threshold-selectivity direction is useful: it recovers a large portion of all-off return while keeping drawdown below all-off and reducing orders versus Candidate B. It is not enough for promotion because it gives back too much of Candidate B's 2022 protection and is slightly worse than all-off in partial 2026 only by a rounding-sized margin, while still materially worse than Candidate B.

The most important result is not that `0.07` is the answer. The important result is that materially fewer PreWeak activations improved the long-span tradeoff. The next useful point is `0.09` to see whether the return/drawdown/2022 tradeoff is monotonic or whether `0.07` is already near the best compromise.

## Recommendation

Run `Selective 09` next over the same long configured span:

- `backtest-start=2016-01-01`
- `backtest-end=2026-06-11`
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `pre-weak-dd-threshold=0.09`
- `pre-weak-growth-target=0.12`
- `pre-weak-def-target=0.30`
- `weak-growth-target=0.10`

Do not run `0.12` yet unless `0.09` shows a monotonic improvement or the diagnostics indicate costly activations below `12%` drawdown. Do not promote Selective 07 from the long-span run alone.

## Verification And File Hygiene

- Confirmed Selective 07 parameters from `algorithmConfiguration.parameters` in the JSON export.
- Confirmed deployment identity and source revision from the log.
- Moved the downloaded files from `C:/Users/douya/Downloads` into `BackTestLogs`; the move removed the Downloads originals.
- Trimmed line-end whitespace in the orders CSV after normalization.
- Verified no `Sleepy Blue Camel*` files remain in Downloads.
