## 2026-04-18 - Aegis optimization round 2 parameterization

### Task
- Adopt `V2` as the default Aegis regime baseline in code.
- Expose the second optimization batch controls:
  - `replacement-score-gap`
  - `hold-stability-bonus`
  - `growth-atr-eligibility-limit`

### Files to touch
- `project-notes/Aegis_Optimization_Round2_Parameterization_2026-04-18.md`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StockSelectionModel.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PortfolioManager.cs`

### Notes
- This changes the default Aegis baseline behavior because the selected `V2` regime settings become the new defaults:
  - `weak-stress-threshold = 27`
  - `favorable-breadth-threshold = 0.75`
  - `upgrade-confirmation-weeks = 1`
- The second optimization batch will tune turnover and replacement behavior around that selected baseline.

### Implementation Result
- Updated `StrategyConfig.cs` so the default Aegis regime baseline now matches `V2`:
  - `weak-stress-threshold = 27`
  - `favorable-breadth-threshold = 0.75`
  - `upgrade-confirmation-weeks = 1`
- Added round-2 cloud parameter names and runtime-configurable defaults for:
  - `replacement-score-gap`
  - `hold-stability-bonus`
  - `growth-atr-eligibility-limit`
- Updated `AegisGrowthAllocation.cs` to parse, validate, and log those round-2 parameters alongside the regime parameters.
- Kept all round-2 fallback defaults equal to the current hard-coded baseline values:
  - `replacement-score-gap = 10`
  - `hold-stability-bonus = 5`
  - `growth-atr-eligibility-limit = 0.06`
- `StockSelectionModel.cs` and `PortfolioManager.cs` already read these values through `StrategyConfig`, so no behavioral redesign was required beyond making the values runtime-configurable.

### Review
- Strict review completed after implementation.
- Validation method:
  - direct source inspection,
  - `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo --no-restore` with `DOTNET_CLI_HOME` redirected into the workspace.
- Build result:
  - `0 Error(s)`,
  - command still exits non-zero because of existing repository-level `NU1903` and `NU1904` warnings, not because of the Aegis changes.
- Cleanup:
  - removed temporary `.dotnet-home/` build artifact after validation.
- No blocking issues found for the next step of running the second optimization batch from the selected `V2` baseline.
