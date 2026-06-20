# Decision Index

Use this index for durable choices that may affect trading behavior, live deployment, persistence, order handling, parameter defaults, or algorithm direction. Open archived notes only when the short entry is insufficient.

## Aegis Live Safety And Deployment

| Date | Topic | Impact | Ref |
|---|---|---|---|
| 2026-04-12 | Live hardening implementation | Live-trading safety and operational behavior | [Aegis_Live_Hardening_Implementation_2026-04-12](../archive/2026-04/Aegis_Live_Hardening_Implementation_2026-04-12.md) |
| 2026-05-19 | Defensive state persistence | State recovery and persistence behavior | [Aegis_Defensive_State_Persistence_Implementation_2026-05-19](../archive/2026-05/Aegis_Defensive_State_Persistence_Implementation_2026-05-19.md) |
| 2026-05-23 | Live state startup diagnostics | Startup diagnostics and live state confidence | [Aegis_Live_State_Startup_Diagnostics_2026-05-23](../archive/2026-05/Aegis_Live_State_Startup_Diagnostics_2026-05-23.md) |
| 2026-05-23 | Deployment identity metadata | Deployment traceability and metadata | [Aegis_Deployment_Identity_Metadata_2026-05-23](../archive/2026-05/Aegis_Deployment_Identity_Metadata_2026-05-23.md) |
| 2026-05-25 | Paper live stable tag | Stable paper-live reference point | [Aegis_Paper_Live_Stable_Tag_2026-05-25](../archive/2026-05/Aegis_Paper_Live_Stable_Tag_2026-05-25.md) |
| 2026-05-26 | Operational hardening | Deployment and operational safety hardening | [Aegis_Operational_Hardening_Implementation_2026-05-26](../archive/2026-05/Aegis_Operational_Hardening_Implementation_2026-05-26.md) |

## Aegis Order And Execution Behavior

| Date | Topic | Impact | Ref |
|---|---|---|---|
| 2026-04-21 | Execution realism fix | Backtest realism and fill assumptions | [Aegis_Execution_Realism_Fix_2026-04-21](../archive/2026-04/Aegis_Execution_Realism_Fix_2026-04-21.md) |
| 2026-04-23 | Order fills warning root cause | Order/fill diagnostics and warning interpretation | [Aegis_OrderFillsWarning_RootCause_2026-04-23](../archive/2026-04/Aegis_OrderFillsWarning_RootCause_2026-04-23.md) |

## Aegis Parameter And Algorithm Decisions

| Date | Topic | Impact | Ref |
|---|---|---|---|
| 2026-04-18 | Regime parameterization | Regime model parameter behavior | [Aegis_Regime_Parameterization_2026-04-18](../archive/2026-04/Aegis_Regime_Parameterization_2026-04-18.md) |
| 2026-05-09 | Weak stress overlay implementation | Defensive overlay behavior | [Aegis_Weak_Stress_Overlay_Implementation_2026-05-09](../archive/2026-05/Aegis_Weak_Stress_Overlay_Implementation_2026-05-09.md) |
| 2026-05-09 | Pre-weak guard implementation | Defensive guard behavior | [Aegis_PreWeak_Guard_Implementation_2026-05-09](../archive/2026-05/Aegis_PreWeak_Guard_Implementation_2026-05-09.md) |
| 2026-05-10 | Severe crash override implementation | Crash regime allocation behavior | [Aegis_Severe_Crash_Override_Implementation_2026-05-10](../archive/2026-05/Aegis_Severe_Crash_Override_Implementation_2026-05-10.md) |
| 2026-05-13 | Stateful severe crash mode | Stateful crash/recovery behavior | [Aegis_Stateful_Severe_Crash_Mode_Implementation_2026-05-13](../archive/2026-05/Aegis_Stateful_Severe_Crash_Mode_Implementation_2026-05-13.md) |
| 2026-05-20 | Stress band parameterization | Defensive stress-band configuration | [Aegis_Stress_Band_Parameterization_Implementation_2026-05-20](../archive/2026-05/Aegis_Stress_Band_Parameterization_Implementation_2026-05-20.md) |
| 2026-05-23 | Promote OptStress defaults | Default parameter promotion | [Aegis_Promote_OptStress_Defaults_2026-05-23](../archive/2026-05/Aegis_Promote_OptStress_Defaults_2026-05-23.md) |
| 2026-06-08 | Weak sleeve target parameterization | Weak sleeve target configuration | [Aegis_Weak_Sleeve_Target_Parameterization_2026-06-08](../archive/2026-06/Aegis_Weak_Sleeve_Target_Parameterization_2026-06-08.md) |
| 2026-06-12 | Candidate B next optimization proposal | Validation-first constraint before severe-crash changes | [Aegis_CandidateB_Next_Optimization_Proposal_2026-06-12](../archive/2026-06/Aegis_CandidateB_Next_Optimization_Proposal_2026-06-12.md) |
| 2026-06-13 | Recent Aegis optimization review | Next move: parameter-only pre-weak selectivity test before severe-crash work | [Aegis_Recent_Optimization_Review_2026-06-13](../archive/2026-06/Aegis_Recent_Optimization_Review_2026-06-13.md) |
| 2026-06-14 | Next optimization strategy | Keep Candidate B baseline; source-code audit with stress protection first | [Aegis_Next_Optimization_Strategy_2026-06-14](../archive/2026-06/Aegis_Next_Optimization_Strategy_2026-06-14.md) |
| 2026-06-15 | PreWeak Recovery Step proposal review | Revise Candidate C: recovery-gated idea accepted, but start more conservatively than G18 | [Aegis_PreWeak_RecoveryStep_Proposal_Review_2026-06-15](../archive/2026-06/Aegis_PreWeak_RecoveryStep_Proposal_Review_2026-06-15.md) |
| 2026-06-15 | Candidate C PreWeak Recovery v2 review | Accept with minor edits; fixed-candidate stress-preservation test approved | [Aegis_CandidateC_PreWeak_Recovery_v2_Second_Round_Review_2026-06-15](../archive/2026-06/Aegis_CandidateC_PreWeak_Recovery_v2_Second_Round_Review_2026-06-15.md) |
| 2026-06-15 | Candidate C PreWeak Recovery v2 implementation plan | Default-off implementation plan with live-state persistence and validation gates | [Aegis_CandidateC_PreWeak_Recovery_v2_Implementation_Plan_2026-06-15](../archive/2026-06/Aegis_CandidateC_PreWeak_Recovery_v2_Implementation_Plan_2026-06-15.md) |
| 2026-06-16 | QuantConnect file-size constraint | Split Aegis source into smaller partial files before cloud upload; avoid partial-save compile cascades | [Aegis_QuantConnect_File_Size_Limit_2026-06-16](../archive/2026-06/Aegis_QuantConnect_File_Size_Limit_2026-06-16.md) |
| 2026-06-16 | Candidate C stress validation | Recovery feature preserved 2021-2022 stress behavior but had zero activations; test unchanged long-window next | [Aegis_CandidateC_PreWeakRecoveryV2_Stress04_2021_2022_Analysis_2026-06-16](../archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_Stress04_2021_2022_Analysis_2026-06-16.md) |
| 2026-06-16 | Candidate C long validation | Promising but not promotion-ready; run date-matched Candidate B control ending 2026-01-01 | [Aegis_CandidateC_PreWeakRecoveryV2_Long_2016_2026-01-01_Analysis_2026-06-16](../archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_Long_2016_2026-01-01_Analysis_2026-06-16.md) |
| 2026-06-16 | Candidate B control attempt | Not a valid Candidate C control because actual chart ran past requested 2026-01-01 end date | [Aegis_CandidateB_DateMatched_Control_Attempt_2016_2026-01-01_Analysis_2026-06-16](../archive/2026-06/Aegis_CandidateB_DateMatched_Control_Attempt_2016_2026-01-01_Analysis_2026-06-16.md) |
| 2026-06-17 | Latest-source Candidate B behavior | Candidate B behavior now differs from prior tag-era artifact because orders are MOC instead of Market; confirm exact tag next | [Aegis_LatestSource_CandidateBBehavior_MinParams_2016_2026-01-01_Analysis_2026-06-17](../archive/2026-06/Aegis_LatestSource_CandidateBBehavior_MinParams_2016_2026-01-01_Analysis_2026-06-17.md) |
| 2026-06-17 | Exact tag Candidate B confirmation | Tag rerun confirms weaker Candidate B read; Candidate C likely better but needs same-execution validation before deployment | [Aegis_TagCandidateB_MinParams_2016_2026-01-01_Confirmation_2026-06-17](../archive/2026-06/Aegis_TagCandidateB_MinParams_2016_2026-01-01_Confirmation_2026-06-17.md) |
| 2026-06-17 | Candidate C same-execution validation | Candidate C improves over Candidate B under Market On Close, but margin is modest; run stress validation before paper-test tag | [Aegis_CandidateC_PreWeakRecoveryV2_SameExecution_2016_2026-01-01_Analysis_2026-06-17](../archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_SameExecution_2016_2026-01-01_Analysis_2026-06-17.md) |
| 2026-06-17 | Candidate C same-execution stress validation | Do not tag Candidate C yet; run Candidate B MOC stress control because Candidate C stress was weaker than older Market-order baselines | [Aegis_CandidateC_PreWeakRecoveryV2_SameExecution_Stress04_2021_2022_Analysis_2026-06-17](../archive/2026-06/Aegis_CandidateC_PreWeakRecoveryV2_SameExecution_Stress04_2021_2022_Analysis_2026-06-17.md) |
| 2026-06-17 | Candidate B same-execution stress control | Candidate B remains the stress baseline; Candidate C misses the same-execution stress gate | [Aegis_TagCandidateB_SameExecution_Stress04_2021_2022_Control_2026-06-17](../archive/2026-06/Aegis_TagCandidateB_SameExecution_Stress04_2021_2022_Control_2026-06-17.md) |
| 2026-06-18 | Candidate B paper-live readiness review | Active paper-live segment is clean, but approve only small-capital real validation pending holdings/equity check | [Aegis_CandidateB_PaperLive_Readiness_Review_2026-06-18](../archive/2026-06/Aegis_CandidateB_PaperLive_Readiness_Review_2026-06-18.md) |
| 2026-06-19 | Candidate B return-gap attribution sprint | Do not implement Candidate D yet; require same-execution weekly diagnostic reruns before choosing the next lever | [Aegis_CandidateB_Return_Gap_Attribution_Sprint_2026-06-19](../archive/2026-06/Aegis_CandidateB_Return_Gap_Attribution_Sprint_2026-06-19.md) |
| 2026-06-19 | B/C diagnostic set completeness | Do not choose Candidate D from this set; rerun paired weekly diagnostics using current source and segmented long windows | [Aegis_BC_Diagnostic_Set_Completeness_2026-06-19](../archive/2026-06/Aegis_BC_Diagnostic_Set_Completeness_2026-06-19.md) |
| 2026-06-20 | Candidate B/C return-gap attribution | Candidate D remains blocked; next approved direction is a bounded post-stress redeployment hypothesis with Candidate B stress preservation | [Aegis_CandidateB_Return_Gap_Attribution_Analysis_2026-06-20](../archive/2026-06/Aegis_CandidateB_Return_Gap_Attribution_Analysis_2026-06-20.md) |
