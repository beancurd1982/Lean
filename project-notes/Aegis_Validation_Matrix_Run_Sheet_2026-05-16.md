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

## Step 30: Phase 1 Run 41 Received

Date:
- 2026-05-17

Uploaded files:
- `41.json`
- `41_logs.txt`
- `41_orders.csv`

Planned normalization:
- `41__2007-10-01_to_2008-12-31__AegisGrowthAllocation__E-pre-weak-plus-stateful-severe__overview.json`
- `41__2007-10-01_to_2008-12-31__AegisGrowthAllocation__E-pre-weak-plus-stateful-severe__logs.txt`
- `41__2007-10-01_to_2008-12-31__AegisGrowthAllocation__E-pre-weak-plus-stateful-severe__orders.csv`

Analysis status:
- Complete.

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/41__2007-10-01_to_2008-12-31__AegisGrowthAllocation__E-pre-weak-plus-stateful-severe__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/41__2007-10-01_to_2008-12-31__AegisGrowthAllocation__E-pre-weak-plus-stateful-severe__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/41__2007-10-01_to_2008-12-31__AegisGrowthAllocation__E-pre-weak-plus-stateful-severe__orders.csv`

Validation:
- Status: completed overview artifact parsed successfully.
- Runtime error: none found in overview status fields.
- Start: `2007-10-01T00:00:00Z`
- End: `2008-12-31T23:59:59Z`
- Parameters matched `E-pre-weak-plus-stateful-severe`.

Parameters:
- `backtest-start=2007-10-01`
- `backtest-end=2008-12-31`
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=true`
- `severe-crash-override-enabled=true`
- `sev-crash-dd-entry=0.10`
- `sev-crash-dd-exit=0.07`
- `sev-crash-recovery-wks=2`

Metrics:
- Net Profit: `-15.010%`
- Compounding Annual Return: `-12.164%`
- Drawdown: `18.500%`
- Sharpe Ratio: `-1.359`
- Sortino Ratio: `-1.347`
- Total Orders: `146`
- Total Fees: `$336.55`
- End Equity: `25496.90`
- Portfolio Turnover: `2.91%`
- Win Rate: `45%`
- Profit-Loss Ratio: `0.46`

Diagnostics:
- Diagnostic weeks: `66`
- Pre-weak guard active weeks: `9`
- First pre-weak active date: `2008-01-14`
- Last pre-weak active date: `2008-08-25`
- Severe-stress signal weeks: `15`
- First severe-stress signal date: `2008-09-22`
- Last severe-stress signal date: `2008-12-29`
- Severe-crash override active weeks: `15`
- First severe-crash override active date: `2008-09-22`
- Last severe-crash override active date: `2008-12-29`
- Severe crash mode states: `none=51`, `enter=1`, `hold=14`
- Severe crash exit reason count: `0`
- All-cash target weeks: `13`
- First all-cash target date: `2008-02-11`
- Last all-cash target date: `2008-12-29`
- Weak final-target weeks: `27`
- First weak final-target date: `2008-01-28`
- Last weak final-target date: `2008-09-15`

Comparison:
- Versus run `31` (`C-pre-weak-only`), run `41` improved the 2008 result: net profit `-15.010%` vs `-16.860%`, drawdown `18.500%` vs `20.300%`, Sharpe `-1.359` vs `-1.460`, fees `$336.55` vs `$350.54`.
- Versus run `26` (`B-weak-stress-only`), run `41` is still materially worse: net profit `-15.010%` vs `-11.732%`, drawdown `18.500%` vs `15.400%`, Sharpe `-1.359` vs `-1.316`, fees `$336.55` vs `$266.69`.
- Versus run `11` (`severe-crash-override`, older/non-stateful evidence), run `41` is materially worse: net profit `-15.010%` vs `-11.549%`, drawdown `18.500%` vs `15.200%`, Sharpe `-1.359` vs `-1.157`.
- Versus run `16` (`stateful-severe-crash`), run `41` is effectively the same result: net profit `-15.010%`, drawdown `18.500%`, Sharpe `-1.359`, orders `146`, fees `$336.55`.

Interpretation:
- `E-pre-weak-plus-stateful-severe` improves over `C`/default in the 2008 crash window but does not solve the sudden-crash drawdown problem.
- The combined run behaves like the current stateful severe-crash implementation because severe-crash has priority once active.
- The stateful severe target (`G0.05/D0.35/C0.60`) is not defensive enough to match the better 2008 protection seen in weak-stress-only or the older severe-crash run.
- Pre-weak helps before the crash, but once the severe layer takes over, the result remains too exposed during the crash leg.

Review:
- No algorithm code was changed during this analysis.
- File normalization followed the established CrisisBackTestLogs naming convention.
- Open risk: continuing runs `42-45` may consume cloud runs without answering the key 2008 defense gap, because run `41` already shows the current combined severe layer is not strong enough.

Next recommendation:
- Do not continue with `42-45` yet unless a complete matrix is required.
- Prefer a small code/config experiment that makes the severe-crash layer stricter, then rerun only the 2008 window first.

## Step 31: Multi-Agent Review Of Run 41

Date:
- 2026-05-17

Review setup:
- Three read-only sub-agents reviewed the Phase 1 results, run `41`, prior severe-crash references, and relevant code/config paths.
- No sub-agent edited files.

Shared findings:
- All three agents agreed that run `41` improved over `C-pre-weak-only`/default in 2008 but was still too weak to justify continuing `42-45` immediately.
- All three agents identified the same root issue: once severe-crash activates, it has priority over pre-weak, so run `41` behaves like current stateful severe run `16`.
- All three agents identified severe sleeve strictness as the most likely problem, not the hysteresis entry/exit parameters.
- The current severe target is `G0.05/D0.35/C0.60`.
- Older severe evidence used a stricter effective severe target around `G0.00/D0.20/C0.80` and produced a materially better 2008 result.

Agent consensus:
- Pause current `E-pre-weak-plus-stateful-severe` cloud runs `42-45`.
- Do not spend more cloud runs on rebound/Covid/2021-22 windows until the severe layer first proves it can improve the 2008 crash window.
- Make a small severe-crash target experiment and rerun only the 2008 window first.

Final proposal from review:
- Change `SevereCrashOverrideSleeveTargets` from `G0.05/D0.35/C0.60` to `G0.00/D0.20/C0.80`.
- Use bands matching the existing weak-stress overlay:
  - growth min/max: `0.00` / `0.05`
  - defensive min/max: `0.00` / `0.25`
  - cash min/max: `0.75` / `1.00`
- Keep the current hysteresis parameters unchanged for the first test:
  - `sev-crash-dd-entry=0.10`
  - `sev-crash-dd-exit=0.07`
  - `sev-crash-recovery-wks=2`

First validation run after the change:
- Run only `2007-10-01` to `2008-12-31`.
- Use `pre-weak-guard-enabled=true`.
- Use `severe-crash-override-enabled=true`.
- Success target: approach or beat run `26`/run `11` on 2008 drawdown and net loss while remaining better than run `41`.

Risks:
- The stricter severe layer may recreate weak-stress cash drag in later recovery windows.
- If the 2008 rerun passes, test only `2020` and `2021-22` next before expanding to the full matrix.

## Step 32: Stricter Severe-Crash Sleeve Implementation

Date:
- 2026-05-17

Approved change:
- Tighten `SevereCrashOverrideSleeveTargets` from `G0.05/D0.35/C0.60` to `G0.00/D0.20/C0.80`.
- Match the existing weak-stress overlay bands:
  - growth min/max: `0.00` / `0.05`
  - defensive min/max: `0.00` / `0.25`
  - cash min/max: `0.75` / `1.00`

Scope:
- Code: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- Tests: `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- No changes to severe-crash entry/exit/recovery parameters.

TDD plan:
- Update existing severe-crash sleeve tests to expect the stricter target.
- Run the targeted tests before production-code change to confirm they fail.
- Apply the minimal `StrategyConfig.cs` change.
- Rerun targeted tests and project build.

Implementation result:
- Updated `StrategyConfig.SevereCrashOverrideSleeveTargets` to `G0.00/D0.20/C0.80`.
- Updated `PortfolioManagerAppliesSevereCrashOverrideSleeves` to expect no growth allocation, `20%` defensive allocation, and `80%` cash.
- Updated override attribution diagnostic expectation to `FinalTarget=G0.0000/D0.2000/C0.8000`.

Verification:
- Pre-change RED attempt: `dotnet test Tests/QuantConnect.Tests.csproj --filter AegisGrowthAllocationTests` could not reach test execution because restore attempted NuGet access and failed/timed out in the sandboxed environment.
- Build: `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo --no-restore` passed with warnings only.
- Targeted tests: `dotnet test Tests/QuantConnect.Tests.csproj --filter AegisGrowthAllocationTests --no-restore --no-build` passed: `43` passed, `0` failed.
- Diff check: `git diff --check` passed with line-ending warnings only.

Strict code review:
- Scope is minimal and limited to the severe-crash sleeve target/bands plus matching tests and notes.
- No entry/exit/recovery behavior changed.
- No live-state persistence or order lifecycle logic changed.
- Main behavioral risk: this will move severe-crash mode much closer to weak-stress cash exposure, so later recovery windows may show cash drag.
- Mitigation: first rerun only the 2008 window; only test `2020` and `2021-22` if the 2008 result materially improves versus run `41`.

## Step 33: Run 46 Strict Severe 2008 Received

Date:
- 2026-05-17

Uploaded files:
- `46.json`
- `46_logs.txt`
- `46_orders.csv`

Planned normalization:
- `46__2007-10-01_to_2008-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__overview.json`
- `46__2007-10-01_to_2008-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__logs.txt`
- `46__2007-10-01_to_2008-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__orders.csv`

Analysis status:
- Complete.

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/46__2007-10-01_to_2008-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/46__2007-10-01_to_2008-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/46__2007-10-01_to_2008-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__orders.csv`

Validation:
- Status: completed overview artifact parsed successfully.
- Runtime error: none found in overview status fields.
- Start: `2007-10-01T00:00:00Z`
- End: `2008-12-31T23:59:59Z`
- Parameters matched strict severe plus pre-weak.
- Severe-crash final target confirmed as `G0.0000/D0.2000/C0.8000`.

Parameters:
- `backtest-start=2007-10-01`
- `backtest-end=2008-12-31`
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=true`
- `severe-crash-override-enabled=true`
- `sev-crash-dd-entry=0.10`
- `sev-crash-dd-exit=0.07`
- `sev-crash-recovery-wks=2`

Metrics:
- Net Profit: `-11.548%`
- Compounding Annual Return: `-9.322%`
- Drawdown: `15.200%`
- Sharpe Ratio: `-1.157`
- Sortino Ratio: `-1.163`
- Total Orders: `142`
- Total Fees: `$320.40`
- End Equity: `26535.55`
- Portfolio Turnover: `2.85%`
- Win Rate: `47%`
- Profit-Loss Ratio: `0.50`

Diagnostics:
- Diagnostic weeks: `66`
- Pre-weak guard active weeks: `9`
- First pre-weak active date: `2008-01-14`
- Last pre-weak active date: `2008-08-25`
- Severe-stress signal weeks: `15`
- First severe-stress signal date: `2008-09-22`
- Last severe-stress signal date: `2008-12-29`
- Severe-crash override active weeks: `15`
- First severe-crash override active date: `2008-09-22`
- Last severe-crash override active date: `2008-12-29`
- Strict severe final-target weeks: `15`
- First strict severe final-target date: `2008-09-22`
- Last strict severe final-target date: `2008-12-29`
- Severe crash mode states: `none=51`, `enter=1`, `hold=14`
- Severe crash exit reason count: `0`
- All-cash target weeks: `13`
- First all-cash target date: `2008-02-11`
- Last all-cash target date: `2008-12-29`
- Weak final-target weeks: `27`
- First weak final-target date: `2008-01-28`
- Last weak final-target date: `2008-09-15`

Comparison:
- Versus run `41` (`E-pre-weak-plus-stateful-severe` before strict sleeve), run `46` is materially better: net profit `-11.548%` vs `-15.010%`, drawdown `15.200%` vs `18.500%`, Sharpe `-1.157` vs `-1.359`, fees `$320.40` vs `$336.55`.
- Versus run `26` (`B-weak-stress-only`), run `46` is slightly better on net profit, drawdown, Sharpe, and Sortino, but has more orders/fees: net profit `-11.548%` vs `-11.732%`, drawdown `15.200%` vs `15.400%`, Sharpe `-1.157` vs `-1.316`, fees `$320.40` vs `$266.69`.
- Versus run `31` (`C-pre-weak-only`) and run `21` (`A-default-off`), run `46` fixes the major 2008 gap: drawdown improves from `20.300%` to `15.200%`, and net profit improves from `-16.860%` to `-11.548%`.
- Versus run `11` (`severe-crash-override`, older/non-stateful evidence), run `46` effectively reproduces the same headline result while using the current stateful severe mode.

Interpretation:
- The strict severe sleeve change succeeded in the target window.
- The problem with run `41` was severe-crash target strictness, not entry/exit hysteresis.
- Run `46` now meets the first validation goal: it approaches/beats run `26` and run `11` on 2008 drawdown and net loss while remaining materially better than run `41`.
- The remaining open question is whether this stricter severe mode causes too much cash drag in later windows.

Review:
- No additional code changes were made during this analysis beyond the already-recorded strict sleeve implementation.
- File normalization followed the established CrisisBackTestLogs naming convention.
- Open risk: strict severe may now over-protect in fast-recovery or slower bear windows.

Next recommendation:
- Do not run the full matrix yet.
- Run `2020 Covid` next with the same strict severe plus pre-weak settings because it is the most likely window to reveal crash-recovery cash drag.
- If 2020 is acceptable, run `2021-22` next.

## Step 34: Run 47 Strict Severe 2020 Received

Date:
- 2026-05-17

Uploaded files:
- `47.json`
- `47_logs.txt`
- `47_orders.csv`

Planned normalization:
- `47__2019-07-01_to_2020-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__overview.json`
- `47__2019-07-01_to_2020-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__logs.txt`
- `47__2019-07-01_to_2020-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__orders.csv`

Analysis status:
- Complete.

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/47__2019-07-01_to_2020-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/47__2019-07-01_to_2020-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/47__2019-07-01_to_2020-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__orders.csv`

Validation:
- Status: completed overview artifact parsed successfully.
- Runtime error: none found in overview status fields.
- Start: `2019-07-01T00:00:00Z`
- End: `2020-12-31T23:59:59Z`
- Parameters matched strict severe plus pre-weak.
- Severe-crash final target confirmed as `G0.0000/D0.2000/C0.8000`.

Parameters:
- `backtest-start=2019-07-01`
- `backtest-end=2020-12-31`
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=true`
- `severe-crash-override-enabled=true`
- `sev-crash-dd-entry=0.10`
- `sev-crash-dd-exit=0.07`
- `sev-crash-recovery-wks=2`

Metrics:
- Net Profit: `42.499%`
- Compounding Annual Return: `26.511%`
- Drawdown: `13.500%`
- Sharpe Ratio: `1.413`
- Sortino Ratio: `1.343`
- Total Orders: `203`
- Total Fees: `$216.08`
- End Equity: `42749.82`
- Portfolio Turnover: `2.85%`
- Win Rate: `72%`
- Profit-Loss Ratio: `1.18`

Diagnostics:
- Diagnostic weeks: `79`
- Pre-weak guard active weeks: `3`
- First pre-weak active date: `2020-07-13`
- Last pre-weak active date: `2020-09-14`
- Severe-stress signal weeks: `18`
- First severe-stress signal date: `2020-03-02`
- Last severe-stress signal date: `2020-11-09`
- Severe-crash override active weeks: `11`
- First severe-crash override active date: `2020-03-23`
- Last severe-crash override active date: `2020-06-01`
- Strict severe final-target weeks: `11`
- First strict severe final-target date: `2020-03-23`
- Last strict severe final-target date: `2020-06-01`
- Severe crash mode states: `none=67`, `enter=1`, `hold=10`, `exit=1`
- Severe crash exit reasons: `regime-recovered=1`
- All-cash target weeks: `3`
- First all-cash target date: `2020-03-23`
- Last all-cash target date: `2020-04-06`
- Weak final-target weeks: `9`
- First weak final-target date: `2020-03-02`
- Last weak final-target date: `2020-11-09`

Comparison:
- Versus run `34` (`C-pre-weak-only`), run `47` is worse on return and risk-adjusted return with no drawdown improvement: net profit `42.499%` vs `46.839%`, drawdown `13.500%` vs `13.500%`, Sharpe `1.413` vs `1.490`, Sortino `1.343` vs `1.443`.
- Versus run `24` (`A-default-off`), run `47` also gives up return with no drawdown improvement: net profit `42.499%` vs `45.966%`, drawdown unchanged at `13.500%`.
- Versus run `29` (`B-weak-stress-only`), run `47` has higher return but worse drawdown: net profit `42.499%` vs `37.705%`, drawdown `13.500%` vs `10.900%`.
- Versus prior severe variants, run `47` is better than run `14` but worse than run `19`: net profit `42.499%` vs `41.980%` and `43.911%`; Sharpe `1.413` vs `1.337` and `1.392`.

Interpretation:
- Run `47` confirms the main risk from strict severe: cash drag during fast crash/recovery.
- The strict severe layer was active from `2020-03-23` through `2020-06-01`, which likely reduced recovery participation.
- It did not reduce the headline 2020 drawdown versus `C` or default, so the 2020 tradeoff is unfavorable compared with `C-pre-weak-only`.
- However, it still preserves more return than weak-stress-only and has better Sharpe than weak-stress-only.

Review:
- No additional code changes were made during this analysis.
- File normalization followed the established CrisisBackTestLogs naming convention.
- Open risk: strict severe is useful for 2008, but too blunt for 2020 unless activation is made more selective or exit faster.

Next recommendation:
- Run `2021-22` next with the same strict severe plus pre-weak settings before changing code again.
- Reason: we need to know whether strict severe hurts the slower bear-market window where `C-pre-weak-only` was strongest.
- If `2021-22` also gives up too much return versus run `35`, then the next design should make strict severe activation more selective rather than globally changing the severe target.

## Step 35: Run 48 Strict Severe 2021-22 Received

Date:
- 2026-05-17

Uploaded files:
- `48.json`
- `48_logs.txt`
- `48_orders.csv`

Planned normalization:
- `48__2021-01-01_to_2022-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__overview.json`
- `48__2021-01-01_to_2022-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__logs.txt`
- `48__2021-01-01_to_2022-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__orders.csv`

Analysis status:
- Complete.

Normalized files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/48__2021-01-01_to_2022-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__overview.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/48__2021-01-01_to_2022-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/48__2021-01-01_to_2022-12-31__AegisGrowthAllocation__F-strict-severe-plus-pre-weak__orders.csv`

Validation:
- Status: completed overview artifact parsed successfully.
- Runtime error: none found in overview status fields.
- Start: `2021-01-01T00:00:00Z`
- End: `2022-12-31T23:59:59Z`
- Parameters matched strict severe plus pre-weak.
- Severe-crash final target confirmed as `G0.0000/D0.2000/C0.8000`.

Parameters:
- `backtest-start=2021-01-01`
- `backtest-end=2022-12-31`
- `crisis-diagnostics=true`
- `weak-stress-overlay-enabled=false`
- `pre-weak-guard-enabled=true`
- `severe-crash-override-enabled=true`
- `sev-crash-dd-entry=0.10`
- `sev-crash-dd-exit=0.07`
- `sev-crash-recovery-wks=2`

Metrics:
- Net Profit: `15.719%`
- Compounding Annual Return: `7.587%`
- Drawdown: `14.800%`
- Sharpe Ratio: `0.538`
- Sortino Ratio: `0.649`
- Total Orders: `307`
- Total Fees: `$307.75`
- End Equity: `34715.55`
- Portfolio Turnover: `2.98%`
- Win Rate: `60%`
- Profit-Loss Ratio: `1.17`

Diagnostics:
- Diagnostic weeks: `82`
- Pre-weak guard active weeks: `9`
- First pre-weak active date: `2022-01-18`
- Last pre-weak active date: `2022-04-25`
- Severe-stress signal weeks: `6`
- First severe-stress signal date: `2022-01-31`
- Last severe-stress signal date: `2022-06-21`
- Severe-crash override active weeks: `11`
- First severe-crash override active date: `2022-05-16`
- Last severe-crash override active date: `2022-07-25`
- Strict severe final-target weeks: `11`
- First strict severe final-target date: `2022-05-16`
- Last strict severe final-target date: `2022-07-25`
- Severe crash mode states: `none=71`, `enter=1`, `hold=10`
- Severe crash exit reason count: `0`
- All-cash target weeks: `0`
- Weak final-target weeks: `7`
- First weak final-target date: `2022-01-31`
- Last weak final-target date: `2022-05-09`

Comparison:
- Versus run `35` (`C-pre-weak-only`), run `48` is worse on return, drawdown, Sharpe, and Sortino: net profit `15.719%` vs `18.081%`, drawdown `14.800%` vs `13.700%`, Sharpe `0.538` vs `0.611`, Sortino `0.649` vs `0.775`.
- Versus run `25` (`A-default-off`), run `48` is better: net profit `15.719%` vs `14.186%`, drawdown `14.800%` vs `16.500%`, Sharpe `0.538` vs `0.454`.
- Versus run `30` (`B-weak-stress-only`), run `48` is better on return, drawdown, Sharpe, and Sortino: net profit `15.719%` vs `14.189%`, drawdown `14.800%` vs `15.900%`, Sharpe `0.538` vs `0.474`.
- Versus prior severe variants, run `48` is materially better than runs `15` and `20`, but still meaningfully worse than `C-pre-weak-only`.

Interpretation:
- Run `48` confirms the same pattern as run `47`: strict severe helps versus default/weak-stress/severe variants but gives up too much versus `C-pre-weak-only`.
- In 2021-22, strict severe activates late (`2022-05-16`) and remains active through `2022-07-25`, after pre-weak had already done useful risk reduction.
- This suggests the strict severe layer is good for 2008-style crisis protection but too blunt as a general severe mode.

Partial `F-strict-severe-plus-pre-weak` read:
- Run `46` fixes 2008: net `-11.548%`, drawdown `15.200%`.
- Run `47` hurts 2020 versus `C`: net `42.499%` vs `46.839%`, drawdown unchanged.
- Run `48` hurts 2021-22 versus `C`: net `15.719%` vs `18.081%`, drawdown worse.
- Across tested windows `46-48`, strict severe average net is `15.557%`, average drawdown is `14.500%`, max drawdown is `15.200%`, and average Sharpe is `0.265`.

Review:
- No additional code changes were made during this analysis.
- File normalization followed the established CrisisBackTestLogs naming convention.
- Open risk: the current strict severe target solves one crash window but degrades the best return-preserving behavior in later crisis windows.

Next recommendation:
- Stop running more `F` windows for now.
- The evidence favors a selective strict-severe trigger rather than a globally stricter severe target.
- Candidate next design: keep `C-pre-weak-only` behavior as the primary mode, and allow strict severe only in true systemic crash conditions similar to 2008, for example when weak regime + severe stress + at least `10%` drawdown + breadth near zero or VIX well above the current threshold.

## Step 36: Defensive Optimization History Check

Scope:
- Check whether defensive optimization started at result `20` or before it.
- Sources reviewed: git history, project notes, and normalized `CrisisBackTestLogs` filenames.

Finding:
- Defensive optimization started before result `20`.
- Result `20` belongs to the `stateful-severe-crash` batch (`16-20`), which was already the fourth defensive experiment batch after weak-stress, pre-weak, and severe-crash override.
- The first actual defensive optimization code experiment was commit `25a99698c` (`feat: add Aegis weak stress overlay`), which produced/used result batch `1-5`.
- The broader defensive work started even earlier with crisis diagnostics in commit `6b14431fe` (`feat: add Aegis crisis diagnostics`) and the agent debate documented in `Aegis_Defensive_Optimization_Agent_Debate_2026-05-09.md`.

Timeline:
- `6b14431fe`: added `crisis-diagnostics` to understand crisis-window behavior before changing defensive logic.
- `25a99698c`: added `weak-stress-overlay-enabled`; this is the first defensive behavior optimization experiment and maps to result files `1-5`.
- `df813174b`: added `pre-weak-guard-enabled`; maps to result files `6-10`.
- `b6377f5a0`: added `severe-crash-override-enabled`; maps to result files `11-15`.
- `c62980486`: added stateful severe-crash mode; maps to result files `16-20`.
- `77a83a1d0` and later: created the validation matrix and reran cleaner comparison batches `21-35`, then follow-up `41`, `46-48`.

Review:
- No code changes were made for this history check.
- The historical interpretation is consistent across commit messages, implementation notes, and normalized result-file names.
- Open risk: result numbering can be misleading because later validation batches (`21-35`) intentionally reran baseline/variant comparisons after several defensive features already existed.

## Step 37: Proposed All-Defensive-Off Comparison Round

Scope:
- Evaluate whether another baseline comparison round should be run with all defensive optimization switches disabled.
- This is intended to compare the current best defensive variants against the original/default behavior on the same crisis windows.

Finding:
- The defensive behavior changes are gated by parameters:
  - `weak-stress-overlay-enabled`
  - `pre-weak-guard-enabled`
  - `severe-crash-override-enabled`
- Setting all three to `false` disables the defensive optimization layers for trading behavior.
- `crisis-diagnostics=true` can remain enabled because it adds logs only and does not change target allocation logic.

Recommended baseline setup:
- Use the same five crisis windows as the validation matrix.
- Set all defensive switches to `false`.
- Label the next result batch as a fresh all-off control round, not as a new defensive variant.

Open question:
- If the goal is "original behavior with current code and all defense gates off", parameter-only is enough.
- If the goal is "exact historical code before defensive optimization commits existed", that requires checking out or branching from an older commit and is a different, heavier comparison.

Review:
- No code changes were made.
- Recommendation is to use the parameter-only comparison first because it is safer, faster, and directly comparable to current cloud code.

## Step 38: Runs 49-53 All-Defensive-Off Upload Intake

Date:
- 2026-05-17

Uploaded files:
- `49.json`, `49_logs.txt`, `49_orders.csv`
- `50.json`, `50_logs.txt`, `50_orders.csv`
- `51.json`, `51_logs.txt`, `51_orders.csv`
- `52.json`, `52_logs.txt`, `52_orders.csv`
- `53.json`, `53_logs.txt`, `53_orders.csv`

Planned normalization:
- Use variant label `G-current-all-defensive-off`.
- Map run windows:
  - `49`: `2007-10-01` to `2008-12-31`
  - `50`: `2009-01-01` to `2009-12-31`
  - `51`: `2010-01-01` to `2010-12-31`
  - `52`: `2019-07-01` to `2020-12-31`
  - `53`: `2021-01-01` to `2022-12-31`

Analysis status:
- Complete.

Normalized files:
- `49__2007-10-01_to_2008-12-31__AegisGrowthAllocation__G-current-all-defensive-off__overview.json`
- `49__2007-10-01_to_2008-12-31__AegisGrowthAllocation__G-current-all-defensive-off__logs.txt`
- `49__2007-10-01_to_2008-12-31__AegisGrowthAllocation__G-current-all-defensive-off__orders.csv`
- `50__2009-01-01_to_2009-12-31__AegisGrowthAllocation__G-current-all-defensive-off__overview.json`
- `50__2009-01-01_to_2009-12-31__AegisGrowthAllocation__G-current-all-defensive-off__logs.txt`
- `50__2009-01-01_to_2009-12-31__AegisGrowthAllocation__G-current-all-defensive-off__orders.csv`
- `51__2010-01-01_to_2010-12-31__AegisGrowthAllocation__G-current-all-defensive-off__overview.json`
- `51__2010-01-01_to_2010-12-31__AegisGrowthAllocation__G-current-all-defensive-off__logs.txt`
- `51__2010-01-01_to_2010-12-31__AegisGrowthAllocation__G-current-all-defensive-off__orders.csv`
- `52__2019-07-01_to_2020-12-31__AegisGrowthAllocation__G-current-all-defensive-off__overview.json`
- `52__2019-07-01_to_2020-12-31__AegisGrowthAllocation__G-current-all-defensive-off__logs.txt`
- `52__2019-07-01_to_2020-12-31__AegisGrowthAllocation__G-current-all-defensive-off__orders.csv`
- `53__2021-01-01_to_2022-12-31__AegisGrowthAllocation__G-current-all-defensive-off__overview.json`
- `53__2021-01-01_to_2022-12-31__AegisGrowthAllocation__G-current-all-defensive-off__logs.txt`
- `53__2021-01-01_to_2022-12-31__AegisGrowthAllocation__G-current-all-defensive-off__orders.csv`

Parameter validation:
- All five runs completed without runtime errors.
- All five runs used:
  - `crisis-diagnostics=true`
  - `weak-stress-overlay-enabled=false`
  - `pre-weak-guard-enabled=false`
  - `severe-crash-override-enabled=false`
- Diagnostic logs confirm no defensive override activation:
  - run `49`: `PreWeakGuardActive=True` count `0`, `SevereCrashOverrideActive=True` count `0`, non-none `SleeveOverride` count `0`
  - run `50`: `PreWeakGuardActive=True` count `0`, `SevereCrashOverrideActive=True` count `0`, non-none `SleeveOverride` count `0`
  - run `51`: `PreWeakGuardActive=True` count `0`, `SevereCrashOverrideActive=True` count `0`, non-none `SleeveOverride` count `0`
  - run `52`: `PreWeakGuardActive=True` count `0`, `SevereCrashOverrideActive=True` count `0`, non-none `SleeveOverride` count `0`
  - run `53`: `PreWeakGuardActive=True` count `0`, `SevereCrashOverrideActive=True` count `0`, non-none `SleeveOverride` count `0`

Metrics:
- Run `49` (`2007-10-01` to `2008-12-31`): net `-16.860%`, CAGR `-13.691%`, drawdown `20.300%`, Sharpe `-1.460`, Sortino `-1.423`, orders `144`, fees `$350.54`.
- Run `50` (`2009-01-01` to `2009-12-31`): net `17.084%`, CAGR `17.101%`, drawdown `4.300%`, Sharpe `1.633`, Sortino `1.722`, orders `89`, fees `$176.25`.
- Run `51` (`2010-01-01` to `2010-12-31`): net `9.326%`, CAGR `9.335%`, drawdown `10.800%`, Sharpe `0.639`, Sortino `0.735`, orders `185`, fees `$419.39`.
- Run `52` (`2019-07-01` to `2020-12-31`): net `45.966%`, CAGR `28.546%`, drawdown `13.500%`, Sharpe `1.440`, Sortino `1.401`, orders `198`, fees `$214.87`.
- Run `53` (`2021-01-01` to `2022-12-31`): net `14.186%`, CAGR `6.871%`, drawdown `16.500%`, Sharpe `0.454`, Sortino `0.555`, orders `303`, fees `$303.69`.

Aggregate `G-current-all-defensive-off`:
- Average net profit: `13.940%`
- Average drawdown: `13.080%`
- Max drawdown: `20.300%`
- Average Sharpe: `0.541`
- Average Sortino: `0.598`
- Total orders: `919`
- Total fees: `$1464.74`

Comparison to earlier `A-default-off`:
- Runs `49-53` exactly match runs `21-25` on headline metrics, orders, and fees.
- This confirms that current-code all-defensive-off behavior is equivalent to the prior `A-default-off` baseline.

Comparison to best results so far:
- `2007-10-01` to `2008-12-31`: best so far is run `46` (`F-strict-severe-plus-pre-weak`) with net `-11.548%` and drawdown `15.200%`; this improves all-off by `5.312` net-profit points and `5.100` drawdown points.
- `2009-01-01` to `2009-12-31`: best return remains all-off / `C-pre-weak-only` at net `17.084%`; weak-stress has the best drawdown (`2.800%`) but gives up return.
- `2010-01-01` to `2010-12-31`: best so far is run `33` (`C-pre-weak-only`) with net `10.729%` and drawdown `10.200%`; this improves all-off by `1.403` net-profit points and `0.600` drawdown points.
- `2019-07-01` to `2020-12-31`: best return is run `34` (`C-pre-weak-only`) with net `46.839%`, improving all-off by `0.873` points with unchanged drawdown; weak-stress has lower drawdown (`10.900%`) but gives up too much return.
- `2021-01-01` to `2022-12-31`: best so far is run `35` (`C-pre-weak-only`) with net `18.081%` and drawdown `13.700%`; this improves all-off by `3.895` net-profit points and `2.800` drawdown points.

Interpretation:
- The all-off rerun is a valid baseline and confirms no hidden defensive behavior remains active when the three switches are false.
- The best single current configuration across the five windows remains `C-pre-weak-only`: better average return and better average drawdown than all-off, but it does not fix 2008 max drawdown.
- The best crash protection result is still strict severe plus pre-weak in 2008, but strict severe is too blunt for 2020 and 2021-22.
- The evidence supports the same strategic direction as before: keep pre-weak as the primary defensive improvement and make strict severe selective, not always-on whenever severe-crash mode is enabled.

Review:
- No code changes were made during this analysis.
- File normalization followed the established CrisisBackTestLogs naming convention.
- Open risk: per-window "best so far" is not the same as one deployable rule; a mixed best-by-window table can overstate what a single live algorithm would achieve unless the severe trigger is made selective and validated.

## Step 39: Live Default Parameter Recommendation

Date:
- 2026-05-18

Scope:
- Identify the best current parameter combination for a paper/live default before deploying the improved algorithm to Interactive Brokers paper.

Recommendation:
- Use `C-pre-weak-only` as the current live-ready default.
- Default values should be:
  - `crisis-diagnostics=false`
  - `weak-stress-overlay-enabled=false`
  - `pre-weak-guard-enabled=true`
  - `severe-crash-override-enabled=false`
  - `pre-weak-guard-drawdown-threshold=0.05`
  - keep severe-crash numeric defaults unchanged but inactive: `sev-crash-dd-entry=0.10`, `sev-crash-dd-exit=0.07`, `sev-crash-recovery-wks=2`

Reason:
- `C-pre-weak-only` is the best single broad configuration across the five crisis windows tested.
- It improved aggregate average net profit versus all-off from `13.940%` to `15.175%`.
- It improved aggregate average drawdown versus all-off from `13.080%` to `12.400%`.
- It materially improved 2021-22 versus all-off: net `18.081%` vs `14.186%`, drawdown `13.700%` vs `16.500%`.
- It improved 2010 and slightly improved 2020 while not hurting 2009.

Rejected as live default for now:
- `weak-stress-overlay-enabled=true`: reduces drawdown but sacrifices too much return in 2009 and 2020.
- `severe-crash-override-enabled=true`: strict severe improves 2008 but is too blunt and causes cash-drag risk in 2020 and 2021-22.

Live-trading risk:
- This change affects live/paper target allocation behavior because pre-weak guard would become active by default.
- Explicit user approval is required before changing code defaults.

Review:
- No code changes were made in this step.
- Current code still defaults all defensive switches to `false`; this note records the recommended change only.

## Step 40: Approved Pre-Weak Live Default Change

Date:
- 2026-05-18

Approval:
- User approved applying the live default recommendation.

Implementation scope:
- Change default behavior so `pre-weak-guard-enabled` defaults to `true`.
- Keep `weak-stress-overlay-enabled` default `false`.
- Keep `severe-crash-override-enabled` default `false`.
- Keep `crisis-diagnostics` default `false`.
- Keep numeric thresholds unchanged.

TDD plan:
- Update the existing default-parameter test to expect pre-weak guard enabled by default.
- Run the focused test before production-code change and confirm it fails for the expected reason.
- Apply the minimal production-code change.
- Rerun focused tests and build checks.

Live-trading risk:
- This changes paper/live target allocation behavior when the user does not explicitly set `pre-weak-guard-enabled`.
- The change is intentional based on the `C-pre-weak-only` crisis-window results.

TDD red attempt:
- Updated tests to expect `pre-weak-guard-enabled` default `true`, explicit `false` disable support, and invalid values falling back to the default.
- Initial no-build test run passed against a stale compiled test assembly and was not valid RED evidence.
- Build-enabled focused test run timed out before returning test results.
- Test-project-only build also timed out before returning output.
- Proceeding with the minimal code change because the intended failing assertion is clear, but verification must use a later successful build/test run.

Implementation:
- Added `StrategyConfig.DefaultPreWeakGuardEnabled = true`.
- Initialized `_preWeakGuardEnabled` from `StrategyConfig.DefaultPreWeakGuardEnabled` so live/paper mode gets the improved default.
- Changed backtest parameter parsing so absent or invalid `pre-weak-guard-enabled` falls back to the same default.
- Updated tests to prove explicit `pre-weak-guard-enabled=false` still disables the guard.

Verification:
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo --no-restore` passed with existing repo/package warnings only.
- `dotnet test Tests/QuantConnect.Tests.csproj --filter AegisGrowthAllocationTests --no-restore` passed: `44` passed, `0` failed.
- `git diff --check` passed with line-ending warnings only.

Strict code review:
- No blocking issues found.
- The default change is intentionally narrow: only pre-weak guard default changed to enabled.
- Weak-stress overlay, severe-crash override, and crisis diagnostics remain disabled by default.
- Explicit `pre-weak-guard-enabled=false` still disables pre-weak in backtests.
- Live/paper mode receives the improved default because the backing field is initialized to `StrategyConfig.DefaultPreWeakGuardEnabled` before the backtest-only parameter parsing block.
- Remaining risk: this changes live/paper allocation behavior during deteriorating neutral/favorable regimes; this is the intended risk tradeoff from the approved `C-pre-weak-only` results.

## Step 41: Pre-Defensive-Optimization Repo Version Check

Date:
- 2026-05-18

Scope:
- Identify the repository version immediately before Aegis defensive optimization work started.

Git history finding:
- First crisis/defensive-support commit: `6b14431fe` (`feat: add Aegis crisis diagnostics`).
- Its parent is `60cafe46e`, which is the repo version immediately before the crisis diagnostics and defensive optimization sequence began.
- First behavior-changing defensive optimization commit: `25a99698c` (`feat: add Aegis weak stress overlay`).
- Its parent is `4b914b05`, which is the repo version immediately before behavior-changing defensive optimization began, but after crisis diagnostics/log analysis had already been added.

Recommended interpretation:
- Use `60cafe46e` if the target is "before all defensive optimization work, including diagnostics."
- Use `4b914b05` if the target is "before defensive trading behavior changes, but with diagnostics work already present."

Review:
- No code changes were made.
- The distinction matters because diagnostics did not change allocation behavior, but it was part of the defensive optimization project workflow.
