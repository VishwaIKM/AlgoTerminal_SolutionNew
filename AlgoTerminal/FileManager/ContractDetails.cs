using AlgoTerminal.CustumException;
using AlgoTerminal.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.FileManager
{
    public class ContractDetails
    {
        #region Find the latest avaliable Contract file in CON AKJ

        private static readonly string DefultContractPath = "C:\\CON_AKJ\\NSE_FO_contract_" + DateTime.Now.ToString("ddMMyyyy") + ".csv";
        private static readonly DirectoryInfo Info = new DirectoryInfo("C:\\CON_AKJ\\");
        private static readonly FileInfo[] filePaths = Info.GetFiles().OrderByDescending(p => p.CreationTime).Where(x => x.Name.Contains("NSE_FO_contract_") && x.Name.Contains(".csv")).ToArray();
        private static string S_Contract_File_Path = filePaths.Count() <= 0 ? DefultContractPath : filePaths[0].FullName;

        public static uint NiftyFutureToken;
        public static uint BankNiftyFutureToken;
        public static uint FinNiftyFutureToken;

        #endregion

        /// <summary>
        /// Access of Contract Details Where Key is uint==>TOKEN
        /// </summary>
        /// 

        #region Properties and Methods
        public static ConcurrentDictionary<uint, ContractRecord.ContractData>? ContractDetailsToken { get; set; }
        /// <summary>
        /// Load Contract Details. The Path need to Manage Manually for now For Unit Test and Live.
        /// </summary>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="ContractLoadingFailed_Exception"></exception>
        public static void LoadContractDetails()
        {

            //Below Contract for UnitTestCaseOnly
            //S_Contract_File_Path = @"D:\Development Vishwa\AlgoTerminal_Solution\UnitTest_Resources\NSE_FO_contract_01062023.csv";
            //Exception will handle in Invoke Method LvL
            if (ContractDetailsToken != null)
            {
                ContractDetailsToken.Clear();
                ContractDetailsToken = null;
            }
            try
            {
                using (FileStream _fs = new(S_Contract_File_Path, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader _sw = new(_fs))
                    {
                        _sw.ReadLine();
                        while (!_sw.EndOfStream)
                        {
                            ContractRecord.ContractData cntrInfo = new();
                            string? line = _sw.ReadLine();

                            if (line == null)
                                continue;

                            string[] arrline = line.Split(',');

                            if (string.IsNullOrEmpty(arrline[18]) || string.IsNullOrWhiteSpace(arrline[18]))
                                continue;

                            cntrInfo.TokenID = Convert.ToUInt32(arrline[0].Trim());

                            if (cntrInfo.TokenID < 1)
                                continue;

                            DateTime dt = Convert.ToDateTime("1/1/1980 12:00:00 AM");//.Add(diff);
                            dt = dt.AddSeconds(Convert.ToInt32(arrline[4]));//cols[28]));//
                            cntrInfo.Expiry = dt;

                            cntrInfo.Symbol = arrline[3].Trim();
                            cntrInfo.Strike = Convert.ToDouble(arrline[5].Trim()) / 100;
                            cntrInfo.LotSize = Convert.ToUInt32(arrline[8].Trim());

                            cntrInfo.FreezeQnty = (int)Convert.ToDouble(arrline[40]);

                            cntrInfo.Opttype = 0;
                            if (arrline[6].Trim() == EnumOptiontype.CE.ToString())
                                cntrInfo.Opttype = EnumOptiontype.CE;
                            if (arrline[6].Trim() == EnumOptiontype.PE.ToString())
                                cntrInfo.Opttype = EnumOptiontype.PE;
                            if (arrline[6].Trim() == EnumOptiontype.XX.ToString())
                                cntrInfo.Opttype = EnumOptiontype.XX;

                            cntrInfo.InstrumentType = arrline[2].Trim();
                            cntrInfo.TrdSymbol = arrline[18];

                            ContractDetailsToken ??= new();


                            if (!ContractDetailsToken.ContainsKey(cntrInfo.TokenID))
                            {
                                ContractDetailsToken.TryAdd(cntrInfo.TokenID, cntrInfo);
                            }
                        }
                    };
                };
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException(DefultContractPath);
            }
            catch (Exception ex)
            {
                throw new ContractLoadingFailed_Exception(ex.Message);
            }

            if (ContractDetailsToken == null)
                throw new ContractLoadingFailed_Exception("Contract Not loaded. Probability is Contract file is blank or Used by another Process.");

            LoadFutToken();
        }
        /// <summary>
        /// Get Contract Details By Token
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static ContractRecord.ContractData GetContractDetailsByToken(uint Token)
        {
            if (ContractDetailsToken == null)
                throw new Exception("Contract File Not Loaded!");

            if (ContractDetailsToken.TryGetValue(Token, out ContractRecord.ContractData? value))
                return value;

            return null;
        }
        /// <summary>
        /// Get By Trading Symbol
        /// </summary>
        /// <param name="TradingSymbol"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static ContractRecord.ContractData GetContractDetailsByTradingSymbol(string TradingSymbol)
        {
            if (ContractDetailsToken == null)
                throw new Exception("Contract File Not Loaded!");

            ContractRecord.ContractData value = ContractDetailsToken.Where(x => x.Value.TrdSymbol == TradingSymbol).Select(x => x.Value).First();
            return value;
        }

        public static uint GetTokenByContractValue(DateTime exp, EnumOptiontype enumOptiontype, EnumIndex enumIndex, double selectedStrike)
        {
            if (ContractDetailsToken == null)
                throw new Exception("Contract File Not Loaded!");


            return ContractDetailsToken.Where(x => x.Value.Expiry == exp
              && x.Value.Opttype == enumOptiontype
              && x.Value.Symbol == enumIndex.ToString().ToUpper()
              && x.Value.Strike == selectedStrike).Select(xx => xx.Key).FirstOrDefault();
        }

        public static uint GetTokenByContractValue(DateTime expiry, EnumOptiontype xX, EnumIndex index)
        {
            if (ContractDetailsToken == null)
                throw new Exception("Contract File Not Loaded!");

            return ContractDetailsToken.Where(x => x.Value.Expiry == expiry
             && x.Value.Opttype == xX
             && x.Value.Symbol == index.ToString().ToUpper()).Select(xx => xx.Key).FirstOrDefault();
        }

        private static void LoadFutToken()
        {
            DateTime[] exp = ContractDetailsToken.Where(x => x.Value.Symbol == "NIFTY" && x.Value.InstrumentType == "FUTIDX").Select(x => Convert.ToDateTime(x.Value.Expiry)).ToArray();

            if (exp.Count() > 0)
            {
                Array.Sort(exp);
                string eNifty = string.Format("NIFTY{0}FUT", exp[0].ToString("yyMMM").ToUpper());
                string finNifty = string.Format("FINNIFTY{0}FUT", exp[0].ToString("yyMMM").ToUpper());
                string eBank = string.Format("BANKNIFTY{0}FUT", exp[0].ToString("yyMMM").ToUpper());

                NiftyFutureToken = ContractDetailsToken.Where(x => x.Value.TrdSymbol == eNifty).Select(x => x.Key).First();
                BankNiftyFutureToken = ContractDetailsToken.Where(x => x.Value.TrdSymbol == eBank).Select(x => x.Key).First();
                FinNiftyFutureToken = ContractDetailsToken.Where(x => x.Value.TrdSymbol == finNifty).Select(x => x.Key).First();


            }
        }

        #endregion
    }
}
