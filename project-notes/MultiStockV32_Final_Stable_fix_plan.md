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

## 2026-02-27
- Step: Start task to add detailed parameter comments in `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs`.
- Summary: Logged intent and will review current parameter definitions.
- Files: `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs`.
- Risks/Open Questions: None yet.

## 2026-02-27
- Step: Add detailed parameter comments in `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs`.
- Summary: Document each `[Parameter]` field with units, usage, and constraints.
- Files: `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs`.
- Risks/Open Questions: None.

## 2026-02-27
- Step: Perform strict code review for parameter comment additions.
- Summary: Review for safety, correctness, and live-trading risks after documentation-only changes.
- Files: `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs`.
- Risks/Open Questions: None identified before review.

## 2026-02-27 - Review
- Scope: `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs` (parameter comments only).
- Findings: No issues found. Documentation-only change; no behavior or live-trading impact.
- Risks/Open Questions: None.

## 2026-02-27
- Step: Add blank lines between parameter fields in `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs`.
- Summary: Improve readability by spacing parameter blocks.
- Files: `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs`.
- Risks/Open Questions: None.

## 2026-02-27
- Step: Perform strict code review for parameter spacing update.
- Summary: Review for safety, correctness, and live-trading risks after formatting-only change.
- Files: `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs`.
- Risks/Open Questions: None identified before review.

## 2026-02-27 - Review
- Scope: `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs` (spacing between parameters).
- Findings: No issues found. Formatting-only change; no behavior or live-trading impact.
- Risks/Open Questions: None.

## 2026-02-27
- Step: Provide thoughts on changing `_minBuyAmount` from fixed USD to percentage (no code changes).
- Summary: Assess tradeoffs and live-trading considerations.
- Files: None (discussion only).
- Risks/Open Questions: Would be a behavioral change affecting live trading; requires explicit confirmation if implemented.

## 2026-02-27
- Step: Clarify base for new `_minBuyPct` (initial cash vs current cash vs total portfolio), especially in live mode.
- Summary: Requested user clarification before code changes.
- Files: None (pending clarification).
- Risks/Open Questions: Base for percentage impacts live trading behavior and backtest comparability.

## 2026-02-27
- Step: Provide recommendation for `_minBuyPct` base calculation.
- Summary: Suggest a base and explain tradeoffs for live/backtest behavior.
- Files: None (discussion only).
- Risks/Open Questions: Requires user confirmation before implementing behavioral change.

## 2026-02-27
- Step: Replace `_minBuyAmount` with `_minBuyPct` (based on `Portfolio.Cash`) in `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs`.
- Summary: Update parameter name/default and adjust buy logic to use percentage of current cash.
- Files: `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs`.
- Risks/Open Questions: Behavior change to buy threshold; confirm with user (requested Portfolio.Cash).

## 2026-02-27
- Step: Perform strict code review for `_minBuyPct` change.
- Summary: Review for safety, correctness, and live-trading risks after behavior update.
- Files: `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs`.
- Risks/Open Questions: None identified before review.

## 2026-02-27 - Review
- Scope: `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs` (`_minBuyAmount` -> `_minBuyPct`, Portfolio.Cash-based).
- Findings:
  - None. Logic aligns with request; behavior change is explicit and limited to buy threshold check.
- Risks/Open Questions:
  - None.

## 2026-02-27
- Step: Review buy sizing logic using `_buyFraction` and `_minBuyPct` (discussion only).
- Summary: Evaluate whether current comparison is meaningful and suggest alternatives.
- Files: None (discussion only).
- Risks/Open Questions: Behavior implications if `_minBuyPct` > `_buyFraction`.

## 2026-02-27
- Step: Discuss desired minimum order sizing behavior (no code changes).
- Summary: Explain that current logic doesn't enforce a fixed-dollar floor across time and suggest storing baseline cash/portfolio value.
- Files: None (discussion only).
- Risks/Open Questions: Need to confirm baseline for minimum order in live mode (initial cash vs initial portfolio value).

## 2026-02-27
- Step: Revert `_minBuyPct` to `_minBuyAmount` with default 1000 and enforce minimum order size in buy logic.
- Summary: Update parameter name/default and adjust sizing to meet minimum when cash allows.
- Files: `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs`.
- Risks/Open Questions: Behavior change to buy sizing; backtest-only per user.

## 2026-02-27
- Step: Perform strict code review for `_minBuyAmount` change and minimum order enforcement.
- Summary: Review for safety, correctness, and live-trading risks after sizing update.
- Files: `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs`.
- Risks/Open Questions: None identified before review.

## 2026-02-27 - Review
- Scope: `Algorithm.CSharp/MyAlgorithms/SingleStockSwingV23_PureClean.cs` (`_minBuyAmount` restore and minimum order enforcement).
- Findings:
  - None. Logic enforces minimum order size when cash allows; otherwise skips buy.
- Risks/Open Questions:
  - None.
