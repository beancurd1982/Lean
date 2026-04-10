# QuantConnect Live Strategy Design Document V1

---

## 1. Strategy Objective

This strategy is designed for live trading on the QuantConnect platform. Its goal is to pursue strong annualized returns and a solid Sharpe Ratio over the long run while keeping maximum portfolio drawdown under control as much as possible.

### Core Objectives
- Pursue a balance among **annualized return, maximum drawdown, and Sharpe Ratio**
- Target a maximum drawdown of roughly **15% or less**
- Ensure the strategy is executable and maintainable in live trading
- Support **semiannual capital inflows**, with the added amount not fixed in advance

### Non-Objectives
- Do not pursue high-frequency trading
- Do not pursue extremely aggressive return targets
- Do not use pure passive buy-and-hold
- Do not chase short-term thematic momentum
- Do not short
- Do not use leverage

---

## 2. Summary of User Preferences

Based on the requirements gathered, the strategy should align with the following preferences:

- The main trading assets should be **U.S. growth stocks**
- The strategy should also include **high-dividend / defensive assets**
- The first implementation should **not include a crypto sleeve**, in order to keep the system simpler and more auditable
- Trading style should be **medium- to low-frequency**
- The main decision cadence should be **once per week**
- Rebalancing should be **kept as light as possible**
- The portfolio should be **moderately concentrated**
- The total number of holdings should be about **8-12**
- Each individual growth stock should have a maximum weight of about **10%-12%**
- In poor environments, defensive assets may rise materially and cash may become the largest buffer
- New capital does not need to be deployed immediately after arrival; holding cash first is acceptable

---

## 3. Overall Strategy Architecture

This strategy now uses a four-layer structure for the first implementation:

1. **Core Growth Stock Layer**
2. **Defensive Layer**
3. **Risk State Layer**
4. **New Capital Deployment Layer**

### 3.1 Core Growth Stock Layer
This is the main return driver. The core assets are high-quality mega-cap technology growth stocks and large-cap growth stocks, held primarily in individual stock form.

### 3.2 Defensive Layer
This layer consists of defensive assets plus cash. It is used to absorb exposure, reduce volatility, and control drawdown in neutral and weak risk environments.

### 3.3 Risk State Layer
This layer determines whether the portfolio should lean more offensive or more defensive and sets the overall framework for high-level allocations.

### 3.4 New Capital Deployment Layer
This layer handles semiannual capital inflows of varying size and decides whether new capital should be deployed immediately, deployed gradually, or temporarily kept in cash.

---

## 4. Asset Layer Design

### 4.1 Role of the Growth Stock Layer
The growth stock layer should be defined as follows:

- Centered on **high-quality mega-cap technology growth stocks**
- Supplemented by a small number of **high-quality large-cap growth stocks**
- Emphasizes liquidity, long holding suitability, and stable trend behavior
- Does not use high-volatility small- and mid-cap growth stocks as the primary allocation universe in the first version

### 4.2 Role of the Defensive Layer
The defensive layer is not the main return engine. Instead, it is used to:

- Absorb portfolio weight when the risk/reward ratio deteriorates
- Reduce overall portfolio volatility
- Preserve dry powder for future redeployment

The defensive layer consists of two parts:

- **Defensive asset sleeve**: high-dividend or otherwise steadier defensive assets
- **Cash sleeve**: the final buffer layer

### 4.3 First-Version Simplification
The earlier concept allowed for a small crypto enhancement sleeve, but the first implementation should deliberately exclude it.

Reason:
- Reduce moving parts in the first live-tradable version
- Keep the strategy easier to explain and review
- Concentrate the first implementation on the interaction among growth, defense, cash, and regime logic

---

## 5. Three-State Risk Regime System

The strategy uses three risk states:

- **Favorable**
- **Neutral**
- **Weak**

The purpose of the risk state is not to predict the market. Its role is to determine how much risk the portfolio should carry at the current time.

### 5.1 Favorable State
Meaning: the current environment supports taking relatively high risk.

Portfolio intent:
- Growth stocks serve as the primary exposure
- The defensive layer is light
- Cash is kept intentionally low, but not eliminated

### 5.2 Neutral State
Meaning: the risk/reward ratio has deteriorated, but not enough to justify a full retreat.

Portfolio intent:
- Reduce part of the growth exposure
- Increase the defensive layer
- Keep a meaningful cash buffer
- Deploy new capital more cautiously

### 5.3 Weak State
Meaning: the current environment is not suitable for maintaining high growth exposure, so the priority should shift toward drawdown control and cash preservation.

Portfolio intent:
- Reduce growth exposure materially
- Raise both defensive assets and cash
- Prioritize waiting rather than deploying new capital

### 5.4 Inputs to the Risk State
The first version of the risk state should be determined jointly by three explicit input groups:

- **Broad market trend condition**:
  - `SPY` relative to its `200-day SMA`
  - slope direction of the `200-day SMA`
- **Market breadth / risk appetite condition**:
  - percentage of confirmed growth-pool names above their own `200-day SMA`
- **Stress / volatility condition**:
  - `5-day average VIX`

### 5.5 Principles for State Transitions
- Downgrades may happen faster than upgrades
- One noisy week should not trigger a major state change
- State changes should move one step at a time:
  - Favorable -> Neutral -> Weak
  - Weak -> Neutral -> Favorable
- Downgrades may occur after **1** weekly review if the lower state is decisively triggered
- Upgrades require **2 consecutive** weekly reviews satisfying the higher state's conditions
- Upgrades should require stronger threshold buffers than downgrades
- Elevated stress should be able to block `Favorable`
- Severe stress should be able to force `Weak`

---

## 6. High-Level Allocation Framework

The first version should use explicit target weights with narrow tolerances rather than very broad discretionary ranges.

### 6.1 Favorable State
- Growth stocks target: **65%**
- Defensive assets target: **20%**
- Cash target: **15%**
- Tolerance guide:
  - Growth stocks: **60% - 70%**
  - Defensive assets: **15% - 25%**
  - Cash: **10% - 20%**

Characteristics:
- The portfolio leans constructive, but remains balanced
- Growth is clearly the main engine
- The defensive layer and cash still remain meaningful stabilizers

### 6.2 Neutral State
- Growth stocks target: **45%**
- Defensive assets target: **30%**
- Cash target: **25%**
- Tolerance guide:
  - Growth stocks: **40% - 50%**
  - Defensive assets: **25% - 35%**
  - Cash: **20% - 30%**

Characteristics:
- The portfolio cools meaningfully from Favorable
- The defensive sleeve becomes material, not symbolic
- Cash becomes a clearer buffer, but not yet the dominant sleeve

### 6.3 Weak State
- Growth stocks target: **10%**
- Defensive assets target: **40%**
- Cash target: **50%**
- Tolerance guide:
  - Growth stocks: **5% - 15%**
  - Defensive assets: **35% - 45%**
  - Cash: **40% - 55%**

Characteristics:
- Drawdown control comes first
- Growth exposure is preserved only in a small core size
- Defensive assets and cash do most of the stabilizing work

---

## 7. Growth Stock Pool Design

The growth stock pool uses:

## **Core Pool + Supplemental Pool + Elimination Buffer Zone**

### 7.1 Core Pool
The core pool is the main growth universe of the strategy.

Characteristics:
- Relatively stable membership
- Centered on high-quality mega-cap technology growth
- Intended to remain style-consistent across market cycles
- Emphasizes long-term reusability in allocation

Confirmed first-version core pool:
- `MSFT`
- `NVDA`
- `AMZN`
- `GOOGL`
- `META`
- `AVGO`
- `AAPL`
- `COST`

Purpose:
- Preserve a stable strategy style
- Prevent strategy drift
- Improve live-trading explainability

### 7.2 Supplemental Pool
The supplemental pool provides a limited source of new opportunities.

Characteristics:
- Still governed by strict rules
- Still biased toward large-cap growth
- Must satisfy minimum standards for liquidity, growth, trend, and quality
- Must not dominate the portfolio

Confirmed first-version supplemental pool:
- `LLY`
- `NFLX`
- `TSLA`

Purpose:
- Add adaptability
- Provide a candidate layer beyond the core pool
- Prevent the system from becoming too rigid

### 7.3 Elimination Buffer Zone
Some weakening stocks do not need to be removed permanently. They can first move into an observation layer. If they improve later, they may return to the candidate layer.

Purpose:
- Reduce strategy rigidity
- Reduce mechanism noise from permanently removing and later re-adding names

---

## 8. Growth Stock Scoring System

The first version uses:

## **3 Main Scoring Dimensions + 1 Risk Constraint Dimension**

### 8.1 Trend Quality
Focuses on whether the intermediate-term trend is healthy and still appropriate to hold.

Key preferences:
- The medium-term direction is upward
- Pullbacks do not damage the structure
- The trend is clean and shows decent continuity
- No need to chase short-term spikes

### 8.2 Relative Strength
Focuses on whether a stock is stronger and more leadership-like than comparable growth names.

Key preferences:
- Strong medium-term relative performance
- Belongs to the group that deserves higher holding priority inside the candidate set
- Avoid letting short-term hot-theme rankings dominate the process

### 8.3 Stability / Holdability
Focuses on whether volatility is reasonable and whether the stock fits a live-trading style that is reviewed weekly and adjusted as little as possible.

Key preferences:
- Strong without being chaotic
- Volatility remains under control
- Not easily forced into frequent rebalancing because of noise

### 8.4 Risk Constraint Items
These serve as filters or penalty items to suppress cases such as:

- Excessively high volatility
- Overly distorted price action
- Structural behavior no longer suitable for holding

### 8.5 Scoring Philosophy
The goal of the scoring system is not to find the stock that is "rising the fastest." Its purpose is to find:

## **The high-quality growth leaders that are most worth holding right now**

---

## 9. Final Portfolio Construction Mechanism

The final growth-stock holdings should not be formed through a simple "buy the top N ranked names" rule. Instead, the strategy should use a four-step process:

### Step 1: Qualification Filter
First remove stocks that are clearly unsuitable for holding.

### Step 2: Composite Scoring
Rank candidates based on trend quality, relative strength, and stability.

### Step 3: Risk Constraint Adjustment
Suppress or penalize stocks whose risk characteristics are not ideal.

### Step 4: Holding Stability First
If an existing holding is still qualified, continue holding it by default. A new candidate should only trigger replacement when it is clearly better.

---

## 10. Holding and Rebalancing Rules

### 10.1 Number of Holdings
The total target number of holdings is about **8-12**.

Specifically:
- In a favorable state, the number of growth positions may be closer to the upper bound
- In a neutral state, both the number of growth names and their weight may be moderately reduced
- In a weak state, only the most core and strongest subset of growth exposure should remain

### 10.2 Individual Growth Stock Weight
- Maximum weight for a single growth stock: **10%-12%**
- High-quality leaders may receive a modest overweight
- Standard candidates should receive somewhat less
- Individual position size should be constrained not only by stock quality but also by the portfolio's current risk state

### 10.3 Basic Rebalancing Principles
- Changes in risk regime take priority over changes in ranking
- Eliminating weaker names takes priority over chasing new strong names
- Reducing total exposure takes priority over frequent rotation
- Slightly better is not worth switching; only materially better is
- When no action is necessary, the best action is to do nothing

---

## 11. Replacement Rules and Turnover Friction

### 11.1 Definition of an Existing Holding Becoming Materially Weaker
An existing holding may be considered materially weaker when any of the following occurs:

- Its trend structure has clearly deteriorated
- Its relative strength has clearly fallen behind
- Its volatility / behavioral pattern is no longer suitable for stable medium- to low-frequency holding

### 11.2 Definition of a New Candidate Being Clearly Stronger
A new candidate should replace an existing holding only under the following conditions:

- If the existing holding is already materially weaker, the new candidate only needs to be clearly healthier and more suitable to hold now
- If the existing holding is still qualified, the new candidate must be materially superior in trend quality, relative strength, and stability

### 11.3 Turnover Friction Principle
The strategy should include explicit turnover friction to avoid routine weekly reshuffling.

Design idea:
- Set a strict cap on the number of stock replacements per week
- Optimization-driven replacements should occur much less often than risk-response actions
- Rebalancing should target the minimum necessary action

### 11.4 Situations Where the Strategy Should Only Trim, Not Replace
The following cases are better handled by trimming exposure rather than replacing holdings:

- The risk state is downgraded
- The risk budget has decreased, but there is no clearly superior candidate
- The current problem is excessive total growth exposure, not stock selection error

### 11.5 Portfolio-Level Priority Rule
When the following situations arise, portfolio-level actions should take priority over stock-level optimization:

- A risk state transition occurs
- Drawdown-control pressure increases
- New capital arrives while the risk environment remains weak

---

## 12. Defensive Layer and Cash Layer Rules

### 12.1 Role of the Defensive Layer
The defensive layer is not intended to generate large profits. Its purpose is to:

- Reduce overall portfolio volatility
- Provide a drawdown buffer
- Preserve dry powder for future redeployment

### 12.2 Structure
The defensive layer is divided into:

- **Defensive asset sleeve**
- **Cash sleeve**

First-version defensive sleeve candidates:
- ETFs: **SCHD**, **VIG**, **XLV**, **XLU**, **USMV**, **SGOV**
- Individual defensive stocks: **JNJ**, **PG**, **DUK**

### 12.3 Role Across the Three States

#### Favorable State
- Defensive exposure remains low
- Cash remains low

#### Neutral State
- Defensive assets are increased materially
- Cash is raised moderately

#### Weak State
- Both defensive assets and cash are increased meaningfully
- Defensive assets can move toward their **40%** target while cash can move toward its **50%** target

### 12.4 Design Constraints
- Do not assume that high-dividend assets are automatically defensive in all cases
- Do not turn the strategy into "all cash whenever there is risk"
- Individual defensive stocks are allowed, but they should behave as sleeve components rather than offensive bets
- A better sequence is usually to increase defensive assets first, then raise cash further if needed

---

## 13. First-Version Simplification: No Crypto Layer

The earlier concept allowed for a small crypto enhancement sleeve, but the first implementation should omit it.

Reasons:
- Keep the first version focused on the main return engine and the main drawdown-control engine
- Reduce sources of behavioral and operational complexity
- Make it easier to evaluate whether the core regime and allocation logic work on their own

---

## 14. New Capital Deployment Module

### 14.1 Capital Inflow Event
The strategy assumes that additional capital will arrive once every six months, but the amount is not fixed.

### 14.2 Undeployed Capital Pool
After new capital arrives, it should not be invested automatically right away. Instead, it should first enter:

## **The Undeployed Capital Pool**

Characteristics:
- It is part of account cash
- But it is not treated as cash that must be deployed immediately
- It is only released gradually when deployment conditions are satisfied

### 14.3 Core Deployment Philosophy
Use the following structure:

## **Semiannual variable capital inflows + an undeployed capital pool + layered release based on risk state**

### 14.4 Linkage Between Deployment and Risk State

#### Favorable State
- Deployment can be more proactive
- But it should still be released gradually
- Filling the full target in a single week is not recommended

#### Neutral State
- Deployment should be more cautious
- Release speed should be slower
- Capital should lean more toward core assets or the defensive layer

#### Weak State
- Cash preservation should be the default priority
- The strategy should generally avoid expanding growth risk proactively

### 14.5 Deployment Priority
Deployment priority for new capital should be:

1. Add to underweight but still high-quality core growth positions
2. Build new high-quality growth positions
3. Add to the defensive layer in neutral or defense-leaning conditions
4. If the current environment or opportunity set is not good enough, continue holding cash

### 14.6 Dynamic Release Principle
The strategy should not use a fixed number of tranches or a fixed dollar amount per release. Instead, the weekly release ratio should be determined dynamically based on:

- The current risk state
- The portfolio's current exposure
- Deviation from target allocation
- Candidate asset quality
- Quality of current opportunities

### 14.7 Situations Where Holding Cash Continues to Be Reasonable
Continuing to hold cash is a clear and reasonable decision under the following conditions:

- The risk state is Weak
- The risk state is Neutral but there is a lack of high-quality opportunities
- The current market structure is not attractive
- The portfolio is already close to the maximum risk allowed in the current state

---

## 15. Weekly Decision Process

Each week, the strategy should run in the following order:

### Step 1: Assess the Current Risk State
Determine whether the environment is Favorable, Neutral, or Weak.

### Step 2: Set the High-Level Risk Budget
Decide:
- Total growth-stock allocation
- Defensive layer weight
- Cash weight

### Step 3: Review Existing Growth Holdings
Identify:
- Whether any holdings have weakened materially
- Whether marginal positions should be removed

### Step 4: Compare New Candidates Only When Necessary
Determine:
- Whether replacement is truly necessary
- Whether there is truly a clearly better new candidate

### Step 5: Adjust the Defensive and Cash Layers
Adjust defensive assets and cash based on the risk state and the portfolio's current exposure.

### Step 6: Process the Undeployed Capital Pool
If new capital exists, decide whether to release any this week and how much.

### Step 7: Execute the Minimum Necessary Rebalancing
Use the minimum required action to achieve a portfolio configuration that is good enough for the current environment.

---

## 16. First-Version Strategy Philosophy

This strategy should follow the philosophy below:

### 16.1 Build a Stable Skeleton Before Optimizing for Strength
First ensure that the following are correct:
- Risk state logic
- Allocation framework
- Defensive layer
- Capital deployment module

Only after that should the strategy refine stock-selection details.

### 16.2 Teach the System Not to Make Big Mistakes First
For the first version, the priorities are:
- Do not let risk get out of control
- Do not let drawdown defense fail
- Keep portfolio behavior stable

### 16.3 Reduce Meaningless Turnover
Avoid:
- Large weekly reshuffles
- Overreacting to small score differences
- Damaging the live-trading experience with rebalancing noise

### 16.4 Keep The First Implementation Simpler Than The Original Concept
The first implementation should intentionally exclude crypto so that the initial version can focus on the core interaction among growth, defense, cash, and risk state.

### 16.5 Treat New Capital as a Strategic Resource
New capital is not an automatic buy order. It is strategic dry powder that the system may deploy deliberately.

---

## 17. Complex Elements Excluded from the First Version

To keep complexity under control, the first version does not prioritize:

- Machine learning models
- Minute-level / intraday trading logic
- An excessively wide stock universe
- A crypto sleeve in the first implementation
- Too many detailed financial fundamental inputs
- Complex black-box multi-factor systems
- Complex sentiment signals

---

## 18. Best Prototype Definition for the First Version

The first version of this strategy can be defined as:

## **A medium- to low-frequency dynamic portfolio system driven primarily by high-quality U.S. growth stocks, with total exposure managed through three risk states, drawdown controlled through a defensive layer and cash, and semiannual variable capital inflows deployed intelligently.**

---

## 19. Recommended Next Steps

Based on the current confirmed Balanced prototype, the next best directions are:

### Direction A: Convert This Revised Design Into A Backtestable Decision Table
That means defining in greater detail:
- The exact decision table combining trend, breadth, and stress
- The explicit downgrade and upgrade buffers
- The state-allocation rebalance triggers around the target weights
- The exact defensive-sleeve usage rules inside each state

### Direction B: Convert The Confirmed Universe And Regime Design Into An Implementation Specification
That means finalizing:
- The qualification and scoring rules for the confirmed growth pools
- The replacement-friction rules
- The weekly rebalance sequence
- The capital-release rules tied to the revised state targets

The current document is already beyond open-ended brainstorming. The next step is precise rule drafting for backtesting and implementation.
