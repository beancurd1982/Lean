## 2026-04-18 - Aegis regime parameterization and execution realism

### Task
- Add the first three cloud-optimization parameters to Aegis:
  - `weak-stress-threshold`
  - `favorable-breadth-threshold`
  - `upgrade-confirmation-weeks`
- Preserve the current hard-coded defaults when parameters are not supplied.
- Add a market-open execution guard so the weekly rebalance path does not submit orders when the market is closed.

### Files to touch
- `project-notes/Aegis_Regime_Parameterization_2026-04-18.md`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/RegimeModel.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`

### Notes
- This change affects backtest execution behavior and cloud optimization inputs.
- Intended behavior change:
  - backtests should no longer submit weekly rebalance orders when the market is closed,
  - the three regime parameters can now be controlled through QuantConnect cloud parameters,
  - default behavior remains unchanged when parameters are not supplied.

### Implementation Result
- Added runtime cloud-parameter names and defaults in `StrategyConfig.cs` for:
  - `weak-stress-threshold`
  - `favorable-breadth-threshold`
  - `upgrade-confirmation-weeks`
- Added runtime parameter configuration helpers in `StrategyConfig.cs`.
- Updated `AegisGrowthAllocation.cs` to:
  - read and validate the three regime parameters during initialization,
  - preserve the current defaults when parameters are missing or invalid,
  - log the active runtime values on startup,
  - skip weekly review if the market is closed,
  - skip individual `SetHoldings` calls when a target symbol market is closed.
- `RegimeModel.cs` required no structural logic change because it already reads the active values through `StrategyConfig`.

### Review
- Strict review completed after implementation.
- Validation method:
  - direct source inspection,
  - `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo --no-restore` with `DOTNET_CLI_HOME` redirected into the workspace.
- Build result:
  - `0 Error(s)`,
  - command still exits non-zero because of existing repository-level `NU1903` and `NU1904` warnings, not because of the Aegis changes.
- Workspace review result:
  - removed temporary `.dotnet-home/` build artifact after validation,
  - remaining untracked files are the local baseline artifacts `Logs_V1.json` and `StrategyReport_V1.pdf`,
  - modified tracked files are limited to:
    - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
    - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
    - `project-notes/Aegis_Regime_Parameterization_2026-04-18.md`
- No review findings remain blocking for the intended next step of rerunning the baseline and then the first three-parameter optimization batch.
