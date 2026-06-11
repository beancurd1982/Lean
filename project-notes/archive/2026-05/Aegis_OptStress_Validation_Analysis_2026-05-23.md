# Aegis OptStress Validation Analysis - 2026-05-23

## Step 1: Scope

User uploaded the five validation backtest result sets for the optimizer candidate:
- `favorable-breadth-threshold=0.85`
- `weak-stress-threshold=33`
- `severe-stress-gap=4`
- `growth-atr-eligibility-limit=0.06`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `crisis-diagnostics=true`

Validation windows expected:
- `2007-10-01` to `2008-12-31`
- `2009-01-01` to `2010-12-31`
- `2019-07-01` to `2020-12-31`
- `2021-01-01` to `2022-12-31`
- `2023-01-01` to `2026-01-01`

Planned work:
- Locate the uploaded result files.
- Extract overview metrics from JSON files.
- Inspect orders/logs for risk signals if present.
- Compare the optimized candidate against available prior/current-default validation runs.
- Decide whether the optimized values are safe to promote as defaults.

## Step 2: Files Located And Normalized

Uploaded result files were found in:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs`

Result sets:
- `OptStress_01_2007-10_2008-12.json`
- `OptStress_01_2007-10_2008-12_orders.csv`
- `OptStress_02_2009_2010.json`
- `OptStress_02_2009_2010_orders.csv`
- `OptStress_03_2019-07_2020.json`
- `OptStress_03_2019-07_2020_orders.csv`
- `OptStress_04_2021_2022.json`
- `OptStress_04_2021_2022_orders.csv`
- `OptStress_05_2023_2026.json`
- `OptStress_05_2023_2026_orders.csv`

The uploaded log files were normalized by the repo-local renamer:
- `2026-05-23_203017__AegisGrowthAllocation__OptStress_01_2007-10_2008-12_logs.txt`
- `2026-05-23_203225__AegisGrowthAllocation__OptStress_02_2009_2010_logs.txt`
- `2026-05-23_203326__AegisGrowthAllocation__OptStress_03_2019-07_2020_logs.txt`
- `2026-05-23_203425__AegisGrowthAllocation__OptStress_04_2021_2022_logs.txt`
- `2026-05-23_203521__AegisGrowthAllocation__OptStress_05_2023_2026_logs.txt`

Parameter confirmation from logs:
- `FavorableBreadthThreshold=0.85`
- `WeakStressThreshold=33`
- `SevereStressGap=4`
- `SevereStressThreshold=37`
- `GrowthAtrEligibilityLimit=0.06`

## Step 3: Overview Metrics

| Run | Window | Net Profit | CAR | Drawdown | Sharpe | Sortino | PSR | Win Rate | Orders | Fees | Turnover | Recovery |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| OptStress 01 | 2007-10-01 to 2008-12-31 | -16.813% | -13.652% | 20.300% | -1.457 | -1.422 | 0.149% | 46% | 145 | $351.63 | 2.96% | 8 |
| OptStress 02 | 2009-01-01 to 2010-12-31 | 30.698% | 14.330% | 10.200% | 1.083 | 1.199 | 56.535% | 60% | 277 | $663.40 | 2.77% | 191 |
| OptStress 03 | 2019-07-01 to 2020-12-31 | 52.993% | 32.623% | 13.200% | 1.604 | 1.599 | 77.659% | 75% | 201 | $214.00 | 2.82% | 140 |
| OptStress 04 | 2021-01-01 to 2022-12-31 | 18.200% | 8.736% | 13.600% | 0.612 | 0.777 | 31.225% | 63% | 297 | $297.57 | 3.09% | 51 |
| OptStress 05 | 2023-01-01 to 2026-01-01 | 88.887% | 23.598% | 10.900% | 1.028 | 1.275 | 80.910% | 70% | 534 | $534.04 | 3.50% | 207 |

## Step 4: Comparison Against Prior Relevant Runs

Closest prior comparison set:
- `G-current-all-defensive-off` for 2007-2008, 2019-2020, and 2021-2022.
- Existing 2009 and 2010 comparison runs were one-year windows, so they are not directly comparable to the new combined 2009-2010 run.

Comparison:
- 2007-2008:
  - Current/default-like prior: Net Profit `-16.860%`, Drawdown `20.300%`, Sharpe `-1.46`, Orders `144`.
  - OptStress: Net Profit `-16.813%`, Drawdown `20.300%`, Sharpe `-1.457`, Orders `145`.
  - Interpretation: essentially unchanged. The optimized stress band does not materially improve 2008 defense.
- 2019-2020:
  - Current/default-like prior: Net Profit `45.966%`, Drawdown `13.500%`, Sharpe `1.44`, Sortino `1.401`, Orders `198`.
  - OptStress: Net Profit `52.993%`, Drawdown `13.200%`, Sharpe `1.604`, Sortino `1.599`, Orders `201`.
  - Interpretation: clearly better return and risk-adjusted performance with slightly lower drawdown and only three extra orders.
- 2021-2022:
  - Current/default-like prior: Net Profit `14.186%`, Drawdown `16.500%`, Sharpe `0.454`, Sortino `0.555`, Orders `303`.
  - OptStress: Net Profit `18.200%`, Drawdown `13.600%`, Sharpe `0.612`, Sortino `0.777`, Orders `297`.
  - Interpretation: clear improvement. Better return, lower drawdown, better Sharpe/Sortino, fewer orders.
- 2023-2026:
  - No exact prior validation window exists in the repo.
  - OptStress result is strong: Net Profit `88.887%`, CAR `23.598%`, Drawdown `10.900%`, Sharpe `1.028`, Sortino `1.275`.

Important earlier alternative:
- `F-strict-severe-plus-pre-weak` was much better in 2007-2008:
  - Net Profit `-11.548%`
  - Drawdown `15.200%`
  - Sharpe `-1.157`
- But it was weaker than OptStress in 2019-2020 and 2021-2022.
- This confirms a tradeoff: strict severe protection helps 2008 more, but costs too much in later periods.

## Step 5: Log And Order Review

Diagnostic summaries:
- Severe crash weeks were `0` in all five OptStress validation runs.
- This is expected with `severe-crash-override-enabled=false`, but it also confirms the higher computed severe threshold (`37`) is not contributing direct severe-crash behavior in these runs.
- Pre-weak guard remained active and materially allocated to cash when triggered:
  - Pre-weak average target was consistently `G0.2400/D0.3000/C0.4600`.

Order review:
- All order rows were `Filled`.
- All order types were `Market`.
- Order counts match the JSON overview totals:
  - 145, 277, 201, 297, 534.
- No obvious order-status issue was found in the CSV files.

## Step 6: Interpretation

The OptStress candidate passes broad validation except for one caveat:
- It improves 2019-2020, 2021-2022, and the post-2023 window.
- It does not worsen 2007-2008 versus the current/default-like run.
- It does not improve 2007-2008 defense either.

This means the candidate is acceptable if the goal is the best balanced live default found so far, but it is not a pure 2008-crash defensive improvement.

Recommendation:
- Promote `favorable-breadth-threshold=0.85`, `weak-stress-threshold=33`, `severe-stress-gap=4`, and `growth-atr-eligibility-limit=0.06` as default values only if we accept the unchanged 2008 drawdown.
- Do not enable strict severe-crash behavior as a default yet, because the earlier tests show it helps 2008 but weakens other periods.
- If we want to further improve 2008 specifically, the next experiment should be a targeted 2008-only crash override that activates only under very extreme market stress, rather than lowering the general weak-stress threshold again.

## Step 7: Strict Review

No code changes were made during this analysis.

Review result:
- The uploaded files and metrics are internally consistent.
- The candidate looks better as a balanced default, but the 2008 result remains a known weakness.
- Further code change should be treated as live-trading behavior change and confirmed before implementation.
