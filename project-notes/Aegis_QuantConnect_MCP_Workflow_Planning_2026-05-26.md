# Aegis QuantConnect MCP Workflow Planning - 2026-05-26

## Planning Start

- Date: 2026-05-26
- Request: Create a comprehensive five-phase plan for replacing screenshot/manual-file QuantConnect result handoff with a direct MCP/API workflow.
- Scope: Documentation only. Do not implement QuantConnect connectivity, scripts, cloud calls, or live-trading changes.
- Plan file: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/Plans/Aegis_QuantConnect_MCP_Workflow_Plan_2026-05-26.md`

## Initial Draft Summary

- Phase 1: Connectivity, credential safety, and access model.
- Phase 2: Read-only QuantConnect MCP proof of connectivity.
- Phase 3: Normalized result ingestion pipeline.
- Phase 4: Automated Aegis analysis and comparison reports.
- Phase 5: Guarded cloud backtest and optimization operations.

## Review Plan

- Spawn three read-only sub agents after saving the initial draft.
- Agent 1 focus: integration feasibility and phase ordering.
- Agent 2 focus: security, credentials, live-trading safety, and Object Store risk.
- Agent 3 focus: analytics workflow, normalization quality, and report usefulness.
- Revise the plan after agent feedback and record the review/debate result here.

## Agent Review Results

### Agent 1 - Integration Feasibility

- Supported the five-phase order but found the initial draft too coupled between cloud fetch and normalization.
- Recommended a durable provider/raw-data boundary with `McpInteractive` and `RestApi` providers.
- Recommended ignored raw storage, normalized output storage, report storage, and explicit import into legacy log folders only when requested.
- Recommended separating launch, import, and analysis commands so reporting failures cannot affect cloud operation state.

### Agent 2 - Security And Live-Trading Safety

- Supported the read-only-first architecture and the out-of-scope boundary excluding live deploy, brokerage orders, and Object Store mutation.
- Requested stricter credential rules than "environment variables or ignored files" because ignored files can still leak through notes, logs, or screenshots.
- Requested least-privilege/read-only access where available, pre/post mutation checks, typed confirmations, duplicate-launch prevention, and a hard denylist for live/Object Store/project mutation actions.
- Requested redaction rules for normalized outputs and committed samples.

### Agent 3 - Analytics Workflow Quality

- Supported the sequencing and multi-metric reporting approach.
- Requested explicit support for backtest summaries, orders, logs, optimization rows, and optimization child backtests.
- Requested formal artifact naming and a canonical `cloud-result-index.csv`.
- Requested benchmark-relative comparison, deployment decision notes, and a hard Phase 5 gate requiring both one backtest and one optimization to complete read-only fetch -> normalize -> index -> report.

## Debate Consensus

- Accepted: provider/raw-data boundary, ignored raw response storage, sanitized committed samples, stricter redaction rules, canonical cloud result index, stronger artifact naming, benchmark-aware reports, typed cloud-write confirmations, duplicate-launch prevention, and separate launch/import/analyze commands.
- Accepted: Phase 5 must not start until one supported provider completes both a backtest and optimization read-only end-to-end workflow.
- Adjusted: the workflow should not require both MCP and REST to work before Phase 5. One supported provider is enough if the unavailable provider is documented as a blocker or fallback limitation.
- Rejected: a single all-in-one command that launches, imports, and analyzes automatically. The agents agreed this would increase duplicate-launch and accidental cloud-write risk.

## Plan Revision Summary

- Added artifact boundaries for `raw`, `samples`, `output`, and `reports`.
- Added provider boundary definitions for `McpInteractive` and `RestApi`.
- Added credential and redaction requirements.
- Added pre/post mutation verification in read-only and write phases.
- Added `cloud-result-index.csv` schema fields and stricter normalized artifact naming.
- Added benchmark-relative report requirements and a `Deployment Decision` section.
- Added hard Phase 5 gate, typed confirmations, duplicate-launch prevention, and denylisted cloud actions.

## Review Result

- Documentation-only review completed.
- No code, cloud calls, credentials, or live-trading behavior changes were introduced.
- No unresolved plan blockers remain, but implementation will still need live confirmation of which QuantConnect provider is available in this Codex environment.

## Validation

- Ran `git diff --check`; no whitespace errors reported for tracked diffs.
- Searched the plan and planning note for common placeholder markers; no placeholder text found in implementation content.
- Checked both new markdown files for trailing whitespace; no trailing whitespace found.
- Git status shows only the new plan file and the new planning note as untracked documentation changes.

## Final Documentation Review

- Finding: No implementation code or cloud automation was added, matching the requested planning-only scope.
- Finding: The revised plan includes the five requested phases and task lists for each phase.
- Finding: Agent feedback was incorporated where it improved safety or workflow quality.
- Finding: The plan explicitly preserves live-trading safety boundaries by excluding live deploy, brokerage orders, and Object Store writes.
- Residual risk: Actual implementation still depends on confirming which QuantConnect provider is usable from this environment.
