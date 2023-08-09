using AlgoTerminal.Model;
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
