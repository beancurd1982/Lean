Date: 2026-04-11

Summary:
- Prepared the Aegis BackTestLogs workflow changes for cleanup and publish.
- Goal is to keep the repo clean while preserving a tracked assistant workflow for renaming and indexing local downloaded logs.

Files Intended For Commit:
- AGENTS.md
- Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/.gitignore
- Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Invoke-BackTestLogRename.ps1
- Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/README.md
- Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv
- project-notes/Aegis_BackTestLog_Automation_2026-04-11.md
- project-notes/Aegis_BackTestLog_Workflow_Correction_2026-04-11.md
- project-notes/Aegis_BackTestLog_Publish_2026-04-11.md

Files Intended To Remain Local Only:
- Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/*.txt

Cleanup Plan:
- Add a repo-local `.gitignore` in `BackTestLogs` so downloaded log text files do not keep the working tree dirty.
- Keep the normalized filename and review state in `log-index.csv` for cross-session traceability.

Risk Notes:
- No trading code is affected.
- The only behavior change is repository hygiene for local backtest log artifacts.
