## 2026-04-18 - Aegis V5 backtest review

### Task
- Review the `V5` Aegis confirmation backtest after locking the selected round-1 and round-2 defaults into code.

### Files reviewed
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Logs_V5.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/StrategyReport_V5.pdf`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Logs_V2.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Logs_V1.json`

### Intake note
- Ran `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Invoke-BackTestLogRename.ps1` with a process-level PowerShell execution-policy bypass because script execution is disabled by default on this machine.
- The script completed successfully.
- `log-index.csv` remains unchanged because the new `V5` artifacts are `pdf/json` backtest result files, not new raw `.txt` log files for normalization.

### V5 headline metrics
- Net Profit: `265.093%`
- CAGR: `17.457%`
- Max Drawdown: `14.9%`
- Sharpe: `0.864`
- Sortino: `0.949`
- PSR: `60.107%`
- Portfolio Turnover: `2.99%`
- Total Orders: `1252`

### Comparison
- Versus `V1`:
  - Net Profit improved from `242.35%` to `265.093%`
  - Sharpe improved from `0.803` to `0.864`
  - PSR improved from `52.65%` to `60.107%`
  - Turnover improved from `3.12%` to `2.99%`
  - Drawdown stayed flat at `14.9%`
- Versus `V2`:
  - Net Profit improved from `256.538%` to `265.093%`
  - Sharpe improved from `0.832` to `0.864`
  - PSR improved from `55.671%` to `60.107%`
  - Turnover moved slightly from `2.95%` to `2.99%`
  - Drawdown stayed flat at `14.9%`

### Interpretation
- `V5` matches the top round-2 optimization region and confirms that the code-default baseline now reproduces the selected winner without cloud parameter overrides.
- This is a successful confirmation run, not a regression.
- The main remaining realism warning still present in the backtest analysis is `OrderFillsDuringExtendedMarketHoursAnalysis`.

### Open risks
- QuantConnect still reports extended-hours order-fill analysis findings in `V5`.
- The issue appears reduced versus the earlier January 1 startup behavior, but it is not fully eliminated.
- This should be treated as the next realism item if another development round is opened.
