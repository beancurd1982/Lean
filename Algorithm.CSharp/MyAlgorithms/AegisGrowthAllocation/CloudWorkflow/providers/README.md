# Aegis Cloud Workflow Providers

This folder records which QuantConnect cloud provider path is available for the Aegis workflow.

## Provider Classes

- `McpInteractive`: official QuantConnect MCP tools exposed directly to the assistant.
- `LocalPlatformMcp`: QuantConnect Local Platform MCP endpoint, if locally available.
- `DockerMcp`: QuantConnect MCP server running through Docker, if the client supports it.
- `RestApi`: QuantConnect REST API fallback for repeatable local scripts.

## Current Provider Decision

- `McpInteractive`: blocked. No QuantConnect MCP tools are preinstalled in this Codex session.
- `LocalPlatformMcp`: configured through project-scoped `.codex/config.toml`, but currently blocked because `http://localhost:3001/` is not reachable from this machine.
- `DockerMcp`: not configured in this repository.
- `RestApi`: fallback if `LocalPlatformMcp` is unavailable, pending credentials supplied outside git.

## Side-Effect Rules

Allowed in Phase 2:

- `read-only` actions that list/read projects, backtests, optimizations, orders, logs, or metadata.

Not allowed in Phase 2:

- `cloud-write` actions that create backtests or optimizations.
- `forbidden-live` actions that start, stop, redeploy, liquidate, or mutate live algorithms or brokerage state.
- `forbidden-object-store` actions that write, delete, migrate, or overwrite Object Store data.

## Credential Rules

- Do not commit credentials or account identifiers.
- Prefer environment variables or an OS/user secret store.
- Do not print tokens or raw authentication failures.
- Commit only sanitized response samples.

## Phase 2 Exit Criteria

Phase 2 is not complete until one supported provider can:

- Discover the Aegis project.
- Read one backtest summary.
- Read one orders response for that backtest.
- Save raw responses only under ignored `CloudWorkflow/raw/`.
- Save a sanitized sample shape under `CloudWorkflow/samples/`.
- Prove pre/post cloud counts did not change.
