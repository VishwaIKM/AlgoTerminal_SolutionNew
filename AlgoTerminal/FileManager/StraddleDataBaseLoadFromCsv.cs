using AlgoTerminal.Model;
using AlgoTerminal.Services;
using System;
using System.Collections.Concurrent;
using System.IO;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.FileManager
{
    public class StraddleDataBaseLoadFromCsv : IStraddleDataBaseLoadFromCsv
    {
        #region DataBase of Straddle

        // For Strategy 
        public ConcurrentDictionary<string, StrategyDetails>? Master_Straddle_Dictionary { get; set; }

        //For LegDetails First Key is Stratgy send is Leg Key
        public ConcurrentDictionary<string, ConcurrentDictionary<string, LegDetails>>? Straddle_LegDetails_Dictionary { get; set; }

        #endregion
        private readonly ILogFileWriter _logWriter;
        public StraddleDataBaseLoadFromCsv(ILogFileWriter logWriter)
        {
            _logWriter = logWriter;
        }

        public bool LoadStaddleStratgy(string path)
        {
            bool status = false;
            try
            {
                using FileStream _fs = new(path, FileMode.Open, FileAccess.Read);
                using StreamReader _reader = new(_fs);

                Master_Straddle_Dictionary ??= new();
                Straddle_LegDetails_Dictionary ??= new();

                string _PreviousKey = "NULL HERE";
                int _row = 0;
                string line;
                while ((line = _reader.ReadLine()) != null)
                {
                    _row++;
                    if (_row < 4)
                        continue;

                    string[] _strategy = line.Split(',');
                    string key = _strategy[0];

                    if (!key.ToUpper().Contains("LEG"))
                    {
                        _PreviousKey = key;

                        StrategyDetails strategyDetails = new();

                        if (!Enum.TryParse(_strategy[1].ToUpper(), out strategyDetails.Index))
                        {
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Invalid Strategy Index");
                            throw new Exception("Data Is Invalid for Index");
                        }
                        if (!Enum.TryParse(_strategy[2].ToUpper(), out strategyDetails.UnderlyingFrom))
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[2]);
                        if (!Enum.TryParse(_strategy[3].ToUpper(), out strategyDetails.StartgyType))
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[3]);
                        if (!Enum.TryParse(_strategy[4].ToUpper(), out strategyDetails.EntryAndExitSetting))
                            _logWriter.WriteLog(EnumLogType.Warning, "On Line " + _row + " Error : " + _strategy[4]);
                        if (!DateTime.TryParse(_strategy[5], out strategyDetails.EntryTime))
                            _logWriter.WriteLog(EnumLogType.Warning, "On Line " + _row + " Error : " + _strategy[5]);
                        if (!DateTime.TryParse(_strategy[6], out strategyDetails.ExitTime))
                            _logWriter.WriteLog(EnumLogType.Warning, "On Line " + _row + " Error : " + _strategy[6]);
                        if (!Enum.TryParse(_strategy[7].ToUpper(), out strategyDetails.Signal))
                            _logWriter.WriteLog(EnumLogType.Warning, "On Line " + _row + " Error : " + _strategy[7]);
                        if (!Enum.TryParse(_strategy[8].ToUpper(), out strategyDetails.SquareOff))
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[8]);

                        if (_strategy[9].ToUpper() == "TRUE")
                        {
                            strategyDetails.IsTrailSLtoBreakEvenPriceEnable = true;
                            if (!Enum.TryParse(_strategy[10], out strategyDetails.TrailSLtoBreakEvenPrice))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[10]);
                        }

                        //Overall StopLoss
                        if (_strategy[11].ToUpper() == "TRUE")
                        {
                            strategyDetails.IsOverallStopLossEnable = true;
                            if (!Enum.TryParse(_strategy[12], out strategyDetails.SettingOverallStopLoss))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[12]);
                            if (!double.TryParse(_strategy[13], out strategyDetails.OverallStopLoss))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[13]);
                        }

                        //Overall ReEntry on SL
                        if (_strategy[14].ToUpper() == "TRUE")
                        {
                            strategyDetails.IsOverallReEntryOnSLEnable = true;
                            if (!Enum.TryParse(_strategy[15], out strategyDetails.SettingOverallReEntryOnSL))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[15]);
                            if (!int.TryParse(_strategy[16], out strategyDetails.OverallReEntryOnSL))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[16]);
                        }

                        //Overall Target
                        if (_strategy[17].ToUpper() == "TRUE")
                        {
                            strategyDetails.IsOverallTargetEnable = true;
                            if (!Enum.TryParse(_strategy[18], out strategyDetails.SettingOverallTarget))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[18]);
                            if (!double.TryParse(_strategy[19], out strategyDetails.OverallTarget))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[19]);
                        }

                        //Overall ReEntry on Target
                        if (_strategy[20].ToUpper() == "TRUE")
                        {
                            strategyDetails.IsOverallReEntryOnTgtEnable = true;
                            if (!Enum.TryParse(_strategy[21], out strategyDetails.SettingOverallReEntryOnTgt))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[21]);
                            if (!int.TryParse(_strategy[22], out strategyDetails.OverallReEntryOnTgt))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[22]);
                        }

                        // Trailling Option

                        if (_strategy[23].ToUpper() == "TRUE")
                        {
                            strategyDetails.IsOverallTrallingOptionEnable = true;
                            if (_strategy[24].ToUpper() == "TRUE")
                            {
                                strategyDetails.IsOverallTrallSLEnable = true;
                            }
                            if (!Enum.TryParse(_strategy[26], out strategyDetails.SettingTrallingOption))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[26]);

                            if (!Enum.TryParse(_strategy[25], out strategyDetails.SettingOverallTrallSL))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[25]);

                            if (!double.TryParse(_strategy[27], out strategyDetails.IfProfitReach))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[27]);
                            if (!double.TryParse(_strategy[28], out strategyDetails.LockProfit))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[28]);
                            if (!double.TryParse(_strategy[29], out strategyDetails.ForEveryIncreaseInProfitBy))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[29]);
                            if (!double.TryParse(_strategy[30], out strategyDetails.Trailprofitby))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[30]);
                            if (!double.TryParse(_strategy[31], out strategyDetails.TrailAmountMove))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[31]);
                            if (!double.TryParse(_strategy[32], out strategyDetails.TrailSLMove))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[32]);

                            //USER

                        }

                        strategyDetails.UserID = _strategy[33];
                        //Data loading to  Concurrent Collections 
                        if (!Master_Straddle_Dictionary.ContainsKey(key))
                        {
                            Master_Straddle_Dictionary.TryAdd(key, strategyDetails);
                        }

                    }
                    else
                    {
                        LegDetails legDetails = new();

                        if (!Enum.TryParse(_strategy[1].ToUpper(), out legDetails.SelectSegment))
                        {
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Invalid Strategy Index");
                            throw new Exception("Data Is Invalid for Index");
                        }
                        if (!int.TryParse(_strategy[2], out legDetails.Lots))
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[2]);
                        if (!Enum.TryParse(_strategy[3].ToUpper(), out legDetails.Position))
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[3]);
                        if (!Enum.TryParse(_strategy[4].ToUpper(), out legDetails.OptionType))
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[4]);
                        if (!Enum.TryParse(_strategy[5].ToUpper(), out legDetails.Expiry))
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[5]);
                        if (!Enum.TryParse(_strategy[6].ToUpper(), out legDetails.StrikeCriteria))
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[6]);
                        if (!Enum.TryParse(_strategy[7].ToUpper(), out legDetails.StrikeType))
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[7]);
                        if (!double.TryParse(_strategy[8], out legDetails.PremiumRangeLower))
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[8]);
                        if (!double.TryParse(_strategy[9], out legDetails.PremiumRangeUpper))
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[9]);
                        if (!double.TryParse(_strategy[10], out legDetails.Premium_or_StraddleWidth))
                            _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[10]);


                        //Leg Setting
                        // Target
                        if (_strategy[11].ToUpper() == "TRUE")
                        {
                            legDetails.IsTargetProfitEnable = true;
                            if (!Enum.TryParse(_strategy[12], out legDetails.SettingTargetProfit))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[12]);
                            if (!double.TryParse(_strategy[13], out legDetails.TargetProfit))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[13]);
                        }

                        //IsStopLoss Enable
                        if (_strategy[14].ToUpper() == "TRUE")
                        {
                            legDetails.IsStopLossEnable = true;
                            if (!Enum.TryParse(_strategy[15], out legDetails.SettingStopLoss))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[15]);
                            if (!double.TryParse(_strategy[16], out legDetails.StopLoss))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[16]);
                        }
                        //IsTrail SL Enable
                        if (_strategy[17].ToUpper() == "TRUE")
                        {
                            legDetails.IsTrailSlEnable = true;
                            if (!Enum.TryParse(_strategy[18], out legDetails.SettingTrailEnable))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[18]);
                            if (!double.TryParse(_strategy[19], out legDetails.TrailSlAmount))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[19]);
                            if (!double.TryParse(_strategy[20], out legDetails.TrailSlStopLoss))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[20]);

                        }
                        //Re entry on target
                        if (_strategy[21].ToUpper() == "TRUE")
                        {
                            legDetails.IsReEntryOnTgtEnable = true;
                            if (!Enum.TryParse(_strategy[22], out legDetails.SettingReEntryOnTgt))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[22]);
                            if (!int.TryParse(_strategy[23], out legDetails.ReEntryOnTgt))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[23]);
                        }
                        //Re entry on SL
                        if (_strategy[24].ToUpper() == "TRUE")
                        {
                            legDetails.IsReEntryOnSLEnable = true;
                            if (!Enum.TryParse(_strategy[25], out legDetails.SettingReEntryOnSL))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[25]);
                            if (!int.TryParse(_strategy[26], out legDetails.ReEntryOnSL))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[26]);
                        }
                        //Momentum
                        if (_strategy[27].ToUpper() == "TRUE")
                        {
                            legDetails.IsSimpleMomentumEnable = true;
                            if (!Enum.TryParse(_strategy[28], out legDetails.SettingSimpleMomentum))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[28]);
                            if (!double.TryParse(_strategy[29], out legDetails.SimpleMomentum))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[29]);
                        }
                        //BreakOut
                        if (_strategy[30].ToUpper() == "TRUE")
                        {
                            legDetails.IsRangeBreakOutEnable = true;
                            if (!DateTime.TryParse(_strategy[31], out legDetails.RangeBreakOutEndTime))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[31]);
                            if (!Enum.TryParse(_strategy[32], out legDetails.SettingRangeBreakOut))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[32]);
                            if (!Enum.TryParse(_strategy[33], out legDetails.SettingRangeBreakOutType))
                                _logWriter.WriteLog(EnumLogType.Error, "On Line " + _row + " Error : " + _strategy[33]);
                        }


                        //Data loading to  Concurrent Collections 
                        if (!Straddle_LegDetails_Dictionary.ContainsKey(_PreviousKey))
                        {
                            ConcurrentDictionary<string, LegDetails> data = new();
                            data.TryAdd(key, legDetails);
                            Straddle_LegDetails_Dictionary.TryAdd(_PreviousKey, data);
                        }
                        else
                        {
                            var Leg_Dic = Straddle_LegDetails_Dictionary[_PreviousKey];
                            if (!Leg_Dic.ContainsKey(key))
                            {
                                Leg_Dic.TryAdd(key, legDetails);
                            }
                        }
                    }
                }
                status = true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.StackTrace);
            }
            return status;
        }
    }
}
