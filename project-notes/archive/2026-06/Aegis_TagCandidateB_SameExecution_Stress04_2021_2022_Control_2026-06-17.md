---
id: AEGIS-BT-2026-06-17-TAGCANDIDATEB-SAMEEXEC-STRESS04
type: backtest-analysis
status: reviewed
date: 2026-06-17
topic: AegisGrowthAllocation
tags: [aegis, candidate-b, candidate-c, same-execution, market-on-close, stress-window, backtest]
related:
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_SameExecution_Stress04_2021_2022_Analysis_2026-06-17.md
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_SameExecution_2016_2026-01-01_Analysis_2026-06-17.md
  - project-notes/archive/2026-06/Aegis_TagCandidateB_MinParams_2016_2026-01-01_Confirmation_2026-06-17.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-17_225256__AegisGrowthAllocation__ManualGrid_TagCandidateB_SameExec_Stress04_2021_2022_logs.txt
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_TagCandidateB_SameExec_Stress04_2021_2022.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_TagCandidateB_SameExec_Stress04_2021_2022_orders.csv
---

# Candidate B Same-Execution Stress Control - 2026-06-17

## Agent Summary
The exact Candidate B tag was rerun on the 2021-2022 stress window under current cloud `Market On Close` execution. Candidate B slightly beats Candidate C on this stress control. This blocks immediate Candidate C paper-test promotion unless the team explicitly accepts a small stress degradation for a modest long-window improvement.

## Settings
- Source: `aegis-growth-allocation-v2026.06.08-candidate-b-paper-live`
- `backtest-start = 2021-01-01`
- `backtest-end = 2022-12-31`
- No `preweak-recovery-enabled` parameter.

All 300 orders in the order artifact are `Market On Close`.

## Same-Execution Stress Comparison
| Metric | Candidate B tag MOC stress | Candidate C MOC stress |
|---|---:|---:|
| Start Equity | 30,000.00 | 30,000.00 |
| End Equity | 35,283.63 | 35,230.42 |
| Net Profit | 17.612% | 17.435% |
| Compounding Annual Return | 8.465% | 8.383% |
| Drawdown | 12.900% | 13.100% |
| Sharpe Ratio | 0.609 | 0.602 |
| Probabilistic Sharpe Ratio | 31.359% | 30.955% |
| Portfolio Turnover | 3.04% | 3.04% |
| Total Fees | 300.60 | 302.60 |
| Orders | 300 | 302 |
| Trades | 198 | 201 |
| Win Rate | 60.61% | 60.20% |
| Profit Factor | 1.8142 | 1.8037 |

Candidate B is slightly better across every stress metric listed. The gap is small, but the direction matters because Candidate C's long-window improvement was also modest.

## Decision
Do not promote Candidate C to paper-test tag yet. Keep Candidate B as the stress baseline.

Candidate C remains useful research evidence, but the current recovery-gated implementation does not improve the stress window enough to justify replacing Candidate B as the default/paper candidate.

## Recommended Next Move
Use Candidate B tag/current Candidate B behavior for paper-live baseline validation. If continuing research, optimize Candidate C only after defining an explicit tradeoff threshold, for example:
- Accept Candidate C only if stress drawdown is no worse than Candidate B by more than 0.1 percentage point.
- Accept Candidate C only if long-window CAR improves by at least 0.5 percentage point.
- Require Candidate C to match or beat Candidate B stress Sharpe.

Under the current evidence, Candidate C misses the stress gate.

## Workflow Notes
- Downloaded files were moved from Downloads into `BackTestLogs`.
- Repo-local normalizer produced normalized log filename.
- Original Downloads files were removed by moving.
