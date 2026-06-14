---
id: AEGIS-BT-2026-06-14-OPTS07A-STRESS04
type: backtest-analysis
status: reviewed
date: 2026-06-14
topic: AegisGrowthAllocation
tags: [aegis, opts07a, stress-window, pre-weak, backtest, validation]
related:
  - project-notes/archive/2026-06/Aegis_OptS07A_2016_2026_Backtest_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_OptS09A_Stress04_2021_2022_Backtest_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_Recent_Optimization_Review_2026-06-13.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_OptS07A_Stress04_2021_2022.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_OptS07A_Stress04_2021_2022_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-14_195824__AegisGrowthAllocation__ManualGrid_WeakSleeve_OptS07A_Stress04_2021_2022_logs.txt
---

# Aegis OptS07A Stress04 2021-2022 Backtest Analysis - 2026-06-14

## Agent Summary

OptS07A slightly improves on OptS09A in the 2021-2022 stress window, but still fails materially versus Candidate B. The high-growth PreWeak finalist branch should be stopped: both OptS07A and OptS09A recover long-span return by sacrificing the exact 2021-2022 protection Candidate B preserved.

## Run Setup

- Run name: `Alert Red Cow`
- Configured period: `2021-01-01` to `2022-12-31`
- Actual metadata period: `2021-01-01` to `2022-12-31`
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

## Stress04 Comparison

| Variant | Net Profit | CAGR | Drawdown | Sharpe | Sortino | PSR | Orders | Fees | Read |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| OptS07A | `15.701%` | `7.579%` | `15.300%` | `0.521` | `0.659` | `26.236%` | `309` | `$309.55` | Slightly better than OptS09A; fails B |
| OptS09A | `15.494%` | `7.482%` | `15.500%` | `0.512` | `0.647` | `25.756%` | `315` | `$315.55` | Fails B |
| Candidate B | `19.808%` | `9.475%` | `12.400%` | `0.695` | `0.882` | `36.392%` | `301` | `$301.59` | Best current weak-sleeve baseline |
| Candidate A | `19.534%` | `9.349%` | `12.600%` | `0.684` | `0.869` | `35.745%` | `305` | `$305.59` | Slightly behind B |
| All-off control | `14.186%` | `6.871%` | `16.500%` | `0.454` | `0.555` | `22.671%` | `303` | `$303.69` | OptS07A beats all-off |
| OptStress04 | `18.200%` | `8.736%` | `13.600%` | `0.612` | `0.777` | `31.225%` | `297` | `$297.57` | Older stronger stress result |

## Diagnostics

- Diagnostic weeks: `104`
- PreWeak weeks: `20`
- NonPreWeak weeks: `84`
- Weak regime weeks: `26`
- Severe crash weeks: `0`
- PreWeak average drawdown: `10.53%`
- NonPreWeak average drawdown: `4.80%`
- PreWeak next-return average: `-0.05%`
- PreWeak forward 4-week average: `0.29%`
- PreWeak forward 8-week average: `-1.25%`
- PreWeak forward 12-week average: `-2.26%`
- PreWeak forward 4-week win rate: `61.11%`
- PreWeak average target: `G0.2400/D0.2500/C0.5100`
- NonPreWeak average target: `G0.3893/D0.3071/C0.3036`

The PreWeak signal identified real weakness, but the selected sleeve was not defensive enough. Forward 8-week and 12-week returns after PreWeak activation were negative, yet the portfolio still carried `24%` growth exposure during PreWeak and suffered materially higher drawdown than Candidate B.

## Gate Check

| Gate | Result | Pass |
| --- | --- | --- |
| Beat Candidate B on 2021-2022 net return | `15.701%` vs B `19.808%` | No |
| Improve drawdown versus Candidate B | `15.300%` vs B `12.400%` | No |
| Improve Sharpe/PSR versus Candidate B | `0.521` / `26.236%` vs B `0.695` / `36.392%` | No |
| Avoid excess orders/fees versus Candidate B | `309` / `$309.55` vs B `301` / `$301.59` | No |
| Beat all-off control | Better return and drawdown than all-off | Yes |

## Decision Read

Stop the high-growth PreWeak finalist branch. OptS07A and OptS09A were attractive in the long-span headline metrics, but both fail the 2021-2022 stress gate. The results indicate the optimization overfit toward bull/rebound capture and did not preserve Candidate B's defensive value.

The better next move is not to run more broad long-span backtests. If we continue optimizing, use a narrower grid around more defensive PreWeak targets and Candidate B-like stress behavior, or pause optimization and keep Candidate B as the current parameter baseline.

## Recommended Next Move

Do not run another single high-growth finalist. Choose between:

1. Conservative path: keep Candidate B as the current baseline and stop this branch.
2. Targeted optimization path: run a small paid optimization focused only on stress-window improvement for `2021-2022`, with growth target capped below `0.24`.

Suggested bounded optimization if continuing:

- Window: `2021-01-01` to `2022-12-31`
- Objective: maximize Sharpe or PSR, with drawdown and net return manually checked against Candidate B
- `pre-weak-dd-threshold`: `0.06` to `0.09`, step `0.01`
- `pre-weak-growth-target`: `0.12` to `0.20`, step `0.04`
- `pre-weak-def-target`: `0.25` to `0.35`, step `0.05`
- Total combinations: `4 x 3 x 3 = 36`

This is below the preferred `50-150` range, but it is intentionally narrow because the broad grid already showed that `0.24` growth is too aggressive for the stress gate.

## Analysis Notes

- The downloaded files were normalized into `BackTestLogs` and removed from Downloads.
- No code or live-trading behavior was changed.
