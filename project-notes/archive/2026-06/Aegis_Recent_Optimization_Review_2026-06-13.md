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
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs
  - Tests/Algorithm/AegisGrowthAllocationTests.cs
---

# Aegis Recent Optimization Review - 2026-06-13

## Agent Summary

The last two weeks moved Aegis from workflow/tooling cleanup into a focused weak-sleeve optimization round. Candidate B is still the best enabled defensive baseline versus Candidate A, but the all-defensive-off control shows the pre-weak guard is expensive over the full 2016-2026 span. The best next move is not severe-crash work; it is a parameter-only pre-weak selectivity test using materially higher `pre-weak-dd-threshold` values.

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

## Interpretation

Candidate B is better than Candidate A, but Candidate A is not different enough to solve the main issue. Moving `pre-weak-dd-threshold` from `0.04` to `0.05` reduced PreWeak activations only from `127` to `114` weeks and produced nearly the same long-run return profile.

The all-defensive-off run is the stronger diagnostic. It suggests the current guard activates too often or too early in markets where staying invested is rewarded. Candidate B buys roughly `2.5` points of max-drawdown reduction and better 2022 behavior, but pays for it with about `4.7` CAGR points and a very large ending-equity gap.

This also weakens the case for severe-crash work as the immediate next move. The current loss is not primarily a missing deeper crash mode; it is opportunity cost from pre-weak defensive activation before or outside true stress.

## Best Next Move

Run a parameter-only selectivity grid on `pre-weak-dd-threshold` before changing code:

| Variant | `pre-weak-dd-threshold` | Purpose |
| --- | ---: | --- |
| Candidate B baseline | `0.04` | Current promoted default |
| Selective 07 | `0.07` | First materially less-frequent guard |
| Selective 09 | `0.09` | Midpoint between Candidate B and near-crisis-only |
| Selective 12 | `0.12` | High-selectivity guard, close to stress-only behavior |
| All defensive off | disabled | Control |

Keep all other parameters fixed:

- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true` for threshold variants
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `pre-weak-growth-target=0.12`
- `pre-weak-def-target=0.30`
- `weak-growth-target=0.10`

## Validation Order

1. Run the three threshold variants on the same long span:
   - `backtest-start=2016-01-01`
   - `backtest-end=2026-06-11`
2. Compare against Candidate B and all-defensive-off on CAGR, drawdown, Sharpe, PSR, 2022, partial 2026, order count, fees, and PreWeak week count.
3. Promote no result from the long span alone. Take the best one or two threshold variants into the five standard windows:
   - `2007-10-01` to `2008-12-31`
   - `2009-01-01` to `2010-12-31`
   - `2019-07-01` to `2020-12-31`
   - `2021-01-01` to `2022-12-31`
   - `2023-01-01` to `2026-01-01`
4. Only after this pass decide whether code-level trigger refinement is needed.

## Promotion Gates

A threshold variant is interesting only if it:

- recovers meaningful return versus Candidate B, with target CAGR at least around `18%` on the long span;
- keeps max drawdown materially below all-off, preferably no worse than about `15.5%`;
- preserves at least half of Candidate B's 2022 protection, meaning 2022 should be no worse than roughly `-11.9%`;
- improves Sharpe/PSR toward all-off rather than merely increasing exposure;
- does not materially increase orders, fees, turnover, or diagnostic false positives;
- does not create renewed recovery drag in 2009-2010 or 2019-2020.

## Do Not Do Yet

- Do not promote all-defensive-off directly; its 2022 and drawdown behavior are worse.
- Do not add severe-crash code yet; the current evidence points first to pre-weak activation frequency.
- Do not change defaults, live parsing, Object Store schema, deployment identity, or order handling during this parameter-only round.

## First Action

Run `Selective 07`:

- `backtest-start=2016-01-01`
- `backtest-end=2026-06-11`
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=true`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `pre-weak-dd-threshold=0.07`
- `pre-weak-growth-target=0.12`
- `pre-weak-def-target=0.30`
- `weak-growth-target=0.10`

If `Selective 07` does not materially reduce PreWeak weeks or recover return, skip small threshold increments and run `0.09` next.

## Risks And Open Questions

- This is still parameter mining unless the promotion gates are enforced across the five standard windows.
- A higher threshold may simply converge toward all-off and lose the 2022 protection.
- Current diagnostics summarize PreWeak weeks but do not yet separate false-positive activation clusters by market regime; code-level attribution may be needed after the parameter-only grid.
- Local targeted NUnit execution has been unreliable due existing Lean test-host environment issues, so cloud backtest diagnostics remain the primary evidence source for this round.
