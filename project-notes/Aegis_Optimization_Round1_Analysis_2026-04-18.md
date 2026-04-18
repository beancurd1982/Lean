## 2026-04-18 - Aegis optimization round 1 analysis

### Task
- Analyze the uploaded Aegis baseline and confirmation backtest artifacts in `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/`.

### Files reviewed
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Logs_V1.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Logs_V2.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Logs_V3.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Logs_V4.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/StrategyReport_V1.pdf`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/StrategyReport_V2.pdf`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/StrategyReport_V3.pdf`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/StrategyReport_V4.pdf`

### Parameter mappings
- `V2`: `weak-stress-threshold=27`, `favorable-breadth-threshold=0.75`, `upgrade-confirmation-weeks=1`
- `V3`: `weak-stress-threshold=27`, `favorable-breadth-threshold=0.75`, `upgrade-confirmation-weeks=2`
- `V4`: `weak-stress-threshold=22`, `favorable-breadth-threshold=0.80`, `upgrade-confirmation-weeks=2`

### Summary findings
- `V2` is the strongest by Sharpe and total return versus the current baseline.
- `V3` is the strongest balanced candidate if lower drawdown is preferred over the small Sharpe edge of `V2`.
- `V4` is defensively cleaner on drawdown but gives up too much return and Sharpe relative to `V2` and `V3`.
- The first-order execution realism issue improved from `V1` to `V2/V3/V4` because the first orders no longer occur on `2018-01-01`, but QuantConnect still flags extended-hours fills in all runs.

### Key metrics
- `V1`: Sharpe `0.803`, Net Profit `242.351%`, Drawdown `14.900%`, Turnover `3.12%`, Orders `1323`
- `V2`: Sharpe `0.832`, Net Profit `256.538%`, Drawdown `14.900%`, Turnover `2.95%`, Orders `1241`
- `V3`: Sharpe `0.802`, Net Profit `240.714%`, Drawdown `13.700%`, Turnover `2.86%`, Orders `1196`
- `V4`: Sharpe `0.786`, Net Profit `227.586%`, Drawdown `13.500%`, Turnover `2.91%`, Orders `1189`

### Interpretation
- `V2` is not just a nominal Sharpe winner; it also improves net profit, reduces turnover, and reduces order count versus `V1`.
- `V3` gives up a small amount of Sharpe and return for better drawdown control, making it the conservative alternative.
- `V4` does not justify its lower performance unless the strategy objective is changed to prioritize drawdown more aggressively.

### Recommended next step
- Treat `V2` as the provisional lead candidate.
- Treat `V3` as the control candidate for robustness comparison.
- Do not continue broad regime optimization yet.
- Next work should focus on either:
  - selecting `V2` or `V3` as the regime baseline, then
  - running a second optimization batch on turnover / replacement controls, or
  - addressing the remaining extended-hours fill flag if execution realism is prioritized before further optimization.

### Review
- Strict review completed for this analysis note.
- No code was changed in this step.
