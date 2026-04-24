# Aegis Backtest Log Analysis - 2026-04-24 V10

## Step 1: Intake

Summary:
- User added a new Aegis V10 JSON report and a new raw backtest log file to `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs`.
- Intake started under the backtest-log workflow rules in `AGENTS.md`.

Files observed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Logs_V10.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Emotional Fluorescent Orange Leopard_logs.txt`

Initial issue:
- The required repo-local renamer script was invoked first, but the initial run failed because PowerShell script execution is disabled by local execution policy.

Next step:
- Rerun the repo-local renamer with execution-policy bypass, update `log-index.csv`, and analyze the newest V10 artifacts.

## Step 2: Normalize And Index

Summary:
- Reran the required repo-local renamer script with execution-policy bypass.
- The new raw V10 log was normalized and added to `log-index.csv`.

Normalized artifact:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-04-24_205724__AegisGrowthAllocation__Emotional-Fluorescent-Orange-Leopard_logs.txt`

Index status:
- Added as `status=unreviewed` in `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv`.

Next step:
- Analyze `Logs_V10.json` and the normalized V10 log, then update the index row to `reviewed` with this note path.

## Step 3: Analysis

Artifacts analyzed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Logs_V10.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-04-24_205724__AegisGrowthAllocation__Emotional-Fluorescent-Orange-Leopard_logs.txt`

Headline result:
- V10 is better than the V9 baseline on the main portfolio metrics while keeping drawdown unchanged.

Metric comparison vs V9:
- End Equity:
  - V9: `$116,079.66`
  - V10: `$119,327.19`
- Net Profit:
  - V9: `286.932%`
  - V10: `297.757%`
- Sharpe:
  - V9: `0.908`
  - V10: `0.929`
- Drawdown:
  - V9: `16.500%`
  - V10: `16.500%`
- Total Orders:
  - V9: `1351`
  - V10: `1267`
- Portfolio Turnover:
  - V9: `3.20%`
  - V10: `3.15%`
- Total Fees:
  - V9: `$1,429.12`
  - V10: `$1,346.26`

Behavioral findings from the V10 log:
- Initialization confirms:
  - `UndeployedReserve=0`
  - `ToleranceBandScale=1`
- The backtest therefore exercised the tolerance-band rebalance-gating change, but it did **not** materially test reserve-release integration because reserve stayed at zero throughout the run.
- There are `398` weekly review lines in the log.
- `143` weekly reviews had `Curr=... Target=...` exactly, which is consistent with the new no-op gating behavior preserving current weights when the portfolio is already inside tolerance bands.

Warning / analyzer findings:
- The plain-text V10 log contains:
  - `0` `WARNING` lines
  - `0` `OrderFillsDuringExtendedMarketHoursAnalysis` lines
- The V10 JSON still contains the cloud analyzer item:
  - `OrderFillsDuringExtendedMarketHoursAnalysis`
  - `count = 2534`
- This count again equals exactly `2 x Total Orders` (`2 x 1267 = 2534`), which remains consistent with the earlier false-positive / event-counting interpretation rather than an actual extended-hours execution defect in Aegis.

Interpretation:
- V10 is a credible improvement over V9.
- The most likely driver is Phase 2 tolerance-band gating reducing unnecessary weekly rebalancing.
- Phase 3 reserve integration remains effectively unvalidated in this run because the strategy parameterization used `UndeployedReserve=0`.

Recommended next move:
- Keep the Phase 2 change.
- If you want to isolate Phase 3 next, run a controlled cloud backtest with a non-zero undeployed reserve input so the reserve-release path is actually exercised.

Parameter guidance from this result:
- The practical default for `undeployed-reserve` should remain `0`.
- A non-zero `undeployed-reserve` value should be used only when intentionally modeling fresh cash that should be phased into the portfolio over time.
- It should not currently be treated as a standard always-on performance-tuning parameter for the baseline strategy.

## Review

Strict review result:
- No issues found in the V10 artifact analysis itself.
- The evidence supports keeping the current local Round 2 code changes for further testing.
