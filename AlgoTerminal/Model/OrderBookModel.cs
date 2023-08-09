using AlgoTerminal.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.Model
{
    public sealed class OrderBookModel : BaseViewModel
    {
        public int ClientID { get; set; }


        private string _status;
        public string Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public string TradingSymbol { get; set; }
        public EnumPosition BuySell { get; set; }

        private double _price;
        private int _OrderQty;
        public double Price { get => _price; set { if (_price != value) { _price = value; OnPropertyChanged(nameof(Price)); } } }
        public int OrderQty { get => _OrderQty; set { if (_OrderQty != value) { _OrderQty = value; OnPropertyChanged(nameof(OrderQty)); } } }
        private int _tradeQty;
        public int TradedQty
        {
            get => _tradeQty;
            set
            {
                if (_tradeQty != value)
                {
                    _tradeQty = value;
                    OnPropertyChanged(nameof(TradedQty));
                }
            }
        }
        public double TriggerPrice { get; set; }
        private long _ModId;
        public long ModeratorID { get => _ModId; set { if (_ModId != value) { _ModId = value; OnPropertyChanged(nameof(ModeratorID)); } } }
        private ulong _exchange;
        public ulong ExchangeID { get => _exchange; set { if (_exchange != value) { _exchange = value; OnPropertyChanged(nameof(ExchangeID)); } } }
        private string _updateTime;
        public string UpdateTime { get => _updateTime; set { if (_updateTime != value) { _updateTime = value; OnPropertyChanged(nameof(UpdateTime)); } } }
        private string _rr;
        public string RejectionReason { get => _rr; set { if (_rr != value) { _rr = value; OnPropertyChanged(nameof(RejectionReason)); } } }
    }
}
