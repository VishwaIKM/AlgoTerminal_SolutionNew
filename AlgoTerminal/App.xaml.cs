using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AlgoTerminal.ViewModel;
using AlgoTerminal.View;

namespace AlgoTerminal
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }
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
                    
                    //ViewModel....
                    services.AddSingleton<DashboardViewModel>();
                    services.AddSingleton<Sample1ViewModel>();
                    services.AddSingleton<Sample2ViewModel>();
                 

                    //View ....
                    services.AddSingleton<DashboardView>(x => new()
                    {
                        DataContext = x.GetRequiredService<DashboardViewModel>()
                    });
                    services.AddSingleton<Sample1>(y => new("Sample1")
                    {
                        DataContext = y.GetRequiredService<Sample1ViewModel>()
                    });
                    services.AddSingleton<Sample2>(y => new("Sample2")
                    {
                        DataContext = y.GetRequiredService<Sample2ViewModel>()
                    });
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost!.StartAsync();

            var _runTheWPF = AppHost!.Services.GetRequiredService<DashboardView>();
            this.MainWindow = _runTheWPF;
            _runTheWPF.Show();


            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost!.StopAsync();
            base.OnExit(e);
        }
    }
}
