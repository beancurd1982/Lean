# BackTestLogs

This folder stores downloaded Lean Cloud backtest log files for `AegisGrowthAllocation`.

Automation provided here does two things:

1. Renames new `.txt` log files to a normalized format based on file creation time.
2. Updates `log-index.csv` so future sessions can see which files were already processed.

Normalized naming convention:

`YYYY-MM-DD_HHmmss__AegisGrowthAllocation__<sanitized-original-stem>.txt`

Example:

`2026-04-11_222549__AegisGrowthAllocation__Pensive-Green-Lion_logs.txt`

Files:

- `Invoke-BackTestLogRename.ps1`: one-shot safe renamer and index updater.
- `log-index.csv`: automatic index of processed files.

Notes:

- Files that already match the normalized pattern are left unchanged.
- The index defaults each new file to `status=unreviewed`.
- The review note column can be updated later when a project-note review is completed.
- Per `AGENTS.md`, this is an assistant-invoked workflow, not a background scheduled task workflow.
- Downloaded `.txt` log files are local workspace artifacts and are intentionally git-ignored in this folder.
- Cross-session review tracking lives in `log-index.csv` and the linked `project-notes/` review files, not in committed raw log text files.
