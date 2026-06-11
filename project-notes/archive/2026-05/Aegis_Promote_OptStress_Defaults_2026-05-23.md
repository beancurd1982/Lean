# Aegis Promote OptStress Defaults - 2026-05-23

## Step 1: Scope

User approved promoting the validated OptStress candidate values as defaults.

This is a live-trading behavior change because these values apply when no cloud/platform parameters override them.

Approved default values:
- `DefaultFavorableBreadthThreshold = 0.85`
- `DefaultWeakStressThreshold = 33`
- `DefaultSevereStressGap = 4`
- Computed `DefaultSevereStressThreshold = 37`
- `DefaultGrowthAtrEligibilityLimit = 0.06` remains unchanged.

Existing defaults intentionally unchanged:
- `DefaultPreWeakGuardEnabled = true`
- `weak-stress-overlay-enabled` default remains false in algorithm parameter parsing.
- `severe-crash-override-enabled` default remains false in algorithm parameter parsing.

Plan:
- Add a test that locks the promoted defaults.
- Verify the test fails before production code changes.
- Update `StrategyConfig`.
- Run builds and review.

## Step 2: TDD Red Check

Test added:
- `UsesPromotedOptStressDefaultParameters`

Local test-run blocker:
- `dotnet test Tests/QuantConnect.Tests.csproj --filter UsesPromotedOptStressDefaultParameters --no-build -nologo` aborted before assertion output because of the known local test-host issue:
  - `GIL must always be released`
  - missing `../../../Data/equity/sgx/map_files`

Fallback red signal:
- A static constant check failed before production changes:
  - `DefaultFavorableBreadthThreshold`: expected `0.85`, got `0.75`
  - `DefaultWeakStressThreshold`: expected `33`, got `27`
  - `DefaultSevereStressGap`: expected `4`, got `3`

## Step 3: Implementation

Files changed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`

Implementation:
- Set `DefaultFavorableBreadthThreshold` to `0.85m`.
- Set `DefaultWeakStressThreshold` to `33m`.
- Set `DefaultSevereStressGap` to `4m`.
- `DefaultSevereStressThreshold` remains computed as weak threshold plus gap, so it is now `37m`.
- Added `UsesPromotedOptStressDefaultParameters` to lock the promoted defaults.

Live behavior impact:
- New live deployments without explicit overrides will use the optimized stress-band defaults.
- The algorithm will classify weak stress later than before (`33` instead of `27`).
- Severe stress threshold is now higher (`37` instead of `30`) because it is computed from the promoted weak threshold plus gap.
- Strict severe-crash override remains disabled by default.
- Weak-stress overlay remains disabled by default.
- Pre-weak guard remains enabled by default.

## Step 4: Verification And Review

Verification:
- Static default check passed after the implementation.
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo --no-restore -v:minimal /clp:ErrorsOnly` passed with zero errors.
- `dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo -v:minimal /clp:ErrorsOnly` passed with zero errors.
- `git diff --check` passed.
- `AegisGrowthAllocation.cs` remains `62,014` characters, below the cloud upload limit.

Targeted test status:
- `dotnet test Tests/QuantConnect.Tests.csproj --filter UsesPromotedOptStressDefaultParameters --no-build -nologo` still aborts due to the known local test-host issue:
  - missing `../../../Data/equity/sgx/map_files`
  - Python.NET GIL finalizer crash
- No assertion failure was observed because the host aborts before reporting the test result.

Strict review:
- The change is intentionally limited to default constants and a default-lock test.
- No order placement, portfolio execution, persistence, restart, or live state code changed.
- Risk remains that 2007-2008 drawdown is not improved by these defaults; this was accepted based on the validation analysis.
- Cloud/platform users can still override all promoted values through parameters.
