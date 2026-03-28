# MultiStockV33_Stable_Base Hardening Plan
Date: 2026-03-27

## Context
`Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs` is intended to become the new base algorithm for the next paper-trading cycle after the current V32 paper test is retired and the paper account is reset.

The objective of this plan is to harden V33 for paper deployment first, with a strong focus on live-safety behavior, state consistency, restart recovery, and operational observability. This plan does not expand the symbol universe yet; the current 12-symbol configuration remains the baseline.

## User Confirmations
- V33 should use a new `ObjectStore` key and should not migrate V32 state.
- If tracked lots become inconsistent, the preferred recovery path is to rebuild tracked lots from current holdings and open orders.
- Keep the current V33 symbol universe unchanged for now.

## Primary Goals
1. Make V33 safe to restart and recover during paper/live-style operation.
2. Prevent lots from getting stuck in `PendingSell` because of terminal order outcomes.
3. Eliminate unsafe fallback behavior that can silently corrupt lot tracking.
4. Add enough validation and diagnostics to support several months of paper testing.
5. Keep changes focused and auditable before any future optimization or symbol expansion.

## Non-Goals For This Phase
- Do not add more symbols yet.
- Do not redesign the trading strategy logic unless required for safety/correctness.
- Do not optimize parameters yet.
- Do not prepare real-money deployment yet.

## Implementation Phases

## Phase 1: State And Order Safety
- Status: completed
- Goal: remove known live-safety defects in persistence and sell-order handling.

### Task 1.1: Create a V33-specific persistence key
- Status: completed
- Scope:
  - Replace the reused V32 `ObjectStore` key with a V33-specific key.
  - Ensure startup behavior assumes a clean-state baseline after paper-account reset.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - V33 no longer shares persistent state with V32.

### Task 1.2: Add startup reconciliation for holdings and open orders
- Status: completed
- Scope:
  - After loading state, compare tracked lots against current holdings.
  - Inspect open orders/tickets and reconcile pending sells to real broker state.
  - Clear stale `PendingSell` flags and stale order IDs.
  - Rebuild lot tracking when stored state is inconsistent with current holdings/open orders.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - A restart with preexisting holdings can recover into internally consistent lot tracking.

### Task 1.3: Handle all terminal sell-order outcomes
- Status: completed
- Scope:
  - Treat rejected sell orders as resolved.
  - Review whether other terminal states need equivalent cleanup behavior.
  - Ensure terminal order resolution always clears `PendingSell` safely.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - No lot can remain stuck in `PendingSell` solely because of a terminal order outcome.

### Task 1.4: Remove unsafe unmatched-order fallback behavior
- Status: completed
- Scope:
  - Stop mutating arbitrary lots when sell fills/resolutions do not match an order ID.
  - Replace the current “first pending lot” fallback with deterministic recovery behavior.
  - Route unresolved cases into reconciliation logic instead of silent mutation.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Unmatched sell events no longer silently corrupt lot accounting.

## Phase 2: Recovery And Invariant Enforcement
- Status: completed
- Goal: make internal state self-checking and recoverable during paper deployment.

### Task 2.1: Define lot-state invariants
- Status: completed
- Scope:
  - Define what must always be true for tracked lots, pending sells, and symbol holdings.
  - Add internal validation helpers to check these invariants at key lifecycle points.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - The algorithm can explicitly detect broken state rather than drifting silently.

### Task 2.2: Implement symbol-level rebuild logic
- Status: completed
- Scope:
  - Build a deterministic routine that reconstructs tracked lots from current holdings and open orders.
  - Decide how to represent rebuilt lots when exact historical entries are unavailable after restart.
  - Ensure rebuilt state is immediately persisted.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Any symbol flagged inconsistent can be rebuilt into a usable state without manual intervention.

### Task 2.3: Define behavior for unrecoverable cases
- Status: completed
- Scope:
  - Decide what the algorithm should do if a symbol cannot be rebuilt safely.
  - Recommended baseline: block new trading for that symbol, emit high-signal error logs, and keep other symbols operating.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Unrecoverable symbol-state problems fail safe instead of failing silently.

## Phase 3: Operational Hardening
- Status: completed
- Goal: improve runtime observability and reduce paper-test ambiguity.

### Task 3.1: Add startup and reconciliation logs
- Status: completed
- Scope:
  - Log state-load summary, rebuild actions, cleared stale pending orders, and blocked symbols.
  - Keep logs concise but actionable for paper-test debugging.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - A restart/recovery event can be understood from logs without guessing.

### Task 3.2: Add configuration and runtime sanity checks
- Status: completed
- Scope:
  - Validate symbol settings and key sizing assumptions at startup.
  - Guard against invalid values that could create unsafe behavior during paper deployment.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Invalid configuration fails early and loudly.

### Task 3.3: Review persistence timing
- Status: completed
- Scope:
  - Review when `SaveState()` is called and whether additional save points are needed after reconciliation/rebuild.
  - Avoid excessive churn while keeping state durable enough for restart safety.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Persistence behavior is deliberate and aligned with restart safety.

## Phase 4: Validation
- Status: completed
- Goal: verify the hardened base before paper deployment.

### Task 4.1: Static review after implementation
- Status: completed
- Scope:
  - Perform a strict code review focused on safety, correctness, and paper/live restart behavior.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Findings are logged and either fixed or explicitly accepted.

### Task 4.2: Build validation
- Status: completed
- Scope:
  - Run project build and confirm V33 compiles cleanly aside from existing unrelated warnings.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Build passes.

### Task 4.3: Scenario checklist for paper readiness
- Status: completed
- Scope:
  - Validate expected behavior for:
    - clean start with no state
    - restart with holdings and no open orders
    - restart with holdings and open sell orders
    - rejected sell order
    - canceled sell order
    - partial sell fill
    - unmatched order event
    - reconciliation-triggered rebuild
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
  - `project-notes/MultiStockV33_Stable_Base_hardening_plan.md`
- Done when:
  - The checklist is completed and any remaining gaps are documented.

## Suggested Execution Order
1. Task 1.1
2. Task 1.2
3. Task 1.3
4. Task 1.4
5. Task 2.1
6. Task 2.2
7. Task 2.3
8. Task 3.1
9. Task 3.2
10. Task 3.3
11. Task 4.1
12. Task 4.2
13. Task 4.3

## Progress Log
- 2026-03-27: Created initial hardening plan for V33 based on user-confirmed deployment strategy and review findings.
- 2026-03-27: Prepared local note changes for commit after confirming the scope is limited to V33 planning/review documentation.
- 2026-03-28: Completed Task 1.1 by changing V33 to use its own `ObjectStore` key (`EightStar_State_V33_Key`) so it no longer shares persisted state with V32.
- 2026-03-28: Completed Task 1.2 by replacing startup "recover if empty" logic with explicit reconciliation against current holdings and open sell orders, with rebuild-from-broker-state behavior when tracked lots are inconsistent.
- 2026-03-28: Completed Task 1.3 by confirming this LEAN codebase treats broker-side rejection through `OrderStatus.Invalid` rather than a separate `Rejected` enum, and keeping terminal cleanup aligned with that model.
- 2026-03-28: Completed Task 1.4 by removing unsafe arbitrary-lot fallback handling and rebuilding tracked lots from holdings/open orders whenever unmatched sell fills or unmatched sell-order resolutions occur.
- 2026-03-28: Started Tasks 2.1-2.3 to formalize lot-state invariants, tighten broker-state rebuild validation, and add a fail-safe blocked-symbol path when rebuild cannot restore a trustworthy state.
- 2026-03-28: Completed Task 2.1 by defining explicit lot-state invariants for quantity, prices, pending-order linkage, holdings parity, and open-order parity, and by checking them at startup, before trading, and after order events.
- 2026-03-28: Completed Task 2.2 by tightening broker-state rebuild logic so it rebuilds in a deterministic way from holdings plus open sell orders, validates the rebuilt lots before accepting them, and saves the rebuilt state immediately in live mode.
- 2026-03-28: Completed Task 2.3 by adding a symbol-level fail-safe block when broker state cannot be rebuilt safely, while allowing recovery and trading to resume automatically once the symbol can be reconciled again.
- 2026-03-28: Started Tasks 3.1-3.3 to improve startup/reconciliation observability, add explicit configuration sanity checks, and make persistence timing deliberate with reduced save churn.
- 2026-03-28: Completed Task 3.1 by adding startup load/reconciliation summaries, richer rebuild transition logs, and explicit blocked-symbol reporting so recovery behavior can be understood from runtime logs.
- 2026-03-28: Completed Task 3.2 by validating core algorithm parameters, per-symbol configuration values, and the persistence key before symbol initialization so invalid setup fails early.
- 2026-03-28: Completed Task 3.3 by making persistence timing explicit through reason-tagged saves and a deterministic state fingerprint that skips unchanged `ObjectStore` writes while preserving rebuild and shutdown durability.
- 2026-03-28: Started Tasks 4.1-4.3 to run the final static review, confirm build status for the hardened V33 base, and record the paper-readiness scenario checklist with any remaining gaps.
- 2026-03-28: Completed Task 4.1 with a final strict static review of the hardened V33 code path. No new blocking issues were found in the restart recovery, pending-sell cleanup, blocked-symbol isolation, or persistence-dedup logic.
- 2026-03-28: Completed Task 4.2 by rerunning `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo`. The build succeeded with 2 existing repo-level package warnings only (`DotNetZip` vulnerability in `QuantConnect.Compression`).
- 2026-03-28: Completed Task 4.3 by documenting the paper-readiness scenario checklist and the remaining accepted limitation around synthetic lot reconstruction after restart.
- 2026-03-28: Re-reviewed the final local V33 hardening changes against the hardening plan and confirmed that all phase and task status markers remain aligned with the implemented code paths.

## Review Log
- 2026-03-27: Plan reviewed for scope alignment. No issues found. The plan is focused on hardening V33 as the next paper-trading base without expanding scope into symbol growth or parameter optimization.
- 2026-03-27: Commit-preparation scope reviewed. No issues found. Pending changes are documentation-only and limited to the V33 hardening/review notes.
- 2026-03-28: Task 1.1 strict review completed. No issues found in the key-isolation change. Validation: `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo` succeeded with existing unrelated warnings only. Note: V33 will now ignore any V32 persisted lot state by design, which matches the planned paper-account reset.
- 2026-03-28: Tasks 1.2-1.4 strict review completed. No new compile issues found. Validation: `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo` succeeded with existing unrelated warnings only. Note: unmatched sell-event handling now prefers deterministic rebuild from holdings/open orders over heuristic lot reassignment, and startup reconciliation now normalizes stale pending state after restart.
- 2026-03-28: Tasks 2.1-2.3 strict review completed. No new issues found in the invariant enforcement, rebuild validation, or blocked-symbol recovery flow. Validation: `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo` succeeded with existing unrelated warnings only. Note: symbols now fail safe by blocking only local trading when broker state cannot be reconstructed, and they re-enter service automatically after a later successful reconciliation.
- 2026-03-28: Tasks 3.1-3.3 strict review completed. No new issues found in the added observability, startup validation, or persistence dedup path. Validation: `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo` succeeded with existing unrelated warnings only. Note: unchanged state snapshots are now skipped intentionally, which reduces `ObjectStore` churn without removing any live-mode save points.
- 2026-03-28: Tasks 4.1-4.3 strict review completed. No new blocking issues found. Final validation: `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo` succeeded with 2 existing repo-level `DotNetZip` warnings only. Accepted limitation: broker-state rebuild after restart intentionally reconstructs synthetic lots from holdings/open orders rather than exact historical entry lots, so lot-level trailing-state continuity across restarts is approximate by design and should be watched during paper testing.
- 2026-03-28: Final local re-review completed. No new issues found. Confirmation: all tasks in this hardening plan are marked `completed`, and the corresponding implementation hooks remain present in `MultiStockV33_Stable_Base.cs` for persistence isolation, startup reconciliation, invariant enforcement, blocked-symbol recovery, operational logging, configuration validation, persistence dedup, and paper-readiness documentation.

## Paper-Readiness Scenario Checklist
- Clean start with no state: validated. `LoadState()` now logs absence of persisted state and initializes an empty fingerprint; no reconciliation rebuild is required when holdings and open orders are empty.
- Restart with holdings and no open orders: validated. `ReconcileLiveState()` routes inconsistent stored state into deterministic rebuild, producing one non-pending synthetic lot from broker holdings and average price.
- Restart with holdings and open sell orders: validated. Rebuild creates pending lots from broker open sell tickets first, then assigns any remaining holdings to one non-pending synthetic lot.
- Rejected sell order: validated. This LEAN codebase reports broker-side rejection via `OrderStatus.Invalid`, which is treated as a resolved terminal sell status and clears pending state.
- Canceled sell order: validated. `ResolvePendingSell()` clears `PendingSell` and removes the lot only if quantity is already zero.
- Partial sell fill: validated. `HandleSellFill()` decrements lot quantity on each negative fill event, and final terminal cleanup happens separately in `ResolvePendingSell()`.
- Unmatched order event: validated. The algorithm no longer mutates an arbitrary lot; it rebuilds from broker state and blocks only the affected symbol if rebuild still cannot validate.
- Reconciliation-triggered rebuild: validated. The same invariant and rebuild path is exercised at startup, before trading scans, while blocked symbols receive order events, and after each order event.

## Remaining Gaps / Accepted Limits
- The scenario checklist above is a static code-path validation, not an automated integration test suite.
- Restart rebuild intentionally collapses exact historical entry structure into broker-state synthetic lots. This is acceptable for the V33 paper baseline, but trailing-stop continuity after restart should be monitored during paper deployment logs.
