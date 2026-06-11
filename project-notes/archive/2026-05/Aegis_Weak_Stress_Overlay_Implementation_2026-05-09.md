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

## Step 8: Overlay Backtest Upload Analysis Plan

Date:
- 2026-05-09

Request:
- Analyze uploaded overlay backtest files in `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs`.

Uploaded file convention:
- `1.json`, `1_logs.txt`, `1_orders.csv`: `2007-10-01` to `2008-12-31`
- `2.json`, `2_logs.txt`, `2_orders.csv`: `2009-01-01` to `2009-12-31`
- `3.json`, `3_logs.txt`, `3_orders.csv`: `2010-01-01` to `2010-12-31`
- `4.json`, `4_logs.txt`, `4_orders.csv`: `2019-07-01` to `2020-12-31`
- `5.json`, `5_logs.txt`, `5_orders.csv`: `2021-01-01` to `2022-12-31`

Analysis plan:
- Inventory the uploaded files.
- Parse overview JSON metrics.
- Parse `[AEGIS-DIAG]` logs for overlay behavior, drawdown, regime timing, and sleeve targets.
- Parse orders CSV for order counts and turnover clues where useful.
- Compare against prior baseline crisis diagnostics recorded in `Aegis_Crisis_Backtest_Log_Analysis_2026-05-09.md`.

## Step 9: Overlay Backtest Analysis Results

Date:
- 2026-05-09

Files analyzed:
- `1.json`, `1_logs.txt`, `1_orders.csv`
- `2.json`, `2_logs.txt`, `2_orders.csv`
- `3.json`, `3_logs.txt`, `3_orders.csv`
- `4.json`, `4_logs.txt`, `4_orders.csv`
- `5.json`, `5_logs.txt`, `5_orders.csv`

Result summary:

| Run | Window | Overlay Net | Baseline Net | Overlay DD | Baseline DD | Judgment |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 1 | 2007-10-01 to 2008-12-31 | `-11.731%` | about `-16.79%` | `15.400%` | about `18.93%` | Clear defensive improvement |
| 2 | 2009-01-01 to 2009-12-31 | `13.556%` | about `18.20%` | `2.800%` | about `3.66%` | Return drag too high |
| 3 | 2010-01-01 to 2010-12-31 | `8.082%` | about `9.30%` | `10.700%` | about `9.74%` | Slightly worse overall |
| 4 | 2019-07-01 to 2020-12-31 | `37.717%` | about `43.51%` | `10.900%` | about `10.91%` | Return drag too high |
| 5 | 2021-01-01 to 2022-12-31 | `14.204%` | about `14.35%` | `15.900%` | about `16.06%` | Nearly neutral, not enough improvement |

Detailed observations:
- Run 1, 2007-2008:
- Overlay improved ending equity by about `$1,538` versus the previous split diagnostic baseline.
- Diagnostic drawdown improved from about `18.93%` to about `13.93%`.
- Post-Weak drawdown improved from about `-11.15%` to about `-5.67%`.
- Overlay target `G0.00/D0.20/C0.80` appeared `13` weeks.
- This is the best evidence that the overlay does what it was designed to do in a severe prolonged crash.

- Run 2, 2009:
- Overlay reduced drawdown, but end equity was about `$1,070` lower than the prior split diagnostic baseline.
- Net return fell from about `18.20%` to about `13.56%`.
- This violates the recovery-preservation concern raised by the agents.

- Run 3, 2010:
- Overlay slightly reduced post-Weak drawdown but overall return and reported overview drawdown were worse.
- End equity was about `$371` lower than baseline.
- This suggests the overlay is not reliably improving shallow or whippy Weak regimes.

- Run 4, 2019-2020:
- Overlay reduced post-Weak diagnostic bleed from about `-2.48%` to about `-0.17%`.
- But total return dropped from about `43.51%` baseline diagnostic return to about `35.39%` diagnostic return, and overview net profit was `37.717%`.
- This is too much recovery drag for the small drawdown benefit.

- Run 5, 2021-2022:
- Overlay target appeared `21` weeks, but performance was almost unchanged.
- Overview drawdown improved only from about `16.5%` prior overview to `15.9%`.
- Diagnostic post-Weak drawdown improved only from about `-8.22%` to about `-7.56%`.
- The worst 2022 losses still happened before Weak activation, confirming this overlay does not solve first-leg damage.

Orders:
- Run 1: `119` filled orders.
- Run 2: `75` filled orders.
- Run 3: `179` filled orders.
- Run 4: `185` filled orders.
- Run 5: `290` filled orders.

Conclusion:
- The Weak stress overlay alone is too blunt as currently designed.
- It is useful in 2008, but it sacrifices too much 2009 and 2020 recovery and does not meaningfully improve 2022.
- Do not promote this overlay as the final defensive solution.

Recommended next step:
- Keep the parameter-gated overlay available for experimentation.
- Do not enable it by default.
- Move to Experiment A: a partial pre-Weak guard, because 2022 still shows that the largest problem is first-leg damage before Weak activation.
- If revisiting the overlay later, make it narrower than the current `StressState == Weak` trigger, likely using severe stress or confirmed equity/breadth deterioration rather than any Weak stress reading.

## Step 10: Commit Overlay Backtest Evidence

Date:
- 2026-05-09

Request:
- Commit and push the local overlay backtest uploads and analysis note before moving to Experiment A.

Commit scope:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/1.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/1_logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/1_orders.csv`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/2.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/2_logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/2_orders.csv`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/3.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/3_logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/3_orders.csv`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/4.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/4_logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/4_orders.csv`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/5.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/5_logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/5_orders.csv`
- `project-notes/Aegis_Weak_Stress_Overlay_Implementation_2026-05-09.md`

Pre-commit check:
- Run `git -c safe.directory=D:/Projects/Git/Lean-1 diff --check`.
