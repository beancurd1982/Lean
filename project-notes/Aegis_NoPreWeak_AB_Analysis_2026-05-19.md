# Aegis No-Pre-Weak A/B Analysis - 2026-05-19

## Step 1: Intake

Date:
- 2026-05-19

Scope:
- Analyze the uploaded `NoPreWeak_2016-2026` backtest result set and compare it against the current default diagnostic summary run.

Uploaded files:
- `NoPreWeak_2016-2026.json`
- `NoPreWeak_2016-2026_orders.csv`
- `NoPreWeak_2016-2026_logs.txt`

Normalized log:
- `2026-05-19_222629__AegisGrowthAllocation__NoPreWeak_2016-2026_logs.txt`

Comparison target:
- `DiagSummary_2016-2026`, which represents current default behavior with `pre-weak-guard-enabled=true`.

Initial question:
- Does disabling pre-weak guard improve long-term return enough to justify the higher drawdown risk, or should the guard remain enabled by default?

## Step 2: Findings

Date:
- 2026-05-19

Diagnostic summary:
- The no-pre-weak log contains `[AEGIS-DIAG-SUMMARY]`.
- `Weeks=522`
- `PreWeakWeeks=0`
- `NonPreWeakWeeks=522`
- `SevereCrashWeeks=0`
- `WeakRegimeWeeks=65`
- `NonPreWeakAvgDrawdown=0.0365`
- `NonPreWeakNextReturnAvg=0.0035`
- `NonPreWeakAvgTarget=G0.4934/D0.2690/C0.2376`

Current default with pre-weak guard:
- CAGR: 18.421%
- Max drawdown: 13.800%
- Net profit: 443.042%
- End equity: $162,912.56
- Total orders: 1,594
- Total fees: $1,890.01
- Orders CSV SHA-256 prefix: `92efd53aea1de4d5`

No-pre-weak result:
- CAGR: 18.868%
- Max drawdown: 16.500%
- Net profit: 463.914%
- End equity: $169,174.07
- Total orders: 1,540
- Total fees: $1,851.50
- Orders CSV SHA-256 prefix: `423592160d1110ed`

A/B delta:
- Disabling pre-weak improves CAGR by 0.447 percentage points.
- Disabling pre-weak improves end equity by $6,261.51 over the full 2016-2026 backtest.
- Disabling pre-weak worsens max drawdown by 2.7 percentage points.
- Disabling pre-weak reduces orders by 54 and fees by $38.51.

Year-by-year return delta, current default minus no-pre-weak:
- 2016: -1.9206 percentage points
- 2017: +0.4801 percentage points
- 2018: -1.0323 percentage points
- 2019: -1.5778 percentage points
- 2020: +0.8456 percentage points
- 2021: -0.1598 percentage points
- 2022: +2.8056 percentage points
- 2023: -3.4703 percentage points
- 2024: -1.1852 percentage points
- 2025: -0.4981 percentage points

Important isolation result:
- `NoPreWeak_2016-2026` exactly matches the prior `Base` run on metrics and orders hash.
- This means the live-state persistence work and compact diagnostic logging are behavior-neutral in this backtest.
- The whole performance difference between the current default run and the base/no-pre-weak run is attributable to the pre-weak guard.

Interpretation:
- The pre-weak guard is doing its defensive job: it cuts maximum drawdown from 16.5% to 13.8%.
- The cost is modest but real: about 0.447 percentage points of annual return and $6.26k ending equity on a $30k starting account.
- The guard helped in 2020 and especially 2022, but it lagged in several strong/rebound years, especially 2023.
- Because pre-weak weeks had positive 4/8/12-week forward returns in the prior diagnostic summary, the current guard is likely too conservative during some recoverable weakness periods.

Recommendation:
- Keep pre-weak enabled as the live default if the goal is a more defensive paper/live strategy.
- Do not remove it, because the drawdown reduction is material and the CAGR cost is not extreme.
- Next optimization should tune pre-weak aggressiveness, not add another overlay.

Suggested next experiment:
- Test a softer pre-weak sleeve that keeps the guard but gives back less upside.
- Candidate target: growth 0.30, defensive 0.30, cash 0.40 instead of current growth 0.24, defensive 0.30, cash 0.46.
- Candidate threshold test: raise pre-weak activation drawdown threshold from 5% to 7% to reduce false-positive activations.

Strict review:
- No code changes were made during this analysis.
- The analyzed no-pre-weak result set is complete: JSON, orders CSV, and normalized log are present.
- The log-index status should be updated from `unreviewed` to `reviewed` for the normalized no-pre-weak log.

## Step 3: Verification

Date:
- 2026-05-19

Checks:
- Confirmed the normalized no-pre-weak log contains `[AEGIS-DIAG-SUMMARY]`.
- Confirmed `NoPreWeak_2016-2026` matches `Base` on metrics and orders hash.
- Updated `log-index.csv` to mark the normalized no-pre-weak log as reviewed and linked to this note.
