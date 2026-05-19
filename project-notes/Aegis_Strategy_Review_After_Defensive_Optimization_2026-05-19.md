# Aegis Strategy Review After Defensive Optimization 2026-05-19

## Step 1: Intake

Date:
- 2026-05-19

Request:
- Evaluate current `AegisGrowthAllocation` performance after defensive optimization.
- Use three sub-agents to analyze the algorithm from complementary perspectives.
- Identify `3` to `5` highest-impact improvement areas.
- Propose the next move.

Scope:
- Read-only strategy review.
- No algorithm code changes in this task.
- Use local backtest artifacts, project notes, and current source code.

Initial evidence to review:
- `project-notes/Aegis_Base_vs_Defensive_Optimization_2026-05-18.md`
- `project-notes/Aegis_Validation_Matrix_Run_Sheet_2026-05-16.md`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Base.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/DefensivelyOptimized.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs`
- current `AegisGrowthAllocation` source files

Agent plan:
- Agent 1: performance and return/risk attribution.
- Agent 2: defensive regime/risk-control review.
- Agent 3: alpha, universe, portfolio construction, and deployment practicality.

Open questions:
- None at intake. If evidence conflicts, document the conflict rather than guessing.

## Step 2: Agent Reviews

Agent 1: performance and return/risk attribution.
- Defensive optimization improved max drawdown from `16.500%` to `13.800%`.
- 2022 improved materially: annual return improved from `-14.301%` to `-11.498%`, and intra-year drawdown improved from `16.083%` to `13.335%`.
- Loss quality improved: average loss improved from `-0.40%` to `-0.36%`, and profit-loss ratio improved from `1.20` to `1.24`.
- Absolute return worsened: net profit fell from `463.914%` to `443.042%`, CAGR fell from `18.868%` to `18.421%`, and ending equity fell by about `$6.26k`.
- Risk-adjusted return did not improve: Sharpe stayed near `0.994`, Sortino moved from `1.080` to `1.075`.
- Main bottleneck: defensive/cash rotation drag in normal years, especially `2023`, `2016`, `2019`, `2018`, and `2024`.

Agent 2: defensive regime and risk-control review.
- Current `pre-weak=true`, `weak-stress=false`, `severe-crash=false` defaults are reasonable for live/paper deployment.
- Pre-weak is useful but not a crash solution: it improved 2010/2020/2021-22 but did not fix the 2008-style sudden-crash window.
- Weak-stress overlay reduces drawdown but is too expensive in recovery/normal windows.
- Strict severe-crash improves 2008 but is too blunt for 2020 and 2021-22.
- Critical live-risk finding: defensive high-water mark and severe-crash state are runtime-only and not persisted in live state. A restart during drawdown could de-calibrate pre-weak/severe drawdown triggers.

Agent 3: alpha, universe, portfolio construction, and live-deployment review.
- Biggest strategic risk is universe hindsight bias: the current fixed growth universe contains many 2026-known mega-cap winners.
- Ranking is fragile because it scores a small fixed candidate list; percentile ranks can swing when only a few names qualify.
- Portfolio construction is equal-weighted inside sleeves; score/volatility weighting may preserve upside while improving risk.
- Selection changes can trigger turnover even when sleeve weights are inside tolerance; optimized run already added `54` orders and higher fees.
- Live deployment should verify the VIX/stress data path and open-order skip behavior before adding more sophistication.

## Step 3: Consolidated Evaluation

Current algorithm status:
- The current algorithm is a better defensive version, not a higher-return version.
- It reduced full-period max drawdown by `2.7` percentage points and improved 2022 loss/drawdown materially.
- It did not improve full-period CAGR, ending equity, Sharpe, or Sortino.
- The defensive default is acceptable for paper/live if the objective is lower drawdown with limited CAGR sacrifice.

Core tradeoff:
- Base algorithm captures more upside in normal/strong years.
- Defensive optimized algorithm protects better in 2022-style bear-market conditions.
- The next optimization should target return drag and robustness, not simply add more defensive switches.

## Step 4: Highest-Impact Improvement Areas

1. Persist defensive state for live safety.
- Persist `_defensiveOverrideEquityHighWaterMark`, `_severeCrashModeActive`, `_severeCrashRecoveryWeeks`, and related severe-crash state.
- Reason: pre-weak is now live-default enabled, and drawdown-triggered logic must survive restarts.
- Impact: high live-safety impact; prevents silent under-defense after restart.

2. Validate and de-bias the universe.
- Test the current fixed universe against non-hindsight alternatives such as a broader liquid universe, sector ETF/core-stock hybrid, or historically available constituents.
- Reason: current 2016-2026 performance may partly reflect selecting known winners in hindsight.
- Impact: high model-validity impact; without this, further optimization may overfit a hand-picked universe.

3. Reduce pre-weak return drag.
- Run diagnostics-enabled attribution to measure returns during pre-weak-active weeks versus inactive weeks.
- Test a narrower pre-weak trigger, a higher drawdown threshold, or a staged pre-weak sleeve that does not cut growth as much in false alarms.
- Reason: pre-weak improved 2022 but caused drag across multiple normal years.
- Impact: high because it targets the main observed cost of defensive optimization.

4. Redesign severe-crash as selective systemic-crash protection.
- Do not enable the current strict severe mode broadly.
- Candidate design: require weak regime + severe stress + drawdown + breadth near zero or VIX materially above the current threshold.
- Also test faster exit or a softer sleeve to avoid 2020-style recovery drag.
- Impact: high because it is the path to fixing 2008 without damaging 2020/2021-22.

5. Improve portfolio construction and turnover discipline.
- Test score/volatility-weighted allocation inside sleeves instead of equal weight.
- Add stronger hold thresholds or replacement hysteresis to reduce unnecessary trades.
- Reason: optimized version added orders/fees while lowering CAGR, and current equal-weighting may leave return/risk efficiency on the table.
- Impact: medium-high; likely improves both return retention and drawdown if done carefully.

## Step 5: Proposed Next Move

Recommended sequence:
1. First implement live-state persistence for defensive high-water/severe-crash state.
2. Then run a diagnostics-enabled full-period attribution backtest using current defaults.
3. Use that attribution to tune pre-weak narrowly, not blindly.
4. In parallel or immediately after, design a universe validation matrix before changing alpha selection logic.

Why this order:
- Live-state persistence is a safety prerequisite now that pre-weak is default-on.
- Diagnostics attribution tells us exactly where the current return drag comes from.
- Universe validation prevents over-optimizing a potentially hindsight-biased symbol list.
- Severe-crash redesign should wait until pre-weak attribution and universe validation are clearer.

Review:
- No code changes were made for this strategy review.
- Three sub-agents completed read-only reviews.
- Main unresolved risk: the current algorithm is suitable for defensive paper testing, but not yet proven as a robust long-term live strategy because universe bias and restart-state safety remain open.
