# Aegis Paper Account Log Analysis - 2026-04-28

## Step 1: Intake

Summary:
- User added a new `PaperAccountLogs` folder under `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/`.
- The folder currently contains:
  - a live algorithm log text export
  - a live orders CSV export

Files observed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/algorithm-log_L-1eb64e3666b1ba1be3cf7de36a45be29.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/live_orders_1763287545_1777375633_L-1eb64e3666b1ba1be3cf7de36a45be29.csv`

Initial finding:
- The exported files are not perfectly single-run scoped:
  - the CSV contains rows from more than one deployment ID
  - the text log begins with an older non-Aegis deployment before the later Aegis live run content

Working assumption for normalization:
- Rename each file using the first relevant Aegis deployment date found in the file contents, not the download timestamp and not the long deployment hash.

Next step:
- Isolate the Aegis-specific content, rename both files using date-based names, and analyze the paper-account behavior.

## Step 2: Normalize Filenames

Summary:
- Renamed both files to date-based names tied to the relevant Aegis content instead of the long deployment hash.

Renames:
- `algorithm-log_L-1eb64e3666b1ba1be3cf7de36a45be29.txt`
  - renamed to `2026-04-24_072147__AegisGrowthAllocation__paper-live-log.txt`
- `live_orders_1763287545_1777375633_L-1eb64e3666b1ba1be3cf7de36a45be29.csv`
  - renamed to `2026-04-27_140000__AegisGrowthAllocation__paper-orders.csv`

Rationale:
- The live log file name is based on the current Interactive Brokers Aegis deployment start captured in the file (`2026-04-24 07:21:47`).
- The orders CSV name is based on the first Aegis order timestamp in the file (`2026-04-27T14:00:00Z`).

## Step 3: Analysis

Artifacts analyzed:
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-04-24_072147__AegisGrowthAllocation__paper-live-log.txt`
- `Algorithm.CSharp/MyAlgorithms/AegisGrowthAllocation/PaperAccountLogs/2026-04-27_140000__AegisGrowthAllocation__paper-orders.csv`

Important context:
- The text log still contains older non-Aegis and earlier Aegis-paper content above the current Interactive Brokers section.
- The CSV still contains three older non-Aegis paper-trading rows plus five Aegis rows for deployment `L-1eb64e3666b1ba1be3cf7de36a45be29`.
- The analysis below is scoped to the current Aegis Interactive Brokers paper deployment only.

Findings:
- Aegis startup/restart behavior:
  - restored prior persisted state with `Holdings=1`
  - detected broker/store mismatch when IB reported `Holdings=0`
  - correctly resolved to `Broker state wins`
  - saved corrected zero-holdings startup state
- Monday weekly review behavior:
  - weekly review did run on `2026-04-27`
  - weekly summary was logged successfully
  - the strategy moved from cash into:
    - growth: `AVGO`, `GOOGL`
    - defensive: `JNJ`, `SCHD`, `SGOV`
- Order behavior:
  - `5` Aegis orders in the CSV
  - all `5` orders ended `Filled`
  - `2` names (`AVGO`, `SCHD`) experienced partial fills before completing
  - total notional value across the five Aegis orders: `$140,096.71`
- Live-state persistence behavior:
  - state was saved on every meaningful submitted / partial / filled order event
  - `OpenOrders` count moved as expected during the partial-fill sequence
  - final weekly-review save shows `Holdings=5 OpenOrders=0`
- Remaining operational warning:
  - the Interactive Brokers deployment still logs:
    - `Warning: usa Index TradeBar data not supported. Please consider reviewing the data providers selection.`
  - this remains the live `VIX` / stress-input concern already identified earlier

Interpretation:
- The paper-account run is operationally encouraging.
- Startup reconciliation, partial-fill handling, and post-order state persistence all behaved the way the live-hardening work intended.
- The strongest remaining concern is still the unsupported live index data warning, not the order-state logic.

Review:
- No issues found in the rename choices after isolating the Aegis-specific timestamps.
- No new operational bug was exposed in the current paper-account run beyond the already-known live index data warning.
