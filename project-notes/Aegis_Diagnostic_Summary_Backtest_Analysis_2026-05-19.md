# Aegis Diagnostic Summary Backtest Analysis - 2026-05-19

## Step 1: Intake

Date:
- 2026-05-19

Scope:
- Analyze the uploaded full-period diagnostic summary backtest files in `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs`.

Uploaded files:
- `DiagSummary_2016-2026.json`
- `DiagSummary_2016-2026_orders.csv`
- `DiagSummary_2016-2026_logs.txt`

Normalized log:
- `2026-05-19_221413__AegisGrowthAllocation__DiagSummary_2016-2026_logs.txt`

Initial task:
- Confirm whether the compact `[AEGIS-DIAG-SUMMARY]` survived QuantConnect log truncation.
- Compare overview and order behavior against the prior `DiagDefault_2016-2026` run.
- Record whether the diagnostic logging change was behavior-neutral.

## Step 2: Findings

Date:
- 2026-05-19

Compact diagnostic result:
- The downloaded log contains the expected `[AEGIS-DIAG-SUMMARY]`.
- New diagnostic log size is 1,368 bytes, versus 102,420 bytes for the prior verbose diagnostic log.
- This confirms the compact end-of-run summary avoids the previous 100KB truncation problem for the 2016-2026 full-period diagnostic run.

Summary line:
- `Weeks=522`
- `Start=2016-01-04`
- `End=2025-12-29`
- `PreWeakWeeks=80`
- `NonPreWeakWeeks=442`
- `SevereCrashWeeks=0`
- `WeakRegimeWeeks=65`
- `PreWeakAvgDrawdown=0.0742`
- `NonPreWeakAvgDrawdown=0.0269`
- `PreWeakNextReturnAvg=0.0018`
- `NonPreWeakNextReturnAvg=0.0037`
- `PreWeakFwd4Avg=0.0075`
- `PreWeakFwd8Avg=0.0254`
- `PreWeakFwd12Avg=0.0468`
- `PreWeakFwd4WinRate=0.6625`
- `PreWeakAvgTarget=G0.2400/D0.3000/C0.4600`
- `NonPreWeakAvgTarget=G0.5012/D0.2633/C0.2354`

Behavior-neutral check versus `DiagDefault_2016-2026`:
- Compounding annual return: 18.421% in both runs.
- Drawdown: 13.800% in both runs.
- Net profit: 443.042% in both runs.
- End equity: $162,912.56 in both runs.
- Total orders: 1,594 in both runs.
- Orders CSV SHA-256 prefix: `92efd53aea1de4d5` in both runs.

Comparison against original `Base` 2016-2026 run:
- Defensive default run CAGR: 18.421%, versus Base 18.868%.
- Defensive default max drawdown: 13.800%, versus Base 16.500%.
- Defensive default end equity: $162,912.56, versus Base $169,174.07.
- Defensive default order count: 1,594, versus Base 1,540.

Interpretation:
- The compact diagnostic logging change did not change trading behavior.
- The current default defensive behavior is effectively the pre-weak guard only in this full-period run; severe-crash override had zero active weeks because it is not enabled by default in backtest parameters.
- Pre-weak guard activates during meaningfully weaker conditions: average drawdown during pre-weak weeks was 7.42%, versus 2.69% outside pre-weak weeks.
- Forward returns after pre-weak activation were positive on average, especially 8-week and 12-week horizons. This suggests the guard is often active near recoverable weakness, not only before further downside.

Proposed next step:
- Run an exact current-code A/B isolation test with `pre-weak-guard-enabled=false` for 2016-2026.
- Purpose: separate the impact of current code/infrastructure from the pre-weak guard itself, using the same latest codebase rather than comparing against the older `Base` commit.
- Suggested upload names:
  - `NoPreWeak_2016-2026.json`
  - `NoPreWeak_2016-2026_orders.csv`
  - `NoPreWeak_2016-2026_logs.txt`

Strict review:
- No new code changes were made during this analysis.
- The log-index status should be updated from `unreviewed` to `reviewed` for the normalized diagnostic summary log.

Open question:
- If the no-pre-weak A/B confirms that the pre-weak guard trades too much upside for drawdown reduction, Phase 4 should tune pre-weak severity rather than adding another defensive overlay.

## Step 3: Verification

Date:
- 2026-05-19

Checks:
- `git diff --check` passed.
- `log-index.csv` was updated to mark the normalized `DiagSummary_2016-2026` log as reviewed and linked to this note.
