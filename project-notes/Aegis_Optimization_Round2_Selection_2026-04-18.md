## 2026-04-18 - Aegis optimization round 2 selection

### Task
- Record the selected round-2 optimization winner and update code defaults accordingly.

### Selected values
- `replacement-score-gap = 10`
- `hold-stability-bonus = 2`
- `growth-atr-eligibility-limit = 0.06`

### Files to touch
- `project-notes/Aegis_Optimization_Round2_Selection_2026-04-18.md`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`

### Notes
- `replacement-score-gap = 10` and `growth-atr-eligibility-limit = 0.06` are already the current defaults.
- The effective code change in this step is to update `hold-stability-bonus` from `5` to `2`.
- This changes the default backtest/live baseline when no cloud override is supplied.

### Review
- Strict review completed after updating the selected default.
- Validation method:
  - direct source inspection,
  - `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo --no-restore` with `DOTNET_CLI_HOME` redirected into the workspace.
- Build result:
  - `0 Error(s)`,
  - command still exits non-zero because of existing repository-level `NU1903` and `NU1904` warnings, not because of the Aegis change.
- Cleanup:
  - removed temporary `.dotnet-home/` build artifact after validation.
- Result:
  - the default turnover-control baseline now matches the selected round-2 winner:
    - `replacement-score-gap = 10`
    - `hold-stability-bonus = 2`
    - `growth-atr-eligibility-limit = 0.06`
