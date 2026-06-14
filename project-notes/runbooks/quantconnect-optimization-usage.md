# QuantConnect Optimization Usage

## Purpose

Use QuantConnect cloud optimization deliberately when it materially improves search efficiency. Optimization is not free, so it should be reserved for cases where a broader parameter grid is justified by prior evidence and has clear decision gates.

## When To Use Cloud Optimization

Use QuantConnect optimization when all of these are true:

- The search has up to `3` parameters, matching the cloud optimization setup limit currently used in this workflow.
- Each parameter has a defensible min, max, and step size.
- The planned grid is sized around `50-150` combinations, which is the preferred balance between useful coverage and cloud cost.
- Manual one-by-one backtests would be slow or likely to miss a better interaction between parameters.
- The optimization has a pre-registered baseline, objective, fixed parameters, validation windows, and promotion gates.
- The expected value of quickly comparing many combinations is worth the cloud cost.

Good examples:

- Testing interaction between `pre-weak-dd-threshold`, `pre-weak-growth-target`, and `pre-weak-def-target`.
- Searching a bounded neighborhood after one or two manual backtests show a parameter family is promising.
- Comparing many combinations where the best result depends on interactions, not one obvious single-parameter change.
- Grid sizes such as `3 x 4 x 4 = 48` at the low end, or `5 x 5 x 6 = 150` at the high end.

Avoid grids much smaller than this unless the choices are very high-conviction. Avoid grids much larger than this unless there is a specific reason to spend more, because larger grids increase cost and parameter-mining risk.

## When To Avoid Cloud Optimization

Avoid cloud optimization when:

- Only one or two obvious candidates need to be checked.
- The purpose is simple confirmation of run identity or a single control comparison.
- The parameter family is not yet justified by evidence.
- The result would still be uninterpretable without better diagnostics.
- The run would encourage parameter mining without out-of-sample or stress-window validation.

In those cases, run targeted manual backtests first.

## Required Pre-Registration

Before launching a cloud optimization, record:

- fixed code revision / deployment identity
- configured date range and intended validation windows
- fixed parameters
- optimized parameters with min, max, and step size
- total combination count, targeting roughly `50-150`
- objective metric and secondary guardrail metrics
- baseline result for comparison
- expected output files and naming convention
- pass/fail gates for promotion or follow-up
- explicit note that optimization results nominate candidates only; they do not directly change defaults

## Aegis Current Application

For the current Aegis pre-weak selectivity work:

- Continue manual targeted checks while the question is still single-parameter threshold shape, such as `0.07` then `0.09`.
- Consider cloud optimization only after the threshold direction is confirmed and the next question becomes multi-parameter interaction, for example:
  - `pre-weak-dd-threshold`
  - `pre-weak-growth-target`
  - `pre-weak-def-target`
- Keep `weak-growth-target`, severe-crash, weak-stress overlay, order handling, Object Store, deployment identity, and live/paper parameters fixed unless separately approved.

## Post-Optimization Discipline

After optimization:

- Do not pick the top row blindly.
- Prefer robust regions over isolated winners.
- Re-run the selected candidate as a normal backtest with diagnostics enabled.
- Validate on the five standard Aegis windows before any promotion.
- Record result files and analysis in `BackTestLogs`, `log-index.csv`, and the relevant project-note index.
