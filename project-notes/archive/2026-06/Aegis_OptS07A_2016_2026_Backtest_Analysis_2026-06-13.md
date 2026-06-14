---
id: AEGIS-BT-2026-06-13-OPTS07A-LONG
type: backtest-analysis
status: reviewed
date: 2026-06-13
topic: AegisGrowthAllocation
tags: [aegis, opts07a, optimization-rerun, pre-weak, backtest, validation]
related:
  - project-notes/archive/2026-06/Aegis_Selective09_2016_2026_Backtest_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_Selective07_2016_2026_Backtest_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_Recent_Optimization_Review_2026-06-13.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_OptS07A_Long_2016_2026.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_OptS07A_Long_2016_2026_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-13_222353__AegisGrowthAllocation__ManualGrid_WeakSleeve_OptS07A_Long_2016_2026_logs.txt
---

# Aegis OptS07A 2016-2026 Backtest Analysis - 2026-06-13

## Agent Summary

OptS07A (`pre-weak-dd-threshold=0.08`, `pre-weak-growth-target=0.24`, `pre-weak-def-target=0.25`) is the strongest long-span candidate so far. It beats Candidate B, Selective 07, Selective 09, and all-off on Sharpe/PSR balance, and it passes the `15.5%` drawdown gate. It still fails the 2022 protection gate, so it is not promotable from this long-span run alone.

## Run Setup

- Run name: `Adaptable Yellow Owlet`
- Configured period: `2016-01-01` to `2026-06-11`
- Actual metadata period: `2016-01-01` to `2026-03-15`
- Actual diagnostic weeks: `2016-01-04` to `2026-03-09`
- Algorithm version: `AegisGrowthAllocation-2026-06-08-candidate-b-defaults`
- Source revision: `4499311dc`
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `pre-weak-dd-threshold=0.08`
- `pre-weak-growth-target=0.24`
- `pre-weak-def-target=0.25`
- `weak-growth-target=0.10`

## Headline Comparison

| Metric | OptS07A | Selective 09 | Selective 07 | Candidate B | All-Off | Read |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Net Profit | `563.383%` | `552.974%` | `477.602%` | `354.365%` | `580.604%` | Near all-off, better than threshold-only variants |
| CAGR | `20.373%` | `20.187%` | `18.751%` | `15.991%` | `20.676%` | Strong pass |
| End Equity | `$199,014.79` | `$195,892.19` | `$173,280.52` | `$136,309.55` | `$204,181.21` | Near all-off |
| Drawdown | `15.300%` | `15.600%` | `14.800%` | `13.700%` | `16.200%` | Passes `15.5%` gate |
| Sharpe | `1.105` | `1.090` | `1.021` | `0.885` | `1.102` | Best of compared runs |
| Sortino | `1.215` | `1.197` | `1.108` | `0.934` | `1.224` | Near all-off |
| PSR | `85.128%` | `83.965%` | `78.387%` | `65.167%` | `84.417%` | Best of compared runs |
| Orders | `1510` | `1507` | `1525` | `1581` | `1493` | Better than B |
| Fees | `$1,852.54` | `$1,859.45` | `$1,834.94` | `$1,821.95` | `$1,860.48` | Within fee gate |
| Expectancy | `0.628` | `0.632` | `0.599` | `0.520` | `0.615` | Strong |

## Diagnostics

- PreWeak weeks: `42`
- NonPreWeak weeks: `490`
- Weak regime weeks: `45`
- Severe crash weeks: `0`
- PreWeak average drawdown: `10.51%`
- NonPreWeak average drawdown: `2.80%`
- PreWeak next-return average: `0.16%`
- PreWeak forward 4-week average: `0.76%`
- PreWeak forward 8-week average: `2.28%`
- PreWeak forward 12-week average: `4.27%`
- PreWeak forward 4-week win rate: `69.05%`
- PreWeak average target: `G0.2400/D0.2500/C0.5100`
- NonPreWeak average target: `G0.4856/D0.2753/C0.2391`

PreWeak activation count:

- Candidate B: `127`
- Selective 07: `63`
- Selective 09: `37`
- OptS07A: `42`
- All-off: `0`

## Chart-Derived Annual Read

| Year | OptS07A | Selective 09 | Selective 07 | Candidate B | All-Off | Opt - B | Opt - S09 | Opt - All-Off |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 2016 | `14.23%` | `14.23%` | `13.61%` | `9.86%` | `14.23%` | `+4.37` | `+0.00` | `+0.00` |
| 2017 | `28.54%` | `28.54%` | `24.42%` | `27.02%` | `28.54%` | `+1.52` | `+0.00` | `+0.00` |
| 2018 | `15.01%` | `15.01%` | `12.70%` | `7.56%` | `15.01%` | `+7.44` | `+0.00` | `+0.00` |
| 2019 | `20.63%` | `20.63%` | `20.86%` | `18.81%` | `20.62%` | `+1.82` | `+0.00` | `+0.00` |
| 2020 | `45.38%` | `48.14%` | `38.82%` | `31.37%` | `48.14%` | `+14.02` | `-2.76` | `-2.76` |
| 2021 | `36.10%` | `35.37%` | `35.66%` | `35.56%` | `35.37%` | `+0.54` | `+0.73` | `+0.73` |
| 2022 | `-13.10%` | `-13.39%` | `-12.55%` | `-9.91%` | `-13.96%` | `-3.18` | `+0.29` | `+0.86` |
| 2023 | `31.87%` | `29.40%` | `29.14%` | `29.00%` | `35.05%` | `+2.87` | `+2.46` | `-3.18` |
| 2024 | `27.03%` | `27.04%` | `28.30%` | `19.00%` | `27.03%` | `+8.03` | `-0.01` | `-0.00` |
| 2025 | `16.88%` | `16.03%` | `13.60%` | `5.96%` | `16.68%` | `+10.92` | `+0.85` | `+0.20` |
| 2026 partial | `-3.26%` | `-3.24%` | `-3.22%` | `-2.36%` | `-3.25%` | `-0.90` | `-0.02` | `-0.01` |

Annual values are chart-derived from `Strategy Equity` series points and should be treated as supporting evidence rather than the primary metric source.

## Gate Check

| Gate | Result | Pass |
| --- | --- | --- |
| Long-span CAGR at least `18.0%` | `20.373%` | Yes |
| Long-span max drawdown no worse than `15.5%` | `15.300%` | Yes |
| 2022 no worse than `-11.9%` | `-13.10%` | No |
| Partial 2026 no worse than all-off and preferably within `0.5` points of B | Near all-off, `0.90` points worse than B | Partial |
| Sharpe and PSR improve versus B | Sharpe `1.105`; PSR `85.128%` | Yes |
| Orders no more than B + `5%` | `1510`, below B | Yes |
| Fees no more than B + `10%` | `$1,852.54`, within gate | Yes |

## Decision Read

OptS07A is the best long-span result so far, and it validates the optimization insight that a higher PreWeak growth target and lower defensive target improve the return/risk tradeoff versus threshold-only tuning. However, the 2022 gate still fails by a meaningful margin: `-13.10%` versus the `-11.9%` threshold and Candidate B's `-9.91%`.

This result should be treated as the leading candidate to take into standard-window validation, not as a default candidate. Its biggest unresolved risk is whether the improved long-run profile is mostly bull/rebound capture while still failing the exact weak-market protection we wanted to preserve.

## Recommendation

Run the second optimizer rerun candidate before moving to stress windows:

- `backtest-start=2016-01-01`
- `backtest-end=2026-06-11`
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `pre-weak-dd-threshold=0.09`
- `pre-weak-growth-target=0.24`
- `pre-weak-def-target=0.25`
- `weak-growth-target=0.10`

This second candidate was the raw optimizer winner by Sharpe/PSR. If it fails 2022 or drawdown worse than OptS07A, stop long-span reruns and move OptS07A plus Candidate B plus all-off into the five standard windows.

## Verification And File Hygiene

- Confirmed OptS07A parameters from `algorithmConfiguration.parameters` in the JSON export.
- Confirmed deployment identity and source revision from the log.
- Moved the downloaded files from `C:/Users/douya/Downloads` into `BackTestLogs`; the move removed the Downloads originals.
- Trimmed line-end whitespace in the orders CSV after normalization.
- Verified no `Adaptable Yellow Owlet*` files remain in Downloads.
