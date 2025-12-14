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
    // 版本 V14: 兼容实时/回测，参数化配置，仅实时交易启用持久化，仅回测启用每月定投
    public class SingleStockSwingV14_Final_Cloud_Persistent : QCAlgorithm
    {
        // ==========================
        //    [Cloud Persistence Key]
        // ==========================
        private const string LotStateKey = "JNJ_LOT_STATE_V14";

        // ==========================================
        //    【云端参数】: 所有配置参数都通过 [Parameter] 绑定
        // ==========================================

        // 1. 资产与资金参数 (回测专用参数)
        [Parameter("ticker")]
        private string _ticker = "JNJ";

        [Parameter("initial-cash")]
        private decimal _initialCashParam = 100000m;

        [Parameter("start-date")]
        private string _startDateString = "20200101";

        [Parameter("end-date")]
        private string _endDateString = "20240101";

        [Parameter("monthly-contribution")]
        private decimal _monthlyContribution = 500m; // 每月定投金额

        // 2. 策略核心参数
        [Parameter("buy-fraction")]
        private decimal _buyFraction = 0.30m;

        [Parameter("sma-length")]
        private int _smaLength = 240;

        [Parameter("take-profit-up")]
        private decimal _takeProfitUp = 0.10m;

        [Parameter("trailing-drop")]
        private decimal _trailingDrop = 0.03m;

        [Parameter("min-buy-amount")]
        private decimal _minBuyAmount = 2500m;

        [Parameter("buy-threshold")]
        private decimal _buyThreshold = 0.04m;

        [Parameter("stop-loss-pct")]
        private decimal _stopLossPct = 0.04m;

        // ==========================
        //    Internal State
        // ==========================
        private Symbol _symbol;
        private SimpleMovingAverage _sma;

        private DateTime _lastActionDate = DateTime.MinValue;
        private bool _soldToday;
        private bool _boughtToday;

        private DateTime _lastContributionDate = DateTime.MinValue;

        private class Lot
        {
            public int Quantity { get; set; }
            public decimal EntryPrice { get; set; }
            public decimal HighestPrice { get; set; }
            public bool TrailingActive { get; set; }
        }

        private readonly List<Lot> _lots = new List<Lot>();

        public override void Initialize()
        {
            Log("[INITIALIZE] Algorithm initialization started.");

            // 1. 【环境配置】
            if (!LiveMode)
            {
                // 【回测模式】: 设置日期和资金
                Log($"[INITIALIZE] Running in Backtest Mode. Reading parameters for configuration.");
                SetCash(_initialCashParam);

                try
                {
                    var startDate = DateTime.ParseExact(_startDateString, "yyyyMMdd", CultureInfo.InvariantCulture);
                    var endDate = DateTime.ParseExact(_endDateString, "yyyyMMdd", CultureInfo.InvariantCulture);

                    SetStartDate(startDate);
                    SetEndDate(endDate);

                    Log($"[INITIALIZE] Backtest Config: StartDate={startDate:yyyy-MM-dd}, EndDate={endDate:yyyy-MM-dd}, Cash={_initialCashParam:N0}, MonthlyContr={_monthlyContribution:N0}");
                }
                catch (FormatException ex)
                {
                    Error($"[INITIALIZE] Date format error. Using hardcoded defaults. Error: {ex.Message}");
                    SetStartDate(2020, 1, 1);
                    SetEndDate(2024, 1, 1);
                }
            }
            else
            {
                // 【实时模式】: 使用经纪商资金
                Log("[INITIALIZE] Running in Live Trading Mode. Using broker cash and real-time market.");
            }

            // 2. FUNDING AND ASSET SETUP (适用于所有模式)
            var equity = AddEquity(_ticker, Resolution.Daily);
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);
            _symbol = equity.Symbol;

            // 3. 【持久化隔离】: 仅在 LiveMode 加载状态
            if (LiveMode)
            {
                LoadLotState();

                // 5. HANDLE EXISTING POSITIONS ON LIVE DEPLOYMENT (仅在 LiveMode 下执行)
                if (Portfolio[_symbol].Quantity != 0 && _lots.Count == 0)
                {
                    var currentPosition = Portfolio[_symbol];
                    _lots.Add(new Lot
                    {
                        Quantity = (int)currentPosition.Quantity,
                        EntryPrice = currentPosition.AveragePrice,
                        HighestPrice = Securities[_symbol].Price,
                        TrailingActive = false
                    });
                    Log($"[INITIALIZE] Live Startup: Detected {currentPosition.Quantity} shares of existing position. Created default Lot record using average price {currentPosition.AveragePrice:N2}.");
                }
            }

            // 4. INDICATOR WARM-UP (适用于所有模式)
            _sma = SMA(_symbol, _smaLength, Resolution.Daily);
            var history = History<TradeBar>(_symbol, _smaLength, Resolution.Daily);
            if (!history.Any())
            {
                Error($"Failed to fetch {_smaLength} data points to warm up SMA.");
            }
            foreach (var bar in history)
            {
                _sma.Update(bar.EndTime, bar.Close);
            }
            Log($"[INITIALIZE] SMA warmed up. Samples: {_sma.Samples}. Current Value: {_sma.Current.Value:N2}");
        }

        // ==========================
        //    OnEndOfAlgorithm: 仅在 LiveMode 保存状态
        // ==========================
        public override void OnEndOfAlgorithm()
        {
            if (LiveMode)
            {
                try
                {
                    ObjectStore.SaveJson(LotStateKey, _lots);
                    Log($"[PERSISTENCE] State saved successfully. Recorded {_lots.Count} Lots.");
                }
                catch (Exception ex)
                {
                    Error($"[PERSISTENCE] Failed to save Lot state: {ex.Message}");
                }
            }
        }

        // ==========================
        //    OnData: 日内逻辑入口 (包含定投逻辑)
        // ==========================
        public override void OnData(Slice data)
        {
            if (IsWarmingUp) return;

            // 【定投逻辑】: 仅在回测模式下执行
            if (!LiveMode)
            {
                HandleMonthlyContribution();
            }

            // 分红处理
            if (data.Dividends != null && data.Dividends.TryGetValue(_symbol, out var dividend))
            {
                var qty = Portfolio[_symbol].Quantity;
                if (qty != 0)
                {
                    Debug($"[{Time}] Dividend received: perShare={dividend.Distribution:N4}, qty={qty}, total={dividend.Distribution * qty:N2}.");
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

        // ==========================
        //    【修正】HandleMonthlyContribution: 回测专用定投逻辑
        // ==========================
        private void HandleMonthlyContribution()
        {
            // 确保定投金额大于零，且月份不同
            if (_monthlyContribution > 0 && Time.Date.Month != _lastContributionDate.Month)
            {
                // 【核心修正】：使用 Portfolio.CashBook.AddAmount() 代替 SetCash()
                // 这允许在运行时增加现金余额
                Portfolio.CashBook["USD"].AddAmount(_monthlyContribution);

                Log($"[CONTRIBUTION] Added {_monthlyContribution:N2} to cash pool. New cash: {Portfolio.Cash:N2}");

                _lastContributionDate = Time.Date;
            }
        }

        // ==========================
        //    Load Lot State Method (仅 LiveMode 使用)
        // ==========================
        private void LoadLotState()
        {
            if (ObjectStore.ContainsKey(LotStateKey))
            {
                try
                {
                    var savedLots = ObjectStore.ReadJson<List<Lot>>(LotStateKey);

                    if (savedLots != null)
                    {
                        _lots.AddRange(savedLots);
                        Log($"[PERSISTENCE] State loaded successfully: {_lots.Count} Lots recovered.");
                    }
                    else
                    {
                        Log($"[PERSISTENCE] Read Lot list from Object Store was null. Starting from a clean state.");
                    }
                }
                catch (Exception ex)
                {
                    Error($"[PERSISTENCE] Failed to load Lot state: {ex.Message}. Starting from a clean state.");
                }
            }
            else
            {
                Log($"[PERSISTENCE] Lot state key ({LotStateKey}) not found. Starting from a clean state.");
            }
        }

        // ==========================
        //    HandleBuyLogic: 使用参数化和 Portfolio.Cash
        // ==========================
        private void HandleBuyLogic(decimal price)
        {
            if (_boughtToday) return;

            var availableCash = Portfolio.Cash;
            if (availableCash <= 0) return;

            var amountToInvest = availableCash * _buyFraction;

            if (amountToInvest < _minBuyAmount)
            {
                return;
            }

            var buyTriggerPrice = _sma.Current.Value * (1 - _buyThreshold);

            if (price >= buyTriggerPrice)
            {
                return;
            }

            // --- Pass all filters, execute buy ---
            var qty = (int)(amountToInvest / price);
            if (qty <= 0) return;

            var ticket = MarketOrder(_symbol, qty);

            if (ticket != null)
            {
                Debug($"[{Time}] BUY Order Sent: Qty={qty}, Price={price:N2}, Estimated Cost≈{qty * price:N2}.");

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

        // ==========================
        //    HandleSellLogic: 使用参数化
        // ==========================
        private void HandleSellLogic(decimal price)
        {
            if (_lots.Count == 0) return;
            var remove = new List<Lot>();

            foreach (var lot in _lots)
            {
                if (price > lot.HighestPrice) lot.HighestPrice = price;

                // 检查硬性止损条件
                var stopLossPrice = lot.EntryPrice * (1 - _stopLossPct);
                if (price <= stopLossPrice)
                {
                    var qtyToSell = -lot.Quantity;
                    if (qtyToSell != 0)
                    {
                        MarketOrder(_symbol, qtyToSell);
                        Debug($"[{Time}] HARD STOP SELL: Qty={qtyToSell}, Entry={lot.EntryPrice:N2}, StopPrice={stopLossPrice:N2}, CurrentPrice={price:N2}.");
                    }
                    remove.Add(lot);
                    _soldToday = true;
                    continue;
                }

                // 检查跟踪止盈激活条件
                if (!lot.TrailingActive && price >= lot.EntryPrice * (1 + _takeProfitUp))
                {
                    lot.TrailingActive = true;
                    Debug($"[{Time}] Lot hit +{_takeProfitUp:P0} profit, trailing stop activated. Entry={lot.EntryPrice:N2}, HighestPrice={lot.HighestPrice:N2}");
                }

                // 检查跟踪止盈清仓条件
                if (lot.TrailingActive)
                {
                    var stopPrice = lot.HighestPrice * (1 - _trailingDrop);
                    if (price <= stopPrice)
                    {
                        var qtyToSell = -lot.Quantity;
                        if (qtyToSell != 0)
                        {
                            MarketOrder(_symbol, qtyToSell);
                            Debug($"[{Time}] Trailing STOP SELL: Qty={qtyToSell}, Entry={lot.EntryPrice:N2}, HighestPrice={lot.HighestPrice:N2}, StopPrice={stopPrice:N2}, CurrentPrice={price:N2}.");
                        }
                        remove.Add(lot);
                        _soldToday = true;
                    }
                }
            }
            foreach (var lot in remove) _lots.Remove(lot);
        }

        // OnOrderEvent 保持不变
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled) return;

            var order = Transactions.GetOrderById(orderEvent.OrderId);
            if (order == null || order.Symbol != _symbol) return;

            var absQuantity = Math.Abs(orderEvent.FillQuantity);
            var tradeValue = orderEvent.FillPrice * absQuantity;
            var feeAmount = orderEvent.OrderFee.Value.Amount;

            if (order.Direction == OrderDirection.Buy)
            {
                Debug($"[{Time}] BUY Fill: Qty={absQuantity}, Price={orderEvent.FillPrice:N2}, Cost={tradeValue + feeAmount:N2}, Fee={feeAmount:N2}.");
            }
            else if (order.Direction == OrderDirection.Sell)
            {
                Debug($"[{Time}] SELL Fill: Qty={-absQuantity}, Price={orderEvent.FillPrice:N2}, Proceeds={tradeValue - feeAmount:N2}, Fee={feeAmount:N2}.");
            }
        }
    }
}
