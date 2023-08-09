using System;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.Model
{
    public sealed class LoggerModel
    {
        public DateTime Time { get; set; }
        public EnumLogType Category { get; set; }  // Type of log
        public string Message { get; set; }
    }
}
