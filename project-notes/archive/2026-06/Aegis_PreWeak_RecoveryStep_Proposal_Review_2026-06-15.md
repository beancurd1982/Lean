---
id: AEGIS-DEC-2026-06-15-PREWEAK-RECOVERY-STEP-REVIEW
type: decision-review
status: reviewed
date: 2026-06-15
topic: AegisGrowthAllocation
tags: [aegis, pre-weak, recovery, allocation, stress-protection, candidate-c]
related:
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeak_Recovery_v2_Implementation_Plan_2026-06-15.md
  - project-notes/archive/2026-06/Aegis_Next_Optimization_Strategy_2026-06-14.md
  - project-notes/archive/2026-06/Aegis_CandidateB_Diagnostic_Stress04_2021_2022_Analysis_2026-06-14.md
  - project-notes/archive/2026-06/Aegis_OptS07A_Stress04_2021_2022_Backtest_Analysis_2026-06-14.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.LiveState.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/LiveStateStore.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs
---

# Aegis PreWeak Recovery Step Proposal Review - 2026-06-15

## Agent Summary

Read this before implementing Candidate C. The recovery-gated PreWeak idea is directionally sound, but the first source-level experiment should be more conservative and more diagnostic than the proposed `G18/D30/C52` recovery sleeve. The recommendation is revise, not reject.

## Review

Candidate B should remain the stress baseline. The compact `2021-2022` diagnostics showed PreWeak forward returns were negative through 12 weeks, so raising fixed PreWeak growth is not justified. A recovery-gated sleeve is a reasonable next hypothesis because it preserves Candidate B during active stress and tests whether the algorithm is late to re-risk after conditions improve.

The proposed `G18/D30/C52` sleeve is too large as the first experimental step. It raises growth by 6 points from Candidate B while leaving defensive unchanged, so all incremental exposure comes out of cash. Because `BuildPreWeakGuardSleeveTargets` gives PreWeak growth a tolerance band of target minus 4 points to target plus 6 points, `G18` can tolerate up to about `24%` growth before being out of band. That approaches the failed OptS07A/OptS09A high-growth PreWeak region in mechanics even though the stated target looks moderate.

Drawdown recovery is a valid gate, but it is not sufficient by itself. A 2 point recovery from the worst PreWeak drawdown can occur inside an ongoing bear-market rally, especially if the absolute drawdown remains high. It should be paired with an absolute drawdown cap or an improving-trend requirement so the recovery state does not re-risk during a lower-high bounce.

Breadth and stress conditions are useful but incomplete. Requiring `StressState != Weak` and `BreadthState != Weak` avoids the worst states, but it still allows Neutral stress and Neutral breadth while trend may remain weak. Since current PreWeak activation already fires when trend, breadth, or stress is not favorable, the recovery gate should not ignore trend. At minimum require trend not Weak; preferably require either trend not Weak plus breadth/stress not Weak, or two of three signals improving versus the prior weekly check.

The cash/defensive split should not be held fixed without a control. Leaving defensive at `30%` makes Candidate C a cash-to-growth experiment, not a defensive-quality experiment. That is acceptable only if explicitly framed that way. A safer first sleeve is `G16/D30/C54`, or a paired test of `G16/D30/C54` versus `G16/D34/C50` to separate cash drag from defensive replacement risk.

## Safer Revised Candidate C

- Keep Candidate B behavior unchanged while stress is active.
- First recovery sleeve: `G16/D30/C54`, not `G18/D30/C52`.
- Recovery trigger: still PreWeak, not Weak regime, `StressState != Weak`, `BreadthState != Weak`, `TrendState != Weak`, drawdown improved at least 2 points from the local PreWeak trough, and absolute drawdown is no worse than 7% to 8%.
- Require 2 consecutive weekly confirmations and reset confirmation if any required signal becomes Weak or drawdown makes a new PreWeak trough.
- Emit diagnostics that separately count baseline PreWeak weeks, recovery PreWeak weeks, recovery entries, failed confirmations, recovery exits, average targets, and forward 4/8/12-week returns for each group.

## Validation Gates

Run `2021-2022` first against Candidate B. Candidate C should stay close on net return, max drawdown, Sharpe, PSR, orders, and fees before any `2016-2026` run. The pass/fail read should include recovery-week attribution, not only headline metrics, because a small improvement could come from noise or one rebound segment.

Only if `G16` preserves stress behavior should `G18` be tested. Paid optimization should remain deferred until the recovery definition and diagnostics prove decision-useful.

## Recommendation

Revise. Implement the recovery concept only as a conservative, pre-registered stress-preservation test. Do not start with `G18/D30/C52` as the sole Candidate C.

## Round 2 Review

The revised Candidate C v2 proposal is acceptable as the final research proposal, with minor implementation edits before coding. It incorporates the safer `G16/D30/C54` sleeve, trend/breadth/stress gates, absolute drawdown cap, reset rules, live-state concern, diagnostics, and Candidate B-relative validation gates.

Remaining required edits:

- Define evaluation order explicitly: first compute whether normal PreWeak would be active under Candidate B rules, then decide whether the recovery sleeve replaces the normal PreWeak sleeve.
- Persist recovery state in live mode rather than relying on deterministic rebuild unless a separate design proves rebuild equivalence. Include local trough, confirmation count, active flag, segment id or episode start, last reset reason, and fingerprint/schema coverage.
- Define drawdown math exactly as `localPreWeakTroughDrawdown - currentDrawdown >= 0.02m`, with a small tolerance for new-trough comparisons.
- Keep reset semantics conservative: any Weak signal or new trough resets confirmation and recovery-active immediately before target selection for that weekly review.
- Pre-register "main falling-stress phase" using dates or objective diagnostics before inspecting Candidate C results.

Final recommendation: accept with minor edits. Do not promote defaults or run paid optimization from v2; use it only as a bounded source-level stress-preservation test.

## Verification

No code or backtest was run for this review. The review is based on the recorded Candidate B compact diagnostics, current sleeve/guard mechanics in source, and the revised Candidate C v2 proposal.

## Risks And Open Questions

- Recovery logic will affect live allocation behavior if promoted, so source changes require separate code review and backtest evidence before default use.
- Any recovery state that can affect allocation after restart must either be persisted in `AegisLiveState` and included in the live-state fingerprint, or be proven derivable from persisted state and current history. Current persistence covers defensive high-water and severe-crash state, but not a PreWeak recovery episode, local trough, or confirmation counter.
- The proposal still needs an explicit definition of local PreWeak trough tracking across restarts before any live/paper promotion.
- It remains unknown whether rebound drag is mainly cash drag, defensive sleeve composition, trend delay, or weekly rebalance cadence.
