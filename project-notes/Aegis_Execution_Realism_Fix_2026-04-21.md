## 2026-04-21 - Aegis execution realism fix

### Task
- Investigate and fix the remaining `OrderFillsDuringExtendedMarketHoursAnalysis` warning for `AegisGrowthAllocation`.
- Keep the change minimal and focused on execution realism.
- Record the QuantConnect documentation references that narrowed the root-cause hypothesis.

### Files to touch
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_Execution_Realism_Fix_2026-04-21.md`

### Notes
- QuantConnect documentation URLs recorded for future reference:
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
- Root-cause review:
  - The remaining realism warning in the `V5` backtest was no longer a startup artifact.
  - The analysis log showed repeated order activity at `2018-01-08T15:00:00Z`, which is `10:00 AM` New York time during the scheduled Monday review.
  - `AegisGrowthAllocation` was placing `SetHoldings` orders intraday while the tradable equity universe was subscribed only at `Resolution.Daily`.
  - QuantConnect guidance indicates that stale fills usually occur when daily data is combined with intraday scheduled-event trading and that market orders outside regular hours are converted to market-on-open orders.
- Implementation:
  - Changed tradable Aegis equities from `Resolution.Daily` to `Resolution.Minute`.
  - Preserved daily ranking/selection behavior by feeding each `AssetState` from a `Resolution.Daily` consolidator instead of minute bars.
  - Removed the direct `slice.Bars` asset-state update loop from `OnData` so each asset state is updated once per daily consolidated bar.
- Expected effect:
  - Order execution now has intraday data available.
  - Strategy regime and stock-selection inputs continue to use daily closes, so the selection logic should remain aligned with the existing design.
- Live-trading impact:
  - This change does affect execution data resolution for tradable symbols.
  - It is intended to improve realism without changing the scheduled review time or widening the strategy scope.
- Status after `V6` review:
  - This implementation attempt is not the accepted fix.
  - The extended-hours warning remained present in `V6`, and the performance regression was not justified by enough realism improvement.
  - The next attempt should target exchange-aware scheduling and explicit regular-hours execution checks before changing other execution assumptions.
  - Local cleanup completed after the review:
    - reverted the abandoned `AegisGrowthAllocation.cs` code change
    - removed the exploratory `Tests/Algorithm/AegisGrowthAllocationTests.cs` file
    - kept the documentation and backtest-artifact updates for the next clean implementation cycle

### Review
- Strict code review completed.
- Findings:
  - No correctness issue found in the implemented change.
  - The update is intentionally narrow: execution subscriptions changed to minute, while decision-state updates remain daily through consolidators.
  - The previous duplicate-update path in `OnData` was removed, which avoids contaminating daily close windows with minute bars.
- Residual risks:
  - Minute subscriptions will increase backtest runtime and data volume.
  - The extended-hours warning cannot be considered closed until the same `V5` backtest window is rerun in QuantConnect Cloud and the analysis output is checked again.
  - Local test/build verification is limited by repo-level NuGet audit warnings currently causing `dotnet build` / `dotnet test` to exit non-zero despite reporting `0 Error(s)`.

### Verification
- Attempted local verification:
  - Added `Tests/Algorithm/AegisGrowthAllocationTests.cs` to assert that tradable Aegis equity subscriptions are configured at `Resolution.Minute`.
  - Ran `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo --no-restore`.
  - Ran `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo --no-restore -p:NuGetAudit=false`.
- Result:
  - Both build commands exited non-zero because existing package vulnerability warnings (`NU1903` for `DotNetZip`, `NU1904` for `System.Drawing.Common`) are being treated as build-failing conditions in this repo state.
  - The build output reported `0 Error(s)`, so no source compile error was surfaced from the Aegis change itself.
- Required external verification:
  - Copy the touched Aegis source file(s) into the QuantConnect Cloud project.
  - Rerun the same `V5` backtest date range.
  - Confirm whether `OrderFillsDuringExtendedMarketHoursAnalysis` no longer appears and whether performance metrics remain within an acceptable range.
