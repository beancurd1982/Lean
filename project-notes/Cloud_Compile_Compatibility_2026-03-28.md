# Cloud Compile Compatibility 2026-03-28

## Scope
- Map the QuantConnect cloud package version `QuantConnect.Common 2.5.16913` to the closest LEAN tag/commit visible in the local repository.
- Update `Algorithm.CSharp/MyAlgorithms/MultiStockV33_Stable_Base.cs` to remove known cloud compile incompatibilities.
- Verify the updated file still builds in the local repository.

## Progress Log
- 2026-03-28: Started compatibility investigation and remediation for `MultiStockV33_Stable_Base.cs` after confirming the cloud `OrderTicket` API does not include `QuantityRemaining`.

## Review Log
- 2026-03-28: Initial scope review completed. No issues found. This task is limited to cloud-compatibility investigation and targeted source updates.
- 2026-03-28: Mapped cloud package `QuantConnect.Common 2.5.16913` to local LEAN tag `16913`, commit `40e887a106489c3af6671d18d36ac7fdc4b0e4db` (`Unseal greeks ComputeIndicator method (#8570)`).
- 2026-03-28: Updated `MultiStockV33_Stable_Base.cs` for cloud compatibility by replacing `OrderTicket.QuantityRemaining` with `ticket.Quantity - ticket.QuantityFilled`.
- 2026-03-28: Reverted the attempted `SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(symbol).SetDataNormalizationMode(...)` replacement after local verification showed this LEAN codebase does not expose the expected extension method on the returned config list. The original `Security.SetDataNormalizationMode(...)` call was kept because it still compiles locally and the cloud issue there is an obsolete warning, not a compile blocker.

## Verification
- 2026-03-28: Verified `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Release -nologo` succeeded after the compatibility update. Existing repo-wide warnings remain unchanged.
- 2026-03-28: Strict review completed. No new issues found. `QuantityRemaining` usage was removed from `MultiStockV33_Stable_Base.cs`, and the original `Security.SetDataNormalizationMode(...)` call was retained because it is only an obsolete warning in cloud, not a compile blocker.
