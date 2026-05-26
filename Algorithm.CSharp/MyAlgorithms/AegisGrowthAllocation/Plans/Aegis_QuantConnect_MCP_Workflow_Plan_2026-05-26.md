# Aegis QuantConnect MCP Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a safer, direct QuantConnect cloud workflow so Aegis backtests, optimizations, logs, orders, and live diagnostics can be fetched and analyzed without screenshot/manual-file handoff.

**Architecture:** Start with read-only connectivity and normalized ingestion before any cloud mutation. Prefer the official QuantConnect MCP server for interactive agent workflows when available; keep a REST API provider as the executable repo-local fallback because it is easier to test, dry-run, and audit. Raw cloud fetch, normalization, reporting, and cloud-write launch commands are separate boundaries. All cloud-write actions require explicit typed human confirmation and project-note audit entries.

**Tech Stack:** QuantConnect MCP server or QuantConnect REST API, PowerShell/.NET-compatible local scripts, JSON/CSV normalized artifacts, existing `project-notes/` workflow logs, Aegis `BackTestLogs` and `CrisisBackTestLogs` conventions.

---

## Scope

This plan covers the five work items needed to replace the current screenshot/upload workflow with a direct, auditable cloud-data workflow:

1. Establish secure QuantConnect connectivity and credentials handling.
2. Prove read-only cloud discovery through MCP first, REST fallback second.
3. Normalize backtest, optimization, order, and log data into repo-local artifacts.
4. Add repeatable analysis/report generation for Aegis comparisons.
5. Add guarded cloud operations for launching backtests/optimizations only after the read-only path is trusted.

## Out Of Scope

- No allocation logic changes in `AegisGrowthAllocation`.
- No live brokerage order placement automation.
- No Object Store write/delete automation.
- No automatic live deploy/stop actions.
- No credentials committed to the repository.
- No replacement of the current manual cloud UI workflow until read-only verification is complete.

## Artifact Boundaries

- `CloudWorkflow/raw/`: ignored local storage for raw MCP/API responses and temporary fetch output. Raw files are not committed by default.
- `CloudWorkflow/samples/`: committed sanitized examples only. Samples must remove credentials, user identifiers, brokerage account data, raw Object Store payloads, and any account-identifying metadata that is not required for analysis.
- `CloudWorkflow/output/`: normalized, redacted, schema-validated artifacts and `cloud-result-index.csv`.
- `CloudWorkflow/reports/`: derived markdown and CSV comparison reports.
- `BackTestLogs` and `CrisisBackTestLogs`: existing manual-analysis folders. Cloud imports update these only through an explicit import task; they are not the primary cloud workflow storage.

## Provider Boundary

- `McpInteractive`: preferred read-only provider for direct assistant workflows when QuantConnect MCP tools are available.
- `RestApi`: executable repo-local fallback provider for repeatable PowerShell workflows.
- The normalizer must depend on raw files, not directly on MCP tools or REST calls.
- Phase 5 does not require both providers to work. It requires one supported provider to complete the read-only end-to-end gate, while the unavailable provider is documented as a blocker or fallback limitation.

## Credential And Redaction Requirements

- No credentials in repo files, project notes, command transcripts, screenshots, normalized artifacts, generated reports, or committed samples.
- Prefer environment variables or an OS/user secret store. Ignored local config is allowed only for non-shared local development and must never be copied into project notes.
- Use least-privilege/read-only access if QuantConnect supports it. If only full-access credentials are available, document that they are used only for read calls until Phase 5.
- Scripts must not print secrets. Failed authentication must not write raw response bodies into project notes.
- Persisted holdings may be summarized for diagnostics, but raw Object Store JSON, brokerage account identifiers, and account-level data must not be committed.

## Phase 1: Connectivity, Credential Safety, And Access Model

**Objective:** Decide the supported access path and prevent unsafe handling of QuantConnect credentials before any API call is automated.

**Files:**
- Create: `project-notes/Aegis_QuantConnect_MCP_Workflow_Implementation_YYYY-MM-DD.md`
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/README.md`
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/.gitignore`
- Optional create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/appsettings.example.json`

**Tasks:**

- [ ] Record the implementation start in a new project note before changing files.
- [ ] Confirm which connection mode is available in this Codex environment: official QuantConnect MCP, QuantConnect Local Platform MCP endpoint, Docker MCP server, or REST API fallback.
- [ ] Create `CloudWorkflow/README.md` explaining the supported connection modes, side-effect classes, artifact boundaries, and credential/redaction rules.
- [ ] Create `CloudWorkflow/.gitignore` that ignores local secrets, raw API/MCP responses, temporary fetch output, and ad hoc scratch exports.
- [ ] Add `appsettings.example.json` only with non-secret placeholders. Treat `projectId` and `organizationId` as account-identifying metadata and use fake example values.
- [ ] Define provider action classes: `read-only`, `cloud-write`, `forbidden-live`, and `forbidden-object-store`.
- [ ] Run `git diff --check`.
- [ ] Review Phase 1 for credential leakage and document the review result in the implementation note.
- [ ] Commit Phase 1 separately with message `docs: define Aegis cloud workflow access model`.

**Acceptance Checks:**

- `git status --short` shows no secret-bearing file staged.
- Searching the diff for `token`, `password`, `apiKey`, `userId`, `organizationId`, and `projectId` finds only fake placeholders or redaction guidance.
- The README clearly says read-only access must be proven before any cloud-write command is added.

## Phase 2: Read-Only QuantConnect MCP Proof Of Connectivity

**Objective:** Prove that Codex can list QuantConnect projects and read one known Aegis backtest without changing cloud state.

**Files:**
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/connection-check.md`
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/providers/README.md`
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/Get-AegisCloudRawResult.ps1` if REST fallback is selected for executable fetches.
- Create or modify only if needed: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/README.md`
- Modify: `project-notes/Aegis_QuantConnect_MCP_Workflow_Implementation_YYYY-MM-DD.md`

**Tasks:**

- [ ] Record the Phase 2 start in the implementation note.
- [ ] Try the official QuantConnect MCP read tools first: list projects, list backtests for the Aegis project, read one backtest summary, and read its orders if the tool exists.
- [ ] If MCP is unavailable, document the blocker and use the QuantConnect REST API read endpoints as the fallback path.
- [ ] Document the selected provider in `CloudWorkflow/providers/README.md`, including whether it is `McpInteractive` or `RestApi`.
- [ ] Capture pre-read counts where available: backtest count, optimization count, live algorithm count/status, and Object Store key count.
- [ ] Fetch one raw backtest summary and one raw orders response into ignored `CloudWorkflow/raw/` storage.
- [ ] Save the exact successful read-only commands or MCP tool names in `CloudWorkflow/connection-check.md`.
- [ ] Save one sanitized sample response shape under `CloudWorkflow/samples/`, with account identifiers and sensitive values removed.
- [ ] Capture post-read counts and confirm no cloud mutation happened: no new backtest, no optimization, no live algorithm change, no Object Store change.
- [ ] Run `git diff --check`.
- [ ] Review Phase 2 for accidental state mutation risk and document the result.
- [ ] Commit Phase 2 separately with message `docs: record Aegis QuantConnect read-only connectivity`.

**Acceptance Checks:**

- The project can be discovered directly from QuantConnect cloud.
- At least one backtest summary can be read without screenshots or manual download.
- One raw backtest summary and one raw orders response are stored only under ignored local storage.
- One sanitized committed sample proves the response shape without leaking credentials or account data.
- The fallback path is documented if MCP is unavailable.

## Phase 3: Normalized Result Ingestion Pipeline

**Objective:** Convert QuantConnect cloud responses into stable local artifacts that match the existing Aegis analysis workflow.

**Files:**
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/Normalize-AegisCloudResult.ps1`
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/Import-AegisCloudResult.ps1`
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/schema/aegis-cloud-result.schema.json`
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/output/README.md`
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/output/cloud-result-index.csv`
- Modify: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/log-index.csv` only when importing crisis runs.
- Modify: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv` only when importing standard runs.
- Modify: `project-notes/Aegis_QuantConnect_MCP_Workflow_Implementation_YYYY-MM-DD.md`

**Tasks:**

- [ ] Record the Phase 3 start in the implementation note.
- [ ] Define a normalized JSON shape containing `schemaVersion`, `sourceProvider`, `sourceRunId`, `sourceFetchedAtUtc`, `sourceRevision`, source path, period, parameters, `parameterHash`, `rawMetricUnits`, headline statistics, benchmark metrics, orders summary, log summary, diagnostics, `parentOptimizationId`, `optimizationRank`, `dataQualityWarnings`, and import timestamp.
- [ ] Support separate raw input types: backtest summary, orders, logs, optimization table rows, and optimization child backtests.
- [ ] Define normalized artifact names:
  - Backtest: `YYYY-MM-DD__cloud-backtest__<period-label>__AegisGrowthAllocation__run-<id>__params-<label>.json`
  - Optimization row: `YYYY-MM-DD__cloud-optimization__opt-<id>__rank-<nnn>__<period-label>__params-<label>.json`
  - Orders: `YYYY-MM-DD__cloud-orders__<period-label>__run-<id>__params-<label>.csv`
  - Logs: `YYYY-MM-DD__cloud-logs__<period-label>__run-<id>__params-<label>.txt`
- [ ] Define `cloud-result-index.csv` fields: `imported_at`, `source_type`, `source_provider`, `qc_project_id`, `qc_backtest_id`, `qc_optimization_id`, `optimization_rank`, `period_start`, `period_end`, `period_label`, `parameter_set`, `parameter_hash`, `source_revision`, `artifact_path`, `report_path`, `status`, and `review_note`.
- [ ] Write a failing parser/normalizer test or validation script using one sanitized sample response.
- [ ] Implement `Normalize-AegisCloudResult.ps1` so it accepts raw file paths and writes normalized JSON plus optional orders CSV without making cloud calls.
- [ ] Implement `Import-AegisCloudResult.ps1` so it copies or registers normalized output, updates `cloud-result-index.csv`, and optionally mirrors artifacts into `BackTestLogs` or `CrisisBackTestLogs` only when explicitly requested.
- [ ] Make re-import idempotent by checking `sourceRunId`, `parameterHash`, and artifact path before writing.
- [ ] Run the validation script against the sanitized sample.
- [ ] Run `git diff --check`.
- [ ] Review Phase 3 for schema drift, naming consistency, and accidental overwrite risk; document the result.
- [ ] Commit Phase 3 separately with message `feat: add Aegis cloud result normalization`.

**Acceptance Checks:**

- A cloud backtest response can become a normalized artifact without manual screenshots.
- Existing analysis scripts or manual review can identify period, parameters, and key metrics from the normalized artifact.
- Re-importing the same run is idempotent or clearly refuses to overwrite without confirmation.
- The primary cloud workflow index is `CloudWorkflow/output/cloud-result-index.csv`, not the legacy manual log indexes.

## Phase 4: Automated Aegis Analysis And Comparison Reports

**Objective:** Generate consistent reports comparing baseline, defensive, optimization, and live-paper runs from normalized cloud artifacts.

**Files:**
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/Analyze-AegisCloudResults.ps1`
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/reports/README.md`
- Create generated reports under: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/reports/`
- Modify: `project-notes/Aegis_QuantConnect_MCP_Workflow_Implementation_YYYY-MM-DD.md`

**Tasks:**

- [ ] Record the Phase 4 start in the implementation note.
- [ ] Define the required comparison columns: period, run id, parameter set, parameter hash, source revision, CAGR, Sharpe, Sortino, drawdown, PSR, net profit, order count, turnover if available, win rate, benchmark CAGR, benchmark drawdown, diagnostic flags, and notes.
- [ ] Add a report command that reads normalized artifacts and writes a markdown report plus CSV summary.
- [ ] Add explicit ranking modes: highest Sharpe, highest CAGR with max drawdown cap, lowest drawdown with minimum CAGR, and best balanced score.
- [ ] Add baseline/current delta columns for CAGR, Sharpe, Sortino, drawdown, PSR, turnover/order count, and win rate.
- [ ] Include a section that identifies parameter combinations that are indistinguishable because multiple runs produce the same statistics.
- [ ] Add a `Deployment Decision` section with one of `Recommended`, `Rejected`, or `Needs More Testing`, plus reason, in-sample/out-of-sample label, preferred candidate, and rejected alternatives.
- [ ] Make the report explicitly state that optimization winners are not automatically live-deployment candidates.
- [ ] Run the report against existing normalized samples or manually imported examples.
- [ ] Run `git diff --check`.
- [ ] Review Phase 4 for misleading ranking logic and document the result.
- [ ] Commit Phase 4 separately with message `feat: add Aegis cloud result comparison reports`.

**Acceptance Checks:**

- A new optimization result can be summarized without screenshot transcription.
- The report can explain why a parameter set is preferred, not just which row sorted first.
- The report distinguishes cloud objective ranking from our live-deployment decision criteria.
- The report shows benchmark-relative deltas and identifies whether the result needs more out-of-sample testing.

## Phase 5: Guarded Cloud Backtest And Optimization Operations

**Objective:** Add optional cloud-write workflows for launching backtests and optimizations only after read-only ingestion/reporting is reliable.

**Files:**
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/Start-AegisCloudBacktest.ps1`
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/Start-AegisCloudOptimization.ps1`
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/cloud-operation-runbook.md`
- Modify: `project-notes/Aegis_QuantConnect_MCP_Workflow_Implementation_YYYY-MM-DD.md`

**Hard Gate:**

- Do not implement Phase 5 until one supported provider has completed both:
  - one cloud backtest read-only fetch -> normalize -> index -> report
  - one cloud optimization read-only fetch -> normalize ranked rows -> index -> report

**Tasks:**

- [ ] Record the Phase 5 start in the implementation note.
- [ ] Implement dry-run mode first: print project id, branch/source revision, parameters, date window, estimated operation type, and the exact cloud action that would be requested.
- [ ] Require `-ConfirmCloudWrite`, `-ProjectId`, `-ExpectedProjectName`, `-SourceRevision`, `-OperationNotePath`, and typed confirmation text for any cloud-write command.
- [ ] Use typed confirmation text `LAUNCH_AEGIS_BACKTEST` for backtests and `LAUNCH_AEGIS_OPTIMIZATION` for optimizations.
- [ ] Before launch, compute a run fingerprint from project id, expected project name, source revision, date window, parameters, and operation type.
- [ ] Prevent duplicate launch by refusing if `cloud-result-index.csv` or recent cloud listings show the same run fingerprint, unless an explicit override is added and documented.
- [ ] Capture pre-launch counts where available: backtests, optimizations, live deployments, and Object Store keys.
- [ ] Require a project-note path argument so every cloud-write action has an audit trail before launch.
- [ ] Add a hard denylist so scripts refuse live deploy/stop, brokerage order operations, Object Store write/delete, and project delete/update actions.
- [ ] Keep launch, import, and analysis separate: `Start-AegisCloudBacktest.ps1` and `Start-AegisCloudOptimization.ps1` launch only; `Import-AegisCloudResult.ps1` fetches/normalizes only; `Analyze-AegisCloudResults.ps1` reports only.
- [ ] Add post-launch polling only for status and result retrieval; do not chain additional optimizations automatically.
- [ ] Capture post-launch counts and confirm exactly one expected backtest or optimization was created, with no live deployment or Object Store mutation.
- [ ] Run dry-run commands for one backtest and one optimization.
- [ ] Run `git diff --check`.
- [ ] Review Phase 5 for accidental live-trading or Object Store mutation risk and document the result.
- [ ] Commit Phase 5 separately with message `feat: add guarded Aegis cloud run launchers`.

**Acceptance Checks:**

- Backtest/optimization launch commands are impossible to run accidentally without the confirmation switch and typed confirmation text.
- Every cloud-write action is tied to a project note before execution.
- No live algorithm or Object Store mutation capability is included.
- Duplicate-launch prevention blocks repeated runs with the same project, revision, date window, and parameter hash.

## Final Validation

- [ ] Run `git status -sb` and confirm only expected files changed before each commit.
- [ ] Run `git diff --check` before every commit.
- [ ] Verify documentation states that credentials are never committed.
- [ ] Verify all cloud-write commands are opt-in and audited.
- [ ] Verify one end-to-end read-only backtest workflow: discover project, read backtest, normalize result, index artifact, generate comparison report.
- [ ] Verify one end-to-end read-only optimization workflow: discover optimization, normalize ranked rows, index artifacts, generate comparison report.
- [ ] Record unresolved risks in the implementation note.

## Known Risks

- QuantConnect MCP availability may depend on the client environment, so REST fallback must remain part of the plan.
- QuantConnect API response shapes can change; the normalizer should fail loudly when required fields are missing.
- Optimization result tables can hide tied or equivalent parameter combinations; reports must surface ties.
- Cloud-write automation can create cost or trading risk if guardrails are weakened; live deploy and Object Store mutation remain out of scope.
- Local ignored files can still leak through logs, screenshots, or copied notes; redaction checks are required before committing samples or reports.
- Retry logic can accidentally create duplicate runs if implemented incorrectly; Phase 5 must fail closed on uncertain launch status.
