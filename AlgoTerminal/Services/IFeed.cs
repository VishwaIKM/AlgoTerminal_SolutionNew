using FeedC;
using FeedCM;

namespace AlgoTerminal.Services
{
    public interface IFeed
    {
        Feed_Ikm? FeedC { get; set; }
        FeedCMIdxC? FeedCM { get; set; }

        bool FeedToStop();
        bool InitializeFeedDll();
    }
}
