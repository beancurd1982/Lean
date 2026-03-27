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
- Status: pending
- Goal: remove known live-safety defects in persistence and sell-order handling.

### Task 1.1: Create a V33-specific persistence key
- Status: pending
- Scope:
  - Replace the reused V32 `ObjectStore` key with a V33-specific key.
  - Ensure startup behavior assumes a clean-state baseline after paper-account reset.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - V33 no longer shares persistent state with V32.

### Task 1.2: Add startup reconciliation for holdings and open orders
- Status: pending
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
- Status: pending
- Scope:
  - Treat rejected sell orders as resolved.
  - Review whether other terminal states need equivalent cleanup behavior.
  - Ensure terminal order resolution always clears `PendingSell` safely.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - No lot can remain stuck in `PendingSell` solely because of a terminal order outcome.

### Task 1.4: Remove unsafe unmatched-order fallback behavior
- Status: pending
- Scope:
  - Stop mutating arbitrary lots when sell fills/resolutions do not match an order ID.
  - Replace the current “first pending lot” fallback with deterministic recovery behavior.
  - Route unresolved cases into reconciliation logic instead of silent mutation.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Unmatched sell events no longer silently corrupt lot accounting.

## Phase 2: Recovery And Invariant Enforcement
- Status: pending
- Goal: make internal state self-checking and recoverable during paper deployment.

### Task 2.1: Define lot-state invariants
- Status: pending
- Scope:
  - Define what must always be true for tracked lots, pending sells, and symbol holdings.
  - Add internal validation helpers to check these invariants at key lifecycle points.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - The algorithm can explicitly detect broken state rather than drifting silently.

### Task 2.2: Implement symbol-level rebuild logic
- Status: pending
- Scope:
  - Build a deterministic routine that reconstructs tracked lots from current holdings and open orders.
  - Decide how to represent rebuilt lots when exact historical entries are unavailable after restart.
  - Ensure rebuilt state is immediately persisted.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Any symbol flagged inconsistent can be rebuilt into a usable state without manual intervention.

### Task 2.3: Define behavior for unrecoverable cases
- Status: pending
- Scope:
  - Decide what the algorithm should do if a symbol cannot be rebuilt safely.
  - Recommended baseline: block new trading for that symbol, emit high-signal error logs, and keep other symbols operating.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Unrecoverable symbol-state problems fail safe instead of failing silently.

## Phase 3: Operational Hardening
- Status: pending
- Goal: improve runtime observability and reduce paper-test ambiguity.

### Task 3.1: Add startup and reconciliation logs
- Status: pending
- Scope:
  - Log state-load summary, rebuild actions, cleared stale pending orders, and blocked symbols.
  - Keep logs concise but actionable for paper-test debugging.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - A restart/recovery event can be understood from logs without guessing.

### Task 3.2: Add configuration and runtime sanity checks
- Status: pending
- Scope:
  - Validate symbol settings and key sizing assumptions at startup.
  - Guard against invalid values that could create unsafe behavior during paper deployment.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Invalid configuration fails early and loudly.

### Task 3.3: Review persistence timing
- Status: pending
- Scope:
  - Review when `SaveState()` is called and whether additional save points are needed after reconciliation/rebuild.
  - Avoid excessive churn while keeping state durable enough for restart safety.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Persistence behavior is deliberate and aligned with restart safety.

## Phase 4: Validation
- Status: pending
- Goal: verify the hardened base before paper deployment.

### Task 4.1: Static review after implementation
- Status: pending
- Scope:
  - Perform a strict code review focused on safety, correctness, and paper/live restart behavior.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Findings are logged and either fixed or explicitly accepted.

### Task 4.2: Build validation
- Status: pending
- Scope:
  - Run project build and confirm V33 compiles cleanly aside from existing unrelated warnings.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`
- Done when:
  - Build passes.

### Task 4.3: Scenario checklist for paper readiness
- Status: pending
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

## Review Log
- 2026-03-27: Plan reviewed for scope alignment. No issues found. The plan is focused on hardening V33 as the next paper-trading base without expanding scope into symbol growth or parameter optimization.
- 2026-03-27: Commit-preparation scope reviewed. No issues found. Pending changes are documentation-only and limited to the V33 hardening/review notes.
