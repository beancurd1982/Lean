# Aegis Paper Account Log Analysis - 2026-05-08

## Step 1: Intake

Date:
- 2026-05-08

Summary:
- User reported a new live log `.txt` file and a new paper account orders `.csv` file added to `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/`.
- Existing normalized April 2026 paper-account artifacts are still present alongside the new raw-named downloads.

Files observed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/algorithm-log_L-1eb64e3666b1ba1be3cf7de36a45be29.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/live_orders_1763287545_1778238535_L-1eb64e3666b1ba1be3cf7de36a45be29.csv`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-04-24_072147__AegisGrowthAllocation__paper-live-log.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-04-27_140000__AegisGrowthAllocation__paper-orders.csv`

Files touched:
- `project-notes/Aegis_PaperAccount_Log_Analysis_2026-05-08.md`

## Step 6: Rename Execution

Date:
- 2026-05-08

Summary:
- Applied the approved collision-safe normalization rule to the two new raw-named paper-account exports.

Renames:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/algorithm-log_L-1eb64e3666b1ba1be3cf7de36a45be29.txt`
  - renamed to `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-05-04_100000__AegisGrowthAllocation__paper-live-log.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/live_orders_1763287545_1778238535_L-1eb64e3666b1ba1be3cf7de36a45be29.csv`
  - renamed to `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-05-04_140000__AegisGrowthAllocation__paper-orders.csv`

Rationale:
- The new filenames reflect the first newly captured Aegis activity in the extended exports.
- This avoids collisions with the earlier normalized files while preserving chronological meaning.

Files touched:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-05-04_100000__AegisGrowthAllocation__paper-live-log.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-05-04_140000__AegisGrowthAllocation__paper-orders.csv`
- `project-notes/Aegis_PaperAccount_Log_Analysis_2026-05-08.md`

## Step 7: Post-Rename Review

Date:
- 2026-05-08

Review scope:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-05-04_100000__AegisGrowthAllocation__paper-live-log.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-05-04_140000__AegisGrowthAllocation__paper-orders.csv`
- `project-notes/Aegis_PaperAccount_Log_Analysis_2026-05-08.md`

Review checks:
- Verified the `PaperAccountLogs` folder now contains both prior normalized April files and the new normalized May 4 files without name collisions.
- Verified the renamed files retain the same content that was analyzed earlier in this note.
- Verified the earlier comparison result still stands:
  - the May 4 orders file extends the April 27 normalized orders file by six Aegis rebalance rows
  - the May 4 log file extends the April 24 normalized log file with the same May 4 rebalance events and state-save transitions already documented above

Review result:
- No issues found with the rename execution or with the recorded naming rule.
- No change to the previously documented operational findings.

## Step 8: User Summary

Date:
- 2026-05-08

Summary request:
- User asked for a concise summary of what the paper-account log and orders CSV show and for an overall assessment of the algorithm's condition.

Summary prepared:
- The April 24 to May 4 paper-account artifacts show the same Aegis Interactive Brokers paper deployment progressing through startup reconciliation, an initial five-position entry, and a later six-order rebalance.
- The operational behavior remained orderly:
  - startup broker/store mismatch was handled safely
  - partial fills were handled correctly
  - state persistence continued after meaningful order events
  - all captured Aegis orders ended filled
- The main remaining concern is still the previously known unsupported live index data warning rather than an order-lifecycle or persistence failure.

## Step 9: Publish Preparation

Date:
- 2026-05-08

Summary:
- User requested that the local changes be committed and pushed.
- Current worktree scope was checked before staging.

Files selected for publish:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-05-04_100000__AegisGrowthAllocation__paper-live-log.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-05-04_140000__AegisGrowthAllocation__paper-orders.csv`
- `project-notes/Aegis_PaperAccount_Log_Analysis_2026-05-08.md`

Scope review:
- No unrelated modified or staged files were present in the worktree at publish time.
- This publish is limited to the new normalized May 4 paper-account artifacts and their analysis note.

Initial plan:
- Inspect the new raw-named files.
- Compare them against the prior normalized April 2026 artifacts to determine what is newly captured.
- Record findings, open questions, and review results in this note before concluding the task.

## Step 2: Delta Analysis

Date:
- 2026-05-08

Artifacts analyzed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/algorithm-log_L-1eb64e3666b1ba1be3cf7de36a45be29.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/live_orders_1763287545_1778238535_L-1eb64e3666b1ba1be3cf7de36a45be29.csv`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-04-24_072147__AegisGrowthAllocation__paper-live-log.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-04-27_140000__AegisGrowthAllocation__paper-orders.csv`

Files touched:
- `project-notes/Aegis_PaperAccount_Log_Analysis_2026-05-08.md`

Comparison result:
- The new raw-named exports are not a separate deployment.
- They extend the same Interactive Brokers paper deployment `L-1eb64e3666b1ba1be3cf7de36a45be29` previously analyzed on 2026-04-28.
- Relative to the normalized April 28 orders CSV, the new CSV adds `6` Aegis rows.
- The added rows correspond to the next weekly review and rebalance on `2026-05-04` at roughly `14:00 UTC` (`10:00 AM` in the log's local timestamping).

New orders captured:
- `JNJ -114` filled at `$227.00`
- `AAPL +107` filled at about `$275.37`
- `AVGO -4` filled at `$420.85`
- `COST +29` filled at `$1,014.00`
- `GOOGL -12` filled at `$382.64`
- `VIG +115` filled at `$228.10`

New behavior observed:
- The strategy did not perform a full liquidation/re-entry cycle.
- It executed a targeted rebalance:
  - fully exited `JNJ`
  - trimmed `AVGO`
  - trimmed `GOOGL`
  - added `AAPL`, `COST`, and `VIG`
- The log shows `AAPL` had a partial fill before completing.
- All six newly captured orders ended `Filled`.
- State persistence continued to fire on every submitted / partial / filled transition that appeared in the log.
- Post-rebalance state finished at `Holdings=7 OpenOrders=0`.

Weekly review summary line:
- `2026-05-04 [AEGIS] Prev=Neutral Act=Neutral Raw=Neutral Trend=Favorable Breadth=Neutral Stress=Neutral Severe=False Curr=G0.25/D0.30/C0.46 Target=G0.45/D0.30/C0.25 Growth[4]=AAPL,AVGO,COST+1 Def[3]=SCHD,SGOV,VIG Forced=0 Trim=False Reserve=0`

Interpretation:
- The paper deployment progressed from the initial five-position allocation into a larger seven-holding mix while staying within the same neutral regime.
- The change appears consistent with the strategy upgrading growth exposure from `0.24` toward `0.45` and reducing cash from `0.46` toward `0.25`.
- Operationally, the order lifecycle and live-state persistence still look healthy in this later rebalance window.

Warnings and risks:
- The known live warning remains present:
  - `Warning: usa Index TradeBar data not supported. Please consider reviewing the data providers selection.`
- No new exceptions or order-state anomalies were present in the added May 4 segment.
- The text log export still contains older non-Aegis and older Aegis-paper sections above the current Interactive Brokers run, so future comparisons should stay deployment-scoped.
- The raw-named May exports were analyzed in place and were not normalized or deduplicated during this task.

Open questions:
- The compact summary token `Growth[4]=AAPL,AVGO,COST+1` suggests one additional growth holding beyond the three explicitly named symbols; from the orders it is most likely the residual `GOOGL` position, but the summary line itself does not spell that out directly.

## Step 3: Review

Date:
- 2026-05-08

Review scope:
- `project-notes/Aegis_PaperAccount_Log_Analysis_2026-05-08.md`

Review checks:
- Verified the new CSV contains `11` Aegis rows total versus `5` in the prior normalized CSV.
- Verified the added delta is exactly the six `2026-05-04` rebalance rows listed above.
- Verified the new text log contains the prior April 28 content plus the added May 4 order and state-save events.
- Checked the added May 4 log segment for new `Warning`, `Error`, or `Exception` text and found none beyond the previously known index data warning.

Review result:
- No issues found in the analysis note.
- No new operational bug was exposed by the newly downloaded paper-account artifacts.

## Step 4: Rename Planning

Date:
- 2026-05-08

Summary:
- User requested that the new raw-named paper-account files be renamed using the prior normalization pattern and then compared against the earlier normalized artifacts.
- Applying the earlier normalization rule literally would create filename collisions with the existing normalized files already in `PaperAccountLogs`.

Collision detail:
- `algorithm-log_L-1eb64e3666b1ba1be3cf7de36a45be29.txt`
  - prior rule would map to `2026-04-24_072147__AegisGrowthAllocation__paper-live-log.txt`
  - that file already exists
- `live_orders_1763287545_1778238535_L-1eb64e3666b1ba1be3cf7de36a45be29.csv`
  - prior rule would map to `2026-04-27_140000__AegisGrowthAllocation__paper-orders.csv`
  - that file already exists

Open question:
- Need user direction on the desired collision-safe naming convention before performing the rename.

## Step 5: Rename Decision

Date:
- 2026-05-08

Summary:
- User selected naming rule `#2`.
- The new raw-named files should be normalized from the new content start date rather than from the original deployment start.

Approved naming rule:
- For extended paper-account exports that would otherwise collide with an existing normalized file, name the new file from the first newly captured Aegis event in that export.
- For the current intake this means:
  - use `2026-05-04_100000__AegisGrowthAllocation__paper-live-log.txt` for the text log
  - use `2026-05-04_140000__AegisGrowthAllocation__paper-orders.csv` for the orders CSV

Files touched:
- `project-notes/Aegis_PaperAccount_Log_Analysis_2026-05-08.md`
