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
- `V7` changed the weekly trigger to an exchange-aware week-start schedule and improved headline performance, but the warning remained and increased to `2698`.
- Current source review shows the `V7` implementation still trades intraday from a Scheduled Event while the market symbol and all tradable equities remain subscribed at `Resolution.Daily`.
- QuantConnect documentation explicitly flags that exact pattern as a stale-fill risk.
- `V8` tested minute subscriptions plus deferred execution, but it did not clear the warning.
- `V8` warning count was `2336`, exactly `2 x Total Orders`.
- The same exact `2 x Total Orders` ratio appears in `V5`, `V6`, `V7`, and `V8`.
- V8 order timestamps are at `10:00 AM` New York time, so the warning classification does not align with the visible order timestamps.
- The V8 execution-path implementation was rolled back because it changed early regime behavior and degraded performance.
- 2026-04-23 root-cause investigation found no local LEAN source implementation of `OrderFillsDuringExtendedMarketHoursAnalysis`; the named analyzer appears to be QuantConnect Cloud report-analysis behavior outside this checkout.
- In both `Logs_V7.json` and `Logs_V8.json`, the warning sample has `status=submitted`, `fillPrice=0.0`, and `fillQuantity=0.0`, so the analyzer sample is not an actual fill despite the warning text saying "filled orders".
- Every final order record in `Logs_V7.json` and `Logs_V8.json` occurs at `10:00 AM` New York time, with zero final orders outside a simple weekday `09:30-16:00` regular-hours check.
- QuantConnect's US equity market-hours documentation states regular trading hours are `09:30-16:00` America/New_York.
- QuantConnect's market-order documentation states market orders are submitted during regular trading hours and are converted to market-on-open only if placed outside regular hours for relevant asset classes.
- `V9` diagnostic run confirmed the first warning sample order and captured diagnostic rows all occurred at exchange-local `10:00:00` with `IsMarketOpen=True` and `RegularHoursOpen=True`.
- `V9` warning count was `2702` with `1351` total orders, again exactly `2 x Total Orders`.
- `V9` diagnostic logs captured `85` submissions, `85` submitted events, and `85` filled events; every captured row had `RegularHoursOpen=True`.

## Working Hypothesis
- The earlier execution-data-resolution hypothesis is rejected as the primary cause.
- The most likely explanation is a QuantConnect Cloud analyzer false positive or analyzer wording/counting bug:
  - the sample row is a submitted event, not a fill
  - the count equals exactly `2 x Total Orders`
  - final order timestamps are regular-session times
  - Aegis already has both schedule-level and per-symbol market-open guards
  - V9 diagnostics directly show submitted and filled events at exchange-local `10:00` with regular-hours checks true
- A lower-probability explanation is a cloud-side time-zone, exchange-hours, or symbol metadata interpretation mismatch that is not visible from the downloaded JSON.
- The next step should not be another execution-path fix. The next step should be either:
  - send the evidence to QuantConnect support/forum, or
  - run one diagnostic cloud backtest with temporary order-event logging if we need more proof.

## Files Likely To Change
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_Execution_Realism_Fix_2026-04-21.md`
- `project-notes/Aegis_Backtest_Log_Analysis_2026-04-21.md`
- `project-notes/Aegis_QC_Docs_Reassessment_2026-04-22.md`

## Plan

### Phase 1 - Fix the highest-priority realism warning
- Keep the exchange-aware weekly schedule already added in `V7`.
- Do not make further execution-path changes for this warning unless new evidence proves an Aegis defect.
- Treat the warning as likely cloud analyzer behavior until QuantConnect confirms otherwise.
- Preserve the V7 schedule-only source baseline because it improves platform alignment without changing strategy behavior.

### Phase 2 - Verify that the fix did not accidentally change strategy logic
- Confirm the weekly regime and target-selection summaries remain aligned with the intended daily-bar logic.
- Compare a small set of milestone weekly summaries between the prior baseline and the new run to ensure that the selected assets and regime states are not unexpectedly drifting because of the execution fix.

### Phase 3 - Cloud validation
- No additional cloud validation is recommended for another execution fix at this time.
- If we run another cloud backtest, make it diagnostic:
  - add temporary backtest-only debug lines for order submission time, symbol exchange-open status, and `OnOrderEvent` status/fill data
  - rerun once
  - remove the instrumentation after collecting evidence

### Phase 4 - Decide whether to keep or revert the fix
- Keep the fix if:
  - the warning is proven to identify a real Aegis market-hours defect and the fix removes or materially reduces it
  - the strategy logic remains intact
  - the performance degradation is acceptable relative to the realism improvement
- Reject or revise the fix if:
  - the warning remains effectively unchanged
  - the change materially perturbs selection logic
  - the degradation is severe without improving realism enough

## Acceptance Criteria
- Primary:
  - The warning is either removed by a proven Aegis fix, or documented as a likely QuantConnect Cloud analyzer false positive with supporting evidence.
- Secondary:
  - No new fatal errors or pathological trading behavior appear in the logs.
  - Weekly decision summaries remain logically consistent with the strategy design.
  - Performance remains within a defensible range relative to the realism improvement.

## Risks
- Any execution-timing change can affect realized performance without changing the signal logic.
- Moving execution later in the session may improve realism but alter fill prices and compounding.
- Guarding too aggressively can accidentally suppress intended trades.

## Recommendation
- Stop trying to eliminate `OrderFillsDuringExtendedMarketHoursAnalysis` through strategy/execution changes unless QuantConnect confirms the analyzer is reporting a real Aegis defect.
- Keep the code at the `V7` schedule-only baseline.
- Preserve the root-cause evidence in `project-notes/Aegis_OrderFillsWarning_RootCause_2026-04-23.md`.
- If we need more evidence, run one diagnostic cloud backtest with temporary order-event logging, then remove the instrumentation.
- Otherwise, ask QuantConnect support/forum why `OrderFillsDuringExtendedMarketHoursAnalysis` samples `status=submitted`, `fillQuantity=0.0` rows at `10:00 AM` New York time and reports a count equal to `2 x Total Orders`.
- V9 supplied the diagnostic evidence; the temporary instrumentation has been removed.
- The recommended external follow-up is now QuantConnect support/forum, not another algorithm execution change.
- Do not spend engineering time on the lower-priority report warnings until the execution-realism issue is either resolved or reduced to an understood residual.

## Review
- Review completed for the planning note.
- Findings:
  - The priority order is consistent with the QuantConnect documentation on fills, market hours, scheduled-event timing, and stale fills.
  - The plan has been revised after V8 because the execution-data-resolution fix did not resolve the warning and introduced strategy drift.
  - The plan has been revised again after root-cause investigation because V7/V8 final order timestamps are regular-session times and the cloud sample is a submitted event, not a fill.
  - V9 diagnostic evidence makes a real Aegis extended-hours execution defect unlikely.
  - The plan intentionally avoids treating performance-commentary warnings as implementation defects.
