---
id: AEGIS-BT-2026-06-21-CANDIDATED-GAP
type: backtest-analysis
status: accepted
date: 2026-06-21
topic: AegisGrowthAllocation
tags: [aegis, candidate-d, candidate-e, attribution, backtest]
related:
  - project-notes/archive/2026-06/Aegis_CandidateD_FourWeekRedeployment_FullLong_Rejection_2026-06-20.md
  - project-notes/archive/2026-06/Aegis_CandidateD_FourWeekRedeployment_2024_2026_Gate_2026-06-20.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.Parameters.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_DiagSameExec_Long_2016_2026-01-01.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2016_2026-01-01.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_DiagSameExec_Long_2016_2026-01-01_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateD_FourWeekPostStressRedeployment_DiagSameExec_Long_2016_2026-01-01_orders.csv
---

# Candidate D Full-Run Gap Attribution

## Agent Summary

Read this before designing Candidate E. Candidate D should remain stopped: the full-run failure is not explained by a small fee/order drag and should not be tuned by small parameter nudges. The visible gap appears in the full-run 2025 path, while standalone 2024-2026 C and D runs are nearly identical, so the next step is targeted diagnostics for path/state carryover rather than Candidate E implementation.

## Decision Or Finding

Do not design or tune Candidate E yet. First close the diagnostic gap around full-run path dependency into 2025.

The exact diagnostic Candidate C full run beats Candidate D materially:

| Metric | Candidate C diagnostic full | Candidate D full | Difference |
|---|---:|---:|---:|
| CAGR | 15.774% | 15.186% | -0.588 pp |
| Net profit | 333.102% | 311.574% | -21.528 pp |
| End equity | 129,931 | 123,472 | -6,459 |
| Drawdown | 14.0% | 14.0% | 0.0 pp |
| Sharpe | 0.863 | 0.827 | -0.036 |
| PSR | 60.847% | 56.353% | -4.494 pp |
| Orders | 1,539 | 1,556 | +17 |
| Fees | 1,743.45 | 1,759.93 | +16.48 |
| Turnover | 3.00 | 3.06 | +0.06 |
| Profit factor | 2.5080 | 2.4085 | -0.0995 |

Fees and order count are worse for D, but the fee delta is far too small to explain the end-equity gap.

## Evidence

Full-run annual attribution shows the decisive separation occurs in 2025:

| Year | B diagnostic | C diagnostic | D | C minus D |
|---|---:|---:|---:|---:|
| 2021 | 32.76% | 32.77% | 31.32% | +1.45 pp |
| 2022 | -9.77% | -9.90% | -9.83% | -0.06 pp |
| 2023 | 31.27% | 31.30% | 31.26% | +0.04 pp |
| 2024 | 16.36% | 16.54% | 16.41% | +0.13 pp |
| 2025 | 9.01% | 12.69% | 7.98% | +4.71 pp |

Selected full-run end-equity snapshots:

| Date | B diagnostic | C diagnostic | D | C minus D |
|---|---:|---:|---:|---:|
| 2021-12-31 | 83,660.98 | 83,626.31 | 82,992.01 | +634.30 |
| 2023-12-29 | 99,092.45 | 98,939.05 | 98,226.09 | +712.95 |
| 2024-12-31 | 115,306.14 | 115,298.86 | 114,342.83 | +956.03 |
| 2025-12-31 | 125,691.05 | 129,930.65 | 123,472.17 | +6,458.48 |

Standalone 2024-2026 diagnostics do not reproduce the full-run failure:

| Run | Start equity | End equity | Return |
|---|---:|---:|---:|
| C diagnostic full, 2024-2025 slice | 98,939.05 | 129,930.65 | 31.324% |
| D full, 2024-2025 slice | 98,226.09 | 123,472.17 | 25.702% |
| C standalone 2024-2026 | 30,000.00 | 39,135.61 | 30.452% |
| D standalone 2024-2026 | 30,000.00 | 39,133.95 | 30.447% |

Order review found a visible 2025 timing divergence:

- Candidate C buys the high-growth basket on 2025-09-08: AVGO, GOOGL, MSFT, NVDA.
- Candidate D has no orders on 2025-09-08.
- Candidate D buys similar growth exposure on 2025-09-15, exits on 2025-09-29, and re-enters on 2025-10-06.
- Candidate C does not make the same 2025-09-15/2025-09-29 churn sequence.

The full-run C and D diagnostic logs both hit the QuantConnect 100 KB log cap and stop at 2018-10-22, so they cannot explain the 2025 path divergence directly.

## Next Step

Do not rerun the ordinary standalone 2024-2026 Candidate D diagnostic as the primary next step; that evidence already exists and does not reproduce the full-run gap.

Preferred next step: add or use a diagnostic-only reduced-log mode that runs the full 2016-2026 path but emits weekly/event diagnostics only for 2024-2025. This should be logging-only and must not change trading behavior, order handling, live state, deployment identity, or parameter defaults.

If diagnostic-only source work is not approved yet, the fallback no-code cloud tests are Candidate D segmented diagnostics for 2018-2019 and 2020-2021. Those fill missing validation windows and may explain earlier path drift, but they are unlikely to fully explain the 2025 warm-state divergence by themselves.

## Diagnostic Window Implementation

Added diagnostic-only parameters:

- `diagnostic-start`
- `diagnostic-end`

These parameters are parsed only when `crisis-diagnostics=true` in backtest mode. They filter weekly crisis diagnostic log emission and diagnostic attribution recording only. They do not change regime decisions, target weights, orders, live state, deployment identity, or parameter defaults.

Run the focused full-path Candidate D diagnostic in QuantConnect with:

```text
backtest-start=2016-01-01
backtest-end=2026-01-01
preweak-recovery-enabled=true
preweak-rec-confirm-weeks=4
preweak-rec-growth-target=0.16
preweak-rec-def-target=0.30
preweak-rec-dd-improve=0.02
preweak-rec-max-dd=0.08
crisis-diagnostics=true
diagnostic-start=2024-01-01
diagnostic-end=2025-12-31
```

Candidate D reduced diagnostic was later run successfully with this window; its metrics matched the prior D full-run failure profile, confirming the diagnostic window did not change trading behavior. A matching Candidate C reduced diagnostic was attempted, but the resulting log started at 2016-01-04 and hit QuantConnect's 100 KB log cap at 2018-10-22, so it did not capture the needed 2024-2025 weekly state. Treat that Candidate C rerun as a valid full-result metrics check only, not as usable reduced-window diagnostic evidence.

A corrected Candidate C reduced diagnostic was then run successfully. It confirmed the 2025 mechanism:

| Date | Candidate C | Candidate D | Decision impact |
|---|---|---|---|
| 2025-09-08 | `Override=none`, target `G0.4500/D0.3000/C0.2500`, buys AVGO/GOOGL/MSFT/NVDA | `Override=pre-weak`, target remains about `G0.1171/D0.2928/C0.5902`, no growth buy | D misses one week of redeployment |
| 2025-09-15 | C already near neutral sleeve | D finally moves to neutral sleeve and buys growth at higher prices | D enters late |
| 2025-09-29 | C remains neutral sleeve | D falls back to pre-weak and sells AVGO/GOOGL/MSFT/NVDA | D churns out of growth |
| 2025-10-06 | C upgrades from neutral toward favorable | D re-enters growth from cash/defensive state | D re-enters after avoidable churn |

This supports designing Candidate E around a bounded, asymmetric redeployment rule rather than small confirmation-count nudges. The issue is not only the extra order cost; the stricter D path keeps exposure too low during a recovery and makes the drawdown trigger self-reinforcing.

## Verification

- Ran `check_log_index.py`: 79 rows, 0 unreviewed, 0 errors, 0 warnings.
- Ran `compare_aegis_backtests.py` for C diagnostic full versus D full.
- Ran `compare_aegis_backtests.py` for C versus D standalone 2024-2026.
- Read C/D full-run order CSVs and compared annual/order-date differences.
- Confirmed both full-run diagnostic logs end at 2018-10-22 with the QuantConnect 100 KB log cap warning.
- Ran `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -nologo`: passed with existing warnings.
- Attempted focused Aegis diagnostic-window tests, but the local test host aborted before executing due missing `../../../Data/equity/sgx/map_files` followed by a Python.NET GIL finalizer crash.
- Normalized Candidate D reduced diagnostic result and Candidate C unfiltered rerun result into `BackTestLogs`; `check_log_index.py` reported 81 rows, 0 unreviewed, 0 errors, 0 warnings.
- Normalized corrected Candidate C reduced diagnostic result into `BackTestLogs`; `check_log_index.py` reported 82 rows, 0 unreviewed, 0 errors, 0 warnings.

## Risks And Open Questions

- The exact 2025 cause cannot be confirmed from the full-run logs because of log truncation.
- Standalone 2024-2026 does not reproduce full-run state carryover, so Candidate E should not be based only on standalone 2024-2026 behavior.
- A reduced-log full-run diagnostic may require a diagnostic-only code change; do not change trading behavior for that.
