# Aegis Severe Crash Override Implementation - 2026-05-10

## Step 1: Intake And Implementation Plan

Date:
- 2026-05-10

Request:
- Implement the approved diagnostic-plus-severe-crash experiment after the pre-Weak guard backtest review.

Goal:
- Add a disabled-by-default, non-live severe-crash sleeve override that can be backtested with explicit diagnostics.

Behavior scope:
- Add `severe-crash-override-enabled`.
- Parse it only for non-live runs.
- Keep default and live behavior unchanged.
- Trigger only in Weak regime when severe stress is present, weekly drawdown from high-water mark is at least `10%`, and at least two of trend/breadth/stress are Weak.
- Target while active: Growth `0.00`, Defensive `0.20`, Cash `0.80`.
- Keep old `weak-stress-overlay-enabled` available for comparison, but do not promote it.

Diagnostics scope:
- Add explicit attribution fields to `[AEGIS-DIAG]`.
- Fields: `PreWeakGuardActive`, `SevereCrashOverrideActive`, `SleeveOverride`, `OverrideReason`, `DrawdownFromHigh`, `BaseTarget`, `FinalTarget`.
- Do not rely on the ambiguous effective `Target=...` field to infer override activation.

Files planned:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_Severe_Crash_Override_Implementation_2026-05-10.md`

Test-first plan:
- Add parameter tests for default-disabled, enabled-by-true, and invalid fallback.
- Add activation tests for disabled parameter, non-Weak regime, missing severe stress, drawdown below threshold, too few weak signals, and positive activation.
- Add PortfolioManager sleeve test for the severe-crash target.
- Add a diagnostic formatting test proving explicit override fields are present.

Verification plan:
- Run focused `AegisGrowthAllocationTests`.
- Run `Algorithm.CSharp` build.
- Run `git diff --check`.
- Perform strict code review and record results.

Live trading safety:
- New behavior is parsed only in non-live initialization.
- Default parameter is false.
- No live state schema change is planned.

## Step 2: Red Test Attempt

Date:
- 2026-05-10

Files touched:
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_Severe_Crash_Override_Implementation_2026-05-10.md`

Tests added:
- severe-crash override defaults disabled,
- `severe-crash-override-enabled=true` enables it,
- invalid severe-crash parameter falls back disabled,
- severe-crash helper requires enabled parameter,
- requires Weak regime,
- requires severe stress,
- requires at least `10%` drawdown from high-water mark,
- requires at least two Weak signals,
- applies with Weak regime, severe stress, `10%` drawdown, and two Weak signals,
- PortfolioManager applies the severe-crash sleeve target,
- diagnostic override attribution is formatted explicitly.

Red verification:
- `dotnet test Tests/QuantConnect.Tests.csproj --no-restore --filter "FullyQualifiedName~QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests" -nologo -p:RunAnalyzers=false -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`

Red result:
- The first run timed out during broad analyzer/build output before reaching a useful summary.
- The rerun with analyzer output filtered failed as expected.
- Failure: `StrategyConfig` does not contain `SevereCrashOverrideSleeveTargets`.

Next step:
- Add the minimal production constants, state, helper, sleeve override, and diagnostic formatter.

## Step 3: Implementation

Date:
- 2026-05-10

Files changed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_Severe_Crash_Override_Implementation_2026-05-10.md`

Implementation details:
- Added `severe-crash-override-enabled`.
- Added `DefaultSevereCrashOverrideDrawdownThreshold=0.10`.
- Added `SevereCrashOverrideSleeveTargets` with Growth `0.00`, Defensive `0.20`, Cash `0.80`.
- Added `_severeCrashOverrideEnabled`, parsed only in non-live initialization.
- Reused the weekly high-water mark for pre-Weak and severe-crash drawdown calculations.
- Added `ShouldApplySevereCrashOverride`.
- Added explicit diagnostic attribution fields through `FormatOverrideDiagnostics`.
- Override priority is:
- severe-crash override,
- weak-stress overlay,
- pre-Weak guard,
- no override.

Green verification:
- `dotnet test Tests/QuantConnect.Tests.csproj --no-restore --filter "FullyQualifiedName~QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests" -nologo -p:RunAnalyzers=false -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`

Green result:
- Focused tests passed: `33` passed, `0` failed, `0` skipped.

Next step:
- Run build and full focused verification without implementation filters where practical.

## Step 4: Refactor After Green

Date:
- 2026-05-10

Refactor:
- Renamed the high-water mark field from pre-Weak-specific wording to defensive-override wording.
- Renamed the update helper to `UpdateDefensiveOverrideHighWaterMark`.
- Routed pre-Weak drawdown calculation through `CalculateDrawdownFromHigh`.

Reason:
- The high-water mark is now shared by both `pre-weak-guard-enabled` and `severe-crash-override-enabled`.
- The old name made the severe-crash implementation harder to audit.

Next step:
- Rerun focused tests and build after the refactor.

## Step 5: Verification

Date:
- 2026-05-10

Commands:
- `git -c safe.directory=D:/Projects/Git/Lean-1 diff --check`
- `dotnet test Tests/QuantConnect.Tests.csproj --no-build --filter "FullyQualifiedName~QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests" -nologo`
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo -p:RunAnalyzers=false -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`

Results:
- `git diff --check` passed with no whitespace errors.
- Focused Aegis tests passed: `33` passed, `0` failed, `0` skipped.
- Algorithm CSharp build passed with `0` errors.
- Build emitted existing project warnings outside this change scope.

Review correction:
- A `--no-build` focused test run after the test-file refactor could have reused stale compiled tests.
- Reran focused Aegis tests with build enabled and analyzer execution disabled.
- Fresh result remained green: `33` passed, `0` failed, `0` skipped.

Strict review finding:
- The initial implementation hard-coded the severe-crash drawdown threshold at `10%`.
- The agreed experiment described a configurable trigger, so this should be exposed as a parameter before backtesting.

Additional red tests:
- `severe-crash-override-drawdown-threshold` can be configured.
- invalid severe-crash threshold falls back to the default.

Additional red result:
- Focused tests failed as expected: `2` failed, `33` passed, `0` skipped.
- Failure was `Expected: not null` for missing `_severeCrashOverrideDrawdownThreshold`.

Next step:
- Add the threshold parameter and use it in the severe-crash helper.

## Step 6: Configurable Threshold Implementation

Date:
- 2026-05-10

Implementation:
- Added `severe-crash-override-drawdown-threshold`.
- Added `_severeCrashOverrideDrawdownThreshold`.
- Parsed the parameter only in non-live initialization.
- Updated `ShouldApplySevereCrashOverride` to use the configured threshold instead of a hard-coded default.

Green result:
- Focused Aegis tests passed: `35` passed, `0` failed, `0` skipped.

Next step:
- Run final verification and strict review.

## Step 7: Final Verification

Date:
- 2026-05-10

Commands:
- `git -c safe.directory=D:/Projects/Git/Lean-1 diff --check`
- `dotnet test Tests/QuantConnect.Tests.csproj --no-restore --filter "FullyQualifiedName~QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests" -nologo -p:RunAnalyzers=false -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo -p:RunAnalyzers=false -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`

Results:
- `git diff --check` passed with no whitespace errors.
- Focused Aegis tests passed: `35` passed, `0` failed, `0` skipped.
- Algorithm CSharp build passed: `0` warnings, `0` errors.

Next step:
- Strict code review.

## Step 8: Strict Code Review

Date:
- 2026-05-10

Reviewed files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_Severe_Crash_Override_Implementation_2026-05-10.md`

Review findings:
- No blocking allocation-safety issue found.
- New behavior is disabled by default and parsed only in non-live mode.
- No live state schema change was introduced.
- Severe-crash override priority is explicit and narrow.
- `weak-stress-overlay-enabled` remains available for comparison and is not promoted.
- Diagnostic attribution now makes override state auditable without inferring from effective target weights.

Review fix:
- Updated the high-water mark tracking condition to also run when `crisis-diagnostics=true`.
- This makes `DrawdownFromHigh` meaningful in baseline diagnostic-only runs, including the planned 2x2 ablation matrix.
- Live behavior remains unchanged because `crisis-diagnostics` is parsed only in non-live mode.

Residual risks:
- The severe-crash trigger is still a hypothesis and must be validated by backtest before any promotion.
- Order tags were not changed in this step; attribution is improved in weekly diagnostics, not per order.
- The first severe-crash target is `G0/D0.20/C0.80`; all-cash behavior is intentionally left for a later variant if needed.

Next step:
- Rerun verification after the review fix.

## Step 9: Final Verification After Review Fix

Date:
- 2026-05-10

Commands:
- `git -c safe.directory=D:/Projects/Git/Lean-1 diff --check`
- `dotnet test Tests/QuantConnect.Tests.csproj --no-restore --filter "FullyQualifiedName~QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests" -nologo -p:RunAnalyzers=false -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo -p:RunAnalyzers=false -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`

Results:
- `git diff --check` passed; only line-ending warnings were reported.
- Focused Aegis tests passed: `35` passed, `0` failed, `0` skipped. Existing package vulnerability warnings were reported during the test project load.
- Algorithm CSharp build passed: `0` warnings, `0` errors.

Final status:
- Severe-crash override experiment is implemented, disabled by default, and scoped to non-live parameter parsing.
- Diagnostics now report the active override, override reason, drawdown from high, base target, and final target.
- Review fix completed: the defensive override high-water mark is tracked when crisis diagnostics are enabled, so diagnostic drawdown values are meaningful even when override parameters are disabled.
