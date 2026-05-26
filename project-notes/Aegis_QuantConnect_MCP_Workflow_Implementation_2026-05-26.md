# Aegis QuantConnect MCP Workflow Implementation - 2026-05-26

## Phase 1 Start

- Date: 2026-05-26
- Phase: 1 - Connectivity, credential safety, and access model.
- Plan: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/Plans/Aegis_QuantConnect_MCP_Workflow_Plan_2026-05-26.md`
- Scope: Documentation and local workflow scaffolding only.
- Trading impact: None. No allocation logic, live deployment logic, Object Store logic, or brokerage/order handling changes.

## Phase 1 Intended Files

- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/README.md`
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/.gitignore`
- Create: `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CloudWorkflow/appsettings.example.json`

## Phase 1 Requirements

- Define supported connection modes without assuming QuantConnect MCP is available in this Codex environment.
- Define credential handling rules before any API/MCP call is automated.
- Define side-effect classes for provider actions.
- Keep raw responses and local secrets out of git.
- Use fake placeholder values only.

## Phase 1 Changes

- Added `CloudWorkflow/README.md` with supported connection modes, credential rules, side-effect classes, artifact boundaries, and pre-commit checks.
- Added `CloudWorkflow/.gitignore` to keep local secrets, raw MCP/API responses, scratch exports, and local transcripts out of git.
- Added `CloudWorkflow/appsettings.example.json` with fake placeholder identifiers only.

## Phase 1 Validation

- Ran `git diff --check`; no whitespace errors reported.
- Ran `git status --short`; only the new `CloudWorkflow` files and this implementation note were changed.
- Searched Phase 1 files for `token`, `password`, `apiKey`, `userId`, `organizationId`, and `projectId`.
- Search result review: all matches are documentation placeholders, redaction guidance, environment variable names, or fake example values.

## Phase 1 Review

- Finding: No trading algorithm code was changed.
- Finding: No live deployment, brokerage, order handling, Object Store, or allocation behavior was changed.
- Finding: Credential handling is explicitly defined before any MCP/API automation is implemented.
- Finding: Provider actions are classified into `read-only`, `cloud-write`, `forbidden-live`, and `forbidden-object-store`.
- Finding: Raw cloud responses are ignored by default through `CloudWorkflow/.gitignore`.
- Residual risk: The actual QuantConnect provider availability still needs to be verified in Phase 2 before we know whether MCP or REST will be the practical first path.

## Phase 2 Start

- Date: 2026-05-26
- Phase: 2 - Read-only QuantConnect MCP proof of connectivity.
- Scope: Attempt provider discovery, document selected provider path, and stop before any cloud call that would require credentials.
- Trading impact: None. No cloud backtest, optimization, live deployment, Object Store, brokerage, or order action was triggered.

## Phase 2 Provider Discovery

- Checked available Codex tools for QuantConnect MCP support using tool discovery.
- Result: no QuantConnect MCP tools are available in this session.
- Checked installable plugin/connector candidates after tool discovery returned no QuantConnect tools.
- Result: no QuantConnect plugin or connector is available to install from the current plugin list.
- Provider decision for now: `McpInteractive` is blocked in this Codex session; `RestApi` is the practical fallback path for a future executable proof once credentials and project identifiers are supplied outside git.

## Phase 2 Current Boundary

- No QuantConnect API credentials were requested, stored, printed, or committed.
- No MCP or REST cloud read was executed.
- No raw response, sanitized sample, or pre/post cloud count can be captured until a provider is connected.
- Phase 2 is therefore prepared but not fully accepted. The remaining acceptance items require a working provider and credentials.

## Phase 2 Changes

- Added `CloudWorkflow/connection-check.md` to record the MCP discovery result, mutation boundary, REST prerequisites, and remaining read-only proof steps.
- Added `CloudWorkflow/providers/README.md` to document the provider classes, current provider decision, side-effect rules, credential rules, and Phase 2 exit criteria.
- Updated this implementation note with Phase 2 provider discovery and the current blocker.

## Phase 2 Validation

- Ran `git diff --check`; no whitespace errors reported.
- Ran `git status --short`; only the Phase 2 documentation files and this note are changed.
- Searched Phase 2 files for credential-like strings and QuantConnect environment variable names.
- Search result review: all matches are environment variable placeholders or credential safety guidance; no real credential or account identifier was found.

## Phase 2 Review

- Finding: No cloud calls were executed because QuantConnect MCP is unavailable and REST credentials were not supplied.
- Finding: No trading algorithm code was changed.
- Finding: No live deployment, brokerage, order handling, Object Store, or allocation behavior was changed.
- Finding: The workflow correctly stops before attempting REST because credentials must be supplied outside git.
- Residual risk: Full Phase 2 acceptance remains blocked until either a QuantConnect MCP tool is made available or REST credentials/project identifiers are supplied through local environment variables or an OS/user secret store.

## Phase 2 MCP Follow-Up Research

- QuantConnect has an official MCP server that connects LLM clients to the QuantConnect API.
- QuantConnect officially documents Claude Code and Copilot setup through QuantConnect Local Platform, using an HTTP MCP endpoint at `http://localhost:3001/`.
- QuantConnect's public Docker MCP server repository exists, but its README currently says the Docker path is deprecated for now and the preferred path is the embedded MCP in VS Code/Local Platform.
- OpenAI Codex supports MCP servers through `config.toml`, including STDIO servers and streamable HTTP servers.
- Conclusion: QuantConnect MCP is not available as a preinstalled Codex plugin in this session, but it should be possible to connect Codex to the Local Platform MCP endpoint if Local Platform exposes `http://localhost:3001/` and this project is trusted.

## Phase 2 Local Platform MCP Configuration

- Added project-scoped `.codex/config.toml`.
- Configured `mcp_servers.quantconnect` to use `http://localhost:3001/`.
- Did not add credentials to config.
- Restricted Phase 2 tools with an allow-list for read-only project, backtest, order, optimization, and MCP-version reads.
- Excluded cloud-write, live-trading, Object Store, project mutation, and file mutation tools from the allow-list.

## Phase 2 Local Endpoint Check

- Ran a local reachability check against `http://localhost:3001/`.
- Result: unreachable on this machine at the time of the check.
- Interpretation: QuantConnect Local Platform MCP is not currently running or not exposing the endpoint.
- Next requirement: start QuantConnect Local Platform with MCP enabled, then restart Codex or start a new Codex session so `.codex/config.toml` can be loaded.
- Trading impact: none. This was a local HTTP reachability check only; no QuantConnect cloud action was attempted.

## Phase 2 MCP Configuration Validation

- Ran `git diff --check`; no whitespace errors reported.
- Ran `git status --short`; changes are limited to `.codex/config.toml`, CloudWorkflow documentation, and this implementation note.
- Searched changed files for credential-like strings and QuantConnect environment variable names.
- Search result review: all matches are environment variable placeholders or credential safety guidance; no real credential or account identifier was found.

## Phase 2 MCP Configuration Review

- Finding: The Codex MCP config contains no credentials.
- Finding: The configured QuantConnect endpoint is local-only: `http://localhost:3001/`.
- Finding: The Phase 2 allow-list includes read-oriented project, backtest, order, optimization, and MCP-version tools only.
- Finding: The allow-list excludes cloud-write, live-trading, Object Store, project mutation, and file mutation tools.
- Residual risk: tool names may differ from the QuantConnect MCP server implementation. If tools do not appear after restarting Codex with Local Platform running, inspect the exposed tool list and adjust the allow-list without broadening into write/live/Object Store tools.
