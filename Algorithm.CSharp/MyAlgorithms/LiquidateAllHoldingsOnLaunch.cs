using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Liquidates all currently held equity positions when the algorithm starts running.
    /// This algorithm is intended for manual launch when the account should be flattened.
    /// </summary>
    public class LiquidateAllHoldingsOnLaunch : QCAlgorithm
    {
        private static readonly TimeSpan LiveHoldingsSyncGracePeriod = TimeSpan.FromMinutes(5);

        private bool _liquidationSubmitted;
        private bool _completed;
        private bool _waitingForLiveSyncLogged;
        private DateTime? _firstHeartbeatTime;
        private HashSet<Symbol> _targetSymbols = new HashSet<Symbol>();

        public override void Initialize()
        {
            SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);

            SetStartDate(2025, 1, 1);
            SetEndDate(2025, 1, 31);
            SetCash(100000);

            Settings.LiquidateEnabled = true;

            // Use a liquid U.S. equity subscription to receive a reliable live/backtest heartbeat.
            AddEquity("SPY", Resolution.Minute);

            Debug("Launch-liquidation algorithm initialized. Waiting for first data event.");
        }

        public override void OnData(Slice slice)
        {
            if (_completed)
            {
                return;
            }

            _firstHeartbeatTime ??= Time;
            EnsureEquityHoldingsAreSubscribed();

            if (!_liquidationSubmitted)
            {
                SubmitLiquidation();
                return;
            }

            if (IsFlatAndNoOpenOrders())
            {
                _completed = true;
                Debug("Launch-liquidation algorithm confirmed all target equity holdings are flat. Stopping algorithm.");
                Quit();
            }
        }

        private void EnsureEquityHoldingsAreSubscribed()
        {
            foreach (var holding in Portfolio.Values.Where(x => x.Invested && x.Symbol.SecurityType == SecurityType.Equity))
            {
                _targetSymbols.Add(holding.Symbol);

                if (!Securities.ContainsKey(holding.Symbol))
                {
                    AddEquity(holding.Symbol.Value, Resolution.Minute);
                    Debug($"Added subscription for existing holding {holding.Symbol.Value} so it can be liquidated.");
                }
            }
        }

        private void SubmitLiquidation()
        {
            var investedEquities = Portfolio.Values
                .Where(x => x.Invested && x.Symbol.SecurityType == SecurityType.Equity)
                .Select(x => x.Symbol)
                .Distinct()
                .ToList();

            if (investedEquities.Count == 0)
            {
                if (LiveMode && _firstHeartbeatTime.HasValue && Time - _firstHeartbeatTime.Value < LiveHoldingsSyncGracePeriod)
                {
                    if (!_waitingForLiveSyncLogged)
                    {
                        Debug($"No invested equities detected yet. Waiting up to {LiveHoldingsSyncGracePeriod.TotalMinutes:N0} minutes for live holdings synchronization before stopping.");
                        _waitingForLiveSyncLogged = true;
                    }

                    return;
                }

                _completed = true;
                Debug("No invested equity holdings were found at launch. Stopping algorithm.");
                Quit();
                return;
            }

            _waitingForLiveSyncLogged = false;
            _targetSymbols = investedEquities.ToHashSet();

            foreach (var symbol in _targetSymbols)
            {
                Transactions.CancelOpenOrders(symbol, "Launch liquidation");
            }

            var tickets = Liquidate(_targetSymbols, asynchronous: true, tag: "Launch liquidation");
            _liquidationSubmitted = true;

            Debug($"Submitted liquidation for {tickets.Count} symbol(s): {string.Join(", ", _targetSymbols.Select(x => x.Value).OrderBy(x => x))}");
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"[ORDER] {Time} Symbol={orderEvent.Symbol} Status={orderEvent.Status} FillQty={orderEvent.FillQuantity} FillPrice={orderEvent.FillPrice}");
        }

        private bool IsFlatAndNoOpenOrders()
        {
            foreach (var symbol in _targetSymbols)
            {
                if (Portfolio[symbol].Invested)
                {
                    return false;
                }

                if (Transactions.GetOpenOrders(symbol).Count > 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
