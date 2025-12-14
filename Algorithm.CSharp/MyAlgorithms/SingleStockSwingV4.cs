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

namespace QuantConnect.Algorithm.CSharp
{
    public class SingleStockSwingV4 : QCAlgorithm
    {
        // ==========================
        //   默认参数（可被 GetParameter 覆盖）
        // ==========================

        private string _ticker = "JNJ";   // 股票代码
        private decimal _initialCash = 5000m;   // 初始资金
        private decimal _monthlyContribution = 500m;    // 每月追加资金

        private decimal _buyFraction = 0.30m;         // 每次买入资金池比例
        private int _smaLength = 120;           // SMA 长度
        private decimal _takeProfitUp = 0.10m;         // +10% 启动 trailing
        private decimal _trailingDrop = 0.03m;         // 从最高点回撤 3% 止盈
        private decimal _minBuyAmount = 1500m;         // 最小买入金额（避免碎单）

        // ==========================
        //   内部状态
        // ==========================

        private Symbol _symbol;
        private SimpleMovingAverage _sma;

        // 逻辑上的资金池（包含初始 + 每月定投 + 分红 + 卖出）
        private decimal _cashPool;

        private DateTime _lastActionDate = DateTime.MinValue;
        private bool _soldToday;
        private bool _boughtToday;

        private class Lot
        {
            public int Quantity;
            public decimal EntryPrice;
            public decimal HighestPrice;
            public bool TrailingActive;
        }

        private readonly List<Lot> _lots = new List<Lot>();

        public override void Initialize()
        {
            // ========= 读取优化参数（如果有） =========
            // 所有参数都是可选的：没传就用默认值

            var tickerParam = GetParameter("ticker");
            var initialParam = GetParameter("initialCash");
            var monthlyParam = GetParameter("monthlyContribution");
            var buyFractionParam = GetParameter("buyFraction");
            var smaParam = GetParameter("smaLength");
            var tpParam = GetParameter("takeProfitUp");
            var trailParam = GetParameter("trailingDrop");
            var minBuyParam = GetParameter("minBuyAmount");

            if (!string.IsNullOrWhiteSpace(tickerParam))
                _ticker = tickerParam;

            if (decimal.TryParse(initialParam, NumberStyles.Any, CultureInfo.InvariantCulture, out var ic))
                _initialCash = ic;

            if (decimal.TryParse(monthlyParam, NumberStyles.Any, CultureInfo.InvariantCulture, out var mc))
                _monthlyContribution = mc;

            if (decimal.TryParse(buyFractionParam, NumberStyles.Any, CultureInfo.InvariantCulture, out var bf))
                _buyFraction = bf;

            if (int.TryParse(smaParam, NumberStyles.Any, CultureInfo.InvariantCulture, out var sl))
                _smaLength = sl;

            if (decimal.TryParse(tpParam, NumberStyles.Any, CultureInfo.InvariantCulture, out var tp))
                _takeProfitUp = tp;

            if (decimal.TryParse(trailParam, NumberStyles.Any, CultureInfo.InvariantCulture, out var tr))
                _trailingDrop = tr;

            if (decimal.TryParse(minBuyParam, NumberStyles.Any, CultureInfo.InvariantCulture, out var mba))
                _minBuyAmount = mba;

            // 打个标记，方便确认参数生效
            Debug($"PARAMS => ticker={_ticker}, initialCash={_initialCash}, monthly={_monthlyContribution}, " +
         $"buyFraction={_buyFraction}, sma={_smaLength}, tpUp={_takeProfitUp}, trailDrop={_trailingDrop}, minBuy={_minBuyAmount}");

            // ========= 正常 Initialize 逻辑 =========

            SetStartDate(2015, 1, 1);
            SetEndDate(2025, 1, 1);

            SetCash(_initialCash);

            var equity = AddEquity(_ticker, Resolution.Daily);
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);   // 为了拿到分红
            _symbol = equity.Symbol;

            _sma = SMA(_symbol, _smaLength, Resolution.Daily);

            _cashPool = Portfolio.Cash;

            // 每月第一交易日追加资金到账户 + 资金池
            Schedule.On(
        DateRules.MonthStart(_symbol),
        TimeRules.AfterMarketOpen(_symbol, 1),
        () =>
        {
            Portfolio.CashBook["USD"].AddAmount(_monthlyContribution);
            _cashPool += _monthlyContribution;
            Debug($"[{Time}] Monthly contribution +{_monthlyContribution}, CashPool={_cashPool}");
        });

            SetWarmup(_smaLength, Resolution.Daily);
        }

        public override void OnData(Slice data)
        {
            if (IsWarmingUp) return;

            // 分红：同步到资金池
            if (data.Dividends != null && data.Dividends.TryGetValue(_symbol, out var dividend))
            {
                var qty = Portfolio[_symbol].Quantity;
                if (qty != 0)
                {
                    var totalDividend = dividend.Distribution * qty;
                    _cashPool += totalDividend;

                    Debug($"[{Time}] Dividend received: perShare={dividend.Distribution}, qty={qty}, total={totalDividend}, CashPool(after)={_cashPool}");
                }
            }

            if (data.Bars == null || !data.Bars.TryGetValue(_symbol, out TradeBar bar))
                return;

            var price = bar.Close;

            if (Time.Date != _lastActionDate.Date)
            {
                _lastActionDate = Time.Date;
                _soldToday = false;
                _boughtToday = false;
            }

            if (!_sma.IsReady)
                return;

            HandleSellLogic(price);

            if (_soldToday)
                return;

            HandleBuyLogic(price);
        }

        private void HandleSellLogic(decimal price)
        {
            if (_lots.Count == 0) return;

            var remove = new List<Lot>();

            foreach (var lot in _lots)
            {
                if (price > lot.HighestPrice)
                    lot.HighestPrice = price;

                if (!lot.TrailingActive && price >= lot.EntryPrice * (1 + _takeProfitUp))
                {
                    lot.TrailingActive = true;
                    Debug($"[{Time}] Lot hit +{_takeProfitUp:P0}, trailing activated. Entry={lot.EntryPrice}, highest={lot.HighestPrice}");
                }

                if (lot.TrailingActive)
                {
                    var stopPrice = lot.HighestPrice * (1 - _trailingDrop);

                    if (price <= stopPrice)
                    {
                        var qtyToSell = -lot.Quantity;
                        if (qtyToSell != 0)
                        {
                            var ticket = MarketOrder(_symbol, qtyToSell);
                            if (ticket != null)
                            {
                                var proceedsEstimate = lot.Quantity * price;
                                _cashPool += proceedsEstimate;

                                Debug($"[{Time}] Trailing stop SELL: qty={qtyToSell}, entry={lot.EntryPrice}, highest={lot.HighestPrice}, stop={stopPrice}, price={price}, CashPool(approx)={_cashPool}");
                            }
                        }

                        remove.Add(lot);
                        _soldToday = true;
                    }
                }
            }

            foreach (var lot in remove)
                _lots.Remove(lot);
        }

        private void HandleBuyLogic(decimal price)
        {
            if (_boughtToday) return;
            if (_cashPool <= 0) return;

            // 只在价格低于 SMA 时买入
            if (price >= _sma.Current.Value)
                return;

            var amountToInvest = _cashPool * _buyFraction;
            if (amountToInvest < _minBuyAmount)
                return;

            var qty = (int)(amountToInvest / price);
            if (qty <= 0) return;

            var ticket = MarketOrder(_symbol, qty);

            if (ticket != null)
            {
                var estimatedCost = qty * price;
                _cashPool -= estimatedCost;

                Debug($"[{Time}] BUY: qty={qty}, price={price}, cost≈{estimatedCost}, CashPool(after)≈{_cashPool}, orderId={ticket.OrderId}");

                _lots.Add(new Lot
                {
                    Quantity = qty,
                    EntryPrice = price,
                    HighestPrice = price,
                    TrailingActive = false
                });

                _boughtToday = true;
            }
        }
    }
}
