using AlgoTerminal.FileManager;
using AlgoTerminal.Manager;
using AlgoTerminal.Services;
using FeedC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.Response
{
    public class FeedCB_C : IFeedResp
    {
        const string PriceFormat = "0.00";
        const string NiftyFutFormat = "Nifty-F {0} ({1})";
        const string BankFutFormat = "Bank-F {0} ({1})";
        const string FinFutFormat = "Fin-F {0} ({1})";
        const int PriceDivisor = 100;
        public static IDashboardModel? _dashboard;

        public FeedCB_C(IDashboardModel dashboardModel)
        {
            _dashboard = dashboardModel;
        }
        private async Task PortFolioUpdate(uint FeedLogTime, ONLY_MBP_DATA_7208 stFeed)
        {
            #region PortFolio Work
            if (General.PortfolioLegByTokens.ContainsKey(stFeed.Token))
            {
                var legs = General.PortfolioLegByTokens[stFeed.Token];
                foreach (var leg in legs)
                {
                    //MTM//LTP when order is placed
                    if (leg.EntryPrice != 0 && leg.ExitPrice == 0 && (leg.Status == EnumStrategyStatus.ENTRY_PARTIALLY_TRADED || leg.Status == EnumStrategyStatus.RUNING))
                    {
                        leg.LTP = Math.Round(Convert.ToDouble(stFeed.LastTradedPrice) / 100.00, 2);
                        uint Bid_Ask = leg.BuySell == EnumPosition.BUY ? stFeed.AskPrice1 : stFeed.BidPrice1;
                        double Pnl = leg.BuySell == EnumPosition.BUY ? (leg.LTP - leg.EntryPrice) : (leg.EntryPrice - leg.LTP);
                        leg.PNL = Math.Round(Pnl * leg.Qty, 2);
                        var stg = General.Portfolios[leg.StgName];
                        double finalLtp = 0;
                        foreach (var x in stg.InnerObject)
                        {
                            finalLtp += x.PNL;
                            //finalMtm += x.MTM;
                        }
                        stg.PNL = Math.Round(finalLtp, 2);
                        //stg.MTM = Math.Round(finalMtm,2);
                    }
                    else if (leg.PNL != 0 && leg.Status == EnumStrategyStatus.REJECTED)
                    {
                        var stg = General.Portfolios[leg.StgName];
                        double finalLtp = 0;
                        leg.PNL = 0;
                        foreach (var x in stg.InnerObject)
                        {
                            finalLtp += x.PNL;
                            //finalMtm += x.MTM;
                        }
                        stg.PNL = Math.Round(finalLtp, 2);
                    }
                }
            }
            #endregion

        }
        private async Task HeaderUpdate(uint FeedLogTime, ONLY_MBP_DATA_7208 stFeed)
        {
            #region Fut Price Update
            if (ContractDetails.NiftyFutureToken == stFeed.Token)
            {
                _dashboard.NiftyFut = string.Format(NiftyFutFormat, (stFeed.LastTradedPrice / (double)PriceDivisor).ToString(PriceFormat), (((int)stFeed.LastTradedPrice - stFeed.ClosingPrice) / (double)PriceDivisor).ToString(PriceFormat));
            }
            else if (ContractDetails.FinNiftyFutureToken == stFeed.Token)
            {
                _dashboard.FinNiftyFut = string.Format(FinFutFormat, (stFeed.LastTradedPrice / (double)PriceDivisor).ToString(PriceFormat), (((int)stFeed.LastTradedPrice - stFeed.ClosingPrice) / (double)PriceDivisor).ToString(PriceFormat));
            }
            else if (ContractDetails.BankNiftyFutureToken == stFeed.Token)
            {
                _dashboard.BankNiftyFut = string.Format(BankFutFormat, (stFeed.LastTradedPrice / (double)PriceDivisor).ToString(PriceFormat), (((int)stFeed.LastTradedPrice - stFeed.ClosingPrice) / (double)PriceDivisor).ToString(PriceFormat));
            }

            #endregion
        }
        public void Feed_CallBack(uint FeedLogTime, ONLY_MBP_DATA_7208 stFeed)
        {
            _ = PortFolioUpdate(FeedLogTime, stFeed);
            _ = HeaderUpdate(FeedLogTime, stFeed);
        }

        public void Messages(string msg)
        {
            throw new NotImplementedException();
        }

    }
}
