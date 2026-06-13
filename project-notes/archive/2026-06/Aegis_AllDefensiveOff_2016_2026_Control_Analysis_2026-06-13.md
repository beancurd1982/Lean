---
id: AEGIS-BT-2026-06-13-ALL-DEFENSIVE-OFF
type: backtest-analysis
status: reviewed
date: 2026-06-13
topic: AegisGrowthAllocation
tags: [aegis, all-defensive-off, candidate-b, backtest, validation]
related:
  - project-notes/archive/2026-06/Aegis_CandidateA_vs_B_2016_2026_Comparison_2026-06-12.md
  - project-notes/archive/2026-06/Aegis_CandidateB_2016_2026_Backtest_Analysis_2026-06-12.md
  - project-notes/archive/2026-06/Aegis_CandidateB_Next_Optimization_Proposal_2026-06-12.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_AllDefensiveOff_Long_2016_2026.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_AllDefensiveOff_Long_2016_2026_orders.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-13_121029__AegisGrowthAllocation__ManualGrid_WeakSleeve_AllDefensiveOff_Long_2016_2026_logs.txt
---

# Aegis All-Defensive-Off 2016-2026 Control Analysis - 2026-06-13

## Agent Summary

The all-defensive-off long-span control materially outperformed Candidate B on aggregate return and risk-adjusted metrics, while accepting a higher max drawdown. This does not automatically invalidate Candidate B, because Candidate B still improved 2022 and partial 2026 downside behavior, but it changes the next optimization question: the pre-weak defensive guard is buying some downside moderation at a large opportunity cost in normal and rebound markets.

## Run Setup

- Configured period: `2016-01-01` to `2026-06-11`
- Actual available data period: `2016-01-01` to `2026-03-14`
- Algorithm version: `AegisGrowthAllocation-2026-06-08-candidate-b-defaults`
- Source revision: `4499311dc`
- `crisis-diagnostics=true`
- `pre-weak-guard-enabled=false`
- `weak-stress-overlay-enabled=false`
- `severe-crash-override-enabled=false`
- `pre-weak-growth-target=0.12`
- `pre-weak-def-target=0.30`
- `weak-growth-target=0.10`
- `pre-weak-dd-threshold=0.04`

## Headline Comparison

| Metric | All Defensive Off | Candidate B | Read |
| --- | ---: | ---: | --- |
| Net Profit | `580.604%` | `354.365%` | All-off better by `226.239` points |
| CAGR | `20.676%` | `15.991%` | All-off better by `4.685` points |
| End Equity | `$204,181.21` | `$136,309.55` | All-off better by `$67,871.66` |
| Drawdown | `16.200%` | `13.700%` | Candidate B better by `2.500` points |
| Sharpe | `1.102` | `0.885` | All-off better |
| Sortino | `1.224` | `0.934` | All-off better |
| PSR | `84.417%` | `65.167%` | All-off better |
| Orders | `1493` | `1581` | All-off has `88` fewer orders |
| Fees | `$1,860.48` | `$1,821.95` | Candidate B lower by `$38.53` |
| Expectancy | `0.615` | `0.520` | All-off better |

## Diagnostics

All defensive off:

- PreWeak weeks: `0`
- NonPreWeak weeks: `532`
- Weak regime weeks: `45`
- Severe crash weeks: `0`
- NonPreWeak average drawdown: `3.41%`
- NonPreWeak next-return average: `0.38%`
- NonPreWeak average target: `G0.4828/D0.2773/C0.2399`

Candidate B:

- PreWeak weeks: `127`
- NonPreWeak weeks: `405`
- Weak regime weeks: `45`
- Severe crash weeks: `0`
- PreWeak average drawdown: `6.69%`
- PreWeak forward 4-week average: `1.20%`
- PreWeak forward 4-week win rate: `71.54%`

## Chart-Derived Annual Read

| Year | All Defensive Off | Candidate B | All-Off - B |
| --- | ---: | ---: | ---: |
| 2016 | `14.23%` | `9.86%` | `+4.37` |
| 2017 | `28.54%` | `27.02%` | `+1.52` |
| 2018 | `15.01%` | `7.56%` | `+7.44` |
| 2019 | `20.62%` | `18.81%` | `+1.81` |
| 2020 | `48.14%` | `31.37%` | `+16.77` |
| 2021 | `35.37%` | `35.56%` | `-0.19` |
| 2022 | `-13.96%` | `-9.91%` | `-4.05` |
| 2023 | `35.05%` | `29.00%` | `+6.04` |
| 2024 | `27.03%` | `19.00%` | `+8.03` |
| 2025 | `16.68%` | `5.96%` | `+10.72` |
| 2026 partial | `-3.25%` | `-2.36%` | `-0.89` |

## Decision Read

Candidate B's pre-weak guard is not a free improvement over the long 2016-2026 control. It lowers maximum drawdown by about `2.5` points and helps the weakest years in this span, especially 2022 and partial 2026, but the opportunity cost is large: lower CAGR, lower Sharpe, lower PSR, more orders, and materially lower ending equity.

The result argues against adding another defensive layer immediately. The more useful next step is attribution and constrained retuning of when the pre-weak guard activates, because the current guard appears too costly outside genuine stress.

## Recommendation

Do not promote all-defensive-off as the live default from this single comparison, because its higher drawdown and worse 2022 behavior are real. Also do not proceed to severe-crash work yet.

Next useful test:

- Keep Candidate B's sleeve targets unchanged.
- Test a less frequent pre-weak activation rule over the same long span and known stress windows.
- Primary goal: recover a meaningful portion of all-off CAGR/Sharpe while preserving most of Candidate B's 2022 and partial-2026 drawdown benefit.

## Verification

- Parsed headline statistics from `ManualGrid_WeakSleeve_AllDefensiveOff_Long_2016_2026.json`.
- Compared against the committed Candidate A and Candidate B long-run result JSON files.
- Confirmed diagnostics from the normalized all-defensive-off log file.
- Moved downloaded result files from `C:/Users/douya/Downloads` into `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs`; the source Downloads files were removed by the move operation.

## Analysis Workflow Findings

- New QuantConnect result files were first identified by sorting `C:/Users/douya/Downloads` by `LastWriteTime`. The result set stem was `Crawling Fluorescent Orange Sardine`.
- The log file was the fastest confirmation that the run matched the intended control: `pre-weak-guard-enabled=false` was reflected by diagnostics with `PreWeakWeeks=0`, while `WeakRegimeWeeks=45` and the algorithm version/source revision matched the Candidate B default code line.
- The downloaded files were moved, not copied, into `BackTestLogs` so the Downloads originals were removed immediately after normalization.
- QuantConnect orders CSV exports may contain trailing spaces after the final empty `Tag` field (`"" `). `git diff --check` does not catch this while the CSV is untracked, so future analysis should run an explicit trailing-whitespace check on any newly normalized untracked CSV before staging or committing.
- The trailing-space fix used a line-end trim only; this preserves CSV fields and removes export-only whitespace noise.
- A PowerShell metric-comparison command failed once with `An empty pipe element is not allowed` because a `foreach` block was piped directly without wrapping the script block. Use `& { ... } | Format-Table` when piping generated objects from a multi-line block.
- Chart-derived annual returns were calculated from `charts.Strategy Equity.series.Equity.values`, using each year's last equity value. Each point is an array where item `0` is Unix time and item `4` is the equity value used for the annual table.
- Because this result materially challenged Candidate B's current baseline, it required a durable note and a `backtest-index.md` entry rather than only an inline response.

## Open Questions

- Is the `2.5` point drawdown reduction enough to justify the large CAGR and ending-equity cost?
- Can the pre-weak guard be made more selective without simply reverting to all-off behavior?
- Does all-off remain too risky on the pre-2016 crisis windows already used for defensive validation?
