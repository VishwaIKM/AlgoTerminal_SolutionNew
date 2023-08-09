using AlgoTerminal.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
