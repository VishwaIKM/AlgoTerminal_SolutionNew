namespace AlgoTerminal.Services
{
    public interface IDashboardModel
    {
        bool _connected { get; set; }
        string BankNifty { get; set; }
        string BankNiftyFut { get; set; }
        string Connected { get; }
        string FinNifty { get; set; }
        string FinNiftyFut { get; set; }
        string Nifty50 { get; set; }
        string NiftyFut { get; set; }

        string MidcpNiftyFut { get;set; }
        string MidcpNifty { get; set; }
    }
}
