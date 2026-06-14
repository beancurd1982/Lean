---
id: AEGIS-BT-2026-06-13-OPTS09A-STRESS04
type: backtest-analysis
status: reviewed
date: 2026-06-13
topic: AegisGrowthAllocation
tags: [aegis, opts09a, stress-window, pre-weak, backtest, validation]
related:
  - project-notes/archive/2026-06/Aegis_OptS09A_2016_2026_Backtest_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_OptS07A_2016_2026_Backtest_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_Recent_Optimization_Review_2026-06-13.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_OptS09A_Stress04_2021_2022.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_OptS09A_Stress04_2021_2022_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-13_225102__AegisGrowthAllocation__ManualGrid_WeakSleeve_OptS09A_Stress04_2021_2022_logs.txt
---

# Aegis OptS09A Stress04 2021-2022 Backtest Analysis - 2026-06-13

## Agent Summary

OptS09A fails the 2021-2022 stress-window validation versus Candidate B. It still beats all-defensive-off, but it gives up too much of Candidate B's protection: lower return, higher drawdown, weaker Sharpe/PSR, and higher order/fee load. This result downgrades OptS09A from "best headline finalist" to "not preferred unless a later window reveals a compensating advantage."

## Run Setup

- Run name: `Virtual Blue Cat`
- Configured period: `2021-01-01` to `2022-12-31`
- Actual metadata period: `2021-01-01` to `2022-12-31`
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

## Stress04 Comparison

| Variant | Net Profit | CAGR | Drawdown | Sharpe | Sortino | PSR | Orders | Fees | Read |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| OptS09A | `15.494%` | `7.482%` | `15.500%` | `0.512` | `0.647` | `25.756%` | `315` | `$315.55` | Fails versus B |
| Candidate B | `19.808%` | `9.475%` | `12.400%` | `0.695` | `0.882` | `36.392%` | `301` | `$301.59` | Best current weak-sleeve baseline |
| Candidate A | `19.534%` | `9.349%` | `12.600%` | `0.684` | `0.869` | `35.745%` | `305` | `$305.59` | Slightly behind B |
| All-off control | `14.186%` | `6.871%` | `16.500%` | `0.454` | `0.555` | `22.671%` | `303` | `$303.69` | OptS09A beats all-off but not enough |
| OptStress04 | `18.200%` | `8.736%` | `13.600%` | `0.612` | `0.777` | `31.225%` | `297` | `$297.57` | Older stronger stress result |
| RSG12 | `16.571%` | `7.983%` | `13.600%` | `0.549` | `0.694` | `27.651%` | `293` | `$293.55` | Also better drawdown than OptS09A |

## Diagnostics

- Diagnostic weeks: `104`
- PreWeak weeks: `18`
- NonPreWeak weeks: `86`
- Weak regime weeks: `26`
- Severe crash weeks: `0`
- PreWeak average drawdown: `10.97%`
- NonPreWeak average drawdown: `4.94%`
- PreWeak next-return average: `-0.05%`
- PreWeak forward 4-week average: `0.39%`
- PreWeak forward 8-week average: `-1.19%`
- PreWeak forward 12-week average: `-1.87%`
- PreWeak forward 4-week win rate: `62.50%`
- PreWeak average target: `G0.2400/D0.2500/C0.5100`
- NonPreWeak average target: `G0.3907/D0.3070/C0.3023`

The negative 8-week and 12-week forward returns after PreWeak activation show that the signal did catch some weak-market stress, but the more growth-heavy `0.24/0.25` sleeve was not defensive enough for this specific validation window.

## Gate Check

| Gate | Result | Pass |
| --- | --- | --- |
| Beat Candidate B on 2021-2022 net return | `15.494%` vs B `19.808%` | No |
| Improve drawdown versus Candidate B | `15.500%` vs B `12.400%` | No |
| Improve Sharpe/PSR versus Candidate B | `0.512` / `25.756%` vs B `0.695` / `36.392%` | No |
| Avoid excess orders/fees versus Candidate B | `315` / `$315.55` vs B `301` / `$301.59` | No |
| Beat all-off control | Better return and drawdown than all-off | Yes |

## Decision Read

OptS09A should not be the preferred finalist after this stress-window result. Its long-span headline advantage is largely rebound/bull-market capture, while the 2021-2022 validation shows weaker protection than Candidate B and even weaker stress quality than older OptStress04/RSG12 references.

This does not invalidate the whole optimization insight. It says the raw winner (`0.09/0.24/0.25`) is too aggressive for the weak-market gate. The next useful test is OptS07A on the same `2021-2022` stress window because it was slightly safer in the long-span run (`15.3%` drawdown and better 2022 read than OptS09A).

## Recommended Next Move

Run OptS07A on the same stress window:

- `backtest-start=2021-01-01`
- `backtest-end=2022-12-31`
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `pre-weak-dd-threshold=0.08`
- `pre-weak-growth-target=0.24`
- `pre-weak-def-target=0.25`
- `weak-growth-target=0.10`

If OptS07A also fails materially versus Candidate B, stop validating the high-growth PreWeak finalists and return to either Candidate B or a narrower optimization around more defensive PreWeak targets.

## Analysis Notes

- The downloaded files were normalized into `BackTestLogs` and removed from Downloads.
- PowerShell `ConvertFrom-Json` handled QC chart point fields awkwardly because chart points store data inside a `value` array; annual split extraction was skipped because the decision did not depend on it.
