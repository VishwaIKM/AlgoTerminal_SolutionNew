using AlgoTerminal.NNAPI;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace AlgoTerminal.FileManager
{
    public class OtherMethods
    {
        static readonly object _roundPriceLock = new();
        /// <summary>
        /// round the price for Buy and Sell according to ticksize
        /// </summary>
        /// <param name="priceInPaise"></param>
        /// <param name="transType"></param>
        /// <param name="ticksize"></param>
        /// <returns></returns>
        public static int RoundThePrice(double priceInPaise, TransType transType, int ticksize = 5)
        {
            lock (_roundPriceLock)
            {
                int _price = (int)priceInPaise;
                if (transType == TransType.B)
                {
                    if (_price % ticksize != 0)
                        _price += (ticksize - (_price % ticksize));
                }
                else
                {
                    _price -= (_price % ticksize);
                }

                return _price;
            }
        }
        public static T DeepCopy<T>(T other)
        {
            using MemoryStream ms = new();
            BinaryFormatter formatter = new()
            {
                Context = new StreamingContext(StreamingContextStates.Clone)
            };
#pragma warning disable SYSLIB0011
            formatter.Serialize(ms, other);
            ms.Position = 0;
            return (T)formatter.Deserialize(ms);
#pragma warning restore SYSLIB0011
        }

        public static string GetNewName(string OldName)
        {
            string NewName = "NotDefine";

            if (OldName.Contains('.'))
            {
                var data = OldName.Split('.');
                var LastName = double.TryParse(data[1], out double value) ? value : 0;
                if (LastName != 0)
                {
                    var Name = data[0][..4];
                    LastName /= 100.00;
                    LastName += 0.01;
                    NewName = Name + LastName;
                }
                else
                {
                    //BUG
                    NewName = OldName + "0.01";
                }
            }
            else
            {
                NewName = OldName + "0.01";
            }
            return NewName;
        }
    }
}
