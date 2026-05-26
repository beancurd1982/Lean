# Aegis Operational Hardening Plan - 2026-05-26

## Summary
- Keep current IB paper-live trading behavior unchanged while improving observability and local validation.
- Implement the next work in three phases:
  - live-state log wording cleanup
  - VIX live data verification diagnostics
  - local Aegis test environment repair or isolation
- Do not change allocation logic, regime thresholds, Object Store schema/key, or the stable tag.

## Phase 1 - Live-State Log Accuracy
- Update the startup mismatch log in `AegisGrowthAllocation.LiveState.cs` so it matches actual behavior.
- If persisted holdings exist and broker holdings are temporarily missing, log that the broker snapshot is not ready and persisted state is retained until warmup or first live data.
- Only log `Broker state wins` when broker state is accepted immediately and can be saved.
- Extract the reconciliation wording into a small helper that can be tested without live brokerage plumbing.
- Acceptance:
  - deferred startup case no longer logs `Broker state wins`
  - behavior of the holdings-sync guard is unchanged
  - no Object Store key or schema change

## Phase 2 - VIX Live Data Verification
- Add diagnostics only; do not replace VIX or add fallback behavior in this phase.
- Emit compact `[AEGIS-STRESS-DIAG]` logs in live mode at warmup completion and before each weekly regime calculation.
- Include stress symbol, stress window count, latest observed stress close, 5-day average when ready, and whether stress data is usable for the weekly review.
- Keep existing weekly review behavior unchanged: if stress data is not ready, continue skipping as the algorithm does today.
- Acceptance:
  - paper-live logs prove whether VIX values populate after warmup and before weekly review
  - if VIX does not populate, create a separate approved plan before changing the stress data source

## Phase 3 - Local Aegis Test Environment
- First try the lowest-risk validation path: run existing Aegis tests through `dotnet vstest` with explicit test names.
- If `dotnet vstest` works, document the command as the recommended local Aegis test command.
- If it still fails, investigate the missing `../../../Data/equity/sgx/map_files` dependency and isolate or stub the minimum local test data required by the shared QuantConnect test host.
- Do not change production algorithm behavior to fix the test host.
- Acceptance:
  - focused Aegis tests can run locally without the SGX map-file/Python.NET finalizer crash, or the reliable alternate command is documented

## Out Of Scope
- No allocation behavior changes.
- No regime-threshold changes.
- No VIX replacement or fallback until diagnostics prove live VIX is unavailable and the change is separately approved.
- No Object Store key or schema change.
- No change to `aegis-growth-allocation-v2026.05.25-paper-live-stable`.

## Verification
- Documentation-only setup commit:
  - `git diff --check`
- Future implementation phases:
  - `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo --no-restore -v:minimal /clp:ErrorsOnly`
  - `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo --no-restore -v:minimal /clp:ErrorsOnly`
  - `dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo -v:minimal /clp:ErrorsOnly`
  - focused Aegis test command from Phase 3
- Paper-live validation:
  - startup deferred holdings case logs accurate wording
  - warmup or first live data saves holdings correctly
  - stress diagnostics show whether VIX is live-ready

## Implementation Status
- 2026-05-26: Implemented Phase 1 log wording cleanup and Phase 2 live stress diagnostics.
- 2026-05-26: Phase 3 identified a reliable focused local command pattern using simple NUnit names with `dotnet vstest`.
- Implementation evidence is recorded in `project-notes/Aegis_Operational_Hardening_Implementation_2026-05-26.md`.
