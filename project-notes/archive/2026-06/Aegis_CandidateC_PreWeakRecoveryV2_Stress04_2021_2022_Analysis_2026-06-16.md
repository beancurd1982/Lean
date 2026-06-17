---
id: AEGIS-BT-2026-06-16-CANDIDATEC-PREWEAK-RECOVERY-V2-STRESS04
type: backtest-analysis
status: reviewed
date: 2026-06-16
topic: AegisGrowthAllocation
tags: [aegis, candidate-c, preweak-recovery, backtest, stress04, 2021-2022]
related:
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeak_Recovery_v2_Implementation_Plan_2026-06-15.md
  - project-notes/archive/2026-06/Aegis_CandidateB_Diagnostic_Stress04_2021_2022_Analysis_2026-06-14.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-16_232905__AegisGrowthAllocation__ManualGrid_CandidateC_PreWeakRecoveryV2_Stress04_2021_2022_logs.txt
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_Stress04_2021_2022.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_Stress04_2021_2022_orders.csv
---

# Candidate C PreWeak Recovery v2 Stress04 Analysis - 2026-06-16

## Agent Summary

Candidate C v2 was run on the 2021-01-01 to 2022-12-31 stress window with `preweak-recovery-enabled=true` and default recovery gates.

The result is stress-safe but not informative for return improvement: the recovery sleeve never activated.

## Settings

Cloud parameters entered:

- `backtest-start = 2021-01-01`
- `backtest-end = 2022-12-31`
- `crisis-diagnostics = true`
- `preweak-recovery-enabled = true`

Source defaults supplied the rest:

- Normal PreWeak: `G0.12/D0.30/C0.58`
- Recovery PreWeak: `G0.16/D0.30/C0.54`
- Weak: `G0.10/D0.40/C0.50`
- Recovery gates: `dd-improve=0.02`, `max-dd=0.08`, `confirm-weeks=2`

## Headline Comparison Versus Candidate B

| Metric | Candidate B stress baseline | Candidate C v2 |
|---|---:|---:|
| Net Profit | 19.771% | 19.780% |
| Compounding Annual Return | 9.458% | 9.462% |
| Drawdown | 12.500% | 12.500% |
| Sharpe Ratio | 0.694 | 0.694 |
| Probabilistic Sharpe Ratio | 36.307% | 36.335% |
| Total Orders | 301 | 302 |
| Gross absolute order value | 772,490.05 | 772,437.27 |

Interpretation: the headline metrics are effectively unchanged from Candidate B.

## Diagnostic Summary

Candidate C diagnostic summary:

- `Weeks=104`
- `PreWeakWeeks=24`
- `NormalPreWeakWeeks=24`
- `RecoveryPreWeakWeeks=0`
- `NonPreWeakWeeks=80`
- `WeakRegimeWeeks=26`
- `PreWeakAvgDrawdown=0.0744`
- `PreWeakNextReturnAvg=-0.0012`
- `PreWeakFwd4Avg=-0.0030`
- `PreWeakFwd8Avg=-0.0138`
- `PreWeakFwd12Avg=-0.0179`
- `PreWeakFwd4WinRate=0.4091`

Recovery gate detail across the 24 PreWeak weeks:

- `weak-signal`: 11 weeks
- `insufficient-recovery`: 9 weeks
- `new-trough`: 3 weeks
- `eligible`: 1 week
- recovery active: 0 weeks

The only positive confirmation week was 2022-04-11. It did not persist for the required second confirmation week.

## Decision

Candidate C v2 passes the stress-preservation gate in a narrow sense:

- Net profit is effectively unchanged.
- Drawdown is not worse.
- PSR is not worse.
- Orders and gross order value are not materially higher.
- There was zero recovery activation before the first baseline Weak regime entry on 2022-05-16.

However, this pass is mostly because the recovery sleeve never activated. This run does not prove that `G16/D30/C54` recovery improves the algorithm.

## Recommended Next Move

Run Candidate C v2 unchanged on the long window, `2016-01-01` to latest available data, with diagnostics enabled and recovery enabled.

Reason:

- The 2021-2022 stress window confirms the feature is default-safe under the first hard gate.
- We now need to know whether the recovery sleeve activates in other historical recovery episodes.
- Do not relax `confirm-weeks` or `dd-improve` yet. A one-week confirmation would likely activate earlier in 2022 and could violate the stress-preservation rule.

## Workflow Notes

- Downloaded files were moved from the default Downloads folder into `BackTestLogs`.
- The repo-local log normalizer was run with process-scoped PowerShell execution-policy bypass because direct script execution is disabled on this machine.
- The original Downloads files were removed by moving them into the repo artifact folder.
