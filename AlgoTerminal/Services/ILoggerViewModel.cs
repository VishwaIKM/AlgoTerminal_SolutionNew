using AlgoTerminal.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AlgoTerminal.Services
{
    public interface ILoggerViewModel
    {
        DataGrid LogDataCollection { get; set; }

        void DisplayLog(EnumDeclaration.EnumLogType Type, string Log);
        bool Start(string LogFileDirectory, string FileName);
        void Stop();
        void WriteLog(EnumDeclaration.EnumLogType type, string log);
    }
}
