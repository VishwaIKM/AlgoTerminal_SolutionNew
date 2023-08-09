using AlgoTerminal.Model;
using System.Collections.ObjectModel;

namespace AlgoTerminal.ViewModel
{
    public sealed class TradeBookViewModel : BaseViewModel
    {
        public static ObservableCollection<TradeBookModel> TradeDataCollection { get; set; }
        public TradeBookViewModel()
        {
            TradeDataCollection ??= new();
        }
    }
}
