# Aegis Pre-Weak Guard Implementation - 2026-05-09

## Step 1: Intake And Scope

Date:
- 2026-05-09

Request:
- Move to Experiment A after committing and pushing the overlay backtest evidence.

Experiment A goal:
- Add a parameter-gated partial pre-Weak guard to reduce first-leg crisis losses before formal Weak activation.

Why this is next:
- The Weak stress overlay improved 2008 but hurt 2009/2020 recovery and barely helped 2022.
- The remaining repeated weakness is pre-Weak damage, especially 2022 losses before Weak activation.

Initial constraints:
- Default behavior must remain unchanged.
- Live behavior must remain unchanged unless explicitly approved later.
- The guard should be partial, not full cash.
- The guard should be testable independently from `weak-stress-overlay-enabled`.

Planned parameter:
- `pre-weak-guard-enabled`

Initial design direction:
- When current active regime is Neutral or Favorable, and risk signals deteriorate enough to warn before formal Weak activation, use a lower-risk sleeve override.
- Candidate conservative sleeve target: Growth `0.24`, Defensive `0.30`, Cash `0.46`.
- Avoid using the guard during normal volatility without confirmation from multiple risk signals.

Open design work:
- Review current regime inputs and portfolio construction path.
- Choose exact activation condition and write tests before production code.

## Step 2: Design After Code Review

Date:
- 2026-05-09

Code reviewed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/RegimeModel.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`

Design:
- Add `pre-weak-guard-enabled`, parsed only in non-live initialization.
- Track a weekly equity high-water mark for guard decisions.
- Apply the guard only when the active regime is not Weak.
- Require both:
- portfolio drawdown from weekly high-water mark is at least the guard threshold,
- and at least one signal is deteriorating: trend not Favorable, breadth not Favorable, or stress is Weak.
- Use the guard sleeve target Growth `0.24`, Defensive `0.30`, Cash `0.46`.

Initial threshold:
- `pre-weak-guard-drawdown-threshold=0.05`
- Default threshold is `5%`.

Reasoning:
- The crisis logs show first Weak activation often occurs after about `8.5%` to `8.8%` equity damage.
- A 5% drawdown threshold is earlier but not hyper-sensitive.
- Requiring signal deterioration should reduce false positives during normal pullbacks.

Test-first plan:
- Add parameter tests for `pre-weak-guard-enabled`.
- Add parameter test for `pre-weak-guard-drawdown-threshold`.
- Add focused tests for the private guard activation helper:
- inactive by default,
- inactive in Weak regime,
- inactive below threshold,
- active above threshold with deteriorating signal,
- inactive above threshold when all signals remain Favorable.

## Step 3: Red Test Attempt

Date:
- 2026-05-09

Files touched:
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_PreWeak_Guard_Implementation_2026-05-09.md`

Tests added:
- `pre-weak-guard-enabled` is disabled by default.
- `pre-weak-guard-enabled=true` enables the experiment.
- invalid `pre-weak-guard-enabled` falls back to disabled.
- `pre-weak-guard-drawdown-threshold` can be configured.
- guard helper requires the parameter to be enabled.
- guard helper does not apply in Weak regime.
- guard helper requires drawdown threshold breach.
- guard helper requires signal deterioration.
- guard helper applies before Weak when drawdown and signals deteriorate.

Expected red references:
- `_preWeakGuardEnabled`
- `_preWeakGuardDrawdownThreshold`
- `_preWeakGuardEquityHighWaterMark`
- `ShouldApplyPreWeakGuard`

Red verification:
- `dotnet test Tests/QuantConnect.Tests.csproj --no-restore --filter "FullyQualifiedName~QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests" -nologo -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`

Red verification result:
- Focused test command failed as expected.
- Summary: `9` failed, `13` passed, `0` skipped, `22` total.
- Failures were the new pre-Weak guard tests, with `Expected: not null` for the missing private fields/helper.

## Step 4: Implementation

Date:
- 2026-05-09

Files changed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_PreWeak_Guard_Implementation_2026-05-09.md`

Implementation details:
- Added `pre-weak-guard-enabled`.
- Added `pre-weak-guard-drawdown-threshold`.
- Added `PreWeakGuardSleeveTargets` with Growth `0.24`, Defensive `0.30`, Cash `0.46`.
- Added non-live parsing for the guard parameters.
- Added weekly equity high-water tracking.
- Added `ShouldApplyPreWeakGuard`.
- Applied the guard as a sleeve-target override only when the Weak stress overlay is not active.

Behavioral safety:
- Default remains unchanged because `pre-weak-guard-enabled` defaults to `false`.
- Live behavior remains unchanged because the parameter is parsed only in the non-live branch.
- Weak regime behavior remains controlled by the existing Weak sleeve or Weak stress overlay, not the pre-Weak guard.

## Step 5: Verification Attempt And Safety Refinement

Date:
- 2026-05-09

Verification command:
- `dotnet test Tests/QuantConnect.Tests.csproj --no-restore --filter "FullyQualifiedName~QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests" -nologo -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`

Result:
- The focused test command did not complete.
- The test host crashed while resolving missing SGX map files: `../../../Data/equity/sgx/map_files`.
- The output also reported a Python.NET finalizer error: `GIL must always be released, and it must be released from the same thread that acquired it`.

Safety refinement:
- Updated the weekly review path so the pre-Weak guard high-water mark is only updated when `pre-weak-guard-enabled=true`.
- This keeps default and live paths more inert while preserving the experiment behavior.

Next verification:
- Rerun a focused test command after the refinement.
- Run a project build.
- Run `git diff --check`.
- Perform and record the strict code review before sign-off.

## Step 6: Focused Verification

Date:
- 2026-05-09

Commands:
- `git -c safe.directory=D:/Projects/Git/Lean-1 diff --check`
- `dotnet test Tests/QuantConnect.Tests.csproj --no-restore --filter "FullyQualifiedName~QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests" -nologo -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`

Results:
- `git diff --check` passed with no whitespace errors.
- Focused Aegis tests passed: `22` passed, `0` failed, `0` skipped.
- The parallel algorithm build attempt failed because `QuantConnect.Algorithm.CSharp.dll` was locked by `VBCSCompiler` while the test build was also running.

Follow-up:
- Rerun the algorithm build serially to separate code verification from the parallel build lock.

## Step 7: Serial Build Verification

Date:
- 2026-05-09

Command:
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`

Result:
- Build passed.
- Summary: `0` warnings, `0` errors.

Next step:
- Perform strict code review of the final diff.

## Step 8: Strict Code Review

Date:
- 2026-05-09

Reviewed files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_PreWeak_Guard_Implementation_2026-05-09.md`

Review result:
- No blocking code issues found.
- The experiment is parameter-gated and remains off by default.
- The parameter is parsed only for non-live runs, so live allocation behavior remains unchanged.
- The high-water mark is updated only when the experiment is enabled.
- The Weak stress overlay keeps priority when both experiments are enabled.
- Focused tests cover default-off behavior, parameter parsing, threshold parsing, Weak-regime exclusion, drawdown requirement, signal-deterioration requirement, and positive activation.

Residual risks:
- The guard may reduce annual return if it activates during normal pullbacks with temporary signal deterioration.
- The guard overrides sleeve targets but does not override regime-specific holding counts, so a Favorable-regime guard can still distribute the lower Growth sleeve across Favorable holding counts.
- The default `5%` drawdown threshold is a hypothesis from crisis diagnostics, not yet proven by backtest.

Required next evidence:
- Run the same five-window crisis/control backtest set with `pre-weak-guard-enabled=true`.
- Keep `weak-stress-overlay-enabled=false` for Experiment A isolation unless explicitly testing combined behavior later.
