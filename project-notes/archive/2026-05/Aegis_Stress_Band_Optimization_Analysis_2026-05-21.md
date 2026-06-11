# Aegis Stress-Band Optimization Analysis - 2026-05-21

## Context

Source:
- Cloud optimization results shared as six screenshots, sorted by Sharpe Ratio.
- Optimization id shown in screenshots: `O-e2bb3b8bf7768c2240c5ac785c34b6dd`.

Fixed setup:
- `backtest-start=2016-01-01`
- `backtest-end=2026-01-01`
- `crisis-diagnostics=false`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `growth-atr-eligibility-limit=0.06`

Optimized parameters:
- `favorable-breadth-threshold`: 0.80 to 0.90
- `weak-stress-threshold`: 27 to 33
- `severe-stress-gap`: 2 to 8

## Findings

Best Sharpe cluster:
- Sharpe Ratio: `1.058`
- PSR: `81.051%`
- Net Profit: `484.018%`
- Drawdown: `13.6%`
- Total Orders: `1,528`
- Compounding annual return: `19.285%`
- Win Rate: `70%`
- Sortino Ratio: `1.156`
- Parameter combinations:
  - `favorable-breadth-threshold=0.90`, `weak-stress-threshold=33`, `severe-stress-gap=8`
  - `favorable-breadth-threshold=0.90`, `weak-stress-threshold=33`, `severe-stress-gap=6`
  - `favorable-breadth-threshold=0.90`, `weak-stress-threshold=33`, `severe-stress-gap=4`
  - `favorable-breadth-threshold=0.85`, `weak-stress-threshold=33`, `severe-stress-gap=8`
  - `favorable-breadth-threshold=0.85`, `weak-stress-threshold=33`, `severe-stress-gap=6`
  - `favorable-breadth-threshold=0.85`, `weak-stress-threshold=33`, `severe-stress-gap=4`

Secondary cluster:
- Sharpe Ratio: `1.036`
- PSR: `79.19%`
- Net Profit: `462.999%`
- Drawdown: `13.4%`
- Total Orders: `1,535`
- Compounding annual return: `18.849%`
- Win Rate: `70%`
- Sortino Ratio: `1.131`
- Main parameter pattern: `weak-stress-threshold=31`, `severe-stress-gap=6` or `8`, favorable breadth `0.85` or `0.90`.

Pattern observations:
- `weak-stress-threshold` is the strongest driver in this grid. Higher weak-stress thresholds, especially `33`, dominate risk-adjusted performance.
- `severe-stress-gap` has a plateau effect at the top. For `weak-stress-threshold=33`, gaps `4`, `6`, and `8` produce identical top-line results.
- `severe-stress-gap=2` is generally weaker when paired with high weak stress, suggesting an overly narrow weak-to-severe band can reduce performance.
- `favorable-breadth-threshold=0.85` and `0.90` often produce identical results in the best clusters, suggesting the algorithm may not be very sensitive above `0.85` during this period.
- `favorable-breadth-threshold=0.80` is not best by Sharpe, but with `weak-stress-threshold=33` it still produces high annual return around `19.142%` with Sharpe `1.024`.

Additional-column observations:
- The best cluster has the highest Sortino Ratio (`1.156`) as well as the highest Sharpe Ratio, so the ranking is not only driven by upside volatility.
- The best cluster has fewer orders (`1,528`) than many lower-ranked candidates, so the result is not coming from higher churn.
- The best cluster has the highest net profit shown (`484.018%`) and the highest annual return (`19.285%`), so there is no return tradeoff versus the lower-ranked combinations in this grid.
- The secondary `weak-stress-threshold=31` cluster has slightly lower drawdown (`13.4%`) but materially lower return, lower Sharpe, lower Sortino, and slightly more orders. The drawdown improvement is not large enough to justify choosing it as the broad-period candidate.

## Current Interpretation

The optimizer is favoring a less reactive stress regime:
- Let weak stress start later: `weak-stress-threshold=33`.
- Do not trigger severe stress too close to weak stress: use `severe-stress-gap=4` or higher.
- Keep the favorable-breadth threshold at `0.85` unless crisis-window validation proves `0.90` is safer.

The best default candidate from this run is:
- `favorable-breadth-threshold=0.85`
- `weak-stress-threshold=33`
- `severe-stress-gap=4`

Reason:
- It ties for best Sharpe, PSR, drawdown, and annual return.
- It is the least extreme value inside the best cluster for favorable breadth and severe stress gap.
- It avoids choosing `0.90` or gap `8` when they do not improve the objective in this optimization result.

## Risks

Open risk:
- This is still optimized on the broad 2016-2026 period. It must not become a live default until crisis-window validation confirms it does not weaken 2008, 2020, and 2021-2022 behavior.

Validation needed:
- Run the best candidate on the crisis windows and compare against current defaults.
- Confirm order count and turnover remain acceptable.
- Confirm the severe-crash override behavior is not delayed too much by the higher computed severe threshold.

## Next Validation Backtests

Decision:
- Do not save the optimized values as live defaults yet.
- First validate the best candidate out-of-sample across crisis and post-crisis windows.

Candidate parameter set:
- `favorable-breadth-threshold=0.85`
- `weak-stress-threshold=33`
- `severe-stress-gap=4`
- `growth-atr-eligibility-limit=0.06`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `crisis-diagnostics=true`

Backtest windows:
- `2007-10-01` to `2008-12-31`
- `2009-01-01` to `2010-12-31`
- `2019-07-01` to `2020-12-31`
- `2021-01-01` to `2022-12-31`
- `2023-01-01` to `2026-01-01`

Requested upload file naming:
- `OptStress_01_2007-10_2008-12.*`
- `OptStress_02_2009_2010.*`
- `OptStress_03_2019-07_2020.*`
- `OptStress_04_2021_2022.*`
- `OptStress_05_2023_2026.*`

Acceptance criteria before changing defaults:
- The candidate should preserve or improve crisis-window drawdown and recovery behavior versus the current defaults.
- The candidate should not introduce materially higher order count, fees, or turnover.
- The candidate should not reduce the defensive behavior in the worst periods enough to offset the broad-period return improvement.

## Strict Review

No code changes were made in this step.

Review result:
- No implementation risk introduced.
- The main risk is overfitting to the 2016-2026 optimizer objective before cross-period validation.
