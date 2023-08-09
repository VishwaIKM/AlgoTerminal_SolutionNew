using System;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.Model
{
    public class ContractRecord
    {
        public record ContractData
        {
            public DateTime Expiry;
            public string? InstrumentType;
            public uint LotSize;
            public string? TrdSymbol;
            public double Strike;
            public string? Symbol;
            public uint TokenID;
            public EnumOptiontype Opttype;
            public int FreezeQnty;
        }
    }
}
