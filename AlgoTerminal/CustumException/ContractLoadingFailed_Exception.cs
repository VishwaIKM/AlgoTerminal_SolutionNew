using System;

namespace AlgoTerminal.CustumException
{
    public class ContractLoadingFailed_Exception : Exception
    {
        public ContractLoadingFailed_Exception() { }

        public ContractLoadingFailed_Exception(string name)
       : base(string.Format("Contract Loading Failed: {0}", name))
        {

        }
    }
}
