#region imports
using System;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    public sealed class PortfolioManager
    {
        // Portfolio target construction comes after the stock-selection model is implemented.
        public DateTime CreatedUtc { get; } = DateTime.UtcNow;
    }
}
