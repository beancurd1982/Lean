---
id: AEGIS-BT-2026-06-17-CANDIDATEC-SAMEEXEC-STRESS04
type: backtest-analysis
status: reviewed
date: 2026-06-17
topic: AegisGrowthAllocation
tags: [aegis, candidate-c, candidate-b, preweak-recovery, same-execution, market-on-close, stress-window, backtest]
related:
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_SameExecution_2016_2026-01-01_Analysis_2026-06-17.md
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_Stress04_2021_2022_Analysis_2026-06-16.md
  - project-notes/archive/2026-06/Aegis_CandidateB_Diagnostic_Stress04_2021_2022_Analysis_2026-06-14.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-17_223847__AegisGrowthAllocation__ManualGrid_CandidateC_PreWeakRecoveryV2_SameExec_Stress04_2021_2022_logs.txt
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_SameExec_Stress04_2021_2022.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_SameExec_Stress04_2021_2022_orders.csv
---

# Candidate C Same-Execution Stress Validation - 2026-06-17

## Agent Summary
Candidate C was rerun on the 2021-2022 stress window with `preweak-recovery-enabled=true` and current cloud `Market On Close` execution. It did not clearly pass the stress gate. The run is worse than the older Candidate B/C stress baselines, but those older baselines used `Market` orders, so the next clean comparison must be a Candidate B same-execution stress control.

## Settings
- `backtest-start = 2021-01-01`
- `backtest-end = 2022-12-31`
- `preweak-recovery-enabled = true`

Log confirms:
- `PreWeakRecoveryEnabled=True`
- `PreWeakRecoveryGrowthTarget=0.16`
- `PreWeakRecoveryDefensiveTarget=0.30`
- `PreWeakGrowthTarget=0.12`
- `PreWeakDefensiveTarget=0.30`
- `WeakGrowthTarget=0.10`

All 302 orders in the order artifact are `Market On Close`.

## Headline Result
| Metric | Candidate C same-execution stress |
|---|---:|
| Start Equity | 30,000.00 |
| End Equity | 35,230.42 |
| Net Profit | 17.435% |
| Compounding Annual Return | 8.383% |
| Drawdown | 13.100% |
| Sharpe Ratio | 0.602 |
| Probabilistic Sharpe Ratio | 30.955% |
| Portfolio Turnover | 3.04% |
| Total Fees | 302.60 |
| Orders | 302 |
| Trades | 201 |
| Win Rate | 60.20% |
| Profit Factor | 1.8037 |

## Non-Clean Comparison Context
| Metric | Candidate B older stress baseline | Candidate C same-execution stress |
|---|---:|---:|
| Order type | Market | Market On Close |
| End Equity | 35,942.31 | 35,230.42 |
| Net Profit | 19.808% | 17.435% |
| Compounding Annual Return | 9.475% | 8.383% |
| Drawdown | 12.400% | 13.100% |
| Sharpe Ratio | 0.695 | 0.602 |
| Probabilistic Sharpe Ratio | 36.392% | 30.955% |
| Orders | 301 | 302 |
| Profit Factor | 1.9057 | 1.8037 |

This comparison is directionally concerning but not a clean rejection because the execution model differs.

## Diagnostic Caveat
The run did not include detailed crisis diagnostics. It validates headline stress performance and execution type, but it does not provide a recovery activation count.

## Decision
Do not create a Candidate C paper-test tag yet. The long-window result was modestly better than Candidate B under MOC, but this stress-window result is weaker than the older stress baselines and needs a clean Candidate B MOC control before promotion.

## Recommended Next Move
Run a Candidate B same-execution stress control using the same current cloud/order behavior:
- Upload latest source or exact Candidate B tag source.
- Set `backtest-start = 2021-01-01`
- Set `backtest-end = 2022-12-31`
- Do not set `preweak-recovery-enabled`, or set it to `false`.

Gate:
- If Candidate C beats or matches Candidate B MOC stress while preserving the long-window improvement, Candidate C can be tagged for paper testing.
- If Candidate C still loses to Candidate B MOC stress, keep Candidate B as the paper/live baseline and return Candidate C to research.

## Workflow Notes
- Downloaded files were moved from Downloads into `BackTestLogs`.
- Repo-local normalizer produced normalized log filename.
- Original Downloads files were removed by moving.
