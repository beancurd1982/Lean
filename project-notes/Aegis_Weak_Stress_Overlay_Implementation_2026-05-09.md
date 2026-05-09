# Aegis Weak Stress Overlay Implementation - 2026-05-09

## Step 1: Intake And Scope

Date:
- 2026-05-09

Request:
- After committing and pushing the crisis log analysis, proceed with the implementation plan.

Approved implementation scope:
- Start with the safer experiment: a parameter-gated Weak-plus-stress overlay.
- Default behavior must remain unchanged.
- The overlay is intended for backtest experimentation first, not live trading behavior.

Planned parameter:
- `weak-stress-overlay-enabled`

Safety constraints:
- Default must be `false`.
- Live mode should ignore the experimental overlay.
- Existing production allocation logic should remain unchanged unless the parameter is enabled in backtests.
- Add focused tests before production code changes.

Initial implementation hypothesis:
- When the active regime is already Weak and stress is severe or deterioration persists, override the Weak target sleeve toward more cash and less equity-like defensive exposure.
- Do not remove the existing recovery behavior globally.

Files expected to review:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`

Open questions:
- Exact overlay thresholds should be chosen conservatively after reading the current portfolio construction code.

## Step 2: Design After Code Review

Date:
- 2026-05-09

Code reviewed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PortfolioManager.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/RegimeModel.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`

Design decision:
- Keep the overlay outside `RegimeModel`.
- Add an optional sleeve-target override to `PortfolioManager.BuildPlan`.
- Only pass the override from `AegisGrowthAllocation` when the new backtest-only parameter is enabled and the current weekly regime snapshot is already Weak with stress deterioration.

Initial overlay target:
- Growth `0.00`
- Defensive `0.20`
- Cash `0.80`

Overlay activation condition:
- Active regime is `Weak`.
- And one of:
- `SevereStress` is true.
- `StressState` is Weak.
- Both `TrendState` and `BreadthState` are Weak.

Reasoning:
- This is narrower than permanent high cash and should preserve normal Weak recovery behavior.
- It directly targets the post-Weak bleed found in 2008, 2010, and 2022.

Test-first plan:
- Add a PortfolioManager test proving default Weak behavior remains Growth `0.10`, Defensive `0.40`.
- Add a PortfolioManager test proving an override target can drive Growth `0.00`, Defensive `0.20`, Cash `0.80`.
- Add algorithm parameter tests proving `weak-stress-overlay-enabled` is default false, enabled by `true`, and invalid values fall back false.

## Step 3: Red Test Attempt

Date:
- 2026-05-09

Files touched:
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_Weak_Stress_Overlay_Implementation_2026-05-09.md`

Tests added:
- Default `weak-stress-overlay-enabled` parameter is disabled.
- `weak-stress-overlay-enabled=true` enables the experiment.
- Invalid `weak-stress-overlay-enabled` falls back to disabled.
- PortfolioManager keeps default Weak sleeves without an override.
- PortfolioManager applies Weak stress overlay sleeves when an override is supplied.

Red verification attempt:
- `dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`

Result:
- Timed out before returning compile output.
- Expected red references in the test source are `_weakStressOverlayEnabled`, `StrategyConfig.WeakStressOverlaySleeveTargets`, and an additional `PortfolioManager.BuildPlan` argument.

Next action:
- Implement the minimal production changes needed to satisfy the tests while preserving default behavior.

## Step 4: Implementation

Date:
- 2026-05-09

Files changed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PortfolioManager.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_Weak_Stress_Overlay_Implementation_2026-05-09.md`

Implementation details:
- Added `weak-stress-overlay-enabled` parameter constant.
- Added `WeakStressOverlaySleeveTargets` with Growth `0.00`, Defensive `0.20`, Cash `0.80`.
- Added `_weakStressOverlayEnabled`, parsed only in non-live initialization.
- Added `ShouldApplyWeakStressOverlay` with activation only when:
- parameter is enabled,
- active regime is Weak,
- and severe stress, Weak stress, or both Weak trend and Weak breadth are present.
- Added optional `sleeveTargetsOverride` to `PortfolioManager.BuildPlan`.
- Suppressed selected growth symbols when the active sleeve target has zero growth allocation.

Behavioral safety:
- Default behavior remains unchanged because the new parameter defaults to `false`.
- Live mode ignores the experimental parameter because it is parsed only in `!LiveMode`.

## Step 5: Verification

Date:
- 2026-05-09

Commands:
- `git -c safe.directory=D:/Projects/Git/Lean-1 diff --check`
- `rg -n "weak-stress-overlay|WeakStressOverlay|_weakStressOverlayEnabled|ShouldApplyWeakStressOverlay|sleeveTargetsOverride" Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj --no-restore -c Debug -nologo -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`
- `dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`
- `dotnet test Tests/QuantConnect.Tests.csproj --no-restore --filter "FullyQualifiedName~QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests" -nologo -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`

Results:
- `git diff --check` completed with exit code `0`, reporting only LF-to-CRLF normalization warnings.
- Symbol search confirmed the new parameter, field, activation helper, overlay targets, and PortfolioManager override path.
- Algorithm project build passed with `0` errors and existing repository warnings.
- Test-project build attempt timed out after three minutes.
- Focused Aegis test command rebuilt enough to run and passed: `13` passed, `0` failed, `0` skipped.
- Test output included existing NuGet vulnerability warnings and one transient file-copy retry warning because a test-platform DLL was in use.

Verification conclusion:
- The focused Aegis tests passed.
- The algorithm project compiles.
- The full test-project build remains too slow/unreliable in this environment, but the focused test command completed successfully.

## Step 6: Strict Code Review

Date:
- 2026-05-09

Review scope:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PortfolioManager.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`

Review findings:
- Default behavior is unchanged because `weak-stress-overlay-enabled` defaults to `false`.
- Live behavior is unchanged because the parameter is parsed only in the non-live initialization branch.
- The regime model is unchanged; the overlay does not alter regime classification or upgrade/downgrade state.
- The overlay is applied only as a sleeve-target override at portfolio-construction time.
- `PortfolioManager.BuildPlan` uses the standard sleeve targets unless an override is explicitly provided.
- When the overlay target has Growth `0.00`, selected growth symbols are suppressed, so diagnostics and targets do not imply an intentional growth sleeve.
- Existing reserve-release behavior remains tied to the active regime and stays zero in Weak.
- Tests cover parameter parsing and the default-vs-overlay sleeve target behavior.

Residual risks:
- The overlay target values are an experiment and require QuantConnect backtests before any performance conclusion.
- The overlay still allows a `20%` defensive sleeve, and that sleeve is not yet filtered to cash-like assets only.
- The activation rule is intentionally conservative but may still need tuning after A/B crisis-window results.

Review result:
- No code-level safety or correctness issues found.
- The change is suitable for backtest experimentation, not for live enablement until backtest evidence is reviewed.

## Step 7: Commit And Push Plan

Date:
- 2026-05-09

Request:
- Commit and push the Weak stress overlay implementation.

Commit scope:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PortfolioManager.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_Weak_Stress_Overlay_Implementation_2026-05-09.md`

Pre-commit verification already completed:
- Algorithm project build passed with `0` errors.
- Focused Aegis tests passed: `13` passed, `0` failed.
- `git diff --check` passed with only LF-to-CRLF warnings.
