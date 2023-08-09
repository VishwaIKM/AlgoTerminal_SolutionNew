using AlgoTerminal.Model;
using AlgoTerminal.NNAPI.Core.Constants;
using AlgoTerminal.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AlgoTerminal.NNAPI
{
    public enum OrderType { LIMIT = 1, IOC, SL, MKT };
    public enum TransType { B = 1, S };
    public class NNAPI
    {
        const int DLL_Version = 3002;

        internal bool VersionOK = false;

        internal List<string> lstOrderTypes = new();

        internal Dictionary<int, string> dcErrorCodes = new();

        internal Dictionary<int, NetPosition> dcNetPosition = new();

        internal Dictionary<int, string> dcLoginReturnCodes = new();
        internal Dictionary<int, string> dcChPwdReturnCodes = new();


        #region "CallBack delegates of C# DLL"
        internal delegate void LoginResponseDel(bool LoginSuccess, string MessageText);
        internal delegate void ChangePasswordResponseDel(bool Success, int UserID, int NewPassword, string MessageText);
        internal delegate void OrderConfirmationDel(int Token, int Price, int Qty, int PendingQty, string BuySell, long AdminOrderID, ulong ExchOrdId, int iUserData, string StrUserData, int TriggerPrice, int TradedQty, long TradedValue, string OrderType, string Time, string Status, string StatusDetail, string RejectionReason);
        internal delegate void TradeDel(int Token, int TradeQty, int TradePrice, string BuySell, int TradeID, ulong ExchOrdId, long AdminOrderID, string TradeTime, int iUserData, string StrUserData);

        internal delegate void StartOrderHistoryDel();
        internal delegate void OrderConfirmationHistoryDel(int Token, int Price, int Qty, int PendingQty, string BuySell, long AdminOrderID, ulong ExchOrdId, int iUserData, string StrUserData, int TriggerPrice, int TradedQty, long TradedValue, string OrderType, string Time, string Status, string StatusDetail, string RejectionReason);
        internal delegate void EndOrderHistoryDel();
        internal delegate void StartTradeHistoryDel();
        internal delegate void TradeConfirmationHistoryDel(int Token, int TradeQty, int TradePrice, string BuySell, int TradeID, ulong ExchOrdId, long AdminOrderID, string TradeTime, int iUserData, string StrUserData);
        internal delegate void EndTradeHistoryDel();

        internal delegate void ReceiveOrderTypeDel(string Description, string DefaultValue, List<string> lstOrderTypes);

        internal delegate void ErrorNotificationDel(string ErrorDescription);

        internal delegate void StartGetPositionDel();
        internal delegate void GetPositionDel(int Token, int BuyTradedQty, long BuyTradedValue, int SellTradedQty, long SellTradedValue);
        internal delegate void EndGetPositionDel();
        #endregion

        Thread _SockedThread;

        private readonly IRespNNAPI iResp;
        internal int UserID_Dll = -1;

        public NNAPI(IRespNNAPI iResp)
        {
            this.iResp = iResp;
            LoadErrorDiic();
            LoadLoginDiic();
            PassWordChangeDiic();
        }

        private void LoadErrorDiic()
        {
            if (dcErrorCodes != null)
            {
                dcErrorCodes.Clear();
                dcErrorCodes.Add(ErrorCode.Invalid_BuySell_Defined, "15001:Invalid Buy/Sell Defined");
                dcErrorCodes.Add(ErrorCode.Invalid_User_ID, "15002:Invalid User ID");
                dcErrorCodes.Add(ErrorCode.Invalid_TokenID, "15003:Invalid TokenID");
                dcErrorCodes.Add(ErrorCode.StopLoss_TriggerPrice_Zero, "15004:TriggerPrice should not be 0 for StopLoss Order");
                dcErrorCodes.Add(ErrorCode.Invalid_Order_Type, "15005:Invalid Order Type");
                dcErrorCodes.Add(ErrorCode.Invalid_Order_Duration, "15006:Invalid Order Duration");
                dcErrorCodes.Add(ErrorCode.Invalid_Quantity, "15007:Invalid Quantity");
                dcErrorCodes.Add(ErrorCode.Invalid_Price, "15008:Invalid Price");
                dcErrorCodes.Add(ErrorCode.Invalid_RemainingQuantity, "15009:Invalid Remaing Quantity");
            }
        }

        private void PassWordChangeDiic()
        {
            if (dcChPwdReturnCodes != null)
            {
                dcChPwdReturnCodes.Clear();
                dcChPwdReturnCodes.Add(1, "Password Changed.");
                dcChPwdReturnCodes.Add(2, "Password Not Changed.");
                dcChPwdReturnCodes.Add(3, "Password Not Changed. Old Password Not Matched.");
                dcChPwdReturnCodes.Add(4, "Password Not Changed. User Not Logged In.");
                dcChPwdReturnCodes.Add(5, "Password Not Changed. Invalid Used ID.");
            }
        }

        private void LoadLoginDiic()
        {
            if (dcLoginReturnCodes != null)
            {
                dcLoginReturnCodes.Clear();
                dcLoginReturnCodes.Add(1, "Login Success");
                dcLoginReturnCodes.Add(2, "Login Failed. Invalid Password.");
                dcLoginReturnCodes.Add(3, "Login Failed. User Already Login.");
                dcLoginReturnCodes.Add(4, "Login Failed. Invalid Used ID.");
            }
        }

        #region "Socket functions"

        private Socket m_socClient;

        internal bool ConnectToServer(string IP, int Port)
        {
            try
            {
                m_socClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                m_socClient.NoDelay = true;
                IPEndPoint ipEnd = new IPEndPoint(IPAddress.Parse(IP), Port);
                m_socClient.Connect(ipEnd);

                //WaitForData();

                if (m_socClient.Connected)
                {
                    _SockedThread = new Thread(new ThreadStart(delegate { ReceivedDataLoop(); }));
                    _SockedThread.Start();

                    MemoryStream ms = new MemoryStream();
                    BinaryWriter bw = new BinaryWriter(ms);

                    bw.Write(12);
                    bw.Write(TransCode.ConnectioRequest);// 11 Connection Request
                    bw.Write(DLL_Version);

                    SendToServer(ms.GetBuffer(), 12);//256);

                    return true;
                }
                else
                {
                    ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                    d.Invoke("Connection failed With Server.");
                    return false;
                }
                //}
            }
            catch (SocketException se)
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke(se.Message);
                return false;
            }
        }

        internal void CloseConnection(bool SendMessage, bool ShowDisconnectedMessage)
        {
            try
            {

                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("1002:Disconnected from Server...");
                if (m_socClient != null)
                {
                    if (SendMessage == true && m_socClient.Connected == true)
                    {
                        MemoryStream ms = new MemoryStream();
                        BinaryWriter bw = new BinaryWriter(ms);
                        bw.Write(12);//Length
                        bw.Write(TransCode.CloseConnectionRequest); // 1002
                        bw.Write(UserID_Dll);

                        SendToServer(ms.GetBuffer(), 12);// 256);

                        Thread.Sleep(5);
                    }
                    m_socClient.Close();
                    m_socClient.Disconnect(true);
                    m_socClient = null;

                }
                try
                {
                    if (_SockedThread.IsAlive)
                        _SockedThread.Interrupt();

                }
                catch
                { }
            }
            catch (Exception ex)
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke(ex.Message);
            }
        }

        internal bool SendToServer(byte[] data, int Size = -1)
        {
            try
            {
                if (IsServerConnected())
                {
                    //if (Size == -1)
                    //{
                    //    Size = data.Length;
                    //}
                    //Size = 12;//256;
                    m_socClient.SendBufferSize = Size;
                    m_socClient.Send(data, Size, 0);
                    return true;
                }
            }
            catch (SocketException se)
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke(se.Message);
            }
            catch (Exception ex)
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke(ex.Message);
            }
            return false;
        }

        private bool Receive(ref byte[] data, uint BytesToRead, uint dataOffset = 0)
        {
            try
            {
                int offset = (int)dataOffset;
                int packetSize = (int)(BytesToRead + dataOffset);

                if (m_socClient.Connected)
                {
                    while (packetSize > offset)
                    {
                        offset += m_socClient.Receive(data, offset, packetSize - offset, SocketFlags.None);
                    }
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("Error in Receiving Packet: ");
                return false;
            }
        }

        internal void ReceivedDataLoop()
        {
            while (m_socClient.Connected)
            {
                try
                {
                    byte[] buffer = new byte[256];
                    //int offset = 0;
                    //while (offset < 256)
                    //{
                    //    offset += m_socClient.Receive(buffer, offset, 256 - offset, 0);
                    //}
                    Receive(ref buffer, 4);
                    BinaryReader br = new BinaryReader(new MemoryStream(buffer, false));
                    int len = br.ReadInt32();
                    Receive(ref buffer, (uint)len - 4, 4);
                    int code = br.ReadInt32();
                    switch (code)
                    {
                        case TransCode.CheckDllVersion: // 12
                            {
                                //Invalid DLL Version Response
                                VersionOK = br.ReadInt32() == 1 ? true : false;

                                if (VersionOK == false)
                                {
                                    ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                                    d.Invoke("DLL Version Not Matched With Moderator.");
                                    CloseConnection(false, true);
                                }
                            }
                            break;
                        case TransCode.CloseConnectionRequest: // 1002
                            {
                                //Server Closed
                                CloseConnection(false, true);
                            }
                            break;
                        case TransCode.LoginResponse: // 1003
                            {
                                //Login Response
                                bool LoginStatus = br.ReadInt32() == 1 ? true : false;
                                int ReturnCode = br.ReadInt32();
                                string dept_code = br.ReadString();
                                string MessageText = "Unknown Login Response";

                                if (dcLoginReturnCodes.ContainsKey(ReturnCode))
                                {
                                    MessageText = dcLoginReturnCodes[ReturnCode];
                                }

                                LoginResponseDel d = new LoginResponseDel(iResp.LoginResponse);
                                d.Invoke(LoginStatus, dept_code + " > " + MessageText);
                            }
                            break;
                        case TransCode.OrderType:// 1010 Order Type
                            {
                                string Description = br.ReadString();
                                string DefaultValue = br.ReadString();
                                lstOrderTypes.Clear();
                                if (DefaultValue != "NA")
                                {
                                    string[] strArr = br.ReadString().Split('|');

                                    foreach (string OT in strArr)
                                    {
                                        lstOrderTypes.Add(OT);
                                    }
                                }
                                else
                                {
                                    DefaultValue = "";
                                }
                                ReceiveOrderTypeDel d = new ReceiveOrderTypeDel(iResp.ReceiveOrderType);
                                d.Invoke(Description, DefaultValue, lstOrderTypes);
                            }
                            break;
                        case TransCode.PasswordChanged: // 1006
                            {
                                //Password Changed Response
                                bool PwdChangeStatus = br.ReadInt32() == 1 ? true : false;
                                int ReturnCode = br.ReadInt32();
                                int UserID = br.ReadInt32();
                                int NewPassword = br.ReadInt32();
                                string MessageText = "";
                                if (dcChPwdReturnCodes.ContainsKey(ReturnCode))
                                {
                                    MessageText = dcChPwdReturnCodes[ReturnCode];
                                }
                                ChangePasswordResponseDel d = new ChangePasswordResponseDel(iResp.ChangePasswordResponse);
                                d.Invoke(PwdChangeStatus, UserID, NewPassword, MessageText);
                            }
                            break;
                        case TransCode.OrderConfirmation: // 2002
                            {
                                //Order Confirmation
                                OrderUpdateDel d = new OrderUpdateDel(OrderUpdate);
                                d.Invoke(buffer);
                            }
                            break;
                        case TransCode.StartOrderHistory: // 5000
                            {
                                //Order History Start
                                StartOrderHistoryDel d = new StartOrderHistoryDel(iResp.StartOrderHistory);
                                d.Invoke();
                            }
                            break;
                        case TransCode.OrderHistory: // 5001
                            {
                                //Order History
                                OrderHistoryDel d = new OrderHistoryDel(OrderHistory);
                                d.Invoke(buffer);
                            }
                            break;
                        case TransCode.EndOrderHistory: // 5002
                            {
                                //Order History End
                                EndOrderHistoryDel d = new EndOrderHistoryDel(iResp.EndOrderHistory);
                                d.Invoke();
                            }
                            break;
                        case TransCode.StartTradeHistory: // 6000
                            {
                                //Trade History Start
                                StartTradeHistoryDel d = new StartTradeHistoryDel(iResp.StartTradeHistory);
                                d.Invoke();
                            }
                            break;
                        case TransCode.TradeHistory: // 6001
                            {
                                //Trade History
                                TradeHistoryDel d = new TradeHistoryDel(TradeHistory);
                                d.Invoke(buffer);
                            }
                            break;
                        case TransCode.EndTradeHistory: // 6002
                            {
                                //Trade History End
                                EndTradeHistoryDel d = new EndTradeHistoryDel(iResp.EndTradeHistory);
                                d.Invoke();
                            }
                            break;
                        case TransCode.TradeUpdate: // 8001
                            {
                                //Trade Update
                                TradeUpdateDel d = new TradeUpdateDel(TradeUpdate);
                                d.Invoke(buffer);
                            }
                            break;
                        case TransCode.StartOpenOrderHistory:// 9000 Open Order History Start
                            {
                                StartOrderHistoryDel d = new StartOrderHistoryDel(iResp.StartOpenOrderHistory);
                                d.Invoke();
                            }
                            break;
                        case TransCode.OpenOrderHistory:// 9001 open Order History
                            {
                                OrderHistoryDel d = new OrderHistoryDel(OpenOrderHistory);
                                d.Invoke(buffer);
                            }
                            break;
                        case TransCode.EndOpenOrderHistory://9002 open Order History End
                            {
                                EndOrderHistoryDel d = new EndOrderHistoryDel(iResp.EndOpenOrderHistory);
                                d.Invoke();
                            }
                            break;
                        default:
                            {
                                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                                d.Invoke("Unknown Message Received. Message Header: " + code);
                                break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                    d.Invoke("Error in Parsing Data from Moderator : " + ex.Message);
                }
            }
        }

        public bool IsServerConnected()
        {
            if (m_socClient != null)
            {
                if (m_socClient.Connected == true)
                    return true;
            }
            return false;
        }

        #endregion

        #region "Functions Calling By User"
        public int Init(string ServerIP, int ServerPort)//, int UserID)
        {
            if (ConnectToServer(ServerIP, ServerPort) == true)
            {
                Thread.Sleep(1000);
                return 1;
            }
            else
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("Server Not Connected");
                return 0;
            }
        }

        public void Login(int UserID, int Password)
        {
            if (VersionOK == true)
            {
                UserID_Dll = UserID;

                MemoryStream mst = new MemoryStream();
                BinaryWriter bws = new BinaryWriter(mst);
                bws.Write(16);//Length
                bws.Write(TransCode.LoginRequest); // 1003
                bws.Write(UserID);
                bws.Write(Password);

                if (!SendToServer(mst.GetBuffer(), 16))// 256))
                {
                    ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                    d.Invoke("Login Request Sending Faliure...");
                }
            }
            else
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("DLL Version Not Matched With Moderator.");
            }
        }

        public void Logout()
        {
            CloseConnection(true, false);
        }

        public void ChangePassword(int OldPassword, int NewPassword)
        {
            if (VersionOK == true)
            {
                MemoryStream mst = new MemoryStream();
                BinaryWriter bws = new BinaryWriter(mst);

                bws.Write(20);//Length
                bws.Write(TransCode.ChangePassWordRequest); // 1006
                bws.Write(UserID_Dll);
                bws.Write(OldPassword);
                bws.Write(NewPassword);
                if (!SendToServer(mst.GetBuffer(), 20))// 256))
                {
                    ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                    d.Invoke("Login Request Sending Faliure...");
                }
            }
            else
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("DLL Version Not Matched With Moderator.");
            }
        }

        public void PlaceOrder(int Token, int Price, int OrderQty, TransType BuySell, OrderType OrderType /*string BuySell, string OrderType*/, int TriggerPrice, int iUserData = -1, int StrUserData = -1)
        {
            if (VersionOK == true)
            {
                //OrderType = OrderType.ToUpper();   @10-02-2017

                //bool Error = false;
                //int ErrorCode = 0;

                //if (Token <= 0)
                //{
                //    Error = true;
                //    ErrorCode = Invalid_TokenID;
                //}
                //else if (Price < 0 || Price % 5 != 0)
                //{
                //    Error = true;
                //    ErrorCode = Invalid_Price;
                //}
                //else if (OrderQty <= 0)
                //{
                //    Error = true;
                //    ErrorCode = Invalid_Quantity;
                //}
                //else if (BuySell[0] != 'B' && BuySell[0] != 'S')
                //{
                //    Error = true;
                //    ErrorCode = Invalid_BuySell_Defined;
                //}
                //else if (!lstOrderTypes.Contains(OrderType)) //LIMIT | IOC | SL | MKT
                //{
                //    Error = true;
                //    ErrorCode = Invalid_Order_Type;
                //}
                //else if (OrderType == "SL" && (TriggerPrice <= 0 || TriggerPrice % 5 != 0))
                //{
                //    Error = true;
                //    ErrorCode = StopLoss_TriggerPrice_Zero;
                //}

                //if (OrderType == "LIMIT" && TriggerPrice > 0)
                //{
                //    TriggerPrice = 0;
                //}

                //if (Error)
                //{
                //    string RejectionReason = "Unknown";
                //    if (dcErrorCodes.ContainsKey(ErrorCode))
                //        RejectionReason = dcErrorCodes[ErrorCode];

                //    //OrderConfirmationDel d = new OrderConfirmationDel(iResp.OrderConfirmation);
                //    //d.Invoke(Token, Price, OrderQty, OrderQty, BuySell, 0, 0, iUserData, StrUserData, TriggerPrice, 0, 0, OrderType, "", "rejected", "rejected", RejectionReason);

                //    OrderConfirmationDel d = new OrderConfirmationDel(iResp.OrderConfirmation);
                //    d.Invoke(Token, Price, OrderQty, OrderQty, BuySell, 0, 0, iUserData, StrUserData, TriggerPrice, 0, 0, OrderType, "", "rejected", "rejected", RejectionReason);
                //}
                //else
                {
                    MemoryStream ms = new MemoryStream();
                    BinaryWriter bw = new BinaryWriter(ms);
                    bw.Write(44);
                    bw.Write(TransCode.NewOrderRequest);//2001
                    //int Length = 36 + StrUserData.Length + 1;
                    //bw.Write(Length);
                    //bw.Write(UserId);
                    bw.Write(UserID_Dll);//8
                    bw.Write(iUserData);//12
                    bw.Write(Token);//16
                    bw.Write(OrderQty);//20
                    bw.Write(Price);//24


                    bw.Write((int)BuySell);
                    bw.Write((int)OrderType);
                    //bw.Write(BuySell[0] == 'B' ? 1 : 2);//28
                    //bw.Write(OrderType[0] == 'L' ? 1 : OrderType[0] == 'I' ? 2 : OrderType[0] == 'S' ? 3 : 4);//36
                    bw.Write(TriggerPrice);//40
                    //bw.Write(StrUserData.Length);
                    bw.Write(StrUserData);
                    if (!SendToServer(ms.GetBuffer(), 44))// 8 + Length))//StrUserData.Length + 1))
                    {
                        ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                        d.Invoke("Order Request Sending Faliure...");
                    }
                }
            }
            else
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("DLL Version Not Matched With Moderator.");
            }
        }

        public void ModifyOrder(int Token, long AdminOrderID, int Price, int Quantity, TransType BuySell, OrderType OrderType /*string BuySell, string OrderType*/, int TriggerPrice, int iUserData = -1, string StrUserData = "")
        {
            if (VersionOK == true)
            {
                //bool Error = false;
                //int ErrorCode = 0;

                //if (Token <= 0)
                //{
                //    Error = true;
                //    ErrorCode = Invalid_TokenID;
                //}
                //else if (Price < 0 || Price % 5 != 0)
                //{
                //    Error = true;
                //    ErrorCode = Invalid_Price;
                //}
                //else if (Quantity <= 0)
                //{
                //    Error = true;
                //    ErrorCode = Invalid_Quantity;
                //}
                //else if (!lstOrderTypes.Contains(OrderType))//OrderType[0] != 'L' && OrderType[0] != 'I' && OrderType[0] != 'S' && OrderType[0] != 'M')
                //{
                //    Error = true;
                //    ErrorCode = Invalid_Order_Type;
                //}
                //else if (OrderType[0] == 'S' && (TriggerPrice <= 0 || TriggerPrice % 5 != 0))
                //{
                //    Error = true;
                //    ErrorCode = StopLoss_TriggerPrice_Zero;
                //}

                //if (OrderType[0] == 'L' && TriggerPrice > 0)
                //{
                //    TriggerPrice = 0;
                //}

                //if (Error)
                //{
                //    string RejectionReason = "Unknown";
                //    if (dcErrorCodes.ContainsKey(ErrorCode))
                //        RejectionReason = dcErrorCodes[ErrorCode];

                //    string status = "open";
                //    if (OrderType[0] == 'S')
                //        status = "Trigger pending";
                //    OrderConfirmationDel d = new OrderConfirmationDel(iResp.OrderConfirmation);
                //    d.Invoke(Token, -1, -1, -1, BuySell, AdminOrderID, 0, iUserData, StrUserData, -1, -1, -1, OrderType, "", status, "not modified", RejectionReason);
                //}
                //else
                {
                    MemoryStream ms = new MemoryStream();
                    BinaryWriter bw = new BinaryWriter(ms);
                    bw.Write(44);
                    bw.Write(TransCode.ModifyOrderRequest);//3001
                    //int Length = 36;
                    //bw.Write(Length);//Length
                    bw.Write(UserID_Dll);//8
                    bw.Write(Token);//12
                    bw.Write(AdminOrderID);//20
                    bw.Write(Quantity);//24
                    bw.Write(Price);//28

                    bw.Write((int)BuySell);
                    bw.Write((int)OrderType);

                    //bw.Write(BuySell[0] == 'B' ? 1 : 2);//32
                    //bw.Write(OrderType[0] == 'L' ? 1 : OrderType[0] == 'I' ? 2 : OrderType[0] == 'S' ? 3 : 4);//36

                    bw.Write(TriggerPrice);//44

                    if (!SendToServer(ms.GetBuffer(), 44))// 8 + Length))
                    {
                        ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                        d.Invoke("Modify Request Sending Faliure...");
                    }
                }
            }
            else
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("DLL Version Not Matched With Moderator.");
            }
        }

        public void CancelOrder(long AdminOrderID, int iUserData = -1, string StrUserData = "")
        {
            if (VersionOK == true)
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(20);
                bw.Write(TransCode.CancelOrderRequest);//4001
                //bw.Write(12);//Length
                bw.Write(UserID_Dll);//8
                bw.Write(AdminOrderID);//16

                if (!SendToServer(ms.GetBuffer(), 20))// 20))
                {
                    ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                    d.Invoke("Cancel Request Sending Faliure...");
                }
            }
            else
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("DLL Version Not Matched With Moderator.");
            }
        }

        public void PlaceStrategy(int Token, int Price, int OrderQty, TransType BuySell, OrderType OrderType /*string BuySell, string OrderType*/, int iUserData = -1, int StrUserData = -1, int param1 = -1, int param2 = -1, int param3 = -1)
        {
            if (VersionOK == true)
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(52);
                bw.Write(TransCode.NewStrategyRequest);
                bw.Write(UserID_Dll);
                bw.Write(iUserData);
                bw.Write(Token);
                bw.Write(OrderQty);
                bw.Write(Price);
                bw.Write((int)BuySell);
                bw.Write((int)OrderType);
                bw.Write(StrUserData);
                bw.Write(param1);
                bw.Write(param2);
                bw.Write(param3);

                if (!SendToServer(ms.GetBuffer(), 52))
                {
                    ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                    d.Invoke("Strategy Request Sending Faliure...");
                }
            }
            else
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("DLL Version Not Matched With Moderator.");
            }
        }

        public void ModifyStrategy(int Token, long AdminOrderID, int Price, int Quantity, OrderType OrderType /*string BuySell, string OrderType*/, int iUserData = -1, int param1 = -1, int param2 = -1, int param3 = -1)
        {
            if (VersionOK == true)
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(48);
                bw.Write(TransCode.ModifyStrategyRequest);
                bw.Write(UserID_Dll);
                bw.Write(Token);
                bw.Write(AdminOrderID);
                bw.Write(Quantity);
                bw.Write(Price);
                bw.Write((int)OrderType);
                bw.Write(param1);
                bw.Write(param2);
                bw.Write(param3);

                if (!SendToServer(ms.GetBuffer(), 48))
                {
                    ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                    d.Invoke("Strategy Modify Sending Faliure...");
                }
            }
            else
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("DLL Version Not Matched With Moderator.");
            }
        }

        public void GetOrderHistory()
        {
            if (VersionOK == true)
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);

                bw.Write(12);//Length
                bw.Write(TransCode.OrderHistory);//5001
                bw.Write(UserID_Dll);//8
                if (!SendToServer(ms.GetBuffer(), 12))// 256))
                {
                    ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                    d.Invoke("OrderHistory Request Sending Faliure...");
                }
            }
            else
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("DLL Version Not Matched With Moderator.");
            }
        }

        public void GetOpenOrderHistory()
        {
            if (VersionOK == true)
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(12);
                bw.Write(TransCode.OpenOrderHistory);//9001
                bw.Write(UserID_Dll);//8
                if (!SendToServer(ms.GetBuffer(), 12))// 256))
                {
                    ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                    d.Invoke("OpenOrderHistory Request Sending Faliure...");
                }
            }
            else
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("DLL Version Not Matched With Moderator.");
            }
        }

        public void GetTradeHistory()
        {
            if (VersionOK == true)
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(12);//Length
                bw.Write(TransCode.TradeHistory);//6001
                bw.Write(UserID_Dll);//8
                if (!SendToServer(ms.GetBuffer(), 12))// 256))
                {
                    ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                    d.Invoke("TradeHistory Request Sending Faliure...");
                }
                else
                    dcNetPosition.Clear();
            }
            else
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("DLL Version Not Matched With Moderator.");
            }
        }

        public void GetPosition(int Token = -1)
        {
            if (VersionOK == true)
            {
                try
                {
                    if (Token == -1)
                    {
                        StartGetPositionDel ds = new StartGetPositionDel(iResp.StartGetPosition);
                        ds.Invoke();
                        Thread.Sleep(10);
                        foreach (int key in dcNetPosition.Keys)
                        {
                            GetPositionDel d = new GetPositionDel(iResp.GetPosition);
                            d.Invoke(key, dcNetPosition[key].TradedQtyBuy, dcNetPosition[key].TradedValueBuy, dcNetPosition[key].TradedQtySell, dcNetPosition[key].TradedValueSell);
                        }
                        Thread.Sleep(10);
                        EndGetPositionDel de = new EndGetPositionDel(iResp.EndGetPosition);
                        de.Invoke();
                    }
                    else
                    {
                        if (dcNetPosition.ContainsKey(Token))
                        {
                            GetPositionDel d = new GetPositionDel(iResp.GetPosition);
                            d.Invoke(Token, dcNetPosition[Token].TradedQtyBuy, dcNetPosition[Token].TradedValueBuy, dcNetPosition[Token].TradedQtySell, dcNetPosition[Token].TradedValueSell);
                        }
                        else
                        {
                            ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                            d.Invoke("GetPosition: Token Not Found");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                    d.Invoke("GetPosition Error. " + ex.Message);
                }
            }
            else
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("DLL Version Not Matched With Moderator.");
            }
        }
        #endregion

        #region "Responce Getting From Admin"
        delegate void OrderUpdateDel(byte[] buffer);
        delegate void OrderHistoryDel(byte[] buffer);

        delegate void TradeUpdateDel(byte[] buffer);
        delegate void TradeHistoryDel(byte[] buffer);

        void OrderUpdate(byte[] buffer)
        {
            try
            {
                BinaryReader br = new BinaryReader(new MemoryStream(buffer, false));
                int len = br.ReadInt32();
                int responseCode = br.ReadInt32();//4
                int Token = br.ReadInt32();//8
                int OrderQty = br.ReadInt32();//12
                int OrderPrice = br.ReadInt32();//16
                int BuySell = br.ReadInt32();//20
                int PendingQty = br.ReadInt32();//24
                int TriggerPrice = br.ReadInt32();//28
                int TradedQty = br.ReadInt32();//32
                long TradedValue = br.ReadInt64();//40
                long AdminOrderID = br.ReadInt64();//48
                int iUserData = br.ReadInt32();//52
                ulong ExchOrdID = br.ReadUInt64();//60
                int OrderType = br.ReadInt32();//64
                long DateTimeinBinary = br.ReadInt64();//76
                string Status = br.ReadString();
                string SubStatus = br.ReadString();
                string RejectionReason = br.ReadString();
                string StrUserData = br.ReadString();
                string Time = DateTime.FromBinary(DateTimeinBinary).ToString("HH:mm:ss");
                OrderConfirmationDel d = new OrderConfirmationDel(iResp.OrderConfirmation);
                d.Invoke(Token, OrderPrice, OrderQty, PendingQty, BuySell == 1 ? "B" : "S", AdminOrderID, ExchOrdID, iUserData, StrUserData, TriggerPrice, TradedQty, TradedValue, OrderType == 1 ? "LIMIT" : OrderType == 2 ? "IOC" : OrderType == 3 ? "SL" : "MKT", Time, Status, SubStatus, RejectionReason);
            }
            catch (Exception ex)
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("OrderUpdate: " + ex.Message);
            }
        }

        void OrderHistory(byte[] buffer)
        {
            try
            {
                BinaryReader br = new BinaryReader(new MemoryStream(buffer, false));
                int len = br.ReadInt32();//4
                int transCode = br.ReadInt32();//8
                int Token = br.ReadInt32();//12
                int OrderQty = br.ReadInt32();//16
                int OrderPrice = br.ReadInt32();//20
                int BuySell = br.ReadInt32();//24
                int PendingQty = br.ReadInt32();//28
                int TriggerPrice = br.ReadInt32();//32
                int TradedQty = br.ReadInt32();//36
                long TradedValue = br.ReadInt64();//44
                long AdminOrderID = br.ReadInt64();//52
                int iUserData = br.ReadInt32();//56
                ulong ExchOrdID = br.ReadUInt64();//64
                int OrderType = br.ReadInt32();//68
                long DateTimeinBinary = br.ReadInt64();//76
                string Status = br.ReadString();
                string SubStatus = br.ReadString();
                string RejectionReason = br.ReadString();
                //string Time = br.ReadString();
                string Time = DateTime.FromBinary(DateTimeinBinary).ToString("HH:mm:ss");
                string StrUserData = br.ReadString();

                OrderConfirmationHistoryDel d = new OrderConfirmationHistoryDel(iResp.OrderHistory);
                d.Invoke(Token, OrderPrice, OrderQty, PendingQty, BuySell == 1 ? "B" : "S", AdminOrderID, ExchOrdID, iUserData, StrUserData, TriggerPrice, TradedQty, TradedValue, OrderType == 1 ? "LIMIT" : OrderType == 2 ? "IOC" : OrderType == 3 ? "SL" : "MKT", Time, Status, SubStatus, RejectionReason);

                Thread.Sleep(10);
                MemoryStream mst = new MemoryStream();
                BinaryWriter bws = new BinaryWriter(mst);
                bws.Write(12);
                bws.Write(TransCode.EndOrderHistory); // 5002
                //bws.Write(4);//Length
                bws.Write(UserID_Dll);

                if (!SendToServer(mst.GetBuffer(), 12))// 256))
                {
                    ErrorNotificationDel de = new ErrorNotificationDel(iResp.ErrorNotification);
                    de.Invoke("Order Confirmation Sending Faliure...");
                }
            }
            catch (Exception ex)
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("OrderHistory: " + ex.Message);
            }
        }

        void OpenOrderHistory(byte[] buffer)
        {
            try
            {
                BinaryReader br = new BinaryReader(new MemoryStream(buffer, false));
                int len = br.ReadInt32();
                int responseCode = br.ReadInt32();//4
                int Token = br.ReadInt32();//8
                int OrderQty = br.ReadInt32();//12
                int OrderPrice = br.ReadInt32();//16
                int BuySell = br.ReadInt32();//20
                int PendingQty = br.ReadInt32();//24
                int TriggerPrice = br.ReadInt32();//28
                int TradedQty = br.ReadInt32();//32
                long TradedValue = br.ReadInt64();//40
                long AdminOrderID = br.ReadInt64();//48
                int iUserData = br.ReadInt32();//52
                ulong ExchOrdID = br.ReadUInt64();//60
                int OrderType = br.ReadInt32();//64
                long DateTimeinBinary = br.ReadInt64();//76
                string Status = br.ReadString();
                string SubStatus = br.ReadString();
                string RejectionReason = br.ReadString();
                //string Time = br.ReadString();
                string StrUserData = br.ReadString();
                string Time = DateTime.FromBinary(DateTimeinBinary).ToString("HH:mm:ss");

                OrderConfirmationHistoryDel d = new OrderConfirmationHistoryDel(iResp.OpenOrderHistory);
                d.Invoke(Token, OrderPrice, OrderQty, PendingQty, BuySell == 1 ? "B" : "S", AdminOrderID, ExchOrdID, iUserData, StrUserData, TriggerPrice, TradedQty, TradedValue, OrderType == 1 ? "LIMIT" : OrderType == 2 ? "IOC" : OrderType == 3 ? "SL" : "MKT", Time, Status, SubStatus, RejectionReason);

                //Thread.Sleep(10);
                // Acknowledgement
                MemoryStream mst = new MemoryStream();
                BinaryWriter bws = new BinaryWriter(mst);
                bws.Write(12);
                bws.Write(TransCode.EndOpenOrderHistory); // 9002
                bws.Write(UserID_Dll);

                if (!SendToServer(mst.GetBuffer(), 12))
                {
                    ErrorNotificationDel de = new ErrorNotificationDel(iResp.ErrorNotification);
                    de.Invoke("Open Order Confirmation Sending Faliure...");
                }
            }
            catch (Exception ex)
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("OpenOrderHistory: " + ex.Message);
            }
        }

        void TradeUpdate(byte[] buffer)
        {
            try
            {
                BinaryReader br = new BinaryReader(new MemoryStream(buffer, false));
                int len = br.ReadInt32();
                int responseCode = br.ReadInt32();
                int Token = br.ReadInt32();
                int TradeID = br.ReadInt32();
                int TradePrice = br.ReadInt32();
                int TradedQty = br.ReadInt32();
                int BuySell = br.ReadInt32();
                ulong ExchOrdID = br.ReadUInt64();
                long AdminOrderID = br.ReadInt64();
                int NeatID = br.ReadInt32();
                int iUserData = br.ReadInt32();
                int iTradeTime = br.ReadInt32();
                string StrUserData = br.ReadString();
                string userCode = br.ReadString();

                if (StrUserData == null)
                    StrUserData = "";
                if (dcNetPosition.ContainsKey(Token))
                {
                    NetPosition stNP = dcNetPosition[Token];
                    if (BuySell == 1)
                    {
                        stNP.TradedQtyBuy += TradedQty;
                        stNP.TradedValueBuy += TradedQty * (long)TradePrice;
                    }
                    else
                    {
                        stNP.TradedQtySell += TradedQty;
                        stNP.TradedValueSell += TradedQty * (long)TradePrice;
                    }
                    dcNetPosition[Token] = stNP;
                }
                else
                {
                    NetPosition stNP = new NetPosition();
                    if (BuySell == 1)
                    {
                        stNP.TradedQtyBuy = TradedQty;
                        stNP.TradedValueBuy = TradedQty * (long)TradePrice;
                    }
                    else
                    {
                        stNP.TradedQtySell = TradedQty;
                        stNP.TradedValueSell = TradedQty * (long)TradePrice;
                    }
                    dcNetPosition.Add(Token, stNP);
                }

                DateTime tm = Convert.ToDateTime("1/1/1980 12:00:00 AM").AddSeconds(Convert.ToInt32(iTradeTime));

                string TradeTime = DateTime.Now.ToString("HH:mm:ss");// tm.ToString("HH:mm:ss");

                TradeDel d = new TradeDel(iResp.Trade);
                d.Invoke(Token, TradedQty, TradePrice, BuySell == 1 ? "B" : "S", TradeID, ExchOrdID, AdminOrderID, TradeTime, iUserData, StrUserData);

                GetPositionDel dp = new GetPositionDel(iResp.GetPosition);
                dp.Invoke(Token, dcNetPosition[Token].TradedQtyBuy, dcNetPosition[Token].TradedValueBuy, dcNetPosition[Token].TradedQtySell, dcNetPosition[Token].TradedValueSell);
            }
            catch (Exception ex)
            {
                ErrorNotificationDel d = new ErrorNotificationDel(iResp.ErrorNotification);
                d.Invoke("TradeUpdate: " + ex.Message);
            }
        }

        void TradeHistory(byte[] buffer)
        {
            try
            {
                BinaryReader br = new BinaryReader(new MemoryStream(buffer, false));
                int len = br.ReadInt32();
                int responseCode = br.ReadInt32();
                int Token = br.ReadInt32();
                int TradeID = br.ReadInt32();
                int TradePrice = br.ReadInt32();
                int TradedQty = br.ReadInt32();
                int BuySell = br.ReadInt32();
                ulong ExchOrdID = br.ReadUInt64();
                long AdminOrderID = br.ReadInt64();
                int NeatID = br.ReadInt32();
                int iUserData = br.ReadInt32();
                int iTradeTime = br.ReadInt32();
                string StrUserData = br.ReadString();
                string userCode = br.ReadString();


                if (dcNetPosition.ContainsKey(Token))
                {
                    NetPosition stNP = dcNetPosition[Token];
                    if (BuySell == 1)
                    {
                        stNP.TradedQtyBuy += TradedQty;
                        stNP.TradedValueBuy += TradedQty * (long)TradePrice;
                    }
                    else
                    {
                        stNP.TradedQtySell += TradedQty;
                        stNP.TradedValueSell += TradedQty * (long)TradePrice;
                    }
                    dcNetPosition[Token] = stNP;
                }
                else
                {
                    NetPosition stNP = new NetPosition();
                    if (BuySell == 1)
                    {
                        stNP.TradedQtyBuy = TradedQty;
                        stNP.TradedValueBuy = TradedQty * (long)TradePrice;
                    }
                    else
                    {
                        stNP.TradedQtySell = TradedQty;
                        stNP.TradedValueSell = TradedQty * (long)TradePrice;
                    }
                    dcNetPosition.Add(Token, stNP);
                }

                DateTime tm = Convert.ToDateTime("1/1/1980 12:00:00 AM").AddSeconds(Convert.ToInt32(iTradeTime));

                string TradeTime = DateTime.Now.ToString("HH:mm:ss");

                TradeConfirmationHistoryDel d = new TradeConfirmationHistoryDel(iResp.TradeHistory);
                d.Invoke(Token, TradedQty, TradePrice, BuySell == 1 ? "B" : "S", TradeID, ExchOrdID, AdminOrderID, TradeTime, iUserData, StrUserData);

                GetPositionDel dp = new GetPositionDel(iResp.GetPosition);
                dp.Invoke(Token, dcNetPosition[Token].TradedQtyBuy, dcNetPosition[Token].TradedValueBuy, dcNetPosition[Token].TradedQtySell, dcNetPosition[Token].TradedValueSell);
            }
            catch (Exception ex)
            {
                ErrorNotificationDel d = new(iResp.ErrorNotification);
                d.Invoke("TradeHistory: " + ex.Message);
            }
        }

        #endregion
    }
}
