# Aegis Live State Startup Diagnostics - 2026-05-23

## Context
- After redeploying `AegisGrowthAllocation`, live logs reported no persisted V2 state and saved `Holdings=0`.
- QuantConnect Holdings UI showed existing IB paper holdings: AAPL, AVGO, COST, GOOGL, JNJ, SCHD, SGOV.
- User cannot download the Object Store JSON because of account limitations.

## Risk
- Startup reconciliation may run before broker holdings are fully reflected in `Portfolio[...]`.
- If the algorithm immediately saves an empty V2 state, the Object Store audit snapshot can become misleading.
- Trading decisions still use live `Portfolio[...]` at review time, but restart diagnostics and persisted state quality are weakened.

## Planned Change
- Add live diagnostic logs that show persisted holdings and current portfolio holdings by ticker at startup, warmup completion, and first live `OnData`.
- Defer the first startup state save when both persisted and captured broker holdings are empty.
- Re-attempt the deferred save after warmup, when live portfolio state is more likely to be populated.

## Files To Touch
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.LiveState.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`

## Implementation
- Made `AegisGrowthAllocation` partial and moved live-state persistence/reconciliation helpers into `AegisGrowthAllocation.LiveState.cs` to keep the main cloud file below the 64,000 character limit.
- Added `[AEGIS-LIVE-DIAG]` logs for startup, warmup completion, and first live `OnData`.
- Diagnostic logs include persisted holdings count/tickers, broker portfolio holdings count/tickers, persisted open orders, and broker open orders.
- Deferred the initial startup state save when both persisted state and broker-captured state are empty.
- Re-attempted the deferred save at warmup completion and first live `OnData`; save occurs only once holdings or open orders are visible.
- Added tests for holdings summary formatting and deferred-save decision behavior.
- Updated the deployment identity label to `AegisGrowthAllocation-2026-05-23-live-state-diagnostics` with source baseline `4c59639af` so redeploy logs identify this diagnostic build.

## Live Evidence From Redeploy
- Startup restored persisted V2 state with 7 holdings: AAPL, AVGO, COST, GOOGL, JNJ, SCHD, SGOV.
- During `Initialize`, broker-captured holdings were still 0.
- Existing logic treated the mismatch as broker state winning and saved `Holdings=0`, overwriting the good persisted snapshot.
- At `WarmupFinished`, broker-captured holdings became visible with 7 holdings.

## Follow-Up Fix
- Treat `persisted holdings > 0` with `startup broker holdings == 0` as a not-yet-ready broker snapshot.
- Defer startup state save in that case instead of saving empty holdings.
- Allow warmup completion or first live `OnData` to save once broker holdings become visible.
- Updated the deployment identity label to `AegisGrowthAllocation-2026-05-23-live-state-diagnostics-v2` for the corrected diagnostic build.

## Verification
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo --no-restore -v:minimal /clp:ErrorsOnly` passed.
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo --no-restore -v:minimal /clp:ErrorsOnly` passed.
- `dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo -v:minimal /clp:ErrorsOnly` passed.
- Targeted `dotnet test` remains blocked by the existing local test-host crash: missing `../../../Data/equity/sgx/map_files` followed by Python.NET GIL finalizer failure.
- `AegisGrowthAllocation.cs` length after splitting: 53,992 characters, below the QuantConnect cloud 64,000 character limit.
- Follow-up fix verification repeated after the persisted-holdings/broker-empty guard: Debug build passed, Release build passed, tests project build passed, and targeted test execution remains blocked by the same local test-host crash.

## Strict Review
- Live trading behavior impact: the weekly trading decision path is unchanged.
- Persistence behavior impact: startup no longer writes an empty V2 snapshot when both persisted and broker-captured state are empty during `Initialize()`.
- Persistence follow-up impact: startup also no longer overwrites a non-empty persisted V2 snapshot when broker holdings are temporarily unavailable during `Initialize()`.
- Remaining risk: if live `Portfolio[...]` never exposes the IB holdings, diagnostics will show broker holdings as empty at all phases and the root cause will remain outside Object Store serialization.
- Remaining risk: if an account is intentionally empty, the startup state save may be deferred until a later event; this is acceptable because there is no holdings/open-order state to preserve.

## Open Questions
- Whether QuantConnect live `Portfolio[...]` is populated before or after `Initialize()` for IB paper accounts with existing positions.
- Whether VIX live index data updates reliably despite the cloud warning.
