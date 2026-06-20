---
id: AEGIS-DEC-2026-06-20-CANDIDATE-D-HYPOTHESIS
type: decision
status: proposed
date: 2026-06-20
topic: AegisGrowthAllocation
tags: [aegis, candidate-d, hypothesis, post-stress-redeployment]
related:
  - project-notes/archive/2026-06/Aegis_CandidateB_Return_Gap_Attribution_Analysis_2026-06-20.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.Recovery.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs
---

# Candidate D Post-Stress Redeployment Hypothesis

## Agent Summary

This is the pre-registered Candidate D proposal. It uses existing Candidate C recovery code but tightens the activation gate from two confirmation weeks to four. The goal is to preserve Candidate B's 2021-2022 stress behavior and keep only the more persistent 2025-style post-stress redeployment signal.

## Candidate ID

`Candidate D - Four-Week Post-Stress Redeployment`

## Baseline

Baseline comparison set:

- Candidate B current-source behavior with `preweak-recovery-enabled=false`.
- Candidate C current-source recovery behavior with `preweak-recovery-enabled=true` and default recovery confirmation of `2` weeks.
- Same-execution Market On Close artifacts recorded in `Aegis_CandidateB_Return_Gap_Attribution_Analysis_2026-06-20.md`.

Candidate B remains the stress baseline. Candidate C is research evidence, not promotion-ready.

## Proposed Change

Run a parameter-only Candidate D variant using existing source code:

```text
preweak-recovery-enabled=true
preweak-rec-confirm-weeks=4
preweak-rec-growth-target=0.16
preweak-rec-def-target=0.30
preweak-rec-dd-improve=0.02
preweak-rec-max-dd=0.08
crisis-diagnostics=true
```

All other Candidate B defaults stay unchanged. No trading-code change is approved by this note.

## Mechanism

Candidate C helped when recovery stayed confirmed long enough to accelerate redeployment from a cash-heavy drawdown-signals posture. The strongest evidence was July-September 2025, where recovery confirmation persisted through at least four weekly observations before the larger September return gap.

Candidate C hurt or failed to help when recovery activated briefly:

- 2021-2022 stress had a single recovery week at confirmation `2`.
- 2016-2017 had three recovery weeks with confirmation `2`, `3`, and `2`, and Candidate C lost to Candidate B.
- 2022-2023 had recovery confirmations of `2`, `2`, and `3`, with no headline benefit.

Requiring `preweak-rec-confirm-weeks=4` should block these short-lived activations while preserving the 2025 path that reached confirmations `4` and `5`.

## Expected Stress Effect

Expected 2021-2022 stress behavior:

- Candidate D should behave like Candidate B or nearly like Candidate B because the observed Candidate C stress activation only reached confirmation `2`.
- Stress gate is Candidate B same-execution stress behavior:
  - net profit at or above `17.612%`;
  - drawdown no worse than `12.900%`;
  - Sharpe at or above `0.609`;
  - PSR at or above `31.359%`;
  - order count no higher than Candidate B by more than routine rounding noise.

Stress validation is the first gate. If Candidate D fails stress, do not run or interpret long-window promotion tests as candidate evidence.

## Expected Long-Window Effect

Expected long-window behavior:

- Candidate D should retain part of Candidate C's 2025 redeployment gain while avoiding the weak 2016 activation path.
- A valid result should improve over Candidate C meaningfully, not merely match it.
- Minimum long-window gate:
  - improve Candidate C same-execution long net profit by at least `3` points, or CAGR by at least `0.3%`;
  - keep drawdown no worse than Candidate C's `14.00%`;
  - avoid material increases in turnover, fees, and orders.

Segmented diagnostics must show evidence across at least two windows before promotion. If the gain remains isolated to 2025, Candidate D can be a research lead but not a promoted candidate.

## Affected Files

Expected source files if later implementation is needed:

- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.Recovery.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`

For the first validation pass, no source file should change because existing parameters already support the hypothesis.

## Parameters

| Parameter | Candidate D Value | Default | Purpose |
|---|---:|---:|---|
| `preweak-recovery-enabled` | `true` | `false` | Enable existing recovery logic for backtest candidate only. |
| `preweak-rec-confirm-weeks` | `4` | `2` | Require persistent recovery before redeployment. |
| `preweak-rec-growth-target` | `0.16` | `0.16` | Keep Candidate C's modest recovery growth target. |
| `preweak-rec-def-target` | `0.30` | `0.30` | Preserve defensive sleeve target. |
| `preweak-rec-dd-improve` | `0.02` | `0.02` | Keep existing improvement threshold. |
| `preweak-rec-max-dd` | `0.08` | `0.08` | Keep existing max drawdown eligibility. |

Default live behavior remains Candidate B because `preweak-recovery-enabled` defaults to `false`.

## Validation Matrix

Run in this order:

| Gate | Candidate D Parameters | Window | Purpose |
|---|---|---|---|
| Stress first | Candidate D set above | `2021-01-01` to `2022-12-31` | Must match or beat Candidate B stress behavior before any promotion discussion. |
| Long segment 2016-2017 | Candidate D set above | `2016-01-01` to `2017-12-31` | Confirm the stricter gate avoids Candidate C's weak 2016 activation path. |
| Long segment 2022-2023 | Candidate D set above | `2022-01-01` to `2023-12-31` | Confirm the stricter gate avoids non-beneficial 2022/2023 activations. |
| Long segment 2024-2026 | Candidate D set above | `2024-01-01` to `2026-01-01` | Confirm Candidate D retains useful 2025 redeployment behavior. |
| Full long | Candidate D set above | `2016-01-01` to `2026-01-01` | Only after the above pass; compare against Candidate B and Candidate C same-execution long controls. |

Use `crisis-diagnostics=true` for all validation runs.

## Rejection Criteria

Reject Candidate D if any of these occur:

- Stress net profit, drawdown, Sharpe, or PSR is worse than Candidate B's same-execution stress control.
- Stress order count or fees materially exceed Candidate B without offsetting risk improvement.
- `2016-2017` remains worse than Candidate B by more than `0.1` net-profit points.
- `2022-2023` degrades versus Candidate B.
- `2024-2026` loses most of Candidate C's redeployment gain and does not create another validated window.
- Full long improvement is below `3` net-profit points and below `0.3%` CAGR versus Candidate C.
- Diagnostic logs show the benefit still depends on one isolated 2025 event.

## Live-Safety Notes

This hypothesis does not approve live deployment or a default change.

The first pass should be parameter-only and should not touch:

- order handling;
- Object Store state;
- live-state schema;
- restart behavior;
- deployment identity;
- default live parameters.

Any later source-code change or default change requires `aegis-candidate-implementer` and then `aegis-live-readiness-reviewer`.

## Next Skill

Use `aegis-backtest-evaluator` after Candidate D stress and diagnostic runs are produced.
