## 2026-04-18 - Aegis optimization round 2 selection publish

### Task
- Publish the selected round-2 turnover-control baseline after updating the default code values.

### Files published
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `project-notes/Aegis_Optimization_Round2_Selection_2026-04-18.md`
- `project-notes/Aegis_Optimization_Round2_Selection_Publish_2026-04-18.md`

### Published defaults
- `replacement-score-gap = 10`
- `hold-stability-bonus = 2`
- `growth-atr-eligibility-limit = 0.06`

### Notes
- `replacement-score-gap = 10` and `growth-atr-eligibility-limit = 0.06` were already the active defaults before this publish step.
- The functional code change published here is the default `hold-stability-bonus` update from `5` to `2`.
- This updates the default baseline used whenever no cloud parameter overrides are supplied.
