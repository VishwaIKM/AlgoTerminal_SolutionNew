using AlgoTerminal.Request;
using AlgoTerminal.Response;
using AlgoTerminal.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AlgoTerminal.UnitTest_Resource
{
    /// <summary>
    /// NOTE THIS CLASS FOR ONLY TESTING DOES NOT HAVE ANY LINK WITH APPLICATION BUSINESS LOGIC
    /// </summary>
    public class FeedLoaderToXml : IFeedLoaderToXml
    {
        private readonly IFeed feed;
        private readonly string fileName = @"D:\Development Vishwa\AlgoTerminal_Solution\UnitTest_Resources\feedC_dic.xml";
        private readonly string fileName2 = @"D:\Development Vishwa\AlgoTerminal_Solution\UnitTest_Resources\feedCM_dic.xml";
        private readonly FeedCB_C _C;
        private readonly FeedCB_CM _CM;

        public FeedLoaderToXml(IFeed feed, FeedCB_C c, FeedCB_CM cM)
        {
            this.feed = feed;
            _C = c;
            _CM = cM;
        }
        /// <summary>
        /// save the feed to Dic 
        /// </summary>
        public void SaveDicData()
        {
            ConcurrentDictionary<ulong, FeedC.ONLY_MBP_DATA_7208> dictionary = feed.FeedC.dcFeedData;
            var serializer = new XmlSerializer(typeof(List<KeyValue<ulong, FeedC.ONLY_MBP_DATA_7208>>));
            using (var s = new StreamWriter(fileName))
            {
                serializer.Serialize(s, dictionary.Select(x => new KeyValue<ulong, FeedC.ONLY_MBP_DATA_7208>() { Key = x.Key, Value = x.Value }).ToList());
            }

            ConcurrentDictionary<string, FeedCM.MULTIPLE_INDEX_BCAST_REC_7207> dictionary2 = feed.FeedCM.dcFeedDataIdx;
            var serializer2 = new XmlSerializer(typeof(List<KeyValue2<string, FeedCM.MULTIPLE_INDEX_BCAST_REC_7207>>));
            using (var s = new StreamWriter(fileName2))
            {
                serializer2.Serialize(s, dictionary2.Select(x => new KeyValue2<string, FeedCM.MULTIPLE_INDEX_BCAST_REC_7207>() { Key = x.Key, Value = x.Value }).ToList());
            }
        }
        // load back to Dic
        public void LoadFromXml()
        {
            feed.FeedC = new FeedC.Feed_Ikm(_C);
            feed.FeedCM = new FeedCM.FeedCMIdxC(_CM);

            LoadFromXml<ulong, FeedC.ONLY_MBP_DATA_7208>();
            LoadFromXml2<string, FeedCM.MULTIPLE_INDEX_BCAST_REC_7207>();
        }
        public void LoadFromXml<TKey, TValue>()
        {

            var serializer = new XmlSerializer(typeof(List<KeyValue<TKey, TValue>>));
            using var s = new StreamReader(fileName);
            var list = serializer.Deserialize(s) as List<KeyValue<TKey, TValue>>;
            var data = list.ToDictionary(x => x.Key, x => x.Value);
            feed.FeedC.dcFeedData ??= new();
            foreach (var kv in data)
            {
                if (!feed.FeedC.dcFeedData.ContainsKey(kv.Key))
                {
                    feed.FeedC.dcFeedData[kv.Key] = kv.Value;
                }
            }
        }
        public void LoadFromXml2<TKey, TValue>()
        {

            var serializer = new XmlSerializer(typeof(List<KeyValue2<TKey, TValue>>));
            using var s = new StreamReader(fileName2);
            var list = serializer.Deserialize(s) as List<KeyValue2<TKey, TValue>>;
            var data = list.ToDictionary(x => x.Key, x => x.Value);
            feed.FeedCM.dcFeedDataIdx ??= new();
            foreach (var kv in data)
            {
                if (!feed.FeedCM.dcFeedDataIdx.ContainsKey(kv.Key))
                {
                    feed.FeedCM.dcFeedDataIdx[kv.Key] = kv.Value;
                }
            }
        }
    }
    public class KeyValue<TKey, TValue>
    {
        public ulong Key { get; set; }
        public FeedC.ONLY_MBP_DATA_7208 Value { get; set; }
    }
    public class KeyValue2<TKey, TValue>
    {
        public string Key { get; set; }
        public FeedCM.MULTIPLE_INDEX_BCAST_REC_7207 Value { get; set; }
    }
}
