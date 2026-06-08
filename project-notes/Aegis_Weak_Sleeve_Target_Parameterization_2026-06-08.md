# Aegis Weak Sleeve Target Parameterization - 2026-06-08

## Summary
- Implemented a bounded defensive optimization hook for AegisGrowthAllocation.
- Added runtime parameters for normal Weak regime and PreWeakGuard sleeve exposure while preserving current defaults.
- This is an experiment-enabler only; no default allocation behavior should change until backtests prove better values.

## Parameters Added
- `pre-weak-growth-target`, default `0.24`
- `pre-weak-def-target`, default `0.30`
- `weak-growth-target`, default `0.10`

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
