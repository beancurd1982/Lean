---
id: AEGIS-HYP-2026-06-21-CANDIDATEE
type: hypothesis
status: proposed
date: 2026-06-21
topic: AegisGrowthAllocation
tags: [aegis, candidate-e, hypothesis, preweak, redeployment]
related:
  - project-notes/archive/2026-06/Aegis_CandidateD_FullRun_Gap_Attribution_2026-06-21.md
  - project-notes/archive/2026-06/Aegis_CandidateD_PostStressRedeployment_Hypothesis_2026-06-20.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.Recovery.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.Parameters.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs
---

# Candidate E - Soft PreWeak Exit Buffer

## Agent Summary

Read this before implementing Candidate E. Candidate D failed because its stricter four-week recovery confirmation preserved stress behavior but kept the strategy trapped in the 12% pre-weak sleeve during a 2025 recovery. Candidate E should test a bounded soft-exit rule that preserves defensive entry, keeps Candidate D's conservative recovery confirmation, and exits pre-weak only when drawdown has nearly recovered to the guard threshold and weak signals are absent.

## Candidate ID

`Candidate E - Soft PreWeak Exit Buffer`

## Baseline

Primary baseline:

- Candidate B same-execution stress control remains the stress gate:
  - net profit `17.612%`
  - drawdown `12.900%`
  - Sharpe `0.609`
  - PSR `31.359%`
  - orders `300`

Comparison evidence:

- Candidate C reduced diagnostic full run:
  - net profit `333.102%`
  - CAGR `15.774%`
  - drawdown `14.0%`
  - orders `1539`
- Candidate D reduced diagnostic full run:
  - net profit `311.574%`
  - CAGR `15.186%`
  - drawdown `14.0%`
  - orders `1556`

Candidate D is rejected as currently specified. Candidate C remains research evidence, not promotion-ready, because it still misses the Candidate B stress gate.

## Proposed Change

Implement a default-off soft exit from the pre-weak guard:

- keep pre-weak defensive entry unchanged;
- keep Candidate D's four-week pre-weak recovery confirmation unchanged for the `pre-weak-recovery` sleeve;
- when pre-weak guard would otherwise remain active, allow the algorithm to use the base regime sleeve instead if all soft-exit conditions are met:
  - active regime is not `Weak`;
  - trend is `Favorable`;
  - breadth is not `Weak`;
  - stress is not `Weak`;
  - current drawdown is no more than `pre-weak-dd-threshold + preweak-soft-exit-dd-buffer`;
  - current segment has recovered by at least `preweak-soft-exit-min-recovery` from its local trough.

Initial Candidate E parameter set:

```text
preweak-recovery-enabled=true
preweak-rec-confirm-weeks=4
preweak-rec-growth-target=0.16
preweak-rec-def-target=0.30
preweak-rec-dd-improve=0.02
preweak-rec-max-dd=0.08
preweak-soft-exit-enabled=true
preweak-soft-exit-dd-buffer=0.005
preweak-soft-exit-min-recovery=0.005
```

## Mechanism

The 2025 C/D reduced diagnostics showed Candidate D stayed in pre-weak on `2025-09-08` with drawdown `0.0424`, local recovery `0.0058`, no weak signals, and a neutral base regime. Candidate C exited pre-weak that same week because its equity path put drawdown below the `0.0400` guard threshold, so it moved to the neutral sleeve and bought AVGO, GOOGL, MSFT, and NVDA one week earlier.

Candidate E tests whether a small soft-exit buffer can avoid this self-reinforcing low-growth trap without weakening defensive entry. The rule is intentionally asymmetric: entry remains strict and immediate, while exit gets a narrow recovery allowance only after weak signals are absent.

## Expected Stress Effect

Candidate E is expected to preserve Candidate B stress behavior better than Candidate C because it keeps Candidate D's four-week recovery confirmation and does not loosen initial pre-weak entry. Stress preservation must be proven, not assumed.

Stress gate:

- same-execution 2021-01-01 through 2022-12-31;
- Market On Close execution;
- match or beat Candidate B drawdown `12.900%`, Sharpe `0.609`, PSR `31.359%`, and order count `300`;
- net profit should be no worse than Candidate B `17.612%` unless the shortfall is trivial and offset by materially better risk metrics.

## Expected Long-Window Effect

Candidate E should recover the 2025 path loss seen in Candidate D and meaningfully improve over Candidate C, not just match it.

Long-window gate:

- same-execution 2016-01-01 through 2026-01-01;
- Market On Close execution;
- beat Candidate C diagnostic full run net profit `333.102%` and CAGR `15.774%`;
- preserve drawdown at or below `14.0%`;
- avoid Candidate D's extra churn pattern around `2025-09-15`, `2025-09-29`, and `2025-10-06`.

## Affected Files

Expected implementation scope:

- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.Recovery.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.Parameters.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`

No Object Store, live state schema, order handling, brokerage model, deployment identity, or default live behavior should change.

## Parameters

New parameters should be default-off:

| Parameter | Default | Allowed range | Purpose |
|---|---:|---:|---|
| `preweak-soft-exit-enabled` | `false` | boolean | Enables Candidate E soft pre-weak exit logic |
| `preweak-soft-exit-dd-buffer` | `0.005` | `0.000` to `0.020` | Allows exit when drawdown is close to the normal pre-weak threshold |
| `preweak-soft-exit-min-recovery` | `0.005` | `0.000` to `0.030` | Requires recovery from the local pre-weak trough before soft exit |

Candidate E test parameters should explicitly enable the feature. Defaults remain Candidate B-safe.

## Validation Matrix

Run in this order:

| Gate | Run | Purpose |
|---|---|---|
| Build/tests | Local build and focused tests | Verify no compile or parameter parsing regression |
| Stress preservation | Candidate E 2021-2022 stress | Must match or beat Candidate B stress gate before long-window testing |
| 2024-2025 diagnostic | Candidate E full-path reduced diagnostic, `diagnostic-start=2024-01-01`, `diagnostic-end=2025-12-31` | Confirm `2025-09-08` exits pre-weak and avoids Candidate D churn |
| Full long | Candidate E 2016-2026 long | Must beat Candidate C long metrics meaningfully |
| Optional segmented long | 2016-2017, 2022-2023, 2024-2026 | Diagnose if full long fails or shows path dependence |

## Rejection Criteria

Reject Candidate E if any of these occur:

- 2021-2022 stress fails Candidate B gate on drawdown, Sharpe, PSR, or order count;
- full long fails to beat Candidate C net profit and CAGR by a meaningful margin;
- 2025 diagnostic still shows D-like delayed redeployment, sell-out on `2025-09-29`, or re-entry churn on `2025-10-06`;
- soft exit triggers during weak-signal periods where the defensive guard is supposed to remain active;
- order count, turnover, or fees increase materially versus Candidate C without a compensating return/risk improvement.

## Live-Safety Notes

Candidate E must remain experimental and default-off. The proposed change affects only weekly sleeve selection during backtests when explicitly enabled. It must not touch:

- Object Store state;
- live-state schema;
- order submission or order reconciliation;
- deployment identity;
- brokerage model;
- default parameter behavior;
- restart behavior.

Live or paper promotion is blocked until stress, full-long, diagnostic, and live-readiness evidence are complete.
