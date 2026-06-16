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
    public partial class AegisGrowthAllocation : QCAlgorithm
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
        private bool _startupStateSaveDeferred;
        private bool _firstLiveDataDiagnosticLogged;
        private DateTime? _lastCompletedWeeklyReviewUtc;
        private Dictionary<Symbol, decimal> _lastPlannedTargetWeights = new Dictionary<Symbol, decimal>();
        private Dictionary<int, AegisOpenOrderState> _trackedOpenOrders = new Dictionary<int, AegisOpenOrderState>();
        private AegisLiveState _loadedLiveState;
        private bool _crisisDiagnosticsEnabled;
        private bool _weakStressOverlayEnabled;
        private bool _preWeakGuardEnabled = StrategyConfig.DefaultPreWeakGuardEnabled;
        private bool _preWeakRecoveryEnabled = StrategyConfig.DefaultPreWeakRecoveryEnabled;
        private bool _severeCrashOverrideEnabled;
        private decimal _preWeakGuardDrawdownThreshold = StrategyConfig.DefaultPreWeakGuardDrawdownThreshold;
        private decimal _preWeakRecoveryDrawdownImprovement = StrategyConfig.DefaultPreWeakRecoveryDrawdownImprovement;
        private decimal _preWeakRecoveryMaxDrawdown = StrategyConfig.DefaultPreWeakRecoveryMaxDrawdown;
        private decimal _severeCrashOverrideDrawdownThreshold = StrategyConfig.DefaultSevereCrashOverrideDrawdownThreshold;
        private decimal _severeCrashOverrideExitDrawdownThreshold = StrategyConfig.DefaultSevereCrashOverrideExitDrawdownThreshold;
        private int _severeCrashOverrideRecoveryConfirmationWeeks = StrategyConfig.DefaultSevereCrashOverrideRecoveryConfirmationWeeks;
        private int _preWeakRecoveryConfirmationWeeksRequired = StrategyConfig.DefaultPreWeakRecoveryConfirmationWeeks;
        private bool _preWeakRecoveryPreviousPreWeakActive;
        private int _preWeakRecoverySegmentId;
        private decimal _preWeakRecoveryLocalTroughDrawdown;
        private int _preWeakRecoveryConfirmationWeeks;
        private bool _preWeakRecoveryActive;
        private string _preWeakRecoveryLastResetReason = "none";
        private bool _severeCrashModeActive;
        private int _severeCrashRecoveryWeeks;
        private string _severeCrashModeState = "none";
        private string _severeCrashExitReason = "none";
        private decimal _defensiveOverrideEquityHighWaterMark;
        private readonly DiagnosticAttributionTracker _diagnosticAttributionTracker = new DiagnosticAttributionTracker();
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
                _preWeakRecoveryEnabled = ParseBooleanParameter(
                    StrategyConfig.PreWeakRecoveryParameter,
                    StrategyConfig.DefaultPreWeakRecoveryEnabled);
                _severeCrashOverrideEnabled = ParseBooleanParameter(StrategyConfig.SevereCrashOverrideParameter, false);
                _preWeakGuardDrawdownThreshold = ParseDecimalParameter(
                    StrategyConfig.PreWeakGuardDrawdownThresholdParameter,
                    StrategyConfig.DefaultPreWeakGuardDrawdownThreshold,
                    value => value > 0m && value < 1m);
                _preWeakRecoveryDrawdownImprovement = ParseDecimalParameter(
                    StrategyConfig.PreWeakRecoveryDrawdownImprovementParameter,
                    StrategyConfig.DefaultPreWeakRecoveryDrawdownImprovement,
                    value => value > 0m && value <= 0.10m);
                _preWeakRecoveryMaxDrawdown = ParseDecimalParameter(
                    StrategyConfig.PreWeakRecoveryMaxDrawdownParameter,
                    StrategyConfig.DefaultPreWeakRecoveryMaxDrawdown,
                    value => value > 0m && value < 1m);
                _preWeakRecoveryConfirmationWeeksRequired = ParseIntParameter(
                    StrategyConfig.PreWeakRecoveryConfirmationWeeksParameter,
                    StrategyConfig.DefaultPreWeakRecoveryConfirmationWeeks,
                    value => value >= 1 && value <= 8);
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
                if (ReconcileLiveStartup())
                {
                    SaveLiveState("Startup reconciliation");
                }
                else
                {
                    _startupStateSaveDeferred = true;
                }
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
                $"AegisGrowthAllocation deployment identity. AlgorithmVersion={StrategyConfig.AlgorithmVersion} SourceRevision={StrategyConfig.SourceRevision} LiveStateKey={StrategyConfig.LiveStateKey} LiveStateSchemaVersion={StrategyConfig.LiveStateSchemaVersion}");

            Debug(
                $"AegisGrowthAllocation initialized. GrowthUniverse={StrategyConfig.GrowthTickers.Count} DefensiveUniverse={StrategyConfig.DefensiveTickers.Count} UndeployedReserve={_undeployedCapitalReserve.ToString(CultureInfo.InvariantCulture)} FavorableBreadthThreshold={StrategyConfig.FavorableBreadthThreshold.ToString(CultureInfo.InvariantCulture)} WeakStressThreshold={StrategyConfig.WeakStressThreshold.ToString(CultureInfo.InvariantCulture)} SevereStressGap={StrategyConfig.SevereStressGap.ToString(CultureInfo.InvariantCulture)} SevereStressThreshold={StrategyConfig.SevereStressThreshold.ToString(CultureInfo.InvariantCulture)} UpgradeConfirmationWeeks={StrategyConfig.UpgradeConfirmationWeeks} GrowthAtrEligibilityLimit={StrategyConfig.GrowthAtrEligibilityLimit.ToString(CultureInfo.InvariantCulture)} ReplacementScoreGap={StrategyConfig.ReplacementScoreGap.ToString(CultureInfo.InvariantCulture)} HoldStabilityBonus={StrategyConfig.HoldStabilityBonus.ToString(CultureInfo.InvariantCulture)} ToleranceBandScale={StrategyConfig.RebalanceToleranceBandScale.ToString(CultureInfo.InvariantCulture)}");
            Debug(
                $"AegisGrowthAllocation sleeve targets. PreWeakGrowthTarget={StrategyConfig.PreWeakGrowthTarget.ToString(CultureInfo.InvariantCulture)} PreWeakDefensiveTarget={StrategyConfig.PreWeakDefensiveTarget.ToString(CultureInfo.InvariantCulture)} PreWeakRecoveryEnabled={_preWeakRecoveryEnabled} PreWeakRecoveryGrowthTarget={StrategyConfig.PreWeakRecoveryGrowthTarget.ToString(CultureInfo.InvariantCulture)} PreWeakRecoveryDefensiveTarget={StrategyConfig.PreWeakRecoveryDefensiveTarget.ToString(CultureInfo.InvariantCulture)} WeakGrowthTarget={StrategyConfig.WeakGrowthTarget.ToString(CultureInfo.InvariantCulture)}");
        }

        public override void OnData(Slice slice)
        {
            if (LiveMode && !IsWarmingUp && !_firstLiveDataDiagnosticLogged)
            {
                _firstLiveDataDiagnosticLogged = true;
                TryCompleteDeferredStartupStateSave("FirstLiveOnData");
            }

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
            if (_crisisDiagnosticsEnabled)
            {
                Debug(FormatCompactCrisisDiagnosticSummary());
            }

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

            LogStressDiagnostic("WeeklyReview");

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

            var totalPortfolioValue = Portfolio.TotalPortfolioValue;
            if (_crisisDiagnosticsEnabled || _preWeakGuardEnabled || _severeCrashOverrideEnabled)
            {
                UpdateDefensiveOverrideHighWaterMark(totalPortfolioValue);
            }
            var drawdownFromHigh = CalculateDrawdownFromHigh(totalPortfolioValue);
            var baseSleeveTargets = StrategyConfig.GetSleeveTargets(regimeSnapshot.ActiveRegime);
            var sleeveTargetsOverride = (SleeveTargets)null;
            var sleeveOverride = "none";
            var overrideReason = "base-regime";
            var severeCrashOverrideActive = UpdateSevereCrashMode(regimeSnapshot, totalPortfolioValue);
            var preWeakGuardActive = false;
            var preWeakRecoveryActive = false;
            var preWeakRecoveryAmount = 0m;
            if (severeCrashOverrideActive)
            {
                sleeveTargetsOverride = StrategyConfig.SevereCrashOverrideSleeveTargets;
                sleeveOverride = "severe-crash";
                overrideReason = $"severe-crash-{_severeCrashModeState}";
                UpdatePreWeakRecoveryState(regimeSnapshot, normalPreWeakActive: false, drawdownFromHigh);
            }
            else if (ShouldApplyWeakStressOverlay(regimeSnapshot))
            {
                sleeveTargetsOverride = StrategyConfig.WeakStressOverlaySleeveTargets;
                sleeveOverride = "weak-stress";
                overrideReason = "weak-stress-overlay";
                UpdatePreWeakRecoveryState(regimeSnapshot, normalPreWeakActive: false, drawdownFromHigh);
            }
            else if (ShouldApplyPreWeakGuard(regimeSnapshot, totalPortfolioValue))
            {
                sleeveTargetsOverride = StrategyConfig.PreWeakGuardSleeveTargets;
                sleeveOverride = "pre-weak";
                overrideReason = "drawdown-signals";
                preWeakGuardActive = true;
                preWeakRecoveryActive = UpdatePreWeakRecoveryState(
                    regimeSnapshot,
                    normalPreWeakActive: true,
                    drawdownFromHigh);
                preWeakRecoveryAmount = Math.Max(0m, _preWeakRecoveryLocalTroughDrawdown - drawdownFromHigh);
                if (preWeakRecoveryActive)
                {
                    sleeveTargetsOverride = StrategyConfig.PreWeakRecoverySleeveTargets;
                    sleeveOverride = "pre-weak-recovery";
                    overrideReason = "pre-weak-recovery-confirmed";
                }
            }
            else
            {
                UpdatePreWeakRecoveryState(regimeSnapshot, normalPreWeakActive: false, drawdownFromHigh);
            }

            var finalSleeveTargets = sleeveTargetsOverride ?? baseSleeveTargets;
            var reserveBeforeReview = _undeployedCapitalReserve;
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

            if (_crisisDiagnosticsEnabled)
            {
                Debug(FormatCrisisDiagnostics(
                    plan,
                    regimeSnapshot,
                    growthSelection,
                    defensiveSelection,
                    currentWeights,
                    breadth,
                    vixAverage5,
                    reserveBeforeReview,
                    preWeakGuardActive,
                    severeCrashOverrideActive,
                    sleeveOverride,
                    overrideReason,
                    preWeakRecoveryActive,
                    _preWeakRecoverySegmentId,
                    _preWeakRecoveryLocalTroughDrawdown,
                    preWeakRecoveryAmount,
                    _preWeakRecoveryConfirmationWeeks,
                    _preWeakRecoveryLastResetReason,
                    drawdownFromHigh,
                    baseSleeveTargets,
                    finalSleeveTargets,
                    _severeCrashModeState,
                    _severeCrashRecoveryWeeks,
                    _severeCrashExitReason));
                RecordCrisisDiagnosticObservation(
                    Time.Date,
                    totalPortfolioValue,
                    regimeSnapshot.ActiveRegime,
                    preWeakGuardActive,
                    preWeakRecoveryActive,
                    severeCrashOverrideActive,
                    drawdownFromHigh,
                    finalSleeveTargets);
            }
            else
            {
                Debug(FormatWeeklySummary(plan, regimeSnapshot));
            }
        }

        public override void OnWarmupFinished()
        {
            if (!LiveMode)
            {
                return;
            }

            TryCompleteDeferredStartupStateSave("WarmupFinished");
            LogStressDiagnostic("WarmupFinished");
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
