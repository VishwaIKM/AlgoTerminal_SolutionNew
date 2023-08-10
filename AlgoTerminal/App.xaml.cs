using AlgoTerminal.FileManager;
using AlgoTerminal.Manager;
using AlgoTerminal.Model;
using AlgoTerminal.Request;
using AlgoTerminal.Response;
using AlgoTerminal.Services;
using AlgoTerminal.UserControls;
using AlgoTerminal.View;
using AlgoTerminal.ViewModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;

namespace AlgoTerminal
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }
        public static string? straddlePath;
        public static string? logFilePath;
        public static string? InterFaceIP;
        public static string? ModServerIP;
        public static int ModServerPort = 0;

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
               .ConfigureAppConfiguration(x =>
               {
                   x.AddJsonFile("appsettings.json");
                   x.AddEnvironmentVariables();
               })
               .ConfigureServices((hostContext, services) =>
               {
                   //File HardCode Path And Config
                   straddlePath = hostContext.Configuration.GetConnectionString("StraddleFilePath");
                   logFilePath = hostContext.Configuration.GetConnectionString("LogFilePath");
                   InterFaceIP = hostContext.Configuration.GetConnectionString("InterFaceIP");
                   ModServerIP = hostContext.Configuration.GetConnectionString("ModServerIP");
                   Int32.TryParse(hostContext.Configuration.GetConnectionString("ModServerPort"), out ModServerPort);
                   //DBContext ...

                   //Model ...
                   services.AddSingleton<FeedCB_C>();
                   services.AddSingleton<FeedCB_CM>();
                   services.AddSingleton<PortfolioModel>();
                   services.AddSingleton<NNAPIRequest>();

                   //services.AddSingleton<NNAPIDLLResp>();

                   //Services ....
                   services.AddSingleton<IAlgoCalculation, AlgoCalculation>();
                   services.AddSingleton<IFeed, Feed>();
                   services.AddSingleton<ILogFileWriter, LogFileWriter>();
                   services.AddSingleton<IStraddleDataBaseLoadFromCsv, StraddleDataBaseLoadFromCsv>();
                   services.AddSingleton<IStraddleManager, StraddleManager>();
                   services.AddSingleton<IApplicationManagerModel, ApplicationManagerModel>();
                   services.AddSingleton<IDashboardModel, DashboardModel>();


                   //ViewModel....
                   services.AddSingleton<DashboardViewModel>();
                   services.AddSingleton<PortfolioViewModel>();
                   services.AddSingleton<LoggerViewModel>();
                   services.AddSingleton<TradeBookViewModel>();
                   services.AddSingleton<NetPositionViewModel>();
                   services.AddSingleton<LoginViewModel>();
                   services.AddSingleton<OrderBookViewModel>();
                   services.AddSingleton<StraddleViewModel>();
                   //services.AddSingleton<BuySellViewModel>();

                   //View ....
                   services.AddSingleton<DashboardView>(x => new()
                   {
                       DataContext = x.GetRequiredService<DashboardViewModel>()
                   });
                   services.AddSingleton<LoginView>(x => new()
                   {
                       DataContext = x.GetRequiredService<LoginViewModel>()
                   });


                   //USERCONTROL'S
                   services.AddSingleton<PortfolioView>(x => new("RuningPortfolio")
                   {
                       DataContext = x.GetRequiredService<PortfolioViewModel>()
                   });
                   services.AddSingleton<LoggerView>(x => new("Logger")
                   {
                       DataContext = x.GetRequiredService<LoggerViewModel>()
                   });
                   services.AddSingleton<TradeBookView>(x => new("Trade")
                   {
                       DataContext = x.GetRequiredService<TradeBookViewModel>()
                   });
                   services.AddSingleton<NetPositionView>(x => new("NetPosition")
                   {
                       DataContext = x.GetRequiredService<NetPositionViewModel>()
                   });
                   services.AddSingleton<OrderBookView>(x => new("OrderBook")
                   {
                       DataContext = x.GetRequiredService<OrderBookViewModel>()
                   });
                   services.AddSingleton<StraddleView>(x => new("Straddle")
                   {
                       DataContext = x.GetRequiredService<StraddleViewModel>()
                   });
               })
               .Build();
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost!.StartAsync();

            var _runTheWPF = AppHost!.Services.GetRequiredService<LoginView>();
            this.MainWindow = _runTheWPF;
            _runTheWPF.Show();


            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            var ApplicationManager = AppHost!.Services.GetRequiredService<IApplicationManagerModel>();
            await AppHost!.StopAsync();
            base.OnExit(e);
            ApplicationManager.ApplicationStopRequirement();
        }
    }
}
