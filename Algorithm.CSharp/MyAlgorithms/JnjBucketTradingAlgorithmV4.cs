#region imports
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Drawing;
using QuantConnect;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Selection;
using QuantConnect.Api;
using QuantConnect.Parameters;
using QuantConnect.Benchmarks;
using QuantConnect.Brokerages;
using QuantConnect.Commands;
using QuantConnect.Configuration;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Algorithm;
using QuantConnect.Indicators;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Custom.IconicTypes;
using QuantConnect.DataSource;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Data.Shortable;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Notifications;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.OptionExercise;
using QuantConnect.Orders.Slippage;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Python;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Positions;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Securities.IndexOption;
using QuantConnect.Securities.Interfaces;
using QuantConnect.Securities.Volatility;
using QuantConnect.Storage;
using QuantConnect.Statistics;
using QCAlgorithmFramework = QuantConnect.Algorithm.QCAlgorithm;
using QCAlgorithmFrameworkBridge = QuantConnect.Algorithm.QCAlgorithm;
using Calendar = QuantConnect.Data.Consolidators.Calendar;
#endregion

namespace QuantConnect.Algorithm.CSharp.MyAlgorithms
{
    public class JnjBucketTradingAlgorithmV4 : QCAlgorithm
    {
        private Symbol _jnj;
        private SimpleMovingAverage _sma120;

        // Internal cash pool (separate from Portfolio.Cash)
        private decimal _cashPool = 0m;

        // 每次买入使用资金池的 30%
        private const decimal BuyFractionOfCashPool = 0.3m;

        // Monthly contribution to the cash pool
        private const decimal MonthlyContribution = 500m;

        // Start date for monthly contributions (to avoid warmup months)
        private static readonly DateTime MonthlyContributionStartDate = new DateTime(2020, 1, 1);

        // Per-day flags
        private DateTime _lastActionDate = DateTime.MinValue;
        private bool _soldToday = false;
        private bool _boughtToday = false;

        private class Lot
        {
            public int BuyOrderId;
            public DateTime EntryTime;
            public decimal EntryPrice;
            public decimal Quantity;
            public decimal RemainingQuantity;

            public bool Level1Hit;        // price >= entry * 1.05
            public bool Level2Hit;        // price >= entry * 1.10
            public bool TrailingActive;   // trailing stop activated

            public decimal HighestPriceAfterLevel2; // for trailing stop
        }

        private readonly List<Lot> _lots = new List<Lot>();
        private readonly Dictionary<int, Lot> _pendingBuyLots = new Dictionary<int, Lot>();
        private readonly HashSet<int> _pendingSellOrderIds = new HashSet<int>();

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2025, 10, 1);
            SetCash(5000);

            var equity = AddEquity("JNJ", Resolution.Daily);
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);
            _jnj = equity.Symbol;

            _sma120 = SMA(_jnj, 120, Resolution.Daily);

            _cashPool = Portfolio.Cash;

            // Monthly contribution
            Schedule.On(
                DateRules.MonthStart(_jnj),
                TimeRules.AfterMarketOpen(_jnj, 1),
                () =>
                {
                    if (Time.Date >= MonthlyContributionStartDate)
                    {
                        Portfolio.CashBook["USD"].AddAmount(MonthlyContribution);
                        _cashPool += MonthlyContribution;
                        Debug("[" + Time + "] Monthly contribution +" + MonthlyContribution + ", CashPool=" + _cashPool);
                    }
                });

            SetWarmup(120, Resolution.Daily);
        }

        public override void OnData(Slice data)
        {
            if (IsWarmingUp) return;

            // 1) Dividends -> add to cash pool
            Dividend dividend;
            if (data.Dividends != null && data.Dividends.TryGetValue(_jnj, out dividend))
            {
                var quantity = Portfolio[_jnj].Quantity;
                if (quantity != 0)
                {
                    var totalDividend = dividend.Distribution * quantity;
                    _cashPool += totalDividend;

                    Debug("[" + Time + "] Dividend received: perShare=" + dividend.Distribution +
                          ", qty=" + quantity + ", total=" + totalDividend +
                          ", CashPool(after)=" + _cashPool);
                }
            }

            // 2) Get current bar
            TradeBar bar;
            if (data.Bars == null || !data.Bars.TryGetValue(_jnj, out bar))
                return;

            var price = bar.Close;

            // Reset daily flags
            if (Time.Date != _lastActionDate.Date)
            {
                _lastActionDate = Time.Date;
                _soldToday = false;
                _boughtToday = false;
            }

            if (!_sma120.IsReady)
                return;

            // 3) Sell logic first (priority)
            HandleSellLogic(price);

            if (_soldToday)
                return;

            // 4) Buy logic
            HandleBuyLogic(price);
        }

        private void HandleSellLogic(decimal price)
        {
            if (_lots.Count == 0) return;

            var removeList = new List<Lot>();

            foreach (var lot in _lots)
            {
                if (lot.RemainingQuantity <= 0)
                {
                    removeList.Add(lot);
                    continue;
                }

                var level1 = lot.EntryPrice * 1.05m;
                var level2 = lot.EntryPrice * 1.10m;

                // 标记达到 5% 和 10%
                if (!lot.Level1Hit && price >= level1)
                {
                    lot.Level1Hit = true;
                }

                if (!lot.Level2Hit && price >= level2)
                {
                    lot.Level2Hit = true;
                    lot.TrailingActive = true;
                    lot.HighestPriceAfterLevel2 = price;

                    Debug("[" + Time + "] Lot hit 10% gain, trailing stop activated. Entry=" +
                          lot.EntryPrice + " price=" + price);
                }

                // Trailing stop: once Level2Hit, track highest price, stop at 3% below
                if (lot.TrailingActive)
                {
                    if (price > lot.HighestPriceAfterLevel2)
                    {
                        lot.HighestPriceAfterLevel2 = price;
                    }

                    var trailingStop = lot.HighestPriceAfterLevel2 * 0.97m;

                    if (price < trailingStop)
                    {
                        var qty = (int)lot.RemainingQuantity;
                        if (qty > 0)
                        {
                            var ticket = MarketOrder(_jnj, -qty);
                            if (ticket != null && ticket.OrderId > 0)
                            {
                                _pendingSellOrderIds.Add(ticket.OrderId);
                                _soldToday = true;

                                Debug("[" + Time + "] Trailing stop triggered: qty=" + qty +
                                      ", entry=" + lot.EntryPrice + ", highest=" +
                                      lot.HighestPriceAfterLevel2 + ", stop=" +
                                      trailingStop + ", price=" + price +
                                      ", orderId=" + ticket.OrderId);
                            }

                            lot.RemainingQuantity = 0;
                            removeList.Add(lot);
                        }
                    }
                }
            }

            foreach (var lot in removeList)
            {
                _lots.Remove(lot);
            }
        }

        private void HandleBuyLogic(decimal price)
        {
            // 资金池太小，不买
            if (_cashPool <= 0) return;

            // 一天只买一次
            if (_boughtToday) return;

            // 必须在 SMA120 下方才考虑买
            if (price >= _sma120.Current.Value)
                return;

            // 本次买入金额 = 资金池的 30%
            var amountToInvest = _cashPool * BuyFractionOfCashPool;
            if (amountToInvest < 100m) // 太少就不买，避免无意义小单
                return;

            var qty = (int)(amountToInvest / price);
            if (qty <= 0) return;

            var ticket = MarketOrder(_jnj, qty);

            if (ticket != null && ticket.OrderId > 0)
            {
                var lot = new Lot
                {
                    BuyOrderId = ticket.OrderId,
                    Quantity = qty,
                    RemainingQuantity = qty,
                    Level1Hit = false,
                    Level2Hit = false,
                    TrailingActive = false,
                    HighestPriceAfterLevel2 = 0m
                };

                _pendingBuyLots[ticket.OrderId] = lot;
                _boughtToday = true;

                Debug("[" + Time + "] Buy signal: qty=" + qty + ", price=" + price +
                      ", invest=" + amountToInvest + ", CashPool(before)=" + _cashPool +
                      ", orderId=" + ticket.OrderId);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent == null) return;
            if (orderEvent.Status != OrderStatus.Filled) return;
            if (orderEvent.Symbol != _jnj) return;

            var qty = orderEvent.FillQuantity;
            var price = orderEvent.FillPrice;
            var value = qty * price;

            if (orderEvent.Direction == OrderDirection.Buy)
            {
                Lot lot;
                if (_pendingBuyLots.TryGetValue(orderEvent.OrderId, out lot))
                {
                    lot.EntryTime = orderEvent.UtcTime;
                    lot.EntryPrice = price;

                    _lots.Add(lot);
                    _pendingBuyLots.Remove(orderEvent.OrderId);

                    _cashPool -= value;

                    Debug("[" + Time + "] Buy filled: qty=" + qty + ", price=" + price +
                          ", cost=" + value + ", CashPool(after)=" + _cashPool);
                }
            }
            else if (orderEvent.Direction == OrderDirection.Sell)
            {
                if (_pendingSellOrderIds.Contains(orderEvent.OrderId))
                {
                    _pendingSellOrderIds.Remove(orderEvent.OrderId);

                    _cashPool += Math.Abs(value);

                    Debug("[" + Time + "] Sell filled: qty=" + qty + ", price=" + price +
                          ", proceeds=" + Math.Abs(value) + ", CashPool(after)=" + _cashPool);
                }
            }
        }
    }
}
