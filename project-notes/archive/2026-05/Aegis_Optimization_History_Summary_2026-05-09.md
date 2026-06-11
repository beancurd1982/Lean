# Aegis Optimization History Summary - 2026-05-09

## Step 1: Intake

Date:
- 2026-05-09

Summary:
- User requested a summary of the git history focused on what has been done to optimize `AegisGrowthAllocation` performance.
- Scope is read-only analysis of commit history and related project notes.

Files planned to review:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- relevant `project-notes/` entries that describe optimization rounds, relaunch planning, warnings, and backtest analysis

## Step 2: Git History Findings

Date:
- 2026-05-09

Performance-relevant commits identified:
- `5c1638191` `Parameterize Aegis regime thresholds`
- `f502b220d` `Set Aegis V2 baseline and batch 2 params`
- `20741de3f` `Select Aegis round 2 turnover defaults`
- `1ede38f05` `fix: keep Aegis exchange-aware schedule`
- `d56948045` `feat: apply Aegis round 2 rebalance gating`
- `60cafe46e` `feat: add Aegis backtest date parameters`

High-level optimization storyline:
- The first optimization wave exposed regime controls so cloud optimization could tune them safely.
- Round 1 analysis selected the `V2` regime region as the best performance-oriented baseline and `V3` as the conservative alternative.
- The second wave exposed turnover and replacement controls, then updated defaults to the selected winner.
- Later structural optimization reduced unnecessary rebalancing through tolerance-band gating and improved the observed baseline metrics in `V10`.
- The most recent backtest-date parameterization is a validation aid for focused crisis-window testing, not a direct performance optimization.

## Step 3: Key Historical Conclusions

Date:
- 2026-05-09

Round 1 conclusions:
- `V2` outperformed the original baseline on Sharpe and total return while reducing turnover and order count.
- `V3` was kept as the lower-drawdown control candidate.

Round 2 conclusions:
- The adopted baseline defaults became:
  - `weak-stress-threshold = 27`
  - `favorable-breadth-threshold = 0.75`
  - `upgrade-confirmation-weeks = 1`
- The selected turnover-control defaults became:
  - `replacement-score-gap = 10`
  - `hold-stability-bonus = 2`
  - `growth-atr-eligibility-limit = 0.06`

Structural optimization conclusions:
- Tolerance-band rebalance gating appears to be the most credible later performance improvement.
- The `V10` evidence versus `V9` showed:
  - higher end equity
  - higher net profit
  - higher Sharpe
  - unchanged drawdown
  - fewer orders
  - slightly lower turnover and fees
- Reserve-release integration was implemented, but the main validating run used `UndeployedReserve=0`, so that path is still not meaningfully proven as a performance enhancer.

Review:
- No code changes were made in this history-summary task.
- The summary is based on git history plus the optimization and backtest-analysis notes already recorded in `project-notes/`.
