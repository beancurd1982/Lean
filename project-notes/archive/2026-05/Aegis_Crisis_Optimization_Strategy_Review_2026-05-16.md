# Aegis Crisis Optimization Strategy Review

## Step 1: Review Scope

Date:
- 2026-05-16

Request:
- Reassess the recent Aegis defensive optimization strategy using all crisis backtest results `1` through `20`, not only the most recent batch.
- Spawn three agents to independently analyze the results and recent optimization path.
- Have the agents challenge each other's conclusions before producing a final proposal.

Context:
- Runs `1` to `5`: weak-stress overlay experiment.
- Runs `6` to `10`: pre-weak guard experiment.
- Runs `11` to `15`: first severe-crash override experiment.
- Runs `16` to `20`: stateful severe-crash override experiment.

Goal:
- Decide whether the current direction is still the right track.
- Identify the best next move before more implementation or cloud backtests.

Next step:
- Dispatch three read-only agents with distinct review lenses:
  - aggregate performance and ranking
  - defensive behavior and crisis risk
  - process/strategy critique and overfitting risk

## Step 2: Coordinator Aggregate Review

Date:
- 2026-05-16

Aggregate metrics across runs `1` to `20`:

| Experiment Family | Avg Net | Avg Drawdown | Worst Drawdown | Avg Sharpe | Total Orders | Total Fees |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| weak-stress-overlay (`1-5`) | 12.366% | 11.140% | 15.900% | 0.502 | 848 | $1,277.41 |
| pre-weak-guard (`6-10`) | 15.162% | 12.420% | 20.300% | 0.604 | 931 | $1,455.67 |
| severe-crash-override (`11-15`) | 13.387% | 12.600% | 19.200% | 0.552 | 934 | $1,451.16 |
| stateful-severe-crash (`16-20`) | 13.549% | 12.900% | 18.500% | 0.540 | 922 | $1,450.78 |

Window-level observations:
- `2008`: severe-crash-override is best on net, drawdown, and Sharpe.
- `2009`: pre-weak, severe, and stateful-severe tie on return/Sharpe; weak-stress has lower drawdown but lower return.
- `2010`: pre-weak is best on return, drawdown, and Sharpe.
- `2020`: pre-weak is best on return/Sharpe; weak-stress is best on drawdown.
- `2021-22`: pre-weak is best on return, drawdown, and Sharpe.

Coordinator conclusion:
- The latest stateful severe-crash direction is not clearly the best next move.
- Pre-weak is the strongest aggregate benchmark, but it fails 2008.
- Severe logic helps 2008 but hurts 2021-22.
- Weak-stress has the best drawdown posture/cost profile but gives up return.
- No single experiment dominates all goals.

## Step 3: Agent Findings

Date:
- 2026-05-16

Agent 1: aggregate performance lens:
- Strongest overall family is pre-weak-guard.
- Weakest aggregate-performance family is weak-stress-overlay, despite its lower drawdown/cost profile.
- Latest stateful severe-crash only slightly improves over first severe-crash and still trails pre-weak by about `8.065` net-profit points across the five windows.
- Final debate position: freeze code and run a same-code-version validation matrix before more implementation.

Agent 2: defensive-risk lens:
- Severe-crash entry is too late to protect first-leg crisis damage by itself.
- Stateful severe-crash improves some severe-mode behavior but does not solve 2022 and worsens 2008 versus hard severe.
- A staged severe component is plausible only if paired with pre-weak protection and strict gates.
- Final debate position: pause implementation; validate current parameter combinations first.

Agent 3: process/overfit lens:
- The recent workflow risks chasing each latest result batch.
- The same five known stress windows have been used for diagnosis, design, and judgment, creating overfit risk.
- Experiments were partly isolated, but not a full same-code-version matrix.
- Combined behavior is especially uncertain because override priority masks lower-priority overlays.
- Final debate position: no code change is justified before more validation.

## Step 4: Final Strategy Proposal

Date:
- 2026-05-16

Consensus:
- Do not implement the two-stage severe-crash mode yet.
- Treat it as a plausible future component, not the immediate next move.
- First run a same-code-version validation matrix using the current committed code.

Primary reason:
- The current evidence says our optimization process may be overreacting to the last observed failure mode.
- A new rule now would add another degree of freedom before we know which existing mechanism is actually robust.

Recommended next backtest matrix:
- `default/off baseline`
- `weak-stress only`
- `pre-weak only`
- `stateful severe only`
- `pre-weak + stateful severe`
- optionally `weak-stress + pre-weak`

Required windows:
- The same five crisis windows for continuity:
  - `2007-10-01` to `2008-12-31`
  - `2009-01-01` to `2009-12-31`
  - `2010-01-01` to `2010-12-31`
  - `2019-07-01` to `2020-12-31`
  - `2021-01-01` to `2022-12-31`
- Add at least three control windows before more code:
  - normal bull period
  - sideways/choppy period
  - shallow correction period
- Also run one broad full-period test to check return preservation.

Decision gates:
- Pre-weak remains the provisional benchmark only if it preserves broad-period CAGR/Sharpe.
- Any combined configuration must not materially regress run `10` / 2021-22 behavior.
- Any severe-crash component must improve 2008 without destroying 2020/2022 recovery capture.
- If no existing combination passes, then consider a narrow two-stage severe-crash implementation.

Next step:
- Define exact parameter sets and windows for the validation matrix before changing code.

## Step 5: Commit Preparation

Date:
- 2026-05-16

Local change:
- This strategy review note is the only uncommitted local change.

Plan status:
- The best next move is documented at strategy level:
  - freeze code
  - run a same-code-version validation matrix
  - compare existing mechanisms before implementing more rules
- A detailed execution run sheet is still needed before cloud backtests:
  - exact parameter sets
  - exact crisis and control windows
  - naming convention for uploaded results
  - decision gates for pass/fail

Commit scope:
- Commit and push this review note only.
