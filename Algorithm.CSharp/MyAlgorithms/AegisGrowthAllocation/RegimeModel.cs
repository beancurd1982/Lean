#region imports
using System;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    public sealed class RegimeModel
    {
        private int _upgradeConfirmationCount;

        public RegimeModel(RiskRegime initialRegime = RiskRegime.Neutral)
        {
            ActiveRegime = initialRegime;
        }

        public RiskRegime ActiveRegime { get; private set; }
        public int UpgradeConfirmationCount => _upgradeConfirmationCount;

        public RegimeSnapshot Update(RegimeInputs inputs)
        {
            inputs.Validate();

            var trendState = ClassifyTrend(inputs);
            var breadthState = ClassifyBreadth(inputs.GrowthBreadth);
            var stressState = ClassifyStress(inputs.VixAverage5);
            var severeStress = inputs.VixAverage5 >= StrategyConfig.SevereStressThreshold;
            var rawRegime = DetermineRawRegime(trendState, breadthState, stressState, severeStress);
            var previousRegime = ActiveRegime;

            if (severeStress)
            {
                ActiveRegime = RiskRegime.Weak;
                _upgradeConfirmationCount = 0;
            }
            else if (rawRegime < ActiveRegime)
            {
                ActiveRegime = StepTowardWeaker(ActiveRegime);
                _upgradeConfirmationCount = 0;
            }
            else if (rawRegime > ActiveRegime)
            {
                _upgradeConfirmationCount++;
                if (_upgradeConfirmationCount >= StrategyConfig.UpgradeConfirmationWeeks)
                {
                    ActiveRegime = StepTowardStronger(ActiveRegime);
                    _upgradeConfirmationCount = 0;
                }
            }
            else
            {
                _upgradeConfirmationCount = 0;
            }

            return new RegimeSnapshot(
                previousRegime,
                ActiveRegime,
                rawRegime,
                trendState,
                breadthState,
                stressState,
                severeStress,
                _upgradeConfirmationCount);
        }

        public void Reset(RiskRegime regime = RiskRegime.Neutral)
        {
            ActiveRegime = regime;
            _upgradeConfirmationCount = 0;
        }

        public void Restore(RiskRegime regime, int upgradeConfirmationCount)
        {
            ActiveRegime = Enum.IsDefined(typeof(RiskRegime), regime)
                ? regime
                : RiskRegime.Neutral;
            _upgradeConfirmationCount = Math.Max(0, upgradeConfirmationCount);
        }

        public static SignalState ClassifyTrend(RegimeInputs inputs)
        {
            var favorableLevel = inputs.SpySma200 * (1m + StrategyConfig.TrendUpperBuffer);
            var weakLevel = inputs.SpySma200 * (1m - StrategyConfig.TrendLowerBuffer);

            if (inputs.SpyClose >= favorableLevel && inputs.SpySma200 > inputs.SpySma200Lookback)
            {
                return SignalState.Favorable;
            }

            if (inputs.SpyClose <= weakLevel && inputs.SpySma200 <= inputs.SpySma200Lookback)
            {
                return SignalState.Weak;
            }

            return SignalState.Neutral;
        }

        public static SignalState ClassifyBreadth(decimal growthBreadth)
        {
            if (growthBreadth >= StrategyConfig.FavorableBreadthThreshold)
            {
                return SignalState.Favorable;
            }

            if (growthBreadth <= StrategyConfig.WeakBreadthThreshold)
            {
                return SignalState.Weak;
            }

            return SignalState.Neutral;
        }

        public static SignalState ClassifyStress(decimal vixAverage5)
        {
            if (vixAverage5 <= StrategyConfig.FavorableStressThreshold)
            {
                return SignalState.Favorable;
            }

            if (vixAverage5 >= StrategyConfig.WeakStressThreshold)
            {
                return SignalState.Weak;
            }

            return SignalState.Neutral;
        }

        public static RiskRegime DetermineRawRegime(
            SignalState trendState,
            SignalState breadthState,
            SignalState stressState,
            bool severeStress)
        {
            if (severeStress)
            {
                return RiskRegime.Weak;
            }

            if (trendState == SignalState.Weak && breadthState == SignalState.Weak)
            {
                return RiskRegime.Weak;
            }

            if (stressState == SignalState.Weak &&
                (trendState != SignalState.Favorable || breadthState != SignalState.Favorable))
            {
                return RiskRegime.Weak;
            }

            if (trendState == SignalState.Favorable &&
                breadthState == SignalState.Favorable &&
                stressState == SignalState.Favorable)
            {
                return RiskRegime.Favorable;
            }

            return RiskRegime.Neutral;
        }

        private static RiskRegime StepTowardWeaker(RiskRegime regime)
        {
            return regime switch
            {
                RiskRegime.Favorable => RiskRegime.Neutral,
                RiskRegime.Neutral => RiskRegime.Weak,
                _ => RiskRegime.Weak
            };
        }

        private static RiskRegime StepTowardStronger(RiskRegime regime)
        {
            return regime switch
            {
                RiskRegime.Weak => RiskRegime.Neutral,
                RiskRegime.Neutral => RiskRegime.Favorable,
                _ => RiskRegime.Favorable
            };
        }
    }

    public sealed class RegimeInputs
    {
        public RegimeInputs(decimal spyClose, decimal spySma200, decimal spySma200Lookback, decimal growthBreadth, decimal vixAverage5)
        {
            SpyClose = spyClose;
            SpySma200 = spySma200;
            SpySma200Lookback = spySma200Lookback;
            GrowthBreadth = growthBreadth;
            VixAverage5 = vixAverage5;
        }

        public decimal SpyClose { get; }
        public decimal SpySma200 { get; }
        public decimal SpySma200Lookback { get; }
        public decimal GrowthBreadth { get; }
        public decimal VixAverage5 { get; }

        public void Validate()
        {
            if (SpyClose <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(SpyClose), "SPY close must be positive.");
            }

            if (SpySma200 <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(SpySma200), "SPY 200-day SMA must be positive.");
            }

            if (SpySma200Lookback <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(SpySma200Lookback), "SPY 200-day SMA lookback value must be positive.");
            }

            if (GrowthBreadth < 0m || GrowthBreadth > 1m)
            {
                throw new ArgumentOutOfRangeException(nameof(GrowthBreadth), "Growth breadth must be expressed as a 0-1 fraction.");
            }

            if (VixAverage5 < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(VixAverage5), "VIX average cannot be negative.");
            }
        }
    }

    public sealed class RegimeSnapshot
    {
        public RegimeSnapshot(
            RiskRegime previousRegime,
            RiskRegime activeRegime,
            RiskRegime rawRegime,
            SignalState trendState,
            SignalState breadthState,
            SignalState stressState,
            bool severeStress,
            int upgradeConfirmationCount)
        {
            PreviousRegime = previousRegime;
            ActiveRegime = activeRegime;
            RawRegime = rawRegime;
            TrendState = trendState;
            BreadthState = breadthState;
            StressState = stressState;
            SevereStress = severeStress;
            UpgradeConfirmationCount = upgradeConfirmationCount;
        }

        public RiskRegime PreviousRegime { get; }
        public RiskRegime ActiveRegime { get; }
        public RiskRegime RawRegime { get; }
        public SignalState TrendState { get; }
        public SignalState BreadthState { get; }
        public SignalState StressState { get; }
        public bool SevereStress { get; }
        public int UpgradeConfirmationCount { get; }
    }

    public enum RiskRegime
    {
        Weak = 0,
        Neutral = 1,
        Favorable = 2
    }

    public enum SignalState
    {
        Weak = 0,
        Neutral = 1,
        Favorable = 2
    }
}
