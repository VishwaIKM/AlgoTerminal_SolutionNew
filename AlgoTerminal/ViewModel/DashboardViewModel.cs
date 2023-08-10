using AlgoTerminal.Command;
using AlgoTerminal.Services;
using AlgoTerminal.UserControls;
using AlgoTerminal.View;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace AlgoTerminal.ViewModel
{
    public class DashboardViewModel : BaseViewModel
    {
        #region Var & Fileds
        public IDashboardModel DashboardModel { get; private set; }

        private readonly PortfolioView _portfolioView;
        private readonly NetPositionView _netPositionView;
        private readonly LoggerView _loggerView;
        private readonly TradeBookView _tradeBookView;
        private readonly OrderBookView _orderBookView;
        private readonly StraddleView _straddleView;
        static string SettingFileName { get { return string.Format(@"{0}\{1}", Environment.CurrentDirectory, "Layout.xml"); } }
        #endregion

        #region Methods 
        public DashboardViewModel(StraddleView straddleView,OrderBookView orderBookView, PortfolioView portfolioView, NetPositionView netPositionView, LoggerView loggerView, TradeBookView tradeBookView, IDashboardModel dashboardModel)//Sample2 sample2, Sample1 sample1)
        {
            //this.sample2 = sample2;
            //this.sample1 = sample1;
            _straddleView = straddleView;
            _netPositionView = netPositionView;
            _loggerView = loggerView;
            _tradeBookView = tradeBookView;
            _portfolioView = portfolioView;
            _orderBookView = orderBookView;
            this.DashboardModel = dashboardModel;
            DockInitialization();
        }

        private void DockInitialization()
        {
            DashboardView.dockManager.RegisterDock(_netPositionView);
            DashboardView.dockManager.RegisterDock(_loggerView);
            DashboardView.dockManager.RegisterDock(_tradeBookView);
            DashboardView.dockManager.RegisterDock(_portfolioView);
            DashboardView.dockManager.RegisterDock(_orderBookView);
            DashboardView.dockManager.RegisterDock(_straddleView);
        }

        public void DashboardView_Closing(object? sender, CancelEventArgs e)
        {
            DashboardView.dockManager.SaveCurrentLayout("DashboardView");

            var doc = new XDocument();
            var rootNode = new XElement("Layouts");
            foreach (var layout in DashboardView.dockManager.Layouts.Values)
                layout.Save(rootNode);
            doc.Add(rootNode);
            doc.Save(SettingFileName);
            DashboardView.dockManager.Dispose();
        }

        public void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(SettingFileName))
            {
                var layout = XDocument.Parse(File.ReadAllText(SettingFileName));
                foreach (var item in layout.Root.Elements())
                {
                    var name = item.Attribute("Name").Value;
                    if (DashboardView.dockManager.Layouts.ContainsKey(name))
                        DashboardView.dockManager.Layouts[name].Load(item);
                    else DashboardView.dockManager.Layouts[name] = new VishwaDockLibNew.LayoutSetting.LayoutSetting(name, item);
                }

                DashboardView.dockManager.ApplyLayout("DashboardView");
            }
            else
            {
                _portfolioView.DockControl.Show();
                _loggerView.DockControl.Show();
            }
        }
        #endregion

        #region Command
        public ICommand OnClick_AlgoTrading => new RelayCommand2(OnClick_AlgoTradingCommand, CanThisMethodExecute);
        private void OnClick_AlgoTradingCommand()
        {
            // this.sample1.DockControl.Show();
            About about = new About();
            about.Show();

        }
        public ICommand OnClick_RuningPortfolio => new RelayCommand2(OnClick_RuningPortfolioCommand, CanThisMethodExecute);

        private void OnClick_RuningPortfolioCommand()
        {
            _portfolioView.DockControl.Show();
        }

        public ICommand OnClick_TradeBook => new RelayCommand2(OnClick_TradeBookCommand, CanThisMethodExecute);

        private void OnClick_TradeBookCommand()
        {
            _tradeBookView.DockControl.Show();
        }

        public ICommand OnClick_NetPosition => new RelayCommand2(OnClick_NetPositionCommand, CanThisMethodExecute);

        private void OnClick_NetPositionCommand()
        {
            _netPositionView.DockControl.Show();
        }

        public ICommand OnClick_LogDetails => new RelayCommand2(OnClick_LogDetailsCommand, CanThisMethodExecute);

        private void OnClick_LogDetailsCommand()
        {
            _loggerView.DockControl.Show();
        }

        public ICommand OnClick_OrderBook => new RelayCommand2(OnClick_OrderBookCommand, CanThisMethodExecute);

        private void OnClick_OrderBookCommand()
        {
            _orderBookView.DockControl.Show();
        }
        public ICommand OnClick_AddStraddle => new RelayCommand2(OnClick_AddStraddleCommand, CanThisMethodExecute);

        private void OnClick_AddStraddleCommand()
        {
          _straddleView.DockControl.Show();
        }

        #endregion
    }
}
