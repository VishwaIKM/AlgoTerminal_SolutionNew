using AlgoTerminal.Model;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AlgoTerminal.Manager
{
    public class General
    {

        public static List<string> TokenList = new List<string>();
        //MAIN portfolio dic
        public static ConcurrentDictionary<string, PortfolioModel>? Portfolios { get; set; } // key()=> stg {"name"}

        //Support 
        public static ConcurrentDictionary<uint, List<InnerObject>>? PortfolioLegByTokens { get; set; } //KEY()=>uint 

        //Usinf

        public static void AddToken(string token)
        {
            TokenList.Add(token);
        }
        public static void RemoveToken(string token)
        {
            TokenList.Remove(token);
        }

        public static bool IsTokenFound(string token)
        {
            return TokenList.Contains(token);
        }

    }
}
