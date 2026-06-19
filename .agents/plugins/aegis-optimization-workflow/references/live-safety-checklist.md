# Aegis Live Safety Checklist

Use this before paper/live promotion of an AegisGrowthAllocation candidate.

## Deployment Identity

- Confirm `StrategyConfig.AlgorithmVersion`.
- Confirm source revision or tag.
- Confirm cloud parameters match the intended candidate.

## State And Persistence

- Confirm Object Store live-state key.
- Confirm live-state schema version.
- Confirm restart recovery path can load or safely initialize state.
- Confirm no incompatible schema change is introduced.

## Orders And Holdings

- Confirm no unexpected open orders.
- Confirm recent order events are consistent with the intended rebalance.
- Confirm holdings count, weights, and equity reconcile with logs.
- Confirm execution model matches validation artifacts.

## Candidate Evidence

- Confirm same-execution stress gate is passed.
- Confirm long-window improvement is meaningful.
- Confirm turnover, fees, and order count remain live-suitable.
- Confirm unresolved risks are recorded before promotion.

## Blocking Conditions

Block promotion when:

- stress validation is incomplete or worse than the accepted gate;
- live-state or Object Store behavior changed without explicit approval;
- order handling changed without explicit approval;
- deployment identity is ambiguous;
- paper-live logs show unresolved state, order, or holdings anomalies.
