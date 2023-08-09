using System.Threading.Tasks;

namespace AlgoTerminal.Services
{
    public interface IApplicationManagerModel
    {
        Task<bool> ApplicationStartUpRequirement();
        void ApplicationStopRequirement();
    }
}
