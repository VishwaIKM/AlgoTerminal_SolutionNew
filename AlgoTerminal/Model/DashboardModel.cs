using AlgoTerminal.Services;
using AlgoTerminal.ViewModel;
using System.Windows.Media;

namespace AlgoTerminal.Model
{
    public class DashboardModel : BaseViewModel, IDashboardModel
    {
        //SERVER
        public Brush ConnectedColor
        {
            get
            {
                if (_connected == true) { return Brushes.Green; }
                return Brushes.Red;
            }
        }
        private bool cc;
        public bool _connected
        {
            get => cc;
            set
            {
                if (cc != value)
                {
                    cc = value;
                    OnPropertyChanged(nameof(Connected));
                    OnPropertyChanged(nameof(_connected));
                    OnPropertyChanged(nameof(ConnectedColor));
                }
            }
        }
        public string Connected
        {
            get
            {
                if (_connected)
                    return "Moderator Connected";
                else return "Moderator Disconnected";
            }
        }
        //SPOT AND FUTURE DATA 
        private string _nifty50;
        public string Nifty50
        {
            get => _nifty50;
            set
            {
                if (_nifty50 != value)
                {
                    _nifty50 = value;
                    OnPropertyChanged(nameof(Nifty50));
                }

            }
        }

        private string _niftyfut;
        public string NiftyFut
        {
            get => _niftyfut;
            set
            {
                if (_niftyfut != value)
                {
                    _niftyfut = value;
                    OnPropertyChanged(nameof(NiftyFut));
                }

            }
        }

        private string _banknifty;
        public string BankNifty
        {
            get => _banknifty;
            set
            {
                if (_banknifty != value)
                {
                    _banknifty = value;
                    OnPropertyChanged(nameof(BankNifty));
                }

            }
        }
        private string _bankniftyfut;
        public string BankNiftyFut
        {
            get => _bankniftyfut;
            set
            {
                if (_bankniftyfut != value)
                {
                    _bankniftyfut = value;
                    OnPropertyChanged(nameof(BankNiftyFut));
                }

            }
        }

        private string _finnifty;
        public string FinNifty
        {
            get => _finnifty;
            set
            {
                if (_finnifty != value)
                {
                    _finnifty = value;
                    OnPropertyChanged(nameof(FinNifty));
                }

            }
        }
        private string _finniftyfut;
        public string FinNiftyFut
        {
            get => _finniftyfut;
            set
            {
                if (_finniftyfut != value)
                {
                    _finniftyfut = value;
                    OnPropertyChanged(nameof(FinNiftyFut));
                }

            }
        }

    }
}
