# Aegis Candidate Hypothesis Template

Use this template before implementing or optimizing a new AegisGrowthAllocation candidate.

## Candidate ID

Name the candidate, for example `Candidate D - PreWeak Exit Hysteresis`.

## Baseline

State the baseline candidate and exact comparison artifacts.

## Proposed Change

Describe the smallest behavior or parameter change being tested.

## Mechanism

Explain why this should preserve Candidate B stress behavior and improve long-window performance.

## Expected Stress Effect

State expected 2021-2022 behavior versus Candidate B and the accepted stress gate.

## Expected Long-Window Effect

State expected 2016-2026 behavior versus Candidate C and the accepted long-window gate.

## Affected Files

List exact source, config, or test files expected to change.

## Parameters

List new or changed parameters, defaults, allowed values, and whether the behavior is default-off.

## Validation Matrix

Include at least:

- Candidate B same-execution stress control;
- candidate same-execution stress run;
- Candidate B/C same-execution long comparison;
- candidate same-execution long run.

## Rejection Criteria

Define the exact metric or behavior failures that reject the candidate.

## Live-Safety Notes

State whether the change touches order handling, Object Store, live state, restart behavior, deployment identity, or default live behavior.
