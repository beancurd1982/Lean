# Aegis Crisis Backtest Results - 2026-05-08

## Step 1: Intake

Date:
- 2026-05-08

Summary:
- User provided three backtest result screenshots for crisis-focused evaluation windows.
- User asked for an analysis of the results and for any additional information needed to interpret them confidently.

Artifacts reviewed:
- screenshot set for crisis backtest run A
- screenshot set for crisis backtest run B
- screenshot set for crisis backtest run C

Important limitation:
- The screenshots do not identify which result corresponds to which date window.
- Analysis therefore treats them as run A, run B, and run C unless the user later maps them to exact periods.

## Step 2: Window Mapping

Date:
- 2026-05-08

User mapping:
- run A = `2007-10-01` to `2010-12-31`
- run B = `2019-07-01` to `2020-12-31`
- run C = `2021-01-01` to `2022-12-31`

Implication:
- The crisis-backtest interpretation can now be made period-specific instead of generic.

## Step 3: Result Summary

Date:
- 2026-05-08

Run A: `2007-10-01` to `2010-12-31`
- net profit `6.393%`
- CAGR `1.923%`
- max drawdown `21.500%`
- Sharpe `0.004`
- Sortino `0.004`
- total orders `412`
- fees `$909.90`

Run B: `2019-07-01` to `2020-12-31`
- net profit `45.975%`
- CAGR `28.551%`
- max drawdown `13.500%`
- Sharpe `1.44`
- Sortino `1.401`
- total orders `198`
- fees `$214.86`

Run C: `2021-01-01` to `2022-12-31`
- net profit `14.202%`
- CAGR `6.878%`
- max drawdown `16.500%`
- Sharpe `0.455`
- Sortino `0.556`
- total orders `303`
- fees `$303.69`

## Step 4: Direct Interpretation

Date:
- 2026-05-08

Initial read from the screenshots:
- `2007-10-01` to `2010-12-31` is the weakest crisis result and is the main pressure point for the current strategy
- `2019-07-01` to `2020-12-31` is clearly the strongest window and indicates the model handles a fast crash and rebound much better
- `2021-01-01` to `2022-12-31` is acceptable but not strong enough on its own to call the strategy reliably defensive

## Step 5: Benchmark Context

Date:
- 2026-05-08

Summary:
- Added broad U.S. market context to avoid judging crisis-window results in isolation.

Benchmark context used:
- S&P 500 total-return reference for annual returns from Slickcharts
- monthly total-return history from OfficialData for partial-window comparisons
- annual total-return references for 2021 and 2022 from DQYDJ

Relative interpretation:
- `2007-10-01` to `2010-12-31`: Aegis finished positive while the broad market context for the window was materially worse, but the strategy still absorbed a `21.5%` drawdown, which is high for a strategy intended to be clearly defensive
- `2019-07-01` to `2020-12-31`: Aegis appears strong both absolutely and relatively, with materially higher return and controlled drawdown
- `2021-01-01` to `2022-12-31`: Aegis appears respectable relative to the broad market, though the drawdown is still not especially low

## Step 6: Current Conclusion

Date:
- 2026-05-08

Conclusion:
- Aegis appears to outperform the broad U.S. market across all three crisis-oriented windows.
- The main unresolved weakness is not outright failure during the global financial crisis window, but that the drawdown profile in `2007-10-01` to `2010-12-31` still looks too loose for a strategy whose stated goal is strong defensiveness.

Additional information that would sharpen the diagnosis:
- benchmark equity curves from the same QuantConnect runs
- Aegis equity curves for the three windows
- weekly summary logs around the worst `2008` to `2009` drawdown segment
- observed cash or defensive sleeve weights during the worst stress periods
