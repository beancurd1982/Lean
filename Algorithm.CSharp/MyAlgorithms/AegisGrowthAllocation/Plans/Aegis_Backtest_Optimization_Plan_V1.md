# Aegis Backtest And Optimization Plan V1

## Summary
This plan defines the next Aegis workstream while live deployment is blocked by account liquidation. The immediate goal is to establish a clean baseline backtest on the current pushed code, then move into controlled, low-overfitting parameter optimization in batches of three cloud parameters at a time.

The plan is deliberately staged:
- establish a trustworthy baseline first
- expose only a small number of high-value parameters
- optimize regime sensitivity before scoring or portfolio fine-tuning
- reject runs that improve one metric by damaging drawdown, turnover, or live suitability

This is intended to support a robust live-trading configuration, not to maximize backtest return at any cost.

## Phase 1 - Baseline Backtest

### Goal
Produce one clean baseline result for the current Aegis implementation before any parameterization or optimization changes.

### Required baseline artifacts
- QuantConnect `Download Results` package if available
- backtest log file
- Overview page screenshot or copied metrics
- Orders page screenshot/export
- Trades page screenshot/export
- backtest name or id
- exact date range

### Baseline metrics to capture
- Net Profit
- CAGR / Compounding Annual Return
- Sharpe
- Sortino
- PSR
- Max Drawdown
- Total Orders
- Fees
- Portfolio Turnover
- Win Rate
- Profit-Loss Ratio

### Baseline review questions
- Does the regime path look plausible through stress periods?
- Is drawdown acceptable for the intended live use?
- Is order count low enough for weekly portfolio behavior?
- Is turnover consistent with the "hold-stability-first" design?
- Are defensive shifts happening when they should?
- Are weak-regime allocations too passive or too cash-heavy?

### Deliverable
- one reviewed baseline run that becomes the comparison anchor for all later optimization batches

## Phase 2 - Parameterization For Cloud Optimization

### Goal
Expose selected Aegis constants as cloud parameters so QuantConnect optimization can search them directly.

### Scope rules
- parameterize only what materially changes regime behavior or turnover behavior
- do not parameterize too many knobs at once
- keep all defaults equal to the current hard-coded production baseline
- prefer parameters that are interpretable and defensible

### First parameter batch to expose
These three should be exposed first because they directly control regime timing and live robustness.

#### 1. `weak-stress-threshold`
- current default: `25`
- purpose: determines when stress shifts the model toward weak regime behavior
- suggested optimization range: `22` to `30`
- suggested step: `1`

#### 2. `favorable-breadth-threshold`
- current default: `0.70`
- purpose: determines how broad market participation must be before favorable classification is allowed
- suggested optimization range: `0.60` to `0.80`
- suggested step: `0.05`

#### 3. `upgrade-confirmation-weeks`
- current default: `2`
- purpose: determines how much confirmation is required before upgrading regime risk posture
- suggested optimization range: `1` to `4`
- suggested step: `1`

### Parameter implementation requirements
- parse values through `GetParameter(...)` in `AegisGrowthAllocation.cs`
- keep safe fallback to current defaults if missing or invalid
- centralize default values in `StrategyConfig.cs`
- ensure backtest and live behavior remain identical when parameters are not supplied

### Deliverable
- current Aegis code supports cloud optimization for the first three regime parameters without changing default behavior

## Phase 3 - First Optimization Batch

### Goal
Find robust regime-sensitivity settings using only the first three parameters.

### Optimization setup
- optimize exactly these three parameters together:
  - `weak-stress-threshold`
  - `favorable-breadth-threshold`
  - `upgrade-confirmation-weeks`
- use the same backtest date range as the baseline
- do not change the universe, schedule, or brokerage assumptions during this batch

### Primary optimization objective
- preferred objective: `Sharpe` or `PSR`

### Secondary acceptance filters
A run is not considered acceptable if it improves the objective but violates one or more of these constraints:
- materially worse max drawdown than baseline
- materially higher portfolio turnover than baseline
- materially higher order count than baseline
- clearly unstable regime behavior visible in logs
- suspiciously concentrated or over-reactive portfolio shifts

### Review process
For the top result set:
- compare top 5 to 10 runs, not just the single winner
- inspect whether the best region is stable or whether only one isolated point wins
- inspect logs for at least the top few runs if possible
- prefer a broad stable plateau over a fragile edge-case winner

### Deliverable
- one selected regime parameter set that beats or matches baseline on risk-adjusted quality without introducing obvious live-trading instability

## Phase 4 - Second Optimization Batch

### Goal
After regime timing is stabilized, tune turnover and replacement behavior.

### Second batch candidates
These should not be optimized until Phase 3 is complete.

#### 1. `replacement-score-gap`
- current default: `10`
- suggested range: `6` to `14`
- suggested step: `2`
- purpose: controls how much better a candidate must be before replacing an existing holding

#### 2. `hold-stability-bonus`
- current default: `5`
- suggested range: `0` to `10`
- suggested step: `2`
- purpose: rewards current holdings and reduces churn

#### 3. `growth-atr-eligibility-limit`
- current default: `0.06`
- suggested range: `0.04` to `0.08`
- suggested step: `0.01`
- purpose: controls how volatile a growth name can be before it becomes ineligible

### Evaluation focus
- turnover
- order count
- stability of selected holdings
- drawdown control during stressed periods
- whether return degradation is acceptable relative to lower churn

### Deliverable
- one turnover-focused parameter set that preserves the strategy's intended low-action behavior

## Phase 5 - What Not To Optimize Early

### Avoid in early optimization rounds
- sleeve target weights
- growth holding counts
- defensive holding counts
- individual score weights
- undeployed reserve
- universe membership

### Reason
These parameters are easier to overfit and harder to interpret cleanly. They should only be touched after regime timing and churn controls are understood.

## Acceptance Criteria
- A reviewed baseline backtest exists before optimization starts.
- First optimization batch uses only the three regime parameters.
- Default code behavior remains unchanged when no parameters are supplied.
- Optimization winners are selected using both objective metrics and behavioral review.
- Chosen settings improve robustness, not just absolute return.

## Recommended Execution Order
1. Run one clean baseline backtest on the current code.
2. Review and archive the baseline artifacts.
3. Parameterize the first three regime controls.
4. Run the first cloud optimization batch.
5. Review the result surface and select a stable winner.
6. Only then parameterize and run the second turnover-focused batch.

## Assumptions
- QuantConnect cloud optimization is limited to three parameters at a time.
- The current Aegis code is the baseline reference point.
- The user's priority is a clean and reliable live-trading candidate, not the highest possible backtest CAGR.
- Live deployment remains deferred until account liquidation is complete.
