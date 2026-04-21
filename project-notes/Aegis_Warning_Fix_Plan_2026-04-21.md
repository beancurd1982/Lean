# Aegis Warning Fix Plan

> Focus on warnings that materially affect backtest realism or live/backtest reconciliation. Do not spend time "cleaning" diagnostic performance commentary unless it exposes a real implementation issue.

## Goal
- Remove or materially reduce the highest-risk backtest realism warning for `AegisGrowthAllocation` while preserving the intended strategy logic and minimizing performance damage.

## Scope Decision
- In scope now:
  - `OrderFillsDuringExtendedMarketHoursAnalysis`
  - Supporting stale-fill / scheduled-event timing checks that directly affect the same execution path
- Out of scope for now:
  - `PerformanceRelativeToBenchmarkAnalysis`
  - `CrisisEventsAnalysis`
  - `StatisticalSignificanceOfDailyReturnsAnalysis`
  - `PortfolioMarginUsageAnalysis`
  - `FlatEquityCurveAnalysis` unless it reveals a true algorithm defect

## Why This Priority
- QuantConnect documentation indicates stale fills and scheduled-event timing mismatches can make backtests diverge materially from live trading.
- QuantConnect documentation also states that market orders outside regular hours are converted to market-on-open orders for US equities.
- The current Aegis warning is in the execution/fill path, not only in the strategy-performance commentary layer.
- A lower-performing but more realistic backtest is more useful than a stronger backtest built on suspect fills.

## Relevant References
- QuantConnect docs root: `https://www.quantconnect.com/docs/v2/`
- Writing Algorithms root: `https://www.quantconnect.com/docs/v2/writing-algorithms`
- Scheduled Events: `https://www.quantconnect.com/docs/v2/writing-algorithms/scheduled-events`
- Market Hours: `https://www.quantconnect.com/docs/v2/writing-algorithms/securities/market-hours`
- US Equity Market Hours: `https://www.quantconnect.com/docs/v2/writing-algorithms/securities/asset-classes/us-equity/market-hours`
- Market Orders: `https://www.quantconnect.com/docs/v2/writing-algorithms/trading-and-orders/order-types/market-orders`
- Liquidating Positions: `https://www.quantconnect.com/docs/v2/writing-algorithms/trading-and-orders/liquidating-positions`
- Position Sizing / `SetHoldings`: `https://www.quantconnect.com/docs/v2/writing-algorithms/trading-and-orders/position-sizing`
- Trade Fills Key Concepts: `https://www.quantconnect.com/docs/v2/writing-algorithms/reality-modeling/trade-fills/key-concepts`
- Live Trading Reconciliation: `https://www.quantconnect.com/docs/v2/writing-algorithms/live-trading/reconciliation`

## Current Evidence
- `V6` still shows `OrderFillsDuringExtendedMarketHoursAnalysis`.
- Warning count improved from `2504` in `V5` to `2196` in `V6`, but the warning remains.
- The sample event is still the first `COST` order at `2018-01-08 10:00 AM` New York time.
- `V6` performance degraded versus `V5`, which indicates the previous change altered execution mechanics without fully fixing the root timing issue.

## Working Hypothesis
- The remaining warning is most likely caused by order submission timing at the Monday `10:00 AM` scheduled review and/or how `SetHoldings` is interacting with the backtest fill model at that time.
- The core issue is more likely market-open gating / scheduled-event fill timing than security-selection logic.
- The best-documented next change is to replace the generic clock-based schedule with a symbol-aware `DateRules.WeekStart(symbol)` plus `TimeRules.AfterMarketOpen(symbol, minutes)` trigger.

## Files Likely To Change
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_Execution_Realism_Fix_2026-04-21.md`
- `project-notes/Aegis_Backtest_Log_Analysis_2026-04-21.md`

## Plan

### Phase 1 - Fix the highest-priority realism warning
- Replace the generic Monday `10:00 AM` callback with an exchange-aware `DateRules.WeekStart(_marketSymbol)` plus `TimeRules.AfterMarketOpen(_marketSymbol, minutes)` schedule.
- Add explicit regular-hours execution gating before `SetHoldings` / liquidation actions using exchange-hours checks.
- If the scheduled callback can still run before the engine has the fresh regular-hours pricing state needed for realistic fills, defer execution to the next safe minute instead of forcing the order immediately.
- Keep the strategy logic, target-weight construction, and daily decision inputs unchanged in this phase.

### Phase 2 - Verify that the fix did not accidentally change strategy logic
- Confirm the weekly regime and target-selection summaries remain aligned with the intended daily-bar logic.
- Compare a small set of milestone weekly summaries between the prior baseline and the new run to ensure that the selected assets and regime states are not unexpectedly drifting because of the execution fix.

### Phase 3 - Cloud validation
- Upload the touched Aegis source files to QuantConnect Cloud.
- Rerun the same backtest window used for `V5` and `V6`.
- Compare:
  - presence/absence of `OrderFillsDuringExtendedMarketHoursAnalysis`
  - warning count if still present
  - `CAGR`, `Sharpe`, `Drawdown`, `Total Orders`, `Portfolio Turnover`, and `End Equity`

### Phase 4 - Decide whether to keep or revert the fix
- Keep the fix if:
  - the warning is removed, or reduced to a clearly acceptable and explainable residual level
  - the strategy logic remains intact
  - the performance degradation is acceptable relative to the realism improvement
- Reject or revise the fix if:
  - the warning remains effectively unchanged
  - the change materially perturbs selection logic
  - the degradation is severe without improving realism enough

## Acceptance Criteria
- Primary:
  - `OrderFillsDuringExtendedMarketHoursAnalysis` is removed, or the count is materially reduced with a clear explanation of the residual cases.
- Secondary:
  - No new fatal errors or pathological trading behavior appear in the logs.
  - Weekly decision summaries remain logically consistent with the strategy design.
  - Performance remains within a defensible range relative to the realism improvement.

## Risks
- Any execution-timing change can affect realized performance without changing the signal logic.
- Moving execution later in the session may improve realism but alter fill prices and compounding.
- Guarding too aggressively can accidentally suppress intended trades.

## Recommendation
- Proceed with `OrderFillsDuringExtendedMarketHoursAnalysis` first.
- Start with the smallest behavior change that matches the QuantConnect documentation:
  - exchange-aware weekly scheduling
  - explicit regular-hours execution checks
- Do not spend engineering time on the lower-priority report warnings until the execution-realism issue is either resolved or reduced to an understood residual.

## Review
- Review completed for the planning note.
- Findings:
  - The priority order is consistent with the QuantConnect documentation on fills, market hours, and reconciliation.
  - No conflicting plan item was identified.
  - The plan intentionally avoids treating performance-commentary warnings as implementation defects.
