---
id: AEGIS-BT-2026-06-13-SHORT2026-DD04-DD05
type: backtest-analysis
status: reviewed
date: 2026-06-13
topic: AegisGrowthAllocation
tags: [aegis, short-window, sensitivity, pre-weak, backtest]
related:
  - project-notes/archive/2026-06/Aegis_CandidateB_2016_2026_Backtest_Analysis_2026-06-12.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_Short2026_CandidateB_DD04_2026-01_2026-03.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_Short2026_CandidateB_DD05_2026-01_2026-03.json
---

# Aegis Short 2026 DD04/DD05 Sensitivity Analysis - 2026-06-13

## Agent Summary

Two short 2026-only Candidate B runs were downloaded: DD04 and DD05. Both have identical headline performance and actual data only through `2026-03-14`, despite `backtest-end=2026-06-11`. This window is too short to guide parameter selection; treat it only as a sanity check that DD04 vs DD05 does not change early-2026 trades.

## Run Setup

| Run | QC Name | Configured Period | Actual Metadata End | `pre-weak-dd-threshold` |
| --- | --- | --- | --- | ---: |
| DD04 | `Focused Blue Owlet` | `2026-01-01` to `2026-06-11` | `2026-03-14` | `0.04` |
| DD05 | `Square Red Orange Fox` | `2026-01-01` to `2026-06-11` | `2026-03-14` | `0.05` |

Shared parameters:

- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `pre-weak-growth-target=0.12`
- `pre-weak-def-target=0.30`
- `weak-growth-target=0.10`

## Result

| Metric | DD04 | DD05 | Read |
| --- | ---: | ---: | --- |
| Net Profit | `-3.602%` | `-3.602%` | Identical |
| CAGR | `-17.047%` | `-17.047%` | Identical |
| Drawdown | `5.000%` | `5.000%` | Identical |
| Sharpe | `-2.717` | `-2.717` | Identical |
| Sortino | `-2.692` | `-2.692` | Identical |
| PSR | `4.784%` | `4.784%` | Identical |
| Orders | `37` | `37` | Identical |
| Fees | `$37.00` | `$37.00` | Identical |

Diagnostic difference:

- DD04 recorded `PreWeakWeeks=1`.
- DD05 recorded `PreWeakWeeks=0`.
- The activation difference did not change trades or headline metrics in this very short window.

## Decision Read

Do not use this result to choose defaults. The effective backtest data ends at `2026-03-14`, making the sample only about ten diagnostic weeks. It confirms only that DD04/DD05 are indistinguishable over early 2026. Continue with the long-span finalist validation plan instead.
