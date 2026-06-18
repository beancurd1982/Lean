---
id: AEGIS-BT-2026-06-17-CANDIDATEC-SAMEEXEC-LONG
type: backtest-analysis
status: reviewed
date: 2026-06-17
topic: AegisGrowthAllocation
tags: [aegis, candidate-c, candidate-b, preweak-recovery, same-execution, market-on-close, backtest]
related:
  - project-notes/archive/2026-06/Aegis_TagCandidateB_MinParams_2016_2026-01-01_Confirmation_2026-06-17.md
  - project-notes/archive/2026-06/Aegis_LatestSource_CandidateBBehavior_MinParams_2016_2026-01-01_Analysis_2026-06-17.md
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_Long_2016_2026-01-01_Analysis_2026-06-16.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-17_222647__AegisGrowthAllocation__ManualGrid_CandidateC_PreWeakRecoveryV2_SameExec_Long_2016_2026-01-01_logs.txt
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_SameExec_Long_2016_2026-01-01.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_SameExec_Long_2016_2026-01-01_orders.csv
---

# Candidate C Same-Execution Long Validation - 2026-06-17

## Agent Summary
Candidate C PreWeak Recovery v2 was rerun after the exact Candidate B tag confirmation, using current cloud conditions and the same `Market On Close` execution behavior. Candidate C still beats Candidate B under same execution, but the edge is modest. The earlier large Candidate C advantage was partly caused by comparing Candidate C `Market` orders to Candidate B `Market On Close` orders.

## Settings
- `backtest-start = 2016-01-01`
- `backtest-end = 2026-01-01`
- `preweak-recovery-enabled = true`

Log confirms `PreWeakRecoveryEnabled=True`, `PreWeakRecoveryGrowthTarget=0.16`, `PreWeakRecoveryDefensiveTarget=0.30`, `PreWeakGrowthTarget=0.12`, `PreWeakDefensiveTarget=0.30`, `WeakGrowthTarget=0.10`.

All orders in artifact are `Market On Close`.

## Same-Execution Comparison
| Metric | Candidate B exact tag rerun | Candidate C same-execution |
|---|---:|---:|
| Final chart timestamp | 2025-12-31 21:15 UTC | 2025-12-31 21:15 UTC |
| Order type | Market On Close | Market On Close |
| Equity | 125,157.95 | 129,140.98 |
| Compounding Annual Return | 15.34% | 15.70% |
| Net Profit | 317.19% | 330.47% |
| Drawdown | 14.00% | 14.00% |
| Sharpe Ratio | 0.842 | 0.859 |
| Probabilistic Sharpe Ratio | 58.54% | 60.40% |
| Portfolio Turnover | 3.04% | 3.01% |
| Fees | 1,741.70 | 1,730.27 |
| Order rows | 1,542 | 1,535 |
| Win Rate | 65.71% | 66.51% |
| Profit Factor | 2.479 | 2.536 |

Candidate C is better on return, Sharpe, PSR, turnover, fees, order count, win rate, and profit factor. Drawdown is unchanged.

## Comparison To Earlier Candidate C Saved Run
| Metric | Earlier Candidate C saved run | Candidate C same-execution |
|---|---:|---:|
| Order type | Market | Market On Close |
| Equity | 155,912.61 | 129,140.98 |
| Compounding Annual Return | 17.90% | 15.70% |
| Net Profit | 419.71% | 330.47% |
| Drawdown | 13.70% | 14.00% |
| Sharpe Ratio | 1.011 | 0.859 |
| Probabilistic Sharpe Ratio | 78.12% | 60.40% |

Earlier headline advantage should not be used as promotion evidence because it mixed order execution models.

## Diagnostic Caveat
Long log truncated by QC 100 KB cap. No `PreWeakRecovery=True` line appears before truncation, so artifact supports headline performance comparison but not complete recovery activation diagnostics.

## Decision
Candidate C passes same-execution long-window comparison as an incremental improvement over Candidate B, but not by enough to skip stress validation. It is not a fully proven production default yet. It is reasonable to prepare as a paper-test candidate if the goal is live behavior validation, but the next research-grade validation should be same-execution stress-window run.

## Recommended Next Move
Run Candidate C same-execution on the 2021-2022 stress window:
- `backtest-start = 2021-01-01`
- `backtest-end = 2022-12-31`
- `preweak-recovery-enabled = true`

No diagnostics unless activation evidence is specifically needed.

Gate:
- If Candidate C preserves Candidate B stress behavior under `Market On Close`, create Candidate C paper-test tag.
- If it worsens 2021-2022 stress drawdown or materially degrades stress return, keep it in research.

## Workflow Notes
- Downloaded files moved from Downloads into `BackTestLogs`.
- Repo-local normalizer produced normalized log filename.
- Original Downloads files were removed by moving.
