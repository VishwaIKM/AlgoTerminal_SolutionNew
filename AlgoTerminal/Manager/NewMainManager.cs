using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgoTerminal.Manager
{
    public class NewMainManager
    {
        public static ConcurrentQueue<string>? OrderCancelQueue { get; private set; }
        public static ConcurrentQueue<string>? OrderFireQueue { get; private set; }
        public NewMainManager() { 
        
            OrderCancelQueue = new ConcurrentQueue<string>();
            OrderFireQueue = new ConcurrentQueue<string>();
        }
    }
}
