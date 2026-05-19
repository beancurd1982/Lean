# Aegis Defensive State Persistence Implementation

## Step 1: Start Phase 1 Implementation

Date:
- 2026-05-19

Scope:
- Implement Phase 1 from `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/Plans/Aegis_Defensive_State_Persistence_And_Attribution_Plan_2026-05-19.md`.

Goal:
- Persist defensive runtime state so paper/live restart does not reset defensive high-water mark or severe-crash mode state.

Initial code finding:
- `AegisGrowthAllocation.BuildPersistedState()` currently saves regime, reserve, last review, targets, holdings, and open orders.
- `AegisGrowthAllocation.RestorePersistedRuntimeState()` currently restores regime, reserve, last review, and targets.
- Runtime-only defensive fields are not persisted:
  - `_defensiveOverrideEquityHighWaterMark`
  - `_severeCrashModeActive`
  - `_severeCrashRecoveryWeeks`
  - `_severeCrashModeState`
  - `_severeCrashExitReason`

Planned files touched:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/LiveStateStore.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`

Risk:
- This affects live/paper restart behavior. The change should preserve current allocation behavior during uninterrupted backtests and live sessions.

Verification target:
- Add failing restart-safety tests first.
- Run targeted `AegisGrowthAllocationTests`.
- Build the algorithm project.

## Step 2: Implement Defensive Runtime State Persistence

Date:
- 2026-05-19

Files touched:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/LiveStateStore.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`

Changes:
- Bumped live-state key/schema from `V1`/`1` to `V2`/`2`.
- Added persisted defensive runtime fields to `AegisLiveState`.
- Added normalization for persisted defensive values on load/save.
- Added defensive runtime values to the live-state fingerprint.
- Restored defensive high-water mark and severe-crash runtime state on startup.
- Saved defensive high-water mark and severe-crash runtime state in `BuildPersistedState()`.
- Added live restore/save log output for defensive high-water and severe-crash fields.
- Added restart-safety tests for persisted state, restored state, and fingerprint change detection.

Verification:
- Initial `dotnet test Tests/QuantConnect.Tests.csproj --filter AegisGrowthAllocationTests` was blocked by sandboxed NuGet access.
- Escalated rerun restored/build dependencies but the first attempt timed out.
- `dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo` succeeded with warnings and 0 errors.
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo --no-restore` succeeded with warnings and 0 errors.
- `dotnet vstest Tests\bin\Debug\QuantConnect.Tests.dll /Tests:QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests.BuildPersistedStateIncludesDefensiveRuntimeState,QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests.RestorePersistedRuntimeStateRestoresDefensiveRuntimeState,QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests.LiveStateFingerprintChangesWhenDefensiveRuntimeStateChanges` reported 3 passed, 0 failed.
- Full filtered `dotnet test ... --filter AegisGrowthAllocationTests --no-restore` reached test execution but aborted due to unrelated environment dependency: missing `../../../Data/equity/sgx/map_files`, followed by Python.NET GIL finalizer crash.
- `git diff --check` passed.

Strict code review:
- No allocation target logic was changed.
- Defensive state is clamped on restore/save to avoid negative high-water mark and negative recovery weeks.
- Null/blank severe-crash state strings are normalized to `none`.
- Fingerprint now includes defensive state so live saves are not skipped when only defensive state changes.
- Live state key/schema bump intentionally resets old v1 ObjectStore state; broker holdings are still reconciled from broker state on startup.
- Remaining risk: the full Aegis test class could not run through `dotnet test` in this local environment because of the unrelated SGX map-file/Python finalizer crash. The new tests passed directly through `dotnet vstest`, and both affected projects compile.

## TODO: Local Test Environment Fix

Date:
- 2026-05-19

Issue:
- Full filtered `dotnet test Tests/QuantConnect.Tests.csproj --filter AegisGrowthAllocationTests --no-restore` aborts during test-host execution because the local environment is missing `../../../Data/equity/sgx/map_files`.
- The abort is followed by a Python.NET GIL finalizer crash.

Impact:
- This blocks a clean full-class `AegisGrowthAllocationTests` run in the local workspace.
- It does not indicate a compile failure in the Aegis defensive persistence changes.

Deferred task:
- Restore or stub the missing SGX map-file test data path required by the QuantConnect test host.
- Re-run `dotnet test Tests/QuantConnect.Tests.csproj --filter AegisGrowthAllocationTests --no-restore`.
- Record the result before treating full local test execution as healthy again.

Current mitigation:
- Both affected projects build successfully.
- The three new defensive persistence tests pass directly through `dotnet vstest`.

## Step 3: Move To Phase 3 Diagnostic Backtest

Date:
- 2026-05-19

Scope:
- Phase 3 from `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/Plans/Aegis_Defensive_State_Persistence_And_Attribution_Plan_2026-05-19.md`.

Backtest parameters:
- `backtest-start=2016-01-01`
- `backtest-end=2026-01-01`
- `crisis-diagnostics=true`

Default parameters to leave unset:
- `pre-weak-guard-enabled`
- `weak-stress-overlay-enabled`
- `severe-crash-override-enabled`
- `sev-crash-dd-entry`
- `sev-crash-dd-exit`
- `sev-crash-recovery-wks`

Reason:
- This run should measure the current default defensive behavior after defensive state persistence, without adding temporary parameter overrides.

Suggested upload names:
- `DiagDefault_2016-2026.json`
- `DiagDefault_2016-2026_orders.csv`
- `DiagDefault_2016-2026_logs.txt`

Analysis target after upload:
- Count weeks with `PreWeakGuardActive=True`.
- Compare return/drawdown contribution during pre-weak-active versus inactive weeks.
- Identify false positives in non-crisis periods.
- Measure 4-week, 8-week, and 12-week forward behavior after pre-weak activation.
- Decide whether Phase 4 should tune pre-weak, redesign severe-crash behavior, or start universe validation.
