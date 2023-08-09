using AlgoTerminal.Model;
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
