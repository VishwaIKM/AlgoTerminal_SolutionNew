using AlgoTerminal.Command;
using AlgoTerminal.Model;
using AlgoTerminal.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace AlgoTerminal.ViewModel
{
    public sealed class PortfolioViewModel : BaseViewModel
    {
        #region Prop & Ver
        public static ObservableCollection<PortfolioModel>? StrategyDataCollection { get; set; }
        public PortfolioModel? SelectedItem { get; set; }
        private IStraddleManager straddleManager;
        private RelayCommand2 stopCommand;

        #endregion

        #region Command
        public ICommand StopCommand => stopCommand ??= new RelayCommand2(Stop);

        #endregion

        #region Methods

        public PortfolioViewModel(IStraddleManager straddleManager)
        {
            this.straddleManager = straddleManager;
            StrategyDataCollection ??= new();
        }
        private void Stop()
        {
            if (SelectedItem != null)
            {
                var result = MessageBox.Show("Do You Want to Stop the Strategy? " + SelectedItem.Name, "ALERT", MessageBoxButton.OKCancel);
                var result2 = SelectedItem;
                if (result == MessageBoxResult.OK)
                {
                    straddleManager.SquareOffStraddle920(result2, EnumDeclaration.EnumStrategyMessage.MANUAL_SQUAREOFF);
                }

            }
            else
                MessageBox.Show("Please Select One Stratrgy.");
        }

        #endregion
    }
}
