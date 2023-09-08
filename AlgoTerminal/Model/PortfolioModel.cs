using AlgoTerminal.ViewModel;
using System;
using System.Collections.ObjectModel;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.Model
{
    public class PortfolioModel : BaseViewModel
    {
        public int LockProfitUsed { get; set; } = 0;
        private string _name = "Loading...";

        public string Name
        {
            get => _name; set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        public EnumIndex Index { get; set; }
        public string? UserID { get; set; }
        private DateTime _entryTime;
        public DateTime EntryTime
        {
            get => _entryTime; set
            {
                if (_entryTime != value)
                {
                    _entryTime = value;
                    OnPropertyChanged(nameof(EntryTime));
                }
            }
        }
        private DateTime _exitTime;
        public DateTime ExitTime
        {
            get => _exitTime; set
            {
                if (_exitTime != value)
                {
                    _exitTime = value;
                    OnPropertyChanged(nameof(ExitTime));
                }
            }
        }
        private double _pnl;
        public double PNL
        {
            get => _pnl; set
            {
                if (_pnl != value)
                {
                    _pnl = value;
                    OnPropertyChanged(nameof(PNL));
                    OnPropertyChanged(nameof(IsMyValueNegative));
                }
            }
        }
        public bool IsMyValueNegative { get { return PNL < 0; } }
        private double _stopLoss;
        public double StopLoss
        {
            get => _stopLoss; set
            {
                if (_stopLoss != value)
                {
                    _stopLoss = value;
                    OnPropertyChanged(nameof(StopLoss));
                }
            }
        }
        private double _targetprofit;

        public double TargetProfit
        {
            get => _targetprofit; set
            {
                if (_targetprofit != value)
                {
                    _targetprofit = value;
                    OnPropertyChanged(nameof(TargetProfit));
                }
            }
        }
        private int _reEntrySl;
        public int ReEntrySL
        {
            get => _reEntrySl; set
            {
                if (_reEntrySl != value)
                {
                    _reEntrySl = value;
                    OnPropertyChanged(nameof(ReEntrySL));
                }
            }
        }
        private int _reEntryTP;

        public int ReEntryTP
        {
            get => _reEntryTP; set
            {
                if (_reEntryTP != value)
                {
                    _reEntryTP = value;
                    OnPropertyChanged(nameof(ReEntryTP));
                }
            }
        }


        public ObservableCollection<InnerObject> InnerObject { get; set; } = new ObservableCollection<InnerObject>();

        //For Calculation They Must update after Trade
        public int BuyTradedQty { get; set; }
        public int SellTradedQty { get; set; }
        private double _totalEntryPremiumPaid;
        public double TotalEntryPremiumPaid
        {
            get => _totalEntryPremiumPaid; internal set
            {
                _totalEntryPremiumPaid = value;
                UpdateInFavorPremiumPaidforTrailSLleg = value;
            }
        }
        public double UpdateInFavorPremiumPaidforTrailSLleg { get; set; }
        public double UpdateInInitialMTMPaidforTrailSLleg { get; set; }
        private string 
        public string IsSTGStatus { get; set; } = "Stopped"; //by def
        public bool IsSTGCompleted { get; set; } = false;
    }
    public class InnerObject : BaseViewModel
    {
        public int STG_ID { get; set; }
        public int Entry_OrderID { get; set; } = 0;
        public int Exit_OrderID { get; set; } = 0;
        public bool IsLegCancelledOrRejected { get; set; } = false; //in Case of Momentum or ORM if Overall square of hit. we will not place new order. 

        private double _ltp;
        public double LTP
        {
            get { return _ltp; }
            set
            {
                if (_ltp != value)
                {
                    _ltp = value;
                    OnPropertyChanged(nameof(LTP));
                }
            }
        }
        private string _name = "Loading...";
        public string StgName { get; set; }
        public string Name
        {
            get => _name; set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        private string _tradingSymbol = "Loading...";
        public string TradingSymbol
        {
            get => _tradingSymbol; set
            {
                if (_tradingSymbol != value)
                {
                    _tradingSymbol = value;
                    OnPropertyChanged(nameof(TradingSymbol));
                }
            }
        }

        private EnumStrategyMessage _message = EnumStrategyMessage.NONE;
        public EnumStrategyMessage Message
        {
            get => _message; set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged(nameof(Message));
                }
            }
        }
        private EnumStrategyStatus _status;
        public EnumStrategyStatus Status
        {
            get => _status; set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }
        private double _entryPrice;
        public double EntryPrice
        {
            get => _entryPrice; set
            {
                if (_entryPrice != value)
                {
                    _entryPrice = value;
                    UpdateInFavorAmountforTrailSLleg = value;
                    OnPropertyChanged(nameof(EntryPrice));
                }
            }
        }
        private double _exitPrice;
        public double ExitPrice
        {
            get => _exitPrice; set
            {
                if (_exitPrice != value)
                {
                    _exitPrice = value;
                    OnPropertyChanged(nameof(ExitPrice));
                }
            }
        }
        private DateTime _entryTime;
        public DateTime EntryTime
        {
            get => _entryTime; set
            {
                if (_entryTime != value)
                {
                    _entryTime = value;
                    OnPropertyChanged(nameof(EntryTime));
                }
            }
        }
        private DateTime _exitTime;
        public DateTime ExitTime
        {
            get => _exitTime; set
            {
                if (_exitTime != value)
                {
                    _exitTime = value;
                    OnPropertyChanged(nameof(ExitTime));
                }
            }
        }
        //private double _mtm;
        //public double MTM
        //{
        //    get => _mtm; set
        //    {
        //        if (_mtm != value)
        //        {
        //            _mtm = value;
        //            OnPropertyChanged(nameof(MTM));
        //        }
        //    }
        //}
        public bool IsMyValueNegative { get { return PNL < 0; } }
        private double _pnl;
        public double PNL
        {
            get => _pnl; set
            {
                if (_pnl != value)
                {
                    _pnl = value;
                    OnPropertyChanged(nameof(PNL));
                    OnPropertyChanged(nameof(IsMyValueNegative));
                }
            }
        }
        private EnumPosition _buysell;
        public EnumPosition BuySell
        {
            get => _buysell; set
            {
                if (_buysell != value)
                {
                    _buysell = value;
                    OnPropertyChanged(nameof(BuySell));
                }
            }
        }
        private int _qty = 0;
        public int Qty
        {
            get => _qty; set
            {
                if (_qty != value)
                {
                    _qty = value;
                    OnPropertyChanged(nameof(Qty));
                }
            }
        }

        private double _reEntrySl;
        public double ReEntrySL
        {
            get => _reEntrySl; set
            {
                if (_reEntrySl != value)
                {
                    _reEntrySl = value;
                    OnPropertyChanged(nameof(ReEntrySL));
                }
            }
        }
        private double _reEntryTP;

        public double ReEntryTP
        {
            get => _reEntryTP; set
            {
                if (_reEntryTP != value)
                {
                    _reEntryTP = value;
                    OnPropertyChanged(nameof(ReEntryTP));
                }
            }
        }
        private double _stopLoss;
        public double StopLoss
        {
            get => _stopLoss; set
            {
                if (_stopLoss != value)
                {
                    _stopLoss = value;
                    OnPropertyChanged(nameof(StopLoss));
                }
            }
        }
        private double _targetprofit;

        public double TargetProfit
        {
            get => _targetprofit; set
            {
                if (_targetprofit != value)
                {
                    _targetprofit = value;
                    OnPropertyChanged(nameof(TargetProfit));
                }
            }
        }
        //Hidden INFO
        public uint Token { get; set; }
        //For Calculation
        public EnumUnderlyingFrom enumUnderlyingFromForLeg { get; set; }
        public double UpdateInFavorAmountforTrailSLleg { get; set; }
        public double EntryUnderliying_INST { get; set; }
        public bool IsLegInMonitoringQue { get; set; } = false;
        public bool IsLegCompleted { get; set; } = false;
    }
}
