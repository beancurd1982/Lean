---
id: AEGIS-LIVE-2026-06-18-CANDIDATEB-READINESS
type: live-trading-readiness-review
status: reviewed
date: 2026-06-18
topic: AegisGrowthAllocation
tags: [aegis, candidate-b, paper-live, real-money-readiness, interactive-brokers, live-state]
related:
  - project-notes/archive/2026-06/Aegis_TagCandidateB_SameExecution_Stress04_2021_2022_Control_2026-06-17.md
  - project-notes/archive/2026-06/Aegis_TagCandidateB_MinParams_2016_2026-01-01_Confirmation_2026-06-17.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-06-18__paper-live__L-90f2dca70246e18f3fd62743102e72c6__algorithm-log.txt
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-06-18__paper-live__L-90f2dca70246e18f3fd62743102e72c6__orders.csv
---

# Candidate B Paper-Live Readiness Review - 2026-06-18

## Agent Summary
Paper-live logs and orders were reviewed for the active Candidate B deployment `L-90f2dca70246e18f3fd62743102e72c6`. The active segment is operationally clean, but evidence is not sufficient for full real-account deployment. Candidate B is reasonable for small-capital real-money validation after current holdings/equity are checked.

## Evidence Reviewed
- Algorithm log export: `algorithm-log_L-90f2dca70246e18f3fd62743102e72c6.txt`
- Live orders export: `live_orders_1763287545_1781783561_L-90f2dca70246e18f3fd62743102e72c6.csv`
- Files moved from Downloads to `PaperAccountLogs`, but raw paper-live exports were intentionally not committed because they contain account/trading details.

## Active Deployment
- Algorithm version: `AegisGrowthAllocation-2026-06-08-candidate-b-defaults`
- Source revision: `4499311dc`
- Live state key: `AegisGrowthAllocation_LiveState_V2`
- Schema version: `2`
- Brokerage context in log: Interactive Brokers.

## Current Deployment Findings
- Active deployment segment starts at 2026-06-08 09:18:44.
- No runtime errors, exceptions, disconnects, rejected orders, or liquidations appear in the active deployment segment.
- Startup initially saw `BrokerHoldings=0` while the persisted state had 7 holdings, correctly treated as broker snapshot not ready.
- After warmup, broker and persisted holdings matched: AAPL=101, AMZN=113, AVGO=70, GOOGL=80, SCHD=830, SGOV=268, USMV=279.
- Deferred startup reconciliation then saved state after warmup.
- 2026-06-08 weekly review ran and submitted 6 orders.
- All 6 active-deployment orders filled.
- 2026-06-15 weekly review ran with no orders and saved state.

## Active Deployment Orders
All current deployment orders are `Market` orders, not `Market On Close`:

| Time UTC | Symbol | Quantity | Status |
|---|---:|---:|---:|
| 2026-06-08 14:00:00 | AVGO | -70 | Filled |
| 2026-06-08 14:00:05 | USMV | -279 | Filled |
| 2026-06-08 14:00:05 | AAPL | -6 | Filled |
| 2026-06-08 14:00:05 | AMZN | 6 | Filled |
| 2026-06-08 14:00:06 | LLY | 25 | Filled |
| 2026-06-08 14:00:08 | VIG | 112 | Filled |

The `Market` order behavior is live-operationally stable in this sample, but it differs from recent cloud backtest controls that showed `Market On Close` execution.

## Historical Deployment Findings
The combined export includes older deployments. Historical segments show two Interactive Brokers automation timeout events:
- After the 2026-05-18 weekly review.
- After the 2026-06-01 weekly review.

These timeout events do not appear in the active `L-90f2...` Candidate B segment, but they remain relevant for real-money readiness because they show brokerage automation/restart risk has occurred in this paper-live account history.

## Readiness Decision
Candidate B is not yet cleared for full real-account deployment.

Candidate B is reasonable for a small-capital real-money validation if:
- Current live holdings match the expected target holdings.
- Current account equity/cash/margin are acceptable.
- The user accepts that live orders are currently `Market` orders around the weekly review time.
- Automatic restart/notification settings are enabled and tested.

## Remaining Required Inputs
Before a real account launch decision:
- Current holdings snapshot from the paper account.
- Current portfolio value/equity and cash.
- Current open orders, expected to be zero.
- Confirmation of brokerage and account type intended for real deployment.
- Whether the live account should use the current `Market` order behavior or whether order timing must be changed to match `Market On Close` backtests.

## Holdings Snapshot Review - 2026-06-18
User provided screenshots from QuantConnect holdings and Interactive Brokers dashboard.

QuantConnect and Interactive Brokers matched by symbol and quantity for the seven live holdings:
- AAPL
- AMZN
- GOOGL
- LLY
- SCHD
- SGOV
- VIG

Cash also matched between QuantConnect and Interactive Brokers. Exact account values and quantities are omitted from this committed note to avoid publishing account-level details.

Approximate allocation from IB NAV:
- Growth sleeve, AAPL/AMZN/GOOGL/LLY: about 43.6%
- Defensive sleeve, SCHD/SGOV/VIG: about 30.7%
- Cash: about 25.5%

This matches the current Candidate B live target posture from the 2026-06-15 review: approximately `G0.44/D0.31/C0.25`.

## Recommendation
Proceed only with staged deployment:
1. Keep Candidate B as the baseline.
2. Current paper holdings/equity are now validated against IB screenshots.
3. Confirm open orders are zero.
4. If clean, deploy to real account with small capital first.
5. Monitor the first two weekly reviews before scaling.
6. Do not deploy full capital until restart/redeploy behavior is explicitly tested and current holdings are verified after a restart.
