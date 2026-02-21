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

## Review Rules
1. Reviews must prioritize: safety, correctness, and live-trading risks.
2. If no issues are found, explicitly state that in the review log.
3. If a change introduces a new risk, document it and propose mitigation.

## Maintenance Rule
1. Regularly review `AGENTS.md` and update rules if workflow improvements are discovered.
