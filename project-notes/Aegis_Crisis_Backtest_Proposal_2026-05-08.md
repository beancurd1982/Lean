# Aegis Crisis Backtest Proposal - 2026-05-08

## Step 1: Proposal Intake

Date:
- 2026-05-08

Summary:
- User wants a proposal only, with no code or config changes, for how to evaluate `AegisGrowthAllocation` under extreme market conditions.
- The explicit concern is whether the algorithm remains defensive and survivable through hard periods such as 2008 and 2020, with additional interest in 2021 behavior.

Files reviewed:
- `project-notes/Aegis_Optimization_Round2_Relaunch_Plan_2026-04-24.md`
- `project-notes/Aegis_Warning_Fix_Plan_2026-04-21.md`
- `project-notes/Aegis_QC_Docs_Reassessment_2026-04-22.md`

Current baseline context:
- The working baseline is the current `AegisGrowthAllocation` source with the `V10` post-optimization evidence noted in the round-2 relaunch note.
- Current concern is no longer basic order-lifecycle robustness; the remaining operational issue is the known QuantConnect analyzer warning.
- The new proposal should therefore focus on portfolio defensiveness and crisis behavior, not execution-path redesign.

Initial recommendation:
- Use crisis windows as targeted validation and veto tests, not as the primary optimization objective.
- Keep the broad-history baseline as the anchor, then add a small scenario suite around 2008, 2020, and the 2021 to 2022 transition regime.

## Step 2: Proposed Validation Windows

Date:
- 2026-05-08

Recommended windows:
- `2007-10-01` to `2010-12-31`
- `2019-07-01` to `2020-12-31`
- `2021-01-01` to `2022-12-31`

Rationale:
- The first window captures the lead-in, crash, and recovery of the global financial crisis rather than only the worst drawdown months.
- The second window captures the pre-Covid setup, the fast crash, and the initial recovery.
- The third window covers the post-crash rotation period plus the 2022 inflation and rate-shock bear market.

## Step 3: Proposed Evaluation Method

Date:
- 2026-05-08

Recommendation:
- Use these windows as validation gates against the current broad-history baseline rather than as the primary optimization target.

Suggested decision framework:
- keep the current full-period backtest as the anchor
- run the current parameter set unchanged through each crisis window
- reject candidate changes that improve the broad baseline but fail the crisis suite

Metrics to track:
- max drawdown
- drawdown duration and recovery time
- cumulative return and CAGR over each window
- downside capture versus `SPY`
- order count, fees, and turnover during stress
- average cash or defensive sleeve exposure during the worst phases

Interpretation standard:
- the algorithm should lose materially less than the broad market in crisis phases
- it should move defensive early enough to matter
- it should avoid excessive churn while under stress
- it should still participate sufficiently in the rebound phase

## Step 4: Proposal Conclusion

Date:
- 2026-05-08

Conclusion:
- The preferred next step is crisis-window validation first, followed by narrow defensive tuning only if the current strategy fails the crisis gates.
- Broad optimization directly against a handful of crisis windows would create unnecessary overfitting risk.
