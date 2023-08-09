using AlgoTerminal.Model;
using AlgoTerminal.Services;
using AlgoTerminal.ViewModel;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.FileManager
{
    public class LogFileWriter : ILogFileWriter
    {
        #region Members
        BlockingCollection<Param>? Blocking_collection { get; set; }
        private StreamWriter? _back_log_writer;
        #endregion
        #region Methods for Log Files

        /// <summary>
        /// Begin the TASK
        /// </summary>
        /// <param name="LogFileDirectory"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public bool Start(string LogFileDirectory, string FileName)
        {
            Blocking_collection ??= new();

            _back_log_writer = new StreamWriter(LogFileDirectory + "\\" + FileName, true)
            {
                AutoFlush = true
            };

            Task.Factory.StartNew(() =>
            {
                foreach (Param p in Blocking_collection.GetConsumingEnumerable())
                {
                    switch (p.Ltype)
                    {
                        case EnumLogType.Info:
                            const string LINE_MSG = "[{0}] {1}";
                            _back_log_writer.WriteLine(string.Format(LINE_MSG, LogTimeStamp(), p.Msg));
                            break;
                        case EnumLogType.Warning:
                            const string WARNING_MSG = "[{1}][Warning] {0}";
                            _back_log_writer.WriteLine(string.Format(WARNING_MSG, p.Msg, LogTimeStamp()));
                            System.Diagnostics.Debug.WriteLine("[Warning]" + p.Msg);
                            break;
                        case EnumLogType.Error:
                            const string ERROR_MSG = "[{1}][Error] {0}";
                            _back_log_writer.WriteLine(string.Format(ERROR_MSG, p.Msg, LogTimeStamp()));
                            System.Diagnostics.Debug.WriteLine("[Error]" + p.Msg);
                            break;
                        default:
                            _back_log_writer.WriteLine(string.Format(LINE_MSG, LogTimeStamp(), p.Msg));
                            System.Diagnostics.Debug.WriteLine("[{0}] {1}", p.Ltype.ToString(), p.Msg);
                            break;
                    }

                    if (p.ShowInLogWindow)
                    {
                        LoggerModel loggerModel = new LoggerModel();
                        loggerModel.Category = p.Ltype;
                        loggerModel.Time = DateTime.Now;
                        loggerModel.Message = p.Msg;
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => { LoggerViewModel.LogDataCollection.Add(loggerModel); }), DispatcherPriority.Background, null);
                    }
                }

                _back_log_writer.Flush();
                _back_log_writer.Close();
            });

            return true;
        }

        /// <summary>
        /// Log add to Blocking collection
        /// </summary>
        /// <param name="type"></param>
        /// <param name="log"></param>
        public void WriteLog(EnumLogType type, string log)
        {
            if (!Blocking_collection.IsAddingCompleted)
            {
                Param p = new Param(type, log, false);
                Blocking_collection.Add(p);
            }
        }

        /// <summary>
        /// log add to Blocking collection also flag will be true so it will show in GUI
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Log"></param>
        public void DisplayLog(EnumLogType Type, string Log)
        {
            if (!Blocking_collection.IsAddingCompleted)
            {
                Param p = new Param(Type, Log, true);
                Blocking_collection.Add(p);
            }
        }

        /// <summary>
        /// Log Time Stamp according to System Time on The Machine
        /// </summary>
        /// <returns></returns>
        string LogTimeStamp()
        {
            DateTime now = DateTime.Now;
            return now.ToString("hh:mm:ss");
        }

        /// <summary>
        /// Dispose the instance 
        /// </summary>
        public void Stop()
        {
            Blocking_collection.CompleteAdding();
        }

        #endregion
    }
}
