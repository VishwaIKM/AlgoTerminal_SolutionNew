using AlgoTerminal.Model;
using System.Collections.ObjectModel;

namespace AlgoTerminal.ViewModel
{
    public sealed class OrderBookViewModel : BaseViewModel
    {
        public static ObservableCollection<OrderBookModel>? OpenOrderBook { get; set; }
        public static ObservableCollection<OrderBookModel>? CloseOrderBook { get; set; }


        public OrderBookViewModel()
        {
            OpenOrderBook ??= new();
            CloseOrderBook ??= new();
        }
    }
}
