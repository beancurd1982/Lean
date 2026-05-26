# Aegis QuantConnect Cloud Connection Check

## Status

- Date: 2026-05-26
- Current status: blocked before cloud read.
- Selected next provider: `RestApi`, unless a QuantConnect MCP connector becomes available in Codex.

## MCP Discovery

- Tool discovery query: QuantConnect MCP cloud backtest optimization projects.
- Result: no QuantConnect MCP tools found.
- Installable plugin/connector scan: no QuantConnect plugin or connector found.

## Cloud Mutation Check

No QuantConnect cloud call was executed during this check, so no cloud mutation was possible from this workflow step.

Expected unchanged areas:

- No new backtest created.
- No new optimization created.
- No live algorithm started, stopped, or redeployed.
- No Object Store key read, written, deleted, or migrated.
- No brokerage account or order state touched.

## REST Fallback Prerequisites

The future REST proof should use credentials supplied outside git.

Required local inputs:

- QuantConnect user id through an environment variable such as `QC_USER_ID`.
- QuantConnect API token through an environment variable such as `QC_API_TOKEN`.
- Aegis project id through an environment variable such as `QC_PROJECT_ID`.
- Optional organization id through an environment variable such as `QC_ORGANIZATION_ID`.

Do not write real values into committed files, project notes, screenshots, command transcripts, or sample artifacts.

## Read-Only Proof To Run Later

When credentials are available outside git, the read-only proof should capture:

- Pre-read backtest count, optimization count, live algorithm status/count if available, and Object Store key count if readable.
- One raw backtest summary saved only under ignored `CloudWorkflow/raw/`.
- One raw orders response saved only under ignored `CloudWorkflow/raw/`.
- A sanitized committed sample response shape under `CloudWorkflow/samples/`.
- Post-read counts proving no new backtest, no new optimization, no live algorithm change, and no Object Store change.

## Blocker

Phase 2 cannot be fully accepted until a working provider is connected. Current options:

- Configure QuantConnect MCP in Codex or expose it as an available MCP tool.
- Use the QuantConnect REST API fallback with credentials supplied through local environment variables or an OS/user secret store.
