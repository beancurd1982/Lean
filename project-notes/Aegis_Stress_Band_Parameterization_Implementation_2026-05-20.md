# Aegis Stress Band Parameterization Implementation - 2026-05-20

## Step 1: Scope

Date:
- 2026-05-20

Issue:
- The cloud optimizer accepted `weak-stress-threshold=32`, but the algorithm rejected it because validation requires the value to be below the fixed severe-stress threshold of 30.
- The regime model also forces `Weak` when VIX average is at or above the fixed severe-stress threshold, so simply widening the weak threshold parser would not be a valid fix.

Decision:
- Add a new runtime parameter named `severe-stress-gap`.
- Compute the severe-stress threshold as `weak-stress-threshold + severe-stress-gap`.
- Preserve current default behavior exactly:
  - `weak-stress-threshold=27`
  - `severe-stress-gap=3`
  - computed severe-stress threshold `30`

Safety requirements:
- Do not change live/default behavior when no new parameter is supplied.
- Do not silently clamp invalid combinations.
- Keep optimizer parameter names within the cloud platform limit.
- Add tests before production changes.

Planned files touched:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_Stress_Band_Parameterization_Implementation_2026-05-20.md`

## Step 2: TDD Red Check

Date:
- 2026-05-20

Tests added first:
- `UsesDefaultStressBandParameters`
- `UsesConfiguredStressBandParameters`
- `InvalidStressBandFallsBackToDefaults`
- `ComputedSevereStressAboveMaximumFallsBackToDefaults`
- `RegimeModelUsesConfiguredSevereStressThreshold`

Expected red result:
- `dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo` failed because `StrategyConfig.DefaultSevereStressThreshold` did not exist yet.

## Step 3: Implementation

Date:
- 2026-05-20

Implementation summary:
- Added `severe-stress-gap` as a runtime parameter.
- Preserved current default stress behavior:
  - weak stress threshold `27`
  - severe stress gap `3`
  - computed severe stress threshold `30`
- Replaced the fixed severe-stress threshold constant with runtime values:
  - `StrategyConfig.SevereStressGap`
  - `StrategyConfig.SevereStressThreshold`
- Updated runtime parameter configuration so `SevereStressThreshold = WeakStressThreshold + SevereStressGap`.
- Added stress-band validation:
  - `weak-stress-threshold > 18`
  - `weak-stress-threshold <= 40`
  - `severe-stress-gap` from 2 to 8
  - computed severe stress threshold must be `<= 45`
- Invalid stress-band input falls back to the default pair instead of silently clamping.
- Initialization logging now includes `SevereStressGap` and `SevereStressThreshold`.

Files changed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `Tests/Algorithm/AegisPortfolioManagerTests.cs`
- `project-notes/Aegis_Stress_Band_Parameterization_Implementation_2026-05-20.md`

## Step 4: Verification And Review

Date:
- 2026-05-20

Verification:
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo --no-restore` passed.
- `dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo` passed.
- `dotnet vstest` can discover the new tests, but targeted execution is blocked by the known local test-host issue:
  - missing `../../../Data/equity/sgx/map_files`
  - Python.NET GIL finalizer crash
- `git diff --check` passed.

Strict review:
- Default behavior is preserved when no new parameter is supplied.
- Existing optimizer value `weak-stress-threshold=32` is now valid only when paired with a valid `severe-stress-gap`.
- Severe-stress behavior remains ordered above weak stress because severe is derived from weak plus gap.
- The severe crash override can be affected only when users explicitly raise the computed severe threshold; this is expected and must be validated in crisis-window backtests before changing defaults.
- No order placement or portfolio construction logic changed directly.

Next validation:
- Rerun the optimizer with:
  - `favorable-breadth-threshold`: 0.80 to 0.90, step 0.05
  - `weak-stress-threshold`: 27 to 33, step 2
  - `severe-stress-gap`: 2 to 8, step 2

## Step 5: Commit Preparation

Date:
- 2026-05-20

Fresh verification before commit:
- `git diff --check` passed.
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo --no-restore` passed with existing package vulnerability warnings and zero errors.
- `dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo` passed with existing package vulnerability warnings and zero errors.

Commit split:
- Backtest evidence and optimizer analysis artifacts will be committed separately from code.
- Stress-band parameterization code, tests, and this implementation note will be committed as the behavior-change commit.
