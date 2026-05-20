# Aegis Optimization Wizard First Pages Analysis - 2026-05-20

## Step 1: Screenshot Intake

Date:
- 2026-05-20

Scope:
- Review the first four optimization-result pages, sorted by Sharpe Ratio.

Optimization ID:
- `O-ab7052db86f4ef310ed5ebd3d41a79c3`

Parameters optimized:
- `favorable-breadth-threshold`
- `weak-stress-threshold`
- `growth-atr-eligibility-limit`

Observed top result:
- Sharpe Ratio: 1.029
- PSR: 78.953%
- Drawdown: 13.7%
- CAGR: 18.561%
- `favorable-breadth-threshold=0.85`
- `weak-stress-threshold=30` or `32`
- `growth-atr-eligibility-limit=0.06` or `0.08`

Initial interpretation:
- Top-ranked results cluster at high favorable breadth threshold and high weak stress threshold.
- `growth-atr-eligibility-limit` appears insensitive above `0.06` in the visible top results.
- The best visible Sharpe improvement is modest versus current default, but drawdown remains near or below the current default.

## Step 2: Full Sharpe-Sorted Grid Review

Date:
- 2026-05-20

Additional screenshots reviewed:
- Pages 5 through 9 of the same optimization result table, still sorted by Sharpe Ratio.

Full-grid pattern:
- `favorable-breadth-threshold=0.85` dominates the top Sharpe results.
- `favorable-breadth-threshold=0.70` dominates the bottom Sharpe results and should be rejected.
- `weak-stress-threshold=30` and `32` are strong at the top when paired with `favorable-breadth-threshold=0.85`.
- `weak-stress-threshold=24` is generally weak, especially when not paired with the best breadth setting.
- `growth-atr-eligibility-limit=0.06` and `0.08` frequently tie in the best rows.
- `growth-atr-eligibility-limit=0.07` performs poorly across many rows and should be rejected.
- `growth-atr-eligibility-limit=0.05` sometimes improves drawdown but often lowers CAGR and Sharpe.

Best visible cluster:
- Sharpe Ratio: 1.029
- PSR: 78.953%
- Drawdown: 13.7%
- CAGR: 18.561%
- `favorable-breadth-threshold=0.85`
- `weak-stress-threshold=30` or `32`
- `growth-atr-eligibility-limit=0.06` or `0.08`

Strong alternative:
- Sharpe Ratio: 1.017
- Drawdown: 13.5%
- CAGR: 18.573%
- `favorable-breadth-threshold=0.75` or `0.80`
- `weak-stress-threshold=28`
- `growth-atr-eligibility-limit=0.05`

Interpretation:
- The optimizer is not saying the algorithm should be broadly more aggressive. It is saying aggressive/favorable mode should require stronger breadth confirmation.
- A higher weak-stress threshold appears beneficial in this grid, meaning the current weak-stress signal can be too sensitive.
- ATR eligibility is not the main driver, and the unstable `0.07` result pattern suggests not overfitting this parameter.

Recommendation:
- Preferred candidate default: `favorable-breadth-threshold=0.85`, `weak-stress-threshold=32`, `growth-atr-eligibility-limit=0.06`.
- Conservative alternative for validation: `favorable-breadth-threshold=0.80`, `weak-stress-threshold=28`, `growth-atr-eligibility-limit=0.05`.

Next validation:
- Before changing defaults, run focused out-of-sample/crisis validation for the preferred candidate against current default and no-pre-weak baseline.
- Do not select the winner solely from full-period Sharpe because the grid was optimized on the same 2016-2026 period.
