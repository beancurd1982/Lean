---
id: AEGIS-OPT-2026-06-12
type: decision
status: proposed
date: 2026-06-12
topic: AegisGrowthAllocation
tags: [aegis, optimization, candidate-b, severe-crash, validation]
related:
  - project-notes/archive/2026-06/Aegis_Weak_Sleeve_Target_Parameterization_2026-06-08.md
  - project-notes/archive/2026-05/Aegis_Validation_Matrix_Run_Sheet_2026-05-16.md
  - project-notes/archive/2026-05/Aegis_Strategy_Review_After_Defensive_Optimization_2026-05-19.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs
  - Tests/Algorithm/AegisGrowthAllocationTests.cs
---

# Aegis Candidate B Next Optimization Proposal - 2026-06-12

## Agent Summary

Candidate B remains the current optimization baseline. The next step should be validation-first, not another immediate parameter or code change. Run fresh Candidate B holdout and control-window checks before testing any selective severe-crash variant. If severe-crash work proceeds, keep it frozen, backtest-only, disabled by default, and implemented through the existing severe-crash path rather than as a new overlay.

## Current Baseline

- Matching local tag found during review: `aegis-growth-allocation-v2026.06.08-candidate-b-paper-live`.
- User-referenced tag name was not present locally: `AegisGrowthAllocation-2026-06-08-candidate-b-defaults`.
- Current Candidate B defaults:
  - `pre-weak-dd-threshold=0.04`
  - `pre-weak-growth-target=0.12`
  - `pre-weak-def-target=0.30`
  - `weak-growth-target=0.10`
- Candidate B five-window aggregate from the 2026-06-08 note:
  - Sum net profit: `158.823%`
  - Average Sharpe: `0.514`
  - Average drawdown: `12.94%`
  - Total orders: `1474`
- Residual risk: Candidate B hurt the `2009-01-01` to `2010-12-31` recovery window by about `3.982%` net profit versus Candidate A.

## Multi-Agent Review Result

Three read-only reviewers evaluated the next optimization direction:

- Performance/optimization reviewer: supported pausing weak-sleeve tuning, but warned that a strict severe trigger has high overfitting risk if designed from already-mined crisis windows.
- Live-risk reviewer: emphasized that any severe-crash trigger can affect persisted live state and target allocations, so new behavior must remain disabled, non-live, and backtest-only until decision-grade evidence exists.
- Implementation/testability reviewer: recommended reusing the existing `SevereCrashOverride` path if code is needed, not creating a separate overlay or state machine.

Rebuttal round converged on the same position:

- Do not implement a new strict trigger now.
- Run Candidate B holdout and control validation first.
- Treat `2007-2008` as a smoke test, not promotion evidence.
- Prefer parameter-only severe-crash experiments before any code change.
- If code is required, add one frozen optional strict gate inside the existing severe-crash entry path only.

## Revised Proposal

1. Keep Candidate B as the baseline.
2. Stop weak-sleeve target tuning for now.
3. Run Candidate B unchanged from `2026-01-01` to latest available data.
4. Add normal/control validation windows before designing more defensive logic.
5. If Candidate B holdout and controls are acceptable, run one pre-registered severe-crash parameter experiment if the current parameters can express the intended behavior.
6. Only if parameter-only testing cannot express the intended selective trigger, implement one disabled, non-live, backtest-only stricter gate inside the existing `SevereCrashOverride` path.
7. Do not change defaults, live parsing, Object Store schema, order handling, or deployment identity during the experimental round.

## Validation Gates

Any candidate that follows this proposal must be compared against Candidate B unchanged and must pass these gates before promotion is considered:

- Improve `2007-10-01` to `2008-12-31` crash protection.
- Avoid renewed recovery drag in:
  - `2009-01-01` to `2010-12-31`
  - `2019-07-01` to `2020-12-31`
  - `2021-01-01` to `2022-12-31`
- Preserve strong recent-window behavior in `2023-01-01` to `2026-01-01`.
- Pass a fresh `2026-01-01` to latest holdout check.
- Avoid material increases in orders, fees, turnover, and cash drag.
- Record severe-rule active weeks, entry dates, exit dates, trigger reason, drawdown, breadth, VIX average, final targets, and recovery capture after exit.
- Count false positives: activations outside true systemic crash conditions.

## Live-Safety Constraints

- New severe-crash experimentation must be disabled by default.
- Experimental parameters must remain non-live/backtest-only until separately approved.
- Do not add a new severe-crash overlay or state machine.
- Do not change Object Store keys or persisted state schema during this phase.
- Before any live/default change, require:
  - restart-state tests,
  - Object Store compatibility review,
  - deployment identity update,
  - paper-live rehearsal,
  - independent order-handling review.

## Risks And Open Questions

- The main optimization risk is overfitting a severe trigger to known 2008, 2020, and 2022 behavior.
- The current fixed growth universe remains a separate structural risk and is not solved by defensive trigger tuning.
- The exact normal/control windows for the next validation batch still need to be chosen before cloud runs.
- Local targeted NUnit execution has previously been blocked by existing Lean test-host environment issues, so cloud/log validation remains important.

## First Action

Run Candidate B unchanged from `2026-01-01` to latest available data, with diagnostics enabled, and record the result before any new parameter or code experiment.
