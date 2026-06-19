---
name: aegis-candidate-implementer
description: Implement one approved AegisGrowthAllocation candidate change after a pre-registered hypothesis exists. Use for scoped Candidate D/E code or parameter changes, never for broad exploratory optimization, and always with build/test verification and strict post-change review.
---

# Aegis Candidate Implementer

Implement only an approved candidate hypothesis.

## Preconditions

- A completed hypothesis proposal exists.
- The user has approved implementation.
- Affected files and validation gates are explicit.

Stop if any precondition is missing.

## Implementation Rules

- Keep changes minimal and scoped to the approved hypothesis.
- Preserve Candidate B defaults unless the hypothesis explicitly changes them.
- Make experimental behavior default-off unless the user explicitly approves a default change.
- Do not change live-state, Object Store, order handling, deployment identity, or restart behavior without explicit user confirmation.
- Keep QuantConnect cloud file-size constraints in mind; avoid expanding already-large files when a partial file is more appropriate.

## Verification

Run the narrowest relevant tests first, then the project build used by Aegis work:

```powershell
dotnet build Tests/QuantConnect.Tests.csproj -nologo
```

If a known unrelated SGX map-file/Python test-host issue appears, report it as unrelated rather than treating it as candidate evidence.

## Review

After implementation, perform a strict code review focused on:

- behavioral correctness;
- live-trading safety;
- default preservation;
- order and persistence side effects;
- missing tests;
- validation gates still matching the approved hypothesis.
