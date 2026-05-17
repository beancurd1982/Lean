# Aegis Validation Matrix Run Sheet

> **For cloud backtest execution:** Use the current committed `research-algorithms` code. Do not change code before this matrix is complete.

**Goal:** Validate existing defensive mechanisms on the same code version before adding another rule.

**Architecture:** This is a cloud-backtest run sheet, not an implementation plan. The matrix compares existing parameter combinations across the same crisis windows and then validates surviving candidates on control/full-period windows.

**Tech Stack:** QuantConnect cloud backtests, AegisGrowthAllocation parameters, uploaded `overview.json`, `logs.txt`, and `orders.csv` files.

---

## Step 1: Purpose

Date:
- 2026-05-16

Why this exists:
- Runs `1-20` were useful, but they were optimized batch-by-batch.
- The strategy review concluded that the next move should be validation, not more code.
- This run sheet gives exact parameter sets, windows, naming, and decision gates.

Core rule:
- Do not implement two-stage severe-crash mode until this matrix shows the missing behavior cannot be expressed with current parameters.

## Step 2: Parameter Constraints

Use these exact parameter names.

Common parameters for every run:
- `backtest-start=<window start>`
- `backtest-end=<window end>`
- `crisis-diagnostics=true`

Available overlay toggles:
- `weak-stress-overlay-enabled`
- `pre-weak-guard-enabled`
- `severe-crash-override-enabled`

Stateful severe-crash parameters:
- `sev-crash-dd-entry=0.10`
- `sev-crash-dd-exit=0.07`
- `sev-crash-recovery-wks=2`

Important constraint:
- Do not set `pre-weak-guard-drawdown-threshold` in cloud runs. It is longer than the 30-character cloud parameter limit.
- For this matrix, pre-weak uses its current default threshold of `0.05`.

## Step 3: Configurations

Run these five required configurations in Phase 1.

### `A-default-off`

Purpose:
- Current code baseline with diagnostics enabled and all defensive experiment overlays disabled.

Parameters:
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=false`
- `severe-crash-override-enabled=false`

### `B-weak-stress-only`

Purpose:
- Re-check weak-stress behavior on the current code version.

Parameters:
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=true`
- `pre-weak-guard-enabled=false`
- `severe-crash-override-enabled=false`

### `C-pre-weak-only`

Purpose:
- Re-check the current provisional benchmark on the current code version.

Parameters:
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=true`
- `severe-crash-override-enabled=false`

### `D-stateful-severe-only`

Purpose:
- Re-check the committed stateful severe-crash behavior on the current code version.

Parameters:
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=false`
- `severe-crash-override-enabled=true`
- `sev-crash-dd-entry=0.10`
- `sev-crash-dd-exit=0.07`
- `sev-crash-recovery-wks=2`

### `E-pre-weak-plus-stateful-severe`

Purpose:
- Test whether pre-weak plus stateful severe gives better 2008 protection without destroying 2021-22 behavior.

Parameters:
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=true`
- `severe-crash-override-enabled=true`
- `sev-crash-dd-entry=0.10`
- `sev-crash-dd-exit=0.07`
- `sev-crash-recovery-wks=2`

Interpretation note:
- Severe-crash has priority over pre-weak in the current code.
- This combined run does not simply add both effects. It tests current production priority behavior.

## Step 4: Phase 1 Crisis Windows

Run all five required configurations across these five windows.

| Window ID | Start | End | Purpose |
| --- | --- | --- | --- |
| `W1-2008-crash` | `2007-10-01` | `2008-12-31` | Systemic crash defense |
| `W2-2009-rebound` | `2009-01-01` | `2009-12-31` | Recovery participation |
| `W3-2010-chop` | `2010-01-01` | `2010-12-31` | Choppy post-crisis behavior |
| `W4-2020-covid` | `2019-07-01` | `2020-12-31` | Fast crash and fast recovery |
| `W5-2021-22-bear` | `2021-01-01` | `2022-12-31` | Inflation/rate bear market |

Required Phase 1 runs:
- `A-default-off` x `W1` to `W5`
- `B-weak-stress-only` x `W1` to `W5`
- `C-pre-weak-only` x `W1` to `W5`
- `D-stateful-severe-only` x `W1` to `W5`
- `E-pre-weak-plus-stateful-severe` x `W1` to `W5`

Total Phase 1 runs:
- 25 cloud backtests.

## Step 5: Phase 2 Control Windows

Only run Phase 2 after Phase 1 is analyzed.

Run Phase 2 for:
- `A-default-off`
- `C-pre-weak-only`
- the best Phase 1 candidate among `B`, `D`, or `E`

Control windows:

| Window ID | Start | End | Purpose |
| --- | --- | --- | --- |
| `C1-bull` | `2013-01-01` | `2014-12-31` | Normal bull market; defense should not over-trigger |
| `C2-chop` | `2015-01-01` | `2016-12-31` | Sideways/choppy market; avoid whipsaw |
| `C3-shallow-correction` | `2018-09-01` | `2018-12-31` | Shallow correction; avoid excessive cash drag |
| `C4-full-period` | `2007-10-01` | `2025-12-31` | Broad return preservation check |

Total Phase 2 runs:
- 12 cloud backtests.

## Step 6: Result Naming

Upload results into:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs`

Raw upload naming:
- Use simple temporary names during upload if needed:
  - `<run-number>.json`
  - `<run-number>_logs.txt`
  - `<run-number>_orders.csv`

Normalized naming after upload:
- `<run-number>__<start>_to_<end>__AegisGrowthAllocation__<config-id>__overview.json`
- `<run-number>__<start>_to_<end>__AegisGrowthAllocation__<config-id>__logs.txt`
- `<run-number>__<start>_to_<end>__AegisGrowthAllocation__<config-id>__orders.csv`

Example:
- `21__2007-10-01_to_2008-12-31__AegisGrowthAllocation__A-default-off__overview.json`
- `21__2007-10-01_to_2008-12-31__AegisGrowthAllocation__A-default-off__logs.txt`
- `21__2007-10-01_to_2008-12-31__AegisGrowthAllocation__A-default-off__orders.csv`

Suggested numbering:
- Phase 1 starts at `21`.
- Use consecutive numbers through `45`.
- Phase 2 starts at `46` after Phase 1 analysis.

## Step 7: Metrics To Capture

From overview JSON:
- Net Profit
- Compounding Annual Return
- Drawdown
- Sharpe Ratio
- Sortino Ratio
- Total Orders
- Total Fees
- Portfolio Turnover
- End Equity

From diagnostics logs:
- `SleeveOverride`
- `OverrideReason`
- `PreWeakGuardActive`
- `SevereCrashOverrideActive`
- `SevereCrashModeState`
- `SevereCrashRecoveryWeeks`
- `SevereCrashExitReason`
- `DrawdownFromHigh`
- active weeks per overlay
- first activation date
- last activation date
- exit reason count

From orders CSV:
- order count
- buy count
- sell count
- top traded symbols
- first order date
- last order date

## Step 8: Decision Gates

Do not judge by one window only.

### Phase 1 pass/fail gates

`C-pre-weak-only` remains the provisional benchmark if:
- it still leads aggregate crisis net profit or Sharpe, and
- its 2021-22 result remains best or near-best, and
- its 2008 weakness is the only major failure.

`E-pre-weak-plus-stateful-severe` becomes the lead candidate only if:
- it improves 2008 drawdown or net profit versus `C-pre-weak-only`, and
- it does not materially regress 2021-22 versus `C-pre-weak-only`, and
- it does not materially regress 2020 recovery capture versus `C-pre-weak-only`.

`D-stateful-severe-only` should be rejected unless:
- it beats `C-pre-weak-only` on 2008, and
- it does not materially lag `C-pre-weak-only` on 2021-22.

`B-weak-stress-only` remains useful only if:
- lower drawdown is the priority over annual return, or
- it identifies a cleaner drawdown-control component for future design.

### Phase 2 pass/fail gates

A candidate passes Phase 2 only if:
- full-period CAGR is not materially worse than baseline or pre-weak,
- full-period Sharpe is not materially worse than baseline or pre-weak,
- control-window false positives are limited,
- turnover and fees are reasonable,
- crisis protection improves without large recovery drag.

Practical materiality thresholds:
- Full-period CAGR loss greater than `1.0 percentage point` versus `A-default-off` is a warning.
- Full-period Sharpe loss greater than `0.10` versus `A-default-off` is a warning.
- 2021-22 net profit loss greater than `2.0 percentage points` versus `C-pre-weak-only` is a warning.
- 2008 drawdown worse than `C-pre-weak-only` is a rejection for any defensive candidate.

## Step 9: What Not To Do Yet

Do not implement:
- two-stage severe-crash mode
- new threshold aliases
- defensive sleeve composition changes
- parameter sensitivity sweeps

Reason:
- Those add new degrees of freedom.
- First, validate what the current committed code can already express.

## Step 10: After Phase 1

After uploading Phase 1 results:
- Ask Codex to rename the uploaded files using this run sheet.
- Ask Codex to analyze Phase 1 before running Phase 2.
- Do not start Phase 2 until Phase 1 identifies the surviving candidate.

Expected analysis output:
- a table comparing all 25 runs
- overlay activation summary
- pass/fail result against the gates above
- recommendation for which configurations proceed to Phase 2

## Step 11: Commit Status

This file is a planning artifact only.
- No algorithm code changes are needed for this run sheet.
- Commit this file before starting Phase 1 cloud runs.

## Step 12: Self-Review

Date:
- 2026-05-16

Review result:
- No blocking issue found.
- The run sheet uses current cloud-safe parameter names for stateful severe-crash settings.
- The long `pre-weak-guard-drawdown-threshold` parameter is explicitly excluded from cloud setup.
- The plan avoids new code and limits the next action to same-code-version validation.
- Phase 1 is intentionally crisis-only; Phase 2 control/full-period runs are conditional to reduce unnecessary cloud runs.

Verification:
- `git -c safe.directory=D:/Projects/Git/Lean-1 diff --check -- project-notes/Aegis_Validation_Matrix_Run_Sheet_2026-05-16.md`
  - passed.

## Step 13: Phase 1 Execution Checklist

Date:
- 2026-05-16

Status:
- Phase 1 is ready to run.
- Use run numbers `21` through `45`.
- Upload each completed run as:
  - `<run-number>.json`
  - `<run-number>_logs.txt`
  - `<run-number>_orders.csv`
- Codex will normalize names after upload.

Common parameter for every run:
- `crisis-diagnostics=true`

### Runs `21-25`: `A-default-off`

Parameters:
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=false`
- `severe-crash-override-enabled=false`

| Run | Window | `backtest-start` | `backtest-end` |
| --- | --- | --- | --- |
| `21` | `W1-2008-crash` | `2007-10-01` | `2008-12-31` |
| `22` | `W2-2009-rebound` | `2009-01-01` | `2009-12-31` |
| `23` | `W3-2010-chop` | `2010-01-01` | `2010-12-31` |
| `24` | `W4-2020-covid` | `2019-07-01` | `2020-12-31` |
| `25` | `W5-2021-22-bear` | `2021-01-01` | `2022-12-31` |

### Runs `26-30`: `B-weak-stress-only`

Parameters:
- `weak-stress-overlay-enabled=true`
- `pre-weak-guard-enabled=false`
- `severe-crash-override-enabled=false`

| Run | Window | `backtest-start` | `backtest-end` |
| --- | --- | --- | --- |
| `26` | `W1-2008-crash` | `2007-10-01` | `2008-12-31` |
| `27` | `W2-2009-rebound` | `2009-01-01` | `2009-12-31` |
| `28` | `W3-2010-chop` | `2010-01-01` | `2010-12-31` |
| `29` | `W4-2020-covid` | `2019-07-01` | `2020-12-31` |
| `30` | `W5-2021-22-bear` | `2021-01-01` | `2022-12-31` |

### Runs `31-35`: `C-pre-weak-only`

Parameters:
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=true`
- `severe-crash-override-enabled=false`

| Run | Window | `backtest-start` | `backtest-end` |
| --- | --- | --- | --- |
| `31` | `W1-2008-crash` | `2007-10-01` | `2008-12-31` |
| `32` | `W2-2009-rebound` | `2009-01-01` | `2009-12-31` |
| `33` | `W3-2010-chop` | `2010-01-01` | `2010-12-31` |
| `34` | `W4-2020-covid` | `2019-07-01` | `2020-12-31` |
| `35` | `W5-2021-22-bear` | `2021-01-01` | `2022-12-31` |

### Runs `36-40`: `D-stateful-severe-only`

Parameters:
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=false`
- `severe-crash-override-enabled=true`
- `sev-crash-dd-entry=0.10`
- `sev-crash-dd-exit=0.07`
- `sev-crash-recovery-wks=2`

| Run | Window | `backtest-start` | `backtest-end` |
| --- | --- | --- | --- |
| `36` | `W1-2008-crash` | `2007-10-01` | `2008-12-31` |
| `37` | `W2-2009-rebound` | `2009-01-01` | `2009-12-31` |
| `38` | `W3-2010-chop` | `2010-01-01` | `2010-12-31` |
| `39` | `W4-2020-covid` | `2019-07-01` | `2020-12-31` |
| `40` | `W5-2021-22-bear` | `2021-01-01` | `2022-12-31` |

### Runs `41-45`: `E-pre-weak-plus-stateful-severe`

Parameters:
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=true`
- `severe-crash-override-enabled=true`
- `sev-crash-dd-entry=0.10`
- `sev-crash-dd-exit=0.07`
- `sev-crash-recovery-wks=2`

| Run | Window | `backtest-start` | `backtest-end` |
| --- | --- | --- | --- |
| `41` | `W1-2008-crash` | `2007-10-01` | `2008-12-31` |
| `42` | `W2-2009-rebound` | `2009-01-01` | `2009-12-31` |
| `43` | `W3-2010-chop` | `2010-01-01` | `2010-12-31` |
| `44` | `W4-2020-covid` | `2019-07-01` | `2020-12-31` |
| `45` | `W5-2021-22-bear` | `2021-01-01` | `2022-12-31` |

Next step:
- Run `21` first and confirm the parameter screen before submitting if anything looks uncertain.

## Step 14: Phase 1 Run 21 Received

Date:
- 2026-05-16

Uploaded files:
- `21.json`
- `21_logs.txt`
- `21_orders.csv`

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/21__2007-10-01_to_2008-12-31__AegisGrowthAllocation__A-default-off__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/21__2007-10-01_to_2008-12-31__AegisGrowthAllocation__A-default-off__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/21__2007-10-01_to_2008-12-31__AegisGrowthAllocation__A-default-off__orders.csv`

Validation:
- Status: `Completed`
- Runtime error: `False`
- Start: `2007-10-01T00:00:00Z`
- End: `2008-12-31T23:59:59Z`
- Parameters matched `A-default-off`.

Metrics:
- Net Profit: `-16.860%`
- Compounding Annual Return: `-13.691%`
- Drawdown: `20.300%`
- Sharpe Ratio: `-1.46`
- Sortino Ratio: `-1.423`
- Total Orders: `144`
- Total Fees: `$350.54`
- End Equity: `24942.03`
- Portfolio Turnover: `2.96%`

Next run:
- `22`: `A-default-off`, `2009-01-01` to `2009-12-31`.

## Step 15: Phase 1 Run 22 Received

Date:
- 2026-05-16

Uploaded files:
- `22.json`
- `22_logs.txt`
- `22_orders.csv`

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/22__2009-01-01_to_2009-12-31__AegisGrowthAllocation__A-default-off__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/22__2009-01-01_to_2009-12-31__AegisGrowthAllocation__A-default-off__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/22__2009-01-01_to_2009-12-31__AegisGrowthAllocation__A-default-off__orders.csv`

Validation:
- Status: `Completed`
- Runtime error: `False`
- Start: `2009-01-01T00:00:00Z`
- End: `2009-12-31T23:59:59Z`
- Parameters matched `A-default-off`.

Metrics:
- Net Profit: `17.084%`
- Compounding Annual Return: `17.101%`
- Drawdown: `4.300%`
- Sharpe Ratio: `1.633`
- Sortino Ratio: `1.722`
- Total Orders: `89`
- Total Fees: `$176.25`
- End Equity: `35125.25`
- Portfolio Turnover: `1.97%`
- Win Rate: `67%`
- Profit-Loss Ratio: `1.40`

Initial comparison:
- Run `22` effectively matches prior 2009 runs `7`, `12`, and `17`.
- The earlier weak-stress run `2` had lower drawdown (`2.800%`) and fewer orders, but lower return (`13.556%`).

Next run:
- `23`: `A-default-off`, `2010-01-01` to `2010-12-31`.

## Step 16: Phase 1 Run 23 Received

Date:
- 2026-05-16

Uploaded files:
- `23.json`
- `23_logs.txt`
- `23_orders.csv`

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/23__2010-01-01_to_2010-12-31__AegisGrowthAllocation__A-default-off__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/23__2010-01-01_to_2010-12-31__AegisGrowthAllocation__A-default-off__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/23__2010-01-01_to_2010-12-31__AegisGrowthAllocation__A-default-off__orders.csv`

Validation:
- Status: `Completed`
- Runtime error: `False`
- Start: `2010-01-01T00:00:00Z`
- End: `2010-12-31T23:59:59Z`
- Parameters matched `A-default-off`.

Metrics:
- Net Profit: `9.326%`
- Compounding Annual Return: `9.335%`
- Drawdown: `10.800%`
- Sharpe Ratio: `0.639`
- Sortino Ratio: `0.735`
- Total Orders: `185`
- Total Fees: `$419.39`
- End Equity: `32797.84`
- Portfolio Turnover: `4.17%`
- Win Rate: `51%`
- Profit-Loss Ratio: `1.16`

Initial comparison:
- Run `23` effectively matches prior 2010 severe/stateful runs `13` and `18`.
- Prior pre-weak run `8` remains better in 2010: `10.729%` net profit, `10.200%` drawdown, and `0.749` Sharpe.
- Prior weak-stress run `3` had slightly lower drawdown than default but lower return.

Next run:
- `24`: `A-default-off`, `2019-07-01` to `2020-12-31`.

## Step 17: Phase 1 Run 24 Received

Date:
- 2026-05-16

Uploaded files:
- `24.json`
- `24_logs.txt`
- `24_orders.csv`

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/24__2019-07-01_to_2020-12-31__AegisGrowthAllocation__A-default-off__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/24__2019-07-01_to_2020-12-31__AegisGrowthAllocation__A-default-off__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/24__2019-07-01_to_2020-12-31__AegisGrowthAllocation__A-default-off__orders.csv`

Validation:
- Status: `Completed`
- Runtime error: `False`
- Start: `2019-07-01T00:00:00Z`
- End: `2020-12-31T23:59:59Z`
- Parameters matched `A-default-off`.

Metrics:
- Net Profit: `45.966%`
- Compounding Annual Return: `28.546%`
- Drawdown: `13.500%`
- Sharpe Ratio: `1.44`
- Sortino Ratio: `1.401`
- Total Orders: `198`
- Total Fees: `$214.87`
- End Equity: `43789.74`
- Portfolio Turnover: `2.96%`
- Win Rate: `71%`
- Profit-Loss Ratio: `1.36`

Initial comparison:
- Run `24` is close to prior pre-weak run `9`, but still slightly lower: `45.966%` net versus `46.848%`, Sharpe `1.44` versus `1.49`.
- Prior weak-stress run `4` still has the best drawdown (`10.900%`) but meaningfully lower return (`37.717%`).
- Prior severe/stateful severe runs `14` and `19` lag default on 2020 return.

Next run:
- `25`: `A-default-off`, `2021-01-01` to `2022-12-31`.

## Step 18: Phase 1 Run 25 Received

Date:
- 2026-05-16

Uploaded files:
- `25.json`
- `25_logs.txt`
- `25_orders.csv`

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/25__2021-01-01_to_2022-12-31__AegisGrowthAllocation__A-default-off__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/25__2021-01-01_to_2022-12-31__AegisGrowthAllocation__A-default-off__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/25__2021-01-01_to_2022-12-31__AegisGrowthAllocation__A-default-off__orders.csv`

Validation:
- Status: `Completed`
- Runtime error: `False`
- Start: `2021-01-01T00:00:00Z`
- End: `2022-12-31T23:59:59Z`
- Parameters matched `A-default-off`.

Metrics:
- Net Profit: `14.186%`
- Compounding Annual Return: `6.871%`
- Drawdown: `16.500%`
- Sharpe Ratio: `0.454`
- Sortino Ratio: `0.555`
- Total Orders: `303`
- Total Fees: `$303.69`
- End Equity: `34255.83`
- Portfolio Turnover: `3.25%`
- Win Rate: `61%`
- Profit-Loss Ratio: `1.00`

Initial comparison:
- Prior pre-weak run `10` remains clearly better for 2021-22: `18.003%` net, `13.800%` drawdown, and `0.609` Sharpe.
- Prior weak-stress run `5` is very close to default return but has lower drawdown and fewer orders.
- Prior severe/stateful severe runs `15` and `20` both lag default in this window.

Completed `A-default-off` baseline summary:

| Run | Window | Net Profit | Drawdown | Sharpe | Orders | Fees |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| `21` | 2008 crash | -16.860% | 20.300% | -1.460 | 144 | $350.54 |
| `22` | 2009 rebound | 17.084% | 4.300% | 1.633 | 89 | $176.25 |
| `23` | 2010 chop | 9.326% | 10.800% | 0.639 | 185 | $419.39 |
| `24` | 2020 Covid | 45.966% | 13.500% | 1.440 | 198 | $214.87 |
| `25` | 2021-22 bear | 14.186% | 16.500% | 0.454 | 303 | $303.69 |

Aggregate `A-default-off` baseline:
- Average Net Profit: `13.940%`
- Average Drawdown: `13.080%`
- Max Drawdown: `20.300%`
- Average Sharpe: `0.541`
- Total Orders: `919`
- Total Fees: `$1464.74`

Next run:
- `26`: `B-weak-stress-only`, `2007-10-01` to `2008-12-31`.

## Step 19: Phase 1 Run 26 Received

Date:
- 2026-05-16

Uploaded files:
- `26.json`
- `26_logs.txt`
- `26_orders.csv`

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/26__2007-10-01_to_2008-12-31__AegisGrowthAllocation__B-weak-stress-only__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/26__2007-10-01_to_2008-12-31__AegisGrowthAllocation__B-weak-stress-only__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/26__2007-10-01_to_2008-12-31__AegisGrowthAllocation__B-weak-stress-only__orders.csv`

Validation:
- Status: `Completed`
- Runtime error: `False`
- Start: `2007-10-01T00:00:00Z`
- End: `2008-12-31T23:59:59Z`
- Parameters matched `B-weak-stress-only`.

Metrics:
- Net Profit: `-11.732%`
- Compounding Annual Return: `-9.472%`
- Drawdown: `15.400%`
- Sharpe Ratio: `-1.316`
- Sortino Ratio: `-1.174`
- Total Orders: `119`
- Total Fees: `$266.69`
- End Equity: `26480.50`
- Portfolio Turnover: `2.12%`
- Win Rate: `40%`
- Profit-Loss Ratio: `0.42`

Diagnostics:
- Diagnostic weeks: `66`
- Weak-stress active weeks: `42`
- First active date: `2008-01-28`
- Last active date: `2008-12-29`
- Active target: `G0.0000/D0.2000/C0.8000`

Initial comparison:
- Run `26` reproduces prior weak-stress run `1` almost exactly.
- Versus default run `21`, weak-stress improves 2008 materially:
  - Net Profit: `-11.732%` versus `-16.860%`
  - Drawdown: `15.400%` versus `20.300%`
  - Orders: `119` versus `144`
  - Fees: `$266.69` versus `$350.54`
- Prior severe run `11` remains slightly better on 2008 return/drawdown, but with more orders and fees.

Next run:
- `27`: `B-weak-stress-only`, `2009-01-01` to `2009-12-31`.

## Step 20: Phase 1 Run 27 Received

Date:
- 2026-05-16

Uploaded files:
- `27.json`
- `27_logs.txt`
- `27_orders.csv`

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/27__2009-01-01_to_2009-12-31__AegisGrowthAllocation__B-weak-stress-only__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/27__2009-01-01_to_2009-12-31__AegisGrowthAllocation__B-weak-stress-only__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/27__2009-01-01_to_2009-12-31__AegisGrowthAllocation__B-weak-stress-only__orders.csv`

Validation:
- Status: `Completed`
- Runtime error: `False`
- Start: `2009-01-01T00:00:00Z`
- End: `2009-12-31T23:59:59Z`
- Parameters matched `B-weak-stress-only`.

Metrics:
- Net Profit: `13.554%`
- Compounding Annual Return: `13.566%`
- Drawdown: `2.800%`
- Sharpe Ratio: `1.451`
- Sortino Ratio: `1.233`
- Total Orders: `75`
- Total Fees: `$128.33`
- End Equity: `34066.09`
- Portfolio Turnover: `1.56%`
- Win Rate: `59%`
- Profit-Loss Ratio: `1.49`

Diagnostics:
- Diagnostic weeks: `52`
- Weak-stress active weeks: `27`
- First active date: `2009-01-05`
- Active target: `G0.0000/D0.2000/C0.8000`

Initial comparison:
- Run `27` reproduces prior weak-stress run `2` almost exactly.
- Versus default run `22`, weak-stress cuts drawdown from `4.300%` to `2.800%` and lowers orders/fees, but gives up about `3.53` net-profit points.
- This confirms the weak-stress overlay is defensive but meaningfully reduces rebound participation in 2009.

Next run:
- `28`: `B-weak-stress-only`, `2010-01-01` to `2010-12-31`.

## Step 21: Phase 1 Run 28 Received

Date:
- 2026-05-16

Uploaded files:
- `28.json`
- `28_logs.txt`
- `28_orders.csv`

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/28__2010-01-01_to_2010-12-31__AegisGrowthAllocation__B-weak-stress-only__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/28__2010-01-01_to_2010-12-31__AegisGrowthAllocation__B-weak-stress-only__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/28__2010-01-01_to_2010-12-31__AegisGrowthAllocation__B-weak-stress-only__orders.csv`

Validation:
- Status: `Completed`
- Runtime error: `False`
- Start: `2010-01-01T00:00:00Z`
- End: `2010-12-31T23:59:59Z`
- Parameters matched `B-weak-stress-only`.

Metrics:
- Net Profit: `8.084%`
- Compounding Annual Return: `8.091%`
- Drawdown: `10.700%`
- Sharpe Ratio: `0.587`
- Sortino Ratio: `0.635`
- Total Orders: `179`
- Total Fees: `$390.42`
- End Equity: `32425.18`
- Portfolio Turnover: `3.69%`
- Win Rate: `48%`
- Profit-Loss Ratio: `1.24`

Diagnostics:
- Diagnostic weeks: `52`
- Weak-stress active weeks: `9`
- First active date: `2010-05-10`
- Last active date: `2010-07-12`
- Active target: `G0.0000/D0.2000/C0.8000`

Initial comparison:
- Run `28` reproduces prior weak-stress run `3` almost exactly.
- Versus default run `23`, weak-stress slightly reduces drawdown and trading cost but lowers net profit and Sharpe.
- Prior pre-weak run `8` remains clearly better in 2010 on net profit, drawdown, and Sharpe.

Next run:
- `29`: `B-weak-stress-only`, `2019-07-01` to `2020-12-31`.

## Step 22: Phase 1 Run 29 Received

Date:
- 2026-05-16

Uploaded files:
- `29.json`
- `29_logs.txt`
- `29_orders.csv`

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/29__2019-07-01_to_2020-12-31__AegisGrowthAllocation__B-weak-stress-only__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/29__2019-07-01_to_2020-12-31__AegisGrowthAllocation__B-weak-stress-only__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/29__2019-07-01_to_2020-12-31__AegisGrowthAllocation__B-weak-stress-only__orders.csv`

Validation:
- Status: `Completed`
- Runtime error: `False`
- Start: `2019-07-01T00:00:00Z`
- End: `2020-12-31T23:59:59Z`
- Parameters matched `B-weak-stress-only`.

Metrics:
- Net Profit: `37.705%`
- Compounding Annual Return: `23.668%`
- Drawdown: `10.900%`
- Sharpe Ratio: `1.316`
- Sortino Ratio: `1.215`
- Total Orders: `185`
- Total Fees: `$201.48`
- End Equity: `41311.40`
- Portfolio Turnover: `2.52%`
- Win Rate: `66%`
- Profit-Loss Ratio: `1.89`

Diagnostics:
- Diagnostic weeks: `79`
- Weak-stress active weeks: `19`
- First active date: `2020-03-02`
- Last active date: `2020-11-09`
- Active target: `G0.0000/D0.2000/C0.8000`

Initial comparison:
- Run `29` reproduces prior weak-stress run `4` almost exactly.
- Versus default run `24`, weak-stress lowers drawdown from `13.500%` to `10.900%`, reduces orders and fees, but gives up about `8.26` net-profit points.
- Prior pre-weak run `9` remains the best 2020 return/Sharpe result, while weak-stress remains best drawdown.

Next run:
- `30`: `B-weak-stress-only`, `2021-01-01` to `2022-12-31`.

## Step 23: Phase 1 Run 30 Received

Date:
- 2026-05-16

Uploaded files:
- `30.json`
- `30_logs.txt`
- `30_orders.csv`

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/30__2021-01-01_to_2022-12-31__AegisGrowthAllocation__B-weak-stress-only__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/30__2021-01-01_to_2022-12-31__AegisGrowthAllocation__B-weak-stress-only__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/30__2021-01-01_to_2022-12-31__AegisGrowthAllocation__B-weak-stress-only__orders.csv`

Validation:
- Status: `Completed`
- Runtime error: `False`
- Start: `2021-01-01T00:00:00Z`
- End: `2022-12-31T23:59:59Z`
- Parameters matched `B-weak-stress-only`.

Metrics:
- Net Profit: `14.189%`
- Compounding Annual Return: `6.872%`
- Drawdown: `15.900%`
- Sharpe Ratio: `0.474`
- Sortino Ratio: `0.531`
- Total Orders: `290`
- Total Fees: `$290.48`
- End Equity: `34256.75`
- Portfolio Turnover: `2.84%`
- Win Rate: `60%`
- Profit-Loss Ratio: `1.13`

Diagnostics:
- Diagnostic weeks: `83`
- Weak-stress active weeks: `19`
- First active date: `2022-01-31`
- Last active date: `2022-08-01`
- Active target: `G0.0000/D0.2000/C0.8000`

Initial comparison:
- Run `30` reproduces prior weak-stress run `5` almost exactly.
- Versus default run `25`, weak-stress gives almost the same return, improves drawdown from `16.500%` to `15.900%`, improves Sharpe from `0.454` to `0.474`, and reduces orders/fees.
- Prior pre-weak run `10` remains much better for 2021-22.
- Prior stateful severe run `20` remains worse than weak-stress and default.

Completed `B-weak-stress-only` summary:

| Run | Window | Net Profit | Drawdown | Sharpe | Orders | Fees |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| `26` | 2008 crash | -11.732% | 15.400% | -1.316 | 119 | $266.69 |
| `27` | 2009 rebound | 13.554% | 2.800% | 1.451 | 75 | $128.33 |
| `28` | 2010 chop | 8.084% | 10.700% | 0.587 | 179 | $390.42 |
| `29` | 2020 Covid | 37.705% | 10.900% | 1.316 | 185 | $201.48 |
| `30` | 2021-22 bear | 14.189% | 15.900% | 0.474 | 290 | $290.48 |

Aggregate `B-weak-stress-only`:
- Average Net Profit: `12.360%`
- Average Drawdown: `11.140%`
- Max Drawdown: `15.900%`
- Average Sharpe: `0.502`
- Total Orders: `848`
- Total Fees: `$1277.40`

Early Phase 1 read after `A` and `B`:
- Weak-stress improves drawdown and cost versus default across the crisis set.
- Weak-stress gives up too much rebound/2020 return to be the lead return-preserving candidate.
- Its role is more likely a drawdown-control reference than a final standalone winner.

Next run:
- `31`: `C-pre-weak-only`, `2007-10-01` to `2008-12-31`.

## Step 24: Phase 1 Run 31 Received

Date:
- 2026-05-17

Uploaded files:
- `31.json`
- `31_logs.txt`
- `31_orders.csv`

Planned normalization:
- `31__2007-10-01_to_2008-12-31__AegisGrowthAllocation__C-pre-weak-only__overview.json`
- `31__2007-10-01_to_2008-12-31__AegisGrowthAllocation__C-pre-weak-only__logs.txt`
- `31__2007-10-01_to_2008-12-31__AegisGrowthAllocation__C-pre-weak-only__orders.csv`

Analysis status:
- Complete.

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/31__2007-10-01_to_2008-12-31__AegisGrowthAllocation__C-pre-weak-only__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/31__2007-10-01_to_2008-12-31__AegisGrowthAllocation__C-pre-weak-only__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/31__2007-10-01_to_2008-12-31__AegisGrowthAllocation__C-pre-weak-only__orders.csv`

Validation:
- Status: completed overview artifact parsed successfully.
- Runtime error: none found in overview status fields.
- Start: `2007-10-01T00:00:00Z`
- End: `2008-12-31T23:59:59Z`
- Parameters matched `C-pre-weak-only`.

Parameters:
- `backtest-start=2007-10-01`
- `backtest-end=2008-12-31`
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=true`
- `severe-crash-override-enabled=false`

Metrics:
- Net Profit: `-16.860%`
- Compounding Annual Return: `-13.691%`
- Drawdown: `20.300%`
- Sharpe Ratio: `-1.46`
- Sortino Ratio: `-1.423`
- Total Orders: `144`
- Total Fees: `$350.54`
- End Equity: `24942.03`
- Portfolio Turnover: `2.96%`
- Win Rate: `45%`
- Profit-Loss Ratio: `0.42`

Diagnostics:
- Diagnostic weeks: `66`
- Pre-weak guard active weeks: `9`
- First pre-weak active date: `2008-01-14`
- Last pre-weak active date: `2008-08-25`
- Severe-stress weeks: `15`
- First severe-stress date: `2008-09-22`
- Last severe-stress date: `2008-12-29`
- All-cash target weeks: `13`
- First all-cash target date: `2008-02-11`
- Last all-cash target date: `2008-12-29`

Comparison:
- Run `31` is statistically identical to run `21` (`A-default-off`) for the same 2008 window.
- Run `31` orders CSV is byte-identical to run `21`, so the trade path did not change.
- Versus run `26` (`B-weak-stress-only`), run `31` is materially worse: net profit `-16.860%` vs `-11.732%`, drawdown `20.300%` vs `15.400%`, Sharpe `-1.46` vs `-1.316`, orders `144` vs `119`, fees `$350.54` vs `$266.69`.

Interpretation:
- `pre-weak-guard-enabled=true` is being recognized by the algorithm, and the diagnostics mark `PreWeakGuardActive=True` for nine weeks.
- The activation did not produce a different order path because the default 2008 path was already constrained to the same effective target weights during the pre-weak window.
- Example: on `2008-01-14`, default-off and pre-weak-only both targeted `G0.2400/D0.3000/C0.4600`; pre-weak-only merely changed the diagnostic override reason.
- This means `C-pre-weak-only` is not an incremental defensive improvement for the 2008 crash window as currently implemented/configured.

Review:
- No algorithm code was changed during this analysis.
- File normalization followed the established CrisisBackTestLogs naming convention.
- Open risk: pre-weak diagnostics can imply an active override even when the resulting target weights are unchanged; this should be considered when interpreting later C runs.

Next run:
- `32`: `C-pre-weak-only`, `2009-01-01` to `2009-12-31`.

## Step 25: Phase 1 Run 32 Received

Date:
- 2026-05-17

Uploaded files:
- `32.json`
- `32_logs.txt`
- `32_orders.csv`

Planned normalization:
- `32__2009-01-01_to_2009-12-31__AegisGrowthAllocation__C-pre-weak-only__overview.json`
- `32__2009-01-01_to_2009-12-31__AegisGrowthAllocation__C-pre-weak-only__logs.txt`
- `32__2009-01-01_to_2009-12-31__AegisGrowthAllocation__C-pre-weak-only__orders.csv`

Analysis status:
- Complete.

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/32__2009-01-01_to_2009-12-31__AegisGrowthAllocation__C-pre-weak-only__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/32__2009-01-01_to_2009-12-31__AegisGrowthAllocation__C-pre-weak-only__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/32__2009-01-01_to_2009-12-31__AegisGrowthAllocation__C-pre-weak-only__orders.csv`

Validation:
- Status: completed overview artifact parsed successfully.
- Runtime error: none found in overview status fields.
- Start: `2009-01-01T00:00:00Z`
- End: `2009-12-31T23:59:59Z`
- Parameters matched `C-pre-weak-only`.

Parameters:
- `backtest-start=2009-01-01`
- `backtest-end=2009-12-31`
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=true`
- `severe-crash-override-enabled=false`

Metrics:
- Net Profit: `17.084%`
- Compounding Annual Return: `17.101%`
- Drawdown: `4.300%`
- Sharpe Ratio: `1.633`
- Sortino Ratio: `1.722`
- Total Orders: `89`
- Total Fees: `$176.25`
- End Equity: `35125.25`
- Portfolio Turnover: `1.97%`
- Win Rate: `67%`
- Profit-Loss Ratio: `1.40`

Diagnostics:
- Diagnostic weeks: `52`
- Pre-weak guard active weeks: `0`
- Severe-stress weeks: `24`
- First severe-stress date: `2009-01-05`
- Last severe-stress date: `2009-06-22`
- All-cash target weeks: `5`
- First all-cash target date: `2009-01-05`
- Last all-cash target date: `2009-02-02`
- Weak final-target weeks: `27`
- First weak final-target date: `2009-01-05`
- Last weak final-target date: `2009-07-13`

Comparison:
- Run `32` is statistically identical to run `22` (`A-default-off`) for the same 2009 window.
- Run `32` orders CSV is byte-identical to run `22`, so the trade path did not change.
- Versus run `27` (`B-weak-stress-only`), run `32` gives higher return but higher drawdown: net profit `17.084%` vs `13.554%`, drawdown `4.300%` vs `2.800%`, Sharpe `1.633` vs `1.451`, orders `89` vs `75`, fees `$176.25` vs `$128.33`.
- Prior runs `07`, `12`, and `17` are also effectively identical for this 2009 window.

Interpretation:
- `C-pre-weak-only` did not activate in 2009 (`PreWeakGuardActive=True` count was zero).
- For the 2009 rebound, pre-weak-only preserves the default return profile, while weak-stress is more defensive but leaves about `3.53` net-profit points on the table.
- This is directionally acceptable for a guard candidate only if it activates in actual pre-crash weakness; so far, run `31` shows activation without trade-path impact, and run `32` shows no activation.

Review:
- No algorithm code was changed during this analysis.
- File normalization followed the established CrisisBackTestLogs naming convention.
- Open risk remains: `C-pre-weak-only` may be too weak to materially affect allocation where we need earlier defense, while still being harmless in rebound windows.

Next run:
- `33`: `C-pre-weak-only`, `2010-01-01` to `2010-12-31`.

## Step 26: Phase 1 Run 33 Received

Date:
- 2026-05-17

Uploaded files:
- `33.json`
- `33_logs.txt`
- `33_orders.csv`

Planned normalization:
- `33__2010-01-01_to_2010-12-31__AegisGrowthAllocation__C-pre-weak-only__overview.json`
- `33__2010-01-01_to_2010-12-31__AegisGrowthAllocation__C-pre-weak-only__logs.txt`
- `33__2010-01-01_to_2010-12-31__AegisGrowthAllocation__C-pre-weak-only__orders.csv`

Analysis status:
- Complete.

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/33__2010-01-01_to_2010-12-31__AegisGrowthAllocation__C-pre-weak-only__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/33__2010-01-01_to_2010-12-31__AegisGrowthAllocation__C-pre-weak-only__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/33__2010-01-01_to_2010-12-31__AegisGrowthAllocation__C-pre-weak-only__orders.csv`

Validation:
- Status: completed overview artifact parsed successfully.
- Runtime error: none found in overview status fields.
- Start: `2010-01-01T00:00:00Z`
- End: `2010-12-31T23:59:59Z`
- Parameters matched `C-pre-weak-only`.

Parameters:
- `backtest-start=2010-01-01`
- `backtest-end=2010-12-31`
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=true`
- `severe-crash-override-enabled=false`

Metrics:
- Net Profit: `10.729%`
- Compounding Annual Return: `10.739%`
- Drawdown: `10.200%`
- Sharpe Ratio: `0.749`
- Sortino Ratio: `0.870`
- Total Orders: `190`
- Total Fees: `$403.37`
- End Equity: `33218.80`
- Portfolio Turnover: `4.08%`
- Win Rate: `51%`
- Profit-Loss Ratio: `1.24`

Diagnostics:
- Diagnostic weeks: `52`
- Pre-weak guard active weeks: `9`
- First pre-weak active date: `2010-06-21`
- Last pre-weak active date: `2010-09-07`
- Severe-stress weeks: `5`
- First severe-stress date: `2010-05-24`
- Last severe-stress date: `2010-07-06`
- All-cash target weeks: `0`
- Weak final-target weeks: `9`
- First weak final-target date: `2010-05-10`
- Last weak final-target date: `2010-07-12`

Comparison:
- Run `33` is the first `C-pre-weak-only` Phase 1 run with a materially different trade path from default.
- Versus run `23` (`A-default-off`), run `33` improved net profit from `9.326%` to `10.729%`, reduced drawdown from `10.800%` to `10.200%`, improved Sharpe from `0.639` to `0.749`, and improved Sortino from `0.735` to `0.870`.
- Cost tradeoff versus run `23`: orders increased from `185` to `190`, while fees decreased from `$419.39` to `$403.37`.
- Versus run `28` (`B-weak-stress-only`), run `33` is better on return and risk-adjusted return: net profit `10.729%` vs `8.084%`, drawdown `10.200%` vs `10.700%`, Sharpe `0.749` vs `0.587`.
- Prior pre-weak run `08` is effectively reproduced by run `33`, confirming the new matrix run is consistent with earlier evidence.

Interpretation:
- `C-pre-weak-only` appears useful in the 2010 choppy recovery window.
- It activates after the May/June 2010 stress period and improves the outcome without the large return sacrifice seen in weak-stress-only.
- Combined Phase 1 read so far: pre-weak-only is neutral in 2008 and 2009, but beneficial in 2010. It is not solving the 2008 crash-defense problem, but it may be a return-preserving choppy-market guard.

Review:
- No algorithm code was changed during this analysis.
- File normalization followed the established CrisisBackTestLogs naming convention.
- Open risk: only two remaining C runs will tell whether this guard preserves 2020 and 2021-22 return while avoiding weak-stress drag.

Next run:
- `34`: `C-pre-weak-only`, `2019-07-01` to `2020-12-31`.

## Step 27: Phase 1 Run 34 Received

Date:
- 2026-05-17

Uploaded files:
- `34.json`
- `34_logs.txt`
- `34_orders.csv`

Planned normalization:
- `34__2019-07-01_to_2020-12-31__AegisGrowthAllocation__C-pre-weak-only__overview.json`
- `34__2019-07-01_to_2020-12-31__AegisGrowthAllocation__C-pre-weak-only__logs.txt`
- `34__2019-07-01_to_2020-12-31__AegisGrowthAllocation__C-pre-weak-only__orders.csv`

Analysis status:
- Complete.

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/34__2019-07-01_to_2020-12-31__AegisGrowthAllocation__C-pre-weak-only__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/34__2019-07-01_to_2020-12-31__AegisGrowthAllocation__C-pre-weak-only__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/34__2019-07-01_to_2020-12-31__AegisGrowthAllocation__C-pre-weak-only__orders.csv`

Validation:
- Status: completed overview artifact parsed successfully.
- Runtime error: none found in overview status fields.
- Start: `2019-07-01T00:00:00Z`
- End: `2020-12-31T23:59:59Z`
- Parameters matched `C-pre-weak-only`.

Parameters:
- `backtest-start=2019-07-01`
- `backtest-end=2020-12-31`
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=true`
- `severe-crash-override-enabled=false`

Metrics:
- Net Profit: `46.839%`
- Compounding Annual Return: `29.056%`
- Drawdown: `13.500%`
- Sharpe Ratio: `1.490`
- Sortino Ratio: `1.443`
- Total Orders: `204`
- Total Fees: `$220.81`
- End Equity: `44051.61`
- Portfolio Turnover: `3.05%`
- Win Rate: `73%`
- Profit-Loss Ratio: `1.23`

Diagnostics:
- Diagnostic weeks: `79`
- Pre-weak guard active weeks: `2`
- First pre-weak active date: `2020-06-01`
- Last pre-weak active date: `2020-09-14`
- Severe-stress weeks: `18`
- First severe-stress date: `2020-03-02`
- Last severe-stress date: `2020-11-09`
- All-cash target weeks: `1`
- First all-cash target date: `2020-03-23`
- Last all-cash target date: `2020-03-23`
- Weak final-target weeks: `19`
- First weak final-target date: `2020-03-02`
- Last weak final-target date: `2020-11-09`

Comparison:
- Versus run `24` (`A-default-off`), run `34` improved net profit from `45.966%` to `46.839%`, improved Sharpe from `1.440` to `1.490`, and improved Sortino from `1.401` to `1.443`.
- Drawdown was unchanged versus run `24` at `13.500%`.
- Cost tradeoff versus run `24`: orders increased from `198` to `204`, and fees increased from `$214.87` to `$220.81`.
- Versus run `29` (`B-weak-stress-only`), run `34` has much higher return but weaker drawdown protection: net profit `46.839%` vs `37.705%`, drawdown `13.500%` vs `10.900%`, Sharpe `1.490` vs `1.316`.
- Prior pre-weak run `09` is effectively reproduced by run `34`, confirming consistency with earlier evidence.
- Severe-crash variants `14` and `19` underperformed run `34` on return and Sharpe without improving drawdown.

Interpretation:
- `C-pre-weak-only` preserves and slightly improves the 2020 return profile.
- It is not a drawdown reducer for the Covid crash window; the peak drawdown remains the same as default.
- This strengthens the view that pre-weak-only is a return-preserving/choppy-market guard, not the main crash-defense mechanism.
- Weak-stress remains the cleanest drawdown reducer for 2020 but at a large return cost of roughly `9.13` net-profit points versus run `34`.

Review:
- No algorithm code was changed during this analysis.
- File normalization followed the established CrisisBackTestLogs naming convention.
- Open risk: if our objective prioritizes drawdown reduction in sudden crashes, `C-pre-weak-only` is insufficient on its own.

Next run:
- `35`: `C-pre-weak-only`, `2021-01-01` to `2022-12-31`.

## Step 28: Phase 1 Run 35 Received

Date:
- 2026-05-17

Uploaded files:
- `35.json`
- `35_logs.txt`
- `35_orders.csv`

Planned normalization:
- `35__2021-01-01_to_2022-12-31__AegisGrowthAllocation__C-pre-weak-only__overview.json`
- `35__2021-01-01_to_2022-12-31__AegisGrowthAllocation__C-pre-weak-only__logs.txt`
- `35__2021-01-01_to_2022-12-31__AegisGrowthAllocation__C-pre-weak-only__orders.csv`

Analysis status:
- Complete.

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/35__2021-01-01_to_2022-12-31__AegisGrowthAllocation__C-pre-weak-only__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/35__2021-01-01_to_2022-12-31__AegisGrowthAllocation__C-pre-weak-only__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/35__2021-01-01_to_2022-12-31__AegisGrowthAllocation__C-pre-weak-only__orders.csv`

Validation:
- Status: completed overview artifact parsed successfully.
- Runtime error: none found in overview status fields.
- Start: `2021-01-01T00:00:00Z`
- End: `2022-12-31T23:59:59Z`
- Parameters matched `C-pre-weak-only`.

Parameters:
- `backtest-start=2021-01-01`
- `backtest-end=2022-12-31`
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=true`
- `severe-crash-override-enabled=false`

Metrics:
- Net Profit: `18.081%`
- Compounding Annual Return: `8.681%`
- Drawdown: `13.700%`
- Sharpe Ratio: `0.611`
- Sortino Ratio: `0.775`
- Total Orders: `305`
- Total Fees: `$305.76`
- End Equity: `35424.19`
- Portfolio Turnover: `3.17%`
- Win Rate: `62%`
- Profit-Loss Ratio: `1.09`

Diagnostics:
- Diagnostic weeks: `83`
- Pre-weak guard active weeks: `9`
- First pre-weak active date: `2022-01-18`
- Last pre-weak active date: `2022-04-25`
- Severe-stress weeks: `6`
- First severe-stress date: `2022-01-31`
- Last severe-stress date: `2022-06-21`
- All-cash target weeks: `0`
- Weak final-target weeks: `19`
- First weak final-target date: `2022-01-31`
- Last weak final-target date: `2022-08-01`

Comparison:
- Versus run `25` (`A-default-off`), run `35` materially improved net profit from `14.186%` to `18.081%`, reduced drawdown from `16.500%` to `13.700%`, improved Sharpe from `0.454` to `0.611`, and improved Sortino from `0.555` to `0.775`.
- Versus run `30` (`B-weak-stress-only`), run `35` also materially improved return and drawdown: net profit `18.081%` vs `14.189%`, drawdown `13.700%` vs `15.900%`, Sharpe `0.611` vs `0.474`.
- Cost tradeoff versus run `25`: orders increased from `303` to `305`, and fees increased from `$303.69` to `$305.76`.
- Prior pre-weak run `10` is effectively reproduced by run `35`, confirming consistency with earlier evidence.
- Severe-crash variants `15` and `20` were materially worse than run `35` on return, drawdown, Sharpe, and Sortino.

Interpretation:
- `C-pre-weak-only` is clearly beneficial in the 2021-22 bear window.
- It activates early in the 2022 decline and improves both return and drawdown without the return drag observed in weak-stress-only.
- This makes pre-weak-only the strongest Phase 1 candidate so far for return-preserving defense in slow/choppy bear-market deterioration.
- It still does not solve the sudden-crash 2008 drawdown problem.

Completed `C-pre-weak-only` summary:

| Run | Window | Net Profit | Drawdown | Sharpe | Orders | Fees |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| `31` | 2008 crash | -16.860% | 20.300% | -1.460 | 144 | $350.54 |
| `32` | 2009 rebound | 17.084% | 4.300% | 1.633 | 89 | $176.25 |
| `33` | 2010 chop | 10.729% | 10.200% | 0.749 | 190 | $403.37 |
| `34` | 2020 Covid | 46.839% | 13.500% | 1.490 | 204 | $220.81 |
| `35` | 2021-22 bear | 18.081% | 13.700% | 0.611 | 305 | $305.76 |

Aggregate `C-pre-weak-only`:
- Average Net Profit: `15.175%`
- Average Drawdown: `12.400%`
- Max Drawdown: `20.300%`
- Average Sharpe: `0.605`
- Total Orders: `932`
- Total Fees: `$1456.73`

Aggregate comparison after `A`, `B`, and `C`:
- `A-default-off`: average net `13.940%`, average drawdown `13.080%`, max drawdown `20.300%`, average Sharpe `0.541`, total orders `919`, total fees `$1464.74`.
- `B-weak-stress-only`: average net `12.360%`, average drawdown `11.140%`, max drawdown `15.900%`, average Sharpe `0.502`, total orders `848`, total fees `$1277.40`.
- `C-pre-weak-only`: average net `15.175%`, average drawdown `12.400%`, max drawdown `20.300%`, average Sharpe `0.605`, total orders `932`, total fees `$1456.73`.

Phase 1 read after `C`:
- `C-pre-weak-only` is the best return-preserving candidate across the five windows.
- It improves the aggregate average net return and Sharpe versus both `A` and `B`.
- It does not reduce the worst-case max drawdown because 2008 remains unchanged at `20.300%`.
- `B-weak-stress-only` is still the cleaner max-drawdown reducer, but it sacrifices too much return in 2009 and 2020.
- The next useful experiment should combine pre-weak-only with a more selective crash-defense layer, rather than using weak-stress-only globally.

Review:
- No algorithm code was changed during this analysis.
- File normalization followed the established CrisisBackTestLogs naming convention.
- Open risk: Phase 1 has not yet found a solution for the sudden-crash 2008 drawdown without return drag.

Next run:
- Pause before Phase 2 recommendation, or proceed to the next matrix candidate if already defined in the run sheet.

## Step 29: Commit And Next-Test Recommendation

Date:
- 2026-05-17

Scope to commit:
- Normalized `C-pre-weak-only` Phase 1 result files for runs `31` to `35`.
- Run-sheet analysis for runs `31` to `35`, including aggregate `A`/`B`/`C` comparison.

Pre-commit review:
- No algorithm code was changed.
- The changed files are documentation/evidence artifacts only.
- `git diff --check` completed with only the existing LF-to-CRLF warning for the markdown file.

Next-test recommendation:
- Prefer testing `E-pre-weak-plus-stateful-severe` next, starting with the 2008 crash window.
- Rationale: `C-pre-weak-only` is the best return-preserving candidate but does not reduce 2008 max drawdown; the next useful question is whether adding the stateful severe layer improves sudden-crash protection without destroying the C gains.
- Do not prioritize rerunning `D-stateful-severe-only` unless a fully sequential `36-40` matrix is required, because prior stateful-severe evidence already showed weaker return/risk behavior than `C`.
