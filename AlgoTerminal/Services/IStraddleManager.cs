using AlgoTerminal.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgoTerminal.Services
{
    public interface IStraddleManager
    {
        Task DataUpdateRequest();
        Task<bool> FirstTimeDataLoadingOnGUI();
        Task<bool> SquareOffStraddle920(PortfolioModel PM, EnumDeclaration.EnumStrategyMessage enumStrategyMessage = EnumDeclaration.EnumStrategyMessage.NONE);
        bool StraddleStartUP();
    }
}
