using AlgoTerminal.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using VishwaDockLibNew;

namespace AlgoTerminal.View
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : Window
    {
        public static DockManager dockManager;
        public DashboardView()
        {
            InitializeComponent();
            dockManager = DockManager;
            var ViewModel = App.AppHost!.Services.GetRequiredService<DashboardViewModel>();
            Loaded += ViewModel.DashboardView_Loaded;
            Closing += ViewModel.DashboardView_Closing;
           
        }
    }
}
