# Balanced Regime Spec Review Round 1 2026-03-29
Date: 2026-03-29

## Scope
- Review only the draft Balanced live-strategy regime spec block provided in chat.
- Focus on safety, drawdown control, failure modes, hidden assumptions, hysteresis quality, and whether the allocation bands are defensible for a Balanced prototype.
- No code changes.

## Change Log
- 2026-03-29: Created round-1 independent regime-spec review note.

## Step Log
- 2026-03-29: Confirmed existing project-note format and opened the current live-strategy design note for consistency.
- 2026-03-29: Performed an independent risk-control review of the draft regime allocation bands and transition rules.

## Summary
- The draft has a sound high-level structure for a prototype: three states, slower upgrades than downgrades, and one-step transitions.
- The current draft is not yet sufficiently defensive for a live Balanced strategy because the Weak regime still allows too much growth exposure and the trigger definitions are not explicit enough to guarantee consistent drawdown control.

## What I Agree With
- Using three states is appropriate for a Balanced prototype. It is simple enough to audit and reduces the risk of overfitting relative to finer-grained regime ladders.
- The chosen inputs are directionally sensible and complementary:
  - trend via SPY relative to its 200-day SMA plus slope,
  - internal breadth/risk appetite via growth-pool participation,
  - stress via short-term VIX.
- Requiring upgrades to be slower than downgrades is the correct bias for drawdown control.
- One-step state transitions are a good anti-whipsaw control and avoid abrupt full-risk re-entry after a short rebound.

## What Is Weak Or Risky
- The Weak allocation band is too permissive for a live Balanced profile. Allowing Growth up to 30 percent in a clearly weak regime can still leave the portfolio carrying meaningful beta while the system itself is admitting trend and participation damage.
- The spec assumes the three accepted inputs are sufficient, but it does not define how conflicting inputs are resolved. Example: SPY slightly above the 200-day SMA while breadth is poor and VIX is elevated. Without a clear precedence rule, weekly decisions can become discretionary or unstable.
- The band widths are large enough that two operators could implement materially different portfolios while claiming compliance. A prototype needs tighter default targets or explicit sub-rules inside each band.
- The Defensive sleeve is underspecified. A 20-40 percent defensive range in Weak may or may not offset residual growth risk depending on what defensive actually contains and how correlated it becomes during equity stress.
- The downgrade rule uses the phrase clearly met, which is not auditable. In live use, ambiguous wording is a failure mode because it invites ad hoc interpretation exactly when markets are deteriorating.
- The hysteresis is only partial. Slower upgrades help, but there is no explicit buffer around threshold boundaries. If the state logic sits near cutoffs, the system can still oscillate weekly between adjacent states.
- Weekly review cadence may be too slow for volatility shock recognition if the Weak state can still retain non-trivial growth exposure. The cadence is acceptable only if the Weak portfolio is materially safer than the current draft implies.
- Cash floors may be too low in Favorable and Neutral for a Balanced live prototype if turnover friction, gap risk, and false-positive regime strength are important concerns.

## Recommended Changes
- Tighten the Weak regime meaningfully:
  - Growth: reduce to 0-20 percent, with a default target near the low end.
  - Defensive: raise to 30-50 percent.
  - Cash: keep 30-60 percent.
- Narrow the Neutral band so it behaves like an actual middle state rather than a wide discretionary zone:
  - Growth: 35-45 percent or at most 35-50 percent.
  - Defensive: 25-35 percent.
  - Cash: 15-25 percent.
- Keep Favorable constructive but still Balanced:
  - Growth: center near 60-65 percent rather than permitting a broad 60-75 percent.
  - Defensive: 15-20 percent.
  - Cash: 10-15 percent.
- Replace clearly met with explicit state-entry logic. The state rule should specify whether Weak is triggered by:
  - all three inputs aligned negatively,
  - two of three with one mandatory input,
  - or a weighted score with fixed cutoffs.
- Add explicit conflict-resolution precedence. My recommendation:
  - stress and breadth should be able to block a Favorable classification even if SPY remains slightly above its 200-day SMA,
  - and a high-stress reading should have veto power against rapid upgrading.
- Add threshold buffers for hysteresis, not just time-based confirmation. Example:
  - require a stronger margin to upgrade than to remain in state,
  - and define separate exit versus re-entry cutoffs for breadth and VIX.
- Consider an accelerated risk-off exception for severe stress. If VIX or breadth deterioration breaches a predefined emergency level, allow a direct move to Weak on that weekly review without waiting for mixed interpretation.
- Define a default target mix inside each state and treat the ranges as limited implementation tolerance, not full discretion.

## Single Most Important Concern
- The single biggest problem is that the Weak regime is not weak enough. In a live Balanced strategy, the state explicitly labeled Weak must materially reduce equity-led drawdown risk; otherwise the regime framework may classify danger correctly but fail to de-risk enough when it matters.

## Open Questions / Risks
- The draft does not define whether the objective is capital preservation first or quicker re-risking after corrections. That choice should drive how aggressive the Weak and upgrade rules are.
- The Defensive sleeve contents remain a major unresolved risk because defensive efficacy depends on instrument selection and correlation behavior during stress.
- If the thresholds behind the accepted inputs are later tuned on a short backtest window, the apparent benefit of the regime framework could be mostly overfit.

## Files Touched
- project-notes/Balanced_Regime_Spec_Review_Round1_2026-03-29.md

## Review Log
- 2026-03-29: Strict post-edit review completed for this note. No issues found in the analysis structure or recorded conclusions. Main conclusion remains: the Weak regime needs materially stronger de-risking and explicit trigger semantics before this is defensible as a live Balanced prototype.


## Round 2 Review
### Scope
- Respond to Reviewer B's critique of the same draft Balanced regime-allocation spec.
- Reassess allocation targets, state separation, defensive-sleeve role, and transition design.

### Reviewer A Response To Reviewer B
#### 1. What I Agree With
- I agree that the allocation bands are too wide for a prototype and should become explicit state targets with only narrow implementation tolerance.
- I agree that state separation is currently too weak, especially between Neutral and Weak. If those two states do not produce clearly different portfolio behavior, the regime framework adds complexity without enough protective value.
- I agree that the defensive sleeve needs a larger and clearer role in Neutral and Weak. In a Balanced live design, the defensive sleeve cannot be a vague residual bucket. It needs to carry more of the burden when Growth is being cut.

#### 2. What I Disagree With Or Still View As Too Lax
- Reviewer B is directionally right, but the proposed Weak target of 20/40/40 is still somewhat lax for a risk-control-first Balanced prototype. Twenty percent Growth in Weak can be acceptable only if the defensive sleeve is genuinely low-beta and the trigger logic is strict. Without that, it still risks carrying too much equity-led drawdown into the weakest state.
- I would also be careful with Favorable at 70/20/10 as a default target. It is defensible, but it pushes the prototype closer to an offensively biased Balanced interpretation. If the stated objective is drawdown-aware live robustness rather than maximizing upside capture, I would prefer a slightly lower default Growth target in Favorable.
- The transition problem is not solved by better targets alone. If thresholds, precedence, and hysteresis remain underdefined, the portfolio can still drift between states too easily even with cleaner target weights.

#### 3. Revised Targets And Transition Refinements I Would Defend
- Favorable target: 65/20/15.
- Neutral target: 45/30/25.
- Weak target: 10/40/50.
- If a slightly more aggressive prototype is required, I could accept 65/20/15, 50/30/20, and 15/40/45. I would not defend a Weak default above 15 percent Growth without stronger evidence.
- Use targets, not broad bands. If tolerances are needed for execution practicality, keep them narrow, such as plus or minus 5 percentage points around each sleeve target.
- Keep one-step transitions.
- Keep upgrades slower than downgrades, but make the rules explicit:
  - downgrade after one weekly review when Weak criteria are met,
  - upgrade only after two consecutive weekly reviews that satisfy the higher state's criteria,
  - and require stronger re-entry thresholds for upgrade than for staying in the current state.
- Add precedence rules:
  - elevated stress should be able to block a Favorable classification,
  - weak breadth plus negative trend should force at least Neutral,
  - severe stress should force Weak regardless of marginal trend improvement.
- Add explicit threshold buffers so Neutral and Weak are separated not only by time confirmation but by meaningfully different trigger levels.

#### 4. Short Reply Back To Reviewer B
- I agree with your core diagnosis: the current draft is too loose, state separation is weak, and the defensive sleeve needs a firmer role. Where I would push further is the Weak state. For a live Balanced prototype, 20 percent Growth in Weak is still on the soft side unless the defensive sleeve is proven to offset it and the trigger logic is explicit. I would tighten the framework into fixed targets with narrow tolerances and make Weak visibly more protective than your proposal.

### Review Outcome
- Round 2 conclusion: Reviewer B's direction is mostly correct, but I would still make the Weak state more defensive and tighten the transition semantics before calling the spec live-safe.

## Files Touched
- project-notes/Balanced_Regime_Spec_Review_Round1_2026-03-29.md

## Round 2 Review Log
- 2026-03-29: Strict post-edit review completed for the round-2 analysis. No issues found in the reasoning consistency or recorded recommendations. Main conclusion remains: Reviewer B is directionally correct, but the Weak target should be more defensive and the transition semantics should be more explicit before this is defensible for live use.

## Round 3 Final Convergence
### Scope
- Converge Reviewer A and Reviewer B positions into a compromise regime-allocation spec for the Balanced prototype.
- Capture any remaining disagreement and a coordinator-facing recommendation.

### Compromise Spec Reviewer A Can Support
- Regime targets should be explicit default weights with narrow tolerances, not broad discretionary bands.
- Favorable target: 65/20/15.
  - Narrow tolerance: Growth 60-70, Defensive 15-25, Cash 10-20.
- Neutral target: 45/30/25.
  - Narrow tolerance: Growth 40-50, Defensive 25-35, Cash 20-30.
- Weak target: 10/40/50.
  - Narrow tolerance: Growth 5-15, Defensive 35-45, Cash 40-55.
- State-classification logic should use an explicit decision table with precedence rules.
- Elevated stress should block Favorable even if trend is marginally positive.
- Severe stress should force Weak regardless of marginal improvement in other inputs.
- Keep one-step transitions only.
- Downgrade after one weekly review when the lower state's criteria are met.
- Upgrade only after two consecutive weekly reviews satisfying the higher state's criteria.
- Add threshold buffers so upgrading requires stronger evidence than merely remaining in state.

### Remaining Disagreement
- The remaining disagreement is minor and mainly about Neutral calibration. Reviewer B is comfortable with a slightly lower Neutral Growth target near 42.5 percent or a broader 35-50 range, while I prefer a cleaner 45/30/25 target with tighter tolerances so Neutral does not drift too close to Weak in live use.

### Final Recommendation To Coordinator
- Adopt explicit state targets with narrow tolerances: Favorable 65/20/15, Neutral 45/30/25, Weak 10/40/50.
- Replace broad bands and subjective wording with a written decision table that defines exactly how trend, breadth, and stress combine.
- Give stress explicit precedence: elevated stress blocks Favorable, severe stress forces Weak.
- Keep one-step transitions and asymmetric hysteresis: one weekly review to downgrade, two consecutive weekly reviews to upgrade.
- Add threshold buffers in addition to time confirmation so upgrades require stronger evidence than staying put.
- Treat the main unresolved judgment call as Neutral calibration only; everything else is now close enough to finalize for a Balanced prototype.

## Round 3 Review Log
- 2026-03-29: Strict post-edit review completed for the round-3 convergence analysis. No issues found in the compromise framing or final recommendations. Main conclusion remains: the converged spec is defensible if it uses explicit targets, explicit precedence, and a genuinely protective Weak state.
