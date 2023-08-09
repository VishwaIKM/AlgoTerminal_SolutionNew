using System.Collections.Generic;

namespace AlgoTerminal.Services
{
    public interface IRespNNAPI
    {
        void LoginResponse(bool LoginSuccess, string MessageText);
        void ChangePasswordResponse(bool Success, int UserID, int NewPassword, string MessageText);

        void OrderConfirmation(int Token, int Price, int Qty, int PendingQty, string BuySell, long AdminOrderID, ulong ExchOrdId, int iUserData, string StrUserData, int TriggerPrice, int TradedQty, long TradedValue, string OrderType, string Time, string Status, string StatusDetail, string RejectionReason);

        void StartOrderHistory();
        void OrderHistory(int Token, int Price, int Qty, int PendingQty, string BuySell, long AdminOrderID, ulong ExchOrdId, int iUserData, string StrUserData, int TriggerPrice, int TradedQty, long TradedValue, string OrderType, string Time, string Status, string StatusDetail, string RejectionReason);
        void EndOrderHistory();

        void StartOpenOrderHistory();


        //Token, OrderPrice, OrderQty, PendingQty, BuySell == 1 ? "B" : "S", AdminOrderID, ExchOrdID, iUserData, StrUserData, TriggerPrice, TradedQty, TradedValue, (OrderType == 1 ? "LIMIT" : OrderType == 2 ? "IOC" : OrderType == 3 ? "SL" : "MKT"), Time, Status, SubStatus, RejectionReason
        void OpenOrderHistory(int Token, int Price, int Qty, int PendingQty, string BuySell, long AdminOrderID, ulong ExchOrdId, int iUserData, string StrUserData, int TriggerPrice, int TradedQty, long TradedValue, string OrderType, string Time, string Status, string StatusDetail, string RejectionReason);
        void EndOpenOrderHistory();

        void Trade(int Token, int TradeQty, int TradePrice, string BuySell, int TradeID, ulong ExchOrdId, long AdminOrderID, string TradeTime, int iUserData, string StrUserData);
        void StartTradeHistory();
        void TradeHistory(int Token, int TradeQty, int TradePrice, string BuySell, int TradeID, ulong ExchOrdId, long AdminOrderID, string TradeTime, int iUserData, string StrUserData);
        void EndTradeHistory();

        void StartGetPosition();
        void GetPosition(int Token, int BuyTradedQty, long BuyTradedValue, int SellTradedQty, long SellTradedValue);
        void EndGetPosition();

        void ReceiveOrderType(string Description, string DefaultValue, List<string> lstOrderTypes);

        void ErrorNotification(string ErrorDescription);
    }
}
