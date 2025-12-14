#region imports
using System;
using System.Collections;
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
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    // 版本 V13：优化版本，引入 _minBuyAmount 和 _buyThreshold 严格控制交易频率。
    public class SingleStockSwingV13 : QCAlgorithm
    {
        // ==========================
        //    默认参数（可被 GetParameter 覆盖）
        // ==========================
        private string _ticker = "JNJ";
        private decimal _initialCash = 5000m;
        private decimal _monthlyContribution = 500m;

        private decimal _buyFraction = 0.30m;
        private int _smaLength = 240;
        private decimal _takeProfitUp = 0.10m;
        private decimal _trailingDrop = 0.03m;

        // 【V13 新增/优化参数】: 用于控制交易频率
        private decimal _minBuyAmount = 2500m; // 重新引入最小买入金额限制
        private decimal _buyThreshold = 0.04m; // 价格必须比 SMA 低 4% (0.04) 才买入

        // 硬止损参数 (4%)
        private decimal _stopLossPct = 0.04m;

        // ==========================
        //    内部状态
        // ==========================
        private Symbol _symbol;
        private SimpleMovingAverage _sma;

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
            var tickerParam = GetParameter("ticker");
            var initialParam = GetParameter("initialCash");
            var monthlyParam = GetParameter("monthlyContribution");
            var buyFractionParam = GetParameter("buyFraction");
            var smaParam = GetParameter("smaLength");
            var tpParam = GetParameter("takeProfitUp");
            var trailParam = GetParameter("trailingDrop");
            var minBuyParam = GetParameter("minBuyAmount"); // V13: 读取新参数
            var buyThreshParam = GetParameter("buyThreshold"); // V13: 读取新参数
            var slParam = GetParameter("stopLossPct");

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

            if (decimal.TryParse(minBuyParam, NumberStyles.Any, CultureInfo.InvariantCulture, out var mba)) // V13
                _minBuyAmount = mba;

            if (decimal.TryParse(buyThreshParam, NumberStyles.Any, CultureInfo.InvariantCulture, out var bt)) // V13
                _buyThreshold = bt;

            if (decimal.TryParse(slParam, NumberStyles.Any, CultureInfo.InvariantCulture, out var slp))
                _stopLossPct = slp;


            Debug($"PARAMS => ticker={_ticker}, initialCash={_initialCash}, monthly={_monthlyContribution}, " +
                  $"buyFraction={_buyFraction}, sma={_smaLength}, tpUp={_takeProfitUp}, trailDrop={_trailingDrop}, " +
                  $"minBuy={_minBuyAmount}, buyThresh={_buyThreshold:P2}, stopLoss={_stopLossPct}");

            // [其余 Initialize 逻辑不变]

            SetStartDate(2015, 1, 1);
            SetEndDate(2025, 1, 1);

            SetCash(_initialCash);

            var equity = AddEquity(_ticker, Resolution.Daily);
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);
            _symbol = equity.Symbol;

            _sma = SMA(_symbol, _smaLength, Resolution.Daily);

            _cashPool = Portfolio.Cash;

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

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            // [V12 修正的 OnOrderEvent 逻辑不变]
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            var order = Transactions.GetOrderById(orderEvent.OrderId);
            if (order == null || order.Symbol != _symbol)
            {
                return;
            }

            var absQuantity = Math.Abs(orderEvent.FillQuantity);
            var tradeValue = orderEvent.FillPrice * absQuantity;

            // 核心修复: OrderFee.Value.Amount
            var feeAmount = orderEvent.OrderFee.Value.Amount;

            if (order.Direction == OrderDirection.Buy)
            {
                var actualCost = tradeValue + feeAmount;
                _cashPool -= actualCost;

                Debug($"[{Time}] BUY Fill: Qty={absQuantity}, Price={orderEvent.FillPrice:N2}, Cost={actualCost:N2}, Fee={feeAmount:N2}, CashPool(sync)={_cashPool:N2}");
            }
            else if (order.Direction == OrderDirection.Sell)
            {
                var actualProceeds = tradeValue - feeAmount;
                _cashPool += actualProceeds;

                Debug($"[{Time}] SELL Fill: Qty={-absQuantity}, Price={orderEvent.FillPrice:N2}, Proceeds={actualProceeds:N2}, Fee={feeAmount:N2}, CashPool(sync)={_cashPool:N2}");
            }
        }

        // [OnData 和 HandleSellLogic 逻辑不变]

        public override void OnData(Slice data)
        {
            if (IsWarmingUp) return;

            // 分红同步
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
                if (price > lot.HighestPrice) lot.HighestPrice = price;

                // 检查是否达到硬性止损条件
                var stopLossPrice = lot.EntryPrice * (1 - _stopLossPct);
                if (price <= stopLossPrice)
                {
                    var qtyToSell = -lot.Quantity;
                    if (qtyToSell != 0)
                    {
                        MarketOrder(_symbol, qtyToSell);
                        Debug($"[{Time}] HARD STOP SELL: qty={qtyToSell}, entry={lot.EntryPrice:N2}, StopPrice={stopLossPrice:N2}, price={price:N2}.");
                    }
                    remove.Add(lot);
                    _soldToday = true;
                    continue;
                }

                // 检查是否达到追踪止盈启动条件
                if (!lot.TrailingActive && price >= lot.EntryPrice * (1 + _takeProfitUp))
                {
                    lot.TrailingActive = true;
                    Debug($"[{Time}] Lot hit +{_takeProfitUp:P0}, trailing activated. Entry={lot.EntryPrice:N2}, highest={lot.HighestPrice:N2}");
                }

                // 检查是否达到追踪止盈平仓条件
                if (lot.TrailingActive)
                {
                    var stopPrice = lot.HighestPrice * (1 - _trailingDrop);
                    if (price <= stopPrice)
                    {
                        var qtyToSell = -lot.Quantity;
                        if (qtyToSell != 0)
                        {
                            MarketOrder(_symbol, qtyToSell);
                            Debug($"[{Time}] Trailing stop SELL: qty={qtyToSell}, entry={lot.EntryPrice:N2}, highest={lot.HighestPrice:N2}, stop={stopPrice:N2}, price={price:N2}.");
                        }
                        remove.Add(lot);
                        _soldToday = true;
                    }
                }
            }
            foreach (var lot in remove) _lots.Remove(lot);
        }

        private void HandleBuyLogic(decimal price)
        {
            if (_boughtToday) return;
            if (_cashPool <= 0) return;

            var amountToInvest = _cashPool * _buyFraction;

            // 【V13 优化 1】: 重新引入最小买入金额限制 (控制频率)
            if (amountToInvest < _minBuyAmount)
            {
                // 可选：Debug($"[{Time}] Buy skipped: AmountToInvest={amountToInvest:N2} < MinBuyAmount={_minBuyAmount:N2}");
                return;
            }

            // 【V13 优化 2】: 价格必须比 SMA 低 _buyThreshold (控制买入质量和频率)
            var buyTriggerPrice = _sma.Current.Value * (1 - _buyThreshold);

            if (price >= buyTriggerPrice)
            {
                // 可选：Debug($"[{Time}] Buy skipped: Price={price:N2} >= TriggerPrice={buyTriggerPrice:N2}");
                return;
            }

            // --- 通过所有过滤器，执行买入 ---
            var qty = (int)(amountToInvest / price);
            if (qty <= 0) return;

            var ticket = MarketOrder(_symbol, qty);

            if (ticket != null)
            {
                Debug($"[{Time}] BUY Order Sent: qty={qty}, price={price:N2}, estimatedCost≈{qty * price:N2}.");

                // Lot EntryPrice 仍暂用当前价格
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
