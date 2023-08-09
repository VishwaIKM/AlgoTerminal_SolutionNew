using AlgoTerminal.FileManager;
using AlgoTerminal.Manager;
using AlgoTerminal.Model;
using AlgoTerminal.Services;
using AlgoTerminal.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.Response
{
    public class NNAPIDLLResp : IRespNNAPI
    {
        private readonly ILogFileWriter _logFileWriter;
        public NNAPIDLLResp(ILogFileWriter logFileWriter)
        {
            _logFileWriter = logFileWriter;
        }
        public void ChangePasswordResponse(bool Success, int UserID, int NewPassword, string MessageText)
        {
            // throw new NotImplementedException();
        }

        public void EndGetPosition()
        {
            // throw new NotImplementedException();
        }

        public void EndOpenOrderHistory()
        {
            //throw new NotImplementedException();
        }

        public void EndOrderHistory()
        {
            //throw new NotImplementedException();
        }

        public void EndTradeHistory()
        {
            //throw new NotImplementedException();
        }

        public void ErrorNotification(string ErrorDescription)
        {

            LoginViewModel.login.ErrorResponse(ErrorDescription);
            if (ErrorDescription.Contains("Unknown Message Received. Message Header: 0"))
                FeedCB_C._dashboard._connected = false;
        }

        public void GetPosition(int Token, int BuyTradedQty, long BuyTradedValue, int SellTradedQty, long SellTradedValue)
        {
            try
            {
                var contract = ContractDetails.GetContractDetailsByToken((uint)Token);
                if (contract == null)
                    throw new Exception("Did not Find Token Details in Contract File. = " + Token);


                if (OrderManagerModel.NetPosition_Dicc_By_Token.ContainsKey(Token))
                {
                    NetPositionModel netPositionModel = OrderManagerModel.NetPosition_Dicc_By_Token[Token];
                    netPositionModel.BuyQuantity = BuyTradedQty;
                    netPositionModel.SellQuantity = SellTradedQty;
                    netPositionModel.BuyAvgPrice = BuyTradedQty != 0 ? Math.Round(BuyTradedValue / (BuyTradedQty * 100.00), 2) : 0;
                    netPositionModel.SellAvgPrice = SellTradedQty != 0 ? Math.Round(SellTradedValue / (SellTradedQty * 100.00), 2) : 0;
                    netPositionModel.NetValue = Math.Round((-BuyTradedValue + SellTradedValue) / 100.00, 2);
                    netPositionModel.NetQuantity = BuyTradedQty - SellTradedQty;
                }
                else
                {
                    NetPositionModel netPositionModel = new();
                    netPositionModel.TradingSymbol = contract.TrdSymbol;
                    netPositionModel.BuyQuantity = BuyTradedQty;
                    netPositionModel.SellQuantity = SellTradedQty;
                    netPositionModel.BuyAvgPrice = BuyTradedQty != 0 ? Math.Round(BuyTradedValue / (BuyTradedQty * 100.00), 2) : 0;
                    netPositionModel.SellAvgPrice = SellTradedQty != 0 ? Math.Round(SellTradedValue / (SellTradedQty * 100.00), 2) : 0;
                    netPositionModel.NetValue = Math.Round((-BuyTradedValue + SellTradedValue) / 100.00, 2);
                    netPositionModel.NetQuantity = BuyTradedQty - SellTradedQty;

                    if (OrderManagerModel.NetPosition_Dicc_By_Token.TryAdd(Token, netPositionModel))
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            NetPositionViewModel.NetPositionCollection.Add(netPositionModel);

                        }), DispatcherPriority.Background, null);
                    }

                }


                //STG OrderConfirm
            }
            catch (Exception ex)
            {
                _logFileWriter.DisplayLog(EnumLogType.Error, "For Token " + Token + " Erorr Occur While Receving Net Postion from MOD check logFile for more Details.");
                _logFileWriter.WriteLog(EnumLogType.Error, ex.ToString());
            }
        }

        public void LoginResponse(bool LoginSuccess, string MessageText)
        {
            LoginViewModel.login.LoginResponse(LoginSuccess, MessageText);
        }

        public void OpenOrderHistory(int Token, int Price, int Qty, int PendingQty, string BuySell, long AdminOrderID, ulong ExchOrdId, int iUserData, string StrUserData, int TriggerPrice, int TradedQty, long TradedValue, string OrderType, string Time, string Status, string StatusDetail, string RejectionReason)
        {
            //throw new NotImplementedException();
        }

        public void OrderConfirmation(int Token, int Price, int Qty, int PendingQty, string BuySell, long AdminOrderID,
            ulong ExchOrdId, int iUserData, string StrUserData, int TriggerPrice, int TradedQty, long TradedValue, string OrderType,
            string Time, string Status, string StatusDetail, string RejectionReason)
        {
            try
            {
                var contract = ContractDetails.GetContractDetailsByToken((uint)Token);
                if (contract == null)
                    throw new Exception("Did not Find Token Details in Contract File. = " + Token);
                //Two Things Need to Update Order=>forManual Order Validation as well as Portfolio Screen

                //OrderBook will remain common for auto order as well as manual order.
                #region OrderBook Managment
                if (OrderManagerModel.OrderBook_Dicc_By_ClientID.ContainsKey(AdminOrderID))
                {
                    OrderBookModel orderBookModel = OrderManagerModel.OrderBook_Dicc_By_ClientID[AdminOrderID];
                    orderBookModel.OrderQty = Qty;
                    orderBookModel.TradedQty = Qty - PendingQty;
                    orderBookModel.TriggerPrice = TriggerPrice;
                    orderBookModel.ModeratorID = AdminOrderID;
                    orderBookModel.ExchangeID = ExchOrdId;
                    orderBookModel.UpdateTime = Time;
                    orderBookModel.RejectionReason = RejectionReason;


                    if ((Status == "modify" || Status == "modified" || Status == "open" || Status == "open pending"
                        || Status == "Trigger Pending" || Status == "put order request") && (!RejectionReason.Contains("NSE Error") || !RejectionReason.Contains("Server Not Connected")))
                    {

                        //will check and the invert this

                    }
                    else
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {

                            if (OrderBookViewModel.OpenOrderBook.Contains(orderBookModel))
                                OrderBookViewModel.OpenOrderBook.Remove(orderBookModel);

                            if (!OrderBookViewModel.CloseOrderBook.Contains(orderBookModel))
                                OrderBookViewModel.CloseOrderBook.Add(orderBookModel);

                        }), DispatcherPriority.Background, null);

                        //PORTFOLIO STATUS
                        if (OrderManagerModel.Portfolio_Dicc_By_ClientID.TryGetValue(iUserData, out InnerObject value))
                        {

                            if (RejectionReason.Contains("NSE Error") || RejectionReason.Contains("Server Not Connected"))
                            {
                                value.Status = EnumStrategyStatus.REJECTED;
                                value.Message = EnumStrategyMessage.ERROR;
                            }
                            else if (Status.Contains("cancelled"))
                            {
                                value.Status = EnumStrategyStatus.REJECTED;
                                value.Message = EnumStrategyMessage.ORDER_CANCELLED_BY_SYSTEM;
                            }
                            else if (value.Entry_OrderID == iUserData && orderBookModel.OrderQty == orderBookModel.TradedQty)
                            {
                                value.Status = EnumStrategyStatus.RUNING;
                            }
                            else if (value.Exit_OrderID == iUserData && orderBookModel.OrderQty == orderBookModel.TradedQty)
                            {
                                value.Status = EnumStrategyStatus.COMPLETED;
                            }
                            else if (value.Entry_OrderID == iUserData && orderBookModel.OrderQty > orderBookModel.TradedQty && orderBookModel.OrderQty > 0)
                            {
                                value.Status = EnumStrategyStatus.ENTRY_PARTIALLY_TRADED;
                            }
                            else if (value.Exit_OrderID == iUserData && orderBookModel.OrderQty > orderBookModel.TradedQty && orderBookModel.OrderQty > 0)
                            {
                                value.Status = EnumStrategyStatus.EXIT_PARTIALLY_TRADED;
                            }

                        }
                    }

                }
                else
                {
                    OrderBookModel orderBookModel = new();
                    orderBookModel.ClientID = iUserData;
                    orderBookModel.Status = Status;
                    orderBookModel.TradingSymbol = contract.TrdSymbol;
                    orderBookModel.BuySell = BuySell == "B" ? EnumDeclaration.EnumPosition.BUY : EnumDeclaration.EnumPosition.SELL;
                    orderBookModel.Price = Price;
                    orderBookModel.OrderQty = Qty;
                    orderBookModel.TradedQty = Qty - PendingQty;
                    orderBookModel.TriggerPrice = TriggerPrice;
                    orderBookModel.ModeratorID = AdminOrderID;
                    orderBookModel.ExchangeID = ExchOrdId;
                    orderBookModel.UpdateTime = Time;
                    orderBookModel.RejectionReason = RejectionReason;

                    //Add to ViewModel Obser
                    if (OrderManagerModel.OrderBook_Dicc_By_ClientID.TryAdd(AdminOrderID, orderBookModel))
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {

                            OrderBookViewModel.OpenOrderBook.Add(orderBookModel);

                        }), DispatcherPriority.Background, null);

                    }
                }
                _logFileWriter.DisplayLog(EnumLogType.Info, "Order Status : ClientID: " + iUserData + " ModID: " + AdminOrderID + " Status: " + Status);


                #endregion



            }
            catch (Exception ex)
            {
                _logFileWriter.DisplayLog(EnumLogType.Error, "Check Log! Recived Unhandle Error in OrderConfirmation from Moderatot for TOKEN: " + Token + ex.StackTrace);
            }

        }

        public void OrderHistory(int Token, int Price, int Qty, int PendingQty, string BuySell, long AdminOrderID, ulong ExchOrdId, int iUserData, string StrUserData, int TriggerPrice, int TradedQty, long TradedValue, string OrderType, string Time, string Status, string StatusDetail, string RejectionReason)
        {
            //throw new NotImplementedException();
        }

        public void ReceiveOrderType(string Description, string DefaultValue, List<string> lstOrderTypes)
        {
            // throw new NotImplementedException();
        }

        public void StartGetPosition()
        {
            // throw new NotImplementedException();
        }

        public void StartOpenOrderHistory()
        {
            //throw new NotImplementedException();
        }

        public void StartOrderHistory()
        {
            //throw new NotImplementedException();
        }

        public void StartTradeHistory()
        {
            // throw new NotImplementedException();
        }

        public void Trade(int Token, int TradeQty, int TradePrice, string BuySell, int TradeID, ulong ExchOrdId, long AdminOrderID, string TradeTime, int iUserData, string StrUserData)
        {
            try
            {
                //add to tradeBookViewModel from MOD resp
                TradeBookModel tradeBookModel = new();
                var cc = ContractDetails.GetContractDetailsByToken((uint)Token);
                tradeBookModel.TradingSymbol = cc.TrdSymbol;
                tradeBookModel.Time = TradeTime;
                tradeBookModel.Quantity = TradeQty;
                tradeBookModel.Price = TradePrice;
                tradeBookModel.BuySell = BuySell == "B" ? EnumDeclaration.EnumPosition.BUY : EnumDeclaration.EnumPosition.SELL;
                tradeBookModel.OptionType = cc.Opttype;
                tradeBookModel.Strike = cc.Strike;
                tradeBookModel.Expiry = cc.Expiry;
                tradeBookModel.ClientId = iUserData;
                tradeBookModel.TradeID = TradeID;
                tradeBookModel.ModeratorID = AdminOrderID;
                tradeBookModel.ExchnageID = ExchOrdId;
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {

                    TradeBookViewModel.TradeDataCollection.Add(tradeBookModel);

                }), DispatcherPriority.Background, null);

                string _logDisplay = "Token: " + Token + " Traded Qty. : " + TradeQty + " Traded Price: " + TradePrice + " Traded ID: " + TradeID + " ExchangeOrderId: " + ExchOrdId + " AdminOrderID: " + AdminOrderID + "";
                if (tradeBookModel.BuySell == EnumPosition.BUY)
                    _logFileWriter.DisplayLog(EnumLogType.Buy, _logDisplay);
                else
                    _logFileWriter.DisplayLog(EnumLogType.Sell, _logDisplay);

                //#region Portfolio
                ////Portfolio Screen only process automated order. Manaul Order should not be processed in Porfolio as they are not part of STg.
                //if (OrderManagerModel.Portfolio_Dicc_By_ClientID.TryGetValue(iUserData, out InnerObject value))
                //{
                //    var leg_Details = value;

                //    if(tradeBookModel.BuySell == EnumPosition.BUY)
                //    {
                //        leg_Details.BuyTradedQty += TradeQty;
                //        leg_Details.BuyValue = TradePrice * TradeQty;
                //        leg_Details.EntryPrice = Math.Round(leg_Details.BuyValue / leg_Details.BuyTradedQty*100.0,2);
                //    }
                //    else
                //    {
                //        leg_Details.SellTradedQty += TradeQty;
                //        leg_Details.SellTradedQty = TradePrice * TradeQty;
                //        leg_Details.EntryPrice = Math.Round(leg_Details.BuyValue / leg_Details.BuyTradedQty*100.0, 2);
                //    }
                //    //When Equal means Leg Complete --> IF Portfolio logic changes and Modify portfolio added Then Below logic may give incorrect data
                //    if(leg_Details.BuyTradedQty == leg_Details.SellTradedQty)
                //    {
                //        leg_Details.Status = EnumStrategyStatus.Complete;
                //        leg_Details.ExitTime = DateTime.Now;

                //        if(leg_Details.BuySell == EnumPosition.BUY)
                //        {
                //            leg_Details.ExitPrice = leg_Details.SellValue / leg_Details.SellTradedQty;
                //        }
                //        else
                //        {
                //            leg_Details.ExitPrice = leg_Details.BuyValue / leg_Details.BuyTradedQty;
                //        }
                //    }
                //    else
                //    {
                //        leg_Details.Status = EnumStrategyStatus.Running;
                //    }
                //}
                //#endregion
            }
            catch (Exception ex)
            {

            }


        }

        public void TradeHistory(int Token, int TradeQty, int TradePrice, string BuySell, int TradeID, ulong ExchOrdId, long AdminOrderID, string TradeTime, int iUserData, string StrUserData)
        {
            //throw new NotImplementedException();
        }
    }
}
