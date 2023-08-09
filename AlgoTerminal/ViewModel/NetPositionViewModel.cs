using AlgoTerminal.Command;
using AlgoTerminal.Manager;
using AlgoTerminal.Model;
using AlgoTerminal.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AlgoTerminal.ViewModel
{
    public sealed class NetPositionViewModel : BaseViewModel
    {
        #region Members prop
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        public static ObservableCollection<NetPositionModel> NetPositionCollection { get; set; }
        public NetPositionModel? SelectedItem { get; set; }
        private readonly IFeed feed;


        //cmd
        private RelayCommand2 _buyOrderCommand;
        private RelayCommand2 _sellOrderCommand;
        #endregion

        #region Methods
        public NetPositionViewModel(IFeed feed)
        {
            NetPositionCollection ??= new();
            StartNetPositionUpdateTask();
            this.feed = feed;
        }

        private void StartNetPositionUpdateTask()
        {
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 10);
            dispatcherTimer.Start();
        }

        private async void dispatcherTimer_Tick(object? sender, EventArgs e)
        {
            #region Netpostion

            foreach (var item in OrderManagerModel.NetPosition_Dicc_By_Token)
            {
                if (feed.FeedC != null)
                {
                    if (feed.FeedC.dcFeedData.TryGetValue((ulong)item.Key, out FeedC.ONLY_MBP_DATA_7208 oNLY_MBP_DATA_7208))
                    {
                        item.Value.LTP = Math.Round(Convert.ToDouble(oNLY_MBP_DATA_7208.LastTradedPrice) / 100.00, 2);
                        item.Value.MTM = Math.Round(item.Value.NetValue + item.Value.NetQuantity * item.Value.LTP, 2);
                    }
                }

            }
            await Task.Delay(101);
            #endregion
        }

        private void ExecuteBuySellCommand(bool IsBuy = false)
        {
            if (SelectedItem == null) return;
            else
            {

                //BuySellView buySellView = App.AppHost!.Services.GetRequiredService<BuySellView>();
                //buySellView.Show();
                //var BuySellWindow = App.AppHost!.Services.GetRequiredService<BuySellView>();
                //BuySellWindow.Show();
            }
        }
        #endregion


        #region Command

        public ICommand BuyOrderCommand => _buyOrderCommand ??= new RelayCommand2(BUY);

        private void BUY()
        {
            ExecuteBuySellCommand(true);
        }

        public ICommand SellOrderCommand => _sellOrderCommand ??= new RelayCommand2(SELL);

        private void SELL()
        {
            ExecuteBuySellCommand();
        }
        #endregion
    }
}
