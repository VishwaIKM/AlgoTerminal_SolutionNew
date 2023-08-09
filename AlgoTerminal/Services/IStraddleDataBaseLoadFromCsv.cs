using AlgoTerminal.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgoTerminal.Services
{
    public interface IStraddleDataBaseLoadFromCsv
    {
        ConcurrentDictionary<string, StrategyDetails>? Master_Straddle_Dictionary { get; set; }
        ConcurrentDictionary<string, ConcurrentDictionary<string, LegDetails>>? Straddle_LegDetails_Dictionary { get; set; }

        bool LoadStaddleStratgy(string path);
    }
}
