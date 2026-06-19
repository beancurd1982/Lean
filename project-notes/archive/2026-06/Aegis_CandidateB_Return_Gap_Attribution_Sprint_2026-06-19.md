---
id: AEGIS-BT-2026-06-19-CANDIDATEB-RETURN-GAP-ATTRIBUTION
type: backtest-analysis
status: reviewed
date: 2026-06-19
topic: AegisGrowthAllocation
tags: [aegis, candidate-b, candidate-c, attribution, candidate-d, diagnostics, market-on-close]
related:
  - project-notes/archive/2026-06/Aegis_Next_Optimization_Strategy_2026-06-14.md
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_SameExecution_2016_2026-01-01_Analysis_2026-06-17.md
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_SameExecution_Stress04_2021_2022_Analysis_2026-06-17.md
  - project-notes/archive/2026-06/Aegis_TagCandidateB_SameExecution_Stress04_2021_2022_Control_2026-06-17.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_TagCandidateB_MinParams_Long_2016_2026-01-01.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_SameExec_Long_2016_2026-01-01.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_TagCandidateB_SameExec_Stress04_2021_2022.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_SameExec_Stress04_2021_2022.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-14_215927__AegisGrowthAllocation__ManualGrid_WeakSleeve_CandidateB_DiagCompact_Stress04_2021_2022_logs.txt
---

# Candidate B Return-Gap Attribution Sprint - 2026-06-19

## Agent Summary

Read this before designing Candidate D. The sprint found a plausible return-gap direction, but not enough same-execution weekly evidence to implement a new candidate. Existing controls show long-window return is available by reducing defensiveness, but high-growth branches failed stress. Candidate B stress diagnostics show high-cash/PreWeak weeks were protective during 2021-2022 and had negative forward returns on average, so the next step is targeted same-execution diagnostic reruns, not a code change.

## Decision

Do not implement Candidate D yet.

The next research step is a targeted diagnostic rerun set with weekly attribution enabled for the same-execution controls:

- Candidate B long, 2016-01-01 through 2026-01-01, Market On Close.
- Candidate C long, 2016-01-01 through 2026-01-01, Market On Close.
- Candidate B stress, 2021-01-01 through 2022-12-31, Market On Close.
- Candidate C stress, 2021-01-01 through 2022-12-31, Market On Close.

Use the diagnostic rows to decide whether Candidate D should target post-stress redeployment, defensive sleeve quality, growth selection, regime timing, or churn. Do not infer missing weekly attribution from headline metrics.

## Ground Truth Controls

| Run | CAGR | Net Profit | Drawdown | Sharpe | PSR | Turnover | Fees | Orders | Win Rate | End Equity | Decision Use |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| Candidate B same-exec long | 15.342% | 317.193% | 14.0% | 0.842 | 58.542% | 3.04% | 1747.99 | 1542 | 68% | 125157.95 | Baseline long |
| Candidate C same-exec long | 15.704% | 330.470% | 14.0% | 0.859 | 60.400% | 3.01% | 1740.63 | 1535 | 69% | 129140.98 | Modest long improvement |
| Candidate B same-exec stress | 8.465% | 17.612% | 12.9% | 0.609 | 31.359% | 3.04% | 300.60 | 300 | 61% | 35283.63 | Stress baseline |
| Candidate C same-exec stress | 8.383% | 17.435% | 13.1% | 0.602 | 30.955% | 3.04% | 302.60 | 302 | 60% | 35230.42 | Fails stress gate |
| All-defensive-off long | 20.676% | 580.604% | 16.2% | 1.102 | 84.417% | 2.92% | 1860.48 | 1493 | 71% | 204181.21 | Diagnostic only |
| OptS07A long | 20.373% | 563.383% | 15.3% | 1.105 | 85.128% | 2.91% | 1852.54 | 1510 | 71% | 199014.79 | Cautionary only |
| OptS07A stress | 7.579% | 15.701% | 15.3% | 0.521 | 26.236% | 3.12% | 309.55 | 309 | 61% | 34710.22 | Fails stress |
| OptS09A long | 20.630% | 577.969% | 15.5% | 1.113 | 85.639% | 2.90% | 1862.35 | 1508 | 71% | 203390.58 | Cautionary only |
| OptS09A stress | 7.482% | 15.494% | 15.5% | 0.512 | 25.756% | 3.14% | 315.55 | 315 | 61% | 34648.22 | Fails stress |

Candidate C clears the long-window comparison but fails the same-execution stress gate versus Candidate B. The all-defensive-off and high-growth controls show where return is available, but they also show the stress cost of taking it bluntly.

## Return-Gap Map

| Lever | Evidence | Read |
|---|---|---|
| Post-stress redeployment / cash drag | Candidate B diagnostic stress rows: target cash >=50% occurred in 50 of 104 weeks and had average forward 4/8/12-week returns of -0.34% / -0.84% / -1.52%. Cash target <30% weeks had +1.76% / +3.35% / +4.95%. Recovery-like events with high cash had average forward 4-week return -0.38%, while non-high-cash events averaged +0.77%. | Directionally important, but 2021-2022 alone does not prove a long-window Candidate D. Needs same-exec long diagnostics. |
| Defensive sleeve quality | PreWeak rows selected SGOV 14 times, PG 11, SCHD 11, XLU 11, DUK 10, XLV 9, VIG 3, USMV 2, JNJ 1. Weak rows selected SGOV 24 times. Defensive ranking is regime-insensitive and uses 126-day return, inverse volatility, and inverse drawdown. | Worth auditing after diagnostics show whether defensive selections caused missed return or preserved stress. |
| Growth selection quality | PreWeak rows concentrated in LLY 19, COST 10, AVGO 9, AAPL 8, with limited entries. Favorable rows held more growth and had stronger forward returns. | Could matter, but current evidence mainly says exposure/regime state dominates selection. |
| Regime timing | PreWeak override active for 24 weeks; base-regime active for 80 weeks. Active Weak had negative forward 4/8/12-week reads, while Active Favorable had positive reads. | Supports a graded recovery/timing investigation, not fixed higher PreWeak growth. |
| Replacement/churn | Candidate B diagnostic stress rows had 8 optimization replacements and 38 new entries, concentrated outside high-cash/PreWeak weeks. Candidate C same-exec long had fewer order rows than B, while stress had 2 more. | Low priority unless diagnostics show bad post-transition replacement timing. |

## Stress-Edge Map

Candidate B's stress edge appears to come from a mix of high cash during weak/breadth-poor periods, conservative PreWeak targets, and limited churn during defensive states.

Key Candidate B diagnostic stress reads:

| Slice | Weeks | Avg Week | Fwd 4w | Fwd 8w | Fwd 12w | Target Cash | Current Cash | Replacements | New Entries |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| PreWeak override | 24 | -0.06% | -0.30% | -1.37% | -1.78% | 58.14% | 56.52% | 0 | 6 |
| Active Weak | 26 | -0.19% | -0.38% | -0.40% | -1.25% | 51.71% | 53.12% | 0 | 2 |
| Target cash >=50% | 50 | -0.13% | -0.34% | -0.84% | -1.52% | 54.80% | 54.75% | 0 | 8 |
| Breadth Weak | 37 | -0.14% | -0.07% | -0.68% | -1.32% | 53.61% | 54.38% | 0 | 4 |
| Base regime | 80 | 0.26% | 1.08% | 2.18% | 3.28% | 32.10% | 34.02% | 8 | 32 |
| Active Favorable | 20 | 1.14% | 1.35% | 3.04% | 4.69% | 16.92% | 21.13% | 4 | 21 |

This argues against a simple "increase growth" Candidate D. High-cash defensive states were active in the exact periods where forward returns were weak. The candidate design problem is narrower: identify recovery or cleaner-risk windows where re-risking would not erase this stress behavior.

## Source-Risk Lever Ranking

| Rank | Lever | Affected Code Path | Decision Value | Implementation Risk | Validation Cost | Required Evidence | User Confirmation Before Implementation |
|---:|---|---|---|---|---|---|---|
| 1 | Diagnostic reruns before Candidate D | `crisis-diagnostics=true`, BackTestLogs artifacts | Very high | Low | Medium | Weekly rows for B/C long and stress with same execution | No, if backtest-only |
| 2 | Post-stress redeployment / reserve release | `PortfolioManager.BuildPlan`, `StrategyConfig.ReserveReleaseRateByRegime`, PreWeak recovery state | High | Medium | Medium | Cash remains high after recovery signals across long and stress diagnostics | Yes if defaults or live behavior change |
| 3 | Graded PreWeak recovery/timing | `AegisGrowthAllocation.Recovery.cs`, PreWeak sleeve targets | High | Medium-high | High | At least two distinct regimes where recovery signals precede positive forward returns without stress degradation | Yes |
| 4 | Defensive sleeve quality | `StockSelectionModel.SelectDefensiveCandidates`, defensive universe/weights | Medium | Medium | Medium | Defensive symbols underperform cash/alternatives during defensive states without reducing drawdown | Yes if universe/defaults change |
| 5 | Growth selection quality | `StockSelectionModel.SelectGrowthCandidates`, holding counts | Medium | Medium | Medium | Selected growth names lag top candidates after recovery events | Yes if selection/defaults change |
| 6 | Replacement/churn behavior | `PortfolioManager.BuildPlan`, replacement gap/hold bonus | Low-medium | Low-medium | Medium | Churn clusters before drawdown or misses rebound | Yes if defaults change |

## Candidate D Gate

No Candidate D hypothesis is approved from this sprint.

The leading hypothesis family is post-stress redeployment / recovery-sensitive cash release, but the evidence is incomplete because:

- Candidate C same-execution long and stress logs have zero `[AEGIS-DIAG-WEEK]` rows.
- Candidate B same-execution stress log also has zero `[AEGIS-DIAG-WEEK]` rows.
- The only complete weekly attribution artifact is Candidate B diagnostic stress from 2021-2022.
- 2021-2022 may diagnose but cannot alone choose symbols, weights, or thresholds.

Candidate D can be proposed only after diagnostic reruns show the same lever across at least two regimes/windows and plausibly explain at least `0.3%` CAGR or `3` long-window net-profit points while preserving Candidate B stress behavior.

## Required Next Run Sheet

Run the same-execution controls with weekly diagnostics enabled and enough log capacity or compact export to preserve all rows:

| Run | Required Parameters | Purpose |
|---|---|---|
| Candidate B long diagnostic | `backtest-start=2016-01-01`, `backtest-end=2026-01-01`, `crisis-diagnostics=true`, Candidate B defaults | Return-gap baseline |
| Candidate C long diagnostic | same dates, `preweak-recovery-enabled=true`, `crisis-diagnostics=true` | Attribute Candidate C's modest long edge |
| Candidate B stress diagnostic | `backtest-start=2021-01-01`, `backtest-end=2022-12-31`, `crisis-diagnostics=true`, Candidate B defaults | Same-execution stress baseline with weekly rows |
| Candidate C stress diagnostic | same dates, `preweak-recovery-enabled=true`, `crisis-diagnostics=true` | Attribute stress miss |

If QuantConnect log limits truncate long diagnostics, split the long window into bounded segments or export compact CSV-style rows. Do not proceed to Candidate D selection from truncated diagnostics.

## Verification

- `check_log_index.py` on `BackTestLogs/log-index.csv`: 59 rows, 0 unreviewed, 0 errors, 0 warnings.
- `compare_aegis_backtests.py` Candidate B vs Candidate C same-exec stress: fails stress gate versus Candidate B.
- `compare_aegis_backtests.py` Candidate B vs Candidate C same-exec long: Candidate C passes measured long comparison.
- Source audit was read-only. No trading code, live-state, Object Store, order-handling, deployment identity, or parameter-default changes were made.

## Risks And Open Questions

- Existing same-execution controls are strong enough for headline comparison but not enough for weekly attribution.
- All-defensive-off and high-growth controls should remain diagnostic/cautionary, not templates.
- Candidate C's small long improvement may come from recovery behavior, reduced churn, timing, or path noise; current artifacts cannot separate those mechanisms.
- Any future Candidate D implementation that touches defaults, persistence, order handling, reserve release, or live behavior requires explicit user confirmation before implementation.
