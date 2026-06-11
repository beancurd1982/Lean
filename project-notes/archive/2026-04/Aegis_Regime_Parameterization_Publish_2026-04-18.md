## 2026-04-18 - Aegis regime parameterization publish

### Task
- Commit and push the Aegis regime-parameterization and execution-realism changes.

### Files to publish
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `project-notes/Aegis_Regime_Parameterization_2026-04-18.md`
- `project-notes/Aegis_Regime_Parameterization_Publish_2026-04-18.md`

### Notes
- This publish includes:
  - the first three cloud optimization parameters,
  - runtime validation and fallback behavior,
  - the market-open guard for weekly rebalance execution.
- Local baseline artifacts in `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/` are intentionally excluded from the commit.
