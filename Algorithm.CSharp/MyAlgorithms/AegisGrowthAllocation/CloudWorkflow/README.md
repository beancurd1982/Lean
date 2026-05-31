# Aegis QuantConnect Cloud Workflow

This folder contains the planned local workflow for reading QuantConnect cloud results directly, normalizing them, and generating Aegis analysis reports without screenshot/manual-file handoff.

Phase 1 is documentation and safety scaffolding only. It does not call QuantConnect, launch backtests, launch optimizations, change live deployments, write Object Store data, or affect the Aegis allocation algorithm.

## Supported Connection Modes

Use the first available mode that can complete the read-only proof in the implementation plan.

- `McpInteractive`: official QuantConnect MCP tools exposed to the assistant. Preferred for interactive agent workflows when available.
- `LocalPlatformMcp`: QuantConnect Local Platform MCP endpoint, if available locally and explicitly configured.
- `DockerMcp`: QuantConnect MCP server running through Docker, if supported by the current client environment.
- `RestApi`: QuantConnect REST API fallback. Preferred for repeatable repo-local scripts because it can be dry-run, logged, and tested directly.

Do not require every mode to work. One supported provider is enough for later phases if unavailable providers are documented as blockers or fallback limitations.

## Codex Local Platform MCP Setup

This repository includes `.codex/config.toml` with a QuantConnect MCP server entry that points to `http://localhost:3001/`.

To use it:

1. Start QuantConnect Local Platform and confirm it exposes the MCP endpoint on `http://localhost:3001/`.
2. Restart Codex or open a new Codex session from this repository so the project-scoped MCP config is loaded.
3. Check that QuantConnect MCP tools appear before attempting Phase 2 cloud reads.

The config contains no credentials and restricts Phase 2 to a read-only tool allow-list.

## Verified Chrome GUI Fallback

When MCP or REST access is unavailable, the QuantConnect cloud backtest project can be maintained through a guarded Chrome GUI workflow. This fallback has been verified against `AegisGrowthAllocation_BackTest`.

Required Aegis source files:

- `AegisGrowthAllocation.cs`
- `AegisGrowthAllocation.LiveState.cs`
- `LiveStateStore.cs`
- `PortfolioManager.cs`
- `RegimeModel.cs`
- `StockSelectionModel.cs`
- `StrategyConfig.cs`

Preferred source restore method:

1. Confirm the visible project is the intended backtest project, not a live-trading project.
2. Confirm the explorer `WORKSPACE (WORKSPACE)` accordion is expanded.
3. Right-click the child `project` row.
4. Select `New File...`.
5. Enter the exact filename.
6. Replace generated content with the matching local source content.
7. Wait for autosave before creating the next file.
8. After all files are present, clear stale `CLOUD TERMINAL` output.
9. Trigger `Cloud Build (Ctrl+Shift+B)`.
10. Verify the fresh terminal output contains `Built project ...` and inspect `PROBLEMS`.

Avoid the small explorer header `New File` button during restore. Its hit area is close to the collapsible workspace header and is easier to misclick.

The `WORKSPACE (WORKSPACE)` header and child `project` row are independent accordions. Expand the parent before using explorer controls and expand the child before visually verifying filenames.

This fallback does not authorize backtests, optimizations, live deployments, Object Store writes, or live-trading project mutation. Those actions require separate explicit approval.

Detailed evidence: `project-notes/Aegis_QuantConnect_Chrome_Automation_Findings_2026-05-30.md`.

## Credential Rules

- Do not commit credentials, API tokens, account identifiers, brokerage account data, raw Object Store JSON, or raw cloud response bodies.
- Prefer environment variables or an OS/user secret store.
- Ignored local config is allowed only for local development and must never be copied into project notes or screenshots.
- Use least-privilege/read-only access if QuantConnect supports it.
- If only full-access credentials are available, use them only for read calls until the guarded cloud-write phase is explicitly approved.
- Scripts must not print secrets. Failed authentication must not write raw response bodies into notes, committed samples, or reports.

## Recommended Local Secret Names

These names are documentation placeholders. Do not place real values in committed files.

- `QC_USER_ID`
- `QC_API_TOKEN`
- `QC_ORGANIZATION_ID`
- `QC_PROJECT_ID`

## Side-Effect Classes

Every provider action must be classified before implementation.

- `read-only`: Lists or reads projects, backtests, optimizations, orders, logs, or metadata.
- `cloud-write`: Creates a backtest or optimization. Requires Phase 5 guardrails and explicit typed confirmation.
- `forbidden-live`: Starts, stops, redeploys, liquidates, or otherwise mutates a live algorithm or brokerage state.
- `forbidden-object-store`: Writes, deletes, migrates, or overwrites Object Store data.

The initial workflow may implement only `read-only` actions. Phase 5 may add narrowly guarded `cloud-write` actions for backtests and optimizations. `forbidden-live` and `forbidden-object-store` actions remain out of scope.

## Artifact Boundaries

- `raw/`: ignored raw MCP/API responses and temporary fetch output.
- `samples/`: sanitized examples only. Remove credentials, user identifiers, brokerage account data, raw Object Store payloads, and nonessential account-identifying metadata.
- `output/`: normalized, redacted, schema-validated artifacts and the future `cloud-result-index.csv`.
- `reports/`: derived markdown and CSV comparison reports.

## Before Committing

Run these checks before committing cloud workflow files:

```powershell
git -c safe.directory=D:/Projects/Git/Lean-1 diff --check
git -c safe.directory=D:/Projects/Git/Lean-1 diff --cached --check
git -c safe.directory=D:/Projects/Git/Lean-1 status --short
```

Also inspect staged diffs for credential-like strings and confirm any `projectId`, `organizationId`, `userId`, `token`, `password`, or `apiKey` values are fake placeholders or redaction guidance.
