# AGENTS.md (Project Workflow Rules)

## Purpose
This file captures the working rules and habits for this project so future work stays consistent and auditable.

## Core Rules (Must Follow)
1. Document by risk and decision value: use final responses for ordinary low-risk work, and create durable project notes only for safety-critical, decision-grade, reusable workflow, multi-agent debate, or user-requested work.
2. Always perform a strict code review after applying changes; record the review in project notes only when the work is note-worthy under Rule 1.
3. Always ask for clarification if an important requirement is uncertain. Do not guess.
4. Preserve auditable records for anything affecting live trading behavior, persistence/Object Store, order handling, deployment safety, parameter selection, or algorithm decisions derived from backtests.

## Documentation Rules
1. Use `project-notes/` as a low-token knowledge base, not a running diary. Start with `project-notes/README.md`, then the relevant `project-notes/_indexes/*.md` file, then open linked detailed notes only when needed.
2. Do not scan or open `project-notes/archive/` directly unless an index points there or the user requests historical investigation.
3. Create markdown notes only for safety-critical, decision-grade, reusable workflow, multi-agent debate, or user-requested work. Routine exploration, small edits, formatting, minor tooling work, and ordinary low-risk reviews belong in the final response.
4. When a project note is required, include metadata or a short agent summary, date, summary, files touched, verification, unresolved risks, and open questions.
5. Keep durable note indexes current with compact entries that link to detailed notes instead of duplicating content.

## Token And Memory Rules
1. Use AgentMemory for stable, reusable project knowledge: workflow lessons, durable decisions, repeated failure/root-cause fixes, user preferences, deployment assumptions, and important algorithm defaults.
2. Do not save temporary facts to AgentMemory: raw logs, screenshots, one-off tab names, intermediate guesses, every file read, or ordinary task status.
3. Use the context-compression skill for long sessions, phase changes, major QuantConnect automation work, multi-agent debates, backtest/optimization rounds, or before restarting Codex.
4. Prefer anchored structured compression with these sections when useful: session intent, current state, decisions made, files modified, files read, backtest/optimization results, known risks, and next steps.
5. Optimize for tokens per completed task, not smallest possible context; preserve file paths, parameter values, error messages, decisions, and next actions to avoid costly re-discovery.
6. For project note retrieval, read `project-notes/README.md` first, then one relevant `_indexes/*.md` file, then only the linked note required for the task. Avoid archive-wide reads unless rebuilding indexes or investigating history.
7. At major task boundaries, save durable lessons to AgentMemory and compress context only when useful; create or update markdown notes only when required by the Documentation Rules.

## Implementation Rules
1. Make minimal, focused changes per step.
2. Prefer safe, auditable behavior over silent fallback logic.
3. Keep encoding stable; avoid introducing non-ASCII unless already present and required.
4. If a change could affect live trading behavior, call it out and confirm before proceeding.

## BackTest Log Rules
1. For `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs`, do not install background automation or OS-level scheduled tasks unless the user explicitly asks for that.
2. When the user says a new Aegis backtest log file was added, the assistant should run the repo-local one-shot renamer:
   - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/Invoke-BackTestLogRename.ps1`
3. New Aegis backtest logs must be normalized to:
   - `YYYY-MM-DD_HHmmss__AegisGrowthAllocation__<sanitized-original-stem>.txt`
4. The assistant must keep `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/BackTestLogs/log-index.csv` updated:
   - add new rows when logs are normalized
   - set `status=reviewed` after a log has been analyzed
   - set `review_ref=<project-note-path>` for decision-grade analysis, or `review_ref=inline:<short-summary>` for routine reviews
5. When choosing which Aegis log to analyze, prefer:
   - the newest `status=unreviewed` row in `log-index.csv`
   - otherwise the newest normalized `.txt` file in `BackTestLogs`
6. Treat `log-index.csv` as the source of truth for routine backtest/log reviews. Markdown notes are required only when the analysis affects a decision, exposes risk, compares runs/candidates, or recommends an algorithm/configuration change.
7. Keep decision-relevant backtest markdown discoverable through `project-notes/_indexes/backtest-index.md` rather than searching archived notes directly.

## Multi-Agent Workflow Rules
1. Default agent model:
   - One **Coordinator** agent owns planning, sequencing, integration, verification, and final sign-off.
   - Additional agents are spawned only when their work materially improves speed, coverage, or safety.
2. Agent roles:
   - **Risk Explorer**: read-only analysis of code paths, assumptions, affected files, and failure modes.
   - **Core Worker**: implements the main code change on a bounded write scope.
   - **Validation Worker**: owns tests, validation helpers, scenario checks, or verification-oriented code on a bounded write scope.
   - **Review Worker**: performs an independent post-implementation review and must not be the primary implementation owner.
3. Automatic task routing:
   - Simple documentation or single-file low-risk work: Coordinator only.
   - Medium multi-file work: Coordinator + Risk Explorer + Core Worker.
   - Safety-critical trading, persistence, restart, or order-handling work: Coordinator + Risk Explorer + Core Worker + Validation Worker + Review Worker.
4. Coordination loop:
   - The Coordinator records durable notes only when the work is note-worthy under Core Rule 1.
   - The Coordinator gathers context and spawns agents when useful.
   - Explorer work happens before implementation when risk discovery is needed.
   - Workers implement only after scope and file ownership are clear.
   - The Coordinator integrates results and triggers an independent review.
   - The task is not considered complete until code review and verification are finished; note updates are required only for note-worthy work.
5. Ownership rules:
   - The Coordinator owns `project-notes/` only when durable notes are required.
   - Each worker must have an explicit, non-overlapping file or responsibility scope whenever possible.
   - Multiple workers must not edit the same file in parallel unless that risk is explicitly accepted.
6. Safety rules for delegation:
   - Read-only exploration is preferred before implementation on high-risk tasks.
   - Live-trading, persistence, order lifecycle, and restart behavior changes require a separate review pass.
   - If requirements are ambiguous and behavior could affect trading or state recovery, the Coordinator must stop and ask for clarification.
7. Completion rule:
   - A delegated workflow is only complete when the Coordinator has reviewed outputs, resolved findings, verified results, and recorded the outcome when note-worthy.

## Review Rules
1. Reviews must prioritize: safety, correctness, and live-trading risks.
2. If no issues are found, explicitly state that in the final response or project note, depending on the documentation threshold.
3. If a change introduces a new risk, document it in the final response; use project notes when the risk is safety-critical, decision-grade, or unresolved.

## Maintenance Rule
1. Update `AGENTS.md` when a concrete workflow improvement is discovered; avoid maintenance work that only adds process overhead.
