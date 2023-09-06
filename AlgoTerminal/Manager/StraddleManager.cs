using AlgoTerminal.FileManager;
using AlgoTerminal.Model;
using AlgoTerminal.NNAPI;
using AlgoTerminal.Services;
using AlgoTerminal.ViewModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.Manager
{
    public class StraddleManager : IStraddleManager
    {
        #region var & filed
        private readonly IStraddleDataBaseLoadFromCsv straddleDataBaseLoad;
        private readonly ILogFileWriter logFileWriter;
        private readonly IAlgoCalculation algoCalculation;
        // private readonly Semaphore _lockOn = new(1,1);
        //DispatcherTimer dispatcherTimer = new();
        #endregion

        #region NEW_STG_MANAGER_CORE 

        #region Configuration Loading....
        /// <summary>
        /// fILE lOADING
        /// </summary>
        /// <returns></returns>
        public bool StraddleStartUP()
        {
            try
            {
                if (File.Exists(App.straddlePath))
                {
                    straddleDataBaseLoad.LoadStaddleStratgy(App.straddlePath);
                }
                else
                {
                    logFileWriter.DisplayLog(EnumDeclaration.EnumLogType.Warning, " Straddle DataBase File not found on given Path " + App.straddlePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                logFileWriter.DisplayLog(EnumDeclaration.EnumLogType.Error, " Application StartUp Block Complete. ");
                logFileWriter.WriteLog(EnumDeclaration.EnumLogType.Error, " Application StartUp Block Complete. " + ex.StackTrace);
                return false;
            }
            finally
            {
                logFileWriter.DisplayLog(EnumDeclaration.EnumLogType.Info, " Application StartUp Block Complete. ");
            }
        }

        /// <summary>
        /// Load the Config Data From Master to Portfolio and inner Object .
        /// </summary>
        /// <returns></returns>
        public async Task<bool> FirstTimeDataLoadingOnGUI()
        {
            bool IsValid = true;
            try//MAIN
            {
                if (straddleDataBaseLoad.Master_Straddle_Dictionary == null || straddleDataBaseLoad.Straddle_LegDetails_Dictionary == null)
                {
                    logFileWriter.DisplayLog(EnumLogType.Error, "The strategy is Not loaded Correctly");
                    IsValid = false;
                    return IsValid;
                }

                //Strategy load
                foreach (string stg_key in straddleDataBaseLoad.Master_Straddle_Dictionary.Keys)
                {
                    try//MID
                    {
                        var stg_value = straddleDataBaseLoad.Master_Straddle_Dictionary[stg_key];
                        PortfolioModel portfolioModel = new()
                        {
                            Name = stg_key,
                            Index = stg_value.Index,
                            EntryTime = stg_value.EntryTime,
                            ExitTime = stg_value.ExitTime,
                            UserID = stg_value.UserID
                        };
                        portfolioModel.InnerObject ??= new();

                        //ADD in Portfolio for GUI
                        if (!General.Portfolios.ContainsKey(portfolioModel.Name))
                        {
                            General.Portfolios.TryAdd(portfolioModel.Name, portfolioModel);
                            if (PortfolioViewModel.StrategyDataCollection == null)
                                throw new Exception("THE PortFolio VIEW->MODEL not initiated");
                            //portfolioViewModel.StrategyDataCollection.Add(portfolioModel);

                            //leg load
                            var LegDetails = new ConcurrentDictionary<string, LegDetails>();
                            if (straddleDataBaseLoad.Straddle_LegDetails_Dictionary.TryGetValue(stg_key, out LegDetails))
                            {
                                foreach (var Leg in LegDetails.Keys)
                                {
                                    try
                                    {
                                        var leg_value = LegDetails[Leg];
                                        InnerObject innerObject = new()
                                        {
                                            StgName = stg_key,
                                            Name = Leg,
                                            BuySell = leg_value.Position,
                                            TradingSymbol = "Loading ...",
                                            Qty = leg_value.Lots,
                                            enumUnderlyingFromForLeg = stg_value.UnderlyingFrom
                                        };
                                        await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                        {

                                            portfolioModel.InnerObject.Add(innerObject);

                                        }), DispatcherPriority.Background, null);

                                        //ADD TO GUI
                                        General.Portfolios.TryUpdate(portfolioModel.Name, portfolioModel, General.Portfolios[portfolioModel.Name]);
                                        if (PortfolioViewModel.StrategyDataCollection == null)
                                            throw new Exception("THE PortFolio VIEW -> MODEL not initiated");
                                    }
                                    catch (Exception ex)
                                    {
                                        IsValid = false;
                                        logFileWriter.WriteLog(EnumDeclaration.EnumLogType.Error, ex.ToString());
                                    }
                                }
                                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    PortfolioViewModel.StrategyDataCollection.Add(portfolioModel);

                                }), DispatcherPriority.Background, null);

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        IsValid = false;
                        logFileWriter.WriteLog(EnumLogType.Error, ex.ToString());
                    }

                }
                //Task.Run(()=>StartMonitoringCommand()); // Thread to watch
#pragma warning disable CS4014 
                MONITORING();
#pragma warning restore CS4014 
                return IsValid;

            }
            catch (Exception ex) { logFileWriter.WriteLog(EnumDeclaration.EnumLogType.Error, ex.ToString()); }
            return false;
        }


        #endregion


        #region NewSTGPortfolioADD
        public async Task<bool> PortfolioReEntry_STG(bool Overall_SL_HIT, bool Overall_TP_HIT, PortfolioModel Portfolio_value, string old_stg_key)
        {
            try
            {

                if (straddleDataBaseLoad.Master_Straddle_Dictionary is null) throw new Exception("straddleDataBaseLoad.Master_Straddle_Dictionary instance not created");
                if (straddleDataBaseLoad.Straddle_LegDetails_Dictionary is null) throw new Exception("straddleDataBaseLoad.Straddle_LegDetails_Dictionary instance not created");
                if (General.Portfolios is null) throw new Exception("General.Portfolios instance not created");
                if (PortfolioViewModel.StrategyDataCollection is null) throw new Exception(" PortfolioViewModel.StrategyDataCollection instance not created");

                string new_stg_key = OtherMethods.GetNewName(old_stg_key);
                //Clean up 
                if (old_stg_key.Contains('.'))
                    old_stg_key = old_stg_key.Split('.')[0];

                var clone_stg = straddleDataBaseLoad.Master_Straddle_Dictionary.ToDictionary(x => x.Key, x => x.Value);
                var clone_leg = straddleDataBaseLoad.Straddle_LegDetails_Dictionary.ToDictionary(x => x.Key, x => x.Value);

                var clone_stg_setting_value = clone_stg[old_stg_key];
                var clone_leg_value = clone_leg[old_stg_key];

                foreach(var cleanup in clone_leg_value.Keys)
                {
                    if (cleanup.Contains('.'))
                        clone_leg_value.Remove(cleanup, out LegDetails? value);
                }


                if (Overall_SL_HIT)
                    clone_stg_setting_value.OverallReEntryOnSL--;
                else if (Overall_TP_HIT)
                    clone_stg_setting_value.OverallReEntryOnTgt--;
                else
                    return false;

               

                if (straddleDataBaseLoad.Master_Straddle_Dictionary.ContainsKey(new_stg_key))
                {
                    logFileWriter.WriteLog(EnumLogType.Error, new_stg_key + "This key already added in Master_Straddle_Dictionary");
                }
                else
                {
                    straddleDataBaseLoad.Master_Straddle_Dictionary.TryAdd(new_stg_key, clone_stg_setting_value);
                }
                if (straddleDataBaseLoad.Straddle_LegDetails_Dictionary.ContainsKey(new_stg_key))
                {
                    logFileWriter.WriteLog(EnumLogType.Error, new_stg_key + "This key already added in Straddle_LegDetails_Dictionary");
                }
                else
                {
                    straddleDataBaseLoad.Straddle_LegDetails_Dictionary.TryAdd(new_stg_key, clone_leg_value);
                }

                PortfolioModel portfolioModel = new()
                {
                    Name = new_stg_key,
                    Index = clone_stg_setting_value.Index,
                    EntryTime = DateTime.Now,
                    ExitTime = clone_stg_setting_value.ExitTime,
                    UserID = clone_stg_setting_value.UserID
                };
                portfolioModel.InnerObject ??= new();

                if (!General.Portfolios.ContainsKey(portfolioModel.Name))
                {
                    General.Portfolios.TryAdd(portfolioModel.Name, portfolioModel);
                }

                foreach (var Leg in clone_leg_value.Keys)
                {
                    try
                    {
                        var leg_value = clone_leg_value[Leg];


                        //Rev.. the Position
                        if (((clone_stg_setting_value.SettingOverallReEntryOnSL == EnumOverallReEntryOnSL.REREVASAP || clone_stg_setting_value.SettingOverallReEntryOnSL == EnumOverallReEntryOnSL.REREVMOMENTUM) && Overall_SL_HIT) ||
                            ((clone_stg_setting_value.SettingOverallReEntryOnTgt == EnumOverallReEntryOnTarget.REREVASAP || clone_stg_setting_value.SettingOverallReEntryOnTgt == EnumOverallReEntryOnTarget.REREVMOMENTUM) && Overall_TP_HIT))
                        {
                            leg_value.Position = leg_value.Position == EnumPosition.BUY ? EnumPosition.SELL : EnumPosition.BUY;
                        }
                        //--------------------------------------------------------------------------------------------------------

                        InnerObject innerObject = new()
                        {
                            StgName = new_stg_key,
                            Name = Leg,
                            BuySell = leg_value.Position,
                            TradingSymbol = "Loading ...",
                            Qty = leg_value.Lots,
                            enumUnderlyingFromForLeg = clone_stg_setting_value.UnderlyingFrom
                        };
                        await Application.Current.Dispatcher.BeginInvoke(new Action(() => { portfolioModel.InnerObject.Add(innerObject); }), DispatcherPriority.Background, null);
                        General.Portfolios.TryUpdate(portfolioModel.Name, portfolioModel, General.Portfolios[portfolioModel.Name]);
                    }
                    catch (Exception ex)
                    {
                        logFileWriter.WriteLog(EnumLogType.Error, ex.ToString());
                    }
                }
                await Application.Current.Dispatcher.BeginInvoke(new Action(() => { PortfolioViewModel.StrategyDataCollection.Add(portfolioModel); }), DispatcherPriority.Background, null);

                double TotalPremium = 0;
                List<Task> tasks = new();
                foreach (string Leg in clone_leg_value.Keys)
                {

                    try
                    {
                        Task t1 = new(() => PortfolioNew_LEG(Leg, new_stg_key, ref TotalPremium));
                        t1.Start();
                        tasks.Add(t1);

                    }
                    catch { }
                }
                Task.WaitAll(tasks.ToArray());

                portfolioModel.TotalEntryPremiumPaid = TotalPremium;
                if (clone_stg_setting_value.IsOverallStopLossEnable)
                {
                    portfolioModel.StopLoss = Math.Round(algoCalculation.GetOverallStopLossValue(TotalPremium,
                                                                                                    clone_stg_setting_value.SettingOverallStopLoss,
                                                                                                    clone_stg_setting_value.OverallStopLoss)
                                                                                                     , 2);

                    portfolioModel.ReEntrySL = clone_stg_setting_value.OverallReEntryOnSL;
                }
                if (clone_stg_setting_value.IsOverallTargetEnable)
                {
                    portfolioModel.TargetProfit = Math.Round(algoCalculation.GetOverallTargetProfitValue(TotalPremium,
                                                                                                   clone_stg_setting_value.SettingOverallTarget,
                                                                                                   clone_stg_setting_value.OverallTarget)
                                                                                                    , 2);

                    portfolioModel.ReEntryTP = clone_stg_setting_value.OverallReEntryOnTgt;
                }
                return true;
            }
            catch (Exception ex)
            {
                logFileWriter.WriteLog(EnumLogType.Error, log: ex.Message + "  " + ex.StackTrace);
                return false;
            }
            finally { }
        }

        #endregion

        #region LEG FIRST ENTRY
        public void PortfolioNew_LEG(string Leg, string new_stg_key, ref double TotalPremium)
        {

            if (straddleDataBaseLoad.Master_Straddle_Dictionary is null) throw new Exception("straddleDataBaseLoad.Master_Straddle_Dictionary instance not created");
            if (straddleDataBaseLoad.Straddle_LegDetails_Dictionary is null) throw new Exception("straddleDataBaseLoad.Straddle_LegDetails_Dictionary instance not created");
            if (General.Portfolios is null) throw new Exception("General.Portfolios instance not created");
            if (PortfolioViewModel.StrategyDataCollection is null) throw new Exception(" PortfolioViewModel.StrategyDataCollection instance not created");
            if (General.PortfolioLegByTokens is null) throw new Exception(" PortfolioViewModel.StrategyDataCollection instance not created");

            var stg_setting_value = straddleDataBaseLoad.Master_Straddle_Dictionary[new_stg_key];
            var Portfolio_value = General.Portfolios[new_stg_key];
            var leg_value = straddleDataBaseLoad.Straddle_LegDetails_Dictionary[new_stg_key];
            var leg_Details = leg_value[Leg];
            var portfolio_leg_value = Portfolio_value.InnerObject.Where(xxx => xxx.Name == Leg).FirstOrDefault() ?? throw new Exception("Leg was not Loaded in GUI or Portfolios.");
            try
            {


                DateTime Expiry = algoCalculation.GetLegExpiry(leg_Details.Expiry,
                                                                                      stg_setting_value.Index,
                                                                                      leg_Details.SelectSegment,
                                                                                      leg_Details.OptionType);


                double StrikeForLeg = EnumSegments.OPTIONS == leg_Details.SelectSegment ? algoCalculation.GetStrike(leg_Details.StrikeCriteria,
                                                                   leg_Details.StrikeType,
                                                                   leg_Details.PremiumRangeLower,
                                                                   leg_Details.PremiumRangeUpper,
                                                                   leg_Details.Premium_or_StraddleWidth,
                                                                   stg_setting_value.Index,
                                                                   stg_setting_value.UnderlyingFrom,
                                                                   leg_Details.SelectSegment,
                                                                   leg_Details.Expiry,
                                                                   leg_Details.OptionType,
                                                                   leg_Details.Position) :
                                                                   -0.01;

                uint Token = EnumSegments.OPTIONS == leg_Details.SelectSegment ? ContractDetails.GetTokenByContractValue(Expiry, leg_Details.OptionType, stg_setting_value.Index, StrikeForLeg) : ContractDetails.GetTokenByContractValue(Expiry, EnumOptiontype.XX, stg_setting_value.Index);
                string TradingSymbol = ContractDetails.GetContractDetailsByToken(Token).TrdSymbol ?? throw new Exception("for " + Token + " Trading Symbol was not Found in Contract Details.");



                portfolio_leg_value.Qty *= (int)ContractDetails.GetContractDetailsByToken(Token).LotSize;
                //Porfolio leg Update
                portfolio_leg_value.Token = Token;
                portfolio_leg_value.TradingSymbol = TradingSymbol;
                portfolio_leg_value.ReEntrySL = leg_Details.ReEntryOnSL;
                portfolio_leg_value.ReEntryTP = leg_Details.ReEntryOnTgt;




                //Simple Movement or RanageBreak Out Enable

                if (leg_Details.IsSimpleMomentumEnable == true && leg_Details.IsRangeBreakOutEnable == true)
                    throw new Exception("Simple Momentum and Range Break Out both are Enable");


                double ORBPrice_OR_SimpleMovementum = 0;

                if (leg_Details.IsSimpleMomentumEnable && ((stg_setting_value.SettingOverallReEntryOnTgt == EnumOverallReEntryOnTarget.REREVMOMENTUM || stg_setting_value.SettingOverallReEntryOnTgt == EnumOverallReEntryOnTarget.REREVMOMENTUM) || (stg_setting_value.SettingOverallReEntryOnTgt == EnumOverallReEntryOnTarget.REMOMENTUM || stg_setting_value.SettingOverallReEntryOnTgt == EnumOverallReEntryOnTarget.REREVMOMENTUM)))
                {
                    ORBPrice_OR_SimpleMovementum = algoCalculation.GetLegMomentumlock(leg_Details.SettingSimpleMomentum,
                                                                               leg_Details.SimpleMomentum,
                                                                               stg_setting_value.Index,
                                                                               Token, portfolio_leg_value).Result;
                }

                if (leg_Details.IsStopLossEnable == true)
                {
                    portfolio_leg_value.StopLoss = Math.Round(algoCalculation.GetLegStopLoss(leg_Details.SettingStopLoss,
                                                                                    leg_Details.OptionType,
                                                                                    leg_Details.Position,
                                                                                    leg_Details.StopLoss,
                                                                                    leg_Details.SelectSegment,
                                                                                    stg_setting_value.Index,
                                                                                    Token, portfolio_leg_value), 2);
                }

                if (leg_Details.IsTargetProfitEnable == true)
                {
                    portfolio_leg_value.TargetProfit = Math.Round(algoCalculation.GetLegTargetProfit(leg_Details.SettingTargetProfit,
                                                                                                        leg_Details.OptionType,
                                                                                                        leg_Details.Position,
                                                                                                        leg_Details.TargetProfit,
                                                                                                        leg_Details.SelectSegment,
                                                                                                        stg_setting_value.Index,
                                                                                                        Token, portfolio_leg_value), 2);
                }
                double _currentLTP = algoCalculation.GetStrikePriceLTP(Token);




                if (!portfolio_leg_value.IsLegCancelledOrRejected && !Portfolio_value.IsSTGCompleted)
                {
                    int OrderID = OrderManagerModel.GetOrderId();
                    OrderManagerModel.Portfolio_Dicc_By_ClientID.TryAdd(OrderID, portfolio_leg_value);
                    LoginViewModel.NNAPIRequest.PlaceOrderRequest((int)Token, price1: _currentLTP, orderQty: portfolio_leg_value.Qty,
                         Buysell: portfolio_leg_value.BuySell, OrderType.LIMIT, 0, OrderID);
                    portfolio_leg_value.Entry_OrderID = OrderID;
                    portfolio_leg_value.EntryPrice = _currentLTP;
                    portfolio_leg_value.Status = EnumStrategyStatus.ENTRY_ADDED;
                    portfolio_leg_value.EntryTime = DateTime.Now;
                    logFileWriter.DisplayLog(EnumLogType.Info, $"Order ID {OrderID} Entry Postion Mapped with LEG: {portfolio_leg_value.Name}  OF STG {new_stg_key}");

                    TotalPremium += (portfolio_leg_value.Qty * portfolio_leg_value.EntryPrice);
                }
                else
                {
                    portfolio_leg_value.Status = EnumStrategyStatus.REJECTED;
                    portfolio_leg_value.Message = EnumStrategyMessage.ORDER_CANCELLED_BY_SYSTEM;

                }
                General.PortfolioLegByTokens.AddOrUpdate(Token, new List<InnerObject>() { portfolio_leg_value }, (key, list) =>
                {
                    list.Add(portfolio_leg_value);
                    return list;
                });
            }
            catch (Exception ex)
            {
                portfolio_leg_value.IsLegCompleted = true;
                portfolio_leg_value.Status = EnumStrategyStatus.REJECTED;
                logFileWriter.WriteLog(EnumLogType.Error, ex.ToString());
            }
            finally { }
        }
        #endregion


        #region Leg RE-ENTRY Place
        public async Task<bool> PortfolioLegReEntry(bool SL_HIT, bool TP_HIT, InnerObject portfolio_leg_value, LegDetails leg_Details, StrategyDetails stg_setting_value, PortfolioModel Portfolio_value, ConcurrentDictionary<string, LegDetails> leg_value)
        {
            // bool statusofReEntry = false;
            InnerObject? innerObject = null;
            try
            {

                #region GET SL RE ENTRY
                if (leg_Details.IsReEntryOnSLEnable == true && SL_HIT)
                {
                    if (portfolio_leg_value.ReEntrySL > 0)
                    {
                        portfolio_leg_value.ReEntrySL--;
                        innerObject = algoCalculation.GetLegDetailsForRentry_SLHIT(leg_Details, portfolio_leg_value, stg_setting_value);
                    }
                }
                #endregion

                #region GET TP RE ENTRY
                if (leg_Details.IsReEntryOnTgtEnable == true && TP_HIT)
                {
                    if (portfolio_leg_value.ReEntryTP > 0)
                    {
                        portfolio_leg_value.ReEntryTP--;
                        innerObject = algoCalculation.GetLegDetailsForRentry_TPHIT(leg_Details, portfolio_leg_value, stg_setting_value);
                    }
                }
                #endregion

                #region ADD NEW LEG ENTRY
                if (innerObject is not null)
                {
                    innerObject.IsLegInMonitoringQue = true;
                    if (General.PortfolioLegByTokens is null) throw new Exception("General PortfolioLegByToken instance is not created.");
                    if (General.Portfolios is null) throw new Exception("General Portfolios instance is not created.");

                    leg_value.TryAdd(innerObject.Name, leg_Details);
                    General.PortfolioLegByTokens.AddOrUpdate(innerObject.Token, new List<InnerObject>() { innerObject }, (key, list) =>
                    {
                        list.Add(innerObject);
                        return list;
                    });

                    await Application.Current.Dispatcher.BeginInvoke(new Action(() => { Portfolio_value.InnerObject.Add(innerObject); }
                    ), DispatcherPriority.Background, null);

                    General.Portfolios.TryUpdate(Portfolio_value.Name, Portfolio_value, General.Portfolios[Portfolio_value.Name]);

                    int OrderID = OrderManagerModel.GetOrderId();
                    OrderManagerModel.Portfolio_Dicc_By_ClientID.TryAdd(OrderID, innerObject);
                    innerObject.Entry_OrderID = OrderID;
                    if (leg_Details.SettingReEntryOnSL == EnumLegReEntryOnSL.RECOST || leg_Details.SettingReEntryOnSL == EnumLegReEntryOnSL.REREVCOST
                    || leg_Details.SettingReEntryOnTgt == EnumLegReEntryOnTarget.RECOST || leg_Details.SettingReEntryOnTgt == EnumLegReEntryOnTarget.REREVCOST)
                    {
                        innerObject.Message = EnumStrategyMessage.RE_ENTRY;
                        var data = await algoCalculation.IsMyPriceHITforCost(SL_HIT, TP_HIT, innerObject.EntryPrice, innerObject.Token, innerObject.IsLegCancelledOrRejected);
                        if (data == true)
                        {
                            if (!innerObject.IsLegCancelledOrRejected && !Portfolio_value.IsSTGCompleted)
                            {
                                LoginViewModel.NNAPIRequest.PlaceOrderRequest((int)innerObject.Token, price1: innerObject.EntryPrice, orderQty: innerObject.Qty,
                            Buysell: innerObject.BuySell, OrderType.LIMIT, 0, OrderID);
                                innerObject.Status = EnumStrategyStatus.ENTRY_ADDED;

                            }
                            else
                            {
                                innerObject.IsLegCompleted = true;
                                innerObject.Status = EnumStrategyStatus.REJECTED;
                            }

                        }
                    }
                    else if (leg_Details.SettingReEntryOnSL == EnumLegReEntryOnSL.REASAP || leg_Details.SettingReEntryOnSL == EnumLegReEntryOnSL.REREVASAP
                    || leg_Details.SettingReEntryOnTgt == EnumLegReEntryOnTarget.REASAP || leg_Details.SettingReEntryOnTgt == EnumLegReEntryOnTarget.REREVASAP)
                    {
                        if (!innerObject.IsLegCancelledOrRejected && !Portfolio_value.IsSTGCompleted)
                        {
                            LoginViewModel.NNAPIRequest.PlaceOrderRequest((int)innerObject.Token, price1: innerObject.EntryPrice, orderQty: innerObject.Qty,
                             Buysell: innerObject.BuySell, OrderType.LIMIT, 0, OrderID);
                            innerObject.Status = EnumStrategyStatus.ENTRY_ADDED;
                        }
                        else
                        {
                            innerObject.IsLegCompleted = true;
                            innerObject.Status = EnumStrategyStatus.REJECTED;
                        }

                    }
                    else if (leg_Details.SettingReEntryOnSL == EnumLegReEntryOnSL.REMOMENTUM || leg_Details.SettingReEntryOnSL == EnumLegReEntryOnSL.REREVMOMENTUM
                    || leg_Details.SettingReEntryOnTgt == EnumLegReEntryOnTarget.REMOMENTUM || leg_Details.SettingReEntryOnTgt == EnumLegReEntryOnTarget.REREVMOMENTUM)
                    {
                        if (leg_Details.IsSimpleMomentumEnable == false)
                        {
                            if (!innerObject.IsLegCancelledOrRejected && !Portfolio_value.IsSTGCompleted)
                            {
                                LoginViewModel.NNAPIRequest.PlaceOrderRequest((int)innerObject.Token, price1: innerObject.EntryPrice, orderQty: innerObject.Qty,
                            Buysell: innerObject.BuySell, OrderType.LIMIT, 0, OrderID);
                                innerObject.Status = EnumStrategyStatus.ENTRY_ADDED;
                            }
                            else
                            {
                                innerObject.IsLegCompleted = true;
                                innerObject.Status = EnumStrategyStatus.REJECTED;
                            }

                        }
                        else
                        {
                            innerObject = algoCalculation.IsSimpleMovementumHitForRentry(innerObject, leg_Details, stg_setting_value);

                            if (!innerObject.IsLegCancelledOrRejected && !Portfolio_value.IsSTGCompleted)
                            {
                                LoginViewModel.NNAPIRequest.PlaceOrderRequest((int)innerObject.Token, price1: innerObject.EntryPrice, orderQty: innerObject.Qty,
                        Buysell: innerObject.BuySell, OrderType.LIMIT, 0, OrderID);
                                innerObject.Status = EnumStrategyStatus.ENTRY_ADDED;
                            }
                            else
                            {
                                innerObject.IsLegCompleted = true;
                                innerObject.Status = EnumStrategyStatus.REJECTED;
                            }
                        }
                    }
                    innerObject.EntryTime = DateTime.Now;
                    General.PortfolioLegByTokens.AddOrUpdate(innerObject.Token, new List<InnerObject>() { innerObject }, (key, list) =>
                    {
                        list.Add(innerObject);
                        return list;
                    });
                    innerObject.IsLegInMonitoringQue = false;
                }
                #endregion
                //statusofReEntry = true;

            }
            catch (Exception ex)
            {
                // statusofReEntry = false;
                logFileWriter.DisplayLog(EnumLogType.Error, Log: ex.ToString());
                return false;
            }
            finally
            {
                //if (statusofReEntry)
                //    logFileWriter.DisplayLog(EnumLogType.Info, $"[RE ENTRY] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");
                //else
                //    logFileWriter.DisplayLog(EnumLogType.Warning, $"[RE ENTRY: FAILED] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");
                innerObject.IsLegInMonitoringQue = false;
            }
            return true;
        }
        #endregion

        #region SQUARE OFF
        #region STG Square OFF
        /// <summary>
        /// Square off Stg. NO CHECK APPLIED Check are in Leg Square of function
        /// </summary>
        /// <param name="PM"></param>
        /// <param name="enumStrategyMessage"></param>
        /// <returns></returns>
        public async Task<bool> SquareOffStraddle920(PortfolioModel PM, EnumStrategyMessage enumStrategyMessage = EnumStrategyMessage.NONE)
        {
            try
            {
                var _totalLeg = PM.InnerObject;
                foreach (var leg in _totalLeg)
                {
                    if (leg.ExitPrice == 0)
                    {
                        if (enumStrategyMessage == EnumStrategyMessage.NONE)
                            await SquareOffStraddle920Leg(portfolio_leg_value: leg);
                        else
                            await SquareOffStraddle920Leg(portfolio_leg_value: leg, enumStrategyMessage);

                        await Task.Delay(50);
                    }
                }
            }
            catch (Exception ex)
            {
                logFileWriter.DisplayLog(EnumLogType.Error, Log: ex.StackTrace + ex.Message.ToString());
                return false;
            }

            return true;
        }
        #endregion

        #region Leg Square OFF
        /// <summary>
        /// Square of the Leg Based on the LEG STATUS
        /// </summary>
        /// <param name="portfolio_leg_value"></param>
        /// <param name="enumStrategyMessage"></param>
        /// <returns></returns>
        private async Task<bool> SquareOffStraddle920Leg(InnerObject portfolio_leg_value, EnumStrategyMessage enumStrategyMessage = EnumStrategyMessage.NONE)
        {
            await Task.Delay(10);
            if (portfolio_leg_value.ExitPrice == 0)
            {
                try
                {
                    if (portfolio_leg_value.Status == EnumStrategyStatus.ENTRY_PARTIALLY_TRADED)
                    {
                        //GET ADMIN ORDER ID AND SEND CANCEL REQUEST.
                        //CHECK TOTAL TRADED QUANTIY.
                        //SEND EXIT FOR SAME QUANTIY.
                        portfolio_leg_value.IsLegCompleted = true;
                        var details = OrderManagerModel.OrderBook_Dicc_By_ClientID.Where(xx => xx.Value.ClientID == portfolio_leg_value.Entry_OrderID).FirstOrDefault();
                        if (details.Key is not 0)
                        {
                            if (details.Value.Status.Contains("open"))
                            {
                                LoginViewModel.NNAPIRequest.CancelOrderRequest(details.Key);
                            }
                        }

                    }
                    else if (portfolio_leg_value.Status == EnumStrategyStatus.NONE)
                    {
                        portfolio_leg_value.IsLegCompleted = true;
                        portfolio_leg_value.IsLegCancelledOrRejected = true;// so new order will not be placed if the Order in Placing QUE.
                    }
                    else if (portfolio_leg_value.Status == EnumStrategyStatus.ENTRY_ADDED)
                    {
                        try
                        {
                            portfolio_leg_value.IsLegCompleted = true;
                            var details = OrderManagerModel.OrderBook_Dicc_By_ClientID.Where(xx => xx.Value.ClientID == portfolio_leg_value.Entry_OrderID).FirstOrDefault();
                            if (details.Key is not 0)
                            {
                                if ((details.Value.Status.Contains("open") || details.Value.Status.Contains("put order request")) && !details.Value.Status.Contains("cancel"))
                                {
                                    LoginViewModel.NNAPIRequest.CancelOrderRequest(details.Key);
                                }
                                else
                                {
                                    logFileWriter.DisplayLog(EnumLogType.Error, $"Can't Send Cancel Request For Open Orded MOD ID: {details.Key} and Client ID: {portfolio_leg_value.Entry_OrderID}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logFileWriter.DisplayLog(EnumLogType.Error, " Unable to find Moderator ID: " + portfolio_leg_value.Token + ex.Message + ex.StackTrace);
                        }
                    }
                    else if (portfolio_leg_value.Status == EnumStrategyStatus.RUNING)
                    {
                        portfolio_leg_value.IsLegCompleted = true;
                        double _currentLTP = algoCalculation.GetStrikePriceLTP(portfolio_leg_value.Token);
                        EnumPosition enumPosition = portfolio_leg_value.BuySell == EnumPosition.BUY ? EnumPosition.SELL : EnumPosition.BUY;
                        int OrderID = OrderManagerModel.GetOrderId();//Get the client unique ID
                        OrderManagerModel.Portfolio_Dicc_By_ClientID.TryAdd(OrderID, portfolio_leg_value);
                        LoginViewModel.NNAPIRequest.PlaceOrderRequest((int)portfolio_leg_value.Token, price1: _currentLTP, orderQty: portfolio_leg_value.Qty,
                             Buysell: enumPosition, OrderType.LIMIT, 0, OrderID);
                        portfolio_leg_value.ExitPrice = _currentLTP;
                        portfolio_leg_value.ExitTime = DateTime.Now;
                        portfolio_leg_value.Status = EnumStrategyStatus.EXIT_ADDED;
                        portfolio_leg_value.Exit_OrderID = OrderID;
                        logFileWriter.DisplayLog(EnumLogType.Info, $"Order ID {OrderID} EXIT Postion Mapped with LEG: {portfolio_leg_value.Name}  OF STG {portfolio_leg_value.StgName}");
                        if (enumStrategyMessage != EnumStrategyMessage.NONE)
                        {
                            portfolio_leg_value.Message = enumStrategyMessage;
                        }
                    }
                    else
                    {
                        portfolio_leg_value.IsLegCompleted = true;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    logFileWriter.DisplayLog(EnumLogType.Error, " Something went wrong while Squared off the Leg with TOKEN: " + portfolio_leg_value.Token + ex.Message + ex.StackTrace);
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }


        #endregion
        #endregion


        #region WATCHER

        public async Task MONITORING()
        {
            while (true)
            {
                try
                {
                    var Stg_status = await PortfolioMonitor_STG();
                }
                catch(Exception ex)
                {
                    logFileWriter.WriteLog(EnumLogType.Error, ex.Message + ex.StackTrace);
                }
                finally
                {
                   
                    await Task.Delay(50);
                }
            }
        }
        #region PortfolioMonitor 
        public async Task<bool> PortfolioMonitor_STG()
        {
            try
            {
                if (straddleDataBaseLoad.Master_Straddle_Dictionary == null)
                    throw new Exception("Master STG is Empty Function from MonitoringThread.");

                if (straddleDataBaseLoad.Straddle_LegDetails_Dictionary == null)
                    throw new Exception("Master LEG is Empty Function from MonitoringThread.");

                if (General.Portfolios == null)
                    throw new Exception("General Portfolio is Empty Function from MonitoringThread.");

                foreach (string stg_key in straddleDataBaseLoad.Master_Straddle_Dictionary.Keys)
                {
                    var stg_setting_value = straddleDataBaseLoad.Master_Straddle_Dictionary[stg_key];
                    var Portfolio_value = General.Portfolios[stg_key];
                    var leg_value = straddleDataBaseLoad.Straddle_LegDetails_Dictionary[stg_key];

                    //if square of then no need to check SL AND TP OR RE-ENTRY
                    if (Portfolio_value.PNL != 0 && !Portfolio_value.IsSTGCompleted)
                    {
                        #region Check Square off Time
                        if (stg_setting_value.EntryAndExitSetting == EnumEntryAndExit.TIMEBASED)
                        {
                            int SquareofSeconds = (int)(stg_setting_value.ExitTime - DateTime.Now).TotalSeconds;
                            if (SquareofSeconds <= 0)
                            {
                                var status = await SquareOffStraddle920(Portfolio_value, EnumStrategyMessage.SYSTEM_SQUAREOFF);
                                if (status)
                                    logFileWriter.DisplayLog(EnumLogType.Info, $"[SQUARE OFF BY TIME] STG {Portfolio_value.Name}");
                                else
                                    logFileWriter.DisplayLog(EnumLogType.Warning, $"[SQUARE OFF BY TIME: FAILED] STG {Portfolio_value.Name}");
                                Portfolio_value.IsSTGCompleted = true;
                                return true;
                            }
                        }
                        #endregion

                        #region Trailing Options Check and Update

                        if (stg_setting_value.IsOverallTrallingOptionEnable == true)
                        {
                            algoCalculation.CheckAndUpdateOverallTrailingOption(Portfolio_value, stg_setting_value);
                        }

                        #endregion

                        #region SL AND TP HIT AND REENTRY
                        bool Overall_SL_HIT = false, Overall_TP_HIT = false;
                        if (stg_setting_value.IsOverallStopLossEnable == true)// && Portfolio_value.StopLoss != 0)
                        {
                            Overall_SL_HIT = algoCalculation.Is_overall_sl_hit(stg_setting_value, Portfolio_value);
                            if (Overall_SL_HIT)
                            {
                                Portfolio_value.IsSTGCompleted = true;
                                logFileWriter.DisplayLog(EnumLogType.Info, "[SL HIT]  STG: " + stg_key);
                                await SquareOffStraddle920(Portfolio_value, EnumStrategyMessage.OVERALL_SL_HIT);
                            }

                        }
                        if (stg_setting_value.IsOverallReEntryOnTgtEnable == true)// && Portfolio_value.TargetProfit != 0)
                        {

                            Overall_TP_HIT = algoCalculation.Is_overall_tp_hit(stg_setting_value, Portfolio_value);
                            if (Overall_TP_HIT)
                            {
                                Portfolio_value.IsSTGCompleted = true;
                                logFileWriter.DisplayLog(EnumLogType.Info, "[TP HIT] STG: " + stg_key);
                                await SquareOffStraddle920(Portfolio_value, EnumStrategyMessage.OVERALL_TP_HIT);
                            }
                        }

                        if ((Overall_SL_HIT && Portfolio_value.ReEntrySL > 0) || Overall_TP_HIT && Portfolio_value.ReEntryTP > 0)
                        {
                            await PortfolioReEntry_STG(Overall_SL_HIT, Overall_TP_HIT, Portfolio_value, stg_key);
                        }
                        #endregion

                        #region LEG WATCH
                        //IF HIT DO NOT MONITOR LEG
                        if (!Overall_SL_HIT && !Overall_TP_HIT)
                        {
                            foreach (string Leg in leg_value.Keys)
                            {
#pragma warning disable CS4014
                                PortfolioMonitor_LEG(Leg, stg_key); // Cause not awaited call //TASK FOR INDIVISUALE LEG
#pragma warning restore CS4014
                            }
                        }
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                logFileWriter.DisplayLog(EnumLogType.Error, Log: "Error In PortfolioMonitor_STG()" + ex.ToString());
                return false;
            }
            await Task.Delay(1000);
            return true;
        }
        #endregion


        #region LegMonitoring
        public async Task<bool> PortfolioMonitor_LEG(string Leg, string stg_key)
        {
            try
            {
                if (straddleDataBaseLoad.Master_Straddle_Dictionary == null)
                    throw new Exception("Master STG is Empty Function from MonitoringThread.");

                if (straddleDataBaseLoad.Straddle_LegDetails_Dictionary == null)
                    throw new Exception("Master LEG is Empty Function from MonitoringThread.");

                if (General.Portfolios == null)
                    throw new Exception("General Portfolio is Empty Function from MonitoringThread.");

                var stg_setting_value = straddleDataBaseLoad.Master_Straddle_Dictionary[stg_key];
                var Portfolio_value = General.Portfolios[stg_key];
                var leg_value = straddleDataBaseLoad.Straddle_LegDetails_Dictionary[stg_key];
                var leg_Details = leg_value[Leg];
                var portfolio_leg_value = Portfolio_value.InnerObject.Where(xxx => xxx.Name == Leg).FirstOrDefault() ?? throw new Exception("Leg was not Loaded in GUI or Portfolios.");
                if (portfolio_leg_value.ExitPrice == 0 && (portfolio_leg_value.Status == EnumStrategyStatus.RUNING ||
                               portfolio_leg_value.Status == EnumStrategyStatus.ENTRY_PARTIALLY_TRADED)
                               && !portfolio_leg_value.IsLegInMonitoringQue && !portfolio_leg_value.IsLegCompleted)
                {
                    portfolio_leg_value.IsLegInMonitoringQue = true;

                    #region TRAIL SL FOR Leg Check and Update

                    if (leg_Details.IsTrailSlEnable == true)
                    {
                        algoCalculation.UpdateLegSLTrail_IF_HIT(portfolio_leg_value, leg_Details, stg_setting_value);
                    }
                    #endregion

                    #region Check SL for LEG
                    bool SL_HIT = false;
                    if (leg_Details.IsStopLossEnable == true)
                    {
                        SL_HIT = algoCalculation.Get_if_SL_is_HIT(portfolio_leg_value.StopLoss,
                                                                        leg_Details.SettingStopLoss,
                                                                        leg_Details.OptionType,
                                                                        leg_Details.Position,
                                                                        stg_setting_value.Index,
                                                                        portfolio_leg_value.Token,
                                                                        stg_setting_value.UnderlyingFrom);

                        if (SL_HIT)
                        {
                            portfolio_leg_value.IsLegCompleted = true;
                            logFileWriter.DisplayLog(EnumLogType.Info, "[SL HIT] Leg :" + Leg + " Stg: " + stg_key);
                        }
                    }
                    #endregion

                    #region Check TP for LEG
                    bool TP_HIT = false;
                    if (leg_Details.IsTargetProfitEnable == true)
                    {
                        TP_HIT = algoCalculation.Get_if_TP_is_HIT(portfolio_leg_value.TargetProfit,
                                                                        leg_Details.SettingStopLoss,
                                                                        leg_Details.OptionType,
                                                                        leg_Details.Position,
                                                                        stg_setting_value.Index,
                                                                        portfolio_leg_value.Token,
                                                                        stg_setting_value.UnderlyingFrom);
                        if (TP_HIT)
                        {
                            portfolio_leg_value.IsLegCompleted = true;
                            logFileWriter.DisplayLog(EnumLogType.Info, "[TP HIT] Leg :" + Leg + "Stg: " + stg_key);
                        }
                    }
                    #endregion

                    #region LEG SQUARE OFF

                    if (stg_setting_value.SquareOff == EnumSquareOff.PARTIAL && (SL_HIT || TP_HIT))
                    {
                        if (SL_HIT)
                        {
                            var status = await SquareOffStraddle920Leg(portfolio_leg_value, EnumStrategyMessage.LEG_SL_HIT);
                            if (status)
                                logFileWriter.DisplayLog(EnumLogType.Info, $"[SQUARE OFF BY SL HIT] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");
                            else
                                logFileWriter.DisplayLog(EnumLogType.Warning, $"[SQUARE OFF BY SL HIT: FAILED] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");
                        }
                        if (TP_HIT)
                        {
                            var status = await SquareOffStraddle920Leg(portfolio_leg_value, EnumStrategyMessage.LEG_TP_HIT);
                            if (status)
                                logFileWriter.DisplayLog(EnumLogType.Info, $"[SQUARE OFF BY TP HIT] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");
                            else
                                logFileWriter.DisplayLog(EnumLogType.Warning, $"[SQUARE OFF BY TP HIT: FAILED] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");
                        }

                        bool statusofReEntry = await PortfolioLegReEntry(SL_HIT, TP_HIT, portfolio_leg_value, leg_Details, stg_setting_value, Portfolio_value, leg_value);
                        if (statusofReEntry)
                            logFileWriter.DisplayLog(EnumLogType.Info, $"[RE ENTRY] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");
                        else
                            logFileWriter.DisplayLog(EnumLogType.Warning, $"[RE ENTRY: FAILED] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");
                    }
                    else if (stg_setting_value.SquareOff == EnumSquareOff.COMPLETE && (SL_HIT || TP_HIT))
                    {
                        foreach (string ChildLeg in leg_value.Keys)
                        {
                            var child_leg_Details = leg_value[Leg];
                            var child_portfolio_leg_value = Portfolio_value.InnerObject.Where(xxx => xxx.Name == ChildLeg).FirstOrDefault() ?? throw new Exception("Leg was not Loaded in GUI or Portfolios.");
                            child_portfolio_leg_value.IsLegCompleted = true;
                            if (child_portfolio_leg_value.ExitPrice == 0)
                            {
                                if (child_portfolio_leg_value.Status == EnumStrategyStatus.RUNING || child_portfolio_leg_value.Status == EnumStrategyStatus.ENTRY_PARTIALLY_TRADED)
                                {
                                    if (SL_HIT)
                                    {
                                        var status = await SquareOffStraddle920Leg(portfolio_leg_value, EnumStrategyMessage.LEG_SL_HIT);
                                        if (status)
                                            logFileWriter.DisplayLog(EnumLogType.Info, $"[SQUARE OFF BY SL HIT ALL LEG SQUAREOFF] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");
                                        else
                                            logFileWriter.DisplayLog(EnumLogType.Warning, $"[SQUARE OFF BY SL HITALL LEG SQUAREOFF : FAILED] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");
                                    }
                                    if (TP_HIT)
                                    {
                                        var status = await SquareOffStraddle920Leg(portfolio_leg_value, EnumStrategyMessage.LEG_TP_HIT);
                                        if (status)
                                            logFileWriter.DisplayLog(EnumLogType.Info, $"[SQUARE OFF BY TP HIT ALL LEG SQUAREOFF] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");
                                        else
                                            logFileWriter.DisplayLog(EnumLogType.Warning, $"[SQUARE OFF BY TP HIT ALL LEG SQUAREOFF: FAILED] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");
                                    }
                                }
                                bool statusofReEntry = await PortfolioLegReEntry(SL_HIT, TP_HIT, child_portfolio_leg_value, child_leg_Details, stg_setting_value, Portfolio_value, leg_value);
                                if (statusofReEntry)
                                    logFileWriter.DisplayLog(EnumLogType.Info, $"[RE ENTRY] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");
                                else
                                    logFileWriter.DisplayLog(EnumLogType.Warning, $"[RE ENTRY: FAILED] STG {Portfolio_value.Name} LEG {portfolio_leg_value.Name}");

                            }
                        }
                    }
                    #endregion

                    portfolio_leg_value.IsLegInMonitoringQue = false;
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        #endregion
        #endregion


        #endregion

        #region Methods & const.
        public StraddleManager(IStraddleDataBaseLoadFromCsv straddleDataBaseLoad,
            ILogFileWriter logFileWriter,
            IAlgoCalculation algoCalculation
           )
        {
            General.Portfolios ??= new();
            General.PortfolioLegByTokens ??= new();
            this.straddleDataBaseLoad = straddleDataBaseLoad;
            this.logFileWriter = logFileWriter;
            this.algoCalculation = algoCalculation;
        }

        /// <summary>
        /// Data Update to ViewModel ()=>(ProtfolioViewModel) of Portfolio Screen
        /// </summary>
        /// <returns></returns>
        public async Task DataUpdateRequest()
        {
            General.PortfolioLegByTokens ??= new();
            if (straddleDataBaseLoad.Master_Straddle_Dictionary == null || straddleDataBaseLoad.Straddle_LegDetails_Dictionary == null)
                throw new Exception("Master dic not loadded posibility The Stg file not loaded.");

            try
            {
                foreach (string stg_key in straddleDataBaseLoad.Master_Straddle_Dictionary.Keys)
                {//ALL STG
                    double TotalPremium = 0;

                    var stgTask = Task.Factory.StartNew((Action)(async () =>
                    {
                        List<Task> tasks = new();
                        var stg_setting_value = straddleDataBaseLoad.Master_Straddle_Dictionary[stg_key];
                        var Portfolio_value = General.Portfolios[stg_key];
                        //waiting for Entry Time
                        if (stg_setting_value.EntryTime >= DateTime.Now)
                        {
                            int milisecond = (int)(stg_setting_value.EntryTime - DateTime.Now).TotalMilliseconds;
                            await Task.Delay(milisecond);



                            //
                            //GUI 
                            var leg_value = straddleDataBaseLoad.Straddle_LegDetails_Dictionary[stg_key];
                            foreach (string Leg in leg_value.Keys)
                            {//ALL LEG

                                var legTask = Task.Factory.StartNew(async () =>
                                {
                                    var leg_Details = leg_value[Leg];
                                    var portfolio_leg_value = Portfolio_value.InnerObject.Where(xxx => xxx.Name == Leg).FirstOrDefault() ?? throw new Exception("Leg was not Loaded in GUI or Portfolios.");
                                    try
                                    {

                                        //  GUIUpdateForLeg.Status = EnumDeclaration.EnumStrategyStatus.Waiting;


                                        //Get Trading Symbol and Token
                                        DateTime Expiry = algoCalculation.GetLegExpiry(leg_Details.Expiry,
                                                                                           stg_setting_value.Index,
                                                                                           leg_Details.SelectSegment,
                                                                                           leg_Details.OptionType);


                                        double StrikeForLeg = EnumSegments.OPTIONS == leg_Details.SelectSegment ? algoCalculation.GetStrike(leg_Details.StrikeCriteria,
                                                                                           leg_Details.StrikeType,
                                                                                           leg_Details.PremiumRangeLower,
                                                                                           leg_Details.PremiumRangeUpper,
                                                                                           leg_Details.Premium_or_StraddleWidth,
                                                                                           stg_setting_value.Index,//NIFTY/BANKNIFTY/FINNIFTY
                                                                                           stg_setting_value.UnderlyingFrom,
                                                                                           leg_Details.SelectSegment,
                                                                                           leg_Details.Expiry,
                                                                                           leg_Details.OptionType,
                                                                                           leg_Details.Position) :
                                                                                           -0.01;

                                        uint Token = EnumSegments.OPTIONS == leg_Details.SelectSegment ? ContractDetails.GetTokenByContractValue(Expiry, leg_Details.OptionType, stg_setting_value.Index, StrikeForLeg) :
                                    ContractDetails.GetTokenByContractValue(Expiry, EnumOptiontype.XX, stg_setting_value.Index);
                                        string TradingSymbol = ContractDetails.GetContractDetailsByToken(Token).TrdSymbol ?? throw new Exception("for " + Token + " Trading Symbol was not Found in Contract Details.");



                                        portfolio_leg_value.Qty *= (int)ContractDetails.GetContractDetailsByToken(Token).LotSize;
                                        //Porfolio leg Update
                                        portfolio_leg_value.Token = Token;
                                        portfolio_leg_value.TradingSymbol = TradingSymbol;
                                        portfolio_leg_value.ReEntrySL = leg_Details.ReEntryOnSL;
                                        portfolio_leg_value.ReEntryTP = leg_Details.ReEntryOnTgt;




                                        //Simple Movement or RanageBreak Out Enable

                                        if (leg_Details.IsSimpleMomentumEnable == true && leg_Details.IsRangeBreakOutEnable == true)
                                            throw new Exception("Simple Momentum and Range Break Out both are Enable");


                                        double ORBPrice_OR_SimpleMovementum = 0;
                                        if (leg_Details.IsRangeBreakOutEnable)
                                        {
                                            ORBPrice_OR_SimpleMovementum = await algoCalculation.GetRangeBreaKOut(leg_Details.SettingRangeBreakOut,
                                                                                      leg_Details.SettingRangeBreakOutType,
                                                                                      leg_Details.RangeBreakOutEndTime,
                                                                                      stg_setting_value.Index,
                                                                                      Token);
                                        }
                                        else if (leg_Details.IsSimpleMomentumEnable)
                                        {
                                            ORBPrice_OR_SimpleMovementum = await algoCalculation.GetLegMomentumlock(leg_Details.SettingSimpleMomentum,
                                                                                                       leg_Details.SimpleMomentum,
                                                                                                       stg_setting_value.Index,
                                                                                                       Token, portfolio_leg_value);
                                        }
                                        if (leg_Details.IsStopLossEnable == true)
                                        {
                                            portfolio_leg_value.StopLoss = Math.Round(algoCalculation.GetLegStopLoss(leg_Details.SettingStopLoss,
                                                                                                            leg_Details.OptionType,
                                                                                                            leg_Details.Position,
                                                                                                            leg_Details.StopLoss,
                                                                                                            leg_Details.SelectSegment,
                                                                                                            stg_setting_value.Index,
                                                                                                            Token, portfolio_leg_value), 2);
                                        }

                                        if (leg_Details.IsTargetProfitEnable == true)
                                        {
                                            portfolio_leg_value.TargetProfit = Math.Round(algoCalculation.GetLegTargetProfit(leg_Details.SettingTargetProfit,
                                                                                                                                leg_Details.OptionType,
                                                                                                                                leg_Details.Position,
                                                                                                                                leg_Details.TargetProfit,
                                                                                                                                leg_Details.SelectSegment,
                                                                                                                                stg_setting_value.Index,
                                                                                                                                Token, portfolio_leg_value), 2);
                                        }
                                        double _currentLTP = algoCalculation.GetStrikePriceLTP(Token);


                                        if (!portfolio_leg_value.IsLegCancelledOrRejected && !Portfolio_value.IsSTGCompleted)
                                        {
                                            //Place the Order Using NNAPI 
                                            int OrderID = OrderManagerModel.GetOrderId();//Get the client unique ID
                                            OrderManagerModel.Portfolio_Dicc_By_ClientID.TryAdd(OrderID, portfolio_leg_value);
                                            LoginViewModel.NNAPIRequest.PlaceOrderRequest((int)Token, price1: _currentLTP, orderQty: portfolio_leg_value.Qty,
                                                 Buysell: portfolio_leg_value.BuySell, OrderType.LIMIT, 0, OrderID);
                                            //portfolio_leg_value.STG_ID = OrderID;
                                            portfolio_leg_value.Entry_OrderID = OrderID;
                                            portfolio_leg_value.EntryPrice = _currentLTP;
                                            portfolio_leg_value.Status = EnumStrategyStatus.ENTRY_ADDED;
                                            portfolio_leg_value.EntryTime = DateTime.Now;
                                            logFileWriter.DisplayLog(EnumLogType.Info, $"Order ID {OrderID} Entry Postion Mapped with LEG: {portfolio_leg_value.Name}  OF STG {stg_key}");
                                            TotalPremium += (portfolio_leg_value.Qty * portfolio_leg_value.EntryPrice);
                                        }
                                        else
                                        {
                                            portfolio_leg_value.Message = EnumStrategyMessage.ORDER_CANCELLED_BY_SYSTEM;
                                            portfolio_leg_value.Status = EnumStrategyStatus.REJECTED;
                                        }



                                        //Bind to Dic responsibile for Feed load ....
                                        //if (General.PortfolioLegByTokens.TryGetValue(Token, out List<InnerObject> value))
                                        //{
                                        //    var legs = value;
                                        //    legs.Add(portfolio_leg_value);
                                        //    General.PortfolioLegByTokens[Token] = legs;
                                        //}
                                        //else
                                        //{
                                        //    List<InnerObject> legs = new()
                                        //    {
                                        //        portfolio_leg_value
                                        //    };
                                        //    General.PortfolioLegByTokens.TryAdd(Token, legs);
                                        //}
                                        // below function is more safe as above is missed to add in case of multi thread
                                        General.PortfolioLegByTokens.AddOrUpdate(Token, new List<InnerObject>() { portfolio_leg_value }, (key, list) =>
                                        {
                                            list.Add(portfolio_leg_value);
                                            return list;
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        portfolio_leg_value.Status = EnumStrategyStatus.REJECTED;
                                        portfolio_leg_value.Message = EnumStrategyMessage.ERROR;
                                        logFileWriter.WriteLog(EnumDeclaration.EnumLogType.Error, ex.ToString());
                                        logFileWriter.DisplayLog(EnumDeclaration.EnumLogType.Error, ex.Message);
                                    }

                                });
                                tasks.Add(legTask);
                            }

                            Task.WaitAll(tasks.ToArray());
              
                            Portfolio_value.TotalEntryPremiumPaid = TotalPremium;


                            await Task.Delay(1000);
                            if (stg_setting_value.IsOverallStopLossEnable)
                            {
                                Portfolio_value.StopLoss = Math.Round(algoCalculation.GetOverallStopLossValue(TotalPremium,
                                                                                                                stg_setting_value.SettingOverallStopLoss,
                                                                                                                stg_setting_value.OverallStopLoss)
                                                                                                                 , 2);

                                Portfolio_value.ReEntrySL = stg_setting_value.OverallReEntryOnSL;
                            }
                            //TP
                            if (stg_setting_value.IsOverallTargetEnable)
                            {
                                Portfolio_value.TargetProfit = Math.Round(algoCalculation.GetOverallTargetProfitValue(TotalPremium,
                                                                                                               stg_setting_value.SettingOverallTarget,
                                                                                                               stg_setting_value.OverallTarget)
                                                                                                                , 2);

                                Portfolio_value.ReEntryTP = stg_setting_value.OverallReEntryOnTgt;
                            }

                        }
                        else
                        {
                            logFileWriter.DisplayLog(EnumLogType.Info, "Can not placed the Streatgy. Time is Already passed NameOfThe Stg : " + stg_key.ToString());
                        }

                    }));
                }

            }
            catch (Exception ex)
            {
                logFileWriter.WriteLog(EnumLogType.Error, ex.ToString());
            }
        }
        #endregion
    }
}
