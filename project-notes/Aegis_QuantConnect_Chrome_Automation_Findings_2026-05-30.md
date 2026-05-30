# Aegis QuantConnect Chrome Automation Findings - 2026-05-30

## Purpose

- Investigate whether Codex can automate parts of the QuantConnect Cloud Platform backtest workflow through the visible Chrome browser.
- Record interface observations, successful interactions, failures, safety boundaries, and the recommended role of GUI automation.
- Scope: investigation only. No QuantConnect source files, parameters, backtests, optimizations, live deployments, Object Store data, or brokerage state were changed.

## Environment

- QuantConnect project URL: `https://www.quantconnect.com/project/28209469`
- Project shown in browser: `AegisGrowthAllocation_BackTest`
- Browser: visible desktop Chrome session.
- Automation method used during this investigation: Windows foreground-window control, cursor positioning, mouse events, keyboard refresh, and desktop screenshots.
- Limitation: the dedicated browser automation connector was not exposed in this Codex session, so the investigation used visible desktop interaction as a fallback.

## Interface Findings

The QuantConnect project workspace loaded successfully after Chrome was refreshed and minimized/restored.

Observed layout:

- Left panel:
  - project details
  - libraries
  - collaborators
  - editable `PARAMETERS` list
- Center panel:
  - source-code editor tabs
  - `AegisGrowthAllocation.cs` open during inspection
- Bottom-center panel:
  - Cloud Terminal
  - visible completed backtest output
- Far-right vertical toolbar:
  - top icon opens the explorer panel
  - other icons expose additional workspace/debugger areas
- Right explorer panel:
  - workspace tree
  - source files visible:
    - `AegisGrowthAllocation.cs`
    - `LiveStateStore.cs`
    - `PortfolioManager.cs`
    - `RegimeModel.cs`
    - `StockSelectionModel.cs`
    - `StrategyConfig.cs`

## Successful GUI Tests

### Open QuantConnect Project

- Opened Chrome to `https://www.quantconnect.com/project/28209469`.
- Result: success.

### Refresh And Restore Workspace

- Initial project page rendered as a blank surface.
- Sent browser refresh and minimized/restored Chrome.
- Result: workspace rendered successfully.

### Open Workspace Explorer

- Clicked the first icon in the far-right vertical toolbar.
- Result: success.
- The explorer panel opened and displayed project source files.

### Open Explorer Context Menu

- Right-clicked empty space inside the workspace explorer.
- Result: success.
- The context menu displayed:
  - `New File...`
  - `New Folder...`
  - `Open in Integrated Terminal`
  - `Add Folder to Workspace...`
  - `Remove Folder from Workspace`
  - `Find in Folder...`
  - `Paste`
  - `Download...`
  - `Upload...`
  - path-copy actions

### Click Upload Menu Entry

- Located and clicked the visible `Upload...` row.
- Result: the click reached the menu entry, but the native file picker did not open successfully.

## File Picker Failure

After clicking `Upload...`, Windows displayed an `Error launching app` dialog.

Observed error pattern:

```text
Unable to find Electron app at C:\Program Files\WindowsApps\OpenAI.Codex_...
Cannot find module 'C:\Program Files\WindowsApps\OpenAI.Codex_...'
```

Interpretation:

- The QuantConnect browser click worked.
- The failure happened when the native file-picker handoff attempted to launch an invalid Codex desktop Electron package path.
- This is not evidence that Chrome file-picker actions are forbidden.
- This appears to be a Codex desktop/native-dialog integration issue in the current environment.
- GUI upload through the native Windows file picker is unreliable until the desktop integration issue is fixed or bypassed.

## Safety Findings

- Navigation, refresh, panel opening, context-menu inspection, and read-only screenshots worked.
- Coordinate-based GUI automation is possible but brittle:
  - layout changes can shift targets
  - browser scaling and window size affect coordinates
  - notification popups can obstruct controls
  - cloud actions can be triggered accidentally if clicks are not verified between steps
- Any action that changes QuantConnect cloud state must remain explicitly confirmed before execution:
  - parameter edits
  - source file upload or overwrite
  - source-code edits
  - build
  - backtest launch
  - optimization launch
  - live deploy/stop
  - Object Store mutation

## Recommended Automation Strategy

Use a layered approach:

1. Prefer QuantConnect MCP if Codex can connect to the Local Platform MCP endpoint.
2. Use QuantConnect REST API for repeatable read-only retrieval and future guarded backtest/optimization launch workflows.
3. Use visible Chrome GUI automation for:
   - interface discovery
   - visual verification
   - fallback operations not exposed by MCP/API
   - manually confirmed troubleshooting steps
4. Avoid native file-picker GUI automation unless:
   - the Codex desktop file-picker issue is fixed, or
   - a browser automation connector supports direct file-input assignment and bypasses the Windows dialog.

## Suggested Next GUI Tests

Perform only read-only or reversible tests first:

- Inspect whether clicking an existing result tab exposes downloadable result metadata.
- Inspect whether parameter rows can be selected without changing values.
- Inspect the build/backtest controls without launching a build or backtest.
- Determine whether the browser DOM can be accessed through a browser automation connector in a future Codex session.

Do not automate file uploads, parameter changes, builds, backtests, optimizations, or deployments until the GUI workflow has explicit step-level confirmations and screenshot verification.

## Screenshot Evidence

Temporary screenshots were created in the repository root during investigation:

- `quantconnect-project-interface-screenshot.png`
- `quantconnect-explorer-panel-screenshot.png`
- `quantconnect-explorer-context-menu.png`
- `quantconnect-upload-dialog-screenshot.png`
- `quantconnect-upload-retry-current-screen.png`
- `quantconnect-upload-retry-result.png`

These files are intentionally left uncommitted. If long-term evidence is required, move selected screenshots into a dedicated documentation evidence folder in a separate change.

## Open Questions

- Can QuantConnect Local Platform expose MCP at `http://localhost:3001/` on this machine?
- Can a future Codex session load the project-scoped `.codex/config.toml` and expose QuantConnect MCP tools?
- Can a browser automation connector provide DOM-level control and direct file-input assignment?
- Is the Codex desktop native file-picker failure reproducible outside QuantConnect?

## Review Result

- Finding: Visible Chrome GUI automation is viable for read-only interface inspection.
- Finding: The QuantConnect workspace explorer can be opened and inspected programmatically.
- Finding: Native file-picker upload is blocked by a Codex desktop integration error.
- Finding: MCP/REST should remain the primary automation path; GUI automation should be a guarded fallback.
- Trading impact: none. No QuantConnect cloud state or algorithm behavior was changed.

## Destructive GUI Test - Delete Temporary Source File

- User explicitly requested a destructive GUI test against the temporary cloud-workspace file `123.cs`.
- Intended action: locate `123.cs` in the QuantConnect workspace explorer, right-click the file, and select `Delete Permanently`.
- Scope boundary: delete only `123.cs`. Do not modify or delete any Aegis algorithm source file.

### Delete Test Result

- Located `123.cs` at the top of the QuantConnect workspace explorer.
- Right-clicked `123.cs` and verified the file context menu.
- Selected `Delete Permanently`.
- QuantConnect displayed a confirmation dialog specifically naming `123.cs`.
- Confirmed the dialog's `Delete` action because the user explicitly requested permanent deletion of the temporary test file.
- Waited for workspace synchronization and captured the explorer again.
- Verification result: `123.cs` no longer appears in the workspace explorer.

### Delete Automation Pattern

- Destructive file deletion requires two verified steps:
  - select `Delete Permanently` from the exact file context menu
  - confirm the second dialog after verifying that it names the intended file
- Coordinate-based deletion must never proceed without an intermediate screenshot/context-menu verification.
- The confirmation dialog must be inspected before clicking `Delete`; if the filename is not the explicitly approved target, cancel.
- Trading impact: none. Only temporary test file `123.cs` was deleted.

## Cloud Workspace Mutation Test - Create Temporary Source File

- User explicitly requested creation of temporary cloud-workspace file `test1.cs`.
- Intended action: click the explorer `New File` icon, enter `test1.cs`, and press Enter.
- Expected result: `test1.cs` appears in the explorer, opens in the editor, and contains the QuantConnect default algorithm class template.
- Scope boundary: create only `test1.cs`. Do not edit existing Aegis algorithm source files or trigger a build/backtest.

### Create Test Result

- Clicked the explorer `New File` icon.
- Entered `test1.cs` and pressed Enter.
- Verification result: `test1.cs` appears in the explorer and opened automatically in the editor.
- Initial screenshot: the editor appeared blank immediately after creation.
- Delayed verification after waiting: QuantConnect asynchronously populated `test1.cs` with the default algorithm class template and default imports.
- Finding: additional `.cs` files created through the explorer toolbar receive generated default algorithm content, but rendering/population may be delayed.
- Trading impact: none. Temporary source file `test1.cs` was added to the cloud workspace. No build or backtest was intentionally triggered by this test.

### Create Automation Pattern

- Click the explorer `New File` icon.
- Type the intended filename while the inline filename editor is active.
- Press Enter.
- Capture a screenshot and verify:
  - the exact filename appears in the explorer
  - the expected editor tab is open
  - the file content matches expectations
- Wait for asynchronous template population before concluding that the editor is blank.
- Verify the generated content explicitly before proceeding.

## Cloud Workspace Mutation Test - Edit Temporary Source File

- User explicitly requested an editor automation test against temporary cloud-workspace file `test1.cs`.
- Intended action: focus the open `test1.cs` editor, select all existing generated content, cut it, enter harmless replacement text, and rely on QuantConnect autosave.
- Replacement text: `// Codex GUI automation edit test`
- Scope boundary: edit only `test1.cs`. Do not modify existing Aegis algorithm source files or trigger build/backtest actions.

### Edit Test Result

- Clicked inside the open `test1.cs` editor.
- Sent `Ctrl+A` to select all generated template content.
- Sent `Ctrl+X` to remove the selected content.
- Typed `// Codex GUI automation edit test`.
- Waited for QuantConnect autosave behavior.
- Verification result: the editor visibly shows only the replacement marker comment.
- Finding: visible Chrome GUI automation can replace cloud-workspace source-file content through editor keyboard input without manually clicking Save.
- Trading impact: none. Only temporary source file `test1.cs` was edited. No build or backtest was triggered.

### Edit Automation Pattern

- Open and visually verify the intended temporary or explicitly approved source file.
- Click inside the editor.
- Send `Ctrl+A`, then `Ctrl+X`.
- Type or paste the intended replacement text.
- Wait for autosave.
- Capture the editor and verify the exact visible content before any build or backtest step.
- Do not use this workflow against production Aegis source files without explicit user confirmation and a pre-edit backup/version reference.

## Cloud Build Control Findings

- User identified the yellow action buttons in the top-right area of the source-code editor panel.
- The first yellow button from the left is the cloud build button.
- Expected workflow after adding or modifying a cloud-workspace source file:
  - verify the intended source file change
  - click the bottom `CLOUD TERMINAL` panel's `Clear Logs` button
  - click the cloud build button
  - inspect the bottom `CLOUD TERMINAL` output for compilation results
  - inspect the adjacent `PROBLEMS` panel for reported issues
- Compilation errors should appear in the `CLOUD TERMINAL`.
- Other reported issues should appear in the `PROBLEMS` panel.
- The `Clear Logs` button is the small list-with-`x` icon near the top-right corner of the bottom `CLOUD TERMINAL` panel.
- Clear old terminal output before each new cloud build so the resulting log evidence can be attributed to the current build.

## Cloud Build Safety Boundary

- Clicking the cloud build button is a QuantConnect cloud action and must not be triggered automatically without explicit user confirmation.
- A build should be performed only after visually verifying the changed files and confirming that no unintended cloud-workspace source file was modified.
- The initial build-automation test should use only the temporary `test1.cs` file state and must not launch a backtest, optimization, or live deployment.
- Build verification should capture:
  - cleared terminal state before triggering build
  - cloud terminal output after the build completes
  - visible problems count
  - `PROBLEMS` panel contents if the count is non-zero

## Cloud Build Mutation Test - Temporary File State

- User explicitly approved a cloud build test after editing temporary source file `test1.cs`.
- Intended sequence:
  - click `Clear Logs`
  - verify the terminal is cleared
  - click the first yellow cloud-build button
  - wait for completion
  - inspect fresh `CLOUD TERMINAL` output
  - inspect `PROBLEMS` if the count is non-zero
- Scope boundary: build only. Do not launch backtest, optimization, or live deployment actions.

### Cloud Build Test Result

- Clicked the bottom `CLOUD TERMINAL` panel's `Clear Logs` button.
- Verified that previous terminal output was removed before triggering the build.
- Clicked the first yellow source-panel action button.
- Tooltip verification: `Cloud Build (Ctrl+Shift+B)`.
- Waited for fresh terminal output.
- Fresh terminal output:

```text
Building project 'AegisGrowthAllocation_BackTest' in Cloud, with Signature 'eeec1f'
Built project 'AegisGrowthAllocation_BackTest' in Cloud for Lean Engine 2.5.0.0.17756, with Id '17ce96-eeec1f'
```

- Build result: success.
- The `PROBLEMS` badge remained at `2`, so the `PROBLEMS` tab was opened for inspection.
- Observed problems: two existing obsolete-API warnings in `AegisGrowthAllocation.cs`:
  - line 83, column 13
  - line 95, column 17
- Warning text:

```text
'Security.SetDataNormalizationMode(DataNormalizationMode)' is obsolete:
'This method is obsolete. Use the SubscriptionDataConfig exposed by SubscriptionManager and the SetDataNormalizationMode() extension method'
```

- Finding: the warnings are associated with existing `AegisGrowthAllocation.cs` code, not the harmless temporary `test1.cs` comment.
- Trading impact: none. A cloud build was triggered, but no backtest, optimization, or live deployment action was launched.

### Verified Cloud Build Automation Pattern

- Click `Clear Logs`.
- Capture and verify empty terminal state.
- Click `Cloud Build (Ctrl+Shift+B)`.
- Wait for fresh terminal output.
- Verify a fresh `Built project ...` line and build id.
- If the `PROBLEMS` badge is non-zero, open the tab and inspect every visible item.
- Attribute each warning/error to its source file before deciding whether it is related to the current edit.

## Negative Cloud Build Test - Intentional Compile Error

- User explicitly requested a negative cloud-build test against temporary file `test1.cs`.
- Intended action:
  - replace `test1.cs` with deliberately invalid C#
  - clear previous Cloud Terminal logs
  - trigger cloud build
  - inspect fresh Cloud Terminal output
  - inspect `PROBLEMS`
- Invalid temporary content: `public class BrokenTest {`
- Scope boundary: edit only `test1.cs`. Do not modify production Aegis source files or trigger backtest, optimization, or live deployment actions.

### Negative Cloud Build Test Result

- Replaced temporary `test1.cs` content with deliberately invalid C#:

```csharp
public class BrokenTest {
```

- Verified the editor displayed `CS1513: } expected` for `test1.cs`.
- Clicked the bottom `CLOUD TERMINAL` panel's `Clear Logs` button.
- Verified the terminal was empty before triggering the build.
- Clicked `Cloud Build (Ctrl+Shift+B)`.
- Fresh terminal output:

```text
Building project 'AegisGrowthAllocation_BackTest' in Cloud, with Signature '78ef42'
Build Error File: test1.cs Line:1 Column:25 - } expected
```

- Build result: expected failure caused by the intentional syntax error.
- Finding: cloud compiler failures can be detected reliably by clearing stale logs, triggering a cloud build, waiting for fresh output, and checking for a `Build Error File:` line.
- Finding: the cloud terminal error identifies the source file, line, column, and compiler message. This is sufficient for automated compile-error triage before any backtest is launched.
- Trading impact: none. Only the temporary `test1.cs` file was edited. No backtest, optimization, live deployment, Object Store write, or production Aegis source-file edit was triggered.

### Negative Cloud Build Review

- Strict review completed.
- Verified scope: only temporary cloud-workspace file `test1.cs` was intentionally changed.
- Verified safety boundary: no production Aegis source file was modified.
- Verified action boundary: cloud build only; no backtest, optimization, or live deployment was launched.
- Residual state: `test1.cs` remains intentionally invalid in the QuantConnect cloud workspace so the failure is visible for inspection. Delete or restore this temporary file before the next real cloud build.

## Documentation Publish Scope

- Date: 2026-05-30.
- User requested that the local documentation changes be committed and pushed.
- Durable repository change: this automation findings note.
- Excluded from commit: root-level PNG screenshots captured during GUI verification. These are temporary local evidence artifacts, not source-controlled project assets.
- Review result: documentation-only publish scope is correct. No production algorithm file, cloud-workspace file, or trading behavior change is included in the Git commit.
