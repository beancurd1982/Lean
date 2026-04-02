# Live Strategy Design Next Steps 2026-03-29
Date: 2026-03-29

## Scope
- Read `Documentation/QuantConnect_Live_Strategy_Design_Document_V1.md`.
- Read `Documentation/QuantConnect_Live_Strategy_Design_Derivation_Record.md`.
- Compare the design intent with the current implementation baseline in `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs`.
- Determine the most practical next steps without changing live-trading behavior.

## Progress Log
- 2026-03-29: Started design-document review and next-step analysis.
- 2026-03-29: Confirmed both design documents converge on the same two follow-up directions: define a backtestable specification, or first split the design into 2-3 prototype variants.
- 2026-03-29: Compared the V1 design against `MultiStockV33_Stable_Base.cs` and confirmed the current V33 algorithm is not yet an implementation of the V1 architecture.
- 2026-03-29: Completed next-step recommendation and recorded the result in this note.

## Findings
- The two design documents are internally consistent. Both say the immediate forward path is:
  - either convert V1 into a backtestable specification,
  - or first split V1 into 2-3 prototype variants and compare them.
- The design document itself explicitly recommends starting with prototype variants first, before code.
- The current `MultiStockV33_Stable_Base.cs` is a hardened operational baseline, not the V1 design translated into code. Key mismatches:
  - It runs a daily per-symbol SMA/threshold/trailing-stop process rather than a weekly top-down decision flow.
  - It has no three-state `Favorable / Neutral / Weak` risk regime.
  - It has no defensive sleeve, no crypto sleeve, and no undeployed-capital module.
  - Its symbol universe and sizing are static per symbol rather than driven by the V1 pool/scoring/turnover framework.
- The latest V33 backtest review reported max drawdown `17.2%`, which is above the stated V1 target of roughly `15% or less`. That strengthens the case for doing architecture/spec work first instead of directly iterating parameters in the current design.

## Recommended Next Steps
1. Freeze `MultiStockV33_Stable_Base.cs` as the current hardened baseline for paper/live-style safety work, and do not repurpose it directly into the new V1 strategy without an explicit confirmation.
2. Create a short prototype-selection note that defines exactly 3 variants from V1:
   - Balanced
   - More defensive
   - More offensive
3. Choose one prototype as the first implementation target. The default recommendation is the Balanced version because it matches the stated objective function most closely.
4. Convert the chosen prototype into a backtestable specification before touching trading logic. At minimum this spec should pin down:
   - core pool and supplemental pool membership rules,
   - defensive assets and crypto instruments,
   - risk-state inputs and transition conditions,
   - scoring formula and qualification filters,
   - turnover cap / replacement-friction rules,
   - capital-inflow detection and undeployed-cash release rules,
   - weekly execution schedule and portfolio construction logic.
5. After the specification is stable, implement it as a new strategy file or versioned branch rather than mutating V33 in place. This is the safer path because V33 currently serves as the hardened operational baseline.

## Open Questions / Risks
- Important implementation choice still unresolved: should the first V1 implementation be a new class (recommended, e.g. V34) or should V33 be repurposed? This affects live-trading safety and should be confirmed before code changes.
- The design documents intentionally stop before parameter-level rules. If those rules are skipped and coding starts immediately, the implementation will end up guessing on risk-state logic, defensive allocation logic, and capital-release behavior.
- Any later attempt to merge the V1 architecture directly into the current V33 class would materially affect trading behavior and should be treated as a separate confirmed task.

## Review Log
- 2026-03-29: Strict review completed for this note and its conclusions. No issues found in the documentation-analysis workflow. Main conclusion remains: the next concrete task should be prototype selection plus backtestable-spec definition, not direct modification of `MultiStockV33_Stable_Base.cs`.
