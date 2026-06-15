---
id: AEGIS-PLAN-2026-06-15-CANDIDATEC-PREWEAK-RECOVERY-V2
type: implementation-plan
status: proposed
date: 2026-06-15
topic: AegisGrowthAllocation
tags: [aegis, candidate-c, preweak, recovery, implementation-plan, live-safety]
related:
  - project-notes/archive/2026-06/Aegis_PreWeak_RecoveryStep_Proposal_Review_2026-06-15.md
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeak_Recovery_v2_Second_Round_Review_2026-06-15.md
  - project-notes/archive/2026-06/Aegis_Next_Optimization_Strategy_2026-06-14.md
  - project-notes/archive/2026-06/Aegis_CandidateB_Diagnostic_Stress04_2021_2022_Analysis_2026-06-14.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.LiveState.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/LiveStateStore.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs
  - Tests/Algorithm/AegisGrowthAllocationTests.cs
---

# Candidate C PreWeak Recovery v2 Implementation Plan - 2026-06-15

## Agent Summary

Read this before implementing Candidate C. This plan is documentation-only and has not been implemented. Candidate C must be built as a default-off research feature that preserves Candidate B behavior unless explicitly enabled.

## Summary

Build Candidate C as a default-off research feature.

Candidate B remains unchanged:

- Normal PreWeak: `G12/D30/C58`
- Weak: `G10/D40/C50`

Candidate C adds a recovery-only PreWeak sleeve:

- Recovery PreWeak: `G16/D30/C54`

Recovery is allowed only after PreWeak has already formed a local drawdown trough and conditions improve. This is a bounded research candidate, not a default promotion.

## Key Implementation Changes

Add default-off parameters in `StrategyConfig.cs`:

- `pre-weak-recovery-enabled`, default `false`
- `pre-weak-recovery-growth-target`, default `0.16`
- `pre-weak-recovery-def-target`, default `0.30`
- `pre-weak-recovery-dd-improvement`, default `0.02`
- `pre-weak-recovery-max-dd`, default `0.08`
- `pre-weak-recovery-confirmation-weeks`, default `2`

Add PreWeak recovery state in `AegisGrowthAllocation.cs`:

- `segmentId`
- `localTroughDrawdown`
- `confirmationWeeks`
- `recoveryActive`
- `lastResetReason`

Evaluation order:

- First compute normal Candidate B PreWeak eligibility using existing logic.
- If normal PreWeak is not active, reset recovery state before target selection.
- If normal PreWeak is active, update recovery state before target selection.
- Recovery sleeve replaces normal PreWeak only when all recovery gates pass.

Recovery gates:

- normal PreWeak active;
- active regime is not `Weak`;
- `TrendState != Weak`;
- `BreadthState != Weak`;
- `StressState != Weak`;
- `localPreWeakTroughDrawdown - currentDrawdown >= 0.02m`;
- `currentDrawdown <= 0.08m`;
- all gates hold for `2` consecutive weekly reviews.

Reset rules:

- reset before target selection on Weak regime entry;
- reset on any Weak signal;
- reset on new local PreWeak trough;
- reset when PreWeak becomes inactive.
- New PreWeak segment starts when PreWeak transitions from inactive to active.

Live persistence:

- Add recovery fields to `AegisLiveState`.
- Include recovery fields in `BuildPersistedState`, `RestorePersistedRuntimeState`, normalization, logging, and fingerprint.
- Keep current `LiveStateSchemaVersion` and key for this default-off research feature to avoid discarding existing live state; require a separate schema/key migration review before any live/default promotion.

Diagnostics:

- Extend `[AEGIS-DIAG-WEEK]` with recovery fields: segment id, trough drawdown, recovery amount, confirmation count, recovery active, reset reason, final sleeve.
- Extend `[AEGIS-DIAG-SUMMARY]` to split Normal PreWeak vs Recovery PreWeak weeks and forward 1/4/8/12-week reads.
- Add whipsaw diagnostic: worst return/drawdown after recovery activation until recovery exit or PreWeak exit.

## Test Plan

Unit tests:

- recovery disabled preserves Candidate B sleeve behavior;
- recovery cannot activate unless normal PreWeak is active;
- recovery requires trend, breadth, and stress all not Weak;
- recovery requires exact drawdown math: `localTroughDrawdown - currentDrawdown >= 0.02m`;
- recovery rejects absolute drawdown above `0.08m`;
- recovery requires 2 consecutive weekly confirmations;
- new trough resets confirmation and active state before target selection;
- Weak regime, any Weak signal, and PreWeak exit reset recovery state;
- live state persists/restores recovery fields and fingerprint changes when they change;
- diagnostics include recovery fields and split Normal vs Recovery PreWeak summary.

Verification commands:

- `dotnet build Tests/QuantConnect.Tests.csproj -nologo`
- targeted Aegis tests if the local test host is stable; otherwise record the known SGX/Python test-host blocker.

## Backtest Validation

First run Candidate B baseline and Candidate C v2 on identical `2021-01-01` to `2022-12-31` settings.

Candidate C v2 passes the stress gate only if:

- net profit is within `1.0pp` of Candidate B or better;
- max drawdown is no more than `0.5pp` worse;
- PSR drop is no worse than `5pp`;
- no unexplained order/turnover spike;
- zero recovery activation before the first baseline Weak regime entry on `2022-05-16`.

Only if the stress gate passes, run `2016-01-01` to latest available data.

Long-run pass requires material improvement without more than `1.0pp` drawdown degradation and without relying on one recovery episode.

No paid optimization until Candidate C v2 preserves `2021-2022` stress behavior.

## Assumptions

- Candidate C is default off and must be explicitly enabled for backtests.
- `G16/D30/C54` is the only first implementation target; do not implement `G18` yet.
- This feature is research-only until backtest evidence and a separate live-safety review support promotion.
- Existing unrelated untracked files, `.rtk/` and `CLAUDE.md`, stay untouched.

## Verification

- This is a planning note only.
- No source implementation was performed when this note was created.
