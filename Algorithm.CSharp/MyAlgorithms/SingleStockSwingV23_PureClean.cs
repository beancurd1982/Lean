#region imports
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Indicators;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Parameters;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    // 版本 V23: 纯净版 (No Momentum Filter)
    // 逻辑：专注均值回归，移除所有复杂过滤器，基于 50,000 初始资金进行压力测试
    public class SingleStockSwingV23_PureClean : QCAlgorithm
    {
        private string LotStateKey => $"{_ticker}_LOT_STATE_V23";

        [Parameter("ticker")] private string _ticker = "JNJ";
        [Parameter("initial-cash")] private decimal _initialCashParam = 50000m;
        [Parameter("start-date")] private string _startDateString = "20150101";
        [Parameter("end-date")] private string _endDateString = "20250101";
        [Parameter("monthly-contribution")] private decimal _monthlyContribution = 500m;
        [Parameter("max-holdings-pct")] private decimal _maxHoldingsPct = 0.90m;
        [Parameter("buy-fraction")] private decimal _buyFraction = 0.20m;
        [Parameter("sma-length")] private int _smaLength = 240;
        [Parameter("buy-threshold")] private decimal _buyThreshold = 0.06m;
        [Parameter("stop-loss-pct")] private decimal _stopLossPct = 0.04m;
        [Parameter("take-profit-up")] private decimal _takeProfitUp = 0.10m;
        [Parameter("trailing-drop")] private decimal _trailingDrop = 0.03m;
        [Parameter("min-buy-amount")] private decimal _minBuyAmount = 2500m;

        private Symbol _symbol;
        private SimpleMovingAverage _sma;
        private DateTime _lastActionDate = DateTime.MinValue;
        private bool _soldToday;
        private bool _boughtToday;
        private DateTime _lastContributionDate = DateTime.MinValue;

        private class Lot
        {
            public int Quantity; public decimal EntryPrice; public decimal HighestPrice;
            public bool TrailingActive; public bool PendingSell;
        }
        private readonly List<Lot> _lots = new List<Lot>();

        public override void Initialize()
        {
            if (!LiveMode)
            {
                SetCash(_initialCashParam);
                SetStartDate(DateTime.ParseExact(_startDateString, "yyyyMMdd", CultureInfo.InvariantCulture));
                SetEndDate(DateTime.ParseExact(_endDateString, "yyyyMMdd", CultureInfo.InvariantCulture));
            }
            var equity = AddEquity(_ticker, Resolution.Daily);
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);
            _symbol = equity.Symbol;
            _sma = SMA(_symbol, _smaLength, Resolution.Daily);

            var history = History<TradeBar>(_symbol, _smaLength, Resolution.Daily);
            foreach (var bar in history) _sma.Update(bar.EndTime, bar.Close);
        }

        public override void OnData(Slice data)
        {
            if (IsWarmingUp) return;
            if (!LiveMode) HandleMonthlyContribution();
            if (data.Bars == null || !data.Bars.TryGetValue(_symbol, out TradeBar bar)) return;

            var price = bar.Close;
            if (Time.Date != _lastActionDate.Date)
            {
                _lastActionDate = Time.Date; _soldToday = false; _boughtToday = false;
            }

            if (!_sma.IsReady) return;

            HandleSellLogic(price);
            if (!_soldToday) HandleBuyLogic(price);
        }

        private void HandleBuyLogic(decimal price)
        {
            if (_boughtToday) return;
            if (Portfolio[_symbol].HoldingsValue >= Portfolio.TotalPortfolioValue * _maxHoldingsPct) return;

            // 资金管理：每次只动用可用现金的 buy-fraction
            var amountToInvest = Portfolio.Cash * _buyFraction;
            if (amountToInvest < _minBuyAmount) return;

            var buyTriggerPrice = _sma.Current.Value * (1 - _buyThreshold);

            if (price < buyTriggerPrice)
            {
                var qty = (int)(amountToInvest / price);
                if (qty > 0)
                {
                    MarketOrder(_symbol, qty);
                    _boughtToday = true;
                }
            }
        }

        private void HandleSellLogic(decimal price)
        {
            foreach (var lot in _lots.Where(l => !l.PendingSell).ToList())
            {
                if (price > lot.HighestPrice) lot.HighestPrice = price;
                bool sell = (price <= lot.EntryPrice * (1 - _stopLossPct));
                if (!lot.TrailingActive && price >= lot.EntryPrice * (1 + _takeProfitUp)) lot.TrailingActive = true;
                if (lot.TrailingActive && price <= lot.HighestPrice * (1 - _trailingDrop)) sell = true;

                if (sell && lot.Quantity > 0)
                {
                    MarketOrder(_symbol, -lot.Quantity);
                    lot.PendingSell = true;
                    _soldToday = true;
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled) return;
            var order = Transactions.GetOrderById(orderEvent.OrderId);
            if (order.Direction == OrderDirection.Buy)
            {
                _lots.Add(new Lot { Quantity = (int)orderEvent.FillQuantity, EntryPrice = orderEvent.FillPrice, HighestPrice = orderEvent.FillPrice });
            }
            else
            {
                var qtyToRem = (int)Math.Abs(orderEvent.FillQuantity);
                foreach (var lot in _lots.OrderBy(l => l.EntryPrice).ToList())
                {
                    if (qtyToRem <= 0) break;
                    var take = Math.Min(lot.Quantity, qtyToRem);
                    lot.Quantity -= take; qtyToRem -= take;
                }
                _lots.RemoveAll(l => l.Quantity <= 0);
            }
        }

        private void HandleMonthlyContribution()
        {
            if (_monthlyContribution > 0 && Time.Date.Month != _lastContributionDate.Month)
            {
                Portfolio.CashBook["USD"].AddAmount(_monthlyContribution);
                _lastContributionDate = Time.Date;
            }
        }
    }
}
