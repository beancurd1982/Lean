# Aegis Pre-Weak Guard Backtest Analysis - 2026-05-09

## Step 1: Intake And File Inventory

Date:
- 2026-05-09

Request:
- Analyze newly uploaded overview JSON, logs, and orders CSV files in `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs`.

Initial inventory:
- Existing earlier numbered result sets: `1.json` through `5.json`, plus matching `_logs.txt` and `_orders.csv`.
- Newer numbered result sets by upload time: `6.json` through `10.json`, plus matching `_logs.txt` and `_orders.csv`.

Working assumption:
- Result sets `6` through `10` are Experiment A pre-Weak guard runs.
- Result sets `1` through `5` are the immediately prior comparison set in the same five-window order.

Open validation:
- Inspect JSON/log contents to confirm metrics and detect whether parameter flags appear in logs.

## Step 2: JSON Parameter And Overview Metrics

Date:
- 2026-05-09

Validated parameter mapping:
- `1` to `5`: `weak-stress-overlay-enabled=true`, `crisis-diagnostics=true`, no explicit `pre-weak-guard-enabled`.
- `6` to `10`: `pre-weak-guard-enabled=true`, `weak-stress-overlay-enabled=false`, `crisis-diagnostics=true`.

Window mapping:
- `1` and `6`: `2007-10-01` to `2008-12-31`
- `2` and `7`: `2009-01-01` to `2009-12-31`
- `3` and `8`: `2010-01-01` to `2010-12-31`
- `4` and `9`: `2019-07-01` to `2020-12-31`
- `5` and `10`: `2021-01-01` to `2022-12-31`

Overview comparison, prior overlay runs versus new pre-Weak guard runs:

| Window | Overlay Run | Overlay Net | Overlay DD | Pre-Weak Run | Pre-Weak Net | Pre-Weak DD | Read |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 2007-10-01 to 2008-12-31 | 1 | `-11.731%` | `15.400%` | 6 | `-16.857%` | `20.300%` | Worse defense |
| 2009-01-01 to 2009-12-31 | 2 | `13.556%` | `2.800%` | 7 | `17.087%` | `4.300%` | Better return, worse DD |
| 2010-01-01 to 2010-12-31 | 3 | `8.082%` | `10.700%` | 8 | `10.729%` | `10.200%` | Better |
| 2019-07-01 to 2020-12-31 | 4 | `37.717%` | `10.900%` | 9 | `46.848%` | `13.500%` | Better return, worse DD |
| 2021-01-01 to 2022-12-31 | 5 | `14.204%` | `15.900%` | 10 | `18.003%` | `13.800%` | Better |

Key overview takeaways:
- The pre-Weak guard materially improves return preservation versus the weak-stress overlay in `2009`, `2010`, `2019-2020`, and `2021-2022`.
- The pre-Weak guard is not enough for `2007-2008`; this run regresses badly versus the weak-stress overlay.
- `2021-2022` is the strongest positive result: higher return and lower drawdown.
- `2019-2020` return improves sharply, but drawdown worsens versus the overlay, so this is not a pure defensive win.

## Step 3: Diagnostic Log Analysis

Date:
- 2026-05-09

Important parser note:
- `Target=G0.2400/D0.3000/C0.4600` is not a reliable guard-active marker by itself.
- The normal portfolio builder can produce the same effective target when selected growth holdings are capped or fewer than the target count.
- The current logs do not explicitly print a `PreWeakGuardActive` flag.

Reconstructed guard eligibility using the current local rule:
- active regime is not Weak,
- drawdown from diagnostic high-water mark is at least `5%`,
- and at least one deteriorating signal exists: trend not Favorable, breadth not Favorable, or stress Weak.

| Run | Diagnostic Range | Diagnostic Return | Diagnostic Max DD | First Weak | Reconstructed Eligible Weeks |
| ---: | --- | ---: | ---: | --- | ---: |
| 6 | `2007-10-01` to `2008-12-29` | `-16.79%` | `-18.93%` | `2008-01-28` | `9` |
| 7 | `2009-01-05` to `2009-12-28` | `18.20%` | `-3.66%` | `2009-01-05` | `0` |
| 8 | `2010-01-04` to `2010-12-27` | `10.71%` | `-9.11%` | `2010-05-10` | `9` |
| 9 | `2019-07-01` to `2020-12-28` | `44.37%` | `-10.91%` | `2020-03-02` | `2` |
| 10 | `2021-01-04` to `2022-12-27` | `18.15%` | `-13.26%` | `2022-01-31` | `16` |

Diagnostic takeaways:
- `2008`: the guard is eligible before and around the first Weak transition, but the drawdown remains severe. Partial de-risking alone did not solve the 2008 crash path.
- `2009`: no reconstructed pre-Weak guard weeks. The run starts Weak and later recovers; this is useful because it shows the guard does not interfere with recovery in this isolated window.
- `2010`: eligible weeks cluster around June to September. This looks constructive: better diagnostic return and lower diagnostic drawdown than the prior baseline/overlay evidence.
- `2020`: only two reconstructed eligible weeks. The guard is not driving COVID-crash protection; return improves mainly because the harsher weak-stress overlay is disabled.
- `2022`: eligible weeks begin on `2022-01-18`, before first Weak on `2022-01-31`, and recur later in the year. This is exactly the intended use case, and the result improves both return and drawdown.

## Step 4: Orders CSV Analysis

Date:
- 2026-05-09

Order counts:

| Window | Overlay Orders | Pre-Weak Orders | Difference |
| --- | ---: | ---: | ---: |
| 2007-2008 | `119` | `144` | `+25` |
| 2009 | `75` | `89` | `+14` |
| 2010 | `179` | `190` | `+11` |
| 2019-2020 | `185` | `204` | `+19` |
| 2021-2022 | `290` | `304` | `+14` |

Order takeaways:
- The pre-Weak run increases order count in every window.
- The increase is moderate, not explosive, but it does add turnover and fees.
- The largest order-count increase is in `2007-2008`, where the performance result is worse, so that trade activity did not buy useful protection.

## Step 5: Interim Conclusion

Date:
- 2026-05-09

Conclusion:
- Experiment A is better than the weak-stress overlay for return preservation.
- Experiment A is useful for the `2022` first-leg problem and looks acceptable in `2010`.
- Experiment A fails the `2008` defensive requirement if used alone.
- Experiment A should not replace the weak-stress overlay as a complete defense.

Recommendation:
- Keep the pre-Weak guard as a promising component.
- Do not promote it to live/default behavior.
- Next experiment should combine a lighter pre-Weak guard with a conditional severe-crash override that only activates when stress becomes truly severe, instead of keeping the blunt `G0/D0.20/C0.80` overlay active for all Weak periods.

Open risk:
- Because the logs do not explicitly print guard activation, future diagnostic code should add a boolean flag such as `PreWeakGuard=True/False` and `SleeveOverride=none/pre-weak/weak-stress`.

## Step 6: Multi-Agent Review Setup

Date:
- 2026-05-09

Request:
- Spawn three subagents to review the previous five result sets and the latest five result sets, then debate until a final proposal emerges.

Agent setup:
- Agent 1: Crisis Defense Reviewer. Focus on 2007-2008 and 2021-2022 drawdown/crash defense.
- Agent 2: Return Preservation Reviewer. Focus on 2009, 2010, and 2019-2020 recovery participation and annual return preservation.
- Agent 3: Execution And Diagnostics Reviewer. Focus on order activity, turnover, log evidence quality, and whether the data can support the next code change.

Files in scope:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/1.json` through `10.json`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/1_logs.txt` through `10_logs.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/CrisisBackTestLogs/1_orders.csv` through `10_orders.csv`
- `project-notes/Aegis_PreWeak_Guard_Backtest_Analysis_2026-05-09.md`

Coordination rule:
- Agents are read-only.
- Coordinator owns synthesis and project-note updates.

## Step 7: First-Round Agent Positions

Date:
- 2026-05-09

Agent 1, Crisis Defense Reviewer:
- Run `6` is not defensive enough: `2007-2008` net worsened from `-11.731%` to `-16.857%`, drawdown worsened from `15.4%` to `20.3%`.
- The failure is likely not the pre-Weak trim itself, but disabling the harsher Weak/crash overlay during severe 2008 stress.
- Run `10` is a real `2021-2022` improvement: net improved from `14.204%` to `18.003%`, drawdown improved from about `15.94%` to about `13.75%`.
- Run `9` worsened COVID drawdown despite better final return, so fast-crash defense remains weak.
- Recommendation: keep pre-Weak guard as a component, but test a hard severe-crash override for `Act=Weak && Severe=True` or all trend/breadth/stress Weak.

Agent 2, Return Preservation Reviewer:
- Excluding `2007-2008`, the latest runs add about `+19.108` percentage points of net profit across the other four windows.
- `2009`, `2010`, `2019-2020`, and `2021-2022` support the softer/latest behavior for return preservation.
- The old broad weak-stress overlay should not be restored globally because it suppresses recovery, especially `2009` and post-March `2020`.
- Recommendation: keep the latest softer/pre-Weak behavior as the return-preserving base, then test only a narrow severe-crash override that releases quickly.

Agent 3, Execution And Diagnostics Reviewer:
- Pre-Weak runs increased order count, fees, gross notional, and turnover in every window.
- Guard activation is not directly provable from current logs because no explicit `PreWeakGuardActive`, `SleeveOverride`, or reason field exists.
- `Target=G0.24/D0.30/C0.46` is ambiguous and cannot be treated as proof of guard activation.
- Recommendation: safest next change is diagnostic-only, then run a 2x2 ablation of overlay off/on and pre-Weak guard off/on before tuning behavior.

First-round consensus:
- Do not promote the current pre-Weak guard alone.
- Do not restore the old broad weak-stress overlay globally.
- Need either explicit attribution diagnostics, a narrow severe-crash override, or both.

First-round disagreement:
- Whether the next step should be diagnostic-only first, or diagnostic plus narrow behavior change in the same experiment.

## Step 8: Agent Debate And Final Convergence

Date:
- 2026-05-09

Debate round:
- Agent 1 argued the next behavior test must address severe 2008 and fast-crash exposure, but conceded the broad weak-stress overlay should not be restored globally.
- Agent 2 argued that `Act=Weak && Severe=True` is too broad because it appears too often during recovery windows, and conceded explicit diagnostics are required.
- Agent 3 moved from diagnostic-only to diagnostic plus one experiment-gated behavior path, but required explicit attribution fields before interpreting the next results.

Coordinator signal-frequency check:
- In runs `6` to `10`, `Act=Weak && Severe=True` is too broad:
- `2009` has `24` such weeks.
- `2020` has `18` such weeks.
- `Act=Weak && Severe=True && all three Trend/Breadth/Stress Weak` catches all `15` severe `2008` weeks, but misses `2020` entirely and catches only `5` weeks in `2022`.

Final consensus:
- Add diagnostics and one experiment-gated severe-crash override in the same bounded change.
- Do not make the new behavior default.
- Do not enable it in live mode.
- Do not restore the old broad weak-stress overlay globally.

Final proposed behavior test:
- New parameter: `severe-crash-override-enabled`, default `false`, parsed only in non-live mode.
- Trigger:
- `ActiveRegime == Weak`
- `Severe == true`
- weekly drawdown from high-water mark is at least `10%`
- at least two of `Trend`, `BreadthState`, and `Stress` are `Weak`
- Target while active: Growth `0.00`, Defensive `0.20`, Cash `0.80`.
- Optional second variant only after the first test: all-cash `G0/D0/C1` when weekly drawdown is at least `12%` and all three signals are Weak.

Release rule:
- Release the severe override when the trigger is false.
- Do not jump straight to higher growth exposure from severe override; fall back to the normal softer Weak/pre-Weak logic for at least one rebalance.

Required diagnostics:
- `PreWeakGuardActive=True/False`
- `SevereCrashOverrideActive=True/False`
- `SleeveOverride=none/pre-weak/severe-crash`
- `OverrideReason`
- `DrawdownFromHigh`
- `BaseTarget`
- `FinalTarget`
- release reason: `trigger-cleared`, `regime-upgrade`, or `still-active`
- order or rebalance reason tags if practical

Backtest plan:
- Run the same five windows with diagnostics enabled.
- Matrix:
- pre-Weak guard off, severe-crash override off
- pre-Weak guard on, severe-crash override off
- pre-Weak guard off, severe-crash override on
- pre-Weak guard on, severe-crash override on

Pass/fail gates:
- `2007-2008`: combined run must materially improve versus run `6`; target drawdown closer to run `1`, with hard fail if drawdown remains near `20.3%`.
- `2021-2022`: preserve most of run `10`; drawdown should stay no worse than about `14.5%`, net profit should not fall below about `17%`.
- `2009`: net profit degradation versus run `7` should be no more than about `1.0` percentage point.
- `2010`: net profit should stay near run `8`, with any return sacrifice justified by clear drawdown improvement.
- `2019-2020`: drawdown should improve versus run `9` without collapsing the March-to-year-end recovery behavior toward run `4`.
- Execution: order count and fees should not rise more than about `10%` to `15%` versus the comparable pre-Weak run unless drawdown improvement is material.

Final proposal:
- The next implementation should be a diagnostic-plus-experiment change, not a live/default promotion.
- The main hypothesis is that a narrow severe-crash override can recover 2008/fast-crash defense while preserving the return benefits seen in runs `7` to `10`.

## Step 9: Commit Scope

Date:
- 2026-05-09

Commit request:
- User requested committing and pushing local changes.

Included scope:
- Experiment A pre-Weak guard implementation and tests.
- Experiment A implementation note.
- Latest pre-Weak backtest result artifacts: `6` to `10` JSON, logs, and orders CSV files.
- Pre-Weak backtest analysis and multi-agent debate note.

Verification before commit:
- Re-run focused Aegis tests.
- Re-run algorithm build.
- Run `git diff --check`.

## Step 10: Commit Verification

Date:
- 2026-05-09

Commands:
- `git -c safe.directory=D:/Projects/Git/Lean-1 diff --check`
- `dotnet test Tests/QuantConnect.Tests.csproj --no-restore --filter "FullyQualifiedName~QuantConnect.Tests.Algorithm.AegisGrowthAllocationTests" -nologo -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`
- `dotnet build Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj -c Debug -nologo -p:NuGetAudit=false -p:TreatWarningsAsErrors=false -p:WarningsAsErrors=''`

Results:
- `git diff --check` passed with no whitespace errors.
- Focused Aegis tests passed: `22` passed, `0` failed, `0` skipped.
- Algorithm CSharp build passed: `0` warnings, `0` errors.
