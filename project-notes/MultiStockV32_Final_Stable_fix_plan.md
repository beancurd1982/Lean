# MultiStockV32_Final_Stable Fix Plan
Date: 2026-02-21

## Context
Review of `Algorithm.CSharp/MyAlgorithms/MultiStockV32_Final_Stable.cs` identified 5 priority issues. This plan proposes concrete fixes before any code changes.

## Priority Issues And Fix Plan
1. Pending sells can get stuck on cancel/reject/partial fills.
   Plan:
   - Track open sell orders and clear `PendingSell` on `OrderStatus.Canceled`, `Invalid`, `Rejected`, or when a partial fill leaves remainder canceled.
   - Store `OrderId` per lot when submitting a sell.
   - On order event, match by `OrderId` to update the correct lot and reset `PendingSell` when the order is fully resolved.

2. Lot accounting is not tied to actual orders (fills can hit wrong lots).
   Plan:
   - Add `OrderId` (or a list of order ids) to `Lot`.
   - On sell submission, assign the `OrderId` to that lot.
   - In `OnOrderEvent`, resolve lots by `OrderId` instead of iterating lots in a fixed order.

3. MaxWeight is not enforced per order and can be exceeded by a single buy.
   Plan:
   - Compute allowed additional value: `(MaxWeight * TotalPortfolioValue) - HoldingsValue`.
   - Cap order size by that amount and by cash.
   - Skip buy if allowed value is below a minimum threshold (ex: less than 1 share).

4. `DataNormalizationMode.Raw` can cause false triggers on splits/dividends.
   Plan:
   - Decide the intended behavior (raw vs adjusted).
   - If avoiding corporate action artifacts, switch to `DataNormalizationMode.Adjusted` or `SplitAdjusted`.
   - If keeping Raw, add guards around split/dividend events or accept the behavior and document it in algorithm notes.

5. No guard on `TotalPortfolioValue` when computing weights.
   Plan:
   - Add an early return when `Portfolio.TotalPortfolioValue <= 0`.
   - Optionally skip trading until all symbols have valid prices and portfolio value is stable.

## Implementation Notes
- Prioritize correctness of order/lot tracking before any parameter changes.
- Each code change should be accompanied by updates to this log folder.

## Next Step
- Confirm preferred data normalization mode (Raw vs Adjusted/SplitAdjusted).
## Data Normalization Recommendation (2026-02-21)
Suggested default: `DataNormalizationMode.Adjusted` for signal/indicator stability (SMA, thresholds, trailing stops) and to avoid dividend/split artifacts.
Use `SplitAdjusted` only if you explicitly want dividend effects to remain in the price series while still handling splits.
Decision: pending user confirmation.
## Update Log
- 2026-02-21: Fixed priority issue #1 in `Algorithm.CSharp/MyAlgorithms/MultiStockV32_Final_Stable.cs`. Added sell order ID tracking and cleared `PendingSell` on cancel/reject/invalid, and processed partial fills so sell lots don't get stuck.
## Update Log
- 2026-02-21: Fixed compile error from literal \\r\\n in sell order block and tightened sell-side detection to handle cancel/reject events safely.
## Update Log
- 2026-02-21: Fixed priority issue #2 by applying sell fills to the specific lot via `PendingSellOrderId` (with fallback if no match). Performed strict review after change (see review summary in this log).
## Review Summary (2026-02-21)
- Potential risk: If a sell fill arrives without a matching `PendingSellOrderId`, the fallback still mutates other lots silently, which can reintroduce mis-attribution. Consider logging or stricter handling if this becomes common.
## Update Log
- 2026-02-21: Addressed review issues by guarding failed sell submissions (do not mark pending when invalid) and adding explicit warnings when sell fills or order resolutions lack a matching lot (fallback only when exactly one pending lot exists).

## Review Summary (2026-02-21)
- Residual risk: If a sell fill or resolution arrives with no matching order id and multiple pending lots, the algorithm logs an error and does not adjust lots, leaving state inconsistent until manual intervention.
## Next Issue
- 2026-02-21: Next priority to address is issue #3 (enforce MaxWeight per buy / cap order size). Pending after that: issue #4 (switch to Adjusted normalization) and issue #5 (guard TotalPortfolioValue for weights).
## Update Log
- 2026-02-21: Started priority issue #3 fix (cap buy size to respect MaxWeight).
## Update Log
- 2026-02-21: Completed priority issue #3 fix by capping buy size to MaxWeight using `maxAdditionalValue`. Also changed buy log message to ASCII to avoid encoding corruption.

## Review Summary (2026-02-21)
- No new critical issues found in the MaxWeight cap logic.
- Note: `totalValue` is still used in weight calculations; early return on `maxAdditionalValue <= 0` avoids division when portfolio value is zero.
## Update Log
- 2026-02-21: Started priority issue #4 fix (switch data normalization to Adjusted for indicator stability).
## Update Log
- 2026-02-21: Completed priority issue #4 by switching `DataNormalizationMode` to `Adjusted` for all symbols.

## Review Summary (2026-02-21)
- No code issues found. Behavior change is intentional: indicators and thresholds now use adjusted price series, reducing split/dividend artifacts.
## Update Log
- 2026-02-21: Started priority issue #5 fix (guard TotalPortfolioValue in weight calculations).
## Update Log
- 2026-02-21: Completed priority issue #5 by guarding `TotalPortfolioValue` before weight calculations in `HandleBuyLogic`. Fixed indentation introduced during edit.

## Review Summary (2026-02-21)
- No new functional issues found. The guard prevents division by zero and avoids buys when portfolio value is not valid.
## Review Summary (2026-02-21)
- Full strict review completed.
- Finding: Unmatched sell fills or order-resolution events with multiple pending lots are logged but not reconciled, leaving state inconsistent until manual intervention.
- No other critical issues identified.
## Discussion Log
- 2026-02-21: Discussed live-trading handling for unmatched sell events. Options: (A) strict/manual resolution (current behavior), (B) auto-reconcile lots from portfolio or force liquidation to reset state. Recommendation: keep strict/manual for safety unless you explicitly want auto-reconcile; decision pending.
## Decision
- 2026-02-21: Chose strict/manual handling for unmatched sell events. No code change required.
