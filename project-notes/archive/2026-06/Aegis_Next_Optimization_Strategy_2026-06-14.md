---
id: AEGIS-DEC-2026-06-14-NEXT-OPTIMIZATION-STRATEGY
type: decision
status: accepted
date: 2026-06-14
topic: AegisGrowthAllocation
tags: [aegis, optimization, strategy, candidate-b, stress-protection, source-audit]
related:
  - project-notes/archive/2026-06/Aegis_Weak_Sleeve_Target_Parameterization_2026-06-08.md
  - project-notes/archive/2026-06/Aegis_CandidateB_2016_2026_Backtest_Analysis_2026-06-12.md
  - project-notes/archive/2026-06/Aegis_CandidateA_vs_B_2016_2026_Comparison_2026-06-12.md
  - project-notes/archive/2026-06/Aegis_AllDefensiveOff_2016_2026_Control_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_OptS09A_2016_2026_Backtest_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_OptS09A_Stress04_2021_2022_Backtest_Analysis_2026-06-13.md
  - project-notes/archive/2026-06/Aegis_OptS07A_Stress04_2021_2022_Backtest_Analysis_2026-06-14.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs
---

# Aegis Next Optimization Strategy - 2026-06-14

## Agent Summary

Read this before starting the next Aegis optimization phase. The recent Candidate B optimization branch recovered long-run return but failed the key `2021-2022` stress gate. The accepted direction is to keep Candidate B as the working baseline and shift from broad parameter search to a source-code audit focused on stress protection first.

## Decision

Use **stress protection first** as the next optimization principle.

Candidate B remains the current general/default baseline:

- `pre-weak-dd-threshold=0.04`
- `pre-weak-growth-target=0.12`
- `pre-weak-def-target=0.30`
- `weak-growth-target=0.10`

Do not promote the high-growth PreWeak branch:

- OptS07A: `0.08 / 0.24 / 0.25 / 0.10`
- OptS09A: `0.09 / 0.24 / 0.25 / 0.10`

Those candidates are useful evidence, but not default candidates.

## Why We Are Here

The 2026-06 Candidate B optimization effort showed a stable tradeoff:

1. Candidate B remains the best stress-aware baseline.
2. All-defensive-off and high-growth PreWeak variants recover long-run return.
3. The return recovery comes from accepting materially worse weak-market behavior.
4. Both OptS07A and OptS09A failed the `2021-2022` stress validation versus Candidate B.

The key failure is not that the optimizer was useless. It identified where long-run return is hiding: less defensive PreWeak exposure. The problem is that the discovered region is too aggressive for the stress behavior the algorithm is supposed to preserve.

## Evidence Trail

- Candidate B was accepted as the weak-sleeve default in [Aegis_Weak_Sleeve_Target_Parameterization_2026-06-08](Aegis_Weak_Sleeve_Target_Parameterization_2026-06-08.md).
- Candidate B long-run validation is recorded in [Aegis_CandidateB_2016_2026_Backtest_Analysis_2026-06-12](Aegis_CandidateB_2016_2026_Backtest_Analysis_2026-06-12.md).
- Candidate B beat Candidate A in [Aegis_CandidateA_vs_B_2016_2026_Comparison_2026-06-12](Aegis_CandidateA_vs_B_2016_2026_Comparison_2026-06-12.md).
- All-defensive-off exposed Candidate B's opportunity cost in [Aegis_AllDefensiveOff_2016_2026_Control_Analysis_2026-06-13](Aegis_AllDefensiveOff_2016_2026_Control_Analysis_2026-06-13.md).
- OptS09A became the best long-span headline candidate in [Aegis_OptS09A_2016_2026_Backtest_Analysis_2026-06-13](Aegis_OptS09A_2016_2026_Backtest_Analysis_2026-06-13.md).
- OptS09A failed `2021-2022` stress validation in [Aegis_OptS09A_Stress04_2021_2022_Backtest_Analysis_2026-06-13](Aegis_OptS09A_Stress04_2021_2022_Backtest_Analysis_2026-06-13.md).
- OptS07A also failed `2021-2022` stress validation in [Aegis_OptS07A_Stress04_2021_2022_Backtest_Analysis_2026-06-14](Aegis_OptS07A_Stress04_2021_2022_Backtest_Analysis_2026-06-14.md).

## Next Workstream

Start with a source-code audit before more web research, plugin searching, or paid optimization.

Audit the algorithm for possible stress-first improvements in these areas:

- regime detection and state transitions;
- PreWeak trigger timing, confirmation, and exit logic;
- whether PreWeak should be graded rather than binary;
- Weak and SevereCrash regime separation;
- sleeve construction and cash/defensive allocation mechanics;
- defensive universe quality during 2021-2022;
- ranking, replacement, and hold-stability behavior during stress;
- rebalance cadence and whether stress transitions cause late or noisy trades;
- attribution of which assets and weeks drove Candidate B's stress advantage.

The audit should propose possible next moves and prioritize them by expected decision value, implementation risk, and validation cost.

## Research And Tooling Order

1. **Source-code audit first.** The code defines the real levers and prevents broad research from becoming noise.
2. **Attribution second.** Use exported trades/equity/logs to identify when the algorithm gave up Candidate B's stress advantage.
3. **Targeted external research third.** Search papers, articles, forums, or financial research only after the audit identifies specific weak spots, such as drawdown-based regime filters, breadth filters, dual momentum crash filters, or defensive ETF rotation.
4. **Plugin/service search only if a concrete gap appears.** Generic trading-optimization plugins are not the first move; specialized statistical tooling may help later if attribution requires it.
5. **Paid QuantConnect optimization only after a pre-registered hypothesis.** If used, keep it bounded and stress-first.

## Success Criteria For Next Candidate

A new candidate should not be judged by long-span CAGR alone. It should first preserve Candidate B-like stress behavior:

- `2021-2022` net return should be close to or better than Candidate B's `19.808%`.
- `2021-2022` drawdown should be close to or better than Candidate B's `12.400%`.
- `2021-2022` Sharpe and PSR should not materially degrade versus Candidate B.
- Long-run return recovery is secondary until the stress gate is met.

Only after a candidate preserves stress behavior should it be tested for long-run return improvement.

## Do Not Do Yet

- Do not promote OptS07A, OptS09A, Selective07, Selective09, or all-defensive-off.
- Do not run another broad long-span optimization without a new source-level hypothesis.
- Do not add severe-crash code, live behavior changes, Object Store changes, order-handling changes, or default changes before the audit.
- Do not treat web research as a substitute for understanding the local algorithm's actual decision path.

## Open Questions For Audit

- Is Candidate B's stress advantage caused by earlier activation, more defensive sleeve targets, different assets held, or fewer bad replacement decisions?
- Does the PreWeak signal need graded exposure instead of a fixed sleeve?
- Does the algorithm need a separate 2021-2022 style rate/inflation stress regime distinct from ordinary drawdown weakness?
- Are defensive assets chosen by the current ranking model actually defensive in the regimes where PreWeak activates?
- Is rebound capture lost because PreWeak exits too late, activates too early, or keeps too much cash?
