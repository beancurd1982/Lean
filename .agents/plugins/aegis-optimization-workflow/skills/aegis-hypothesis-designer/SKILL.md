---
name: aegis-hypothesis-designer
description: Design pre-registered AegisGrowthAllocation candidate hypotheses before optimization or implementation. Use for Candidate B improvement ideas, Candidate D/E proposals, bounded QuantConnect optimization plans, and converting vague Aegis performance goals into measurable stress and long-window validation gates.
---

# Aegis Hypothesis Designer

Convert optimization ideas into bounded, testable candidate proposals.

## Required Inputs

Read:

- `.agents/plugins/aegis-optimization-workflow/references/candidate-gates.md`
- `.agents/plugins/aegis-optimization-workflow/references/hypothesis-template.md`

If project evidence is needed, use the coordinator context path. Do not scan archive notes directly.

## Design Rules

- Reject vague "try optimizer and see" requests until they become bounded hypotheses.
- Name the candidate explicitly, for example `Candidate D - PreWeak Exit Hysteresis`.
- Identify the exact mechanism expected to improve results.
- Define stress-window and long-window gates before implementation.
- Define rejection criteria before implementation.
- Keep the first implementation small enough that failure is informative.
- Prefer default-off experimental behavior unless the user explicitly approves a default change.

## Required Proposal Shape

Use the template from `references/hypothesis-template.md` and fill every section:

- Candidate ID
- Baseline
- Proposed change
- Mechanism
- Expected stress effect
- Expected long-window effect
- Affected files
- Parameters
- Validation matrix
- Rejection criteria
- Live-safety notes

If any section cannot be filled from available evidence, ask for clarification before proposing implementation.
