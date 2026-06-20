---
id: AEGIS-BT-2026-06-20-RETURN-GAP
type: backtest-analysis
status: accepted
date: 2026-06-20
topic: AegisGrowthAllocation
tags: [aegis, candidate-b, candidate-c, diagnostics, attribution]
related:
  - project-notes/archive/2026-06/Aegis_CandidateB_Return_Gap_Attribution_Sprint_2026-06-19.md
  - project-notes/archive/2026-06/Aegis_BC_Diagnostic_Set_Completeness_2026-06-19.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CurrentSource_CandidateBBehavior_DiagSameExec_Stress04_2021_2022.json
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_CandidateC_PreWeakRecoveryV2_DiagSameExec_Stress04_2021_2022.json
---

# Candidate B Return-Gap Attribution Analysis

## Agent Summary

Read this note before choosing Candidate D. The completed same-execution weekly diagnostic grid shows Candidate C's edge over Candidate B is concentrated in one 2025 redeployment path, while Candidate B still owns the 2021-2022 stress gate. Candidate D should not be implemented yet; the next approved direction is a bounded post-stress redeployment hypothesis with additional confirmation, not a broad optimizer run.

## Decision Or Finding

Candidate D remains blocked. Candidate C does not provide enough robust evidence to promote its current recovery logic as the next baseline because:

- Candidate B still wins the same-execution 2021-2022 stress gate.
- Candidate C's segmented long-grid edge is concentrated in `2024-2026-01-01`.
- Earlier recovery activations in 2016 and 2022 were neutral or negative, not consistently positive.
- True sleeve contribution is not directly logged; current diagnostics support exposure/path attribution, not sleeve P&L attribution.

The best next lever is post-stress redeployment / cash-drag reduction, but it needs a pre-registered bounded hypothesis before any code change.

## Evidence

Control set used:

- Candidate B current-source behavior, same-execution stress `2021-2022`.
- Candidate B current-source behavior, segmented long windows `2016-2017`, `2018-2019`, `2020-2021`, `2022-2023`, `2024-2026-01-01`.
- Candidate C PreWeakRecoveryV2, same-execution stress `2021-2022`.
- Candidate C PreWeakRecoveryV2, the same five segmented long windows.

All runs used Market On Close execution and weekly `crisis-diagnostics=true` rows.

## Segment Metrics

| Segment | B Net | C Net | Delta | B CAGR | C CAGR | Delta | B DD | C DD | Orders Delta |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Stress 2021-2022 | 17.613% | 17.432% | -0.181 pts | 8.465% | 8.382% | -0.083 pts | 12.9% | 13.1% | +2 |
| 2016-2017 | 35.679% | 35.359% | -0.320 pts | 16.513% | 16.376% | -0.137 pts | 9.4% | 9.4% | -8 |
| 2018-2019 | 19.454% | 19.454% | 0.000 pts | 9.299% | 9.299% | 0.000 pts | 8.9% | 8.9% | 0 |
| 2020-2021 | 62.430% | 62.430% | 0.000 pts | 27.419% | 27.419% | 0.000 pts | 14.1% | 14.1% | 0 |
| 2022-2023 | 16.300% | 16.300% | 0.000 pts | 7.868% | 7.868% | 0.000 pts | 12.0% | 12.0% | 0 |
| 2024-2026-01-01 | 27.803% | 30.452% | +2.649 pts | 13.037% | 14.202% | +1.165 pts | 10.7% | 10.7% | +7 |

Weekly-return compounding across the five long segments produced:

| Candidate | Weeks | Net | CAGR | Max DD |
|---|---:|---:|---:|---:|
| B behavior | 517 | 294.670% | 14.861% | 13.0% |
| C recovery | 517 | 300.850% | 15.042% | 13.0% |
| Delta | 517 | +6.180 pts | +0.181 pts | 0.0 pts |

This clears the raw `3` long-window net-profit-point threshold, but fails the evidence-quality requirement because the edge is not distributed across at least two distinct windows.

## Return-Gap Map

Candidate C differs from Candidate B only when recovery logic activates or when earlier recovery-driven equity/weight path changes later rebalance timing.

Recovery activations:

| Window | C Recovery Weeks | Result |
|---|---:|---|
| Stress 2021-2022 | 1 | C still loses stress by `0.181` net-profit points and has worse DD/Sharpe/PSR. |
| 2016-2017 | 3 | C loses `0.320` net-profit points. |
| 2018-2019 | 0 | Identical. |
| 2020-2021 | 0 | Identical. |
| 2022-2023 | 3 | Identical headline metrics. |
| 2024-2026-01-01 | 4 | C gains `2.649` net-profit points. |

The 2024-2026 edge traces to July-September 2025:

- B stayed in `drawdown-signals` at `G0.1200/D0.3000/C0.5800` through `2025-09-02`.
- C entered `pre-weak-recovery-confirmed` on `2025-07-07`, `2025-07-14`, `2025-07-21`, and `2025-07-28`, lifting target growth to `G0.1600`.
- By `2025-09-02`, C had recovered enough to return to `base-regime` with target `G0.4500/D0.3000/C0.2500`, while B was still at the defensive drawdown-signals target.
- On `2025-09-08`, C returned `1.966%` versus B `0.414%`, a `+1.552` weekly gap with the same growth symbols (`AVGO,GOOGL,MSFT,NVDA`) but much higher current growth exposure.
- On `2025-09-15`, C added another `+0.356` weekly gap.

This supports the cash-drag / redeployment-timing lever, not a symbol-selection claim.

## Stress-Edge Map

Candidate B remains the stress baseline:

| Metric | Candidate B | Candidate C | C Delta |
|---|---:|---:|---:|
| Net profit | 17.613% | 17.432% | -0.181 pts |
| CAGR | 8.465% | 8.382% | -0.083 pts |
| Drawdown | 12.9% | 13.1% | +0.2 pts worse |
| Sharpe | 0.609 | 0.602 | -0.007 |
| PSR | 31.356% | 30.942% | -0.414 pts |
| Orders | 299 | 301 | +2 |
| Fees | $299.60 | $301.60 | +$2.00 |

The stress-window recovery activation on `2022-04-18` did not produce a stress improvement. This blocks promotion of Candidate C and requires Candidate D to preserve Candidate B's stress behavior.

## Lever Score

| Lever | Decision Value | Implementation Risk | Validation Cost | Evidence Read |
|---|---|---|---|---|
| Post-stress redeployment / cash drag | High | Medium | Medium | Best-supported lever; material 2025 path benefit, but not yet robust across windows. |
| Defensive sleeve quality | Medium | Medium | High | Diagnostics show defensive exposure but not sleeve P&L; needs additional attribution data. |
| Growth selection quality | Medium | Medium | Medium | Some 2016 differences came from selected growth names, but 2025 edge used same names and different exposure timing. |
| Regime timing | High | High | Medium | C changed effective return to base-regime in 2025; same mechanism can harm stress if too early. |
| Replacement/churn behavior | Low | Low | Low | Order, replacement, turnover, and fee deltas are small; not the primary gap source. |

## Next Approved Hypothesis Direction

Design, but do not implement yet, a bounded Candidate D hypothesis around recovery-confirmed redeployment timing:

- Keep Candidate B stress defaults as the baseline.
- Limit any increased growth target to confirmed post-stress recovery states.
- Require a stress-preservation gate against Candidate B 2021-2022 before long-window evaluation.
- Include explicit rejection if 2016 or 2022 activation windows degrade versus Candidate B.
- Add or request sleeve-level contribution diagnostics only if the hypothesis depends on defensive/growth sleeve P&L rather than exposure timing.

Candidate D should not be selected from a broad optimizer or from the 2025 event alone.

## Verification

- Confirmed JSON, orders CSV, and normalized logs exist for all current-source B/C stress and segmented long controls.
- Confirmed weekly diagnostic row counts:
  - stress: `104` rows per candidate;
  - `2016-2017`: `104` rows per candidate;
  - `2018-2019`: `105` rows per candidate;
  - `2020-2021`: `104` rows per candidate;
  - `2022-2023`: `104` rows per candidate;
  - `2024-2026-01-01`: `105` rows per candidate.
- Analysis used local result JSON statistics plus weekly `Eq`, `DD`, `Reason`, `Target`, `Curr`, `PreWeakRecovery`, `OptRepl`, and `NewEntries` diagnostics.
- No trading code was changed.

## Risks And Open Questions

- Segment compounding is diagnostic only because each two-year run starts fresh; it is not a substitute for a single uninterrupted same-execution long run.
- Sleeve contribution cannot be proven from current logs; only exposure, regime, selection, and equity-path effects are available.
- Candidate C's strongest benefit is path-dependent and concentrated in 2025, so it is not enough to choose Candidate D without a pre-registered stress-preserving hypothesis.
