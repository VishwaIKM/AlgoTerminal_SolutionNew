using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgoTerminal.CustumException
{
    public class FileAcessLvlIssue_Exception:Exception
    {
        public FileAcessLvlIssue_Exception() { }

        public FileAcessLvlIssue_Exception(string name)
       : base(string.Format("Unable to read and write File access lvl issue: {0}", name))
        {

        }
    }
}
