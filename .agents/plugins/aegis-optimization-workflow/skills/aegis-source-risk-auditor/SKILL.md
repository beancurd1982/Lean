---
name: aegis-source-risk-auditor
description: Perform read-only AegisGrowthAllocation source and risk audits before candidate changes. Use when investigating regime logic, PreWeak behavior, defensive ranking, sleeve targets, replacement churn, Market On Close execution, Object Store state, live-state recovery, order handling, or why Candidate B/C stress behavior differs.
---

# Aegis Source Risk Auditor

Audit source behavior and risk before implementation. Do not edit files.

## Required Context

Read the coordinator context path when candidate history matters. Then inspect only relevant source files under `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/`.

## Audit Focus

Check:

- regime detection and state transitions;
- PreWeak trigger, exit, recovery, and hysteresis behavior;
- defensive universe and ranking quality;
- growth/defensive/cash sleeve mechanics;
- replacement-score and hold-stability churn behavior;
- Market On Close assumptions and order timing;
- Object Store persistence and live-state recovery risk;
- deployment identity and parameter-default implications.

## Output

Return a ranked table with:

- lever;
- affected code path;
- expected decision value;
- implementation risk;
- validation cost;
- required evidence;
- whether user confirmation is required before implementation.

Flag any live-state, persistence, order-handling, or deployment-default risk as requiring explicit user confirmation.
