---
id: AEGIS-DEC-2026-06-13-RECENT-OPTIMIZATION-REVIEW
type: decision
status: proposed
date: 2026-06-13
topic: AegisGrowthAllocation
tags: [aegis, optimization, pre-weak, candidate-b, severe-crash, validation]
related:
  - project-notes/archive/2026-06/Aegis_Weak_Sleeve_Target_Parameterization_2026-06-08.md
  - project-notes/archive/2026-06/Aegis_CandidateB_Next_Optimization_Proposal_2026-06-12.md
  - project-notes/archive/2026-06/Aegis_CandidateB_2016_2026_Backtest_Analysis_2026-06-12.md
  - project-notes/archive/2026-06/Aegis_CandidateA_vs_B_2016_2026_Comparison_2026-06-12.md
  - project-notes/archive/2026-06/Aegis_AllDefensiveOff_2016_2026_Control_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_Selective07_2016_2026_Backtest_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_Selective09_2016_2026_Backtest_Analysis_2026-06-13.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs
  - Tests/Algorithm/AegisGrowthAllocationTests.cs
---

# Aegis Recent Optimization Review - 2026-06-13

## Agent Summary

The last two weeks moved Aegis from workflow/tooling cleanup into a focused weak-sleeve optimization round. Candidate B is still the best enabled defensive baseline versus Candidate A, but the all-defensive-off control shows the pre-weak guard is expensive over the full configured 2016-2026 span. The best next move is not severe-crash code work; it is a backtest-only pre-weak selectivity test with stricter run verification, explicit non-promotion guardrails, and threshold attribution before any default change.

## Multi-Agent Review Result

Three independent reviewers challenged this note before revision:

- Performance reviewer: agreed the all-defensive-off result is the key diagnostic, but argued the `0.07/0.09/0.12` grid was under-justified without threshold-band attribution. Recommendation: treat `0.12` as a boundary/control, not a likely promotion candidate, and test across the five standard windows before revisiting severe-crash work.
- Risk/live-safety reviewer: agreed with backtest-first experimentation, but objected that "parameter-only" is not automatically safe because cloud/live parameters can change allocation behavior. Recommendation: explicitly forbid live/paper/default/deployment/Object Store/order-path changes during this round and require per-run identity checks before interpreting results.
- Evidence/methodology reviewer: agreed with the direction, but flagged reproducibility gaps. Recommendation: state the actual data end date, distinguish chart-derived annual reads from exported headline metrics, require source revision/parameter/log verification, and make promotion gates numeric.

Consensus revision: keep the next move focused on pre-weak selectivity, but make it a pre-registered backtest-only experiment with stricter evidence requirements. Do not promote all-off, do not add severe-crash code yet, and do not treat the first long-span grid result as promotion evidence.

Post-review update: Selective 07 and Selective 09 were run after this review. Selective 07 improved return but failed the 2022 gate. Selective 09 moved close to all-off behavior, missed the drawdown gate by `0.1` point, and failed the 2022 gate more clearly. This means single-threshold backtests have answered the first question: threshold selectivity helps return, but threshold-only tuning is too blunt. The next move should be a bounded QuantConnect optimization over threshold plus PreWeak sleeve targets.

## Data Source Caveat

- The recent "2016-2026" runs were configured as `2016-01-01` to `2026-06-11`, but actual available result metadata ended at `2026-03-14`.
- References to "partial 2026" mean performance through the actual available 2026 data in these exports, not through `2026-06-11`.
- Headline metrics come from QuantConnect result JSON `statistics` / `totalPerformance` exports.
- Annual-return reads are lower-confidence supporting evidence because they were chart-derived from `Strategy Equity` series points, not a separately exported annual-return table.

## Recent Moves Reviewed

- QuantConnect workflow was improved through Chrome/download workflow notes and project-note reorganization, reducing friction in cloud-run capture and durable analysis.
- Weak sleeve targets were parameterized so PreWeak and Weak sleeve exposure could be tested without broad code changes.
- Candidate B was promoted on 2026-06-08 with:
  - `pre-weak-dd-threshold=0.04`
  - `pre-weak-growth-target=0.12`
  - `pre-weak-def-target=0.30`
  - `weak-growth-target=0.10`
- Candidate B beat Candidate A across the five standard windows, with aggregate net `158.823%` versus `142.318%`, average drawdown `12.94%` versus `13.46%`, and fewer orders.
- Candidate B also beat Candidate A on the long 2016-2026 run, but only slightly: net `354.365%` versus `350.119%`, same drawdown `13.7%`, and better Sharpe/PSR.
- All-defensive-off then challenged the whole pre-weak guard premise over 2016-2026: net `580.604%`, CAGR `20.676%`, Sharpe `1.102`, and PSR `84.417%`, versus Candidate B net `354.365%`, CAGR `15.991%`, Sharpe `0.885`, and PSR `65.167%`.
- Candidate B's defense was still useful in the weakest periods: all-off drawdown was `16.2%` versus Candidate B `13.7%`, all-off 2022 was `-13.96%` versus Candidate B `-9.91%`, and all-off partial 2026 was `-3.25%` versus Candidate B `-2.36%`.

## Evidence Source Table

| Run | Key Difference | Source Revision | Identity | Actual End | Artifacts |
| --- | --- | --- | --- | --- | --- |
| Candidate A long | `pre-weak-dd-threshold=0.05` | `4499311dc` | `AegisGrowthAllocation-2026-06-08-candidate-b-defaults` | `2026-03-14` | JSON/orders/logs in `BackTestLogs` |
| Candidate B long | `pre-weak-dd-threshold=0.04` | `4499311dc` | `AegisGrowthAllocation-2026-06-08-candidate-b-defaults` | `2026-03-14` | JSON/orders/logs in `BackTestLogs` |
| All defensive off long | `pre-weak-guard-enabled=false` | `4499311dc` | `AegisGrowthAllocation-2026-06-08-candidate-b-defaults` | `2026-03-14` | JSON/orders/logs in `BackTestLogs` |
| Selective 07 long | `pre-weak-dd-threshold=0.07` | `4499311dc` | `AegisGrowthAllocation-2026-06-08-candidate-b-defaults` | `2026-03-15` | JSON/orders/logs in `BackTestLogs` |
| Selective 09 long | `pre-weak-dd-threshold=0.09` | `4499311dc` | `AegisGrowthAllocation-2026-06-08-candidate-b-defaults` | `2026-03-15` | JSON/orders/logs in `BackTestLogs` |

## Interpretation

Candidate B is better than Candidate A, but Candidate A is not different enough to solve the main issue. Moving `pre-weak-dd-threshold` from `0.04` to `0.05` reduced PreWeak activations only from `127` to `114` weeks and produced nearly the same long-run return profile.

The all-defensive-off run is the stronger diagnostic. Current evidence suggests, but does not yet isolate, excessive or poorly timed PreWeak activation in markets where staying invested is rewarded. Candidate B buys roughly `2.5` points of max-drawdown reduction and better 2022 behavior, but pays for it with about `4.7` CAGR points and a very large ending-equity gap.

This weakens the case for severe-crash work as the immediate next move, but does not eliminate severe-crash work as a future candidate. Severe-crash behavior may still matter for `2007-2008`, where PreWeak activation was low and forward diagnostics were poor. The immediate evidence gap is narrower: determine whether PreWeak can be made more selective without surrendering the 2022 and partial-2026 protection.

## Best Next Move

Selective 07 and Selective 09 have now completed the manual threshold-shape check. The next move is a bounded QuantConnect optimization, still backtest-only, before changing code or defaults:

| Parameter | Min | Max | Step | Values |
| --- | ---: | ---: | ---: | ---: |
| `pre-weak-dd-threshold` | `0.05` | `0.09` | `0.01` | `5` |
| `pre-weak-growth-target` | `0.12` | `0.24` | `0.04` | `4` |
| `pre-weak-def-target` | `0.25` | `0.40` | `0.05` | `4` |

Total combinations: `5 x 4 x 4 = 80`.

Keep all other parameters fixed:

- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true` for threshold variants
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `weak-growth-target=0.10`

## QuantConnect Optimization Usage

QuantConnect cloud optimization can test up to `3` parameters at once by setting min, max, and step size values for each optimized parameter. This can compare hundreds of parameter combinations much faster than manual one-by-one backtests, but it is not free and should not be used reflexively.

Preferred optimization size is roughly `50-150` combinations. Examples: `3 x 4 x 4 = 48` is near the practical low end, while `5 x 5 x 6 = 150` is a good upper-end grid. Smaller grids may not justify using the optimizer; larger grids increase cost and parameter-mining risk unless there is a specific reason.

For the current stage, Selective 07 and Selective 09 have provided enough single-parameter evidence to justify a bounded optimization. The proposed `80`-combination grid is within the preferred `50-150` combination range.

The next question is now a genuine multi-parameter interaction: whether a more selective trigger plus less defensive PreWeak sleeve targets can recover return while preserving more of Candidate B's 2022 protection.

Before launching optimization, pre-register the objective metric, guardrail metrics, baseline comparison, validation windows, and promotion gates. Optimization results can nominate candidates only; they must be rerun as normal diagnostic backtests and validated across the standard windows before any default or live behavior change.

## Non-Promotion Guardrail

This round can nominate candidates for further validation only. It must not directly change:

- `StrategyConfig` defaults
- cloud live parameters
- paper-live parameters
- deployment identity or tags
- Object Store keys, schema, or persisted live state
- order sizing, order submission, rebalance cadence, or fill assumptions
- live/paper allocation behavior

Any default or live behavior change requires a separate decision note, independent review, restart/state safety review, and paper-live rehearsal.

## Validation Order

1. Launch the `80`-combination QuantConnect optimization over the same configured long span:
   - `backtest-start=2016-01-01`
   - `backtest-end=2026-06-11`
2. Use Sharpe or PSR as the optimizer objective.
3. Manually filter top candidates against CAGR, drawdown, 2022, partial 2026, order count, fees, and target/cash exposure.
4. Rerun the best one or two candidates as normal diagnostic backtests.
5. Promote no result from the long span or optimizer output alone. Take the best one or two candidates into the five standard windows, and include all-defensive-off in those windows if not already available for the same current code line:
   - `2007-10-01` to `2008-12-31`
   - `2009-01-01` to `2010-12-31`
   - `2019-07-01` to `2020-12-31`
   - `2021-01-01` to `2022-12-31`
   - `2023-01-01` to `2026-01-01`
6. Only after this pass decide whether code-level trigger refinement or severe-crash work is needed.

## Required Per-Run Verification

Do not interpret a new result until the following are confirmed from the exported artifacts:

- run label and intended variant
- source revision
- deployment identity / algorithm version
- configured start/end and actual available start/end
- parsed parameter log lines, especially `pre-weak-dd-threshold` and sleeve targets
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true` for threshold variants
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- JSON, orders CSV, and log file all normalized into `BackTestLogs`
- Downloads originals removed after normalization
- orders CSV checked for trailing export whitespace before staging

Current QuantConnect cloud exports in this batch report actual available data only through about `2026-03-14/15` even when the configured `backtest-end` is `2026-06-11`. Treat that actual end date as expected for cross-run comparison in this batch; do not flag it as a separate defect unless a run has a different effective data horizon.

## Required Diagnostics

Capture these for each threshold variant:

- PreWeak week count
- PreWeak weeks by year
- activation clusters and approximate entry/exit dates
- drawdown band at activation: `4-7%`, `7-9%`, `9-12%`, `12%+`
- average next 4/8/12-week returns by band or cluster if available
- 2022 and partial-2026 contribution
- recovery capture after PreWeak exit
- final growth/defensive/cash target averages
- order count, fees, turnover, and any rebalance/order bursts
- false-positive definition and count: PreWeak activations where forward 4-week return was positive enough that defensive rotation likely reduced return without material drawdown protection

## Promotion Gates

A threshold variant is interesting only if it:

- long-span CAGR is at least `18.0%`;
- long-span max drawdown is no worse than `15.5%`;
- 2022 is no worse than `-11.9%`, preserving at least half of Candidate B's observed 2022 protection versus all-off;
- partial 2026 is no worse than all-off and preferably no worse than Candidate B by more than `0.5` points;
- Sharpe and PSR improve versus Candidate B;
- orders are no more than Candidate B + `5%`;
- fees are no more than Candidate B + `10%`;
- target/cash exposure does not show unexplained cash drag outside stress clusters;
- `2007-2008` drawdown and net profit do not degrade versus Candidate B beyond a pre-declared tolerance when tested in the stress windows;
- 2009-2010 and 2019-2020 recovery behavior does not materially degrade.

## Do Not Do Yet

- Do not promote all-defensive-off directly; its 2022 and drawdown behavior are worse.
- Do not add severe-crash code yet; the current evidence points first to pre-weak activation frequency.
- Do not change defaults, live parsing, Object Store schema, deployment identity, or order handling during this parameter-only round.
- Do not run another single threshold-only backtest unless the optimization results expose a specific gap.

## Post-Optimization Update

The bounded `80`-combination optimization confirmed that the useful region is selective PreWeak activation with a more growth-heavy PreWeak sleeve:

- `pre-weak-dd-threshold`: strongest at `0.08` to `0.09`
- `pre-weak-growth-target`: strongest at `0.20` to `0.24`, with `0.24` leading
- `pre-weak-def-target`: strongest at `0.25`; `0.30` acceptable; `0.35` and `0.40` generally weaker

The two normal rerun finalists were:

| Candidate | Parameters | CAGR | Drawdown | Sharpe | PSR | 2022 Read | Decision |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| OptS09A | `0.09 / 0.24 / 0.25 / 0.10` | `20.630%` | `15.500%` | `1.113` | `85.639%` | `-13.35%` | Best headline; stress risk |
| OptS07A | `0.08 / 0.24 / 0.25 / 0.10` | `20.373%` | `15.300%` | `1.105` | `85.128%` | `-13.10%` | Slightly safer finalist |

Both beat Candidate B materially on long-span return/risk metrics, but both fail the 2022 protection gate versus Candidate B. The optimizer did its job: it nominated finalists. It did not produce a promotable default.

## First Action

OptS09A has now been tested on the `2021-2022` stress window and failed versus Candidate B:

- OptS09A: net `15.494%`, drawdown `15.500%`, Sharpe `0.512`, PSR `25.756%`
- Candidate B: net `19.808%`, drawdown `12.400%`, Sharpe `0.695`, PSR `36.392%`

OptS07A has now also been tested on the `2021-2022` stress window:

- OptS07A: net `15.701%`, drawdown `15.300%`, Sharpe `0.521`, PSR `26.236%`
- Candidate B: net `19.808%`, drawdown `12.400%`, Sharpe `0.695`, PSR `36.392%`

This means both high-growth finalists failed the stress gate. Stop this branch as a promotion path. If continuing optimization, use a narrower stress-first grid rather than more long-span/high-growth reruns.

Stress-first grid if continuing:

- `backtest-start=2021-01-01`
- `backtest-end=2022-12-31`
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `weak-growth-target=0.10`

Optimized parameters:

- `pre-weak-dd-threshold`: min `0.06`, max `0.09`, step `0.01`
- `pre-weak-growth-target`: min `0.12`, max `0.20`, step `0.04`
- `pre-weak-def-target`: min `0.25`, max `0.35`, step `0.05`

This is `36` combinations, below the usual preferred range, but intentionally narrow because the broad grid already showed that `0.24` growth is too aggressive for the stress gate.

## Risks And Open Questions

- This is still parameter mining unless the promotion gates are enforced across the five standard windows.
- A higher threshold may simply converge toward all-off and lose the 2022 protection.
- Current diagnostics summarize PreWeak weeks but do not yet separate false-positive activation clusters by market regime; code-level attribution may be needed after the parameter-only grid.
- The long-span control may overweight post-2016 bull/rebound behavior, so crisis-window evidence must remain decisive.
- Severe-crash work is deferred, not rejected; it may return as the right tool if selective PreWeak cannot protect 2007-2008 or fast selloffs.
- Local targeted NUnit execution has been unreliable due existing Lean test-host environment issues, so cloud backtest diagnostics remain the primary evidence source for this round.
