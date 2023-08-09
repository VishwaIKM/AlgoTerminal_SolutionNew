using AlgoTerminal.Model;
using AlgoTerminal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgoTerminal.ViewModel
{
    public class LoggerViewModel : BaseViewModel
    {
        private static ObservableCollection<LoggerModel>? LogGrid;
        private readonly ILogFileWriter _LogFileWriter;
        public LoggerViewModel(ILogFileWriter logFileWriter)
        {
            LogDataCollection ??= new();
            _LogFileWriter = logFileWriter;
            _LogFileWriter.Start(App.logFilePath, "Log.txt");

        }


        public static ObservableCollection<LoggerModel>? LogDataCollection
        {
            get { return LogGrid; }
            set { LogGrid = value; }
        }

    }
}
