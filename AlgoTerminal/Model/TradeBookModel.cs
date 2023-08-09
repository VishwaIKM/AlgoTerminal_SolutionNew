using System;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.Model
{
    public sealed class TradeBookModel
    {
        public string? TradingSymbol { get; set; }
        public string Time { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public EnumPosition BuySell { get; set; }
        public EnumOptiontype OptionType { get; set; }
        public double Strike { get; set; }
        public string? Symbol { get; set; }
        public DateTime Expiry { get; set; }
        public int ClientId { get; set; }
        public int TradeID { get; set; }
        public long ModeratorID { get; set; }
        public ulong ExchnageID { get; set; }

    }
}
