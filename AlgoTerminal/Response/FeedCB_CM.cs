using AlgoTerminal.Services;
using FeedCM;
using System;

namespace AlgoTerminal.Response
{
    public class FeedCB_CM : IFeedRespCMIdx
    {

        private readonly IDashboardModel _dashboard;
        const string k_nifty_index_format = "Nifty-S {0} ({1})";
        const string k_bank_index_format = "Bank-S {0} ({1})";
        const string k_fin_nifty_format = "FinNifty-S {0} ({1})";
        const string k_priceformat = "0.00";

        public FeedCB_CM(IDashboardModel dashboardModel)
        {
            _dashboard = dashboardModel;
        }

        public void Feed_CMIdx_CallBack(uint FeedLogTime, string IndexName)
        {
            //Not in Use
        }

        public void Feed_CMIdx_CallBack_Data(MULTIPLE_INDEX_BCAST_REC_7207 mULTIPLE_INDEX_BCAST_REC_7207, string text)
        {
            int diffPrice = Convert.ToInt32(mULTIPLE_INDEX_BCAST_REC_7207.IndexValue - mULTIPLE_INDEX_BCAST_REC_7207.ClosingIndex);
            if (text == "Nifty 50")
            {
                _dashboard.Nifty50 = string.Format(k_nifty_index_format, mULTIPLE_INDEX_BCAST_REC_7207.IndexValue.ToString(k_priceformat), (mULTIPLE_INDEX_BCAST_REC_7207.IndexValue - mULTIPLE_INDEX_BCAST_REC_7207.ClosingIndex).ToString(k_priceformat));

            }
            else if (text == "Nifty Bank")
            {
                _dashboard.BankNifty = string.Format(k_bank_index_format, mULTIPLE_INDEX_BCAST_REC_7207.IndexValue.ToString(k_priceformat), (mULTIPLE_INDEX_BCAST_REC_7207.IndexValue - mULTIPLE_INDEX_BCAST_REC_7207.ClosingIndex).ToString(k_priceformat));
            }
            else if (text == "Nifty Fin Service")
            {
                _dashboard.FinNifty = string.Format(k_fin_nifty_format, mULTIPLE_INDEX_BCAST_REC_7207.IndexValue.ToString(k_priceformat), (mULTIPLE_INDEX_BCAST_REC_7207.IndexValue - mULTIPLE_INDEX_BCAST_REC_7207.ClosingIndex).ToString(k_priceformat));
            }
        }

        public void Feed_CM_CallBack(uint FeedLogTime, ONLY_MBP_DATA_7208 stFeed)
        {
            //NOT IN USE
        }

        public void Feed_CM_TNDTC_18705_CallBack(uint FeedLogTime, INTERACTIVE_ONLY_MBP_DATA_TNDTC_18705 stFeed)
        {
            //
        }

        public void Messages(string msg)
        {
            //FOR LOGGER
        }
    }
}
