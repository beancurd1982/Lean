# Live Strategy New Algorithm Next Steps 2026-03-29
Date: 2026-03-29

## Scope
- Re-evaluate the next steps using only:
  - `Documentation/QuantConnect_Live_Strategy_Design_Document_V1.md`
  - `Documentation/QuantConnect_Live_Strategy_Design_Derivation_Record.md`
- Treat the design as a brand new algorithm that does not yet exist.
- Do not use `MultiStockV33_Stable_Base.cs` as implementation context or as a migration base.

## Progress Log
- 2026-03-29: User clarified that the V1 design documents are for a brand new algorithm, not an evolution of V33.
- 2026-03-29: Reframed the task to determine the next implementation steps solely from the two design documents.
- 2026-03-29: Extracted the concrete work sequence and the unresolved design choices that still need user confirmation before coding.

## Findings
- The two design documents are aligned. They define a full strategy architecture and explicitly stop short of executable rule detail.
- The architecture already fixed these top-level choices:
  - medium- to low-frequency weekly process,
  - growth-led portfolio,
  - three-state risk regime,
  - defensive sleeve plus cash,
  - small crypto enhancement sleeve,
  - semiannual variable capital inflows managed through an undeployed-capital pool.
- The next work is not brainstorming the architecture again. The next work is translating the architecture into implementation-grade rules.
- The documents also recommend a useful sequencing rule:
  - first decide the prototype variant,
  - then refine that variant into a backtestable specification,
  - then implement code.

## Recommended Next Steps
1. Lock the first prototype target.
   - Recommended default: Balanced.
   - Alternatives still allowed by the design: More Defensive or More Offensive.
2. Convert the V1 design into an implementation specification for that one prototype.
3. Resolve the still-missing rule details before coding:
   - exact growth-stock core pool,
   - exact supplemental-pool admission rules,
   - exact defensive instruments,
   - exact crypto instrument set,
   - exact risk-state inputs, thresholds, and hysteresis logic,
   - exact stock scoring formula and penalties,
   - exact turnover-friction and replacement-cap rules,
   - exact capital-inflow input method and release logic,
   - exact weekly schedule and execution timing.
4. Once the specification is explicit enough to backtest without guesswork, create a brand new strategy class and implement it.
5. After implementation, run backtests and compare actual outcomes against the stated objective function: annualized return, max drawdown, and Sharpe, with drawdown targeted around 15 percent or less.

## Open Questions / Risks
- Prototype choice is still open. Coding before choosing `Balanced`, `More Defensive`, or `More Offensive` would force hidden assumptions into allocation logic.
- The documents do not yet define the exact asset lists or thresholds. Those missing details are important enough that they should be confirmed rather than guessed.
- The risk-state module is the highest-impact missing piece because it controls allocation, crypto activity, defensive posture, and capital deployment.

## Review Log
- 2026-03-29: Strict review completed for this corrected note. No issues found in the updated interpretation. Main conclusion: the next implementation work is specification refinement for a new strategy, not reuse of any existing algorithm.
- 2026-03-29: User selected the `Balanced` prototype as the first implementation target.
- 2026-03-29: Strict review completed after recording the `Balanced` prototype choice. No issues found in the documentation update.
- 2026-03-29: Started rough asset-universe shortlist for the `Balanced` prototype. Goal: propose initial candidate lists for core growth, defensive sleeve, and crypto sleeve before writing the backtestable specification.
- 2026-03-29: Completed rough first-pass candidate universe for the `Balanced` prototype. Included core growth, defensive sleeve, and crypto sleeve proposals, with a note that the final crypto symbols depend on the intended live brokerage integration.
- 2026-03-29: Strict review completed for the rough universe shortlist. No issues found in the shortlist workflow. Residual risk: the final crypto symbol set should be confirmed against the planned QuantConnect live brokerage before implementation.
- 2026-03-29: User asked whether the proposed pool members would always remain in the pools. Started clarification of pool-stability logic based on the V1 design document.
- 2026-03-29: Strict review completed for the pool-logic clarification step. No issues found in the documentation workflow.
- 2026-03-29: User updated the rough universe direction for the `Balanced` prototype.
  - Core growth pool: add `TSLA`.
  - Defensive sleeve candidates: add `JNJ`.
  - Crypto sleeve: remove entirely to simplify the first implementation.
- 2026-03-29: Design-impact note recorded. Removing crypto materially simplifies the original V1 architecture and should be reflected in the next specification draft as a four-layer system: growth, defensive, risk state, and new-capital deployment.
- 2026-03-29: Strict review completed after recording the universe update. No issues found in the note update.
- 2026-03-29: User confirmed that the defensive sleeve may include individual defensive stocks such as `JNJ`.
- 2026-03-29: Prepared the current working candidate set for the first `Balanced` prototype using the latest confirmed direction:
  - Core growth pool includes `TSLA` in addition to the rough first-pass large-cap growth list.
  - Defensive sleeve may include both defensive ETFs and individual defensive stocks, including `JNJ`.
  - Crypto remains removed from the first implementation.
- 2026-03-29: Strict review completed after recording the current working candidate set. No issues found in the note update.
- 2026-03-29: Started research for two additional individual-stock candidates for the defensive sleeve. The shortlist will be based on the current Balanced-prototype direction and verified with current source checks before recommendation.
- 2026-03-29: Recommended two additional individual-stock candidates for the defensive sleeve after current-source review: `PG` and `DUK`.
- 2026-03-29: Strict review completed for the defensive-stock recommendation step. No issues found in the workflow. Residual risk: final investable-universe approval should still consider the intended live brokerage and the user's tolerance for utility-sector rate sensitivity.
- 2026-03-29: User confirmed `PG` and `DUK` as additional individual-stock candidates for the defensive sleeve.
- 2026-03-29: Moved to the next specification phase after confirming the current working universe.
- 2026-03-29: Strict review completed after recording the confirmed defensive additions. No issues found in the note update.
- 2026-03-29: User accepted the first-version growth-pool split for the `Balanced` prototype.
  - Core Growth Pool: `MSFT`, `NVDA`, `AMZN`, `GOOGL`, `META`, `AVGO`, `AAPL`, `COST`
  - Supplemental Growth Pool: `LLY`, `NFLX`, `TSLA`
  - Defensive Sleeve Candidates: `SCHD`, `VIG`, `XLV`, `XLU`, `USMV`, `SGOV`, `JNJ`, `PG`, `DUK`
- 2026-03-29: Strict review completed after recording the accepted pool split. No issues found in the note update.
- 2026-03-29: User asked for a recommendation on the first-version regime model inputs and the reasoning behind them.
- 2026-03-29: Started V1 regime-design recommendation. Focus: choose practical indicators for broad market trend, breadth / risk appetite, and stress / volatility without adding unnecessary complexity.
- 2026-03-29: Strict review completed for the regime-design recommendation step. No issues found in the documentation workflow.
- 2026-03-29: User accepted the recommended V1 regime model inputs.
  - Broad market trend: `SPY` vs `200-day SMA` plus `200-day SMA` slope.
  - Breadth / risk appetite: percentage of confirmed growth-pool names above their own `200-day SMA`.
  - Stress / volatility: `5-day average VIX`.
- 2026-03-29: Moved to the next specification phase: define regime transition rules and target allocation ranges for `Favorable`, `Neutral`, and `Weak`.
- 2026-03-29: Strict review completed after recording the accepted regime model. No issues found in the note update.
- 2026-03-29: User requested a two-agent expert review of the current Balanced-prototype specification details, with independent review first and then several rounds of discussion / argument between the reviewers.
- 2026-03-29: User clarified the delegated-review scope. The two-agent review should focus specifically on the proposed regime-transition logic and state allocation ranges for the Balanced prototype.

- 2026-03-29: Reviewer B started an independent critique of the draft Balanced-prototype regime-allocation spec. Scope limited to regime allocation bands and transition rules using the already accepted regime inputs.

## Reviewer B Critique - Regime Allocation Spec
Date: 2026-03-29
Summary: Independent review of the draft `Favorable / Neutral / Weak` allocation bands and weekly transition rules for the Balanced prototype.
Files touched: `project-notes/Live_Strategy_New_Algorithm_Next_Steps_2026-03-29.md`

### What Reviewer B Agrees With
- The three-state structure is practical for a first prototype. It is easy to explain, easy to monitor, and avoids the false precision of a more granular regime ladder.
- The transition asymmetry is directionally correct. Faster downgrades and slower upgrades are appropriate for a live weekly process.
- One-step state movement is a good guardrail. It reduces whipsaw risk and prevents abrupt full-risk re-expansion after a short-lived improvement.
- Keeping all three sleeves present across regimes is sensible for a Balanced design. It avoids an overly binary risk-on / risk-off behavior.

### What Reviewer B Finds Weak or Impractical
- The allocation bands are too wide for clean implementation. Large ranges such as `Growth 35%-55%` or `Cash 25%-50%` leave too much discretion and make backtest interpretation harder.
- The three states are not separated strongly enough. `Neutral` and `Weak` overlap materially in both growth and defensive exposure, so realized behavior may look too similar across states.
- The defensive sleeve does not clearly act as the stabilizer. In `Weak`, defensive only rises to `20%-40%` while cash can dominate. That can turn the regime system into mostly a cash throttle rather than a balanced reallocation framework.
- The spec does not yet define whether allocations are chosen as fixed targets, midpoint defaults, or secondary-rule outputs within each band. Without that, the bands are not actually executable.
- The downgrade rule says Weak can trigger after one weekly review if clearly met, but "clearly" is not operational. That creates ambiguity exactly where fast action matters most.
- The upgrade rule is simpler, but it still lacks a definition of what counts as consecutive confirmation if signals are mixed or borderline across the three accepted inputs.

### Concrete Changes Reviewer B Recommends
- Tighten the bands substantially and make them state targets with narrow tolerances rather than broad discretionary ranges.
- Increase separation between states so each regime produces visibly different portfolio behavior.
- Make defensive exposure do more work in `Neutral` and `Weak`, instead of relying mainly on higher cash.
- Replace "clearly met" with an explicit rule based on the regime scoring or threshold framework that will later classify the accepted inputs.
- Keep one-step transitions and slower upgrades, but add a documented exception only if the portfolio team explicitly wants crash-mode behavior. Otherwise keep the system simple.

### Reviewer B Suggested Prototype Targets
- Favorable: Growth `70%`, Defensive `20%`, Cash `10%`
- Neutral: Growth `50%`, Defensive `30%`, Cash `20%`
- Weak: Growth `20%`, Defensive `40%`, Cash `40%`
- If bands are required instead of point targets, keep them narrow, for example within about `+/- 5%` around each state target.

### Single Most Important Improvement
- Convert the current broad bands into explicit per-state target weights with narrow tolerance bands. That one change does the most to improve implementability, explainability, testability, and behavioral separation.

### Open Questions / Risks
- If the design intent is for `Weak` to behave mostly as capital preservation through cash, that should be stated directly; otherwise the current ranges understate the role of the defensive sleeve.
- If the portfolio may add sleeve-level ranking or opportunistic tilts later, that should remain a second-order overlay. It should not be hidden inside wide base regime bands.

## Review Log
- 2026-03-29: Strict review completed for the Reviewer B critique entry. No issues found in the note update. Main conclusion: the current state bands are too wide for a clean Balanced prototype and should be converted into explicit per-state targets with narrow tolerances.



- 2026-03-29: Reviewer B started round-2 response to Reviewer A on the draft Balanced-prototype regime-allocation spec. Focus: points of agreement, disagreement, revised targets, and direct reply.

## Reviewer B Response To Reviewer A - Round 2
Date: 2026-03-29
Summary: Reviewer B response to Reviewer A's critique of the draft Balanced-prototype regime-allocation spec.
Files touched: `project-notes/Live_Strategy_New_Algorithm_Next_Steps_2026-03-29.md`

### Reviewer B Agreement With Reviewer A
- `Weak` should be tightened versus the original draft. The original version leaves too much residual growth risk for a state that is supposed to represent a clearly adverse backdrop.
- The phrase `clearly met` is too ambiguous for a live weekly process and should be replaced with explicit classification and precedence logic.
- Breadth and stress should be able to block a move into `Favorable`. That is consistent with keeping the regime model from being dominated by a single market-index trend input.
- High stress should slow or veto rapid upgrades. That is directionally correct for a weekly system where reversals can be noisy.
- Hysteresis should include threshold buffers, not only time confirmation. Time alone is not enough to control churning near regime boundaries.

### Reviewer B Partial Disagreement Or Reservations
- Reviewer A's `Weak` growth band of `0%-20%` is directionally reasonable but should not default too close to zero for a first Balanced prototype. That begins to push the design toward a defensive or tactical-preservation model rather than a balanced-growth model with regime control.
- Allowing `cash` up to `60%` in `Weak` is practical only if the explicit design goal is aggressive capital preservation. For a Balanced prototype, that upper end is probably too high as the default state behavior.
- Favorable centered at only `60%-65%` growth is slightly too muted if the model is intended to earn its return mostly from the growth sleeve in constructive conditions. A balanced design still needs a meaningful risk-on expression when the regime is genuinely supportive.

### Reviewer B Revised Targets After Seeing Reviewer A's Proposal
- Favorable: Growth `65%`, Defensive `20%`, Cash `15%`
- Neutral: Growth `42.5%`, Defensive `30%`, Cash `27.5%`
- Weak: Growth `10%`, Defensive `40%`, Cash `50%`
- If bands are retained instead of point targets:
  - Favorable: Growth `60%-70%`, Defensive `15%-25%`, Cash `10%-20%`
  - Neutral: Growth `35%-50%`, Defensive `25%-35%`, Cash `20%-35%`
  - Weak: Growth `5%-15%`, Defensive `35%-45%`, Cash `40%-55%`

### Reviewer B Transition Refinements
- Replace narrative state descriptions with an explicit regime decision table using the already accepted inputs: trend, breadth, and stress.
- Use precedence rules so severe stress can cap the best possible state and broad weakness can prevent a `Favorable` reading even when `SPY` trend is positive.
- Require both time confirmation and threshold buffers for upgrades. Example principle: the signal must not just cross the line; it must clear the line by a meaningful margin and remain there for two consecutive weekly reviews.
- Keep downgrades faster than upgrades, but still use buffered thresholds so a one-week downgrade only happens when the weak reading is decisively outside the neutral zone.
- Keep one-step transitions as the default rule.

### Reviewer B Main Position After Round 2
- Reviewer A is right on the need for explicit logic, stress-based veto power, and buffered hysteresis.
- Reviewer A is directionally right that the original `Weak` state was too permissive.
- Reviewer B would still defend a slightly less defensive stance than A in `Favorable`, and would avoid making `Weak` so cash-heavy that the Balanced prototype stops behaving like a balanced strategy.

## Review Log
- 2026-03-29: Strict review completed for Reviewer B's round-2 response entry. No issues found in the note update. Main conclusion: adopt A's structural refinements to transition logic, but keep the final allocation targets slightly less defensive than A's most conservative suggestion.

- 2026-03-29: Reviewer B started round-3 final convergence response on the Balanced-prototype regime-allocation spec. Focus: compromise spec, remaining disagreement, and final recommendation to the coordinator.

## Reviewer B Final Convergence Response - Round 3
Date: 2026-03-29
Summary: Reviewer B final compromise position after Reviewer A's latest allocation and transition recommendations.
Files touched: `project-notes/Live_Strategy_New_Algorithm_Next_Steps_2026-03-29.md`

### Best Compromise Spec Reviewer B Supports
- Use fixed regime targets with narrow tolerances rather than broad bands.
- Target allocations:
  - Favorable: Growth `65%`, Defensive `20%`, Cash `15%`
  - Neutral: Growth `45%`, Defensive `30%`, Cash `25%`
  - Weak: Growth `10%`, Defensive `40%`, Cash `50%`
- Narrow tolerance rule for implementation and drift control:
  - Default tolerance around each sleeve target: about `+/- 5%`
  - Keep total regime identity stable; do not use the tolerance band as a hidden discretionary overlay.

### Transition Rules Reviewer B Supports
- Keep one-step transitions only: `Favorable -> Neutral -> Weak` and `Weak -> Neutral -> Favorable`.
- Use an explicit regime decision table based on the already accepted inputs: market trend, breadth, and stress.
- Use explicit precedence rules so stress can cap the best possible state and weak breadth can block `Favorable` even when trend is positive.
- Require harder thresholds for upgrades than for downgrades.
- Require two consecutive weekly confirmations for upgrades.
- Allow a one-review downgrade only when the lower-state condition is decisively met outside the hysteresis buffer.
- Stress veto: high stress blocks entry into `Favorable` and should also block rapid upgrades out of `Weak`.
- Hysteresis rule: thresholds must include buffers so signals need to clear a boundary by a meaningful margin before a state change is allowed.

### Remaining Disagreement
- Only a small residual disagreement remains: whether `Neutral` should stay at `45/30/25` or move to a slightly more aggressive `50/30/20`.
- Reviewer B prefers `45/30/25` as the cleaner first-version Balanced default because it creates better separation from `Favorable` and reduces ambiguity in the middle state.

### Reviewer B Final Recommendation To Coordinator
- Adopt fixed targets, not broad ranges.
- Set the first-version targets to `65/20/15`, `45/30/25`, and `10/40/50` for `Favorable`, `Neutral`, and `Weak`.
- Lock in explicit precedence rules before any implementation detail is drafted.
- Let high stress veto `Favorable` and slow upgrades.
- Make upgrade thresholds stricter than downgrade thresholds, with two weekly confirmations for upgrades.
- Keep `50/30/20` for `Neutral` only as an optional later aggressiveness variant, not the default.

## Review Log
- 2026-03-29: Strict review completed for Reviewer B's round-3 convergence entry. No issues found in the note update. Main conclusion: the best compromise is now narrow fixed targets with stress-based precedence and asymmetric hysteresis, using `45/30/25` as the default neutral state.
- 2026-03-29: Completed two-agent expert review focused on the regime-transition and state-allocation spec.
- 2026-03-29: Independent review round result:
  - Both reviewers agreed the original broad allocation bands were too wide for an implementable prototype.
  - Both reviewers agreed the original `Weak` state was too permissive.
  - Both reviewers agreed `clearly met` must be replaced with explicit regime logic, precedence rules, and buffered hysteresis.
- 2026-03-29: Multi-round discussion / rebuttal result:
  - Consensus moved to fixed state targets with narrow tolerances.
  - Consensus default targets: `Favorable 65/20/15`, `Neutral 45/30/25`, `Weak 10/40/50` for `Growth / Defensive / Cash`.
  - Consensus transition rules: one-step moves only; one weekly review for decisive downgrades; two consecutive weekly confirmations for upgrades; stress can block `Favorable`; severe stress can force `Weak`; threshold buffers should be stronger for upgrades than for downgrades.
  - Residual minor disagreement only on whether `Neutral` could be slightly more aggressive (`50/30/20`), but both reviewers recommended `45/30/25` as the safer default for the Balanced prototype.
- 2026-03-29: Strict review completed for the delegated-review workflow. No issues found in the coordination process. Main conclusion: the regime/allocation spec is now mature enough to proceed to explicit decision-table drafting.
- 2026-03-29: Started documentation revision task. Goal: update the design markdown files to reflect the current agreed first-version Balanced prototype, including pool selections, removal of crypto, and the revised regime allocation / transition spec.
- 2026-03-29: Completed documentation revision for:
  - `Documentation/QuantConnect_Live_Strategy_Design_Document_V1.md`
  - `Documentation/QuantConnect_Live_Strategy_Design_Derivation_Record.md`
- 2026-03-29: Applied the following updates to the active design document:
  - removed crypto from the first-version architecture,
  - changed the architecture from five layers to four layers,
  - added the confirmed core growth pool, supplemental pool, and defensive sleeve candidates,
  - replaced broad regime bands with fixed state targets and narrow tolerances,
  - updated transition principles to reflect explicit asymmetric hysteresis and stress precedence,
  - updated next-step guidance to focus on decision-table drafting and implementation specification.
- 2026-03-29: Applied the following updates to the derivation record:
  - preserved the original reasoning trail,
  - added later refinement notes where relevant,
  - appended a post-V1 refinement addendum capturing the current first implementable Balanced prototype.
- 2026-03-29: Strict documentation review completed. No blocking issues found. Residual note: the derivation record intentionally still contains historical references to crypto in earlier sections, because it is a reasoning-history document; the later refinement sections and addendum explicitly supersede that earlier path for the current implementation.
- 2026-03-29: Completed documentation revisions to align the design files with the current accepted Balanced prototype.
  - Files touched: `Documentation/QuantConnect_Live_Strategy_Design_Document_V1.md`, `Documentation/QuantConnect_Live_Strategy_Design_Derivation_Record.md`
  - Summary: removed crypto from the first implementation path; updated the architecture to a four-layer first version; added confirmed core / supplemental / defensive candidate lists; updated regime inputs; replaced broad allocation ranges with default fixed state targets and narrow tolerances; tightened transition principles; refreshed next-step guidance.
- 2026-03-29: Strict documentation review completed after the revisions.
  - Result: no blocking consistency issues found.
  - Residual note: the derivation record now preserves both the original reasoning path and a post-V1 refinement addendum, which is intentional because that file serves as a design-history document.
- 2026-03-29: Applied final consistency cleanup after the main documentation revision.
  - Files touched: `Documentation/QuantConnect_Live_Strategy_Design_Document_V1.md`, `Documentation/QuantConnect_Live_Strategy_Design_Derivation_Record.md`
  - Summary: corrected remaining wording drift around the current four-layer architecture and the revised defensive-layer wording in the Weak state.
- 2026-03-29: Final strict documentation review completed.
  - Result: no blocking inconsistencies found in the revised markdown files.
  - Open note: `project-notes/Balanced_Regime_Spec_Review_Round1_2026-03-29.md` was generated during the delegated review workflow and remains as supporting review documentation.
- 2026-03-29: Completed documentation edits in `Documentation/QuantConnect_Live_Strategy_Design_Document_V1.md` and `Documentation/QuantConnect_Live_Strategy_Design_Derivation_Record.md` to reflect the current Balanced-prototype direction.
- 2026-03-29: Final strict documentation review completed after the last consistency cleanup.
  - Result: no blocking inconsistencies found in `Documentation/QuantConnect_Live_Strategy_Design_Document_V1.md` or `Documentation/QuantConnect_Live_Strategy_Design_Derivation_Record.md`.
  - Validation method: direct markdown inspection of the revised architecture, pool, regime, defensive-layer, and next-step sections.
  - Testing: none run, because this was a documentation-only revision.
- 2026-03-29: User requested commit and push of the current documentation changes.
- 2026-03-29: Started commit-preparation review to confirm the exact file set before creating the commit.
- 2026-03-29: Commit scope reviewed before staging.
  - Intended commit files: `Documentation/QuantConnect_Live_Strategy_Design_Document_V1.md`, `Documentation/QuantConnect_Live_Strategy_Design_Derivation_Record.md`, `project-notes/Live_Strategy_New_Algorithm_Next_Steps_2026-03-29.md`, `project-notes/Balanced_Regime_Spec_Review_Round1_2026-03-29.md`.
  - Intentionally excluded from this commit as unrelated or superseded: `project-notes/V33_Backtest_Review_2026-03-28.md`, `project-notes/Live_Strategy_Design_Next_Steps_2026-03-29.md`.
- 2026-03-29: Strict review completed for commit-scope selection. No issues found in the staging plan.
