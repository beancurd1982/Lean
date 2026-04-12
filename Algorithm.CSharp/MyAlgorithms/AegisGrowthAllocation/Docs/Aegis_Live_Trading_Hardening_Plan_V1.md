# Aegis Live Hardening Plan V1

## Summary

Purpose:

- harden `AegisGrowthAllocation` for Interactive Brokers live trading
- add ObjectStore persistence and broker-first restart recovery
- make restart behavior safe, deterministic, and auditable
- implement this in staged phases so each phase is testable and reviewable before moving on

## Phase 1: Persistence Foundation

Goal:
Define and wire the minimum persisted live state model without changing trading behavior yet.

Tasks:

- Add a versioned Aegis ObjectStore state schema and fixed state key.
- Create persisted fields for:
  - schema version
  - saved timestamp
  - active regime
  - regime upgrade confirmation count
  - undeployed reserve
  - last completed weekly review timestamp
  - last planned target weights by ticker
  - broker holdings snapshot by ticker
  - tracked open orders by order id and ticker
- Add load/save helpers with fingerprint-based no-op save skipping.
- Add safe load behavior for missing, null, corrupt, or version-mismatched state.
- Add focused live-only logging for load/save results.

Acceptance:

- Aegis can load and save ObjectStore state in live mode without changing order behavior.
- Missing or bad state does not crash initialization.

## Phase 2: Regime State Recovery

Goal:
Make regime hysteresis restart-safe.

Tasks:

- Extend `RegimeModel` to expose and restore:
  - active regime
  - upgrade confirmation count
- Add a restore path used only during live startup.
- Ensure runtime regime state is seeded from ObjectStore before the first weekly review.
- Keep backtest behavior unchanged when no restored state exists.

Acceptance:

- A live restart preserves regime state instead of resetting to default.
- Backtest path remains functionally unchanged.

## Phase 3: Broker-First Startup Reconciliation

Goal:
Make IB the source of truth on restart and rebuild Aegis runtime state safely.

Tasks:

- Add live startup reconciliation after symbols/indicators are initialized.
- Read broker holdings from `Portfolio` for all tracked Aegis symbols.
- Read open broker orders from `Transactions.GetOpenOrders()`.
- Compare broker state with persisted state.
- If broker and store differ, log the mismatch and replace runtime truth with broker truth.
- Rebuild in-memory current holdings snapshot and tracked open-order state from broker reality.
- Mark startup reconciliation complete only after this pass succeeds.
- Do not place any orders during startup.

Acceptance:

- Restart with existing IB holdings does not trigger immediate rebalance.
- Restart with mismatched ObjectStore state resolves to broker truth cleanly.

## Phase 4: Live Weekly Review Safety Gates

Goal:
Prevent unsafe weekly actions when startup or open-order state is not clean.

Tasks:

- Gate weekly review on:
  - warmup complete
  - startup reconciliation complete
  - no tracked open Aegis orders
- Keep live decision time at Monday 10:00 ET.
- Use broker-derived current holdings state as the current portfolio input to planning.
- Leave open orders untouched on restart and skip new weekly actions until a later scheduled review.
- Persist updated state after a successful weekly plan submission path.

Acceptance:

- If open Aegis orders exist, weekly review logs a skip and submits nothing.
- If state is clean, weekly review behaves normally.

## Phase 5: Order Lifecycle Persistence

Goal:
Keep persisted live state synchronized with actual order progress.

Tasks:

- Add `OnOrderEvent` handling for tracked Aegis orders.
- Track submitted, partially filled, filled, canceled, and invalid orders.
- Update tracked open-order state on each meaningful event.
- Save ObjectStore state when fills or cancellations materially change live state.
- Clear no-longer-open tracked orders promptly.
- Save final state in `OnEndOfAlgorithm`.

Acceptance:

- Restart after partial fills or pending orders restores meaningful runtime context.
- State writes are idempotent and not excessively noisy.

## Phase 6: Validation and Live Readiness Review

Goal:
Prove the live-mode design is safe enough to move to paper/live validation.

Tasks:

- Run focused scenario validation for:
  - empty ObjectStore + no holdings
  - empty ObjectStore + existing holdings
  - valid ObjectStore + matching holdings
  - valid ObjectStore + mismatched holdings
  - startup with open orders
  - restart after a weekly rebalance
  - corrupt or incompatible stored state
  - regime hysteresis restore
- Perform a strict review focused on:
  - startup no-trade safety
  - broker-first reconciliation correctness
  - ObjectStore failure handling
  - open-order gating behavior
- Record residual risks and any live-run operational checklist.

Acceptance:

- No startup orders are placed.
- Broker-first recovery works in all planned restart scenarios.
- Review explicitly signs off on persistence, restart, and order-handling behavior.

## Implementation Notes

Key additions:

- new Aegis live-state persistence model and helper
- `RegimeModel` restore interface
- live-only startup reconciliation flow in `AegisGrowthAllocation`
- live order-event tracking and ObjectStore save triggers

Recommended file placement:

- keep the plan doc in `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/Docs/`
- keep persistence types/helper in the Aegis algorithm folder in `LiveStateStore.cs`

## Test Plan

Minimum validation set:

- restore from empty store
- restore from corrupted store
- restore from matching broker/store state
- restore from mismatched broker/store state
- restore with open orders
- confirm no startup rebalance
- confirm weekly skip when open orders exist
- confirm regime state survives restart
- confirm `OnOrderEvent` updates persistence correctly
- confirm `OnEndOfAlgorithm` saves final state

## Assumptions

- Broker is the source of truth.
- Aegis remains weight-based, not lot-based.
- Startup behavior is reconcile-and-wait, not reconcile-and-trade.
- Open orders are preserved, not canceled automatically.
- Monday 10:00 ET remains the live weekly review time.
