# Aegis Base Vs Defensive Optimization Analysis 2026-05-18

## Step 1: Intake

Date:
- 2026-05-18

Request:
- Analyze backtest result files uploaded to `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs`.
- Compare `Base` results from repo version `29a06184b1f9393821e21d33e4eb785bfd3c06fb` against latest `DefensivelyOptimized` results.
- Verify whether the latest algorithm has been optimized.

Files observed:
- `Base.json`
- `Base_orders.csv`
- `Base_logs.txt`
- `DefensivelyOptimized.json`
- `DefensivelyOptimized_orders.csv`
- `DefensivelyOptimized_logs.txt`

Plan:
- Preserve the user's base/optimized grouping while following the project backtest-log workflow where possible.
- Parse overview JSON metrics, order CSV summaries, and diagnostic/log evidence.
- Compare headline performance, risk, order count/fees, and any parameter/date mismatch.
- Record findings and review result before final response.

Open questions:
- None yet. If the files contain different date ranges or incompatible parameters, call that out before drawing conclusions.

## Step 2: Refreshed Base Upload

Date:
- 2026-05-18

Observation:
- User uploaded a refreshed original-algorithm baseline after confirming it starts at `2016-01-01`.
- `Base.json` and `Base_orders.csv` now have later timestamps and larger file sizes than the initially inspected baseline.
- The prior mismatch where `Base` started at `2018-01-01` must be discarded.

Next action:
- Rerun the repo-local `BackTestLogs` renamer/index workflow.
- Parse the refreshed `Base` files and existing `DefensivelyOptimized` files from scratch.

## Step 3: Normalization And Data Validation

Files after renamer:
- `Base.json`
- `Base_orders.csv`
- `2026-05-18_221018__AegisGrowthAllocation__Base_logs.txt`
- `DefensivelyOptimized.json`
- `DefensivelyOptimized_orders.csv`
- `2026-05-18_221205__AegisGrowthAllocation__DefensivelyOptimized_logs.txt`

Validation:
- Both JSON files report `Status=Completed`.
- Runtime errors: none in either JSON.
- Both runs now cover the same window:
  - Base: `2016-01-01T00:00:00Z` to `2026-01-01T23:59:59Z`
  - DefensivelyOptimized: `2016-01-01T00:00:00Z` to `2026-01-01T23:59:59Z`
- Both runs used `backtest-start=2016-01-01` and `backtest-end=2026-01-01`.
- No explicit defensive parameters were present in the optimized result payload; this is expected because the latest code now defaults pre-weak guard to enabled.

## Step 4: Headline Comparison

Metrics:

| Metric | Base | DefensivelyOptimized | Optimized Delta |
| --- | ---: | ---: | ---: |
| Net Profit | `463.914%` | `443.042%` | `-20.872 pp` |
| CAGR | `18.868%` | `18.421%` | `-0.447 pp` |
| Max Drawdown | `16.500%` | `13.800%` | `-2.700 pp` |
| Sharpe | `0.994` | `0.994` | `0.000` |
| Sortino | `1.080` | `1.075` | `-0.005` |
| End Equity | `$169,174.07` | `$162,912.56` | `-$6,261.51` |
| Orders | `1540` | `1594` | `+54` |
| Fees | `$1851.50` | `$1890.01` | `+$38.51` |
| Turnover | `3.04%` | `3.12%` | `+0.08 pp` |
| Win Rate | `70%` | `69%` | `-1 pp` |
| Profit-Loss Ratio | `1.20` | `1.24` | `+0.04` |
| Average Win | `0.47%` | `0.45%` | `-0.02 pp` |
| Average Loss | `-0.40%` | `-0.36%` | `+0.04 pp` |

Interpretation:
- The latest version is more defensive: max drawdown fell by `2.7` percentage points and average loss improved from `-0.40%` to `-0.36%`.
- The latest version is not better on absolute return over this full 2016-2026 period: net profit and CAGR are lower.
- Risk-adjusted return is roughly unchanged: Sharpe is identical and Sortino is nearly identical.

## Step 5: Year-Level Comparison

Annual return and intra-year drawdown:

| Year | Base Return | Optimized Return | Return Delta | Base Year DD | Optimized Year DD | DD Delta |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 2016 | `13.631%` | `11.710%` | `-1.921 pp` | `7.816%` | `8.185%` | `+0.369 pp` |
| 2017 | `24.199%` | `24.679%` | `+0.480 pp` | `6.695%` | `6.748%` | `+0.053 pp` |
| 2018 | `8.282%` | `7.253%` | `-1.029 pp` | `10.774%` | `11.036%` | `+0.262 pp` |
| 2019 | `23.001%` | `21.425%` | `-1.576 pp` | `7.489%` | `6.833%` | `-0.656 pp` |
| 2020 | `28.243%` | `29.075%` | `+0.832 pp` | `13.351%` | `13.352%` | `+0.001 pp` |
| 2021 | `36.574%` | `36.426%` | `-0.148 pp` | `5.201%` | `5.201%` | `0.000 pp` |
| 2022 | `-14.301%` | `-11.498%` | `+2.803 pp` | `16.083%` | `13.335%` | `-2.748 pp` |
| 2023 | `35.708%` | `32.238%` | `-3.470 pp` | `5.476%` | `5.554%` | `+0.078 pp` |
| 2024 | `26.402%` | `25.210%` | `-1.192 pp` | `11.019%` | `10.788%` | `-0.231 pp` |
| 2025 | `16.976%` | `16.478%` | `-0.498 pp` | `10.984%` | `10.387%` | `-0.597 pp` |

Interpretation:
- The clearest improvement is `2022`: optimized return improves by `2.803` percentage points and intra-year drawdown improves by `2.748` percentage points.
- The optimized version also modestly improves `2020` return and reduces drawdown in `2019`, `2024`, and `2025`.
- The return drag is spread across several non-crisis years, especially `2023`, `2016`, `2019`, `2018`, and `2024`.

## Step 6: Orders And Logs

Order comparison:
- Base: `1540` filled orders, `741` buys, `799` sells, gross buy value about `$4.47M`, gross sell value about `$4.49M`.
- DefensivelyOptimized: `1594` filled orders, `764` buys, `830` sells, gross buy value about `$4.49M`, gross sell value about `$4.50M`.
- Optimized version adds `54` filled orders and `$38.51` extra fees.

Log comparison:
- Both logs contain `397` weekly `[AEGIS]` entries.
- Neither log contains `[AEGIS-DIAG]`, which is expected because `crisis-diagnostics` was not enabled.
- Pre-weak-like target weeks increased from `22` in Base to `61` in DefensivelyOptimized.
- This supports that the optimized/default pre-weak behavior is materially changing allocation decisions.

## Step 7: Conclusion

Answer:
- The latest algorithm has been optimized for defense, but not for maximum total return.

Evidence supporting defensive optimization:
- Max drawdown improved from `16.500%` to `13.800%`.
- 2022 bear-market return improved from `-14.301%` to `-11.498%`.
- 2022 intra-year drawdown improved from `16.083%` to `13.335%`.
- Average loss improved from `-0.40%` to `-0.36%`.
- Profit-loss ratio improved from `1.20` to `1.24`.

Evidence against claiming broad performance optimization:
- Net profit fell from `463.914%` to `443.042%`.
- CAGR fell from `18.868%` to `18.421%`.
- Sharpe was unchanged at `0.994`.
- Sortino was nearly unchanged/slightly lower at `1.075` vs `1.080`.
- Orders and fees increased slightly.

Practical interpretation:
- If the deployment goal is "more defensive while preserving most return", the latest version is an improvement.
- If the deployment goal is "highest CAGR / highest ending equity", the original baseline is still stronger in this 2016-2026 full-period backtest.
- The optimized version delivers a lower-risk profile, not a strictly superior all-metric profile.

Review:
- No algorithm code changes were made for this analysis.
- The initial Base result with a `2018` start was discarded after the refreshed `2016` Base upload.
- Residual risk: this is still one long full-period comparison. Before live confidence is final, keep monitoring paper logs to confirm the lower-drawdown behavior does not create excessive cash drag in normal markets.
