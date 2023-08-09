using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.Model
{
    /// <summary>
    /// Blocking collection data structure
    /// </summary>
    public class Param
    {
        public EnumLogType Ltype { get; set; }  // Type of log
        public string Msg { get; set; }     // Message
        public bool ShowInLogWindow { get; set; } //Whether to display in Log Window or not

        public Param()
        {
            Ltype = EnumLogType.Info;
            Msg = "";
            ShowInLogWindow = false;
        }

        public Param(EnumLogType logType, string logMsg, bool showInGui = true)
        {
            Ltype = logType;
            Msg = logMsg;
            ShowInLogWindow = showInGui;
        }
    }
}
