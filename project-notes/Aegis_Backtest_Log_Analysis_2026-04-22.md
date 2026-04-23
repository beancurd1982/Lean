## 2026-04-22 - Aegis backtest log analysis

### Task
- Inspect newly added Aegis backtest artifacts in `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs`.
- Normalize any new raw Aegis log filenames using the repo-local renamer.
- Read the newest relevant log/report artifacts and summarize the findings.

### Files to touch
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv`
- `project-notes/Aegis_Backtest_Log_Analysis_2026-04-22.md`

### Notes
- New artifacts identified:
  - Raw log: `Formal Orange Cormorant_logs.txt`
  - Report JSON: `Logs_V7.json`
- Ran the repo-local log renamer and normalized the raw log to:
  - `2026-04-22_221208__AegisGrowthAllocation__Formal-Orange-Cormorant_logs.txt`
- Updated `log-index.csv` with the new normalized entry and completed the review for that row.
- Text log observations:
  - Backtest id: `739dbcbe84e0de081df36b02d25b1967`
  - The exchange-aware weekly schedule is active. The first review now occurs on `2018-01-02 10:00:00`, and holiday-shortened weeks shift to Tuesday reviews such as `2018-01-16` and `2018-02-20`.
  - The run completed through the end of the test window with regular `[AEGIS]` weekly summaries.
  - QuantConnect's 100 KB log cap truncated the text log, but the visible portion is sufficient to confirm schedule behavior and normal strategy progression.
- `Logs_V7.json` observations:
  - `OrderFillsDuringExtendedMarketHoursAnalysis` is still present.
  - Warning count increased to `2698`.
  - The sample event is now the first `AMZN` order at `2018-01-02T15:00:00Z` / `2018-01-02 10:00 AM` New York time with `status=submitted`.
  - Other report analyses remain:
    - `PortfolioMarginUsageAnalysis`
    - `PerformanceRelativeToBenchmarkAnalysis`
    - `CrisisEventsAnalysis` with `count=2`
    - `StatisticalSignificanceOfDailyReturnsAnalysis`
- Performance comparison:
  - `V5`:
    - `Compounding Annual Return`: `17.457%`
    - `Drawdown`: `14.900%`
    - `Sharpe Ratio`: `0.864`
    - `Sortino Ratio`: `0.949`
    - `Probabilistic Sharpe Ratio`: `60.107%`
    - `End Equity`: `109528.05`
    - `Net Profit`: `265.093%`
    - `Total Orders`: `1252`
    - `Portfolio Turnover`: `2.99%`
    - `Total Fees`: `$1316.50`
    - `OrderFillsDuringExtendedMarketHoursAnalysis`: `2504`
  - `V6`:
    - `Compounding Annual Return`: `14.540%`
    - `Drawdown`: `15.600%`
    - `Sharpe Ratio`: `0.75`
    - `Sortino Ratio`: `0.81`
    - `Probabilistic Sharpe Ratio`: `50.960%`
    - `End Equity`: `89628.71`
    - `Net Profit`: `198.762%`
    - `Total Orders`: `1098`
    - `Portfolio Turnover`: `2.79%`
    - `Total Fees`: `$1143.57`
    - `OrderFillsDuringExtendedMarketHoursAnalysis`: `2196`
  - `V7`:
    - `Compounding Annual Return`: `18.291%`
    - `Drawdown`: `16.500%`
    - `Sharpe Ratio`: `0.91`
    - `Sortino Ratio`: `0.998`
    - `Probabilistic Sharpe Ratio`: `65.089%`
    - `End Equity`: `116270.92`
    - `Net Profit`: `287.570%`
    - `Total Orders`: `1349`
    - `Portfolio Turnover`: `3.20%`
    - `Total Fees`: `$1427.14`
    - `OrderFillsDuringExtendedMarketHoursAnalysis`: `2698`
- Interpretation:
  - The schedule-only change worked in the narrow sense that reviews now follow exchange-aware week starts instead of a generic Monday calendar rule.
  - The schedule-only change did not resolve the target warning. The warning is worse in `V7` than in both `V6` and `V5`.
  - Because the warning sample remains a `10:00 AM` New York submitted order, the root cause still appears tied to order submission semantics or QuantConnect's warning logic around those submitted orders, not merely the weekly date rule.
  - `V7` recovered and exceeded `V5` on most headline return metrics, but that stronger performance does not change the fact that the execution-realism warning remains unresolved.
- Next-step implication:
  - The next investigation should focus on the actual order submission path around `SetHoldings` and liquidations at the scheduled callback, including whether orders are being submitted in a way QuantConnect still classifies as extended-hours-related even when the exchange-aware schedule and `IsMarketOpen` checks are present.

### Review
- Review completed.
- Findings:
  - The new log was normalized and documented correctly.
  - The exchange-aware schedule change is active in the V7 backtest output.
  - `OrderFillsDuringExtendedMarketHoursAnalysis` remains unresolved and increased to `2698`.
  - V7 performance improved materially versus V5/V6 despite the warning remaining.
- Open questions:
  - Why QuantConnect still classifies the `10:00 AM` New York submitted orders under `OrderFillsDuringExtendedMarketHoursAnalysis`.
  - Whether the warning is triggered by submitted market orders at the scheduled callback before a later regular-hours fill path is observed.
  - Whether the next fix needs to change order submission mechanics rather than only scheduling.
