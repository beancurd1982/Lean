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
        private decimal _undeployedCapitalReserve;

        public override void Initialize()
        {
            SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);

            if (!LiveMode)
            {
                SetStartDate(2018, 1, 1);
                SetCash(30000);
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

            _undeployedCapitalReserve = ParseDecimalParameter(StrategyConfig.UndeployedReserveParameter);
            SetWarmUp(StrategyConfig.WarmupTradingDays, Resolution.Daily);

            _regimeModel = new RegimeModel();
            _stockSelectionModel = new StockSelectionModel();
            _portfolioManager = new PortfolioManager();

            Schedule.On(
                DateRules.Every(DayOfWeek.Monday),
                TimeRules.At(StrategyConfig.WeeklyDecisionTime.Hours, StrategyConfig.WeeklyDecisionTime.Minutes),
                WeeklyReview);

            Debug($"AegisGrowthAllocation initialized. GrowthUniverse={StrategyConfig.GrowthTickers.Count} DefensiveUniverse={StrategyConfig.DefensiveTickers.Count} UndeployedReserve={_undeployedCapitalReserve.ToString(CultureInfo.InvariantCulture)}");
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

        private void WeeklyReview()
        {
            if (IsWarmingUp)
            {
                return;
            }

            if (!_marketState.IsReady || _stressWindow.Count < StrategyConfig.VixAverageWindow)
            {
                Debug($"[AEGIS] {Time} Weekly review skipped. MarketReady={_marketState.IsReady} StressCount={_stressWindow.Count}");
                return;
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

            var plan = _portfolioManager.BuildPlan(
                regimeSnapshot.PreviousRegime,
                regimeSnapshot.ActiveRegime,
                growthSelection,
                defensiveSelection,
                currentWeights,
                _undeployedCapitalReserve);

            ExecutePlan(plan, currentWeights);

            Debug(FormatWeeklySummary(plan, regimeSnapshot));
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

        private decimal ParseDecimalParameter(string name)
        {
            var raw = GetParameter(name);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return 0m;
            }

            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? Math.Max(0m, value)
                : 0m;
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
