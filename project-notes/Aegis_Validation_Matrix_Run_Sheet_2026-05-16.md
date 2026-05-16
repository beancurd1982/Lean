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
