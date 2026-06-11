# Aegis OptA 2016-2026 Analysis - 2026-05-20

## Step 1: Intake

Date:
- 2026-05-20

Scope:
- Analyze the uploaded `OptA_2016-2026` validation backtest and compare it with current default and no-pre-weak/base.

Uploaded files:
- `OptA_2016-2026.json`
- `OptA_2016-2026_orders.csv`
- `OptA_2016-2026_logs.txt`

Normalized log:
- `2026-05-20_214027__AegisGrowthAllocation__OptA_2016-2026_logs.txt`

OptA parameters:
- `favorable-breadth-threshold=0.85`
- `weak-stress-threshold=32`
- `growth-atr-eligibility-limit=0.06`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`

Comparison targets:
- `DiagSummary_2016-2026`: current default defensive behavior.
- `NoPreWeak_2016-2026`: same current code with pre-weak guard disabled, matching the prior base behavior.

## Step 2: Findings

Date:
- 2026-05-20

Important parameter validation issue:
- The uploaded run requested `weak-stress-threshold=32`.
- The algorithm rejected it as out of range and logged: `[AEGIS] Out-of-range decimal parameter weak-stress-threshold=32. Using default 27.`
- Therefore this run is not exactly the intended optimizer candidate.
- Actual effective parameters were:
  - `favorable-breadth-threshold=0.85`
  - `weak-stress-threshold=27`
  - `growth-atr-eligibility-limit=0.06`

OptA results:
- CAGR: 18.561%
- Max drawdown: 13.700%
- Net profit: 449.505%
- End equity: $164,851.61
- Sharpe Ratio: 1.029
- Sortino Ratio: 1.117
- PSR: 78.953%
- Total orders: 1,530
- Total fees: $1,827.25
- Portfolio turnover: 3.02%

OptA diagnostic summary:
- `Weeks=522`
- `PreWeakWeeks=72`
- `NonPreWeakWeeks=450`
- `SevereCrashWeeks=0`
- `WeakRegimeWeeks=66`
- `PreWeakAvgDrawdown=0.0762`
- `NonPreWeakAvgDrawdown=0.0263`
- `PreWeakNextReturnAvg=0.0028`
- `PreWeakFwd4Avg=0.0102`
- `PreWeakFwd8Avg=0.0291`
- `PreWeakFwd12Avg=0.0540`
- `PreWeakFwd4WinRate=0.7083`
- `NonPreWeakAvgTarget=G0.4720/D0.2780/C0.2500`

Versus current default:
- CAGR improved from 18.421% to 18.561%, +0.140 percentage points.
- Max drawdown improved from 13.800% to 13.700%, -0.100 percentage points.
- End equity improved from $162,912.56 to $164,851.61, +$1,939.05.
- Sharpe improved from 0.994 to 1.029.
- Total orders fell from 1,594 to 1,530, -64.
- Fees fell from $1,890.01 to $1,827.25, -$62.76.

Versus no-pre-weak/base:
- CAGR is lower by 0.307 percentage points.
- Max drawdown is better by 2.8 percentage points.
- End equity is lower by $4,322.46.
- Orders are lower by 10.

Year-by-year OptA versus current default:
- 2016: +0.7201 percentage points
- 2017: +3.2721 percentage points
- 2018: +2.3165 percentage points
- 2019: -2.2393 percentage points
- 2020: +0.2986 percentage points
- 2021: +0.0890 percentage points
- 2022: +0.0249 percentage points
- 2023: -0.6756 percentage points
- 2024: -2.4290 percentage points
- 2025: -0.1831 percentage points

Interpretation:
- Even with `weak-stress-threshold` falling back to 27, OptA is an improvement over the current default.
- The main useful change is likely `favorable-breadth-threshold=0.85`.
- `growth-atr-eligibility-limit=0.06` is the current default, so it did not create a new behavioral change.
- Because `weak-stress-threshold=32` was not accepted, the optimizer result cannot be trusted as a true test of that parameter until the validation range is fixed or a lower valid candidate is used.

Recommendation:
- Do not change defaults yet.
- Run crisis-window validation for the effective OptA settings before adopting `favorable-breadth-threshold=0.85`.
- Separately fix or widen the valid range for `weak-stress-threshold` if we want to genuinely test 30/32.

Suggested immediate validation runs:
- `OptA_2007-2010`
- `OptA_2019-2020`
- `OptA_2021-2022`

Use actual effective parameters:
- `favorable-breadth-threshold=0.85`
- omit `weak-stress-threshold`, or set it to `27`
- `growth-atr-eligibility-limit=0.06`
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`

Strict review:
- No code changes were made during this analysis.
- The OptA backtest set is complete.
- The normalized OptA log should be marked reviewed in `log-index.csv`.

## Step 3: Verification

Date:
- 2026-05-20

Checks:
- Confirmed the normalized OptA log contains `[AEGIS-DIAG-SUMMARY]`.
- Confirmed the OptA log contains the parameter fallback warning for `weak-stress-threshold=32`.
- Confirmed OptA metrics match the optimization-table top-row values.
- Updated `log-index.csv` to mark the normalized OptA log as reviewed and linked to this note.
