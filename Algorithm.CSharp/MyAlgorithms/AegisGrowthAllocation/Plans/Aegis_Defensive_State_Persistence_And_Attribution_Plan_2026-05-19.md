# Aegis Defensive State Persistence And Attribution Plan

## Summary
The next move is to make the current defensive logic safe for paper/live restart behavior before adding more optimization changes. After that, run an attribution backtest to measure where the current return drag comes from, then decide whether the next improvement should be pre-weak tuning, severe-crash redesign, or universe validation.

This plan should not change allocation behavior in the first phase. The first implementation step is persistence and verification only.

## Phase 1 - Persist Defensive Runtime State

### Goal
Make the defensive high-water mark and severe-crash state survive algorithm restart, redeploy, or live node recovery.

### Tasks
- Add persisted defensive fields to `AegisLiveState`:
  - defensive override equity high-water mark
  - severe-crash active flag
  - severe-crash recovery weeks
  - severe-crash mode state
  - severe-crash exit reason
- Bump the live-state schema version because the persisted state shape changes.
- Restore these fields during startup from live state.
- Include these fields when building the saved live state.
- Include these fields in the state fingerprint so live state is saved when defensive state changes.
- Add restore/save log output showing defensive high-water mark and severe-crash state.
- Keep trading allocation logic unchanged in this phase.

### Deliverable
A restart-safe defensive state implementation with no intended change to backtest allocation behavior.

## Phase 2 - Restart-Safety Tests

### Goal
Prove the new persisted state protects defensive behavior across restart.

### Tasks
- Add a test proving defensive high-water mark restores correctly.
- Add a test proving severe-crash mode state restores correctly.
- Add a test proving defensive state changes trigger a new persisted-state save.
- Confirm old schema behavior is safe and documented.
- Run the Aegis test suite.
- Build the algorithm project.

### Deliverable
Passing tests and build evidence showing defensive state persistence works.

## Phase 3 - Full-Period Diagnostic Backtest

### Goal
Measure the current default defensive behavior across the full validation period before further tuning.

### Backtest setup
- Start: `2016-01-01`
- End: `2026-01-01`
- `crisis-diagnostics=true`
- Use current defaults for defensive parameters.

### Suggested artifact names
- `DiagDefault_2016-2026.json`
- `DiagDefault_2016-2026_orders.csv`
- `DiagDefault_2016-2026_logs.txt`

### Analysis tasks
- Compare weeks where `PreWeakGuardActive=True` against inactive weeks.
- Measure return drag from pre-weak behavior.
- Measure drawdown reduction from pre-weak behavior.
- Count active pre-weak weeks.
- Identify false positives in non-crisis years.
- Measure 4-week, 8-week, and 12-week forward returns after pre-weak activation.

### Deliverable
A documented attribution report that explains whether the current defensive default is helping enough to justify its return drag.

## Phase 4 - Decide The Next Optimization Target

### Goal
Choose the next behavior change based on attribution evidence instead of section-by-section backtest reactions.

### Decision rules
- If pre-weak reduces drawdown but creates too many false positives, tune pre-weak thresholds or defensive sleeve composition.
- If pre-weak is already efficient, redesign severe-crash logic for rare systemic crash conditions only.
- If universe selection explains more performance variation than defensive logic, start the universe validation matrix before more defensive tuning.
- If attribution is inconclusive, run a control backtest with pre-weak disabled and diagnostics enabled before changing behavior.

### Deliverable
One documented next optimization target with evidence and rejected alternatives.

## Phase 5 - Paper Deployment Checklist

### Goal
Confirm the current improved algorithm can be deployed to the Interactive Brokers paper account with clear defaults.

### Tasks
- Confirm no explicit defensive parameters are required for normal paper deployment.
- Confirm `pre-weak-guard-enabled` defaults to `true`.
- Confirm `weak-stress-overlay-enabled` defaults to `false`.
- Confirm `severe-crash-override-enabled` defaults to `false`.
- Confirm `crisis-diagnostics` defaults to `false`.
- After first paper run, verify live logs show defensive state restore/save correctly.

### Deliverable
A paper deployment checklist confirming the algorithm is running with the intended default defensive posture.

## Assumptions
- The immediate priority is live/paper reliability, not adding new return-seeking behavior.
- Existing v1 live state can be reset by the schema bump; broker holdings still reconcile from live holdings.
- No severe-crash or weak-stress overlay default changes are included in this plan.
- Any behavior change after this plan must be documented in `project-notes/` before implementation.
