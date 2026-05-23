# Aegis Deployment Identity Metadata - 2026-05-23

## Step 1: Scope

User approved adding deployment/version identity before live deployment.

Goal:
- Make live logs and ObjectStore state show which algorithm release/source revision produced the run.
- Improve future compatibility audits before deploying new code over old ObjectStore state.

Design decision:
- Add a stable release-style `AlgorithmVersion` constant.
- Add a `SourceRevision` constant that can be updated at release/deploy time.
- Persist both fields in `AegisLiveState`.
- Include both fields in startup and live-state restore/save logs.

Important constraint:
- A commit cannot reliably contain its own final git SHA as a constant because changing the constant changes the commit hash. Therefore `SourceRevision` should be treated as a release/deploy metadata field, not self-generating magic.

Planned files:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/LiveStateStore.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`
- `project-notes/Aegis_Deployment_Identity_Metadata_2026-05-23.md`

## Step 2: TDD Red Check

Tests added first:
- `BuildPersistedStateIncludesDeploymentIdentity`
- `LiveStateFingerprintChangesWhenDeploymentIdentityChanges`

Expected red result:
- `dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo -v:minimal /clp:ErrorsOnly` failed because:
  - `AegisLiveState` did not contain `AlgorithmVersion`
  - `AegisLiveState` did not contain `SourceRevision`
  - `StrategyConfig` did not contain `AlgorithmVersion`
  - `StrategyConfig` did not contain `SourceRevision`

## Step 3: Implementation

Files changed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/LiveStateStore.cs`
- `Tests/Algorithm/AegisGrowthAllocationTests.cs`

Implementation summary:
- Added `StrategyConfig.AlgorithmVersion = "AegisGrowthAllocation-2026-05-23-optstress-defaults"`.
- Added `StrategyConfig.SourceRevision = "eb3145647"`.
- Added `AlgorithmVersion` and `SourceRevision` to `AegisLiveState`.
- `BuildPersistedState()` now stores both metadata fields.
- `LiveStateStore.NormalizeState()` fills missing metadata:
  - missing algorithm version becomes the current `StrategyConfig.AlgorithmVersion`
  - missing source revision becomes `unknown`
- Live-state fingerprint now includes both metadata fields.
- Startup logs now include algorithm version, source revision, live-state key, and schema version.
- State restore/save logs now include algorithm version and source revision.

Important limitation:
- `SourceRevision` records the source revision used when this change was made. It cannot automatically be the final commit SHA of the same commit. For future release discipline, update this value intentionally when cutting a deployable release or use a release tag that maps to a GitHub commit.

## Step 4: Verification And Review

Verification:
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo --no-restore -v:minimal /clp:ErrorsOnly` passed with zero errors.
- `dotnet build Tests/QuantConnect.Tests.csproj --no-restore -nologo -v:minimal /clp:ErrorsOnly` passed with zero errors.
- `git diff --check` passed.
- `AegisGrowthAllocation.cs` size is `62,425` characters, below the cloud upload limit.

Targeted test status:
- `dotnet test Tests/QuantConnect.Tests.csproj --filter "BuildPersistedStateIncludesDeploymentIdentity|LiveStateFingerprintChangesWhenDeploymentIdentityChanges|UsesPromotedOptStressDefaultParameters" --no-build -nologo` aborted due to the known local test-host issue:
  - missing `../../../Data/equity/sgx/map_files`
  - Python.NET GIL finalizer crash
- No assertion failure was observed because the host aborts before reporting test results.

Strict review:
- This change does not modify order placement, target generation, portfolio construction, or broker reconciliation.
- ObjectStore schema number and key remain unchanged, so this is additive metadata for V2 state.
- Existing V2 state without the new fields can still deserialize; normalization fills missing metadata.
- Existing V1 state remains unsupported because the current code only reads `AegisGrowthAllocation_LiveState_V2` and rejects schema mismatches.
- The metadata fields make future live deployment audits easier, but they are not a full object-store migration framework.
