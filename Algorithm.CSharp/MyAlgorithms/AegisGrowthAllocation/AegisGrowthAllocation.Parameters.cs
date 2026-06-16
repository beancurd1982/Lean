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
    {        private static double GetWeeklyDecisionMinutesAfterMarketOpen()
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

        private void ConfigureRuntimeParameters()
        {
            var favorableBreadthThreshold = ParseDecimalParameter(
                StrategyConfig.FavorableBreadthThresholdParameter,
                StrategyConfig.DefaultFavorableBreadthThreshold,
                value => value > StrategyConfig.WeakBreadthThreshold && value <= 1m);
            var stressBand = StrategyConfig.ParseStressBandParameters(name => GetParameter(name), message => Debug(message));
            var weakStressThreshold = stressBand.WeakStressThreshold;
            var severeStressGap = stressBand.SevereStressGap;
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
            var sleeveTargets = StrategyConfig.ParseSleeveTargetParameters(name => GetParameter(name), message => Debug(message));

            StrategyConfig.ConfigureRuntimeParameters(
                favorableBreadthThreshold,
                weakStressThreshold,
                severeStressGap,
                upgradeConfirmationWeeks,
                growthAtrEligibilityLimit,
                replacementScoreGap,
                holdStabilityBonus,
                rebalanceToleranceBandScale,
                sleeveTargets.PreWeakGrowthTarget,
                sleeveTargets.PreWeakDefensiveTarget,
                sleeveTargets.PreWeakRecoveryGrowthTarget,
                sleeveTargets.PreWeakRecoveryDefensiveTarget,
                sleeveTargets.WeakGrowthTarget);
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
    }
}
