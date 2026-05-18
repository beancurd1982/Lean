#region imports
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    public class AegisGrowthAllocation : QCAlgorithm
    {
        private Symbol _marketSymbol;
        private Symbol _stressSymbol;
        private MarketState _marketState;
        private RollingWindow<decimal> _stressWindow;
        private readonly Dictionary<Symbol, AssetState> _assetStates = new Dictionary<Symbol, AssetState>();
        private RegimeModel _regimeModel;
        private StockSelectionModel _stockSelectionModel;
        private PortfolioManager _portfolioManager;
        private LiveStateStore _liveStateStore;
        private decimal _undeployedCapitalReserve;
        private bool _startupReconciliationComplete;
        private DateTime? _lastCompletedWeeklyReviewUtc;
        private Dictionary<Symbol, decimal> _lastPlannedTargetWeights = new Dictionary<Symbol, decimal>();
        private Dictionary<int, AegisOpenOrderState> _trackedOpenOrders = new Dictionary<int, AegisOpenOrderState>();
        private AegisLiveState _loadedLiveState;
        private bool _crisisDiagnosticsEnabled;
        private bool _weakStressOverlayEnabled;
        private bool _preWeakGuardEnabled = StrategyConfig.DefaultPreWeakGuardEnabled;
        private bool _severeCrashOverrideEnabled;
        private decimal _preWeakGuardDrawdownThreshold = StrategyConfig.DefaultPreWeakGuardDrawdownThreshold;
        private decimal _severeCrashOverrideDrawdownThreshold = StrategyConfig.DefaultSevereCrashOverrideDrawdownThreshold;
        private decimal _severeCrashOverrideExitDrawdownThreshold = StrategyConfig.DefaultSevereCrashOverrideExitDrawdownThreshold;
        private int _severeCrashOverrideRecoveryConfirmationWeeks = StrategyConfig.DefaultSevereCrashOverrideRecoveryConfirmationWeeks;
        private bool _severeCrashModeActive;
        private int _severeCrashRecoveryWeeks;
        private string _severeCrashModeState = "none";
        private string _severeCrashExitReason = "none";
        private decimal _defensiveOverrideEquityHighWaterMark;
        private static readonly TimeSpan UsaRegularMarketOpenTime = new TimeSpan(9, 30, 0);

        public override void Initialize()
        {
            SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);

            if (!LiveMode)
            {
                ConfigureBacktestDates();
                SetCash(30000);
                _crisisDiagnosticsEnabled = ParseBooleanParameter(StrategyConfig.CrisisDiagnosticsParameter, false);
                _weakStressOverlayEnabled = ParseBooleanParameter(StrategyConfig.WeakStressOverlayParameter, false);
                _preWeakGuardEnabled = ParseBooleanParameter(
                    StrategyConfig.PreWeakGuardParameter,
                    StrategyConfig.DefaultPreWeakGuardEnabled);
                _severeCrashOverrideEnabled = ParseBooleanParameter(StrategyConfig.SevereCrashOverrideParameter, false);
                _preWeakGuardDrawdownThreshold = ParseDecimalParameter(
                    StrategyConfig.PreWeakGuardDrawdownThresholdParameter,
                    StrategyConfig.DefaultPreWeakGuardDrawdownThreshold,
                    value => value > 0m && value < 1m);
                _severeCrashOverrideDrawdownThreshold = ParseDecimalParameter(
                    StrategyConfig.SevereCrashOverrideDrawdownThresholdParameter,
                    StrategyConfig.DefaultSevereCrashOverrideDrawdownThreshold,
                    value => value > 0m && value < 1m);
                _severeCrashOverrideExitDrawdownThreshold = ParseDecimalParameter(
                    StrategyConfig.SevereCrashOverrideExitDrawdownThresholdParameter,
                    StrategyConfig.DefaultSevereCrashOverrideExitDrawdownThreshold,
                    value => value > 0m && value < _severeCrashOverrideDrawdownThreshold);
                _severeCrashOverrideRecoveryConfirmationWeeks = ParseIntParameter(
                    StrategyConfig.SevereCrashOverrideRecoveryConfirmationWeeksParameter,
                    StrategyConfig.DefaultSevereCrashOverrideRecoveryConfirmationWeeks,
                    value => value >= 1 && value <= 8);
            }

            _marketSymbol = AddEquity(StrategyConfig.MarketTicker, Resolution.Daily).Symbol;
            Securities[_marketSymbol].SetDataNormalizationMode(DataNormalizationMode.Adjusted);
            _stressSymbol = AddIndex(StrategyConfig.StressTicker, Resolution.Daily).Symbol;
            _marketState = new MarketState(
                _marketSymbol,
                SMA(_marketSymbol, 200, Resolution.Daily),
                StrategyConfig.CloseWindowSize,
                StrategyConfig.SpySmaLookbackWindowSize);
            _stressWindow = new RollingWindow<decimal>(StrategyConfig.VixAverageWindow);

            foreach (var ticker in StrategyConfig.GrowthTickers.Concat(StrategyConfig.DefensiveTickers).Distinct())
            {
                var security = AddEquity(ticker, Resolution.Daily);
                security.SetDataNormalizationMode(DataNormalizationMode.Adjusted);
                var isGrowth = StrategyConfig.GrowthTickers.Contains(ticker);
                var isDefensive = StrategyConfig.DefensiveTickers.Contains(ticker);

                _assetStates[security.Symbol] = new AssetState(
                    security.Symbol,
                    ticker,
                    isGrowth,
                    isDefensive,
                    ticker == "SGOV",
                    SMA(security.Symbol, 50, Resolution.Daily),
                    SMA(security.Symbol, 200, Resolution.Daily),
                    ATR(security.Symbol, 20, MovingAverageType.Simple, Resolution.Daily),
                    StrategyConfig.CloseWindowSize);
            }

            StrategyConfig.ResetRuntimeParameters();
            ConfigureRuntimeParameters();
            _undeployedCapitalReserve = ParseDecimalParameter(
                StrategyConfig.UndeployedReserveParameter,
                0m,
                value => value >= 0m);
            SetWarmUp(StrategyConfig.WarmupTradingDays, Resolution.Daily);

            _regimeModel = new RegimeModel();
            _stockSelectionModel = new StockSelectionModel();
            _portfolioManager = new PortfolioManager();
            _liveStateStore = new LiveStateStore(this);

            if (LiveMode)
            {
                _loadedLiveState = _liveStateStore.Load();
                RestorePersistedRuntimeState(_loadedLiveState);
                ReconcileLiveStartup();
                SaveLiveState("Startup reconciliation");
            }
            else
            {
                _startupReconciliationComplete = true;
            }

            Schedule.On(
                DateRules.WeekStart(_marketSymbol, extendedMarketHours: false),
                TimeRules.AfterMarketOpen(_marketSymbol, GetWeeklyDecisionMinutesAfterMarketOpen(), extendedMarketOpen: false),
                WeeklyReview);

            Debug(
                $"AegisGrowthAllocation initialized. GrowthUniverse={StrategyConfig.GrowthTickers.Count} DefensiveUniverse={StrategyConfig.DefensiveTickers.Count} UndeployedReserve={_undeployedCapitalReserve.ToString(CultureInfo.InvariantCulture)} FavorableBreadthThreshold={StrategyConfig.FavorableBreadthThreshold.ToString(CultureInfo.InvariantCulture)} WeakStressThreshold={StrategyConfig.WeakStressThreshold.ToString(CultureInfo.InvariantCulture)} UpgradeConfirmationWeeks={StrategyConfig.UpgradeConfirmationWeeks} GrowthAtrEligibilityLimit={StrategyConfig.GrowthAtrEligibilityLimit.ToString(CultureInfo.InvariantCulture)} ReplacementScoreGap={StrategyConfig.ReplacementScoreGap.ToString(CultureInfo.InvariantCulture)} HoldStabilityBonus={StrategyConfig.HoldStabilityBonus.ToString(CultureInfo.InvariantCulture)} ToleranceBandScale={StrategyConfig.RebalanceToleranceBandScale.ToString(CultureInfo.InvariantCulture)}");
        }

        public override void OnData(Slice slice)
        {
            if (slice.Bars.TryGetValue(_marketSymbol, out var marketBar))
            {
                _marketState.Update(marketBar);
            }

            if (slice.Bars.TryGetValue(_stressSymbol, out var stressBar))
            {
                _stressWindow.Add(stressBar.Close);
            }

            foreach (var assetState in _assetStates.Values)
            {
                if (slice.Bars.TryGetValue(assetState.Symbol, out var bar))
                {
                    assetState.Update(bar);
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (!LiveMode || !_assetStates.ContainsKey(orderEvent.Symbol))
            {
                return;
            }

            RefreshTrackedOpenOrdersFromBroker();
            SaveLiveState($"Order event {orderEvent.OrderId} {orderEvent.Status}");
        }

        public override void OnEndOfAlgorithm()
        {
            if (LiveMode)
            {
                SaveLiveState("Algorithm end");
            }
        }

        private void WeeklyReview()
        {
            if (IsWarmingUp)
            {
                return;
            }

            if (LiveMode && !_startupReconciliationComplete)
            {
                Debug($"[AEGIS-LIVE] {Time}: weekly review skipped. Startup reconciliation incomplete.");
                return;
            }

            if (!IsMarketOpen(_marketSymbol))
            {
                Debug($"[AEGIS] {Time} Weekly review skipped. Market closed for {_marketSymbol.Value}.");
                return;
            }

            if (!_marketState.IsReady || _stressWindow.Count < StrategyConfig.VixAverageWindow)
            {
                Debug($"[AEGIS] {Time} Weekly review skipped. MarketReady={_marketState.IsReady} StressCount={_stressWindow.Count}");
                return;
            }

            if (LiveMode)
            {
                RefreshTrackedOpenOrdersFromBroker();
                if (_trackedOpenOrders.Count > 0)
                {
                    Debug($"[AEGIS-LIVE] {Time}: weekly review skipped. OpenOrders={_trackedOpenOrders.Count} Symbols={string.Join(",", _trackedOpenOrders.Values.Select(order => order.Ticker).Distinct().OrderBy(ticker => ticker, StringComparer.Ordinal))}");
                    return;
                }
            }

            var snapshots = _assetStates.Values.Select(CreateSnapshot).ToList();
            var growthSnapshots = snapshots.Where(snapshot => snapshot.IsGrowth).ToList();
            var defensiveSnapshots = snapshots.Where(snapshot => snapshot.IsDefensive).ToList();
            var readyGrowthSnapshots = growthSnapshots.Where(snapshot => snapshot.IsDataReady).ToList();
            if (readyGrowthSnapshots.Count == 0)
            {
                Debug($"[AEGIS] {Time} Weekly review skipped. No ready growth snapshots.");
                return;
            }

            var breadth = readyGrowthSnapshots.Count(snapshot => snapshot.Close > snapshot.Sma200) / (decimal)readyGrowthSnapshots.Count;
            var vixAverage5 = _stressWindow.Average();
            var regimeSnapshot = _regimeModel.Update(
                new RegimeInputs(
                    _marketState.CurrentClose,
                    _marketState.CurrentSma200,
                    _marketState.LookbackSma200,
                    breadth,
                    vixAverage5));

            var currentWeights = GetCurrentWeights();
            var currentGrowthHoldings = currentWeights.Keys
                .Where(symbol => _assetStates.TryGetValue(symbol, out var assetState) && assetState.IsGrowth)
                .ToList();

            var growthSelection = _stockSelectionModel.SelectGrowthCandidates(
                growthSnapshots,
                regimeSnapshot.ActiveRegime,
                currentGrowthHoldings);
            var defensiveSelection = _stockSelectionModel.SelectDefensiveCandidates(
                defensiveSnapshots,
                regimeSnapshot.ActiveRegime);

            var reserveBeforeReview = _undeployedCapitalReserve;
            var totalPortfolioValue = Portfolio.TotalPortfolioValue;
            if (_crisisDiagnosticsEnabled || _preWeakGuardEnabled || _severeCrashOverrideEnabled)
            {
                UpdateDefensiveOverrideHighWaterMark(totalPortfolioValue);
            }
            var drawdownFromHigh = CalculateDrawdownFromHigh(totalPortfolioValue);
            var baseSleeveTargets = StrategyConfig.SleeveTargetsByRegime[regimeSnapshot.ActiveRegime];
            var sleeveTargetsOverride = (SleeveTargets)null;
            var sleeveOverride = "none";
            var overrideReason = "none";
            var severeCrashOverrideActive = UpdateSevereCrashMode(regimeSnapshot, totalPortfolioValue);
            var preWeakGuardActive = false;
            if (severeCrashOverrideActive)
            {
                sleeveTargetsOverride = StrategyConfig.SevereCrashOverrideSleeveTargets;
                sleeveOverride = "severe-crash";
                overrideReason = _severeCrashModeState == "hold"
                    ? "severe-crash-hysteresis-hold"
                    : "weak-severe-dd10-signals2";
            }
            else if (ShouldApplyWeakStressOverlay(regimeSnapshot))
            {
                sleeveTargetsOverride = StrategyConfig.WeakStressOverlaySleeveTargets;
                sleeveOverride = "weak-stress";
                overrideReason = "weak-stress-overlay";
            }
            else if (ShouldApplyPreWeakGuard(regimeSnapshot, totalPortfolioValue))
            {
                sleeveTargetsOverride = StrategyConfig.PreWeakGuardSleeveTargets;
                sleeveOverride = "pre-weak";
                overrideReason = "pre-weak-drawdown-signal";
                preWeakGuardActive = true;
            }

            var finalSleeveTargets = sleeveTargetsOverride ?? baseSleeveTargets;
            var plan = _portfolioManager.BuildPlan(
                regimeSnapshot.PreviousRegime,
                regimeSnapshot.ActiveRegime,
                growthSelection,
                defensiveSelection,
                currentWeights,
                _undeployedCapitalReserve,
                totalPortfolioValue,
                sleeveTargetsOverride);

            ExecutePlan(plan, currentWeights);
            if (plan.ReleasedReserve > 0m)
            {
                _undeployedCapitalReserve = Math.Max(0m, _undeployedCapitalReserve - plan.ReleasedReserve);
            }

            _lastCompletedWeeklyReviewUtc = UtcTime;
            _lastPlannedTargetWeights = new Dictionary<Symbol, decimal>(plan.TargetWeights);
            RefreshTrackedOpenOrdersFromBroker();
            SaveLiveState("Weekly review");

            Debug(FormatWeeklySummary(plan, regimeSnapshot));
            if (_crisisDiagnosticsEnabled)
            {
                Debug(FormatCrisisDiagnostics(
                    plan,
                    regimeSnapshot,
                    currentWeights,
                    breadth,
                    vixAverage5,
                    reserveBeforeReview,
                    preWeakGuardActive,
                    severeCrashOverrideActive,
                    sleeveOverride,
                    overrideReason,
                    drawdownFromHigh,
                    baseSleeveTargets,
                    finalSleeveTargets,
                    _severeCrashModeState,
                    _severeCrashRecoveryWeeks,
                    _severeCrashExitReason));
            }
        }

        private static double GetWeeklyDecisionMinutesAfterMarketOpen()
        {
            var decisionOffset = StrategyConfig.WeeklyDecisionTime - UsaRegularMarketOpenTime;
            return Math.Max(0d, decisionOffset.TotalMinutes);
        }

        private void ConfigureBacktestDates()
        {
            var startDate = ParseBacktestStartDate();
            SetStartDate(startDate);

            var endDate = ParseOptionalBacktestDate(StrategyConfig.BacktestEndParameter);
            if (!endDate.HasValue)
            {
                return;
            }

            if (endDate.Value.Date < startDate.Date)
            {
                Debug(
                    $"[AEGIS] Backtest end date {endDate.Value:yyyy-MM-dd} is before start date {startDate:yyyy-MM-dd}. Ignoring {StrategyConfig.BacktestEndParameter}.");
                return;
            }

            SetEndDate(endDate.Value);
        }

        private void ExecutePlan(PortfolioPlan plan, IReadOnlyDictionary<Symbol, decimal> currentWeights)
        {
            var orderedTargets = plan.TargetWeights
                .OrderBy(pair => pair.Value == 0m ? 0 : 1)
                .ThenBy(pair => pair.Key.Value, StringComparer.Ordinal);

            foreach (var target in orderedTargets)
            {
                var currentWeight = currentWeights.TryGetValue(target.Key, out var value)
                    ? value
                    : 0m;

                if (Math.Abs(target.Value - currentWeight) < StrategyConfig.SmallTradeThreshold)
                {
                    continue;
                }

                if (!IsMarketOpen(target.Key))
                {
                    Debug($"[AEGIS] {Time} Order skipped for {target.Key.Value}. Market is closed.");
                    continue;
                }

                SetHoldings(target.Key, (double)target.Value);
            }
        }

        private Dictionary<Symbol, decimal> GetCurrentWeights()
        {
            var weights = new Dictionary<Symbol, decimal>();
            if (Portfolio.TotalPortfolioValue <= 0m)
            {
                return weights;
            }

            foreach (var assetState in _assetStates.Values)
            {
                var holdingsValue = Portfolio[assetState.Symbol].HoldingsValue;
                if (holdingsValue <= 0m)
                {
                    continue;
                }

                weights[assetState.Symbol] = holdingsValue / Portfolio.TotalPortfolioValue;
            }

            return weights;
        }

        private AssetSnapshot CreateSnapshot(AssetState state)
        {
            return new AssetSnapshot(
                state.Symbol,
                state.Ticker,
                state.IsGrowth,
                state.IsDefensive,
                state.IsSgov,
                state.IsDataReady,
                state.CurrentClose,
                state.Sma50.Current.Value,
                state.Sma200.Current.Value,
                state.Atr20.Current.Value,
                ComputeReturn(state.CloseWindow, StrategyConfig.Return21Period),
                ComputeReturn(state.CloseWindow, StrategyConfig.Return63Period),
                ComputeReturn(state.CloseWindow, StrategyConfig.Return126Period),
                ComputeVolatility(state.CloseWindow, StrategyConfig.VolatilityLookbackDays),
                ComputeDrawdown(state.CloseWindow, StrategyConfig.DrawdownLookbackDays));
        }

        private static decimal ComputeReturn(RollingWindow<decimal> closeWindow, int period)
        {
            if (closeWindow.Count < period)
            {
                return 0m;
            }

            var prior = closeWindow[period - 1];
            return prior > 0m ? (closeWindow[0] / prior) - 1m : 0m;
        }

        private static decimal ComputeVolatility(RollingWindow<decimal> closeWindow, int lookbackDays)
        {
            var sampleCount = Math.Min(closeWindow.Count, lookbackDays);
            if (sampleCount < 3)
            {
                return 0m;
            }

            var closes = new List<decimal>(sampleCount);
            for (var index = sampleCount - 1; index >= 0; index--)
            {
                closes.Add(closeWindow[index]);
            }

            var returns = new List<double>(closes.Count - 1);
            for (var index = 1; index < closes.Count; index++)
            {
                if (closes[index - 1] <= 0m)
                {
                    continue;
                }

                returns.Add((double)((closes[index] / closes[index - 1]) - 1m));
            }

            if (returns.Count < 2)
            {
                return 0m;
            }

            var average = returns.Average();
            var variance = returns.Sum(value => Math.Pow(value - average, 2)) / returns.Count;
            return (decimal)Math.Sqrt(variance);
        }

        private static decimal ComputeDrawdown(RollingWindow<decimal> closeWindow, int lookbackDays)
        {
            var sampleCount = Math.Min(closeWindow.Count, lookbackDays);
            if (sampleCount == 0)
            {
                return 0m;
            }

            decimal peak = 0m;
            decimal maxDrawdown = 0m;

            for (var index = sampleCount - 1; index >= 0; index--)
            {
                var close = closeWindow[index];
                if (close > peak)
                {
                    peak = close;
                }

                if (peak <= 0m)
                {
                    continue;
                }

                var drawdown = (peak - close) / peak;
                if (drawdown > maxDrawdown)
                {
                    maxDrawdown = drawdown;
                }
            }

            return maxDrawdown;
        }

        private void ConfigureRuntimeParameters()
        {
            var favorableBreadthThreshold = ParseDecimalParameter(
                StrategyConfig.FavorableBreadthThresholdParameter,
                StrategyConfig.DefaultFavorableBreadthThreshold,
                value => value > StrategyConfig.WeakBreadthThreshold && value <= 1m);
            var weakStressThreshold = ParseDecimalParameter(
                StrategyConfig.WeakStressThresholdParameter,
                StrategyConfig.DefaultWeakStressThreshold,
                value => value > StrategyConfig.FavorableStressThreshold && value < StrategyConfig.SevereStressThreshold);
            var upgradeConfirmationWeeks = ParseIntParameter(
                StrategyConfig.UpgradeConfirmationWeeksParameter,
                StrategyConfig.DefaultUpgradeConfirmationWeeks,
                value => value >= 1 && value <= 8);
            var growthAtrEligibilityLimit = ParseDecimalParameter(
                StrategyConfig.GrowthAtrEligibilityLimitParameter,
                StrategyConfig.DefaultGrowthAtrEligibilityLimit,
                value => value >= 0.03m && value <= StrategyConfig.GrowthAtrForcedExitLimit);
            var replacementScoreGap = ParseDecimalParameter(
                StrategyConfig.ReplacementScoreGapParameter,
                StrategyConfig.DefaultReplacementScoreGap,
                value => value >= 0m && value <= 20m);
            var holdStabilityBonus = ParseDecimalParameter(
                StrategyConfig.HoldStabilityBonusParameter,
                StrategyConfig.DefaultHoldStabilityBonus,
                value => value >= 0m && value <= 20m);
            var rebalanceToleranceBandScale = ParseDecimalParameter(
                StrategyConfig.RebalanceToleranceBandScaleParameter,
                StrategyConfig.DefaultRebalanceToleranceBandScale,
                value => value > 0m && value <= 2m);

            StrategyConfig.ConfigureRuntimeParameters(
                favorableBreadthThreshold,
                weakStressThreshold,
                upgradeConfirmationWeeks,
                growthAtrEligibilityLimit,
                replacementScoreGap,
                holdStabilityBonus,
                rebalanceToleranceBandScale);
        }

        private DateTime ParseBacktestStartDate()
        {
            var defaultValue = StrategyConfig.DefaultBacktestStartDate;
            var parsed = ParseOptionalBacktestDate(StrategyConfig.BacktestStartParameter);
            if (parsed.HasValue)
            {
                return parsed.Value;
            }

            return defaultValue;
        }

        private DateTime? ParseOptionalBacktestDate(string name)
        {
            var raw = GetParameter(name);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (!TryParseBacktestDate(raw, out var value))
            {
                Debug($"[AEGIS] Invalid backtest date parameter {name}={raw}. Ignoring value.");
                return null;
            }

            return value.Date;
        }

        private static bool TryParseBacktestDate(string raw, out DateTime value)
        {
            return DateTime.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
                out value);
        }

        private decimal ParseDecimalParameter(string name, decimal defaultValue, Func<decimal, bool> validator)
        {
            var raw = GetParameter(name);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return defaultValue;
            }

            if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                Debug($"[AEGIS] Invalid decimal parameter {name}={raw}. Using default {defaultValue.ToString(CultureInfo.InvariantCulture)}.");
                return defaultValue;
            }

            if (!validator(value))
            {
                Debug($"[AEGIS] Out-of-range decimal parameter {name}={value.ToString(CultureInfo.InvariantCulture)}. Using default {defaultValue.ToString(CultureInfo.InvariantCulture)}.");
                return defaultValue;
            }

            return value;
        }

        private int ParseIntParameter(string name, int defaultValue, Func<int, bool> validator)
        {
            var raw = GetParameter(name);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return defaultValue;
            }

            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                Debug($"[AEGIS] Invalid integer parameter {name}={raw}. Using default {defaultValue}.");
                return defaultValue;
            }

            if (!validator(value))
            {
                Debug($"[AEGIS] Out-of-range integer parameter {name}={value}. Using default {defaultValue}.");
                return defaultValue;
            }

            return value;
        }

        private void UpdateDefensiveOverrideHighWaterMark(decimal totalPortfolioValue)
        {
            if (totalPortfolioValue > _defensiveOverrideEquityHighWaterMark)
            {
                _defensiveOverrideEquityHighWaterMark = totalPortfolioValue;
            }
        }

        private decimal CalculateDrawdownFromHigh(decimal totalPortfolioValue)
        {
            if (totalPortfolioValue <= 0m || _defensiveOverrideEquityHighWaterMark <= 0m)
            {
                return 0m;
            }

            return Math.Max(0m, 1m - totalPortfolioValue / _defensiveOverrideEquityHighWaterMark);
        }

        private bool ShouldApplyWeakStressOverlay(RegimeSnapshot regimeSnapshot)
        {
            return _weakStressOverlayEnabled &&
                   regimeSnapshot.ActiveRegime == RiskRegime.Weak &&
                   (regimeSnapshot.SevereStress ||
                    regimeSnapshot.StressState == SignalState.Weak ||
                    (regimeSnapshot.TrendState == SignalState.Weak && regimeSnapshot.BreadthState == SignalState.Weak));
        }

        private bool ShouldApplyPreWeakGuard(RegimeSnapshot regimeSnapshot, decimal totalPortfolioValue)
        {
            if (!_preWeakGuardEnabled ||
                regimeSnapshot.ActiveRegime == RiskRegime.Weak ||
                totalPortfolioValue <= 0m ||
                _defensiveOverrideEquityHighWaterMark <= 0m)
            {
                return false;
            }

            var drawdown = CalculateDrawdownFromHigh(totalPortfolioValue);
            if (drawdown < _preWeakGuardDrawdownThreshold)
            {
                return false;
            }

            return regimeSnapshot.TrendState != SignalState.Favorable ||
                   regimeSnapshot.BreadthState != SignalState.Favorable ||
                   regimeSnapshot.StressState == SignalState.Weak;
        }

        private bool ShouldApplySevereCrashOverride(RegimeSnapshot regimeSnapshot, decimal totalPortfolioValue)
        {
            if (!_severeCrashOverrideEnabled ||
                regimeSnapshot.ActiveRegime != RiskRegime.Weak ||
                !regimeSnapshot.SevereStress ||
                totalPortfolioValue <= 0m ||
                _defensiveOverrideEquityHighWaterMark <= 0m)
            {
                return false;
            }

            if (CalculateDrawdownFromHigh(totalPortfolioValue) < _severeCrashOverrideDrawdownThreshold)
            {
                return false;
            }

            return CountWeakSignals(regimeSnapshot) >= 2;
        }

        private bool UpdateSevereCrashMode(RegimeSnapshot regimeSnapshot, decimal totalPortfolioValue)
        {
            _severeCrashModeState = "none";
            _severeCrashExitReason = "none";

            if (!_severeCrashOverrideEnabled)
            {
                _severeCrashModeActive = false;
                _severeCrashRecoveryWeeks = 0;
                return false;
            }

            if (!_severeCrashModeActive)
            {
                _severeCrashRecoveryWeeks = 0;
                if (!ShouldApplySevereCrashOverride(regimeSnapshot, totalPortfolioValue))
                {
                    return false;
                }

                _severeCrashModeActive = true;
                _severeCrashModeState = "enter";
                return true;
            }

            var drawdown = CalculateDrawdownFromHigh(totalPortfolioValue);
            if (drawdown < _severeCrashOverrideExitDrawdownThreshold)
            {
                _severeCrashModeActive = false;
                _severeCrashRecoveryWeeks = 0;
                _severeCrashModeState = "exit";
                _severeCrashExitReason = "drawdown-recovered";
                return false;
            }

            if (IsSevereCrashRecoveredRegime(regimeSnapshot.ActiveRegime))
            {
                _severeCrashRecoveryWeeks++;
                if (_severeCrashRecoveryWeeks >= _severeCrashOverrideRecoveryConfirmationWeeks)
                {
                    _severeCrashModeActive = false;
                    _severeCrashModeState = "exit";
                    _severeCrashExitReason = "regime-recovered";
                    return false;
                }
            }
            else
            {
                _severeCrashRecoveryWeeks = 0;
            }

            _severeCrashModeState = "hold";
            return true;
        }

        private static bool IsSevereCrashRecoveredRegime(RiskRegime regime)
        {
            return regime == RiskRegime.Neutral ||
                   regime == RiskRegime.Favorable;
        }

        private static int CountWeakSignals(RegimeSnapshot regimeSnapshot)
        {
            var count = 0;
            if (regimeSnapshot.TrendState == SignalState.Weak)
            {
                count++;
            }

            if (regimeSnapshot.BreadthState == SignalState.Weak)
            {
                count++;
            }

            if (regimeSnapshot.StressState == SignalState.Weak)
            {
                count++;
            }

            return count;
        }

        private bool ParseBooleanParameter(string name, bool defaultValue)
        {
            var raw = GetParameter(name);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return defaultValue;
            }

            if (bool.TryParse(raw, out var value))
            {
                return value;
            }

            Debug($"[AEGIS] Invalid boolean parameter {name}={raw}. Using default {defaultValue}.");
            return defaultValue;
        }

        private void RestorePersistedRuntimeState(AegisLiveState state)
        {
            if (state == null)
            {
                return;
            }

            _regimeModel.Restore(state.ActiveRegime, state.UpgradeConfirmationCount);
            _undeployedCapitalReserve = Math.Max(0m, state.UndeployedReserve);
            _lastCompletedWeeklyReviewUtc = state.LastCompletedWeeklyReviewUtc;
            _lastPlannedTargetWeights = state.LastPlannedTargetWeights
                .Where(kvp => TryGetTrackedSymbol(kvp.Key, out _))
                .ToDictionary(
                    kvp =>
                    {
                        TryGetTrackedSymbol(kvp.Key, out var symbol);
                        return symbol;
                    },
                    kvp => kvp.Value);
        }

        private void ReconcileLiveStartup()
        {
            try
            {
                var brokerHoldings = CaptureBrokerHoldingsByTicker();
                var brokerOpenOrders = CaptureBrokerOpenOrders();
                var persistedHoldings = _loadedLiveState?.BrokerHoldingsByTicker ?? new Dictionary<string, decimal>();
                var persistedOpenOrders = _loadedLiveState?.OpenOrders ?? new List<AegisOpenOrderState>();

                var holdingsMatch = DictionariesMatch(brokerHoldings, persistedHoldings);
                var openOrdersMatch = OpenOrdersMatch(brokerOpenOrders.Values, persistedOpenOrders);

                if (!holdingsMatch || !openOrdersMatch)
                {
                    Debug(
                        $"[AEGIS-LIVE] {Time}: broker/store mismatch detected. BrokerHoldings={brokerHoldings.Count} StoreHoldings={persistedHoldings.Count} BrokerOpenOrders={brokerOpenOrders.Count} StoreOpenOrders={persistedOpenOrders.Count}. Broker state wins.");
                }
                else
                {
                    Debug($"[AEGIS-LIVE] {Time}: broker/store state matched on startup.");
                }

                _trackedOpenOrders = brokerOpenOrders;
                _startupReconciliationComplete = true;
                Debug(
                    $"[AEGIS-LIVE] {Time}: startup reconciliation complete. Holdings={brokerHoldings.Count} OpenOrders={_trackedOpenOrders.Count} RestoredRegime={_regimeModel.ActiveRegime} LastReview={_lastCompletedWeeklyReviewUtc?.ToString("u") ?? "none"}");
            }
            catch (Exception ex)
            {
                _startupReconciliationComplete = false;
                Error($"[AEGIS-LIVE] {Time}: startup reconciliation failed: {ex.Message}");
            }
        }

        private void SaveLiveState(string reason)
        {
            if (!LiveMode)
            {
                return;
            }

            RefreshTrackedOpenOrdersFromBroker();
            _liveStateStore.Save(BuildPersistedState(), reason);
        }

        private AegisLiveState BuildPersistedState()
        {
            return new AegisLiveState
            {
                SchemaVersion = StrategyConfig.LiveStateSchemaVersion,
                ActiveRegime = _regimeModel.ActiveRegime,
                UpgradeConfirmationCount = _regimeModel.UpgradeConfirmationCount,
                UndeployedReserve = _undeployedCapitalReserve,
                LastCompletedWeeklyReviewUtc = _lastCompletedWeeklyReviewUtc,
                LastPlannedTargetWeights = _lastPlannedTargetWeights
                    .ToDictionary(kvp => kvp.Key.Value, kvp => kvp.Value, StringComparer.Ordinal),
                BrokerHoldingsByTicker = CaptureBrokerHoldingsByTicker(),
                OpenOrders = _trackedOpenOrders.Values
                    .OrderBy(order => order.OrderId)
                    .Select(order => new AegisOpenOrderState
                    {
                        OrderId = order.OrderId,
                        Ticker = order.Ticker,
                        Direction = order.Direction,
                        Quantity = order.Quantity,
                        Status = order.Status
                    })
                    .ToList()
            };
        }

        private Dictionary<string, decimal> CaptureBrokerHoldingsByTicker()
        {
            var holdings = new Dictionary<string, decimal>(StringComparer.Ordinal);

            foreach (var assetState in _assetStates.Values)
            {
                var quantity = Portfolio[assetState.Symbol].Quantity;
                if (Math.Abs(quantity) <= StrategyConfig.LiveStateQuantityTolerance)
                {
                    continue;
                }

                holdings[assetState.Ticker] = quantity;
            }

            return holdings;
        }

        private Dictionary<int, AegisOpenOrderState> CaptureBrokerOpenOrders()
        {
            return Transactions.GetOpenOrders()
                .Where(order => _assetStates.ContainsKey(order.Symbol))
                .ToDictionary(
                    order => order.Id,
                    order => new AegisOpenOrderState
                    {
                        OrderId = order.Id,
                        Ticker = order.Symbol.Value,
                        Direction = order.Direction.ToString(),
                        Quantity = order.Quantity,
                        Status = order.Status.ToString()
                    });
        }

        private void RefreshTrackedOpenOrdersFromBroker()
        {
            _trackedOpenOrders = CaptureBrokerOpenOrders();
        }

        private bool TryGetTrackedSymbol(string ticker, out Symbol symbol)
        {
            var assetState = _assetStates.Values.FirstOrDefault(state => state.Ticker.Equals(ticker, StringComparison.Ordinal));
            if (assetState != null)
            {
                symbol = assetState.Symbol;
                return true;
            }

            symbol = null;
            return false;
        }

        private static bool DictionariesMatch(
            IReadOnlyDictionary<string, decimal> left,
            IReadOnlyDictionary<string, decimal> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            foreach (var kvp in left)
            {
                if (!right.TryGetValue(kvp.Key, out var value))
                {
                    return false;
                }

                if (Math.Abs(kvp.Value - value) > StrategyConfig.LiveStateQuantityTolerance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool OpenOrdersMatch(
            IEnumerable<AegisOpenOrderState> brokerOrders,
            IEnumerable<AegisOpenOrderState> persistedOrders)
        {
            var brokerList = brokerOrders
                .OrderBy(order => order.OrderId)
                .ToList();
            var persistedList = persistedOrders
                .OrderBy(order => order.OrderId)
                .ToList();

            if (brokerList.Count != persistedList.Count)
            {
                return false;
            }

            for (var index = 0; index < brokerList.Count; index++)
            {
                var broker = brokerList[index];
                var persisted = persistedList[index];
                if (broker.OrderId != persisted.OrderId ||
                    !string.Equals(broker.Ticker, persisted.Ticker, StringComparison.Ordinal) ||
                    !string.Equals(broker.Direction, persisted.Direction, StringComparison.Ordinal) ||
                    !string.Equals(broker.Status, persisted.Status, StringComparison.Ordinal) ||
                    Math.Abs(broker.Quantity - persisted.Quantity) > StrategyConfig.LiveStateQuantityTolerance)
                {
                    return false;
                }
            }

            return true;
        }

        private string FormatWeeklySummary(PortfolioPlan plan, RegimeSnapshot regimeSnapshot)
        {
            var currentCashWeight = Math.Max(0m, 1m - plan.CurrentGrowthWeight - plan.CurrentDefensiveWeight);
            var targetGrowthWeight = plan.TargetWeights
                .Where(pair => _assetStates.TryGetValue(pair.Key, out var assetState) && assetState.IsGrowth)
                .Sum(pair => pair.Value);
            var targetDefensiveWeight = plan.TargetWeights
                .Where(pair => _assetStates.TryGetValue(pair.Key, out var assetState) && assetState.IsDefensive)
                .Sum(pair => pair.Value);
            var targetCashWeight = Math.Max(0m, 1m - targetGrowthWeight - targetDefensiveWeight);

            return string.Format(
                CultureInfo.InvariantCulture,
                "[AEGIS] {0:yyyy-MM-dd} Prev={1} Act={2} Raw={3} Trend={4} Breadth={5} Stress={6} Severe={7} Curr=G{8:0.00}/D{9:0.00}/C{10:0.00} Target=G{11:0.00}/D{12:0.00}/C{13:0.00} Growth{14} Def{15} Forced={16} Trim={17} Reserve={18:0.##}",
                Time,
                plan.PreviousRegime,
                plan.ActiveRegime,
                regimeSnapshot.RawRegime,
                regimeSnapshot.TrendState,
                regimeSnapshot.BreadthState,
                regimeSnapshot.StressState,
                regimeSnapshot.SevereStress,
                plan.CurrentGrowthWeight,
                plan.CurrentDefensiveWeight,
                currentCashWeight,
                targetGrowthWeight,
                targetDefensiveWeight,
                targetCashWeight,
                FormatSymbolPreview(plan.SelectedGrowthSymbols),
                FormatSymbolPreview(plan.SelectedDefensiveSymbols),
                plan.ForcedExitSymbols.Count,
                plan.TrimOnly,
                plan.ReleasedReserve);
        }

        private string FormatCrisisDiagnostics(
            PortfolioPlan plan,
            RegimeSnapshot regimeSnapshot,
            IReadOnlyDictionary<Symbol, decimal> currentWeights,
            decimal breadth,
            decimal vixAverage5,
            decimal reserveBeforeReview,
            bool preWeakGuardActive,
            bool severeCrashOverrideActive,
            string sleeveOverride,
            string overrideReason,
            decimal drawdownFromHigh,
            SleeveTargets baseSleeveTargets,
            SleeveTargets finalSleeveTargets,
            string severeCrashModeState,
            int severeCrashRecoveryWeeks,
            string severeCrashExitReason)
        {
            var currentCashWeight = Math.Max(0m, 1m - plan.CurrentGrowthWeight - plan.CurrentDefensiveWeight);
            var targetGrowthWeight = plan.TargetWeights
                .Where(pair => _assetStates.TryGetValue(pair.Key, out var assetState) && assetState.IsGrowth)
                .Sum(pair => pair.Value);
            var targetDefensiveWeight = plan.TargetWeights
                .Where(pair => _assetStates.TryGetValue(pair.Key, out var assetState) && assetState.IsDefensive)
                .Sum(pair => pair.Value);
            var targetCashWeight = Math.Max(0m, 1m - targetGrowthWeight - targetDefensiveWeight);

            return string.Format(
                CultureInfo.InvariantCulture,
                "[AEGIS-DIAG] {0:yyyy-MM-dd} Equity={1:0.00} Cash={2:0.00} Prev={3} Act={4} Raw={5} Trend={6} BreadthState={7} Stress={8} Severe={9} Inputs=SpyClose:{10:0.00},SpySma200:{11:0.00},SpySma200Lookback:{12:0.00},Breadth:{13:0.0000},Vix5:{14:0.00} Curr=G{15:0.0000}/D{16:0.0000}/C{17:0.0000} Target=G{18:0.0000}/D{19:0.0000}/C{20:0.0000} {21} Bands={22} SelectionChange={23} Rebalance={24} Trim={25} Forced={26} OptRepl={27} NewEntries={28} ReserveBefore={29:0.####} ReserveAfter={30:0.####} ReleasedReserve={31:0.####} Growth={32} Defensive={33} CurrentWeights={34} TargetWeights={35}",
                Time,
                Portfolio.TotalPortfolioValue,
                Portfolio.Cash,
                plan.PreviousRegime,
                plan.ActiveRegime,
                regimeSnapshot.RawRegime,
                regimeSnapshot.TrendState,
                regimeSnapshot.BreadthState,
                regimeSnapshot.StressState,
                regimeSnapshot.SevereStress,
                _marketState.CurrentClose,
                _marketState.CurrentSma200,
                _marketState.LookbackSma200,
                breadth,
                vixAverage5,
                plan.CurrentGrowthWeight,
                plan.CurrentDefensiveWeight,
                currentCashWeight,
                targetGrowthWeight,
                targetDefensiveWeight,
                targetCashWeight,
                FormatOverrideDiagnostics(
                    preWeakGuardActive,
                    severeCrashOverrideActive,
                    sleeveOverride,
                    overrideReason,
                    drawdownFromHigh,
                    baseSleeveTargets,
                    finalSleeveTargets,
                    severeCrashModeState,
                    severeCrashRecoveryWeeks,
                    severeCrashExitReason),
                plan.SleevesWithinToleranceBands,
                plan.HasSelectionChange,
                plan.HasRebalanceTrigger,
                plan.TrimOnly,
                FormatSymbolList(plan.ForcedExitSymbols),
                plan.OptimizationReplacementsUsed,
                plan.NewEntriesUsed,
                reserveBeforeReview,
                _undeployedCapitalReserve,
                plan.ReleasedReserve,
                FormatSymbolList(plan.SelectedGrowthSymbols),
                FormatSymbolList(plan.SelectedDefensiveSymbols),
                FormatSymbolWeights(currentWeights),
                FormatSymbolWeights(plan.TargetWeights));
        }

        private string FormatOverrideDiagnostics(
            bool preWeakGuardActive,
            bool severeCrashOverrideActive,
            string sleeveOverride,
            string overrideReason,
            decimal drawdownFromHigh,
            SleeveTargets baseSleeveTargets,
            SleeveTargets finalSleeveTargets,
            string severeCrashModeState,
            int severeCrashRecoveryWeeks,
            string severeCrashExitReason)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "PreWeakGuardActive={0} SevereCrashOverrideActive={1} SleeveOverride={2} OverrideReason={3} DrawdownFromHigh={4:0.0000} BaseTarget={5} FinalTarget={6} SevereCrashModeState={7} SevereCrashRecoveryWeeks={8} SevereCrashExitReason={9}",
                preWeakGuardActive,
                severeCrashOverrideActive,
                sleeveOverride,
                overrideReason,
                drawdownFromHigh,
                FormatSleeveTargets(baseSleeveTargets),
                FormatSleeveTargets(finalSleeveTargets),
                severeCrashModeState,
                severeCrashRecoveryWeeks,
                severeCrashExitReason);
        }

        private static string FormatSleeveTargets(SleeveTargets targets)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "G{0:0.0000}/D{1:0.0000}/C{2:0.0000}",
                targets.GrowthTarget,
                targets.DefensiveTarget,
                targets.CashTarget);
        }

        private static string FormatSymbolPreview(IReadOnlyList<Symbol> symbols)
        {
            if (symbols.Count == 0)
            {
                return "[0]";
            }

            const int previewCount = 3;
            var preview = string.Join(",", symbols.Take(previewCount).Select(symbol => symbol.Value));
            var extraCount = symbols.Count - previewCount;

            return extraCount > 0
                ? $"[{symbols.Count}]={preview}+{extraCount}"
                : $"[{symbols.Count}]={preview}";
        }

        private static string FormatSymbolList(IEnumerable<Symbol> symbols)
        {
            var values = symbols
                .Select(symbol => symbol.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList();

            return values.Count == 0
                ? "none"
                : string.Join(",", values);
        }

        private static string FormatSymbolWeights(IReadOnlyDictionary<Symbol, decimal> weights)
        {
            if (weights.Count == 0)
            {
                return "none";
            }

            return string.Join(
                ",",
                weights
                    .OrderBy(pair => pair.Key.Value, StringComparer.Ordinal)
                    .Select(pair => string.Format(CultureInfo.InvariantCulture, "{0}:{1:0.0000}", pair.Key.Value, pair.Value)));
        }

        private sealed class AssetState
        {
            public AssetState(
                Symbol symbol,
                string ticker,
                bool isGrowth,
                bool isDefensive,
                bool isSgov,
                SimpleMovingAverage sma50,
                SimpleMovingAverage sma200,
                AverageTrueRange atr20,
                int closeWindowSize)
            {
                Symbol = symbol;
                Ticker = ticker;
                IsGrowth = isGrowth;
                IsDefensive = isDefensive;
                IsSgov = isSgov;
                Sma50 = sma50;
                Sma200 = sma200;
                Atr20 = atr20;
                CloseWindow = new RollingWindow<decimal>(closeWindowSize);
            }

            public Symbol Symbol { get; }
            public string Ticker { get; }
            public bool IsGrowth { get; }
            public bool IsDefensive { get; }
            public bool IsSgov { get; }
            public SimpleMovingAverage Sma50 { get; }
            public SimpleMovingAverage Sma200 { get; }
            public AverageTrueRange Atr20 { get; }
            public RollingWindow<decimal> CloseWindow { get; }

            public bool IsDataReady =>
                Sma50.IsReady &&
                Sma200.IsReady &&
                Atr20.IsReady &&
                CloseWindow.Count >= StrategyConfig.Return126Period;

            public decimal CurrentClose => CloseWindow.Count > 0 ? CloseWindow[0] : 0m;

            public void Update(TradeBar bar)
            {
                CloseWindow.Add(bar.Close);
            }
        }

        private sealed class MarketState
        {
            public MarketState(
                Symbol symbol,
                SimpleMovingAverage sma200,
                int closeWindowSize,
                int smaWindowSize)
            {
                Symbol = symbol;
                Sma200 = sma200;
                CloseWindow = new RollingWindow<decimal>(closeWindowSize);
                Sma200Window = new RollingWindow<decimal>(smaWindowSize);
            }

            public Symbol Symbol { get; }
            public SimpleMovingAverage Sma200 { get; }
            public RollingWindow<decimal> CloseWindow { get; }
            public RollingWindow<decimal> Sma200Window { get; }

            public bool IsReady =>
                Sma200.IsReady &&
                CloseWindow.Count > 0 &&
                Sma200Window.Count > StrategyConfig.TrendSlopeLookbackDays;

            public decimal CurrentClose => CloseWindow.Count > 0 ? CloseWindow[0] : 0m;
            public decimal CurrentSma200 => Sma200.Current.Value;
            public decimal LookbackSma200 => Sma200Window[StrategyConfig.TrendSlopeLookbackDays];

            public void Update(TradeBar bar)
            {
                CloseWindow.Add(bar.Close);
                if (Sma200.IsReady)
                {
                    Sma200Window.Add(Sma200.Current.Value);
                }
            }
        }
    }
}
