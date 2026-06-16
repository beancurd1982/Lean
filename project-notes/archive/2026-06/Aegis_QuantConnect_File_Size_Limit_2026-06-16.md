---
id: AEGIS-QC-FILE-SIZE-LIMIT-2026-06-16
type: workflow-risk
status: active
date: 2026-06-16
topic: AegisGrowthAllocation
tags: [aegis, quantconnect, cloud-compile, file-size-limit, partial-class, workflow]
related:
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeak_Recovery_v2_Implementation_Plan_2026-06-15.md
  - project-notes/archive/2026-06/Aegis_CandidateC_PreWeak_Recovery_v2_Second_Round_Review_2026-06-15.md
files:
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.LiveState.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/LiveStateStore.cs
  - Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs
---

# Aegis QuantConnect File Size Limit - 2026-06-16

## Agent Summary

QuantConnect Cloud rejected `AegisGrowthAllocation.cs` after the Candidate C recovery implementation because the file exceeded the cloud editor save limit.

Observed cloud error:

```text
File AegisGrowthAllocation.cs not saved. It exceeds the maximum size of 64000 characters by using 68688.
```

This caused cascading compile errors in `AegisGrowthAllocation.LiveState.cs` because cloud saved some updated partial files but rejected the main partial class file that defines shared fields such as `_regimeModel`, `_undeployedCapitalReserve`, `_severeCrash...`, and `_preWeakRecovery...`.

## Impact

- Do not diagnose the follow-on `does not exist in the current context` errors as missing local code.
- The real root cause is a partial upload caused by QuantConnect's per-file character limit.
- Any future Aegis implementation that increases `AegisGrowthAllocation.cs` size risks repeating the same cloud failure.

## Best Fix

Refactor `AegisGrowthAllocation.cs` into smaller `partial class` files before uploading Candidate C to QuantConnect Cloud.

Recommended split:

- `AegisGrowthAllocation.cs`: `Initialize`, `OnData`, and weekly review orchestration.
- `AegisGrowthAllocation.Parameters.cs`: parameter parsing and runtime configuration helpers.
- `AegisGrowthAllocation.Recovery.cs`: Candidate C PreWeak recovery state machine.
- `AegisGrowthAllocation.Diagnostics.cs`: weekly diagnostic formatting and diagnostic attribution tracker.
- Keep `AegisGrowthAllocation.LiveState.cs` as the live-state partial.
- Keep `LiveStateStore.cs` and `StrategyConfig.cs` separate.

## Implemented Split

Implemented locally on 2026-06-16:

| File | Approx chars after split | Purpose |
|---|---:|---|
| `AegisGrowthAllocation.cs` | 30,243 | Fields, initialization, event handlers, weekly review orchestration, portfolio execution, asset/market state classes |
| `AegisGrowthAllocation.Diagnostics.cs` | 24,513 | Weekly diagnostics, stress diagnostics, diagnostic attribution tracker |
| `AegisGrowthAllocation.LiveState.cs` | 14,829 | Live-state restore/build/reconciliation partial |
| `AegisGrowthAllocation.Parameters.cs` | 7,554 | Backtest dates, runtime parameter parsing/configuration |
| `AegisGrowthAllocation.Recovery.cs` | 8,375 | PreWeak recovery, weak/pre-weak/severe-crash override state logic |

All Aegis partial files are comfortably below QuantConnect's 64,000-character save limit.

## Upload Rule

After the split, upload all production Aegis files together:

```text
Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs
Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.Parameters.cs
Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.Recovery.cs
Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.Diagnostics.cs
Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.LiveState.cs
Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/LiveStateStore.cs
Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs
```

Do not upload local test files to QuantConnect Cloud.

## Verification Needed

After refactoring:

- Confirm each uploaded Aegis source file is below QuantConnect's 64,000-character limit.
- Run local compile:

```powershell
dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -nologo
dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo -maxcpucount:1 -nodeReuse:false -p:BuildInParallel=false -p:UseSharedCompilation=false -clp:ErrorsOnly
```

- Compile in QuantConnect Cloud before running any backtest.

Local verification after implementation:

- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -nologo -clp:ErrorsOnly` passed.
- `dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo -maxcpucount:1 -nodeReuse:false -p:BuildInParallel=false -p:UseSharedCompilation=false -clp:ErrorsOnly` passed.
- `git diff --check` passed.

## Open Risks

- QuantConnect may have other editor-side limits besides character count; keep new partial files comfortably below 64,000 characters rather than just under the threshold.
- A partial upload can leave the cloud project in a mixed-source state. If cloud compile errors look inconsistent with local compile, first verify every production Aegis file saved successfully in cloud.
- QuantConnect parameter names are limited to 30 characters. Candidate C recovery cloud parameter keys were shortened to:
  - `preweak-recovery-enabled`
  - `preweak-rec-growth-target`
  - `preweak-rec-def-target`
  - `preweak-rec-dd-improve`
  - `preweak-rec-max-dd`
  - `preweak-rec-confirm-weeks`
- QuantConnect Cloud backtest parameter UI allows at most 20 parameters. Do not enter every default-valued Aegis parameter. For Candidate C v2 default-value validation, rely on source defaults and only enter parameters that differ from defaults or define the test window. Minimal Candidate C stress-window parameters:
  - `backtest-start = 2021-01-01`
  - `backtest-end = 2022-12-31`
  - `crisis-diagnostics = true`
  - `preweak-recovery-enabled = true`
