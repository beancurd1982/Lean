---
id: AEGIS-WF-2026-06-18-OPTIMIZATION-WORKFLOW-PLUGIN
type: workflow
status: accepted
date: 2026-06-18
topic: AegisGrowthAllocation
tags: [aegis, workflow, codex-plugin, skills, optimization]
related:
  - project-notes/archive/2026-06/Aegis_Next_Optimization_Strategy_2026-06-14.md
  - project-notes/archive/2026-06/Aegis_TagCandidateB_SameExecution_Stress04_2021_2022_Control_2026-06-17.md
files:
  - .agents/plugins/aegis-optimization-workflow
---

# Aegis Optimization Workflow Plugin - 2026-06-18

## Agent Summary

Read this when using or updating the repo-local Aegis optimization workflow plugin. The plugin standardizes Candidate B/C/D research so future optimization starts with a bounded hypothesis, compares only same-execution evidence, preserves Candidate B as the stress baseline, and blocks paper/live promotion until live-safety checks pass.

## Decision Or Finding

Create a repo-local Codex plugin at `.agents/plugins/aegis-optimization-workflow` with six skills:

- `aegis-optimization-coordinator`
- `aegis-hypothesis-designer`
- `aegis-source-risk-auditor`
- `aegis-candidate-implementer`
- `aegis-backtest-evaluator`
- `aegis-live-readiness-reviewer`

The plugin is workflow infrastructure only. It does not change Aegis trading behavior.

## Evidence

The workflow encodes the current Candidate B/C state:

- Candidate B remains the 2021-2022 same-execution stress baseline.
- Candidate C modestly improves the same-execution long window.
- Candidate C misses the Candidate B same-execution stress gate.
- Future candidates should be hypothesis-led and attribution-led instead of broad optimizer sweeps.

## Verification

Validation should include:

- skill frontmatter checks with `quick_validate.py`;
- plugin manifest validation with `validate_plugin.py`;
- script smoke tests against existing Candidate B/C artifacts;
- review confirming no trading source files changed.

## Risks And Open Questions

- The repo-local marketplace must be installed or opened through Codex plugin UI before the skills are available in future sessions.
- The helper scripts are reporting tools only; `Invoke-BackTestLogRename.ps1` remains the source of truth for log normalization.
- Candidate gates should be updated when a newer decision note supersedes Candidate B/C evidence.
