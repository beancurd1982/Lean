#region imports
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Indicators; // 仍需保留，但不再使用
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    // 版本 Benchmark：单股票定投基准策略 (Buy-and-Hold with Monthly Contribution)
    public class SingleStockSwingBenchmark : QCAlgorithm
    {
        // ==========================
        //    默认参数
        // ==========================
        private string _ticker = "JNJ";
        private decimal _initialCash = 5000m;
        private decimal _monthlyContribution = 500m;

        // ==========================
        //    内部状态
        // ==========================
        private Symbol _symbol;
        private decimal _cashPool;
        private bool _boughtToday;

        public override void Initialize()
        {
            // ========= 读取参数（仅保留基准必需参数） =========
            var tickerParam = GetParameter("ticker");
            var initialParam = GetParameter("initialCash");
            var monthlyParam = GetParameter("monthlyContribution");

            if (!string.IsNullOrWhiteSpace(tickerParam))
                _ticker = tickerParam;

            if (decimal.TryParse(initialParam, NumberStyles.Any, CultureInfo.InvariantCulture, out var ic))
                _initialCash = ic;

            if (decimal.TryParse(monthlyParam, NumberStyles.Any, CultureInfo.InvariantCulture, out var mc))
                _monthlyContribution = mc;

            Debug($"BENCHMARK PARAMS => ticker={_ticker}, initialCash={_initialCash}, monthly={_monthlyContribution}");

            SetStartDate(2015, 1, 1);
            SetEndDate(2025, 1, 1);
            SetCash(_initialCash);

            var equity = AddEquity(_ticker, Resolution.Daily);
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);
            _symbol = equity.Symbol;

            _cashPool = Portfolio.Cash;

            // 1. 设置月度定投计划
            Schedule.On(
                DateRules.MonthStart(_symbol),
                TimeRules.AfterMarketOpen(_symbol, 1),
                () =>
                {
                    Portfolio.CashBook["USD"].AddAmount(_monthlyContribution);
                    _cashPool += _monthlyContribution;
                    Debug($"[{Time}] Monthly contribution +{_monthlyContribution}, CashPool={_cashPool}");

                    // 触发买入（定投后立即买入）
                    _boughtToday = false;
                });

            SetWarmup(1, Resolution.Daily); // 只需要1天预热，确保数据可用
        }

        // 2. 简化 OnOrderEvent，只更新 CashPool
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
                return;

            // 仅处理买入订单（Benchmark不应该有卖出）
            if (orderEvent.Direction == OrderDirection.Buy)
            {
                var absQuantity = Math.Abs(orderEvent.FillQuantity);
                var tradeValue = orderEvent.FillPrice * absQuantity;
                var feeAmount = orderEvent.OrderFee.Value.Amount;
                var actualCost = tradeValue + feeAmount;

                // 更新现金池
                _cashPool -= actualCost;

                Debug($"[{Time}] BUY Fill: Qty={absQuantity}, Price={orderEvent.FillPrice:N2}, Cost={actualCost:N2}, Fee={feeAmount:N2}, CashPool(sync)={_cashPool:N2}");
            }
        }

        // 3. 简化 OnData 逻辑
        public override void OnData(Slice data)
        {
            if (IsWarmingUp) return;

            // 分红同步 (重要：定投策略通常会再投资分红，但为简化模型，仅加入现金池)
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

            // 每日检查是否需要买入（特别是初始资金或定投刚完成后）
            HandleBuyLogic(bar.Close);
        }

        private void HandleBuyLogic(decimal price)
        {
            if (_boughtToday) return;

            // 每次买入时，投入所有可用现金（CashPool）
            var amountToInvest = _cashPool;

            // 仅在 CashPool 有足够金额进行一次有意义的交易时才进行
            if (amountToInvest < price) // 至少能买一股
                return;

            // --- 执行买入：投入所有现金 ---
            var qty = (int)(amountToInvest / price);
            if (qty <= 0) return;

            var ticket = MarketOrder(_symbol, qty);

            if (ticket != null)
            {
                Debug($"[{Time}] BENCHMARK BUY Sent: qty={qty}, price={price:N2}, estimatedCost≈{qty * price:N2}.");
                _boughtToday = true;
            }
        }
    }
}
