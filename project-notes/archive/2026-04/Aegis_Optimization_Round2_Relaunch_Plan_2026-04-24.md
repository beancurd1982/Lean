# Aegis Optimization Round 2 Relaunch Plan - 2026-04-24

## Task
- Review the existing Aegis optimization notes.
- Define the current post-warning-investigation baseline.
- Write a short next-round optimization plan without starting implementation.

## Files Reviewed
- `project-notes/Aegis_Optimization_Plan_2026-04-18.md`
- `project-notes/Aegis_Optimization_Round1_Analysis_2026-04-18.md`
- `project-notes/Aegis_Optimization_Round2_Parameterization_2026-04-18.md`
- `project-notes/Aegis_Optimization_Round2_Selection_2026-04-18.md`
- `project-notes/Aegis_Backtest_V5_Review_2026-04-18.md`
- `project-notes/Aegis_Next_Moves_Analysis_2026-04-21.md`
- `project-notes/Aegis_Backtest_Log_Analysis_2026-04-23_V9.md`
- `project-notes/Aegis_OrderFillsWarning_RootCause_2026-04-23.md`
- `project-notes/Aegis_Warning_Fix_Plan_2026-04-21.md`

## Baseline Definition
- Use the current `AegisGrowthAllocation.cs` source as the active baseline.
- This is the `V7` schedule-only algorithm behavior:
  - exchange-aware `WeekStart(_marketSymbol, extendedMarketHours: false)`
  - exchange-aware `AfterMarketOpen(_marketSymbol, minutes, extendedMarketOpen: false)`
  - no V8 deferred execution path
  - no temporary V9 diagnostic instrumentation
- Use the `V9` backtest evidence as the baseline reference point for next-round optimization decisions because it:
  - confirms the current source shape was tested after the warning investigation
  - preserves the strong performance profile of the `V7` baseline
  - provides evidence that the extended-hours warning is likely a false positive

## Baseline Metrics
- Source baseline: current `research-algorithms` branch head after the V9 evidence push.
- Reference backtest: `Logs_V9.json`
- Metrics:
  - CAGR: `18.260%`
  - Sharpe: `0.908`
  - Drawdown: `16.500%`
  - Net Profit: `286.932%`
  - End Equity: `$116,079.66`
  - Total Orders: `1351`

## What The Existing Notes Already Settled
- Round 1 regime tuning selected the `V2` region as the preferred regime baseline:
  - `weak-stress-threshold = 27`
  - `favorable-breadth-threshold = 0.75`
  - `upgrade-confirmation-weeks = 1`
- The prior turnover-control round selected:
  - `replacement-score-gap = 10`
  - `hold-stability-bonus = 2`
  - `growth-atr-eligibility-limit = 0.06`
- The extended-hours warning should not drive further execution changes unless QuantConnect provides contrary evidence.

## Round 2 Relaunch Goal
- Improve risk-adjusted returns and portfolio behavior from the current V9 baseline without reopening the execution-realism workstream.

## Round 2 Scope
- In scope:
  - allocation behavior
  - rebalance sensitivity
  - reserve deployment behavior
  - narrowly targeted regime/allocation parameter tests
- Out of scope:
  - execution-model rewrites
  - live-state persistence changes
  - broad symbol-universe redesign
  - chasing the extended-hours analyzer warning

## Execution Phases

### Phase 1: Freeze The Baseline

Goal:
- Lock the optimization starting point so later results are comparable.

Tasks:
- Record the current baseline source shape as:
  - `V7` schedule-only execution behavior
  - no V8 deferred execution path
  - no V9 temporary diagnostics
- Use `Logs_V9.json` as the reference backtest for the next round.
- Preserve the current baseline metrics as the comparison target for all later candidates:
  - CAGR `18.260%`
  - Sharpe `0.908`
  - Drawdown `16.500%`
  - Net Profit `286.932%`
  - End Equity `$116,079.66`
  - Total Orders `1351`
- Do not reopen execution-realism work unless QuantConnect provides contrary evidence.

### Phase 2: Add Tolerance-Band Rebalance Gating

Goal:
- Reduce unnecessary weekly trading caused by small weight deviations.

Tasks:
- Define the tolerance-band rule for when a holding should be left unchanged.
- Implement the planner/execution gating so small target-versus-current weight gaps do not trigger orders.
- Keep the change isolated from regime selection, ranking, and sleeve construction logic.
- Run a cloud backtest against the same baseline window.
- Compare turnover, total orders, Sharpe, drawdown, and end equity against `V9`.
- Decide whether the tolerance-band change is a keep/revise/revert candidate before moving on.

### Phase 3: Integrate Undeployed Capital Into Targets

Goal:
- Make released reserve affect target allocations instead of remaining a bookkeeping-only value.

Tasks:
- Identify where `releasedReserve` is calculated and where target weights are finalized.
- Wire released reserve into target-weight construction in a minimal, auditable way.
- Preserve the intended defensive behavior in weak regimes.
- Run a cloud backtest against the same baseline window.
- Compare capital usage, cash behavior, Sharpe, drawdown, and end equity against both `V9` and the Phase 2 result.
- Decide whether the reserve integration should remain fixed or become regime-sensitive in a later round.

### Phase 4: Re-Check Allocation Behavior

Goal:
- Confirm the structural changes did not distort intended sleeve behavior.

Tasks:
- Review favorable / defensive / cash sleeve outcomes after the Phase 2 and Phase 3 changes.
- Check whether the strategy still expresses the intended regime posture.
- Review weekly summaries for signs of over-trading, under-deployment, or unintended concentration.
- Confirm the changes are understandable as single-mechanism effects, not bundled behavioral drift.

### Phase 5: Run A Narrow Parameter Batch

Goal:
- Optimize around the stabilized structural design, not around an incomplete planner.

Tasks:
- Build a small test grid around the current winning defaults:
  - `favorable-breadth-threshold`
  - `weak-stress-threshold`
  - undeployed reserve level
  - tolerance-band width
- Keep the search intentionally small so attribution remains clear.
- Rank results by Sharpe, drawdown, end equity, and order count instead of return alone.
- Select one winner and one conservative alternative.
- Confirm the winner with a follow-up cloud backtest before changing defaults in code.

### Phase 6: Baseline Update Decision

Goal:
- Decide whether the optimization round produced a new default baseline.

Tasks:
- Compare the best candidate against the current `V9` reference.
- Keep the new candidate only if it improves or credibly matches risk-adjusted performance without adding operational complexity.
- Record the selected values, rejected alternatives, and open risks in `project-notes/`.
- If no candidate clearly beats the baseline, keep the current defaults and stop the round.

## First Hypothesis To Test
- The current baseline likely gives up some risk-adjusted performance by rebalancing too precisely each week.
- The first implementation round should therefore target tolerance-band rebalance gating before any new regime parameter sweep.

## Acceptance Criteria For The Next Round
- A candidate should beat or credibly match the V9 baseline on Sharpe while keeping drawdown controlled.
- Any change should be explainable by one narrow mechanism, not by multiple bundled behavior changes.
- The next round should not materially increase operational complexity for live trading.

## Risks
- Tolerance bands can reduce turnover but also allow drift that weakens intended sleeve exposures.
- Releasing undeployed reserve into targets can improve capital usage but may raise drawdown in weak regimes.
- If multiple optimization ideas are bundled into one round, attribution will become weak and the result will be hard to trust.

## Recommendation
- Treat `V9` as the working baseline reference.
- Start the next optimization round with tolerance-band rebalance gating.
- After that, address undeployed-capital target integration.
- Only then run a small parameter sweep around the existing regime defaults.
- Do not bundle multiple behavioral changes into a single backtest round unless the earlier phase explicitly requires it.

## Status Update After V10
- `V10` is the first post-implementation cloud backtest for the Round 2 structural changes.
- `V10` improved the main baseline metrics versus `V9`:
  - End Equity: `$119,327.19` vs `$116,079.66`
  - Net Profit: `297.757%` vs `286.932%`
  - Sharpe: `0.929` vs `0.908`
  - Drawdown: unchanged at `16.500%`
  - Total Orders: `1267` vs `1351`
- `V10` used:
  - `UndeployedReserve=0`
  - `ToleranceBandScale=1`
- Interpretation:
  - `V10` is strong evidence that Phase 2 tolerance-band rebalance gating is a keep candidate.
  - `V10` does **not** validate Phase 3 reserve integration because the reserve path was inactive with `UndeployedReserve=0`.
- Working conclusion:
  - keep the current tolerance-band change in the local source baseline
  - do not treat undeployed reserve as a default optimization knob
  - if reserve behavior is tested further, do it only through an explicit staged-cash scenario

## Review
- Strict review completed for this planning note.
- Findings:
  - The plan is consistent with the earlier optimization history and the later warning investigation.
  - It avoids repeating already-resolved execution work.
  - It keeps live-trading risk low by prioritizing narrow, auditable behavior changes over another broad refactor.
  - The plan now has explicit phases and task lists, which should make later execution and review easier to audit.

## Publish
- This note is ready to be committed and pushed as a documentation-only planning update.

## Open Questions
- What tolerance-band width should be the initial hypothesis value?
- Should undeployed reserve stay fixed by regime, or should it become regime-sensitive?
- After the first design-gap fixes, should the next batch optimize allocation sleeves first or regime thresholds first?
- After `V10`, does it still make sense to optimize undeployed reserve at all for the default baseline, or should it remain a scenario-only parameter for staged cash deployment?
