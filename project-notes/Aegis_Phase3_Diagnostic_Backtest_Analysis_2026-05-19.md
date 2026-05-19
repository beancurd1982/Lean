# Aegis Phase 3 Diagnostic Backtest Analysis

## Scope

Date:
- 2026-05-19

Files analyzed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/DiagDefault_2016-2026.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/DiagDefault_2016-2026_orders.csv`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-05-19_213850__AegisGrowthAllocation__DiagDefault_2016-2026_logs.txt`

Run setup:
- `backtest-start=2016-01-01`
- `backtest-end=2026-01-01`
- `crisis-diagnostics=true`
- Defensive behavior left at current defaults.

## Summary

The Phase 3 diagnostic run exactly matches the prior `DefensivelyOptimized` run on headline metrics and order file contents. This confirms the Phase 1 live-state persistence change did not alter backtest trading behavior.

The run also confirms the current improved algorithm remains more defensive than the original baseline, but still gives up some long-run return:
- Baseline CAGR: `18.868%`
- Phase 3/current CAGR: `18.421%`
- Baseline max drawdown: `16.500%`
- Phase 3/current max drawdown: `13.800%`
- Baseline ending equity: `$169,174.07`
- Phase 3/current ending equity: `$162,912.56`

## Headline Metrics

Current Phase 3 / defensively optimized:
- Total orders: `1594`
- Net profit: `443.042%`
- CAGR: `18.421%`
- Max drawdown: `13.800%`
- Sharpe: `0.994`
- Sortino: `1.075`
- PSR: `74.337%`
- Win rate: `69%`
- Profit-loss ratio: `1.24`
- Total fees: `$1,890.01`
- Portfolio turnover: `3.12%`
- End equity: `$162,912.56`

Baseline comparison:
- Total orders: `1540`
- Net profit: `463.914%`
- CAGR: `18.868%`
- Max drawdown: `16.500%`
- Sharpe: `0.994`
- Sortino: `1.080`
- PSR: `73.313%`
- Total fees: `$1,851.50`
- Portfolio turnover: `3.04%`
- End equity: `$169,174.07`

## Behavior Check Against Prior Defensive Run

Comparison against `DefensivelyOptimized`:
- Orders CSV SHA/content matched exactly.
- Total order count matched exactly: `1594`.
- Gross order value matched exactly: `$8,987,337.18`.
- Headline metrics matched exactly.
- Annual return path matched exactly.

Interpretation:
- The Phase 1 live-state persistence implementation is behavior-neutral in backtest mode.
- This is the intended result because Phase 1 changed persisted live state only, not allocation rules.

## Annual Returns

Current Phase 3 / defensively optimized annual path:
- 2016: `11.7101%`
- 2017: `24.6793%`
- 2018: `7.5699%`
- 2019: `21.6092%`
- 2020: `29.3700%`
- 2021: `35.4267%`
- 2022: `-11.4439%`
- 2023: `32.2375%`
- 2024: `24.0568%`
- 2025: `17.1002%`

Baseline annual path:
- 2016: `13.6307%`
- 2017: `24.1992%`
- 2018: `8.6022%`
- 2019: `23.1870%`
- 2020: `28.5244%`
- 2021: `35.5865%`
- 2022: `-14.2495%`
- 2023: `35.7078%`
- 2024: `25.2420%`
- 2025: `17.5983%`

Interpretation:
- The main defensive benefit remains 2022, where drawdown and annual loss improved materially.
- The main return drag appears in normal or recovery years, especially 2016, 2018, 2019, and 2023.
- 2020 improved slightly on full-year return, but the COVID crash segment itself was essentially unchanged.

## Stress Period Checks

Current Phase 3 / defensively optimized:
- Max drawdown: `13.7738%`, from `2021-12-29` to `2022-10-10`.
- 2018 Q4: `-8.4899%`.
- COVID crash window `2020-02-19` to `2020-03-23`: `-11.8755%`.
- 2022 full year: `-11.4439%`.

Baseline:
- Max drawdown: `16.5079%`, from `2021-12-29` to `2022-10-10`.
- 2018 Q4: `-8.2211%`.
- COVID crash window `2020-02-19` to `2020-03-23`: `-11.8449%`.
- 2022 full year: `-14.2495%`.

Interpretation:
- Defensive optimization helped the long 2022 drawdown.
- It did not materially help the fast COVID crash.
- It slightly hurt 2018 Q4.

## Orders

Current Phase 3:
- Orders: `1594`
- Buys: `764`
- Sells: `830`
- Gross order value: `$8,987,337.18`
- Fees: `$1,890.01`

Order count by year:
- 2016: `174`
- 2017: `117`
- 2018: `190`
- 2019: `141`
- 2020: `124`
- 2021: `177`
- 2022: `138`
- 2023: `171`
- 2024: `186`
- 2025: `176`

Interpretation:
- The current version trades slightly more than baseline overall.
- Additional turnover is not extreme, but the improvement in drawdown comes with higher fees and lower ending equity.

## Diagnostic Log Limitation

The QuantConnect log was truncated:
- The log contains weekly `[AEGIS-DIAG]` lines only through `2017-07-10`.
- QuantConnect then reports the 100KB log limit.

Available diagnostic lines:
- Weekly diagnostic rows parsed: `80`
- `PreWeakGuardActive=True`: `14` rows
- `SevereCrashOverrideActive=True`: `0` rows
- All pre-weak activity in the available diagnostic log occurred in 2016.

Available pre-weak forward behavior from the truncated diagnostic sample:
- Average next-week return after pre-weak active: `0.49%`
- Average 4-week forward return: `1.12%`
- Average 8-week forward return: `2.14%`
- Average 12-week forward return: `4.70%`

Interpretation:
- The visible 2016 pre-weak activations look like partial false positives: the algorithm de-risked into a market that often recovered over the next 4 to 12 weeks.
- This supports the earlier concern that pre-weak improves defense but may create return drag in non-crisis drawdowns.
- Because the log is truncated, this cannot yet be treated as full-period attribution evidence.

## Conclusion

Phase 3 confirms:
- Phase 1 persistence is behavior-neutral in backtest mode.
- The current default defensive version is safer than the baseline on max drawdown.
- The current default defensive version still gives up return, mostly outside the major 2022 defensive win.
- The diagnostic log format is too verbose for full-period attribution under QuantConnect's 100KB log cap.

Recommended next move:
- Do not tune pre-weak yet from this truncated log.
- Add compact diagnostic summary output or a lower-volume attribution mechanism before Phase 4 tuning.
- The next implementation should aggregate pre-weak/severe-crash attribution across the whole run and emit a small end-of-run summary, rather than logging every weekly diagnostic line.

## Review

No code changes were made for this analysis.

Risk:
- Full-period pre-weak attribution remains incomplete because of log truncation.

Open question:
- Whether to implement compact diagnostic summary counters next, or run several shorter diagnostic windows to avoid the log cap.
