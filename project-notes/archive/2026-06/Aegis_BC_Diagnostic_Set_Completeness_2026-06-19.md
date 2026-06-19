---
id: AEGIS-BT-2026-06-19-BC-DIAGNOSTIC-COMPLETENESS
type: backtest-analysis
status: reviewed
date: 2026-06-19
topic: AegisGrowthAllocation
tags: [aegis, candidate-b, candidate-c, diagnostics, attribution, market-on-close]
related:
  - project-notes/archive/2026-06/Aegis_CandidateB_Return_Gap_Attribution_Sprint_2026-06-19.md
  - project-notes/archive/2026-06/Aegis_TagCandidateB_SameExecution_Stress04_2021_2022_Control_2026-06-17.md
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_SameExecution_Stress04_2021_2022_Analysis_2026-06-17.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateB_DiagSameExec_Stress04_2021_2022.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateB_DiagSameExec_Long_2016_2026-01-01.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_DiagSameExec_Stress04_2021_2022.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_DiagSameExec_Long_2016_2026-01-01.json
---

# Aegis B/C Diagnostic Set Completeness - 2026-06-19

## Agent Summary

Read this before choosing Candidate D. The four requested diagnostic runs were normalized and reviewed for completeness. Headline metrics are usable and confirm the prior B/C pattern, but the weekly attribution set is not complete enough to choose a new lever: Candidate B emitted only summary diagnostics, Candidate C stress emitted full weekly rows, and Candidate C long truncated around 2018-10 due to the QuantConnect log cap.

## Decision

Do not design or implement Candidate D from this diagnostic set.

The next step is a corrected diagnostic rerun set:

- Rerun Candidate B stress and long using the current source that contains weekly diagnostics, with `preweak-recovery-enabled=false` or omitted and `crisis-diagnostics=true`.
- Rerun Candidate C long in smaller segments, with `preweak-recovery-enabled=true` and `crisis-diagnostics=true`, so weekly rows do not truncate.
- Candidate C stress does not need rerun unless a cleaner paired rerun is desired; it produced full weekly rows.

## Headline Metrics

| Run | CAGR | Net Profit | Drawdown | Sharpe | PSR | Turnover | Fees | Orders | Win Rate | End Equity | Read |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| Candidate B stress diagnostic | 8.465% | 17.613% | 12.900% | 0.609 | 31.356% | 3.03% | 299.60 | 299 | 60% | 35283.75 | Stress baseline |
| Candidate C stress diagnostic | 8.382% | 17.432% | 13.100% | 0.602 | 30.942% | 3.04% | 301.60 | 301 | 60% | 35229.47 | Still misses B stress gate |
| Candidate B long diagnostic | 15.391% | 318.970% | 14.000% | 0.845 | 58.948% | 3.04% | 1740.58 | 1536 | 69% | 125691.05 | Long baseline |
| Candidate C long diagnostic | 15.774% | 333.102% | 14.000% | 0.863 | 60.847% | 3.00% | 1743.45 | 1539 | 69% | 129930.65 | Improves long return, with slightly more fees/orders |

Candidate C still has the same tradeoff: better long-window performance, but worse 2021-2022 stress metrics versus Candidate B.

## Diagnostic Completeness

| Run | Weekly Rows | Coverage | Completeness Read |
|---|---:|---|---|
| Candidate B stress diagnostic | 0 | Summary only: 104 weeks, 2021-01-04 through 2022-12-27 | Not enough for weekly B/C attribution |
| Candidate B long diagnostic | 0 | Summary only: 522 weeks, 2016-01-04 through 2025-12-29 | Not enough for weekly B/C attribution |
| Candidate C stress diagnostic | 104 | 2021-01-04 through 2022-12-27 | Complete stress weekly attribution |
| Candidate C long diagnostic | 147 | 2016-01-04 through 2018-10-22 | Truncated by log cap; not decision-grade for long attribution |

Candidate B was likely run from the Candidate B tag/source that emits `[AEGIS-DIAG-SUMMARY]` but not `[AEGIS-DIAG-WEEK]`. For weekly attribution, run Candidate B behavior from the current source with recovery disabled; current source includes weekly diagnostics and default-off recovery behavior.

## Candidate B Summary Diagnostics

Candidate B stress summary:

- `Weeks=104`
- `PreWeakWeeks=24`
- `WeakRegimeWeeks=26`
- `PreWeakFwd4Avg=-0.0024`
- `PreWeakFwd8Avg=-0.0124`
- `PreWeakFwd12Avg=-0.0154`
- `PreWeakFwd4WinRate=0.5455`
- `PreWeakAvgTarget=G0.1200/D0.3000/C0.5800`

Candidate B long summary:

- `Weeks=522`
- `PreWeakWeeks=136`
- `WeakRegimeWeeks=45`
- `PreWeakFwd4Avg=0.0108`
- `PreWeakFwd8Avg=0.0247`
- `PreWeakFwd12Avg=0.0404`
- `PreWeakFwd4WinRate=0.7500`
- `PreWeakAvgTarget=G0.1200/D0.3000/C0.5800`

This is important: Candidate B PreWeak exposure looks bad in 2021-2022 stress but positive over the long window. That argues for conditional recovery logic, but weekly rows are still needed to avoid overfitting.

## Candidate C Stress Weekly Diagnostics

Candidate C stress produced 104 weekly rows:

| Slice | Weeks | Avg Week | Fwd 4w | Fwd 8w | Fwd 12w | Target Cash | Replacements | New Entries |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Normal PreWeak | 23 | -0.04% | -0.12% | -1.17% | -1.54% | 58.24% | 0 | 6 |
| Base regime | 80 | 0.24% | 0.91% | 1.83% | 2.69% | 32.09% | 8 | 33 |
| Cash target >=50% | 50 | -0.13% | -0.32% | -0.80% | -1.42% | 54.81% | 0 | 8 |
| Cash target <30% | 52 | 0.44% | 1.47% | 2.79% | 4.07% | 21.94% | 7 | 27 |
| Active Weak | 26 | -0.19% | -0.36% | -0.37% | -1.19% | 51.80% | 0 | 2 |
| Active Favorable | 20 | 1.07% | 1.15% | 2.50% | 3.65% | 16.84% | 4 | 21 |

Candidate C recovery activated only once in the stress window. The summary reports:

- `RecoveryPreWeakWeeks=1`
- `RecoveryPreWeakFwd4Avg=-0.0331`
- `RecoveryPreWeakFwd8Avg=-0.0386`
- `RecoveryPreWeakFwd12Avg=-0.0340`
- `RecoveryPreWeakFwd4WinRate=0.0000`
- `RecoveryPreWeakAvgTarget=G0.1600/D0.3000/C0.5400`

This explains why Candidate C did not improve the stress run: the recovery activation was rare and occurred in an unfavorable forward-return window.

## Candidate C Long Diagnostic Caveat

Candidate C long emitted only 147 weekly rows, ending at 2018-10-22. The log is not decision-grade for the full 2016-2026 attribution.

The partial rows still show that recovery can activate outside 2021-2022, but this cannot be used to select Candidate D because the later years are missing.

## Required Reruns

Use current source for both B and C so weekly `[AEGIS-DIAG-WEEK]` rows are available.

| Run | Source | Parameters | Purpose |
|---|---|---|---|
| Candidate B stress weekly diagnostic | Current source | `backtest-start=2021-01-01`, `backtest-end=2022-12-31`, `crisis-diagnostics=true`; omit `preweak-recovery-enabled` or set `false` | Paired weekly stress attribution |
| Candidate B long segmented diagnostics | Current source | Split into yearly or two-year windows, `crisis-diagnostics=true`; omit `preweak-recovery-enabled` or set `false` | Full long weekly baseline without log truncation |
| Candidate C long segmented diagnostics | Current source | Same segments, `preweak-recovery-enabled=true`, `crisis-diagnostics=true` | Full long weekly recovery attribution |

Candidate C stress can be kept as the current complete weekly stress artifact unless the team wants every run generated in one paired rerun batch.

## Verification

- `check_log_index.py`: 63 rows, 4 unreviewed before this note, 0 errors, 0 warnings.
- Candidate B/C diagnostic stress comparison: Candidate C fails stress gate versus Candidate B on drawdown, net profit, orders, profit factor, PSR, and Sharpe.
- Candidate B/C diagnostic long comparison: Candidate C improves CAGR, net profit, Sharpe, PSR, turnover, and end equity, but has slightly more fees and orders.
- No trading source, live-state, Object Store, order-handling, deployment identity, or parameter-default changes were made.

## Risks And Open Questions

- Candidate B weekly attribution is missing from this run set.
- Candidate C long attribution is truncated and should not drive a Candidate D hypothesis.
- Candidate C stress recovery activation was negative, but one activation is too small to generalize.
- Candidate D should remain blocked until paired weekly rows are available across stress and long windows.
