# V33 Backtest Review 2026-03-28

## Scope
- Review QuantConnect cloud backtest output for `MultiStockV33_Stable_Base`.
- Extract key performance, drawdown, and trade quality metrics from the provided JSON export.

## Progress Log
- 2026-03-28: Started backtest-result review for `C:\Users\douya\Downloads\V33 BackTestResults.json`.

## Review Log
- 2026-03-28: Initial scope review completed. No issues found. This task is limited to reading and interpreting the backtest JSON export.

## Findings
- Headline result: Start equity `$250,000`, end equity `$1,070,894.72`, net profit `328.358%`, CAGR `15.647%`, max drawdown `17.200%`, Sharpe `0.913`, Sortino `0.920`.
- Trade profile: `685` closed trades, `54.7%` win rate, profit factor `2.562`, average trade duration `43.19` days, maximum closed-trade drawdown `-$100,864.41`.
- Costs and activity: `1242` total orders, total fees `$3,948.01`, portfolio turnover `1.61%`, volume `$37.27M`.
- Per-symbol contribution is concentrated: top contributors were `NVDA` (`$179,191.78`), `COST` (`$153,012.79`), and `AVGO` (`$88,933.47`), while `JPM` contributed only `$15,292.20` across `116` trades.
- The exported benchmark chart is flat at `0.0` throughout, so benchmark-relative fields such as alpha/beta are not informative in this run.

## Verification
- 2026-03-28: Backtest JSON parsed successfully with PowerShell `ConvertFrom-Json`. No data-integrity issue found in the export structure.
- 2026-03-28: Review completed. No issues found in the review workflow itself. The main analytical caveat is the missing/zero benchmark series in the exported result payload.
