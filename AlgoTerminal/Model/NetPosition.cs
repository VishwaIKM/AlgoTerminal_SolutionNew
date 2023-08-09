using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgoTerminal.Model
{
    public sealed class NetPosition
    {
        public int TradedQtyBuy { get; set; }
        public int TradedQtySell { get; set; }
        public long TradedValueBuy { get; set; }
        public long TradedValueSell { get; set; }
    }
}
