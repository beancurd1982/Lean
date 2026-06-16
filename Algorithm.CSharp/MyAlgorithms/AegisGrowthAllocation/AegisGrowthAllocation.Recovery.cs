#region imports
using System;
using QuantConnect.Algorithm;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    public partial class AegisGrowthAllocation : QCAlgorithm
    {
        private void UpdateDefensiveOverrideHighWaterMark(decimal totalPortfolioValue)
        {
            if (totalPortfolioValue > _defensiveOverrideEquityHighWaterMark)
            {
                _defensiveOverrideEquityHighWaterMark = totalPortfolioValue;
            }
        }

        private decimal CalculateDrawdownFromHigh(decimal totalPortfolioValue)
        {
            if (_defensiveOverrideEquityHighWaterMark <= 0m)
            {
                return 0m;
            }

            return Math.Max(0m, (_defensiveOverrideEquityHighWaterMark - totalPortfolioValue) / _defensiveOverrideEquityHighWaterMark);
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

        private bool UpdatePreWeakRecoveryState(
            RegimeSnapshot regimeSnapshot,
            bool normalPreWeakActive,
            decimal drawdownFromHigh)
        {
            if (!_preWeakRecoveryEnabled || !normalPreWeakActive)
            {
                ResetPreWeakRecoveryState(normalPreWeakActive ? "disabled" : "preweak-inactive");
                _preWeakRecoveryPreviousPreWeakActive = normalPreWeakActive;
                return false;
            }

            if (!_preWeakRecoveryPreviousPreWeakActive)
            {
                _preWeakRecoverySegmentId++;
                _preWeakRecoveryLocalTroughDrawdown = Math.Max(0m, drawdownFromHigh);
                _preWeakRecoveryConfirmationWeeks = 0;
                _preWeakRecoveryActive = false;
                _preWeakRecoveryLastResetReason = "segment-start";
            }

            _preWeakRecoveryPreviousPreWeakActive = true;

            if (regimeSnapshot.ActiveRegime == RiskRegime.Weak ||
                regimeSnapshot.TrendState == SignalState.Weak ||
                regimeSnapshot.BreadthState == SignalState.Weak ||
                regimeSnapshot.StressState == SignalState.Weak)
            {
                ResetPreWeakRecoveryState("weak-signal");
                _preWeakRecoveryPreviousPreWeakActive = true;
                return false;
            }

            if (drawdownFromHigh > _preWeakRecoveryLocalTroughDrawdown + 0.000001m)
            {
                _preWeakRecoveryLocalTroughDrawdown = drawdownFromHigh;
                _preWeakRecoveryConfirmationWeeks = 0;
                _preWeakRecoveryActive = false;
                _preWeakRecoveryLastResetReason = "new-trough";
                return false;
            }

            var recoveredDrawdown = _preWeakRecoveryLocalTroughDrawdown - drawdownFromHigh;
            if (drawdownFromHigh > _preWeakRecoveryMaxDrawdown ||
                recoveredDrawdown < _preWeakRecoveryDrawdownImprovement)
            {
                _preWeakRecoveryConfirmationWeeks = 0;
                _preWeakRecoveryActive = false;
                _preWeakRecoveryLastResetReason = drawdownFromHigh > _preWeakRecoveryMaxDrawdown
                    ? "drawdown-too-high"
                    : "insufficient-recovery";
                return false;
            }

            _preWeakRecoveryConfirmationWeeks++;
            _preWeakRecoveryLastResetReason = "eligible";
            if (_preWeakRecoveryConfirmationWeeks >= _preWeakRecoveryConfirmationWeeksRequired)
            {
                _preWeakRecoveryActive = true;
            }

            return _preWeakRecoveryActive;
        }

        private void ResetPreWeakRecoveryState(string reason)
        {
            _preWeakRecoveryConfirmationWeeks = 0;
            _preWeakRecoveryActive = false;
            _preWeakRecoveryLocalTroughDrawdown = 0m;
            _preWeakRecoveryLastResetReason = reason;
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
            if (!_severeCrashModeActive)
            {
                _severeCrashRecoveryWeeks = 0;
                if (!ShouldApplySevereCrashOverride(regimeSnapshot, totalPortfolioValue))
                {
                    return false;
                }

                _severeCrashModeActive = true;
                _severeCrashModeState = "enter";
                _severeCrashExitReason = "none";
                return true;
            }

            if (ShouldApplySevereCrashOverride(regimeSnapshot, totalPortfolioValue))
            {
                _severeCrashRecoveryWeeks = 0;
                _severeCrashModeState = "hold";
                _severeCrashExitReason = "none";
                return true;
            }

            var currentDrawdown = CalculateDrawdownFromHigh(totalPortfolioValue);
            var recoveredEnough = currentDrawdown <= _severeCrashOverrideExitDrawdownThreshold;
            var regimeRecovered = IsSevereCrashRecoveredRegime(regimeSnapshot.ActiveRegime);
            if (recoveredEnough && regimeRecovered)
            {
                _severeCrashRecoveryWeeks++;
                _severeCrashModeState = "recovering";
                if (_severeCrashRecoveryWeeks >= _severeCrashOverrideRecoveryConfirmationWeeks)
                {
                    _severeCrashModeActive = false;
                    _severeCrashRecoveryWeeks = 0;
                    _severeCrashModeState = "exit";
                    _severeCrashExitReason = "confirmed-recovery";
                    return false;
                }

                return true;
            }

            _severeCrashRecoveryWeeks = 0;
            _severeCrashModeState = "hold";
            _severeCrashExitReason = recoveredEnough ? "regime-not-recovered" : "drawdown-not-recovered";
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
    }
}
