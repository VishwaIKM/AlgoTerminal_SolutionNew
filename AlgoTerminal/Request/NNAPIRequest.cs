using AlgoTerminal.FileManager;
using AlgoTerminal.NNAPI;
using AlgoTerminal.Response;
using AlgoTerminal.Services;
using System;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.Request
{
    public class NNAPIRequest
    {
        #region Members
        static readonly object _placeOrderLock = new();
        static readonly object _modifyOrderLock = new();
        static readonly object _cancelOrderLock = new();
        #endregion
        private readonly NNAPIDLLResp S_ResponseObj;
        private readonly NNAPI.NNAPI Nnapi;
        private readonly ILogFileWriter logFileWriter;
        public NNAPIRequest(ILogFileWriter logFileWriter)
        {
            this.logFileWriter = logFileWriter;
            S_ResponseObj = new NNAPIDLLResp(logFileWriter);
            Nnapi = new NNAPI.NNAPI(S_ResponseObj);
        }

        #region instances

        #region Methods
        /// <summary>
        /// TO Connect with Server With parm IP and Port Required
        /// </summary>
        public int InitializeServer()
        {
            return Nnapi.Init(App.ModServerIP, App.ModServerPort);
        }


        /// <summary>
        /// Login Request To API
        /// USER ID (INT)
        /// PSSWOARD (INT)
        /// </summary>
        public void LoginRequest(int UserID, int Password)
        {
            Nnapi.Login(UserID, Password);

        }


        /// <summary>
        /// Log out --> Close Connection from server and abort the thread.
        /// </summary>
        public void LogOutRequest()
        {
            Nnapi.Logout();
        }


        /// <summary>
        /// PLACE ORDER TO MODRATOR THROUGH THE NNAPI
        /// </summary>
        /// <param name="OrderType"></param>
        /// <param name="tokenId"></param>
        /// <param name="price"></param>
        /// <param name="orderQty"></param>
        /// <param name="transType"></param>
        /// <param name="orderType"></param>
        /// <param name="triggerPrice"></param>
        /// <param name="marketWatch_OrderID"></param>
        internal void PlaceOrderRequest(int tokenId, double price1, int orderQty, EnumPosition Buysell, OrderType orderType, int triggerPrice, int marketWatch_OrderID, int strUserdata = -1)
        {
            lock (_placeOrderLock)
            {
                int price = 0;
                price = (int)(price1 * 100);
                try
                {
                    if (price > 0 && orderQty > 0)
                    {

                        TransType transType = Buysell == EnumPosition.BUY ? TransType.B : TransType.S;

                        price = OtherMethods.RoundThePrice(price, transType);
                        Nnapi.PlaceOrder(tokenId, price, orderQty, transType, orderType, triggerPrice, marketWatch_OrderID, strUserdata);

                        if (transType == TransType.B)
                        {
                            logFileWriter.DisplayLog(EnumLogType.Success, "Order Type :" + orderType + " Order ID :" + marketWatch_OrderID + " TransType :B " +
                                " Token Id :" + tokenId + " Price :" + price + " Trigger Price: " + triggerPrice + " has been Placed.");
                        }
                        else
                        {
                            logFileWriter.DisplayLog(EnumLogType.Success, "Order Type :" + orderType + " Order ID :" + marketWatch_OrderID + " TransType :S " +
                                " Token Id :" + tokenId + " Price :" + price + " Trigger Price: " + triggerPrice + " has been Placed.");
                        }

                    }
                    else
                    {
                        logFileWriter.DisplayLog(EnumLogType.Error, "Price or Quantity is 0. Order did not placed.");
                    }
                }
                catch (OverflowException)
                {
                    logFileWriter.DisplayLog(EnumLogType.Error, "unable to Convert the Price.");
                    logFileWriter.WriteLog(EnumLogType.Error, "OverFlowException");
                }
                catch (Exception e)
                {
                    logFileWriter.DisplayLog(EnumLogType.Error, "Unable to Place Order To Server");
                    logFileWriter.WriteLog(EnumLogType.Error, e.ToString());
                }
            }
        }


        /// <summary>
        /// Order Histroy Request to get the Previous Record of Order From MOD Server Through the NNAPI
        /// </summary>
        internal void GetOrderHistoryRequest()
        {
            Nnapi.GetOrderHistory();
        }


        /// <summary>
        /// Order OPEN Histroy Request to get the Previous Record of Order From MOD Server Through the NNAPI
        /// </summary>
        internal void GetOpenOrderHistoryRequest() { Nnapi.GetOpenOrderHistory(); }


        /// <summary>
        /// TRADE Histroy Request to get the Previous Record of TRADE From MOD Server Through the NNAPI
        /// </summary>
        internal void GetTradeHistoryRequest() { Nnapi.GetTradeHistory(); }


        /// <summary>
        /// POSITION Request to get the Previous Record of POSITION From MOD Server Through the NNAPI
        /// </summary>
        internal void GetPositionRequest() { Nnapi.GetPosition(); }


        /// <summary>
        /// Request for Cancel Order
        /// </summary>
        /// <param name="_adminOrderId"></param> 
        internal void CancelOrderRequest(long _adminOrderId)
        {
            lock (_cancelOrderLock)
            {
                Nnapi.CancelOrder(_adminOrderId);
                logFileWriter.DisplayLog(EnumLogType.Info, $"Cancel Order Send to Modrator with AdminId: {_adminOrderId}");
                //General.S_Logger.DisplayLog(E_Log_Type.Success, "Cancel Order Send to Modrator with AdminId: " + _adminOrderId);
            }
        }

        /// <summary>
        /// Request For Modify Order
        /// </summary>
        /// <param name="token"></param>
        /// <param name="_adminOrderId"></param>
        /// <param name="_price"></param>
        /// <param name="orderQty"></param>
        /// <param name="transType"></param>
        /// <param name="orderType"></param>
        /// <param name="_triggerPrice"></param>
        internal void ModifyOrderRequest(int token, long _adminOrderId, int _price, int orderQty, TransType transType, OrderType orderType, int _triggerPrice)
        {
            lock (_modifyOrderLock)
            {
                if (_price > 0 || orderQty > 0)
                {
                    Nnapi.ModifyOrder(token, _adminOrderId, _price, orderQty, transType, orderType, _triggerPrice);
                    //General.S_Logger.DisplayLog(E_Log_Type.Success, "Modify Order Send to Modrator with AdminId: " + _adminOrderId + "");
                }
                else
                {
                    //General.S_Logger.DisplayLog(E_Log_Type.Error, "Price or Quantity is 0. Order did not Modify.");
                }
            }
        }

        #endregion

        #endregion
    }
}
