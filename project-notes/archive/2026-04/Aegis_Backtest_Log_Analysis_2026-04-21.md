## 2026-04-21 - Aegis backtest log analysis

### Task
- Inspect newly added Aegis backtest artifacts in `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs`.
- Normalize any new log filenames using the repo-local renamer.
- Read the newest relevant log/report artifacts and summarize the findings.
- Record the relevant QuantConnect documentation URLs used to interpret the warning and revise the next-step guidance.

### Files to touch
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv`
- `project-notes/Aegis_Backtest_Log_Analysis_2026-04-21.md`

### Notes
- QuantConnect documentation URLs recorded for future Aegis issue triage:
  - Docs root: `https://www.quantconnect.com/docs/v2/`
  - Writing Algorithms root: `https://www.quantconnect.com/docs/v2/writing-algorithms`
  - Scheduled Events: `https://www.quantconnect.com/docs/v2/writing-algorithms/scheduled-events`
  - Market Hours: `https://www.quantconnect.com/docs/v2/writing-algorithms/securities/market-hours`
  - US Equity Market Hours: `https://www.quantconnect.com/docs/v2/writing-algorithms/securities/asset-classes/us-equity/market-hours`
  - Market Orders: `https://www.quantconnect.com/docs/v2/writing-algorithms/trading-and-orders/order-types/market-orders`
  - Liquidating Positions: `https://www.quantconnect.com/docs/v2/writing-algorithms/trading-and-orders/liquidating-positions`
  - Position Sizing / `SetHoldings`: `https://www.quantconnect.com/docs/v2/writing-algorithms/trading-and-orders/position-sizing`
  - Trade Fills Key Concepts: `https://www.quantconnect.com/docs/v2/writing-algorithms/reality-modeling/trade-fills/key-concepts`
  - Live Trading Reconciliation: `https://www.quantconnect.com/docs/v2/writing-algorithms/live-trading/reconciliation`
- New artifacts identified:
  - Log file: `Swimming Fluorescent Orange Fish_logs.txt`
  - Report JSON: `Logs_V6.json`
- Ran the repo-local log renamer and normalized the new log to:
  - `2026-04-21_205409__AegisGrowthAllocation__Swimming-Fluorescent-Orange-Fish_logs.txt`
- Updated `log-index.csv` via the renamer and then reviewed the new entry.
- Log observations:
  - Backtest completed successfully with no fatal exceptions.
  - Runtime summary: `96.12 seconds`, `32,584,076` data points processed.
  - The weekly `[AEGIS]` summaries appear throughout the run, so the strategy logic executed normally.
  - End-of-run data notes remain for `SGOV` adjusted starting dates.
- `Logs_V6.json` observations:
  - The `OrderFillsDuringExtendedMarketHoursAnalysis` warning is still present.
  - Warning count improved from `2504` in `V5` to `2196` in `V6`, but it did not clear.
  - The sample event is still the first `COST` order at `2018-01-08T15:00:00Z` / `2018-01-08 10:00 AM` New York time.
- Performance comparison, `V5` vs `V6`:
  - `Compounding Annual Return`: `17.457%` -> `14.540%`
  - `Drawdown`: `14.900%` -> `15.600%`
  - `Sharpe Ratio`: `0.864` -> `0.75`
  - `Sortino Ratio`: `0.949` -> `0.81`
  - `End Equity`: `109528.05` -> `89628.71`
  - `Total Orders`: `1252` -> `1098`
  - `Portfolio Turnover`: `2.99%` -> `2.79%`
- Interim conclusion:
  - The minute-subscription change reduced the extended-hours warning count modestly but did not solve the issue.
  - This suggests the remaining problem is likely tied to order submission timing or market-open gating, not only subscription resolution.
- Follow-up question under review:
  - Explain why `V6` performance degraded even though the code change was narrow and not intended to alter strategy logic.
  - Conclusion:
    - The intended signal logic stayed largely the same because `AssetState` continues to use daily bars through consolidators.
    - The regression is most likely coming from execution mechanics, not from a deliberate strategy-rule change.
    - Changing tradable equities from daily to minute subscriptions altered the prices available to `SetHoldings` at the Monday `10:00 AM` scheduled rebalance, the resulting order quantities, and the fill path.
    - Small intraday differences in entry/exit price, integer share rounding, and order sequencing compound materially over a multi-year backtest.
    - `V6` is therefore closer to a different execution model than `V5`, even though the high-level regime and selection rules were preserved.
    - Because the extended-hours warning still remains in `V6`, this run is in an awkward middle state: some optimistic bias was removed, but the underlying timing issue is not fully corrected yet.
- Additional documentation research in progress:
  - Review relevant QuantConnect `writing-algorithms` pages for scheduled events, market hours, market orders, `SetHoldings`, extended market hours, and reality-modeling / trade-fill behavior.
  - Goal: determine whether there is more direct platform guidance that narrows the root cause of `OrderFillsDuringExtendedMarketHoursAnalysis`.
- Documentation findings:
  - `SetHoldings` submits market orders.
  - Market orders submitted during pre-market or post-market hours are converted by LEAN into market-on-open orders.
  - `Liquidate` behaves similarly when called while the market is closed.
  - QuantConnect explicitly documents that stale fills usually occur when an algorithm uses daily data but trades intraday with scheduled events.
  - QuantConnect provides symbol-aware scheduling rules like `TimeRules.AfterMarketOpen(symbol, minutes)` and `DateRules.WeekStart(symbol)`, which are better aligned with exchange trading hours than a generic `TimeRules.At(10, 0)` callback.
  - By default, subscriptions only include regular trading hours unless `extendedMarketHours` is explicitly enabled for an intraday subscription.
- Revised implication for Aegis:
  - The remaining issue is likely not just "minute vs daily subscription".
  - The combination of scheduled intraday trading, `SetHoldings` market-order behavior, and a generic clock-based scheduled callback is still the strongest documented suspect.
  - The next implementation should move from a generic Monday `10:00 AM` callback to an exchange-aware `DateRules.WeekStart(symbol)` plus `TimeRules.AfterMarketOpen(symbol, minutes)` schedule and keep explicit regular-hours checks around order submission.

### Review
- Review completed.
- Findings:
  - No repository workflow issue found after normalization; the new log is indexed and reviewed.
  - The new backtest does not validate the execution-realism fix.
  - The strategy’s risk-adjusted performance degraded materially versus `V5`.
- Open questions:
  - Whether the QuantConnect analysis is triggered by order submission timing at the scheduled Monday `10:00 AM` event even with minute subscriptions.
  - Whether the next fix should explicitly guard execution with `IsMarketOpen(symbol)` or shift order placement to a later regular-hours point after a fresh minute bar has arrived.
  - Whether the regression came from changed weekly decisions or from changed fill/execution mechanics.
