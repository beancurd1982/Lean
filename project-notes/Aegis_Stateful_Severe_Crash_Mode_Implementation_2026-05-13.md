# Aegis Stateful Severe-Crash Mode Implementation

## Step 1: Implementation Plan

Date:
- 2026-05-13

Request:
- Implement the next severe-crash experiment before another cloud backtest round.
- Do not commit or push.
- Provide a final report when complete.

Goal:
- Replace the current one-week severe-crash override behavior with a stateful mode that avoids fast enter/exit churn.

Design:
- Keep `severe-crash-override-enabled` disabled by default.
- Enter severe-crash mode when all are true:
  - override parameter is enabled
  - active regime is `Weak`
  - stress is severe
  - drawdown from high-water mark is at or above 10%
  - at least two weak signals are present
- Hold severe-crash mode while recovery is unconfirmed.
- Exit severe-crash mode when either:
  - drawdown improves below 7%
  - regime is `Neutral` or `Strong` for two consecutive weekly reviews
- Use softer experimental severe target:
  - `G0.05/D0.35/C0.60`
- Add diagnostics:
  - `SevereCrashModeState=enter/hold/exit/none`
  - `SevereCrashRecoveryWeeks`
  - `SevereCrashExitReason`

Files expected to change:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- this project note

TDD plan:
- Add failing tests for default parameters and state transitions.
- Verify tests fail for missing stateful behavior.
- Implement minimal state and diagnostics to pass.
- Run focused Aegis tests and Algorithm CSharp build.

Safety notes:
- This affects backtest behavior when `severe-crash-override-enabled=true`.
- Live behavior should remain unchanged because the experimental parameter defaults off and parameter parsing remains non-live only.
- The change is intentionally not promoted to default behavior.

Next step:
- Inspect current implementation and tests before writing failing tests.

## Step 2: Red Tests Added

Date:
- 2026-05-13

Tests added:
- Config parsing for `sev-crash-dd-exit`.
- Config parsing for `sev-crash-recovery-wks`.
- Severe-crash mode enter behavior.
- Severe-crash mode hold behavior when recovery is unconfirmed.
- Severe-crash mode exit by drawdown recovery.
- Severe-crash mode exit by consecutive recovered regime weeks.
- Softer severe-crash sleeve target expectations: `G0.05/D0.35/C0.60`.
- Diagnostics expectations for `SevereCrashModeState`, `SevereCrashRecoveryWeeks`, and `SevereCrashExitReason`.

Expected red result:
- Tests should fail because state fields, parser fields, `UpdateSevereCrashMode`, softer sleeve targets, and expanded diagnostics are not implemented yet.

Red result:
- Focused Aegis test build failed as expected.
- First missing pieces reported:
  - `StrategyConfig.DefaultSevereCrashOverrideExitDrawdownThreshold`
  - `StrategyConfig.DefaultSevereCrashOverrideRecoveryConfirmationWeeks`

Next step:
- Implement the minimal configuration fields, state machine, softer target, and diagnostics required by the tests.

## Step 3: Implementation Applied

Date:
- 2026-05-13

Files touched:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_Stateful_Severe_Crash_Mode_Implementation_2026-05-13.md`

Changes made:
- Added experimental parameters:
  - `sev-crash-dd-entry`
  - `sev-crash-dd-exit`
  - `sev-crash-recovery-wks`
- Added defaults:
  - exit drawdown threshold: `0.07`
  - recovery confirmation weeks: `2`
- Changed severe-crash experimental target from `G0.00/D0.20/C0.80` to `G0.05/D0.35/C0.60`.
- Added stateful severe-crash mode:
  - enter when the existing severe weak drawdown condition is met
  - hold while recovery is unconfirmed
  - exit when drawdown recovers below the exit threshold
  - exit after consecutive `Neutral` or `Favorable` weekly regimes meet the confirmation count
- Added diagnostics:
  - `SevereCrashModeState`
  - `SevereCrashRecoveryWeeks`
  - `SevereCrashExitReason`
- Updated tests to cover parsing, state transitions, softer sleeve targets, and diagnostics.

Live-trading safety note:
- The feature remains disabled by default.
- Parameter parsing remains in the non-live initialization path.
- No live behavior should change unless this experiment is later promoted into live initialization.

Next step:
- Run focused verification and build checks.

## Step 4: Verification

Date:
- 2026-05-13

Commands run:
- `git -c safe.directory=D:/Projects/Git/Lean-1 diff --check`
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo -p:RunAnalyzers=false -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`
- `dotnet vstest Tests\bin\Debug\QuantConnect.Tests.dll --TestCaseFilter:"Name~SevereCrash"`
- `dotnet vstest Tests\bin\Debug\QuantConnect.Tests.dll --TestCaseFilter:"Name~PreWeakGuard"`
- `dotnet vstest Tests\bin\Debug\QuantConnect.Tests.dll --TestCaseFilter:"Name~WeakStressOverlay"`
- `dotnet vstest Tests\bin\Debug\QuantConnect.Tests.dll --TestCaseFilter:"Name~OverrideAttribution"`
- `dotnet vstest Tests\bin\Debug\QuantConnect.Tests.dll --TestCaseFilter:"FullyQualifiedName~QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests"`

Results:
- Diff whitespace check: passed, with existing line-ending normalization warnings only.
- Algorithm CSharp build: passed, `0 Warning(s)`, `0 Error(s)`.
- Severe-crash focused tests: passed, `20/20`.
- Pre-weak guard tests: passed, `9/9`.
- Weak-stress overlay tests: passed, `4/4`.
- Override attribution diagnostics test: passed, `1/1`.
- Full `AegisGrowthAllocationTests` class: passed, `43/43`.

Verification limitations:
- `dotnet test Tests/QuantConnect.Tests.csproj` attempted NuGet restore and failed because access to `api.nuget.org` is blocked in this environment.
- Re-running the test-project build with escalated permissions still failed with the same NuGet access errors.
- `dotnet vstest` was used against the current `Tests/bin/Debug/QuantConnect.Tests.dll`, which had been rebuilt during the current verification cycle.
- Separate `AegisPortfolioManagerTests` still has one failing test:
  - `LimitsBuysToReleasedReserveAndPrioritizesExistingGrowthHoldings`
  - failure: expected an existing growth target weight greater than `0.075`, actual remained `0.075`
  - `Tests/Algorithm/AegisPortfolioManagerTests.cs` and `PortfolioManager.cs` were not changed by this task, so this is recorded as an existing/unrelated verification gap rather than part of this severe-crash implementation.

## Step 5: Strict Code Review

Date:
- 2026-05-13

Review scope:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`

Findings:
- No blocking safety or correctness issues found in the implemented severe-crash state machine.
- Entry remains gated by the existing severe weak drawdown condition and `severe-crash-override-enabled=true`.
- Exit behavior is explicit and auditable through diagnostics.
- `Neutral` and `Favorable` regimes are used as recovered regimes; this maps the earlier "Strong" wording to the enum available in the codebase.
- The mode is not persisted for live restart recovery, but this is acceptable for this experiment because the feature remains disabled by default and parameter parsing remains non-live only. If this is later promoted to live trading, state persistence must be revisited before enablement.
- The softer target `G0.05/D0.35/C0.60` is intentional for this experiment and covered by tests.

Next step:
- Run cloud backtests with this experiment enabled and compare against the previous 5-window baseline/Experiment A results.

## Step 6: Cloud Parameter Name Fix

Date:
- 2026-05-13

Issue:
- QuantConnect cloud parameter names must be no more than 30 characters.
- Three newly added parameter names were too long:
  - `severe-crash-override-drawdown-threshold`
  - `severe-crash-override-exit-drawdown-threshold`
  - `severe-crash-override-recovery-confirmation-weeks`

Approved replacement names:
- `severe-crash-override-drawdown-threshold` -> `sev-crash-dd-entry`
- `severe-crash-override-exit-drawdown-threshold` -> `sev-crash-dd-exit`
- `severe-crash-override-recovery-confirmation-weeks` -> `sev-crash-recovery-wks`

Planned changes:
- Rename the constants in `StrategyConfig.cs`.
- Update tests to use the cloud-safe names.
- Keep behavior and default values unchanged.

Next step:
- Apply the rename and rerun focused verification.

## Step 7: Parameter Rename Verification and Review

Date:
- 2026-05-13

Changes applied:
- `StrategyConfig.SevereCrashOverrideDrawdownThresholdParameter` now maps to `sev-crash-dd-entry`.
- `StrategyConfig.SevereCrashOverrideExitDrawdownThresholdParameter` now maps to `sev-crash-dd-exit`.
- `StrategyConfig.SevereCrashOverrideRecoveryConfirmationWeeksParameter` now maps to `sev-crash-recovery-wks`.
- Tests now use the short cloud-safe names.

Verification:
- `git -c safe.directory=D:/Projects/Git/Lean-1 diff --check`
  - passed, with line-ending normalization warnings only.
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo -p:RunAnalyzers=false -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`
  - passed, `0 Error(s)`.
  - existing repository warnings remain.
- `dotnet vstest Tests\bin\Debug\QuantConnect.Tests.dll --TestCaseFilter:"FullyQualifiedName~QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests"`
  - passed, `43/43`.
- `dotnet vstest Tests\bin\Debug\QuantConnect.Tests.dll --TestCaseFilter:"Name~SevereCrash"`
  - passed, `20/20`.
- Search confirmed the retired long parameter keys are no longer present in code or tests.

Strict review:
- No blocking issue found.
- Rename is limited to cloud parameter keys; behavior, defaults, and state-machine logic are unchanged.
- `severe-crash-override-enabled` remains valid at 29 characters and was not renamed.

Cloud backtest parameters:
- `crisis-diagnostics=true`
- `severe-crash-override-enabled=true`
- `sev-crash-dd-entry=0.10`
- `sev-crash-dd-exit=0.07`
- `sev-crash-recovery-wks=2`
- keep `weak-stress-overlay-enabled=false` and `pre-weak-guard-enabled=false` when isolating this severe-crash experiment.

## Step 8: Stateful Severe-Crash Backtest Result Analysis

Date:
- 2026-05-13

Input files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/16.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/16_logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/16_orders.csv`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/17.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/17_logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/17_orders.csv`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/18.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/18_logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/18_orders.csv`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/19.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/19_logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/19_orders.csv`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/20.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/20_logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/20_orders.csv`

Assumed mapping:
- `16`: `2007-10-01` to `2008-12-31`
- `17`: `2009-01-01` to `2009-12-31`
- `18`: `2010-01-01` to `2010-12-31`
- `19`: `2019-07-01` to `2020-12-31`
- `20`: `2021-01-01` to `2022-12-31`

Next step:
- Parse overview metrics, diagnostics logs, and orders, then compare against the previous severe-crash override run `11` to `15`.

## Step 9: Stateful Severe-Crash Backtest Findings

Date:
- 2026-05-13

Run status:
- Runs `16` to `20` all completed.
- No runtime errors were reported in the overview JSON files.
- All five runs used the intended parameters:
  - `crisis-diagnostics=true`
  - `weak-stress-overlay-enabled=false`
  - `pre-weak-guard-enabled=false`
  - `severe-crash-override-enabled=true`
  - `sev-crash-dd-entry=0.10`
  - `sev-crash-dd-exit=0.07`
  - `sev-crash-recovery-wks=2`

Overview metrics:

| Run | Window | Net Profit | CAR | Drawdown | Sharpe | Orders | Fees |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 16 | 2007-10-01 to 2008-12-31 | -15.010% | -12.163% | 18.500% | -1.359 | 146 | $336.55 |
| 17 | 2009-01-01 to 2009-12-31 | 17.087% | 17.103% | 4.300% | 1.633 | 89 | $176.25 |
| 18 | 2010-01-01 to 2010-12-31 | 9.320% | 9.329% | 10.800% | 0.638 | 185 | $419.33 |
| 19 | 2019-07-01 to 2020-12-31 | 43.911% | 27.342% | 13.500% | 1.392 | 199 | $215.04 |
| 20 | 2021-01-01 to 2022-12-31 | 12.437% | 6.048% | 17.400% | 0.396 | 303 | $303.61 |

Comparison against prior severe-crash run `11` to `15`:

| Window | Stateful Net Change | Stateful Drawdown Change | Interpretation |
| --- | ---: | ---: | --- |
| 2008 crash | -3.461 pts | +3.300 pts | Worse. The softer `G5/D35/C60` target lost more than the earlier `G0/D20/C80` target in the deepest crash window. |
| 2009 rebound | 0.000 pts | 0.000 pts | Same. Severe-crash mode never triggered. |
| 2010 chop | 0.000 pts | 0.000 pts | Same. Severe-crash mode never triggered. |
| 2020 Covid | +1.931 pts | 0.000 pts | Better return with same reported drawdown. Hysteresis held protection longer after the initial shock. |
| 2021-22 bear | +2.342 pts | -1.800 pts | Better than the prior severe-crash run, but still worse than the pre-weak-guard round. |

Severe-crash diagnostic behavior:
- Run `16`: active for 15 diagnostic weeks from `2008-09-22` through `2008-12-29`; no exit before the backtest ended.
- Run `17`: active for 0 weeks.
- Run `18`: active for 0 weeks.
- Run `19`: active for 11 weeks from `2020-03-23` through `2020-06-01`; exited on `2020-06-08` via `regime-recovered`.
- Run `20`: active for 16 weeks across two periods; first period exited on `2022-04-04` via `regime-recovered`; second period held through late July 2022.

Key interpretation:
- The state machine works technically: it enters only in severe weak drawdown conditions, holds during unconfirmed recovery, and exits via the intended recovered-regime rule.
- The softer target helped in `2020` and improved the previous severe-crash result in `2021-22`.
- The same softer target hurt badly in `2008`; the old stricter crash target was clearly better during the true systemic crash.
- This suggests the next improvement should not be a single severe-crash target for all crashes. A better design is likely a two-stage severe crash policy:
  - initial severe-crash entry uses stricter `G0/D20/C80` or similar when stress is still severe
  - recovery/hold phase transitions to softer `G5/D35/C60` only after stress improves or regime recovery begins

Orders:
- Order counts are reasonable relative to earlier runs.
- Run `20` order count dropped from the previous severe-crash run `15` count of `318` to `303`, while return and drawdown both improved versus run `15`.
- Run `16` order count increased slightly versus run `11` from `142` to `146`, with worse return and drawdown.

Next recommendation:
- Do not promote the current stateful severe-crash version as-is.
- Test a two-stage severe-crash mode next:
  - enter stage: strict target `G0/D20/C80`
  - hold stage: keep strict target while `Severe=True`
  - recovery stage: switch to softer `G5/D35/C60` after severe stress clears, then exit after the existing recovered-regime confirmation.

## Step 10: Commit Preparation

Date:
- 2026-05-13

Request:
- Rename the newly uploaded run `16` to `20` backtest files properly.
- Include the renamed result files, code changes, tests, and this note in the commit.
- Push the `research-algorithms` branch.

Planned file naming:
- Use the existing crisis backtest naming pattern:
  - `<run>__<start>_to_<end>__AegisGrowthAllocation__stateful-severe-crash__overview.json`
  - `<run>__<start>_to_<end>__AegisGrowthAllocation__stateful-severe-crash__logs.txt`
  - `<run>__<start>_to_<end>__AegisGrowthAllocation__stateful-severe-crash__orders.csv`

Run mapping:
- `16`: `2007-10-01` to `2008-12-31`
- `17`: `2009-01-01` to `2009-12-31`
- `18`: `2010-01-01` to `2010-12-31`
- `19`: `2019-07-01` to `2020-12-31`
- `20`: `2021-01-01` to `2022-12-31`

Next step:
- Rename the files and verify there are no raw `16.json` to `20_orders.csv` names left.

## Step 11: Pre-Commit Verification

Date:
- 2026-05-13

Renamed files:
- 15 uploaded result files were normalized under the `stateful-severe-crash` experiment label.
- Raw names `16.json` through `20_orders.csv` are no longer present.

Verification:
- `git -c safe.directory=D:/Projects/Git/Lean-1 diff --check`
  - passed, with line-ending normalization warnings only.
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo -p:RunAnalyzers=false -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`
  - passed, `0 Warning(s)`, `0 Error(s)`.
- `dotnet vstest Tests\bin\Debug\QuantConnect.Tests.dll --TestCaseFilter:"FullyQualifiedName~QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests"`
  - passed, `43/43`.

Strict review:
- No blocking issue found in the rename or code changes.
- The commit scope is limited to the stateful severe-crash implementation, tests, project note, and related crisis backtest result artifacts.

Next step:
- Stage, commit, and push `research-algorithms`.
