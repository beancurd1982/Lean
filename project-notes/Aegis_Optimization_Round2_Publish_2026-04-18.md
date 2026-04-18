## 2026-04-18 - Aegis optimization round 2 parameterization publish

### Task
- Commit and push the Aegis round-2 parameterization changes.

### Files to publish
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `project-notes/Aegis_Optimization_Round2_Parameterization_2026-04-18.md`
- `project-notes/Aegis_Optimization_Round2_Publish_2026-04-18.md`

### Notes
- This publish makes `V2` the default Aegis regime baseline in code:
  - `weak-stress-threshold = 27`
  - `favorable-breadth-threshold = 0.75`
  - `upgrade-confirmation-weeks = 1`
- This publish also exposes the second optimization batch controls:
  - `replacement-score-gap`
  - `hold-stability-bonus`
  - `growth-atr-eligibility-limit`
