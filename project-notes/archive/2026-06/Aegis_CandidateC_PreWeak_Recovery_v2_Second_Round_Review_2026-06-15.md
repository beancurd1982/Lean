---
id: AEGIS-DEC-2026-06-15-CANDIDATEC-PREWEAK-RECOVERY-V2-REVIEW
type: decision-review
status: accepted-with-minor-edits
date: 2026-06-15
topic: AegisGrowthAllocation
tags: [aegis, candidate-c, preweak, recovery, validation, live-safety]
related:
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeak_Recovery_v2_Implementation_Plan_2026-06-15.md
  - project-notes/archive/2026-06/Aegis_PreWeak_RecoveryStep_Proposal_Review_2026-06-15.md
  - project-notes/archive/2026-06/Aegis_Next_Optimization_Strategy_2026-06-14.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.LiveState.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs
---

# Candidate C PreWeak Recovery v2 Second-Round Review - 2026-06-15

## Agent Summary

Read this before implementing Candidate C v2. The revised proposal is accepted as a final research proposal with minor edits: define drawdown/recovery timing exactly, make live-state rebuild/persistence deterministic rather than optional, and predefine the validation meaning of "main falling-stress phase."

## Review

Candidate C v2 addresses the material first-round objections. The recovery sleeve is reduced to `G16/D30/C54`, all three signal states must avoid Weak, absolute drawdown must be no worse than `8%`, and recovery state resets on Weak/state deterioration, new trough, or PreWeak exit. This is now a conservative, falsifiable stress-preservation test rather than a disguised high-growth PreWeak retest.

The proposal still needs minor specification edits before implementation:

- Define drawdown units and timing: use the same weekly decision timestamp as the PreWeak guard; a 2 percentage point recovery means drawdown improves by `0.02` absolute from the local PreWeak trough.
- Define the PreWeak segment: increment segment id when PreWeak transitions inactive to active; reset local trough and confirmation at segment start.
- Resolve persistence explicitly: either persist all recovery state fields with versioning, or rebuild them deterministically from persisted high-water/regime history and recent weekly observations. Do not leave this as an implementation choice after backtests begin.
- Define validation "main falling-stress phase" without hindsight in the pass/fail sheet. At minimum, no recovery activation before a segment has made a local trough, then completed two eligible weekly checks with no Weak signal and absolute drawdown <= `8%`.
- Add a whipsaw diagnostic: worst return/drawdown after recovery activation until recovery exit or PreWeak exit.

## Recommendation

Accept with minor edits. Do not reject and do not reopen parameter optimization before this v2 fixed-candidate test.

## Verification

No code or backtest was run for this review.

## Risks And Open Questions

- The `2pp` recovery and `8%` absolute drawdown gates remain plausible but unproven; they must be treated as pre-registered research parameters, not defaults.
- If recovery activations are rare, long-window improvement may be one-period dependent even if headline metrics improve.
- Live promotion requires separate review of restart/state behavior.
