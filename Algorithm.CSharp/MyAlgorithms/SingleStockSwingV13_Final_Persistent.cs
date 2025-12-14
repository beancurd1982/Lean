#region imports
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;          // [New] Used for file operations
using Newtonsoft.Json;    // [New] Used for JSON serialization
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
    // Version V13_Final: JNJ Portfolio D Optimal Parameters, includes [Persistent State]
    public class SingleStockSwingV13_Final_Persistent : QCAlgorithm
    {
        // ==========================
        //    [Portfolio D Optimal Parameters] (Remains unchanged)
        // ==========================
        private string _ticker = "JNJ";
        // In live trading, these parameters are no longer used for actual calculation but are retained for parameters like BuyFraction
        private decimal _initialCash = 5000m;
        private decimal _monthlyContribution = 500m;

        private decimal _buyFraction = 0.30m;
        private int _smaLength = 240;
        private decimal _takeProfitUp = 0.10m;
        private decimal _trailingDrop = 0.03m;

        private decimal _minBuyAmount = 2500m;
        private decimal _buyThreshold = 0.04m;
        private decimal _stopLossPct = 0.04m;

        // ==========================
        //    Internal State
        // ==========================
        private Symbol _symbol;
        private SimpleMovingAverage _sma;

        // [Removed]: _cashPool, as live trading will rely on Portfolio.Cash
        private DateTime _lastActionDate = DateTime.MinValue;
        private bool _soldToday;
        private bool _boughtToday;

        // [Lot Class Definition Remains Unchanged]: Stores information for each purchase
        private class Lot
        {
            public int Quantity { get; set; }
            public decimal EntryPrice { get; set; }
            public decimal HighestPrice { get; set; }
            public bool TrailingActive { get; set; }
        }

        private readonly List<Lot> _lots = new List<Lot>();
        private string _lotStateFilePath; // [New] File path variable

        public override void Initialize()
        {
            // Initialize file path: The state file will be stored in the directory where the LEAN Launcher is located
            _lotStateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{_ticker}_lots_state.json");

            // 1. [New] Load existing Lot state
            LoadLotState();

            // ... (GetParameter logic remains the same, ensuring backtesting platform parameters can override defaults) ...
            // ... (The parameter reading and Debug logic here is consistent with the original algorithm) ...

            // An initial cash amount must be set, but it will be overridden by the IB account balance in live trading
            SetCash(100000);

            var equity = AddEquity(_ticker, Resolution.Daily);
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);
            _symbol = equity.Symbol;

            // [Adjustment] Remove SetStartDate, SetEndDate, SetWarmup
            // Indicators need to be manually warmed up in live trading

            // Manually warm up the SMA indicator
            _sma = SMA(_symbol, _smaLength, Resolution.Daily);
            var history = History<TradeBar>(_symbol, _smaLength, Resolution.Daily);
            if (!history.Any())
            {
                Error($"Failed to fetch {_smaLength} data points to warm up SMA, please check data source or IB connection.");
            }
            foreach (var bar in history)
            {
                _sma.Update(bar.EndTime, bar.Close);
            }
            Debug($"SMA is warmed up with {_sma.Samples} samples. Value: {_sma.Current.Value:N2}");

            // [Removed] Monthly contribution logic (not needed in live trading)

            // [New] Check for untracked positions upon startup (only in Live Trading mode)
            if (LiveMode && Portfolio[_symbol].Quantity != 0 && _lots.Count == 0)
            {
                // If there's a position upon live startup but no Lot record, create a default Lot using the average price
                var currentPosition = Portfolio[_symbol];
                _lots.Add(new Lot
                {
                    Quantity = (int)currentPosition.Quantity, // Integer conversion
                    EntryPrice = currentPosition.AveragePrice,
                    HighestPrice = Securities[_symbol].Price,
                    TrailingActive = false
                });
                Log($"Live Trading Startup: Detected {currentPosition.Quantity} shares of existing position. Created default Lot record using average price {currentPosition.AveragePrice:N2}.");
            }

        } // End Initialize

        // ==========================
        //    [New] Load Lot State Method
        // ==========================
        private void LoadLotState()
        {
            if (File.Exists(_lotStateFilePath))
            {
                try
                {
                    var jsonState = File.ReadAllText(_lotStateFilePath);
                    var savedLots = JsonConvert.DeserializeObject<List<Lot>>(jsonState);
                    _lots.AddRange(savedLots);
                    Log($"[Persistence] Successfully loaded algorithm state: recovered {_lots.Count} Lots.");
                }
                catch (Exception ex)
                {
                    Error($"[Persistence] Failed to load Lot state file ({_lotStateFilePath}): {ex.Message}");
                }
            }
            else
            {
                Log($"[Persistence] Lot state file not found ({_lotStateFilePath}), starting from a clean state.");
            }
        }

        // ==========================
        //    [New] Save Lot State Method (Called when algorithm ends)
        // ==========================
        public override void OnEndOfAlgorithm()
        {
            // Ensure state is saved when the algorithm stops or shuts down normally
            try
            {
                var jsonState = JsonConvert.SerializeObject(_lots);
                File.WriteAllText(_lotStateFilePath, jsonState);
                Log($"[Persistence] Algorithm state saved to {_lotStateFilePath}. Recorded {_lots.Count} Lots.");
            }
            catch (Exception ex)
            {
                Error($"[Persistence] Failed to save Lot state: {ex.Message}");
            }
        }

        // ==========================
        //    [Adjustment] OnOrderEvent: Remove _cashPool synchronization
        // ==========================
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            var order = Transactions.GetOrderById(orderEvent.OrderId);
            if (order == null || order.Symbol != _symbol)
            {
                return;
            }

            // In Live Trading, we rely on Portfolio.Cash for automatic updates
            // The following cashPool synchronization code is removed to prevent conflicts with brokerage data

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

        // ==========================
        //    [Adjustment] OnData: Remove _cashPool synchronization
        // ==========================
        public override void OnData(Slice data)
        {
            if (IsWarmingUp) return;

            // Dividend Synchronization: Removed _cashPool sync because dividends should automatically enter Portfolio.Cash in live trading
            if (data.Dividends != null && data.Dividends.TryGetValue(_symbol, out var dividend))
            {
                var qty = Portfolio[_symbol].Quantity;
                if (qty != 0)
                {
                    // In Live Mode, dividends are automatically added to Portfolio.Cash, we only log
                    // Old code: _cashPool += totalDividend; (Removed)
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
        //    [Adjustment] HandleBuyLogic: Remove _cashPool dependency
        // ==========================
        private void HandleBuyLogic(decimal price)
        {
            if (_boughtToday) return;

            // [Adjustment] Use Portfolio.Cash instead of _cashPool
            var availableCash = Portfolio.Cash;
            if (availableCash <= 0) return;

            // Calculate investment amount using real-time available cash
            var amountToInvest = availableCash * _buyFraction;

            // [V13 Optimization 1]: Re-introduce minimum buy amount limit (Controls frequency)
            if (amountToInvest < _minBuyAmount)
            {
                return;
            }

            // [V13 Optimization 2]: Price must be lower than SMA by _buyThreshold (Controls buy quality and frequency)
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
                Debug($"[{Time}] BUY Order Sent: qty={qty}, price={price:N2}, estimatedCost≈{qty * price:N2}.");

                // Lot EntryPrice temporarily uses the current price
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

        // HandleSellLogic remains unchanged because it only relies on the _lots list and no longer depends on _cashPool
        private void HandleSellLogic(decimal price)
        {
            if (_lots.Count == 0) return;
            var remove = new List<Lot>();

            foreach (var lot in _lots)
            {
                if (price > lot.HighestPrice) lot.HighestPrice = price;

                // Check if the hard stop loss condition is met
                var stopLossPrice = lot.EntryPrice * (1 - _stopLossPct);
                if (price <= stopLossPrice)
                {
                    var qtyToSell = -lot.Quantity;
                    if (qtyToSell != 0)
                    {
                        MarketOrder(_symbol, qtyToSell);
                        // Hard Stop triggered, logging
                        Debug($"[{Time}] HARD STOP SELL: qty={qtyToSell}, entry={lot.EntryPrice:N2}, StopPrice={stopLossPrice:N2}, price={price:N2}.");
                    }
                    remove.Add(lot);
                    _soldToday = true;
                    continue;
                }

                // Check if the trailing take profit activation condition is met
                if (!lot.TrailingActive && price >= lot.EntryPrice * (1 + _takeProfitUp))
                {
                    lot.TrailingActive = true;
                    Debug($"[{Time}] Lot hit +{_takeProfitUp:P0}, trailing activated. Entry={lot.EntryPrice:N2}, highest={lot.HighestPrice:N2}");
                }

                // Check if the trailing take profit liquidation condition is met
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
    }
}
