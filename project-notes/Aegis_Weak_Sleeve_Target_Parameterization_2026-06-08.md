# Aegis Weak Sleeve Target Parameterization - 2026-06-08

## Summary
- Implemented a bounded defensive optimization hook for AegisGrowthAllocation.
- Added runtime parameters for normal Weak regime and PreWeakGuard sleeve exposure while preserving current defaults.
- This is an experiment-enabler only; no default allocation behavior should change until backtests prove better values.

## Parameters Added
- `pre-weak-growth-target`, default `0.12` after Candidate B promotion
- `pre-weak-def-target`, default `0.30`
- `weak-growth-target`, default `0.10`
- `pre-weak-dd-threshold`, default `0.04` after Candidate B promotion

## Scope Boundaries
- PreWeak cash is calculated as `1 - pre-weak-growth-target - pre-weak-def-target`.
- Weak defensive target remains fixed at `0.40`; Weak cash is calculated as `1 - weak-growth-target - 0.40`.
- Severe Crash Override targets were not changed.
- Weak Stress Overlay targets were not changed.
- Object Store key/schema, live-state persistence, order handling, and deployment identity were not changed.

## Validation Plan
- First pilot should use only three combinations:
  - Combo A: `0.24 / 0.30 / 0.10`
  - Combo B: `0.18 / 0.35 / 0.10`
  - Combo C: `0.12 / 0.40 / 0.05`
- Run each across the five standard windows:
  - `2007-10-01` to `2008-12-31`
  - `2009-01-01` to `2010-12-31`
  - `2019-07-01` to `2020-12-31`
  - `2021-01-01` to `2022-12-31`
  - `2023-01-01` to `2026-01-01`

## Promotion Criteria
- Improve 2008 or 2021-2022 drawdown/bleed.
- Do not materially weaken 2009-2010 or 2019-2020 recovery.
- Keep 2023-2026 PSR, Sharpe, and CAR close to current default.
- Do not materially increase orders or fees.
- Promote only on composite robustness, not one favorable window.

## Pilot Result - Combo A Stress01
- Window: `2007-10-01` to `2008-12-31`.
- Files:
  - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_ComboA_RSG10_Stress01_2007-10_2008-12.json`
  - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/ManualGrid_WeakSleeve_ComboA_RSG10_Stress01_2007-10_2008-12_orders.csv`
  - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/2026-06-08_201717__AegisGrowthAllocation__ManualGrid_WeakSleeve_ComboA_RSG10_Stress01_2007-10_2008-12_logs.txt`
- Verified parameters from log:
  - `ReplacementScoreGap=10`
  - `HoldStabilityBonus=2`
  - `ToleranceBandScale=1.0`
  - `PreWeakGrowthTarget=0.24`
  - `PreWeakDefensiveTarget=0.30`
  - `WeakGrowthTarget=0.10`
- Result: PSR `0.149%`, Sharpe `-1.457`, Sortino `-1.421`, CAR `-13.649%`, Net Profit `-16.808%`, Drawdown `20.300%`, Total Orders `145`, End Equity `$24,957.47`.
- Comparison: identical headline metrics to the earlier Stress01 RSG12/default-sleeve reference, so Combo A did not improve the 2007-2008 crash window.
- Diagnostic implication: PreWeak was active for only `9` of `66` weeks, and forward diagnostics were poor (`PreWeakFwd4WinRate=0.0000`), so changing PreWeak sleeve weights alone is unlikely to fix this window.
- Next step: continue the pilot with Combo B on the same Stress01 window before expanding to all five windows. If Combo B also matches or underperforms, the higher-impact target is likely PreWeak trigger quality/timing rather than sleeve sizing.

## Candidate A vs Candidate B - Five-Window Validation
- Candidate A:
  - `pre-weak-dd-threshold=0.05`
  - `pre-weak-growth-target=0.12`
  - `pre-weak-def-target=0.30`
  - `weak-growth-target=0.10`
- Candidate B:
  - `pre-weak-dd-threshold=0.04`
  - `pre-weak-growth-target=0.12`
  - `pre-weak-def-target=0.30`
  - `weak-growth-target=0.10`
- Both candidates were tested across the five standard windows after renaming the drawdown threshold parameter to the cloud-safe `pre-weak-dd-threshold`.

| Window | Candidate A Sharpe / Net / DD | Candidate B Sharpe / Net / DD | Read |
| --- | --- | --- | --- |
| 2007-10-01 to 2008-12-31 | `-1.417` / `-15.911%` / `19.4%` | `-1.444` / `-14.627%` / `18.2%` | B improves net and drawdown; A has slightly better Sharpe. |
| 2009-01-01 to 2010-12-31 | `0.992` / `26.713%` / `9.6%` | `0.865` / `22.731%` / `9.6%` | A is clearly better in recovery. |
| 2019-07-01 to 2020-12-31 | `1.464` / `47.010%` / `13.7%` | `1.464` / `47.010%` / `13.7%` | Tie. |
| 2021-01-01 to 2022-12-31 | `0.684` / `19.534%` / `12.6%` | `0.695` / `19.808%` / `12.4%` | B is slightly better. |
| 2023-01-01 to 2026-01-01 | `0.712` / `64.972%` / `12.0%` | `0.989` / `83.901%` / `10.8%` | B is materially better. |

Aggregate read:
- Candidate A sum net profit across windows: `142.318%`; average Sharpe: `0.487`; average drawdown: `13.46%`; total orders: `1483`.
- Candidate B sum net profit across windows: `158.823%`; average Sharpe: `0.514`; average drawdown: `12.94%`; total orders: `1474`.
- Candidate B is the stronger composite candidate despite hurting the 2009-2010 recovery window.
- Candidate B was accepted as the new default on 2026-06-08:
  - `DefaultPreWeakGuardDrawdownThreshold=0.04`
  - `DefaultPreWeakGrowthTarget=0.12`
  - `DefaultPreWeakDefensiveTarget=0.30`
  - `DefaultWeakGrowthTarget=0.10`
- Residual risk: Candidate B hurts the 2009-2010 recovery window by about `3.982%` net profit versus Candidate A, so future tuning should watch recovery drag.

## Verification
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -nologo` completed with 0 errors.
- `dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo -clp:ErrorsOnly` completed with 0 errors.
- Targeted `dotnet test` could not complete because the local Lean test host crashed before executing tests due the existing SGX map-file/Python GIL environment issue:
  - missing `../../../Data/equity/sgx/map_files`
  - `GIL must always be released, and it must be released from the same thread that acquired it`

## Review Notes
- Defaults are preserved through constants and reset/runtime parameter paths.
- Invalid sleeve target combinations fall back to defaults.
- Initialization logs now include parsed sleeve target values for cloud verification.
- Residual risk: automated NUnit assertions could not be executed locally until the local test environment is repaired.
