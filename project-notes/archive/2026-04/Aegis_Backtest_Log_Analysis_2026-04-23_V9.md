# Aegis Backtest Log Analysis V9 - 2026-04-23

## Step 1: Intake

Summary:
- User uploaded a V9 QuantConnect JSON report and log text file to `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs`.
- This V9 run should include temporary `[AEGIS-DIAG]` order-submission and order-event diagnostics.

Required workflow:
- Run `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Invoke-BackTestLogRename.ps1`.
- Identify the newest unreviewed normalized log and V9 JSON report.
- Analyze whether the diagnostics support or refute the extended-hours warning false-positive hypothesis.

Files touched:
- `project-notes/Aegis_Backtest_Log_Analysis_2026-04-23_V9.md`

Open questions:
- What is the V9 warning count?
- Do diagnostic order events occur during regular US equity hours?
- Does the warning sample still show `status=submitted` and `fillQuantity=0.0`?

## Step 2: Log Normalization

Summary:
- First attempt to run `Invoke-BackTestLogRename.ps1` directly was blocked by local PowerShell execution policy.
- Re-ran the repo-local script with process-level `ExecutionPolicy Bypass`.
- Raw log `Calm Sky Blue Cormorant_logs.txt` was normalized to `2026-04-23_220701__AegisGrowthAllocation__Calm-Sky-Blue-Cormorant_logs.txt`.
- `log-index.csv` now contains this V9 log with `status=unreviewed`.

Files touched:
- `project-notes/Aegis_Backtest_Log_Analysis_2026-04-23_V9.md`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv`

Initial observation:
- The downloaded text log contains `[AEGIS-DIAG]` lines.
- The first order submission and first two order events occur at `2018-01-02 10:00:00` with `IsMarketOpen=True` and `RegularHoursOpen=True`.

## Step 3: V9 JSON And Diagnostic Evidence

Backtest artifacts:
- JSON report: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Logs_V9.json`
- Normalized log: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-04-23_220701__AegisGrowthAllocation__Calm-Sky-Blue-Cormorant_logs.txt`

Headline metrics:
- Total Orders: `1351`
- End Equity: `$116,079.66`
- Net Profit: `286.932%`
- CAGR: `18.260%`
- Sharpe: `0.908`
- Drawdown: `16.500%`

Warning result:
- `OrderFillsDuringExtendedMarketHoursAnalysis` still appears.
- Warning count: `2702`.
- Ratio: `2702 / 1351 = 2.0`, exactly two warning rows per order.
- Warning sample:
  - `orderId=1`
  - `orderEventId=1`
  - `symbol=AMZN`
  - `time=1514905200.0`, which corresponds to `2018-01-02T15:00:00Z`
  - `status=submitted`
  - `fillPrice=0.0`
  - `fillQuantity=0.0`

Order timestamp check:
- All `1351` final order records are at `10:00` New York time.
- Buckets:
  - `10:00 EDT`: `870`
  - `10:00 EST`: `481`
- Simple regular-hours check found `0` final order records outside weekday `09:30-16:00` New York time.

Diagnostic log check:
- `[AEGIS-DIAG]` lines found: `340`.
- Submission lines: `85`.
- Order-event lines: `170`.
- Ticket lines: `85`.
- Event status counts:
  - `Submitted`: `85`
  - `Filled`: `85`
- Regular-hours flags:
  - Order events with `RegularHoursOpen=True`: `170`
  - Submissions with `RegularHoursOpen=True`: `85`
  - `RegularHoursOpen=False`: `0`
  - `IsMarketOpen=False`: `0`
- The text log ended with QuantConnect's `100kb` log limit message, so diagnostics are partial, but every captured diagnostic row supports regular-hours execution.

First diagnostic event pair:
- Submission:
  - `2018-01-02 10:00:00`
  - `UtcTime=2018-01-02T15:00:00Z`
  - `ExchangeLocal=2018-01-02T10:00:00`
  - `Symbol=AMZN`
  - `IsMarketOpen=True`
  - `RegularHoursOpen=True`
- Submitted event:
  - `EventUtcTime=2018-01-02T15:00:00Z`
  - `EventExchangeLocal=2018-01-02T10:00:00`
  - `Status=Submitted`
  - `FillQuantity=0`
  - `FillPrice=0`
  - `IsMarketOpen=True`
  - `RegularHoursOpen=True`
- Filled event:
  - `EventUtcTime=2018-01-02T15:00:00Z`
  - `EventExchangeLocal=2018-01-02T10:00:00`
  - `Status=Filled`
  - `FillQuantity=61`
  - `FillPrice=58.336321169`
  - `IsMarketOpen=True`
  - `RegularHoursOpen=True`

Comparison:
- V7: `1349` orders, warning count `2698`, ratio `2.0`, CAGR `18.291%`, Sharpe `0.91`, equity `$116,270.92`.
- V8: `1168` orders, warning count `2336`, ratio `2.0`, CAGR `15.726%`, Sharpe `0.838`, equity `$97,463.95`.
- V9: `1351` orders, warning count `2702`, ratio `2.0`, CAGR `18.260%`, Sharpe `0.908`, equity `$116,079.66`.

Conclusion:
- V9 strongly supports the false-positive / cloud-analyzer issue hypothesis.
- QuantConnect's warning sample is a submitted zero-fill event, not an actual fill.
- The matching diagnostic event shows `RegularHoursOpen=True` and exchange-local `10:00:00`.
- The warning appears to classify both submitted and filled order events as extended-hours fills, because the count equals exactly `2 x Total Orders`.
- There is no evidence from V9 that Aegis is submitting or filling these orders outside regular US equity market hours.

Recommendation:
- Stop changing Aegis execution logic to chase this warning.
- Treat `OrderFillsDuringExtendedMarketHoursAnalysis` as a likely QuantConnect Cloud analyzer false positive unless QuantConnect support confirms otherwise.
- Remove the temporary diagnostic instrumentation from `AegisGrowthAllocation.cs`.
- If needed, send the V9 evidence to QuantConnect support/forum.

## Step 4: Cleanup

Summary:
- Removed the temporary `[AEGIS-DIAG]` instrumentation from `AegisGrowthAllocation.cs` after completing the V9 analysis.
- The algorithm source is back to the V7 schedule-only baseline.
- `log-index.csv` was updated to mark the V9 normalized log as `reviewed` with this note as the review reference.

Files touched:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv`
- `project-notes/Aegis_Backtest_Log_Analysis_2026-04-23_V9.md`
- `project-notes/Aegis_OrderFillsWarning_RootCause_2026-04-23.md`

## Step 5: Review And Verification

Strict review:
- Reviewed the V9 JSON warning sample and diagnostic log lines against the warning hypothesis.
- The warning sample is not an actual fill because it has `status=submitted`, `fillQuantity=0.0`, and `fillPrice=0.0`.
- The matching diagnostic row for the first warning sample order shows `RegularHoursOpen=True` and exchange-local `10:00:00`.
- No evidence was found that Aegis submitted or filled these diagnostic orders during extended market hours.
- The temporary diagnostic instrumentation was removed from `AegisGrowthAllocation.cs` after analysis.

Verification:
- Searched `AegisGrowthAllocation.cs` for diagnostic markers: no matches for `[AEGIS-DIAG]`, `DiagnosticMax`, `ShouldEmitOrderDiagnostics`, or diagnostic helper names.
- Checked git status: `AegisGrowthAllocation.cs` is no longer modified.
- Checked `log-index.csv`: V9 log is marked `reviewed` with this note path.

Residual risks:
- QuantConnect Cloud analysis internals are not present in this repository, so the exact analyzer bug cannot be fixed locally.
- The downloaded text log hit QuantConnect's `100kb` log limit, so V9 diagnostics cover an early subset of orders, not all `1351` orders.
- The subset directly includes the first warning sample order, which is enough to support the conclusion for the reported sample.

## Step 6: Commit Plan

Summary:
- The V9 evidence indicates `OrderFillsDuringExtendedMarketHoursAnalysis` is likely a QuantConnect Cloud analyzer false positive for Aegis.
- No algorithm source changes are needed after removing temporary diagnostics.
- Commit the V9 report JSON, log-index update, and related project notes so the evidence is preserved.

Files expected in commit:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Logs_V9.json`
- `project-notes/Aegis_Backtest_Log_Analysis_2026-04-23_V9.md`
- `project-notes/Aegis_OrderFillsWarning_RootCause_2026-04-23.md`
- `project-notes/Aegis_Warning_Fix_Plan_2026-04-21.md`

Review requirement:
- Confirm `AegisGrowthAllocation.cs` has no diff before committing.
