using System;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.Model
{
    #region Strategy Level Setting ... 

    public class StrategyDetails
    {
        //InstrumentSettings


        public EnumIndex Index = EnumIndex.NIFTY;
        public EnumUnderlyingFrom UnderlyingFrom = EnumUnderlyingFrom.CASH;


        //EntrySettings


        public EnumEntryAndExit EntryAndExitSetting = EnumEntryAndExit.TIMEBASED;
        public EnumStrategyType StartgyType = EnumStrategyType.INTRADAY;

        //TimeBased ==> EntryAndExitSetting
        public DateTime EntryTime = DateTime.Today.AddHours(9).AddMinutes(35); //Defult

        //TimeBased ==> EntryAndExitSetting || OnlyEntrySignalBased==> Below Time Will Be exit based
        public DateTime ExitTime = DateTime.Today.AddHours(15).AddMinutes(15); //Defult

        //SignalBases ==> otherSignal
        public EnumSignalType Signal = EnumSignalType.NONE;


        //LegwiseSLsettings


        public EnumSquareOff SquareOff = EnumSquareOff.PARTIAL;

        public bool IsTrailSLtoBreakEvenPriceEnable = false;
        public EnumTrailSLtoBreakEvenPrice TrailSLtoBreakEvenPrice = EnumTrailSLtoBreakEvenPrice.ALLLEGS;


        //OverallStrategySettings


        public bool IsOverallStopLossEnable = false;
        public EnumOverallStopLoss SettingOverallStopLoss = EnumOverallStopLoss.MTM;
        public double OverallStopLoss = 0;

        public bool IsOverallReEntryOnSLEnable = false;
        public EnumOverallReEntryOnSL SettingOverallReEntryOnSL = EnumOverallReEntryOnSL.REASAP;
        public int OverallReEntryOnSL = 0;

        public bool IsOverallTargetEnable = false;
        public EnumOverallTarget SettingOverallTarget = EnumOverallTarget.MTM;
        public double OverallTarget = 0;

        public bool IsOverallReEntryOnTgtEnable = false;
        public EnumOverallReEntryOnTarget SettingOverallReEntryOnTgt = EnumOverallReEntryOnTarget.REASAP;
        public int OverallReEntryOnTgt = 0;

        public bool IsOverallTrallingOptionEnable = false;
        public bool IsOverallTrallSLEnable = false;
        public EnumOverallTrailingOption SettingOverallTrallSL = EnumOverallTrailingOption.LOCK;
        public EnumOverallTrailingOptionTrailAndSLSelected SettingTrallingOption = EnumOverallTrailingOptionTrailAndSLSelected.MTM;
        public double IfProfitReach = 0;
        public double LockProfit = 0;
        public double ForEveryIncreaseInProfitBy = 0;
        public double Trailprofitby = 0;
        public double TrailAmountMove = 0;
        public double TrailSLMove = 0;



        //User
        public string? UserID;
    }
    public class LegDetails
    {
        //LegSetting

        public bool IsTargetProfitEnable = false;
        public EnumLegTargetProfit SettingTargetProfit = EnumLegTargetProfit.POINTS;
        public double TargetProfit = 0;

        public bool IsStopLossEnable = false;
        public EnumLegSL SettingStopLoss = EnumLegSL.POINTS;
        public double StopLoss = 0;

        public bool IsTrailSlEnable = false;
        public EnumLegTrailSL SettingTrailEnable = EnumLegTrailSL.POINTS;
        public double TrailSlAmount = 0;
        public double TrailSlStopLoss = 0;

        public bool IsReEntryOnTgtEnable = false;
        public EnumLegReEntryOnTarget SettingReEntryOnTgt = EnumLegReEntryOnTarget.REASAP;
        public int ReEntryOnTgt = 0;

        public bool IsReEntryOnSLEnable = false;
        public EnumLegReEntryOnSL SettingReEntryOnSL = EnumLegReEntryOnSL.REASAP;
        public int ReEntryOnSL = 0;

        public bool IsSimpleMomentumEnable = false;
        public EnumLegSimpleMomentum SettingSimpleMomentum = EnumLegSimpleMomentum.POINTS;
        public double SimpleMomentum = 0;

        public bool IsRangeBreakOutEnable = false;
        public DateTime RangeBreakOutEndTime = DateTime.Today.AddHours(9).AddMinutes(45);
        public EnumRangeBreakout SettingRangeBreakOut = EnumRangeBreakout.HIGH;
        public EnumRangeBreakoutType SettingRangeBreakOutType = EnumRangeBreakoutType.UNDERLYING;

        //Leg

        public EnumSegments SelectSegment = EnumSegments.OPTIONS;
        public int Lots = 1;
        public EnumPosition Position = EnumPosition.BUY;
        public EnumOptiontype OptionType = EnumOptiontype.CE;
        public EnumExpiry Expiry = EnumExpiry.WEEKLY;
        public EnumSelectStrikeCriteria StrikeCriteria = EnumSelectStrikeCriteria.STRIKETYPE;
        public EnumStrikeType StrikeType = EnumStrikeType.ATM;
        public double PremiumRangeLower = 0;
        public double PremiumRangeUpper = 0;
        public double Premium_or_StraddleWidth = 0;




    }

    #endregion
}
