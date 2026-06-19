# Aegis Candidate Gates

Use these gates for AegisGrowthAllocation Candidate B/C/D research until a newer decision note supersedes them.

## Current Baselines

Candidate B same-execution stress control, 2021-01-01 through 2022-12-31, Market On Close:

- Net profit: `17.612%`
- Drawdown: `12.900%`
- Sharpe: `0.609`
- PSR: `31.359%`
- Orders: `300`

Candidate C same-execution long run, 2016-01-01 through 2026-01-01, Market On Close:

- CAGR: `15.70%` vs Candidate B `15.34%`
- Net profit: `330.47%` vs Candidate B `317.19%`
- Drawdown: `14.00%` vs Candidate B `14.00%`

Candidate C same-execution stress run, 2021-01-01 through 2022-12-31, Market On Close:

- Net profit: `17.435%`
- Drawdown: `13.100%`
- Sharpe: `0.602`
- PSR: `30.955%`
- Orders: `302`

## Promotion Default

A new candidate must:

- match or beat Candidate B stress behavior, especially drawdown, Sharpe, PSR, and order count;
- improve long-window performance meaningfully versus Candidate C, not just by noise;
- use the same execution model for all comparisons;
- keep turnover, fees, and order count consistent with weekly live-trading behavior;
- have explicit rejection criteria before implementation or optimization.

Candidate C remains research evidence, not a promoted default, because its long-window edge is modest and it misses the Candidate B same-execution stress gate.
