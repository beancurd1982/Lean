# AGENTS.md (Project Workflow Rules)

## Purpose
This file captures the working rules and habits for this project so future work stays consistent and auditable.

## Core Rules (Must Follow)
1. Every step must be recorded in the project notes markdown before moving to the next step.
2. Always perform a strict code review after applying changes, and document the review results.
3. Always ask for clarification if an important requirement is uncertain. Do not guess.
4. Always update the project notes before starting the next task or issue.

## Documentation Rules
1. Use the `project-notes/` folder for all workflow logs and plans.
2. Each change should be logged with date, summary, and files touched.
3. Record unresolved risks or open questions explicitly.

## Implementation Rules
1. Make minimal, focused changes per step.
2. Prefer safe, auditable behavior over silent fallback logic.
3. Keep encoding stable; avoid introducing non-ASCII unless already present and required.
4. If a change could affect live trading behavior, call it out and confirm before proceeding.

## Multi-Agent Workflow Rules
1. Default agent model:
   - One **Coordinator** agent owns planning, sequencing, project-note updates, integration, verification, and final sign-off.
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
   - The Coordinator logs the task in `project-notes/`.
   - The Coordinator gathers context and spawns agents when useful.
   - Explorer work happens before implementation when risk discovery is needed.
   - Workers implement only after scope and file ownership are clear.
   - The Coordinator integrates results and triggers an independent review.
   - The task is not considered complete until code review, verification, and note updates are finished.
5. Ownership rules:
   - The Coordinator always owns `project-notes/`.
   - Each worker must have an explicit, non-overlapping file or responsibility scope whenever possible.
   - Multiple workers must not edit the same file in parallel unless that risk is explicitly accepted.
6. Safety rules for delegation:
   - Read-only exploration is preferred before implementation on high-risk tasks.
   - Live-trading, persistence, order lifecycle, and restart behavior changes require a separate review pass.
   - If requirements are ambiguous and behavior could affect trading or state recovery, the Coordinator must stop and ask for clarification.
7. Completion rule:
   - A delegated workflow is only complete when the Coordinator has reviewed outputs, resolved findings, verified results, and recorded the outcome in `project-notes/`.

## Review Rules
1. Reviews must prioritize: safety, correctness, and live-trading risks.
2. If no issues are found, explicitly state that in the review log.
3. If a change introduces a new risk, document it and propose mitigation.

## Maintenance Rule
1. Regularly review `AGENTS.md` and update rules if workflow improvements are discovered.
