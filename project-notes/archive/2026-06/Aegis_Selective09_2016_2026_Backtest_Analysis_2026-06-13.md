---
id: AEGIS-BT-2026-06-13-SELECTIVE09-LONG
type: backtest-analysis
status: reviewed
date: 2026-06-13
topic: AegisGrowthAllocation
tags: [aegis, selective09, pre-weak, candidate-b, backtest, validation]
related:
  - project-notes/archive/2026-06/Aegis_Recent_Optimization_Review_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_Selective07_2016_2026_Backtest_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_AllDefensiveOff_2016_2026_Control_Analysis_2026-06-13.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_Selective09_Long_2016_2026.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_Selective09_Long_2016_2026_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-13_214807__AegisGrowthAllocation__ManualGrid_WeakSleeve_Selective09_Long_2016_2026_logs.txt
---

# Aegis Selective 09 2016-2026 Backtest Analysis - 2026-06-13

## Agent Summary

Selective 09 nearly converges to all-defensive-off behavior over the long span. It delivers excellent return and risk-adjusted metrics, but it fails the pre-registered drawdown gate by `0.1` point and gives up too much of Candidate B's 2022 downside protection. This result confirms that threshold-only tuning has a clear tradeoff curve, but it does not identify a promotable threshold.

## Run Setup

- Run name: `Geeky Red Mule`
- Configured period: `2016-01-01` to `2026-06-11`
- Actual metadata period: `2016-01-01` to `2026-03-15`
- Actual diagnostic weeks: `2016-01-04` to `2026-03-09`
- Algorithm version: `AegisGrowthAllocation-2026-06-08-candidate-b-defaults`
- Source revision: `4499311dc`
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `pre-weak-dd-threshold=0.09`
- `pre-weak-growth-target=0.12`
- `pre-weak-def-target=0.30`
- `weak-growth-target=0.10`

## Headline Comparison

| Metric | Selective 09 | Selective 07 | Candidate B | All Defensive Off | Read |
| --- | ---: | ---: | ---: | ---: | --- |
| Net Profit | `552.974%` | `477.602%` | `354.365%` | `580.604%` | S09 is close to all-off |
| CAGR | `20.187%` | `18.751%` | `15.991%` | `20.676%` | Strong pass |
| End Equity | `$195,892.19` | `$173,280.52` | `$136,309.55` | `$204,181.21` | Near all-off |
| Drawdown | `15.600%` | `14.800%` | `13.700%` | `16.200%` | Misses `15.5%` gate by `0.1` |
| Sharpe | `1.090` | `1.021` | `0.885` | `1.102` | Near all-off |
| Sortino | `1.197` | `1.108` | `0.934` | `1.224` | Near all-off |
| PSR | `83.965%` | `78.387%` | `65.167%` | `84.417%` | Near all-off |
| Orders | `1507` | `1525` | `1581` | `1493` | Better than B |
| Fees | `$1,859.45` | `$1,834.94` | `$1,821.95` | `$1,860.48` | Within fee gate |
| Expectancy | `0.632` | `0.599` | `0.520` | `0.615` | Best of compared runs |

## Diagnostics

Selective 09:

- PreWeak weeks: `37`
- NonPreWeak weeks: `495`
- Weak regime weeks: `45`
- Severe crash weeks: `0`
- PreWeak average drawdown: `11.58%`
- NonPreWeak average drawdown: `2.88%`
- PreWeak next-return average: `0.03%`
- PreWeak forward 4-week average: `0.77%`
- PreWeak forward 8-week average: `1.92%`
- PreWeak forward 12-week average: `3.90%`
- PreWeak forward 4-week win rate: `59.46%`
- PreWeak average target: `G0.1200/D0.3000/C0.5800`
- NonPreWeak average target: `G0.4853/D0.2756/C0.2392`

PreWeak activation count:

- Candidate B: `127`
- Selective 07: `63`
- Selective 09: `37`
- All-off: `0`

## Chart-Derived Annual Read

| Year | Selective 09 | Selective 07 | Candidate B | All-Off | S09 - B | S09 - S07 | S09 - All-Off |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 2016 | `14.23%` | `13.61%` | `9.86%` | `14.23%` | `+4.37` | `+0.62` | `+0.00` |
| 2017 | `28.54%` | `24.42%` | `27.02%` | `28.54%` | `+1.52` | `+4.12` | `+0.00` |
| 2018 | `15.01%` | `12.70%` | `7.56%` | `15.01%` | `+7.44` | `+2.30` | `+0.00` |
| 2019 | `20.63%` | `20.86%` | `18.81%` | `20.62%` | `+1.82` | `-0.23` | `+0.00` |
| 2020 | `48.14%` | `38.82%` | `31.37%` | `48.14%` | `+16.77` | `+9.32` | `+0.00` |
| 2021 | `35.37%` | `35.66%` | `35.56%` | `35.37%` | `-0.19` | `-0.29` | `-0.00` |
| 2022 | `-13.39%` | `-12.55%` | `-9.91%` | `-13.96%` | `-3.47` | `-0.83` | `+0.57` |
| 2023 | `29.40%` | `29.14%` | `29.00%` | `35.05%` | `+0.40` | `+0.27` | `-5.64` |
| 2024 | `27.04%` | `28.30%` | `19.00%` | `27.03%` | `+8.04` | `-1.26` | `+0.01` |
| 2025 | `16.03%` | `13.60%` | `5.96%` | `16.68%` | `+10.07` | `+2.43` | `-0.65` |
| 2026 partial | `-3.24%` | `-3.22%` | `-2.36%` | `-3.25%` | `-0.88` | `-0.02` | `+0.01` |

Annual values are chart-derived from `Strategy Equity` series points and should be treated as supporting evidence rather than the primary metric source.

## Gate Check

| Gate | Result | Pass |
| --- | --- | --- |
| Long-span CAGR at least `18.0%` | `20.187%` | Yes |
| Long-span max drawdown no worse than `15.5%` | `15.600%` | No, misses by `0.1` |
| 2022 no worse than `-11.9%` | `-13.39%` | No |
| Partial 2026 no worse than all-off and preferably within `0.5` points of B | Near all-off, `0.88` points worse than B | Partial |
| Sharpe and PSR improve versus B | Sharpe `1.090`; PSR `83.965%` | Yes |
| Orders no more than B + `5%` | `1507`, below B | Yes |
| Fees no more than B + `10%` | `$1,859.45`, within gate | Yes |

## Decision Read

Selective 09 strengthens the case that fewer PreWeak activations recover return, but it also shows the threshold-only path is converging toward all-off. The jump from `0.07` to `0.09` improves CAGR, Sharpe, PSR, orders, and ending equity, but worsens drawdown and 2022 protection. Since `0.09` is already near all-off in many annual periods, `0.12` is unlikely to be useful as a promotion candidate.

The useful conclusion is that a single threshold is probably too blunt. We need either:

- a multi-parameter optimization that changes both activation frequency and PreWeak sleeve targets, or
- more diagnostic attribution before creating new trigger logic.

Given the user preference that QuantConnect optimization is useful when a justified grid can cover `50-150` combinations, this is now a reasonable point to use optimization rather than guessing one manual backtest at a time.

## Recommendation

Do not promote Selective 09. Do not run `0.12` next.

Next useful move: run a QuantConnect optimization over the same long configured span with this `80`-combination grid:

| Parameter | Min | Max | Step | Values |
| --- | ---: | ---: | ---: | ---: |
| `pre-weak-dd-threshold` | `0.05` | `0.09` | `0.01` | `5` |
| `pre-weak-growth-target` | `0.12` | `0.24` | `0.04` | `4` |
| `pre-weak-def-target` | `0.25` | `0.40` | `0.05` | `4` |

Keep fixed:

- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `weak-growth-target=0.10`
- all Candidate B baseline regime, stress, replacement, stability, and tolerance parameters

Use Sharpe or PSR as the optimizer objective, then manually filter candidates against drawdown, 2022, partial 2026, orders, fees, and standard-window validation. Optimization output should nominate candidates only; rerun any selected candidate as a normal diagnostic backtest before promotion.

## Verification And File Hygiene

- Confirmed Selective 09 parameters from `algorithmConfiguration.parameters` in the JSON export.
- Confirmed deployment identity and source revision from the log.
- Moved the downloaded files from `C:/Users/douya/Downloads` into `BackTestLogs`; the move removed the Downloads originals.
- Trimmed line-end whitespace in the orders CSV after normalization.
- Verified no `Geeky Red Mule*` files remain in Downloads.
