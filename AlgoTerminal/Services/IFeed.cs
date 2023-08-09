using FeedC;
using FeedCM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
