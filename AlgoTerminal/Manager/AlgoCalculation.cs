using AlgoTerminal.FileManager;
using AlgoTerminal.Model;
using AlgoTerminal.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.Manager
{
    public class AlgoCalculation : IAlgoCalculation
    {

        private readonly IFeed _feed;
        private readonly ILogFileWriter _logFileWriter;

        private const double STT_Opt = 0.00060;
        private const double Stt_Fut = 0.0001;
        private const double Exp_Opt = 0.000572;
        private const double Exp_Fut = 0.0000238;
        private const double StampDuty_Fut = .00002;
        private const double StampDuty_Opt = .00003;
        private double UnderLingValue(EnumIndex enumIndex)
        {
            if (_feed.FeedCM == null)
                throw new Exception("Feed for Captial is NULL");
            string? SpotString;
            if (enumIndex == EnumIndex.NIFTY) SpotString = "Nifty 50";
            else if (enumIndex == EnumIndex.BANKNIFTY) SpotString = "Nifty Bank";
            else if (enumIndex == EnumIndex.FINNIFTY) SpotString = "Nifty Fin Service";
            else if (enumIndex == EnumIndex.MIDCPNIFTY) SpotString = "NIFTY MID SELECT";
            else SpotString = null;
            if (SpotString == null)
                throw new Exception();

            return _feed.FeedCM.dcFeedDataIdx[SpotString].IndexValue;
        }
        private double GetInstrumentPrice(uint Token)
        {
            if (_feed.FeedC == null)
                throw new Exception("Feed for F&O is NULL");
            return Convert.ToDouble(_feed.FeedC.dcFeedData[Token].LastTradedPrice) / 100.00;
        }
        public AlgoCalculation(IFeed feed, ILogFileWriter logFileWriter)
        {
            _logFileWriter = logFileWriter;
            _feed = feed;
        }

        #region Strike Setting Only ==> As per EnumDeclaration.EnumSelectStrikeCriteria

        /// <summary>
        ///  Provide the strike Value according to the Strike Criteria .
        /// </summary>
        /// <param name="_strike_criteria"></param>
        /// <param name="_strike_type"></param>
        /// <param name="_premium_lower_range"></param>
        /// <param name="_premium_upper_range"></param>
        /// <param name="_premium_or_StraddleValue"></param>
        /// <param name="enumIndex"></param>
        /// <param name="enumUnderlyingFrom"></param>
        /// <param name="enumSegments"></param>
        /// <param name="enumExpiry"></param>
        /// <param name="enumOptiontype"></param>
        /// <param name="enumPosition"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="CustumException">Can give Erorr if Contract not loaded, Feed is not initiated or Feed Dic do not have any token Details</exception>
        public double GetStrike(EnumSelectStrikeCriteria _strike_criteria,
            EnumStrikeType _strike_type,
            double _premium_lower_range, double _premium_upper_range, double _premium_or_StraddleValue,
            EnumIndex enumIndex,
            EnumUnderlyingFrom enumUnderlyingFrom,
            EnumSegments enumSegments,
            EnumExpiry enumExpiry,
            EnumOptiontype enumOptiontype,
            EnumPosition enumPosition)
        {

            return _strike_criteria switch
            {
                EnumSelectStrikeCriteria.STRIKETYPE => GetStrikeType(_strike_type, enumIndex, enumUnderlyingFrom, enumSegments, enumExpiry, enumOptiontype),
                EnumSelectStrikeCriteria.PREMIUMRANGE => CommonFunctionForPremium(_premium_lower_range, _premium_upper_range, _premium_or_StraddleValue, enumIndex, enumSegments, enumExpiry, enumOptiontype, enumPosition, _strike_criteria),
                EnumSelectStrikeCriteria.CLOSESTPREMIUM => CommonFunctionForPremium(_premium_lower_range, _premium_upper_range, _premium_or_StraddleValue, enumIndex, enumSegments, enumExpiry, enumOptiontype, enumPosition, _strike_criteria),
                EnumSelectStrikeCriteria.PREMIUMGREATEROREQUAL => CommonFunctionForPremium(_premium_lower_range, _premium_upper_range, _premium_or_StraddleValue, enumIndex, enumSegments, enumExpiry, enumOptiontype, enumPosition, _strike_criteria),
                EnumSelectStrikeCriteria.PREMIUMLESSOREQUAL => CommonFunctionForPremium(_premium_lower_range, _premium_upper_range, _premium_or_StraddleValue, enumIndex, enumSegments, enumExpiry, enumOptiontype, enumPosition, _strike_criteria),
                EnumSelectStrikeCriteria.STRADDLEWIDTH => GetStraddleWidth(_premium_or_StraddleValue, enumIndex, enumUnderlyingFrom, enumSegments, enumExpiry, enumOptiontype, enumPosition, _strike_criteria),
                _ => throw new NotImplementedException(),
            };
        }

        private double GetStraddleWidth(double premium_or_StraddleValue, EnumIndex enumIndex, EnumUnderlyingFrom enumUnderlyingFrom,
            EnumSegments enumSegments, EnumExpiry enumExpiry, EnumOptiontype enumOptiontype, EnumPosition enumPosition,
            EnumSelectStrikeCriteria strike_criteria)
        {
            //ATM Strike +/- (value) * (Call LTP + Put LTP)

            double StraddleWidth;
            if (ContractDetails.ContractDetailsToken == null || _feed.FeedC == null)
                throw new Exception("Contract not Loaded or Feed not Init");
            //Set Spot according to underlying Form
            double ATMStrike = GetStrikeType(EnumStrikeType.ATM, enumIndex, enumUnderlyingFrom, enumSegments, enumExpiry, enumOptiontype);
            DateTime exp = GetLegExpiry(enumExpiry, enumIndex, enumSegments, enumOptiontype);

            //GET CE AND PE TOKEN
            uint[] Token = ContractDetails.ContractDetailsToken.Where(x => x.Value.Symbol == enumIndex.ToString().ToUpper()
                 && x.Value.Expiry == exp
                 && x.Value.Strike == ATMStrike)
                    .Select(x => x.Key)
                    .ToArray();
            if (Token.Length > 2)
                throw new Exception("Call and Put Can not have muplitpile value -> logic fail :) ");

            double _ATMStraddleprice = Convert.ToDouble(_feed.FeedC.dcFeedData[Token[0]].LastTradedPrice + _feed.FeedC.dcFeedData[Token[1]].LastTradedPrice) / 100.00;
            StraddleWidth = ATMStrike + premium_or_StraddleValue * _ATMStraddleprice;
            var list = ContractDetails.ContractDetailsToken.Where(y => y.Value.Symbol == enumIndex.ToString().ToUpper() &&
                y.Value.Opttype == enumOptiontype).Select(x => x.Value.Strike).ToList().Distinct();
            //uint nearest = ContractDetails.ContractDetailsToken[Token[0]].LotSize;
            //double StraddleWidthNearestStrike = (double)Math.Round(StraddleWidth / nearest) * nearest;
            return list.Aggregate((x, y) => Math.Abs(x - StraddleWidth) < Math.Abs(y - StraddleWidth) ? x : y);
        }
        private double GetStrikeType(EnumStrikeType strike_type,
            EnumIndex enumIndex, EnumUnderlyingFrom enumUnderlyingFrom,
            EnumSegments enumSegments, EnumExpiry enumExpiry,
            EnumOptiontype enumOptiontype)
        {
            //Call option = Spot Price – Strike Price ()
            //Put option  = Strike Price – Spot price (OPS Direction to Call)
            if (ContractDetails.ContractDetailsToken != null && _feed.FeedCM != null && _feed.FeedC != null && _feed.FeedC.dcFeedData != null && enumSegments == EnumSegments.OPTIONS)
            {
                double ATMStrike = 0, SpotPrice = 0, FinalStrike = 0;
                string Symbol = enumIndex.ToString().ToUpper();
                if (enumUnderlyingFrom == EnumUnderlyingFrom.FUTURES)
                {
                    // Setting to EnumSegments.Futures => get Future Expiry first Month
                    DateTime exp = GetLegExpiry(enumExpiry, enumIndex, EnumSegments.FUTURES, enumOptiontype);
                    uint FUTToken = ContractDetails.ContractDetailsToken.Where(x => x.Value.Expiry == exp
                    && x.Value.Opttype == EnumOptiontype.XX
                    && x.Value.Symbol == Symbol)
                         .Select(s => s.Key)
                         .FirstOrDefault();
                    if (FUTToken != 0)
                        SpotPrice = _feed.FeedC.dcFeedData[FUTToken].LastTradedPrice / 100;
                }
                else
                {
                    string? SpotString;
                    if (enumIndex == EnumIndex.NIFTY) SpotString = "Nifty 50";
                    else if (enumIndex == EnumIndex.BANKNIFTY) SpotString = "Nifty Bank";
                    else if (enumIndex == EnumIndex.FINNIFTY) SpotString = "Nifty Fin Service";
                    else if (enumIndex == EnumIndex.MIDCPNIFTY) SpotString = "NIFTY MID SELECT";
                    else SpotString = null;
                    if (SpotString == null)
                        throw new Exception();

                    if (_feed.FeedCM.dcFeedDataIdx.TryGetValue(SpotString, out FeedCM.MULTIPLE_INDEX_BCAST_REC_7207 valueCM))
                    {
                        var feeddata = valueCM;
                        SpotPrice = feeddata.IndexValue;
                    }
                }

                var list = ContractDetails.ContractDetailsToken.Where(y => y.Value.Symbol == Symbol &&
                y.Value.Opttype == enumOptiontype).Select(x => x.Value.Strike).ToList().Distinct();
                //ATM Strike
                ATMStrike = list.Aggregate((x, y) => Math.Abs(x - SpotPrice) < Math.Abs(y - SpotPrice) ? x : y);

                if (strike_type == EnumStrikeType.ATM)
                    return ATMStrike;

                double[] sorteddata = list.ToArray();
                Array.Sort(sorteddata);

                int _atmIndex = Array.IndexOf(sorteddata, ATMStrike);
                int _indexNeedToReturn;
                if (enumOptiontype == EnumOptiontype.CE)
                    _indexNeedToReturn = _atmIndex + (int)strike_type;
                else
                    _indexNeedToReturn = _atmIndex - (int)strike_type;

                FinalStrike = sorteddata[_indexNeedToReturn];
                return FinalStrike;
            }
            else
                throw new Exception("Feed is not Initialized");

        }

        private double CommonFunctionForPremium(double premium_lower_range,
            double premium_upper_range,
            double _premium, EnumIndex enumIndex,
            EnumSegments enumSegments, EnumExpiry enumExpiry,
            EnumOptiontype enumOptiontype, EnumPosition enumPosition,
            EnumSelectStrikeCriteria enumSelectStrikeCriteria)
        {
            //Closest to High Pre => SELL
            //Closest to Low Pre => BUY
            //Range will be diff for call and put

            if (ContractDetails.ContractDetailsToken == null) throw new Exception("Contract is Initialize");
            if (_feed.FeedC == null) throw new Exception("Feed is Not Initialize");
            //expiry
            DateTime exp = GetLegExpiry(enumExpiry, enumIndex, enumSegments, enumOptiontype);

            //valid Token
            uint[] TokenList = ContractDetails.ContractDetailsToken.Where(x => x.Value.Symbol == enumIndex.ToString().ToUpper() &&
                 x.Value.Opttype == enumOptiontype
                 && x.Value.Expiry == exp)
                    .Select(x => x.Key)
                    .ToArray();

            //Feed Avaliable for Token in Feed Dic
            uint[] FeedAvaliableTokenInDic = _feed.FeedC.dcFeedData.Select(x => Convert.ToUInt32(x.Key)).ToArray();

            //Intersection 
            var FinalTokenSetAfterIntersection = TokenList.Intersect(FeedAvaliableTokenInDic);

            //All Premium
            double[] premium = FinalTokenSetAfterIntersection.Select(xx => Convert.ToDouble(_feed.FeedC.dcFeedData[xx].LastTradedPrice) / 100.00).ToArray();
            uint Token; int index = -1;
            //Valid Premium in Range

            if (enumSelectStrikeCriteria == EnumSelectStrikeCriteria.PREMIUMRANGE)
            {
                double[] PremiumInRange = premium.Where(x => x >= premium_lower_range && x <= premium_upper_range).ToArray();
                if (PremiumInRange.Length <= 0) throw new Exception("No Strike Found with in provided Range");
                //GetToken =>Note need to handle if same Price find in multipile strike
                if (enumPosition == EnumPosition.BUY) index = Array.IndexOf(premium, PremiumInRange.Min());
                else if (enumPosition == EnumPosition.SELL) index = Array.IndexOf(premium, PremiumInRange.Max());
            }
            else if (enumSelectStrikeCriteria == EnumSelectStrikeCriteria.CLOSESTPREMIUM)
            {
                List<double> list = premium.ToList();
                index = Array.IndexOf(premium, list.Aggregate((x, y) => Math.Abs(x - _premium) < Math.Abs(y - _premium) ? x : y));
            }
            else if (enumSelectStrikeCriteria == EnumSelectStrikeCriteria.PREMIUMGREATEROREQUAL)
            {
                double[] PremiumInRange = premium.Where(x => x >= _premium).ToArray();
                if (PremiumInRange.Length <= 0) throw new Exception("No Strike Found with in provided Range");
                index = Array.IndexOf(premium, PremiumInRange.Min());
            }
            else if (enumSelectStrikeCriteria == EnumSelectStrikeCriteria.PREMIUMLESSOREQUAL)
            {
                double[] PremiumInRange = premium.Where(x => x <= _premium).ToArray();
                if (PremiumInRange.Length <= 0) throw new Exception("No Strike Found with in provided Range");
                index = Array.IndexOf(premium, PremiumInRange.Max());
            }
            else
            {
                throw new Exception("Invalid Option for Select Strike Option");
            }
            if (index >= 0)
            {
                Token = FinalTokenSetAfterIntersection.ElementAt(index);
                return ContractDetails.ContractDetailsToken[Token].Strike;
            }
            throw new Exception("Some Calculation is Missed ==> GetPremiumRange Function");
        }
        #endregion

        #region IS SL HIT FOR LEG
        public bool Get_if_SL_is_HIT(double CurrentStopLossValue, EnumLegSL enumLegSL,
            EnumOptiontype enumOptiontype, EnumPosition enumPosition,
            EnumIndex enumIndex, uint Token, EnumUnderlyingFrom underlyingFrom)
        {
            double CurrentPrice;
            if ((enumLegSL == EnumLegSL.UNDERLING || enumLegSL == EnumLegSL.UNDERLINGPERCENTAGE))
            {
                if (underlyingFrom == EnumUnderlyingFrom.CASH)
                {
                    CurrentPrice = UnderLingValue(enumIndex);
                }
                else
                {
                    DateTime exp = GetLegExpiry(EnumExpiry.MONTHLY, enumIndex, EnumSegments.FUTURES, enumOptiontype);
                    uint FUTToken = ContractDetails.ContractDetailsToken.Where(x => x.Value.Expiry == exp
                    && x.Value.Opttype == EnumOptiontype.XX
                    && x.Value.Symbol == enumIndex.ToString())
                         .Select(s => s.Key)
                         .FirstOrDefault();
                    //underlying Token
                    CurrentPrice = GetInstrumentPrice(FUTToken);
                }

                if (CurrentPrice != 0)
                {
                    if ((enumPosition == EnumPosition.BUY && enumOptiontype == EnumOptiontype.PE) || ((enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX) && enumPosition == EnumPosition.SELL))
                    {
                        if (CurrentPrice >= CurrentStopLossValue) return true;
                    }
                    else
                    {
                        if (CurrentPrice <= CurrentStopLossValue) return true;
                    }

                }
            }
            else
            {
                CurrentPrice = GetInstrumentPrice(Token);
                if (CurrentPrice != 0)
                {
                    if (enumPosition == EnumPosition.BUY && (enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX || enumOptiontype == EnumOptiontype.PE))
                    {
                        if (CurrentStopLossValue >= CurrentPrice) return true;
                    }
                    else
                    {
                        if (CurrentStopLossValue <= CurrentPrice) return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region IS TP HIT FOR LEG
        public bool Get_if_TP_is_HIT(double CurrentTargetProfitValue, EnumLegSL enumLegSL,
            EnumOptiontype enumOptiontype, EnumPosition enumPosition,
            EnumIndex enumIndex, uint Token, EnumUnderlyingFrom underlyingFrom)
        {
            double CurrentPrice;
            if ((enumLegSL == EnumLegSL.UNDERLING || enumLegSL == EnumLegSL.UNDERLINGPERCENTAGE))
            {
                if (underlyingFrom == EnumUnderlyingFrom.CASH)
                {
                    CurrentPrice = UnderLingValue(enumIndex);
                }
                else
                {
                    DateTime exp = GetLegExpiry(EnumExpiry.MONTHLY, enumIndex, EnumSegments.FUTURES, enumOptiontype);
                    uint FUTToken = ContractDetails.ContractDetailsToken.Where(x => x.Value.Expiry == exp
                    && x.Value.Opttype == EnumOptiontype.XX
                    && x.Value.Symbol == enumIndex.ToString())
                         .Select(s => s.Key)
                         .FirstOrDefault();
                    //underlying Token
                    CurrentPrice = GetInstrumentPrice(FUTToken);
                }

                //CurrentPrice = UnderLingValue(enumIndex);
                if (CurrentPrice != 0)
                {
                    if ((enumPosition == EnumPosition.BUY && enumOptiontype == EnumOptiontype.PE) || ((enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX) && enumPosition == EnumPosition.SELL))
                    {
                        if (CurrentPrice <= CurrentTargetProfitValue) return true;
                    }
                    else
                    {
                        if (CurrentPrice >= CurrentTargetProfitValue) return true;
                    }

                }
            }
            else
            {
                CurrentPrice = GetInstrumentPrice(Token);
                if (CurrentPrice != 0)
                {
                    if (enumPosition == EnumPosition.BUY && (enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX || enumOptiontype == EnumOptiontype.PE))
                    {
                        if (CurrentTargetProfitValue <= CurrentPrice) return true;
                    }
                    else
                    {
                        if (CurrentTargetProfitValue >= CurrentPrice) return true;
                    }
                }
            }
            return false;
        }
        #endregion


        #region Get LIVE StopLoss for Leg
        public double GetLegStopLoss(EnumLegSL enumLegSL,
            EnumOptiontype enumOptiontype, EnumPosition enumPosition,
            double StopLoss,
            EnumSegments enumSegments,
            EnumIndex enumIndex, uint Token, InnerObject legDetails)
        {
            double entryPrice;
            if ((enumLegSL == EnumLegSL.UNDERLING || enumLegSL == EnumLegSL.UNDERLINGPERCENTAGE))
            {
                if (legDetails.enumUnderlyingFromForLeg == EnumUnderlyingFrom.CASH)
                {
                    entryPrice = UnderLingValue(enumIndex);
                }
                else
                {
                    DateTime exp = GetLegExpiry(EnumExpiry.MONTHLY, enumIndex, EnumSegments.FUTURES, enumOptiontype);
                    uint FUTToken = ContractDetails.ContractDetailsToken.Where(x => x.Value.Expiry == exp
                    && x.Value.Opttype == EnumOptiontype.XX
                    && x.Value.Symbol == enumIndex.ToString())
                         .Select(s => s.Key)
                         .FirstOrDefault();
                    //underlying Token
                    entryPrice = GetInstrumentPrice(FUTToken);
                }
            }
            else
                entryPrice = GetInstrumentPrice(Token);

            if (enumSegments == EnumSegments.FUTURES)
                enumOptiontype = EnumOptiontype.XX;

            legDetails.EntryUnderliying_INST = entryPrice;

            return enumLegSL switch
            {
                EnumLegSL.POINTS => GetLegPoint_SL(entryPrice, enumOptiontype, enumPosition, StopLoss),
                EnumLegSL.POINTPERCENTAGE => GetLegPointPercentage_SL(entryPrice, enumOptiontype, enumPosition, StopLoss),
                EnumLegSL.UNDERLINGPERCENTAGE => GetLegPointPercentage_underlyingSL(entryPrice, enumOptiontype, enumPosition, StopLoss),
                EnumLegSL.UNDERLING => GetLegPoint_UnderlyingSL(entryPrice, enumOptiontype, enumPosition, StopLoss),
                _ => throw new NotImplementedException(),
            };
        }
        private double GetLegPointPercentage_underlyingSL(double entryPrice, EnumOptiontype enumOptiontype, EnumPosition enumPosition, double StopLoss)
        {
            if ((enumPosition == EnumPosition.BUY && enumOptiontype == EnumOptiontype.PE) || ((enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX) && enumPosition == EnumPosition.SELL))
                return entryPrice + entryPrice * StopLoss / 100.00;
            else if ((enumPosition == EnumPosition.SELL && enumOptiontype == EnumOptiontype.PE) || ((enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX) && enumPosition == EnumPosition.BUY))
                return entryPrice - entryPrice * StopLoss / 100.00;
            else
                throw new NotImplementedException("Invalid Option");
        }

        private double GetLegPoint_UnderlyingSL(double entryPrice, EnumOptiontype enumOptiontype, EnumPosition enumPosition, double StopLoss)
        {
            if ((enumPosition == EnumPosition.BUY && enumOptiontype == EnumOptiontype.PE) || ((enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX) && enumPosition == EnumPosition.SELL))
                return entryPrice + StopLoss;
            else if ((enumPosition == EnumPosition.SELL && enumOptiontype == EnumOptiontype.PE) || ((enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX) && enumPosition == EnumPosition.BUY))
                return entryPrice - StopLoss;
            else
                throw new NotImplementedException("Invalid Option");
        }
        private double GetLegPointPercentage_SL(double entryPrice, EnumOptiontype enumOptiontype, EnumPosition enumPosition, double StopLoss)
        {
            if (enumPosition == EnumPosition.BUY && (enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX || enumOptiontype == EnumOptiontype.PE))
                return entryPrice - entryPrice * StopLoss / 100.00;
            else if (enumPosition == EnumPosition.SELL && (enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX || enumOptiontype == EnumOptiontype.PE))
                return entryPrice + entryPrice * StopLoss / 100.00;
            else
                throw new NotImplementedException("Invalid Option");
        }

        private double GetLegPoint_SL(double entryPrice, EnumOptiontype enumOptiontype, EnumPosition enumPosition, double StopLoss)
        {
            if (enumPosition == EnumPosition.BUY && (enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX || enumOptiontype == EnumOptiontype.PE))
                return entryPrice - StopLoss;
            else if (enumPosition == EnumPosition.SELL && (enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX || enumOptiontype == EnumOptiontype.PE))
                return entryPrice + StopLoss;
            else
                throw new NotImplementedException("Invalid Option");
        }
        #endregion

        #region Get LIVE Target Profit for Leg
        public double GetLegTargetProfit(EnumLegTargetProfit enumLegTP,
            EnumOptiontype enumOptiontype,
            EnumPosition enumPosition,
            double TargetProfit, EnumSegments enumSegments,
            EnumIndex enumIndex, uint Token, InnerObject legDetails)
        {
            double entryPrice;
            if ((enumLegTP == EnumLegTargetProfit.UNDERLING || enumLegTP == EnumLegTargetProfit.UNDERLINGPERCENTAGE))
                if (legDetails.enumUnderlyingFromForLeg == EnumUnderlyingFrom.CASH)
                {
                    entryPrice = UnderLingValue(enumIndex);
                }
                else
                {
                    DateTime exp = GetLegExpiry(EnumExpiry.MONTHLY, enumIndex, EnumSegments.FUTURES, enumOptiontype);
                    uint FUTToken = ContractDetails.ContractDetailsToken.Where(x => x.Value.Expiry == exp
                    && x.Value.Opttype == EnumOptiontype.XX
                    && x.Value.Symbol == enumIndex.ToString())
                         .Select(s => s.Key)
                         .FirstOrDefault();
                    //underlying Token
                    entryPrice = GetInstrumentPrice(FUTToken);
                }
            else
                entryPrice = GetInstrumentPrice(Token);

            if (enumSegments == EnumSegments.FUTURES)
                enumOptiontype = EnumOptiontype.XX;

            legDetails.EntryUnderliying_INST = entryPrice;

            return enumLegTP switch
            {
                EnumLegTargetProfit.POINTS => GetLegPoint_TP(entryPrice, enumOptiontype, enumPosition, TargetProfit),
                EnumLegTargetProfit.POINTPERCENTAGE => GetLegPointPercentage_TP(entryPrice, enumOptiontype, enumPosition, TargetProfit),
                EnumLegTargetProfit.UNDERLING => GetLegUnderlingTP(entryPrice, enumOptiontype, enumPosition, TargetProfit),
                EnumLegTargetProfit.UNDERLINGPERCENTAGE => GetLegUnderlingPercentageTP(entryPrice, enumOptiontype, enumPosition, TargetProfit),
                _ => throw new NotImplementedException(),
            };
        }

        private double GetLegUnderlingPercentageTP(double entryPrice, EnumOptiontype enumOptiontype, EnumPosition enumPosition, double StopLoss)
        {
            if ((enumPosition == EnumPosition.BUY && enumOptiontype == EnumOptiontype.PE) || ((enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX) && enumPosition == EnumPosition.SELL))
                return entryPrice - entryPrice * StopLoss / 100.00;
            else if ((enumPosition == EnumPosition.SELL && enumOptiontype == EnumOptiontype.PE) || ((enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX) && enumPosition == EnumPosition.BUY))
                return entryPrice + entryPrice * StopLoss / 100.00;
            else
                throw new NotImplementedException("Invalid Option");
        }

        private double GetLegUnderlingTP(double entryPrice, EnumOptiontype enumOptiontype, EnumPosition enumPosition, double StopLoss)
        {
            if ((enumPosition == EnumPosition.BUY && enumOptiontype == EnumOptiontype.PE) || ((enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX) && enumPosition == EnumPosition.SELL))
                return entryPrice - StopLoss;
            else if ((enumPosition == EnumPosition.SELL && enumOptiontype == EnumOptiontype.PE) || ((enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX) && enumPosition == EnumPosition.BUY))
                return entryPrice + StopLoss;
            else
                throw new NotImplementedException("Invalid Option");
        }
        private double GetLegPointPercentage_TP(double entryPrice, EnumOptiontype enumOptiontype, EnumPosition enumPosition, double StopLoss)
        {
            if (enumPosition == EnumPosition.BUY && (enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX || enumOptiontype == EnumOptiontype.PE))
                return entryPrice + entryPrice * StopLoss / 100.00;
            else if (enumPosition == EnumPosition.SELL && (enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX || enumOptiontype == EnumOptiontype.PE))
                return entryPrice - entryPrice * StopLoss / 100.00;
            else
                throw new NotImplementedException("Invalid Option");
        }

        private double GetLegPoint_TP(double entryPrice, EnumOptiontype enumOptiontype, EnumPosition enumPosition, double StopLoss)
        {
            if (enumPosition == EnumPosition.BUY && (enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX || enumOptiontype == EnumOptiontype.PE))
                return entryPrice + StopLoss;
            else if (enumPosition == EnumPosition.SELL && (enumOptiontype == EnumOptiontype.CE || enumOptiontype == EnumOptiontype.XX || enumOptiontype == EnumOptiontype.PE))
                return entryPrice - StopLoss;
            else
                throw new NotImplementedException("Invalid Option");
        }

        #endregion

        #region Get Target Profit for Leg When TP Hit
        private double GetLegTargetProfit_OnEntryPrice(EnumLegTargetProfit enumLegTP,
            EnumOptiontype enumOptiontype, EnumPosition enumPosition,
            double TargetProfit,
            EnumSegments enumSegments,
            double entryPrice)
        {


            if (enumSegments == EnumSegments.FUTURES)
                enumOptiontype = EnumOptiontype.XX;



            return enumLegTP switch
            {
                EnumLegTargetProfit.POINTS => GetLegPoint_TP(entryPrice, enumOptiontype, enumPosition, TargetProfit),
                EnumLegTargetProfit.POINTPERCENTAGE => GetLegPointPercentage_TP(entryPrice, enumOptiontype, enumPosition, TargetProfit),
                EnumLegTargetProfit.UNDERLING => GetLegUnderlingTP(entryPrice, enumOptiontype, enumPosition, TargetProfit),
                EnumLegTargetProfit.UNDERLINGPERCENTAGE => GetLegUnderlingPercentageTP(entryPrice, enumOptiontype, enumPosition, TargetProfit),
                _ => throw new NotImplementedException(),
            };
        }
        #endregion

        #region Get StopLoss for Leg on Rentry with old Price
        private double GetLegStopLoss_OnEntryPrice(EnumLegSL enumLegSL,
            EnumOptiontype enumOptiontype, EnumPosition enumPosition,
            double StopLoss,
            EnumSegments enumSegments,
            double entryPrice)
        {

            if (enumSegments == EnumSegments.FUTURES)
                enumOptiontype = EnumOptiontype.XX;

            return enumLegSL switch
            {
                EnumLegSL.POINTS => GetLegPoint_SL(entryPrice, enumOptiontype, enumPosition, StopLoss),
                EnumLegSL.POINTPERCENTAGE => GetLegPointPercentage_SL(entryPrice, enumOptiontype, enumPosition, StopLoss),
                EnumLegSL.UNDERLINGPERCENTAGE => GetLegPointPercentage_underlyingSL(entryPrice, enumOptiontype, enumPosition, StopLoss),
                EnumLegSL.UNDERLING => GetLegPoint_UnderlyingSL(entryPrice, enumOptiontype, enumPosition, StopLoss),
                _ => throw new NotImplementedException(),
            };
        }
        #endregion

        #region Get Re-Entry Details for Leg When SL Hit

        public InnerObject GetLegDetailsForRentry_SLHIT(LegDetails leg_Details, InnerObject OldLegDetails, StrategyDetails stg_setting_value)
        {
            return leg_Details.SettingReEntryOnSL switch
            {
                EnumLegReEntryOnSL.RECOST => GetLegReEntryForCOST(OldLegDetails, leg_Details),
                EnumLegReEntryOnSL.REREVCOST => GetLegReEntryForCOST(OldLegDetails, leg_Details, true),
                EnumLegReEntryOnSL.REASAP => GetReEntryForASAP(OldLegDetails, leg_Details, stg_setting_value),
                EnumLegReEntryOnSL.REREVASAP => GetReEntryForASAP(OldLegDetails, leg_Details, stg_setting_value, true),
                EnumLegReEntryOnSL.REMOMENTUM => GetReEntryForMOMENTUM(OldLegDetails, leg_Details, stg_setting_value),
                EnumLegReEntryOnSL.REREVMOMENTUM => GetReEntryForMOMENTUM(OldLegDetails, leg_Details, stg_setting_value, true),
                _ => throw new NotImplementedException(),
            };
        }
        private InnerObject GetReEntryForMOMENTUM(InnerObject OldLegDetails, LegDetails leg_Details, StrategyDetails stg_setting_value, bool Reverse = false)
        {

            if (leg_Details.IsSimpleMomentumEnable == false)
            {
                return GetReEntryForASAP(OldLegDetails, leg_Details, stg_setting_value, Reverse);
            }

            InnerObject newLegDetails = new();
            if (Reverse)
                newLegDetails.BuySell = OldLegDetails.BuySell == EnumPosition.BUY ? EnumPosition.SELL : EnumPosition.BUY;
            else
                newLegDetails.BuySell = OldLegDetails.BuySell;
            newLegDetails.Qty = OldLegDetails.Qty;
            newLegDetails.StgName = OldLegDetails.StgName;
            newLegDetails.ReEntryTP = OldLegDetails.ReEntryTP;
            newLegDetails.ReEntrySL = OldLegDetails.ReEntrySL;

            //Get Trading Symbol and Token
            DateTime Expiry = GetLegExpiry(leg_Details.Expiry,
                                                               stg_setting_value.Index,
                                                               leg_Details.SelectSegment,
                                                               leg_Details.OptionType);
            double StrikeForLeg = EnumSegments.OPTIONS == leg_Details.SelectSegment ? GetStrike(leg_Details.StrikeCriteria,
                                                              leg_Details.StrikeType,
                                                              leg_Details.PremiumRangeLower,
                                                              leg_Details.PremiumRangeUpper,
                                                              leg_Details.Premium_or_StraddleWidth,
                                                              stg_setting_value.Index,
                                                              stg_setting_value.UnderlyingFrom,
                                                              leg_Details.SelectSegment,
                                                              leg_Details.Expiry,
                                                              leg_Details.OptionType,
                                                               newLegDetails.BuySell) :
                                                              -0.01;
            uint Token = EnumSegments.OPTIONS == leg_Details.SelectSegment ? ContractDetails.GetTokenByContractValue(Expiry, leg_Details.OptionType, stg_setting_value.Index, StrikeForLeg) :
       ContractDetails.GetTokenByContractValue(Expiry, EnumOptiontype.XX, stg_setting_value.Index);
            string TradingSymbol = ContractDetails.GetContractDetailsByToken(Token).TrdSymbol ?? throw new Exception("for " + Token + " Trading Symbol was not Found in Contract Details.");

            newLegDetails.Token = Token;
            newLegDetails.TradingSymbol = TradingSymbol;
            newLegDetails.Name = OtherMethods.GetNewName(OldLegDetails.Name);
            return newLegDetails;

        }
        public InnerObject IsSimpleMovementumHitForRentry(InnerObject newLegDetails, LegDetails leg_Details, StrategyDetails stg_setting_value)
        {
            var data2 = GetLegMomentumlock(leg_Details.SettingSimpleMomentum,
                                                                    leg_Details.SimpleMomentum,
                                                                    stg_setting_value.Index,
                                                                    newLegDetails.Token, newLegDetails).Result;

            if (leg_Details.IsStopLossEnable == true)
            {
                newLegDetails.StopLoss = Math.Round(GetLegStopLoss(leg_Details.SettingStopLoss,
                                                                                leg_Details.OptionType,
                                                                                leg_Details.Position,
                                                                                leg_Details.StopLoss,
                                                                                leg_Details.SelectSegment,
                                                                                stg_setting_value.Index,
                                                                                newLegDetails.Token, newLegDetails), 2);
            }

            if (leg_Details.IsTargetProfitEnable == true)
            {
                newLegDetails.TargetProfit = Math.Round(GetLegTargetProfit(leg_Details.SettingTargetProfit,
                                                                                                    leg_Details.OptionType,
                                                                                                    leg_Details.Position,
                                                                                                    leg_Details.TargetProfit,
                                                                                                    leg_Details.SelectSegment,
                                                                                                    stg_setting_value.Index,
                                                                                                    newLegDetails.Token, newLegDetails), 2);
            }

            double _currentLTP = GetStrikePriceLTP(newLegDetails.Token);
            newLegDetails.EntryPrice = _currentLTP;
            return newLegDetails;
        }
        private InnerObject GetReEntryForASAP(InnerObject OldLegDetails, LegDetails leg_Details, StrategyDetails stg_setting_value, bool Reverse = false)
        {
            InnerObject newLegDetails = new();
            if (Reverse)
                newLegDetails.BuySell = OldLegDetails.BuySell == EnumPosition.BUY ? EnumPosition.SELL : EnumPosition.BUY;
            else
                newLegDetails.BuySell = OldLegDetails.BuySell;

            newLegDetails.Qty = OldLegDetails.Qty;
            newLegDetails.StgName = OldLegDetails.StgName;
            newLegDetails.ReEntryTP = OldLegDetails.ReEntryTP;
            newLegDetails.ReEntrySL = OldLegDetails.ReEntrySL;
            DateTime Expiry = GetLegExpiry(leg_Details.Expiry,
                                                               stg_setting_value.Index,
                                                               leg_Details.SelectSegment,
                                                               leg_Details.OptionType);


            double StrikeForLeg = EnumSegments.OPTIONS == leg_Details.SelectSegment ? GetStrike(leg_Details.StrikeCriteria,
                                                               leg_Details.StrikeType,
                                                               leg_Details.PremiumRangeLower,
                                                               leg_Details.PremiumRangeUpper,
                                                               leg_Details.Premium_or_StraddleWidth,
                                                               stg_setting_value.Index,
                                                               stg_setting_value.UnderlyingFrom,
                                                               leg_Details.SelectSegment,
                                                               leg_Details.Expiry,
                                                               leg_Details.OptionType,
                                                                newLegDetails.BuySell) :
                                                               -0.01;

            uint Token = EnumSegments.OPTIONS == leg_Details.SelectSegment ? ContractDetails.GetTokenByContractValue(Expiry, leg_Details.OptionType, stg_setting_value.Index, StrikeForLeg) :
        ContractDetails.GetTokenByContractValue(Expiry, EnumOptiontype.XX, stg_setting_value.Index);
            string TradingSymbol = ContractDetails.GetContractDetailsByToken(Token).TrdSymbol ?? throw new Exception("for " + Token + " Trading Symbol was not Found in Contract Details.");

            newLegDetails.Token = Token;
            newLegDetails.TradingSymbol = TradingSymbol;

            if (leg_Details.IsStopLossEnable == true)
            {
                newLegDetails.StopLoss = Math.Round(GetLegStopLoss(leg_Details.SettingStopLoss,
                                                                                leg_Details.OptionType,
                                                                                leg_Details.Position,
                                                                                leg_Details.StopLoss,
                                                                                leg_Details.SelectSegment,
                                                                                stg_setting_value.Index,
                                                                                Token, newLegDetails), 2);
            }

            if (leg_Details.IsTargetProfitEnable == true)
            {
                newLegDetails.TargetProfit = Math.Round(GetLegTargetProfit(leg_Details.SettingTargetProfit,
                                                                                                    leg_Details.OptionType,
                                                                                                    leg_Details.Position,
                                                                                                    leg_Details.TargetProfit,
                                                                                                    leg_Details.SelectSegment,
                                                                                                    stg_setting_value.Index,
                                                                                                    Token, newLegDetails), 2);
            }
            double _currentLTP = GetStrikePriceLTP(Token);
            newLegDetails.EntryPrice = _currentLTP;

            newLegDetails.Name = OtherMethods.GetNewName(OldLegDetails.Name);
            return newLegDetails;

        }

        private InnerObject GetLegReEntryForCOST(InnerObject OldLegDetails, LegDetails leg_Details, bool Reverse = false)
        {
            InnerObject newLegDetails = new();

            if (Reverse)
                newLegDetails.BuySell = OldLegDetails.BuySell == EnumPosition.BUY ? EnumPosition.SELL : EnumPosition.BUY;
            else
                newLegDetails.BuySell = OldLegDetails.BuySell;
            newLegDetails.EntryPrice = OldLegDetails.EntryPrice;
            newLegDetails.Qty = OldLegDetails.Qty;
            newLegDetails.StgName = OldLegDetails.StgName;
            newLegDetails.Name = OtherMethods.GetNewName(OldLegDetails.Name);

            newLegDetails.Token = OldLegDetails.Token;
            //calculate the STOP LOSS VALUE
            newLegDetails.StopLoss = GetLegStopLoss_OnEntryPrice(leg_Details.SettingStopLoss,
                                        leg_Details.OptionType,
                                        newLegDetails.BuySell,
                                        leg_Details.StopLoss,
                                        leg_Details.SelectSegment,
                                        OldLegDetails.EntryUnderliying_INST);

            newLegDetails.TargetProfit = GetLegTargetProfit_OnEntryPrice(leg_Details.SettingTargetProfit,
                                        leg_Details.OptionType,
                                        newLegDetails.BuySell,
                                        leg_Details.TargetProfit,
                                        leg_Details.SelectSegment,
                                        OldLegDetails.EntryUnderliying_INST);

            newLegDetails.TradingSymbol = OldLegDetails.TradingSymbol;
            newLegDetails.ReEntryTP = OldLegDetails.ReEntryTP;
            newLegDetails.ReEntrySL = OldLegDetails.ReEntrySL;
            newLegDetails.EntryUnderliying_INST = OldLegDetails.EntryUnderliying_INST;

            return newLegDetails;

        }

        #endregion

        #region Get Re-Entry Details for Leg When TP Hit

        public InnerObject GetLegDetailsForRentry_TPHIT(LegDetails leg_Details, InnerObject OldLegDetails, StrategyDetails stg_setting_value)
        {
            return leg_Details.SettingReEntryOnTgt switch
            {
                EnumLegReEntryOnTarget.RECOST => GetLegReEntryForCOST(OldLegDetails, leg_Details),
                EnumLegReEntryOnTarget.REREVCOST => GetLegReEntryForCOST(OldLegDetails, leg_Details, true),
                EnumLegReEntryOnTarget.REASAP => GetReEntryForASAP(OldLegDetails, leg_Details, stg_setting_value),
                EnumLegReEntryOnTarget.REREVASAP => GetReEntryForASAP(OldLegDetails, leg_Details, stg_setting_value, true),
                EnumLegReEntryOnTarget.REMOMENTUM => GetReEntryForMOMENTUM(OldLegDetails, leg_Details, stg_setting_value),
                EnumLegReEntryOnTarget.REREVMOMENTUM => GetReEntryForMOMENTUM(OldLegDetails, leg_Details, stg_setting_value, true),
                _ => throw new NotImplementedException(),
            };
        }

        #endregion

        #region Get Taril SL HIT Value
        /// <summary>
        /// Provide the New StopLoss When SetAmount Move in Favour OtherWise Return 0;
        /// </summary>
        /// <param name="enumLegTrailSL"></param>
        /// <param name="entryPrice"></param>
        /// <param name="xAmount_Percentage"></param>
        /// <param name="yStopLoss"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        //private bool GetTrailSLHit(EnumLegTrailSL enumLegTrailSL, double entryPrice, double xAmount_Percentage, double ltp)
        //{

        //    return enumLegTrailSL switch
        //    {
        //        EnumLegTrailSL.POINTS => GetLegPointTailSL(entryPrice, xAmount_Percentage, ltp),
        //        EnumLegTrailSL.POINTPERCENTAGE => GetLegPointPercentageTrailSL(entryPrice, xAmount_Percentage, ltp),
        //        _ => throw new NotImplementedException(),
        //    };
        //}

        //private bool GetLegPointPercentageTrailSL(double entryPrice, double xAmount_Percentage, double ltp)
        //{
        //    return entryPrice + entryPrice * xAmount_Percentage / 100.00 >= ltp;
        //}

        //private bool GetLegPointTailSL(double entryPrice, double xAmount_Percentage, double ltp)
        //{
        //    return entryPrice + xAmount_Percentage >= ltp;
        //}

        private double GetLtpForUnderLine(StrategyDetails stg_setting_value, LegDetails leg_Details)
        {
            double ltp;
            if (stg_setting_value.UnderlyingFrom == EnumUnderlyingFrom.CASH)
            {
                ltp = UnderLingValue(stg_setting_value.Index);
            }
            else
            {
                DateTime exp = GetLegExpiry(EnumExpiry.MONTHLY, stg_setting_value.Index, EnumSegments.FUTURES, leg_Details.OptionType);
                uint FUTToken = ContractDetails.ContractDetailsToken.Where(x => x.Value.Expiry == exp
                && x.Value.Opttype == EnumOptiontype.XX
                && x.Value.Symbol == stg_setting_value.Index.ToString())
                     .Select(s => s.Key)
                     .FirstOrDefault();
                //underlying Token
                ltp = GetInstrumentPrice(FUTToken);
            }
            return ltp;
        }

        public void UpdateLegSLTrail_IF_HIT(InnerObject portfolio_leg_value, LegDetails leg_Details, StrategyDetails stg_setting_value)
        {


            double ltp = 0;
            if (leg_Details.SettingTrailEnable == EnumLegTrailSL.UNDERLING || leg_Details.SettingTrailEnable == EnumLegTrailSL.UNDERLINGPERCENTAGE)
            {
                ltp = GetLtpForUnderLine(stg_setting_value, leg_Details);
            }
            else
            {
                ltp = GetStrikePriceLTP(portfolio_leg_value.Token);
            }

            if (leg_Details.SettingTrailEnable == EnumLegTrailSL.POINTS || leg_Details.SettingTrailEnable == EnumLegTrailSL.UNDERLING)
            {


                if (portfolio_leg_value.BuySell == EnumPosition.BUY && (leg_Details.OptionType == EnumOptiontype.CE || leg_Details.OptionType == EnumOptiontype.XX || leg_Details.OptionType == EnumOptiontype.PE))
                {
                    if (portfolio_leg_value.UpdateInFavorAmountforTrailSLleg + leg_Details.TrailSlAmount >= ltp)
                    {
                        portfolio_leg_value.StopLoss -= leg_Details.TrailSlStopLoss; portfolio_leg_value.UpdateInFavorAmountforTrailSLleg -= leg_Details.TrailSlAmount;
                    }
                }
                else if (portfolio_leg_value.BuySell == EnumPosition.SELL && (leg_Details.OptionType == EnumOptiontype.CE || leg_Details.OptionType == EnumOptiontype.XX || leg_Details.OptionType == EnumOptiontype.PE))
                {
                    if (portfolio_leg_value.UpdateInFavorAmountforTrailSLleg + leg_Details.TrailSlAmount >= ltp)
                    {
                        portfolio_leg_value.StopLoss += leg_Details.TrailSlStopLoss; portfolio_leg_value.UpdateInFavorAmountforTrailSLleg += leg_Details.TrailSlAmount;
                    }
                }
            }
            else if (leg_Details.SettingTrailEnable == EnumLegTrailSL.POINTPERCENTAGE || leg_Details.SettingTrailEnable == EnumLegTrailSL.UNDERLINGPERCENTAGE)
            {

                if (portfolio_leg_value.BuySell == EnumPosition.BUY && (leg_Details.OptionType == EnumOptiontype.CE || leg_Details.OptionType == EnumOptiontype.XX || leg_Details.OptionType == EnumOptiontype.PE))
                {
                    if (portfolio_leg_value.UpdateInFavorAmountforTrailSLleg + (portfolio_leg_value.UpdateInFavorAmountforTrailSLleg * leg_Details.TrailSlAmount) >= ltp)
                    {
                        portfolio_leg_value.StopLoss = portfolio_leg_value.StopLoss - portfolio_leg_value.StopLoss * leg_Details.TrailSlStopLoss / 100.00;
                        portfolio_leg_value.UpdateInFavorAmountforTrailSLleg = portfolio_leg_value.UpdateInFavorAmountforTrailSLleg - portfolio_leg_value.UpdateInFavorAmountforTrailSLleg * leg_Details.TrailSlAmount / 100.00;
                    }
                }
                else if (portfolio_leg_value.BuySell == EnumPosition.SELL && (leg_Details.OptionType == EnumOptiontype.CE || leg_Details.OptionType == EnumOptiontype.XX || leg_Details.OptionType == EnumOptiontype.PE))
                {
                    if (portfolio_leg_value.UpdateInFavorAmountforTrailSLleg + (portfolio_leg_value.UpdateInFavorAmountforTrailSLleg * leg_Details.TrailSlAmount) >= ltp)
                    {
                        portfolio_leg_value.StopLoss = portfolio_leg_value.StopLoss + portfolio_leg_value.StopLoss * leg_Details.TrailSlStopLoss / 100.00;
                        portfolio_leg_value.UpdateInFavorAmountforTrailSLleg = portfolio_leg_value.UpdateInFavorAmountforTrailSLleg + portfolio_leg_value.UpdateInFavorAmountforTrailSLleg * leg_Details.TrailSlAmount / 100.00;
                    }
                }
            }
        }
        #endregion

        #region Get Expiry
        public DateTime GetLegExpiry(EnumExpiry enumExpiry, EnumIndex enumIndex, EnumSegments enumSegments, EnumOptiontype enumOptiontype)
        {
            string Symbol = enumIndex.ToString().ToUpper();

            if (ContractDetails.ContractDetailsToken == null)
                throw new Exception("Contract data Not Found");

            if (enumSegments == EnumSegments.FUTURES || enumExpiry == EnumExpiry.MONTHLY)
            {
                DateTime[] data = ContractDetails.ContractDetailsToken.Where(x => x.Value.Symbol == Symbol && x.Value.Opttype == EnumOptiontype.XX).Select(x => x.Value.Expiry).ToArray();
                Array.Sort(data);
                return data[0];
            }
            DateTime[] data1 = ContractDetails.ContractDetailsToken.Where(x => x.Value.Symbol == Symbol && x.Value.Opttype == enumOptiontype).Select(x => x.Value.Expiry).Distinct().ToArray();
            if (data1.Count() > 0)
            {
                Array.Sort(data1);
                if (enumExpiry == EnumExpiry.WEEKLY)
                    return data1[0];
                else if (enumExpiry == EnumExpiry.NEXTWEEKLY)
                    return data1[1];
                else
                    throw new Exception("Invalid Expiry Selected");
            }
            else
                throw new Exception("Expiry Not Found");
        }

        #endregion

        #region Get Simple Momentum Initial Value Only
        /// <summary>
        /// Simple Momentum => . This function will  hold the order  
        /// </summary>
        /// <param name="enumLegSimpleMomentum"></param>
        /// <param name="momentumPrice"></param>
        /// <param name="enumIndex"></param>
        /// <param name="enumExpiry"></param>
        /// <param name="selectedStrike"></param>
        /// <param name="enumOptiontype"></param>
        /// <param name="enumSegments"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<double> GetLegMomentumlock(EnumLegSimpleMomentum enumLegSimpleMomentum,
            double momentumPrice,
            EnumIndex enumIndex
            , uint Token, InnerObject innerObject)
        {
            double _current_Price;
            if (enumLegSimpleMomentum == EnumLegSimpleMomentum.POINTS || enumLegSimpleMomentum == EnumLegSimpleMomentum.POINTPERCENTAGE)
                _current_Price = GetInstrumentPrice(Token);
            else if (enumLegSimpleMomentum == EnumLegSimpleMomentum.UNDERLING || enumLegSimpleMomentum == EnumLegSimpleMomentum.UNDERLINGPERCENTAGE)
                _current_Price = UnderLingValue(enumIndex);
            else
                throw new Exception("Invalid Option Selected");

            if (_current_Price <= 0)
                throw new Exception("Invalid Price Fetched from Feed");



            var value = enumLegSimpleMomentum switch
            {
                EnumLegSimpleMomentum.POINTS => GetLegSimpleMomentum_UnderlyingPoints(momentumPrice, _current_Price),
                EnumLegSimpleMomentum.POINTPERCENTAGE => GetLegSimple_UnderlyingPointPercentage(momentumPrice, _current_Price),
                EnumLegSimpleMomentum.UNDERLING => GetLegSimpleMomentum_UnderlyingPoints(momentumPrice, _current_Price),
                EnumLegSimpleMomentum.UNDERLINGPERCENTAGE => GetLegSimple_UnderlyingPointPercentage(momentumPrice, _current_Price),
                _ => throw new NotImplementedException(),
            };
            innerObject.Message = EnumStrategyMessage.MOMENTUM;
            if (enumLegSimpleMomentum == EnumLegSimpleMomentum.POINTS || enumLegSimpleMomentum == EnumLegSimpleMomentum.POINTPERCENTAGE)
            {
                if (momentumPrice > 0)
                {
                    while (value > GetInstrumentPrice(Token))
                        await Task.Delay(300);
                }
                else
                {
                    while (value < GetInstrumentPrice(Token))
                        await Task.Delay(300);
                }
                return GetInstrumentPrice(Token);
            }
            else if (enumLegSimpleMomentum == EnumLegSimpleMomentum.UNDERLING || enumLegSimpleMomentum == EnumLegSimpleMomentum.UNDERLINGPERCENTAGE)
            {
                if (momentumPrice > 0)
                {
                    while (value > UnderLingValue(enumIndex))
                        await Task.Delay(300);
                }
                else
                {
                    while (value < UnderLingValue(enumIndex))
                        await Task.Delay(300);
                }
            }
            return UnderLingValue(enumIndex);
        }

        private double GetLegSimple_UnderlyingPointPercentage(double momentumPrice, double currentPriceForStrike)
        {

            return currentPriceForStrike + currentPriceForStrike * momentumPrice / 100;
        }

        private double GetLegSimpleMomentum_UnderlyingPoints(double momentumPrice, double currentPriceForStrike)
        {
            return currentPriceForStrike + momentumPrice;
        }


        #endregion

        #region Get RangeBreakOut


        /// <summary>
        /// This function will take time Run it using async only...
        /// </summary>
        /// <param name="enumRangeBreakout"></param>
        /// <param name="enumRangeBreakoutType"></param>
        /// <param name="DataSetOfPrice"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<double> GetRangeBreaKOut(EnumRangeBreakout enumRangeBreakout,
            EnumRangeBreakoutType enumRangeBreakoutType,
            DateTime RangeBreakOutEndTime,
            EnumIndex enumIndex
            , uint Token)
        {
            if (ContractDetails.ContractDetailsToken == null)
                throw new Exception("Contract is Null");

            if (_feed.FeedC == null)
                throw new Exception("Feed FO is NULL");

            if (_feed.FeedCM == null)
                throw new Exception("Feed CM is NULL");



            List<double> SetOfPrice = new();
            if (enumRangeBreakoutType == EnumRangeBreakoutType.INSTRUMENT)
            {

                //add Price in the Set 
                while (RangeBreakOutEndTime >= DateTime.Now)
                {
                    await Task.Delay(300);
                    double price = Convert.ToDouble(_feed.FeedC.dcFeedData[Token].LastTradedPrice / 100.00);
                    if (!SetOfPrice.Contains(price))
                        SetOfPrice.Add(price);
                }
            }
            else if (enumRangeBreakoutType == EnumRangeBreakoutType.UNDERLYING)
            {
                string? SpotString;
                if (enumIndex == EnumIndex.NIFTY) SpotString = "Nifty 50";
                else if (enumIndex == EnumIndex.BANKNIFTY) SpotString = "Nifty Bank";
                else if (enumIndex == EnumIndex.FINNIFTY) SpotString = "Nifty Fin Service";
                else if (enumIndex == EnumIndex.MIDCPNIFTY) SpotString = "NIFTY MID SELECT";
                else SpotString = null;
                if (SpotString == null)
                    throw new Exception();

                //add Price in the Set 
                while (RangeBreakOutEndTime >= DateTime.Now)
                {
                    await Task.Delay(300);
                    double price = _feed.FeedCM.dcFeedDataIdx[SpotString].IndexValue;
                    if (!SetOfPrice.Contains(price))
                        SetOfPrice.Add(price);
                }
            }
            else
                throw new Exception("Invalid Option Selected");


            return enumRangeBreakout switch
            {
                EnumRangeBreakout.HIGH => GetRangeBreaKOutHigh(SetOfPrice),
                EnumRangeBreakout.LOW => GetRangeBreaKOutILow(SetOfPrice),
                _ => throw new NotImplementedException(),
            };
        }

        private double GetRangeBreaKOutILow(List<double> _setOfPrice)
        {
            return _setOfPrice.Min();
        }

        private double GetRangeBreaKOutHigh(List<double> _setOfPrice)
        {
            return _setOfPrice.Max();
        }

        #endregion

        #region Get MTM
        public double GetMTM(double BuyAvg_Price, double SellAvg_Price, double Exp_, int BuyTrdqty, int SellTrdQty, uint Token)
        {
            if (_feed.FeedC == null)
                throw new Exception("Feed for F&O not init.");
            uint Bid_Ask = BuyTrdqty - SellTrdQty > 0 ? _feed.FeedC.dcFeedData[Token].AskPrice1 : _feed.FeedC.dcFeedData[Token].BidPrice1;
            return (BuyAvg_Price * BuyTrdqty) + (SellAvg_Price * SellTrdQty) + Math.Abs(BuyTrdqty - SellTrdQty) * Bid_Ask - Math.Max(BuyTrdqty, SellTrdQty) * Exp_;
        }
        #endregion

        #region Get Expence
        public double GetExpenses(uint Token, EnumSegments enumSegments)
        {
            if (_feed.FeedC == null)
                throw new Exception("Feed for F&O not init.");
            if (EnumSegments.OPTIONS == enumSegments)
                return _feed.FeedC.dcFeedData[Token].BidPrice1 * STT_Opt + (_feed.FeedC.dcFeedData[Token].BidPrice1 + _feed.FeedC.dcFeedData[Token].AskPrice1) * Exp_Opt + _feed.FeedC.dcFeedData[Token].AskPrice1 * StampDuty_Opt;
            else
                return _feed.FeedC.dcFeedData[Token].BidPrice1 * Stt_Fut + (_feed.FeedC.dcFeedData[Token].BidPrice1 + _feed.FeedC.dcFeedData[Token].AskPrice1) * Exp_Fut + _feed.FeedC.dcFeedData[Token].AskPrice1 * StampDuty_Fut;
        }
        #endregion

        #region Get Stike Price using Token LTP

        public double GetStrikePriceLTP(uint Token)
        {
            if (_feed.FeedC == null)
                throw new Exception("Feed for F&O not init.");
            return Convert.ToDouble(_feed.FeedC.dcFeedData[Token].LastTradedPrice) / 100.00;

        }
        #endregion

        #region Get Overall SL Value

        public double GetOverallStopLossValue(double TotalPremium, EnumOverallStopLoss enumOverallStopLoss, double stopLossValue)
        {
            return enumOverallStopLoss switch
            {
                EnumOverallStopLoss.TOTALPREMIUMPERCENTAGE => GetOverallStopLossValueUsingPremium(TotalPremium, stopLossValue),
                EnumOverallStopLoss.MTM => GetOverallStopLossValueUsingMtm(stopLossValue),
                _ => throw new NotImplementedException(),
            };
        }

        private double GetOverallStopLossValueUsingMtm(double stopLossValue)
        {
            return -stopLossValue;
        }

        private double GetOverallStopLossValueUsingPremium(double TotalPremium, double stopLossValue)
        {
            return -(TotalPremium * stopLossValue / 100.0);
        }

        #endregion

        #region Get Overall TP Value

        public double GetOverallTargetProfitValue(double TotalPremium, EnumOverallTarget enumOverallTargetProfit, double TargetPofit)
        {
            return enumOverallTargetProfit switch
            {
                EnumOverallTarget.TOTALPREMIUMPERCENTAGE => GetOverallTPValueUsingPremium(TotalPremium, TargetPofit),
                EnumOverallTarget.MTM => GetOverallTPValueUsingMtm(TargetPofit, TotalPremium),
                _ => throw new NotImplementedException(),
            };
        }

        private double GetOverallTPValueUsingMtm(double targetPofit, double TotalPremium)
        {
            return targetPofit;
        }

        private double GetOverallTPValueUsingPremium(double totalPremium, double targetPofit)
        {
            return (totalPremium * targetPofit / 100.0);
        }
        #endregion

        #region Check if Overall SL is HIT


        public bool Is_overall_sl_hit(StrategyDetails stg_setting_value, PortfolioModel portfolio_value)
        {
            return stg_setting_value.SettingOverallStopLoss switch
            {
                EnumOverallStopLoss.MTM => CheckIfStopLossHitUsingMTM(portfolio_value),
                EnumOverallStopLoss.TOTALPREMIUMPERCENTAGE => CheckIfStopLossHitUsingPremium(portfolio_value),
                _ => throw new NotImplementedException(),
            };
        }

        private bool CheckIfStopLossHitUsingPremium(PortfolioModel portfolio_value)
        {
            var _currentPremium = portfolio_value.TotalEntryPremiumPaid + portfolio_value.PNL;
            if (portfolio_value.StopLoss >= _currentPremium)
            {
                return true;
            }

            return false;
        }

        private bool CheckIfStopLossHitUsingMTM(PortfolioModel portfolio_value)
        {

            if (portfolio_value.StopLoss >= portfolio_value.PNL)
            {
                return true;
            }

            return false;
        }
        #endregion

        #region Check if Overall TP is HIT

        public bool Is_overall_tp_hit(StrategyDetails stg_setting_value, PortfolioModel portfolio_value)
        {
            return stg_setting_value.SettingOverallTarget switch
            {
                EnumOverallTarget.MTM => CheckIfTargetProfitHitUsingMTM(portfolio_value),
                EnumOverallTarget.TOTALPREMIUMPERCENTAGE => CheckIfTargetProfitHitUsingPremium(portfolio_value),
                _ => throw new NotImplementedException(),
            };
        }

        private bool CheckIfTargetProfitHitUsingPremium(PortfolioModel portfolio_value)
        {
            var _currentPremium = portfolio_value.TotalEntryPremiumPaid + portfolio_value.PNL;
            if (_currentPremium >= portfolio_value.TargetProfit)
            {
                return true;
            }

            return false;
        }

        private bool CheckIfTargetProfitHitUsingMTM(PortfolioModel portfolio_value)
        {
            if (portfolio_value.TargetProfit <= portfolio_value.PNL)
            {
                return true;
            }

            return false;
        }


        #endregion


        #region Is Price Match For Re entry
        public async Task<bool> IsMyPriceHITforCost(bool sL_HIT, bool tP_HIT, double entryPrice, uint token, bool CancelOrReject)
        {
            try
            {
                if (sL_HIT)
                {
                    while (entryPrice >= GetInstrumentPrice(token))
                    {
                        await Task.Delay(400);
                        if(CancelOrReject)
                            break;

                    }
                }
                else if (tP_HIT)
                {
                    while (GetInstrumentPrice(token) <= entryPrice)
                    {
                        await Task.Delay(400);
                        if (CancelOrReject)
                            break;
                    }
                }
                return true;
            }
            catch { }
            {
                return false;
            }
        }
        #endregion

        #region overall Trailling Option
        public void CheckAndUpdateOverallTrailingOption(PortfolioModel portfolio_value, StrategyDetails stg_setting_value)
        {
            var data = stg_setting_value.SettingOverallTrallSL switch
            {
                EnumOverallTrailingOption.LOCK => CheckTheLock(portfolio_value, stg_setting_value),
                EnumOverallTrailingOption.LOCKANDTRAIL => CheckTheLockAndTrail(portfolio_value, stg_setting_value),
                EnumOverallTrailingOption.OVERALLTRAILANDSL => CheckTheOverallTrailAndSL(portfolio_value, stg_setting_value),
                _ => throw new NotImplementedException(),
            };
        }

        private bool CheckTheOverallTrailAndSL(PortfolioModel portfolio_value, StrategyDetails stg_setting_value)
        {
            return stg_setting_value.SettingTrallingOption switch
            {
                EnumOverallTrailingOptionTrailAndSLSelected.MTM => CheckTrailSLusingMtm(portfolio_value, stg_setting_value),
                EnumOverallTrailingOptionTrailAndSLSelected.TOTALPREMIUMPERCENTAGE => CheckTrailSLusingPremiumPercentage(portfolio_value, stg_setting_value),
                _ => throw new NotImplementedException(),
            };
        }

        private bool CheckTrailSLusingPremiumPercentage(PortfolioModel portfolio_value, StrategyDetails stg_setting_value)
        {
            //check Premium in favour
            var _currentPremium = portfolio_value.UpdateInFavorPremiumPaidforTrailSLleg + portfolio_value.PNL;

            //%% in favour 
            var _neededAmountMove = portfolio_value.UpdateInFavorPremiumPaidforTrailSLleg + portfolio_value.UpdateInFavorPremiumPaidforTrailSLleg * stg_setting_value.TrailAmountMove / 100;


            if (_currentPremium >= _neededAmountMove)
            {

                //Check Big JUMP
                var jump = _neededAmountMove - portfolio_value.PNL;
                int remainder;
                int quotient = Math.DivRem((int)jump, (int)stg_setting_value.TrailAmountMove, out remainder);

                if (quotient < 1)
                    quotient = 1;


                portfolio_value.UpdateInFavorPremiumPaidforTrailSLleg = _neededAmountMove;
                portfolio_value.StopLoss = Math.Round(portfolio_value.StopLoss + (stg_setting_value.TrailSLMove * quotient), 2);

                return true;
            }
            return false;
        }

        private bool CheckTrailSLusingMtm(PortfolioModel portfolio_value, StrategyDetails stg_setting_value)
        {
            //Check MTM in Favour 
            var _needMTMToMove = portfolio_value.UpdateInInitialMTMPaidforTrailSLleg + stg_setting_value.TrailAmountMove;
            if (portfolio_value.PNL >= _needMTMToMove)
            {
                //Check THE big JUMP

                var jump = portfolio_value.PNL - _needMTMToMove;
                int remainder;
                int quotient = Math.DivRem((int)jump, (int)stg_setting_value.TrailAmountMove, out remainder);

                if (quotient < 1)
                    quotient = 1;


                portfolio_value.UpdateInInitialMTMPaidforTrailSLleg = _needMTMToMove;
                portfolio_value.StopLoss = Math.Round(portfolio_value.StopLoss + (stg_setting_value.TrailSLMove * quotient), 2);

                return true;
            }
            return false;
        }

        private bool CheckTheLockAndTrail(PortfolioModel portfolio_value, StrategyDetails stg_setting_value)
        {
            if (portfolio_value.PNL >= stg_setting_value.IfProfitReach && portfolio_value.LockProfitUsed == 0)
            {
                stg_setting_value.IsOverallStopLossEnable = true;
                stg_setting_value.SettingOverallStopLoss = EnumOverallStopLoss.MTM;

                portfolio_value.StopLoss = stg_setting_value.LockProfit;
                portfolio_value.LockProfitUsed = 1;//WILL Update Once
            }
            else if (portfolio_value.LockProfitUsed == 1 && portfolio_value.StopLoss != 0)
            {
                //Code For Trail
                CheckTrailSLusingMtm(portfolio_value, stg_setting_value);
            }
            return true;
        }

        private bool CheckTheLock(PortfolioModel portfolio_value, StrategyDetails stg_setting_value)
        {
            if (portfolio_value.PNL >= stg_setting_value.IfProfitReach && portfolio_value.LockProfitUsed == 0)
            {

                stg_setting_value.IsOverallStopLossEnable = true;
                stg_setting_value.SettingOverallStopLoss = EnumOverallStopLoss.MTM;

                portfolio_value.StopLoss = stg_setting_value.LockProfit;
                portfolio_value.LockProfitUsed = 1;//WILL Update Once
            }
            return true;
        }
        #endregion
    }
}
