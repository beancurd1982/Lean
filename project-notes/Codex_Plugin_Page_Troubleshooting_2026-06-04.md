# Codex Plugin Page Troubleshooting - 2026-06-04

## Step 1 - Start Investigation
- Summary: User reports an already-installed Codex plugin no longer appears in the Plugins page after restarting Codex.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Plan: Inspect local Codex plugin/config state, identify whether the plugin is missing from installation metadata or only hidden/unloaded, then recommend or apply a focused fix.
- Open questions: Which specific plugin is missing is not yet confirmed.
- Risks: Changes are local Codex configuration only; no live-trading behavior is affected.

## Step 2 - Config and Cache Check
- Summary: `C:/Users/douya/.codex/config.toml` still contains enabled plugin entries, including `espresso@awesome-codex-plugins`, and the plugin cache directory exists under `C:/Users/douya/.codex/plugins/cache`.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Inspect cached plugin manifests and marketplace metadata to determine whether the Plugins page has enough metadata to display the plugin.
- Open questions: The specific missing plugin is still assumed to be the external `espresso` plugin because bundled/curated plugins are visible in this session.
- Risks: No live-trading behavior is affected.

## Step 3 - Manifest Discovery
- Summary: The external plugin cache contains `espresso/local/.codex-plugin/plugin.json`; no `marketplace.json` was found under `C:/Users/douya/.codex/plugins/cache/awesome-codex-plugins`.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Read the plugin manifest and local Codex logs for validation or discovery errors.
- Open questions: Need to confirm whether the Plugins page is hiding only external marketplace plugins or this specific plugin.
- Risks: No live-trading behavior is affected.

## Step 4 - Manifest Read
- Summary: `espresso` manifest exists and contains `name`, `description`, and `hooks` fields. A broad log search was noisy due to large session payloads, so further checks should target config and marketplace files only.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Inspect the marketplace working copy under `C:/Users/douya/.codex/.tmp/marketplaces/awesome-codex-plugins` and the Codex CLI plugin commands if available.
- Open questions: Need to verify whether `espresso` is listed in marketplace metadata after restart.
- Risks: No live-trading behavior is affected.

## Step 5 - Marketplace Checkout and CLI Discovery
- Summary: The `awesome-codex-plugins` marketplace checkout exists under `.codex/.tmp/marketplaces`, and Codex CLI exposes a `plugin` management command.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Use focused CLI/list commands and marketplace-index reads to compare installed state with UI-visible state.
- Open questions: Need to confirm whether `espresso` is returned by `codex plugin list` or only present in config/cache.
- Risks: No live-trading behavior is affected.

## Step 6 - Marketplace Metadata Check
- Summary: `espresso` is present in both `plugins.json` and `.agents/plugins/marketplace.json` for `awesome-codex-plugins`; marketplace install metadata points to revision `b4dd3ac78e50c9cdfd3dbb3fea4ec3a75d8e2daa`.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Run Codex CLI plugin list/marketplace list commands to confirm runtime plugin visibility.
- Open questions: Need to determine whether the desktop Plugins page is out of sync with CLI state.
- Risks: No live-trading behavior is affected.

## Step 7 - CLI Runtime Mismatch
- Summary: `codex plugin list` returned `No marketplace plugins found`; `codex plugin marketplace list` returned `No plugin marketplaces in scope`, despite marketplace entries in `C:/Users/douya/.codex/config.toml`.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Check Codex diagnostics and feature/config scope to explain why the CLI/runtime is not considering the marketplace entries.
- Open questions: Need to know whether this is caused by config parsing, profile scope, feature gating, or desktop app state.
- Risks: No live-trading behavior is affected.

## Step 8 - Sandbox Scope Identified
- Summary: `codex doctor` showed the sandboxed CLI reads `C:/Users/CodexSandboxOffline/.codex`, not the user's real `C:/Users/douya/.codex`; therefore earlier CLI plugin list results do not reflect the desktop app's real plugin state.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Run a narrowly scoped unsandboxed Codex CLI plugin/doctor check against the real user environment.
- Open questions: Need user approval for unsandboxed command execution.
- Risks: No live-trading behavior is affected.

## Step 9 - Real Marketplace Scope
- Summary: Unsandboxed `codex plugin marketplace list` sees `openai-primary-runtime`, `awesome-codex-plugins`, and `openai-curated` in the real user environment.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Run unsandboxed plugin list and inspect whether `espresso` is visible from Codex's real runtime state.
- Open questions: Need to determine whether desktop Plugins page differs from CLI plugin list.
- Risks: No live-trading behavior is affected.

## Step 10 - Real Plugin List
- Summary: Unsandboxed `codex plugin list --marketplace awesome-codex-plugins` reports `espresso@awesome-codex-plugins` as `installed, enabled` with version `local`.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Check plugin file integrity, especially hook paths, before concluding this is a desktop UI listing issue.
- Open questions: Need to know whether the Plugins page search/filter is hiding the plugin even though CLI state is correct.
- Risks: No live-trading behavior is affected.

## Step 11 - Plugin Integrity Issue
- Summary: `espresso` manifest references `./hooks/hooks.json`, but that file is missing from both the marketplace checkout and installed plugin cache.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Refresh the `awesome-codex-plugins` marketplace snapshot and re-check the installed cache.
- Open questions: Need to see whether a marketplace refresh restores the missing hook file or whether the marketplace entry itself is incomplete.
- Risks: No live-trading behavior is affected; refreshing Codex plugin marketplace state may update local Codex plugin metadata.

## Step 12 - Marketplace Refresh
- Summary: `codex plugin marketplace upgrade awesome-codex-plugins` reported the marketplace is already up to date.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Re-check cache and document review conclusion.
- Open questions: Need to confirm whether the missing hook file remains after refresh.
- Risks: No live-trading behavior is affected.

## Step 13 - User Correction
- Summary: User clarified the missing plugin is the bundled Codex plugin `Chrome`, not the external `espresso` plugin.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Inspect `chrome@openai-bundled` config, cache, manifest, and real CLI visibility.
- Open questions: Need to determine whether `Chrome` is installed/enabled but hidden from the desktop Plugins page, or whether the bundled plugin has been superseded by the `Browser` plugin in the current UI.
- Risks: No live-trading behavior is affected.

## Step 14 - Chrome Plugin State
- Summary: `C:/Users/douya/.codex/config.toml` has `[plugins."chrome@openai-bundled"] enabled = true`, and the cache exists at `C:/Users/douya/.codex/plugins/cache/openai-bundled/chrome/26.601.21317`. However, real `codex plugin list --marketplace openai-bundled` reports `No plugins found in marketplace openai-bundled`.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Inspect `openai-bundled` marketplace metadata and compare with the installed cache.
- Open questions: Need to determine whether the desktop Plugins page is correct because the current bundled marketplace omits Chrome from listing, even while config/cache keep it available.
- Risks: No live-trading behavior is affected.

## Step 15 - Bundled Metadata Shape
- Summary: `C:/Users/douya/.codex/.tmp/bundled-marketplaces/openai-bundled` contains a Chrome `extension-host` payload but no `marketplace.json` and no `.codex-plugin/plugin.json`; the installed Chrome cache also has no `plugin.json`.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Verify the operational Chrome integration pieces, especially extension installation and native host manifest.
- Open questions: Need to determine whether this is only a Plugins-page listing/metadata absence or a broken Chrome integration.
- Risks: No live-trading behavior is affected.

## Step 16 - Sandboxed Node Block
- Summary: Chrome integration check scripts failed under sandboxed `node.exe` with `Access is denied`.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Rerun the Chrome checks outside the sandbox using the Codex-bundled Node runtime.
- Open questions: Need to confirm whether Chrome extension and native host are correctly installed.
- Risks: No live-trading behavior is affected.

## Step 17 - Chrome Operational Verification
- Summary: Unsandboxed Chrome diagnostics show the Chrome extension is installed and enabled in the `Default` profile, native host manifest is correct, Google Chrome is installed as the default browser, and Chrome is currently running.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Finding: The issue appears to be Plugins-page listing/metadata behavior for bundled `Chrome`, not a broken Chrome integration.
- Open questions: Need product/UI confirmation on whether `Chrome` is intentionally hidden from the Plugins page because the current in-app surfaced plugin is `Browser`, while Chrome integration remains installed as a bundled support component.
- Risks: No live-trading behavior is affected.

## Step 18 - Strict Review
- Review scope: Local Codex plugin/config/cache diagnostics and this project note update.
- Review result: No code changes were made. No repository behavior or live-trading behavior was changed.
- Safety/correctness findings: `chrome@openai-bundled` remains enabled in config and operational checks pass; the visible Plugins page mismatch is not explained by missing extension/native-host installation.
- Residual risk: The bundled `openai-bundled` marketplace metadata lacks a plugin listing for Chrome, so the desktop Plugins page may be unable or not intended to show it even while the integration works.

## Step 19 - Chrome Plugin Live Test Request
- Summary: User asked to use the Chrome plugin to open a new tab and navigate to YouTube.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Inspect Chrome plugin control instructions and execute the plugin-backed Chrome open command if available.
- Open questions: Need to confirm whether Chrome plugin exposes a direct MCP/tool command in this session or only bundled scripts.
- Risks: Opens a browser tab only; no live-trading behavior is affected.

## Step 20 - Chrome Plugin Control Path
- Summary: Loaded Chrome plugin control instructions. The plugin uses `scripts/browser-client.mjs` through the Node runtime and the Chrome extension backend.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Bootstrap the Chrome extension backend, create a new tab, navigate to `https://www.youtube.com`, and keep the tab open for the user.
- Open questions: Need to confirm browser-client communication succeeds.
- Risks: Opens a browser tab only; no live-trading behavior is affected.

## Step 21 - Chrome Plugin Live Test Result
- Summary: Attempted to bootstrap the Chrome plugin browser-client twice through the Node runtime. Both attempts failed before tab creation with `windows sandbox failed: spawn setup refresh` and `node_repl kernel exited unexpectedly`.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Result: Could not use the Chrome plugin to open YouTube in this turn because the required browser-client runtime crashed before communicating with Chrome.
- Open questions: Need to determine why the Node runtime fails during Windows sandbox setup for `browser-client.mjs`; prior extension/native-host diagnostics still passed.
- Risks: No browser navigation was completed through the Chrome plugin; no live-trading behavior is affected.

## Step 22 - Strict Review
- Review scope: Chrome plugin live test and project note update.
- Review result: No code changes were made. The Chrome plugin was not able to complete the requested navigation because runtime bootstrap failed twice.
- Safety/correctness findings: It would be misleading to claim the Chrome plugin opened YouTube; the only successful earlier checks were extension/native-host/install diagnostics.
- Residual risk: A normal OS-level Chrome launch could open YouTube, but that would not validate the Chrome plugin control path.

## Step 23 - Sandbox Hypothesis Test
- Summary: User approved testing whether the Windows sandbox override causes the Chrome plugin runtime failure.
- Evidence: Chrome extension/native-host diagnostics pass, while `browser-client.mjs` fails during startup with `windows sandbox failed: spawn setup refresh`.
- Hypothesis: The `[windows] sandbox = "elevated"` override in `C:/Users/douya/.codex/config.toml` may be causing or contributing to the Chrome plugin browser-client startup failure.
- Planned minimal change: Back up `config.toml`, remove only the `sandbox = "elevated"` line from the `[windows]` section, retry the Chrome plugin YouTube navigation, then document results.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Risks: Local Codex execution behavior may change until the config is restored; no live-trading behavior is affected.

## Step 24 - Config Override Removed
- Summary: Backed up `C:/Users/douya/.codex/config.toml` to `C:/Users/douya/.codex/config.toml.backup-before-chrome-sandbox-test-20260604` and removed only the line `sandbox = "elevated"` from `[windows]`.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`, `C:/Users/douya/.codex/config.toml`
- Next step: Retry Chrome plugin browser-client startup and YouTube navigation.
- Open questions: Need to see whether the currently running Codex app/runtime picks up the config change without restart.
- Risks: Local Codex execution behavior may differ while the override remains removed; no live-trading behavior is affected.

## Step 25 - Chrome Plugin Retry Succeeded
- Summary: After removing `[windows] sandbox = "elevated"`, the Chrome plugin browser-client connected to the Chrome extension backend and opened a new Chrome tab at `https://www.youtube.com/`.
- Verification: The browser-client returned title `YouTube` and URL `https://www.youtube.com/`.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`, `C:/Users/douya/.codex/config.toml`
- Finding: The Windows sandbox override was the likely cause of the prior `windows sandbox failed: spawn setup refresh` Chrome-plugin startup failure.
- Open questions: Need to decide whether to keep the override removed or restore it from backup after testing.
- Risks: Local Codex execution behavior may differ while the override remains removed; no live-trading behavior is affected.

## Step 26 - Strict Review
- Review scope: Config change and Chrome plugin retry.
- Review result: The change was minimal and reversible. The backup is `C:/Users/douya/.codex/config.toml.backup-before-chrome-sandbox-test-20260604`.
- Safety/correctness findings: Chrome plugin navigation succeeded only after removing `sandbox = "elevated"`, supporting the sandbox-root-cause hypothesis.
- Residual risk: Leaving the override removed changes Codex's Windows sandbox behavior from the explicit prior setting to the default behavior. Restore from backup if elevated sandbox behavior is needed more than Chrome plugin control.

## Step 27 - Return to Optimization Automation Planning
- Summary: User wants to resume the automation flow for optimizing the algorithm after confirming Chrome plugin control works.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Next step: Review recent project notes related to QuantConnect Chrome automation and MCP workflow, then recommend a focused next phase.
- Open questions: Need to confirm whether the next automation should target research/backtest parameter optimization only, or any workflow that could affect live trading.
- Risks: Any change that modifies algorithm parameters, deployment workflow, or live account behavior could affect live trading and must be confirmed before implementation.

## Step 28 - Context Review Result
- Summary: Existing notes show a guarded Chrome GUI fallback has already verified source restore, cloud build, one backtest launch, and downloads for overview JSON, orders CSV, and text log. The remaining unverified area is systematic optimization flow: launching or inspecting optimization runs, retrieving optimization result rows/child backtests, normalizing artifacts, and producing comparison reports.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Recommendation: Do read-only result ingestion/reporting first, then perform one small guarded optimization smoke test, then scale to parameter sweeps.
- Open questions: Need user confirmation whether the next workflow is read-only ingestion/reporting or active cloud optimization launch.
- Risks: Optimization launch changes QuantConnect cloud state and consumes cloud resources; live deployment remains out of scope unless explicitly approved.

## Step 29 - Chrome Plugin Verification After Commit
- Summary: After committing and pushing the troubleshooting notes, retried the Chrome plugin using the current versioned cache path `C:/Users/douya/.codex/plugins/cache/openai-bundled/chrome/26.601.21317/scripts/browser-client.mjs`.
- Verification: The Chrome extension backend connected successfully and returned one open tab: `Algorithmic Trading Platform - QuantConnect.com` at `https://www.quantconnect.com/project/28209469`.
- Files touched: `project-notes/Codex_Plugin_Page_Troubleshooting_2026-06-04.md`
- Finding: Chrome plugin control is usable again through the extension backend.
- Open questions: None for basic plugin connectivity.
- Risks: No QuantConnect page state was changed; this was a read-only tab-list verification.

## Step 30 - Strict Review
- Review scope: Chrome plugin verification and project note update.
- Review result: No issues found.
- Safety/correctness findings: Verification was read-only and did not click, edit, build, backtest, optimize, deploy, access brokerage, or touch Object Store.
- Residual risk: Future browser automation should use the versioned `chrome/26.601.21317` path or dynamically resolve the versioned directory rather than relying on `chrome/latest`.
