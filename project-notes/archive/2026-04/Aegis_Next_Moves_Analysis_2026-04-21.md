## 2026-04-21 - Aegis next-moves analysis

### Task
- Review `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs` and related Aegis markdown files.
- Determine the highest-value next optimization moves based on the documented plan, completed work, and current implementation state.

### Files to touch
- `project-notes/Aegis_Next_Moves_Analysis_2026-04-21.md`

### Notes
- Analysis-only step.
- No executable algorithm logic or live-trading behavior is being changed in this step.
- QuantConnect documentation URLs now recorded in the newer execution-realism analysis notes and warning-fix plan for future issue triage.
- Reviewed:
  - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/AegisGrowthAllocation.cs`
  - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StrategyConfig.cs`
  - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/RegimeModel.cs`
  - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/StockSelectionModel.cs`
  - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PortfolioManager.cs`
  - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/Docs/AegisGrowthAllocation_Implementation_Spec_V1.md`
  - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/Docs/Aegis_Live_Trading_Hardening_Plan_V1.md`
  - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/Docs/QuantConnect_Live_Strategy_Design_Document_V1.md`
  - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/Docs/QuantConnect_Live_Strategy_Design_Derivation_Record.md`
  - `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/Plans/Aegis_Backtest_Optimization_Plan_V1.md`
  - `project-notes/Aegis_Regime_Parameterization_2026-04-18.md`
  - `project-notes/Aegis_Optimization_Round1_Analysis_2026-04-18.md`
  - `project-notes/Aegis_Optimization_Round2_Parameterization_2026-04-18.md`
  - `project-notes/Aegis_Optimization_Round2_Selection_2026-04-18.md`
  - `project-notes/Aegis_Backtest_V5_Review_2026-04-18.md`
- Current status inferred from the notes:
  - baseline backtest completed
  - round-1 regime optimization completed and `V2` selected as the regime baseline
  - round-2 turnover optimization completed and `hold-stability-bonus = 2` selected
  - `V5` confirmation backtest validated the current default baseline
- Recommended next moves:
  1. Address the remaining execution-realism warning (`OrderFillsDuringExtendedMarketHoursAnalysis`) before opening another optimization round.
  2. Implement the specification's tolerance-band rebalance gating so the planner does not keep targeting exact sleeve weights every review.
  3. Finish the undeployed-capital module so released reserve affects target weights instead of being logged only.
  4. Run targeted validation on unresolved design questions still called out in the spec:
     - Monday 10:00 AM versus Friday near close
     - `TSLA` supplemental versus core
     - defensive-sleeve concentration in weak regimes
- Key code/spec gaps found:
  - `PortfolioManager.cs` computes `releasedReserve` but does not feed it back into target weights.
  - The implementation spec requires tolerance-band rebalance gating, but the current planner always rebuilds exact target weights for the selected symbols.
  - The project notes still identify extended-hours fills as the main remaining realism risk after `V5`.
- Revised warning-fix direction from later documentation research:
  - Prioritize exchange-aware scheduling (`WeekStart(symbol)` plus `AfterMarketOpen(symbol, minutes)`) and explicit regular-hours execution checks before revisiting other execution-model changes.

### Review
- Strict review completed after updating this note.
- Verified the note remains ASCII-safe and consistent with the reviewed source and project-note history.
- No code or live-trading behavior changed in this step.
