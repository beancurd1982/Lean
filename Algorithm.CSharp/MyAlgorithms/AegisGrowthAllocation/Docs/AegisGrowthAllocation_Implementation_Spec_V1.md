# AegisGrowthAllocation Implementation Spec V1

## 1. Purpose

This document converts the committed Balanced live-strategy design into an implementation-ready specification for the first `AegisGrowthAllocation` prototype.

It is intended to define:

- the exact investable pools,
- the weekly operating schedule,
- the state-classification rules,
- the target allocation rules,
- the candidate scoring rules,
- the turnover-friction rules,
- the capital-deployment rules,
- and the intended module boundaries for the first C# implementation.

This is a new strategy. It should not reuse `MultiStockV33_Stable_Base.cs` as a migration base.

---

## 2. First-Version Scope

### 2.1 Strategy Type

- Live-trading oriented
- Weekly decision cadence
- Long-only
- No leverage
- No shorting
- Growth-led, drawdown-aware, moderately concentrated portfolio

### 2.2 First-Version Simplifications

- No crypto sleeve
- No options
- No intraday alpha model
- No alternative-data inputs
- No dynamic universe expansion beyond the confirmed core and supplemental pools
- No broker-specific capital-inflow automation yet; inflow handling should be designed through an explicit manual input parameter or object-store update path

---

## 3. Folder Structure

The intended first implementation layout is:

```text
Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/
    AegisGrowthAllocation.cs
    StrategyConfig.cs
    RegimeModel.cs
    StockSelectionModel.cs
    PortfolioManager.cs
    Docs/
        QuantConnect_Live_Strategy_Design_Document_V1.md
        QuantConnect_Live_Strategy_Design_Derivation_Record.md
        AegisGrowthAllocation_Implementation_Spec_V1.md
```

---

## 4. Investable Pools

### 4.1 Growth Core Pool

- `MSFT`
- `NVDA`
- `AMZN`
- `GOOGL`
- `META`
- `AVGO`
- `AAPL`
- `COST`

### 4.2 Growth Supplemental Pool

- `LLY`
- `NFLX`
- `TSLA`

### 4.3 Defensive Candidate Pool

- `SCHD`
- `VIG`
- `XLV`
- `XLU`
- `USMV`
- `SGOV`
- `JNJ`
- `PG`
- `DUK`

### 4.4 Pool Membership Rules

- The first version uses a fixed investable list.
- Core and supplemental names are all subscribed from the start.
- Defensive candidates are all subscribed from the start.
- No additional symbols are admitted in V1.
- A symbol that leaves the portfolio does not leave the investable pool.

---

## 5. Data And Indicator Requirements

### 5.1 Resolution

- Daily resolution only for V1

### 5.2 Minimum Warmup

- Warm up at least `252` trading days before allowing decisions

### 5.3 Required Indicators

For `SPY`:

- `SMA(200)`
- `SMA(200)` slope proxy using current value versus value `20` trading days ago

For each growth and defensive symbol:

- `SMA(50)`
- `SMA(200)`
- `ATR(20)`
- rolling daily closes for `21`, `63`, `126`, and `252` trading-day calculations

For stress:

- `VIX` daily close
- `5-day SMA` of `VIX`

### 5.4 History-Readiness Rule

- A symbol is eligible for ranking or holding only after all required indicators are ready
- If a symbol is not ready, it is ignored rather than estimated

---

## 6. Weekly Operating Schedule

### 6.1 Decision Time

- Evaluate once per week on `Monday 10:00 AM New York time`

Reason:

- The previous week's daily data is fully settled
- The strategy avoids reacting during the opening minutes
- The cadence remains simple and auditable

### 6.2 Weekly Sequence

1. Update the raw regime classification
2. Apply hysteresis and determine the active regime
3. Compute target sleeve allocations for growth, defensive, and cash
4. Review existing growth holdings for forced exits or trims
5. Rank eligible growth candidates
6. Decide whether any replacement is justified
7. Rank defensive candidates and set defensive sleeve targets
8. Process undeployed capital
9. Submit the minimum necessary rebalance orders

### 6.3 Non-Scheduled Trading

- No discretionary midweek optimization
- Midweek action is allowed only for forced risk exits if a held symbol becomes ineligible due to missing data or corporate-action handling issues

---

## 7. Regime Classification

### 7.1 Raw Inputs

### Broad Market Trend

Classify `SPY` as:

- `Favorable` if:
  - `SPY Close >= 1.02 * SMA200`
  - and `SMA200(current) > SMA200(20 trading days ago)`
- `Weak` if:
  - `SPY Close <= 0.98 * SMA200`
  - and `SMA200(current) <= SMA200(20 trading days ago)`
- Otherwise `Neutral`

### Breadth / Risk Appetite

Compute the percentage of all confirmed growth-pool names above their own `200-day SMA`.

Classify breadth as:

- `Favorable` if `>= 70%`
- `Weak` if `<= 40%`
- Otherwise `Neutral`

### Stress / Volatility

Use `5-day average VIX`.

Classify stress as:

- `Favorable` if `<= 18`
- `Weak` if `>= 25`
- Otherwise `Neutral`

Define `SevereStress` as:

- `VIX 5-day average >= 30`

### 7.2 Raw Regime Decision Table

Determine a weekly raw regime using the following precedence:

1. If `SevereStress` is true, raw regime = `Weak`
2. Else if `Trend = Weak` and `Breadth = Weak`, raw regime = `Weak`
3. Else if `Stress = Weak` and at least one of `Trend` or `Breadth` is not `Favorable`, raw regime = `Weak`
4. Else if `Trend = Favorable` and `Breadth = Favorable` and `Stress = Favorable`, raw regime = `Favorable`
5. Else raw regime = `Neutral`

This makes stress a cap on aggressive positioning and prevents a single positive index signal from forcing a constructive state when breadth or volatility disagrees.

### 7.3 Hysteresis Rules

- Regime movement is one step at a time only:
  - `Favorable -> Neutral -> Weak`
  - `Weak -> Neutral -> Favorable`
- Exception: if `SevereStress` is true, the active regime is forced directly to `Weak` in the current weekly cycle
- Downgrades happen after one weekly review if the raw regime is lower than the current active regime
- Upgrades require two consecutive weekly reviews with the higher raw regime
- Outside the `SevereStress` override, a direct `Weak -> Favorable` or `Favorable -> Weak` transition is not allowed in one weekly cycle

### Upgrade Counters

- Maintain a consecutive count for raw `Neutral` and raw `Favorable`
- Reset the relevant counter whenever the raw regime changes away from that target

---

## 8. Sleeve Targets

### 8.1 Active-Regime Targets

- `Favorable`: Growth `65%`, Defensive `20%`, Cash `15%`
- `Neutral`: Growth `45%`, Defensive `30%`, Cash `25%`
- `Weak`: Growth `10%`, Defensive `40%`, Cash `50%`

### 8.2 Tolerance Bands

- `Favorable`:
  - Growth `60%-70%`
  - Defensive `15%-25%`
  - Cash `10%-20%`
- `Neutral`:
  - Growth `40%-50%`
  - Defensive `25%-35%`
  - Cash `20%-30%`
- `Weak`:
  - Growth `5%-15%`
  - Defensive `35%-45%`
  - Cash `40%-55%`

### 8.3 Rebalance Trigger Rule

- Rebalance only when a sleeve is outside its tolerance band
- Or when a position-level forced exit or justified replacement occurs
- Or when undeployed capital is released

---

## 9. Growth-Sleeve Construction

### 9.1 Target Number Of Growth Holdings

- `Favorable`: target `6` growth names
- `Neutral`: target `4` growth names
- `Weak`: target `1` growth name

### 9.2 Base Eligibility Rules

A growth symbol is eligible only if all of the following are true:

- `Close > SMA200`
- `SMA50 >= SMA200`
- `63-day return > 0`
- `ATR20 / Close <= 0.06`
- data is ready

If fewer symbols satisfy all rules:

- use the eligible subset only
- do not relax the rules to force full deployment

### 9.3 Growth Scoring Formula

Each eligible growth symbol receives a composite score from `0` to `100`.

#### Trend Quality: 45 points

- `20` points: distance above `SMA200`, capped at `+15%`
- `15` points: distance of `SMA50` above `SMA200`, capped at `+10%`
- `10` points: positive `21-day return`, scaled and capped

#### Relative Strength: 35 points

- `20` points: percentile rank of `126-day return` within the growth pool
- `15` points: percentile rank of `63-day return` within the growth pool

#### Stability / Holdability: 20 points

- `10` points: lower `63-day realized volatility` is better, scored by inverse rank within the eligible pool
- `10` points: smaller `63-day peak-to-trough drawdown` is better, scored by inverse rank within the eligible pool

### 9.4 Risk Penalties

Subtract penalties after the base score:

- `-10` points if `ATR20 / Close > 0.05`
- `-10` points if price is more than `20%` above `SMA200`
- `-5` points if `21-day return < 0`

Final score floor:

- minimum `0`

### 9.5 Selection Rule

- Rank all eligible growth symbols by final score descending
- Existing valid holdings receive a `+5` hold-stability bonus before final ranking comparison
- Use the top names up to the active regime's target holding count, subject to replacement-friction rules

---

## 10. Growth Position Sizing

### 10.1 Position Caps

- Hard cap for any single growth name: `12%` of total portfolio value

### 10.2 Default Growth Weights

Assign equal weight within the growth sleeve, then cap at `12%`.

That yields:

- `Favorable`: about `10.83%` each across `6` names
- `Neutral`: about `11.25%` each across `4` names
- `Weak`: growth sleeve target is `10%`, so the single retained growth name targets `10%`

### 10.3 Cap Handling

- If equal-weight sizing exceeds `12%`, clamp the position at `12%`
- Any leftover growth-sleeve allocation stays in cash for V1 rather than being redistributed aggressively

---

## 11. Defensive-Sleeve Construction

### 11.1 Defensive Eligibility

A defensive candidate is eligible if:

- data is ready
- and either:
  - `Close > SMA200`
  - or the symbol is `SGOV`

### 11.2 Defensive Ranking

Eligible defensive symbols are ranked using:

- `50%` by `126-day return` percentile
- `30%` by inverse `63-day realized volatility` percentile
- `20%` by inverse `63-day drawdown` percentile

### 11.3 Number Of Defensive Holdings

- `Favorable`: hold top `2`
- `Neutral`: hold top `3`
- `Weak`: hold top `3`

### 11.4 Defensive Weighting

- Equal-weight the selected defensive holdings within the defensive sleeve target
- If fewer than the target number qualify, allocate only to the qualified subset
- Any unused defensive sleeve target stays in cash

### 11.5 SGOV Treatment

- `SGOV` is allowed as part of the defensive sleeve
- Raw cash remains separate and is not replaced by `SGOV`

---

## 12. Replacement And Turnover Friction

### 12.1 Forced Exit Conditions

An existing growth holding is force-exited if any of the following becomes true:

- `Close < SMA200`
- `SMA50 < SMA200`
- `ATR20 / Close > 0.07`
- data is not ready or is invalid

### 12.2 Optimization Replacement Conditions

A new growth candidate may replace an existing non-forced holding only if:

- the portfolio is not currently over its growth-sleeve target
- the new candidate is eligible
- the existing holding is still eligible but ranked lower
- and the new candidate's final score exceeds the existing holding's final score by at least `10` points

### 12.3 Weekly Replacement Caps

- Maximum forced-exit replacements per week: no hard cap if needed for safety
- Maximum optimization-driven replacements per week: `1`
- Maximum total new growth entries per week from ranking-driven changes: `2`

### 12.4 Trim-Only Situations

If the regime is downgraded and no holding is force-invalid:

- trim growth exposure to the new sleeve target first
- do not replace names unless a candidate clears the explicit superiority threshold

### 12.5 Portfolio-Level Priority

Priority order:

1. honor the active regime sleeve targets
2. process forced exits
3. process justified optimization replacements
4. process undeployed-capital release

---

## 13. Undeployed Capital Module

### 13.1 Representation

Track a decimal `UndeployedCapitalReserve`.

- It represents new capital that has arrived but is not yet authorized for deployment
- It is part of account cash economically, but not considered automatically deployable

### 13.2 First-Version Input Method

V1 should support one of these explicit inputs:

- a manual algorithm parameter for reserve amount
- or an object-store value updated outside the strategy

The implementation should not guess deposits from brokerage cash changes in V1.

### 13.3 Weekly Release Rates

- `Favorable`: release up to `33%` of current reserve in one weekly cycle
- `Neutral`: release up to `15%` of current reserve in one weekly cycle
- `Weak`: release `0%`

### 13.4 Release Preconditions

Reserve capital may be released only if:

- the active regime allows a positive release rate
- and at least one sleeve is below its target

### 13.5 Release Priority

Apply released reserve in this order:

1. top up underweight existing growth holdings that remain selected
2. fund newly approved growth entries
3. fund underweight defensive positions
4. keep any remainder as cash

---

## 14. Order Handling Rules

### 14.1 Order Style

- Use straightforward market orders during the weekly rebalance event for V1

### 14.2 Execution Principle

- Submit the minimum necessary orders
- Avoid full liquidation and rebuild unless required by a regime step-down or eligibility failure

### 14.3 Small-Trade Filter

- Skip any target change smaller than `0.5%` of total portfolio value

---

## 15. Module Responsibilities

### 15.1 `AegisGrowthAllocation.cs`

- `QCAlgorithm` entry point
- security setup
- indicator wiring
- schedule registration
- coordination across modules
- order submission

### 15.2 `StrategyConfig.cs`

- symbol lists
- thresholds
- target allocations
- replacement caps
- release rates
- feature toggles

### 15.3 `RegimeModel.cs`

- raw input evaluation
- raw regime classification
- hysteresis state machine
- active regime output

### 15.4 `StockSelectionModel.cs`

- eligibility filters
- score calculations
- ranking
- hold-stability bonus
- defensive ranking

### 15.5 `PortfolioManager.cs`

- sleeve target construction
- position targeting
- trim / replace decisions
- undeployed-capital release handling
- rebalance instruction generation

---

## 16. Explicit Non-Goals For V1

- No intraday stop logic
- No volatility-targeted leverage adjustment
- No dynamic expansion of the stock universe
- No factor-model optimization
- No tax-aware lot logic
- No broker-event inference for deposit detection

---

## 17. Open Validation Items Before Coding

These items are now narrowed to implementation-validation work, not open-ended architecture work:

- Verify that the chosen `VIX` data source is available and reliable for the intended QuantConnect deployment mode
- Confirm whether `Monday 10:00 AM` is the preferred live rebalance time or whether `Friday near close` is preferred operationally
- Confirm whether `TSLA` should remain supplemental or be promoted into the core pool
- Validate that the defensive candidate set does not create unintended sector concentration in weak regimes
- Backtest the proposed raw regime thresholds and replacement-score gap before treating them as final

---

## 18. Immediate Next Coding Step

The next coding step should be:

1. scaffold the five C# files,
2. implement `StrategyConfig` and `RegimeModel` first,
3. then implement `StockSelectionModel`,
4. then implement `PortfolioManager`,
5. then wire everything together in `AegisGrowthAllocation.cs`.

That order keeps the highest-impact state and allocation logic explicit before order-generation code is written.
