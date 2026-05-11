# Aegis Severe-Crash Override Backtest Analysis

## Step 1: Intake

Date:
- 2026-05-11

Request:
- Analyze newly uploaded crisis backtest results in `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs`.
- New result IDs are `11` through `15`, mapped to the same five crisis periods used before.
- Ask for clarification if required.

Observed files:
- `11.json`, `11_logs.txt`, `11_orders.csv`
- `12.json`, `12_logs.txt`, `12_orders.csv`
- `13.json`, `13_logs.txt`, `13.csv`
- `14.json`, `14_logs.txt`, `14_orders.csv`
- `15.json`, `15_logs.txt`, `15_orders.csv`

Initial note:
- `13.csv` appears to be the period 13 orders file by naming pattern and location; this will be verified from headers before analysis.

Next step:
- Parse the overview JSON, diagnostics logs, and orders files for periods `11` through `15`.

## Step 2: Parsed Result Summary

Date:
- 2026-05-11

Input mapping:
- `11`: 2007-10-01 to 2008-12-31
- `12`: 2009-01-01 to 2009-12-31
- `13`: 2010-01-01 to 2010-12-31
- `14`: 2019-07-01 to 2020-12-31
- `15`: 2021-01-01 to 2022-12-31

Parameter check:
- All five runs used `crisis-diagnostics=true`.
- All five runs used `severe-crash-override-enabled=true`.
- All five runs used `weak-stress-overlay-enabled=false`.
- All five runs used `pre-weak-guard-enabled=false`.
- No explicit `severe-crash-override-drawdown-threshold` was found in the uploaded JSON parameters, so the code default of `0.10` applies.

Overview metrics:

| ID | Period | Net Profit | CAR | Drawdown | Sharpe | Orders | Severe Override Weeks |
|---:|---|---:|---:|---:|---:|---:|---:|
| 11 | 2007-10-01 to 2008-12-31 | -11.549% | -9.323% | 15.200% | -1.157 | 142 | 15 |
| 12 | 2009-01-01 to 2009-12-31 | 17.087% | 17.103% | 4.300% | 1.633 | 89 | 0 |
| 13 | 2010-01-01 to 2010-12-31 | 9.320% | 9.329% | 10.800% | 0.638 | 185 | 0 |
| 14 | 2019-07-01 to 2020-12-31 | 41.980% | 26.204% | 13.500% | 1.337 | 200 | 7 |
| 15 | 2021-01-01 to 2022-12-31 | 10.095% | 4.935% | 19.200% | 0.309 | 318 | 4 |

Diagnostics:
- `11` severe-crash override was active from 2008-09-22 through 2008-12-29.
- `14` severe-crash override was active from 2020-03-23 through 2020-05-04.
- `15` severe-crash override was active only on 2022-03-14, 2022-05-02, 2022-05-16, and 2022-06-21.
- `12` had severe stress weeks, but diagnostic drawdown never reached the 10% trigger threshold.
- `13` maximum diagnostic drawdown was 9.77%, so it also never reached the 10% trigger threshold.

Orders file check:
- `13.csv` has the expected orders header and was treated as the period 13 orders file.
- CSV order counts matched JSON total orders for all five runs.

## Step 3: Comparison Against Earlier Crisis Runs

Date:
- 2026-05-11

Comparison baseline:
- `1` through `5`: earlier baseline severe-period runs.
- `6` through `10`: previous experiment set.
- `11` through `15`: new severe-crash override experiment.

Comparison against previous experiment set:

| Period | New ID | Net Profit Delta | CAR Delta | Drawdown Delta | Sharpe Delta | Read |
|---|---:|---:|---:|---:|---:|---|
| 2007-10-01 to 2008-12-31 | 11 | +5.308 pts | +4.366 pts | -5.100 pts | +0.303 | Clear improvement versus previous experiment |
| 2009-01-01 to 2009-12-31 | 12 | 0.000 pts | 0.000 pts | 0.000 pts | 0.000 | No effect; override never triggered |
| 2010-01-01 to 2010-12-31 | 13 | -1.409 pts | -1.410 pts | +0.600 pts | -0.111 | Worse despite no override trigger; likely from code-path or run-set differences versus `8` |
| 2019-07-01 to 2020-12-31 | 14 | -4.868 pts | -2.857 pts | 0.000 pts | -0.153 | Worse; override likely missed part of the rebound |
| 2021-01-01 to 2022-12-31 | 15 | -7.908 pts | -3.711 pts | +5.400 pts | -0.300 | Clearly worse; isolated override weeks caused defensive whipsaw |

Comparison against original baseline set:

| Period | New ID | Net Profit Delta | CAR Delta | Drawdown Delta | Sharpe Delta | Read |
|---|---:|---:|---:|---:|---:|---|
| 2007-10-01 to 2008-12-31 | 11 | +0.182 pts | +0.148 pts | -0.200 pts | +0.159 | Slight improvement |
| 2009-01-01 to 2009-12-31 | 12 | +3.531 pts | +3.535 pts | +1.500 pts | +0.182 | Better return, higher drawdown |
| 2010-01-01 to 2010-12-31 | 13 | +1.238 pts | +1.239 pts | +0.100 pts | +0.051 | Slightly better than original baseline |
| 2019-07-01 to 2020-12-31 | 14 | +4.263 pts | +2.528 pts | +2.600 pts | +0.021 | Better return, materially higher drawdown |
| 2021-01-01 to 2022-12-31 | 15 | -4.109 pts | -1.944 pts | +3.300 pts | -0.165 | Worse than original baseline |

## Step 4: Interpretation

Date:
- 2026-05-11

Findings:
- The severe-crash override is not ready to promote as-is.
- It works best in the 2008-style prolonged crash because it stays active for a sustained period after the threshold is crossed.
- It is harmful in fast-recovery or choppy bear-market conditions. In 2020, it activates after the drawdown is already deep and remains defensive through part of the rebound. In 2022, it activates intermittently and then releases, causing buy/sell churn.
- The 10% drawdown threshold is too late for fast crashes and too blunt for long choppy markets.
- The current release rule is too loose. It can leave severe mode one week after entering even if drawdown remains high, because `Severe=True` can disappear while the portfolio is still stressed.
- The 80% cash target did not reliably lower reported drawdown in 2020 or 2022. This means the timing and release logic matter more than simply raising cash.

Order observations:
- `11` traded heavily around the 2008 September-October crash and then stayed defensive; this improved the previous experiment drawdown materially.
- `14` sold into March 2020 stress, then bought again during the rebound window; this likely reduced upside capture.
- `15` repeatedly sold on severe weeks and bought back the following weeks, especially around March-May 2022; this increased turnover and worsened drawdown and return.

QuantConnect analysis flags:
- Extended market hours order-fill warnings appear in all five JSON files.
- Portfolio margin underuse appears in all five JSON files, consistent with intentional defensive cash behavior.
- Relative-to-benchmark/statistical-significance warnings remain in most periods and should not be ignored when evaluating promotion.

## Step 5: Recommendation

Date:
- 2026-05-11

Recommendation:
- Do not promote `severe-crash-override-enabled=true` as the default behavior.
- Keep it as an experiment branch/parameter for now.
- Next experiment should focus on hysteresis and staged exposure, not a hard one-week switch to `G0/D20/C80`.

Specific next experiment:
- Enter severe-crash mode at drawdown >= 10% with `Weak` and `Severe=True`.
- Stay in severe-crash mode until drawdown improves below 7% or regime recovers to `Neutral`/`Strong` for at least two consecutive weekly reviews.
- Use a less extreme severe target first: `G0.05/D0.35/C0.60` instead of `G0/D0.20/C0.80`.
- Add diagnostics for `SevereCrashModeState=enter/hold/exit` so we can separate initial trigger from continuation behavior.

Open questions:
- Confirm whether run set `6` through `10` is the intended comparison target for the new `11` through `15` set.
- Confirm whether `13.csv` should be renamed to `13_orders.csv` for consistency.

## Step 6: Documentation-Only Review

Date:
- 2026-05-11

Review scope:
- No algorithm code was changed during this analysis.
- New documentation file added: `project-notes/Aegis_Severe_Crash_Override_Backtest_Analysis_2026-05-11.md`.

Review findings:
- No code safety issue introduced.
- The analysis explicitly records file naming irregularity for `13.csv`.
- The recommendation does not change live behavior.
- Main residual risk is comparison ambiguity if `6` through `10` are not the intended prior experiment baseline.

## Step 7: Rename Plan

Date:
- 2026-05-11

Request:
- Rename `13.csv` to a consistent orders-file name.
- If a better naming rule exists, apply it to the crisis backtest result files in `CrisisBackTestLogs`.
- Record the analysis findings and rename rule in this note.

Proposed naming rule:
- Normalize numbered crisis backtest result artifacts to:
  - `<run-id>__<start>_to_<end>__AegisGrowthAllocation__<experiment-label>__<artifact-type>.<ext>`
- Artifact types:
  - `overview.json`
  - `logs.txt`
  - `orders.csv`
- Keep the numeric run id at the front so chronological experiment grouping remains obvious.
- Keep the date window in the filename so files are understandable without opening the JSON.
- Keep an experiment label so baseline, pre-weak, and severe-crash runs can be compared safely.

Scope:
- Rename numbered result sets `1` through `15`.
- Do not rename already-descriptive raw/legacy log files unless they are part of the numbered result sets.

Next step:
- Inspect current folder contents and apply non-overlapping file renames.

## Step 8: Rename Execution Correction

Date:
- 2026-05-11

Issue during first rename attempt:
- The first PowerShell rename command used an unsafe interpolation form for run `1`.
- `1.json`, `1_logs.txt`, and `1_orders.csv` were moved to `.json`, `.txt`, and `.csv`.
- The command stopped before overwriting any intended target.

Correction plan:
- Rename `.json`, `.txt`, and `.csv` to the intended run `01` names.
- Rename remaining numbered result sets `2` through `15` using explicit `${id}` interpolation and target collision checks.
- Verify no numbered artifacts or accidental dot-prefixed artifacts remain afterward.

## Step 9: Rename Completion and Review

Date:
- 2026-05-11

Completed rename:
- Normalized all numbered result artifacts from runs `1` through `15`.
- `13.csv` is now:
  - `13__2010-01-01_to_2010-12-31__AegisGrowthAllocation__severe-crash-override__orders.csv`
- The three accidental dot-prefixed files were corrected to run `01` names.

Final naming groups:
- `01` through `05`: `weak-stress-overlay`
- `06` through `10`: `pre-weak-guard`
- `11` through `15`: `severe-crash-override`

Verification:
- Folder listing confirms each run has:
  - `__overview.json`
  - `__logs.txt`
  - `__orders.csv`
- No accidental `.json`, `.txt`, or `.csv` files remain.
- Already-descriptive legacy/raw logs were left unchanged.

Strict review:
- No algorithm code was changed during this rename.
- The rename improves auditability by making each artifact self-describing.
- Risk: Git reports this as deletes plus new files until staged, because the filenames changed substantially.
- Risk: any ad hoc script expecting `1.json` through `15.json` must be updated to the new descriptive convention.

Updated recommendation after the severe-crash analysis:
- Keep the severe-crash override disabled by default.
- Do not promote the hard `G0/D20/C80` override.
- Next implementation should test a stateful severe-crash mode with hysteresis and staged exposure:
  - Enter at drawdown >= 10%, `Weak`, and `Severe=True`.
  - Hold until drawdown improves below 7% or regime is `Neutral`/`Strong` for two consecutive weekly reviews.
  - Start with `G5/D35/C60`.
  - Log `SevereCrashModeState=enter/hold/exit`.

## Step 10: Commit and Push Preparation

Date:
- 2026-05-11

Request:
- Commit and push the local changes.

Intended commit scope:
- Severe-crash override implementation and focused tests.
- Severe-crash implementation project note.
- Severe-crash backtest analysis project note.
- Normalized crisis backtest artifacts for runs `01` through `15`.

Pre-commit checks to run:
- `git diff --check`
- Focused Aegis tests.
- Algorithm CSharp build.

## Step 11: Verification Attempt and Root Cause

Date:
- 2026-05-11

Verification attempt:
- Ran `git diff --check`, focused Aegis tests, and Algorithm CSharp build in parallel.

Result:
- `git diff --check` passed with only CRLF conversion warnings.
- Algorithm CSharp build passed with `0` warnings and `0` errors.
- Focused Aegis test command failed during project build before test execution.

Root cause:
- The test command and build command both wrote build outputs at the same time.
- The failure was a file lock on `Common/bin/Debug/QuantConnect.Common.deps.json`.
- This is a verification orchestration issue, not an Aegis test failure.

Correction:
- Rerun the focused Aegis tests sequentially after the build finishes.

## Step 12: Pre-Commit Verification

Date:
- 2026-05-11

Final verification results:
- `git -c safe.directory=D:/Projects/Git/Lean-1 diff --check` passed with only CRLF conversion warnings.
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo -p:RunAnalyzers=false -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''` passed with `0` warnings and `0` errors.
- Sequential focused Aegis test run passed: `35` passed, `0` failed, `0` skipped.

Known warnings:
- The focused test command still reports existing NuGet vulnerability warnings from project dependencies.

Next step:
- Stage the intended Aegis changes and create a commit.

## Step 13: Staged Diff Check Fix

Date:
- 2026-05-11

Issue:
- After staging, `git diff --cached --check` reported trailing whitespace in the newly added severe-crash orders CSV files.
- The earlier unstaged `git diff --check` did not report these because the files were still untracked at that point.

Fix:
- Strip end-of-line whitespace from orders CSV artifacts.
- Restage the CSV files.
- Rerun staged diff check before committing.

Result:
- Stripped end-of-line whitespace from the 15 normalized orders CSV files.
- `git diff --cached --check` passed after restaging.

Next step:
- Commit the staged Aegis changes.

## Step 14: Commit Result

Date:
- 2026-05-11

Commit created:
- `b6377f5a0 feat: add Aegis severe crash override experiment`

Committed scope:
- Severe-crash override experiment implementation.
- Focused Aegis tests for severe-crash override behavior and diagnostics.
- Severe-crash implementation note.
- Severe-crash backtest analysis note.
- Normalized crisis backtest result artifacts for runs `01` through `15`.

Next step:
- Commit this note update, then push `research-algorithms`.
