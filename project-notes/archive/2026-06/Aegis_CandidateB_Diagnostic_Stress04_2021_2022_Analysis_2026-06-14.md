---
id: AEGIS-BT-2026-06-14-CANDIDATEB-DIAG-STRESS04
type: backtest-analysis
status: reviewed
date: 2026-06-14
topic: AegisGrowthAllocation
tags: [aegis, backtest, diagnostics, candidate-b, stress04]
related:
  - project-notes/archive/2026-06/Aegis_Next_Optimization_Strategy_2026-06-14.md
  - project-notes/archive/2026-06/Aegis_Weak_Sleeve_Target_Parameterization_2026-06-08.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_CandidateB_Diag_Stress04_2021_2022.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_CandidateB_Diag_Stress04_2021_2022_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-14_214207__AegisGrowthAllocation__ManualGrid_WeakSleeve_CandidateB_Diag_Stress04_2021_2022_logs.txt
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_CandidateB_DiagCompact_Stress04_2021_2022.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_CandidateB_DiagCompact_Stress04_2021_2022_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-14_215927__AegisGrowthAllocation__ManualGrid_WeakSleeve_CandidateB_DiagCompact_Stress04_2021_2022_logs.txt
---

# Aegis Candidate B Diagnostic Stress04 Analysis - 2026-06-14

## Agent Summary

Read this before requesting another diagnostic backtest. The new `[AEGIS-DIAG-WEEK]` rows worked, but the current verbose row format hit QuantConnect's 100 KB log cap before the end of the `2021-2022` stress window. The run still confirms Candidate B behavior, but it does not provide a complete weekly attribution set.

## Run Setup

- Window: `2021-01-01` to `2022-12-31`
- Parameters: Candidate B defaults with `crisis-diagnostics=true`
- PreWeak: enabled, `pre-weak-dd-threshold=0.04`, `pre-weak-growth-target=0.12`, `pre-weak-def-target=0.30`
- Weak target: `weak-growth-target=0.10`
- Weak stress overlay: disabled
- Severe crash override: disabled

## Headline Result

The diagnostic build reproduced Candidate B closely:

| Metric | Diagnostic run |
|---|---:|
| Net Profit | `19.771%` |
| CAGR | `9.458%` |
| Drawdown | `12.500%` |
| Sharpe | `0.694` |
| Sortino | `0.881` |
| PSR | `36.307%` |
| Orders | `301` |
| End Equity | `$35,931.41` |

This is effectively equivalent to the previously recorded Candidate B Stress04 baseline (`19.808%`, `12.400%`, Sharpe `0.695`, PSR `36.392%`, `301` orders). The small differences are not decision-relevant.

## Diagnostic Coverage

The new weekly diagnostics emitted correctly, but QuantConnect stopped logging with:

> You currently have a maximum of 100kb of log data per backtest, and 100kb total max per day.

Available weekly diagnostic rows:

- Count: `82`
- First row: `2021-01-04`
- Last row: `2022-07-25`
- Missing: late July 2022 through December 2022

Regime and override counts in available rows:

| State | Count |
|---|---:|
| Active Neutral | `51` |
| Active Favorable | `20` |
| Active Weak | `11` |
| PreWeak active | `17` |
| PreWeak inactive | `65` |
| Rebalance true | `61` |
| Selection change true | `59` |
| Trim-only true | `9` |

## Partial Attribution Findings

PreWeak was active from `2022-01-18` through `2022-05-09`, then the algorithm entered Weak on `2022-05-16`.

This supports the current interpretation that Candidate B's PreWeak guard is acting as a transition bridge before formal Weak regime activation:

- PreWeak average drawdown in captured rows: `6.43%`
- PreWeak max drawdown in captured rows: `8.30%`
- Weak average drawdown in captured rows: `8.60%`
- Weak max drawdown in captured rows: `10.83%`

Most frequent selected assets in captured rows:

| Sleeve | Most frequent symbols |
|---|---|
| Growth | `LLY`, `MSFT`, `COST`, `GOOGL`, `AVGO`, `NVDA` |
| Defensive | `SCHD`, `VIG`, `USMV`, `XLV`, `PG`, `DUK`, `SGOV`, `XLU`, `JNJ` |

Forced exits captured:

- `TSLA`: once
- `COST`: once

## Interpretation

The instrumentation proves the weekly attribution path is useful, but the row is too verbose for QuantConnect's log limit. It includes full candidate score lists and full weight maps every week, which is more than the cloud log cap can support over a two-year run.

Do not run another full diagnostic backtest with the current verbose format unless the purpose is only to inspect the first half of the window.

## Recommended Next Move

Change diagnostics from verbose text rows to a compact attribution mode before the next backtest.

Recommended compact row content:

- date;
- active/raw regime;
- trend/breadth/stress states;
- drawdown from high;
- sleeve override and reason;
- current and target sleeve totals;
- selected growth/defensive symbols only;
- top 3 growth scores;
- top 3 defensive scores;
- forced exits;
- rebalance/selection-change/trim flags.

Defer full candidate score lists and full target/current weights unless a separate debug parameter is enabled for short-window investigations.

## Follow-Up Implementation

The compact weekly diagnostic row was implemented after this analysis.

The `[AEGIS-DIAG-WEEK]` row now keeps:

- date, equity, active/raw regime, trend/breadth/stress states, breadth value, VIX5, severe flag;
- drawdown from high;
- sleeve override and reason;
- current and target sleeve totals;
- final sleeve target;
- rebalance, selection-change, trim, forced-exit, replacement, new-entry, reserve, and released-reserve fields;
- selected growth and defensive symbols;
- top 3 growth candidate scores and top 3 defensive candidate scores.

It removes:

- full growth candidate score lists;
- full defensive candidate score lists;
- full current weight maps;
- full target weight maps.

This should materially reduce log volume while preserving the attribution fields needed for the next Candidate B `2021-2022` diagnostic run.

## Compact Diagnostic Run

The compact diagnostic build was rerun on the same `2021-2022` stress window and completed the full weekly attribution set without hitting the QuantConnect log cap.

Headline metrics reproduced the verbose diagnostic run exactly:

| Metric | Compact diagnostic run |
|---|---:|
| Net Profit | `19.771%` |
| CAGR | `9.458%` |
| Drawdown | `12.500%` |
| Sharpe | `0.694` |
| Sortino | `0.881` |
| PSR | `36.307%` |
| Orders | `301` |

Diagnostic coverage:

| Field | Value |
|---|---:|
| Weekly rows | `104` |
| First row | `2021-01-04` |
| Last row | `2022-12-27` |
| PreWeak override weeks | `24` |
| Weak regime weeks | `26` |
| Severe crash weeks | `0` |
| Rebalance true | `77` |
| Selection change true | `74` |
| Trim-only true | `11` |

PreWeak occurred in three separate segments:

- `2022-01-18` through `2022-05-09`
- `2022-08-15` through `2022-08-22`
- `2022-11-14` through `2022-12-12`

Weak regime occurred from `2022-05-16` through `2022-08-08`, `2022-08-29` through `2022-11-07`, and again on `2022-12-19` and `2022-12-27`.

The summary diagnostic is important:

| Forward read | Value |
|---|---:|
| PreWeak avg drawdown | `7.44%` |
| NonPreWeak avg drawdown | `3.70%` |
| Weak avg drawdown | `9.63%` |
| PreWeak next-week return avg | `-0.12%` |
| NonPreWeak next-week return avg | `+0.28%` |
| PreWeak forward 4-week avg | `-0.30%` |
| PreWeak forward 8-week avg | `-1.38%` |
| PreWeak forward 12-week avg | `-1.79%` |
| PreWeak forward 4-week win rate | `40.91%` |
| PreWeak average target | `G12% / D30% / C58%` |
| NonPreWeak average target | `G38.63% / D30.75% / C30.63%` |

Most frequent selected assets:

| Sleeve | Most frequent selected symbols |
|---|---|
| Growth | `LLY`, `MSFT`, `COST`, `GOOGL`, `AVGO`, `NVDA`, `META`, `AAPL` |
| Defensive | `XLV`, `SCHD`, `SGOV`, `VIG`, `USMV`, `PG`, `DUK`, `XLU`, `JNJ` |

Most frequent top-ranked candidates:

| Candidate list | Frequent top-ranked symbols |
|---|---|
| Growth top score | `AAPL`, `LLY`, `AVGO`, `COST` |
| Defensive top score | `DUK`, `JNJ`, `SGOV`, `PG`, `SCHD` |

Forced exits were limited to `TSLA` once and `COST` once.

## Compact Run Interpretation

The compact attribution changes the next-move conclusion. Candidate B's low-growth PreWeak state was not merely leaving upside behind during this stress window. PreWeak weeks had negative average forward returns out to 12 weeks, and the 4-week forward win rate was only `40.91%`.

This supports keeping stress protection as the first priority for algorithm defaults. The next optimization should not simply raise fixed PreWeak growth exposure again. The stronger research path is to improve recovery behavior after stress improves, while preserving the defensive PreWeak posture when drawdown and signal stress are still active.

Recommended next design direction:

1. Keep Candidate B as the stress-protection baseline.
2. Add a stateful or graded recovery mechanism that only re-risks after measurable improvement, such as improving breadth, falling stress, or drawdown recovery from the local trough.
3. Validate recovery logic first on `2021-2022`, then on `2016-2026`; do not promote a long-window winner unless it keeps the 2022 stress window close to Candidate B.

## Verification

- Files were moved from Downloads into `BackTestLogs`; the Downloads originals were removed.
- JSON statistics were parsed successfully.
- Orders CSV contained `301` rows with first order `2021-01-04T15:00:00Z` and last order `2022-12-27T15:00:00Z`.
- Weekly diagnostic rows were parsed successfully from the log until the QuantConnect cap stopped logging.
- Follow-up compact diagnostics build verification: `dotnet build Tests/QuantConnect.Tests.csproj -nologo` completed successfully.
- Compact diagnostic rerun parsed successfully with `104` weekly rows and no QuantConnect log-cap truncation.

## Risks And Open Questions

- The compact run explains the full `2021-2022` stress window, but it is still one stress episode; future recovery logic must be validated on the long window and not tuned only to 2022.
- The next trading-behavior change should be explicitly reviewed before implementation because it will affect algorithm defaults and stress behavior.
