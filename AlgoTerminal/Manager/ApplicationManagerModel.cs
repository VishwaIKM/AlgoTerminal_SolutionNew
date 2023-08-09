using AlgoTerminal.FileManager;
using AlgoTerminal.Services;
using AlgoTerminal.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AlgoTerminal.Model.EnumDeclaration;

namespace AlgoTerminal.Manager
{
    public class ApplicationManagerModel : IApplicationManagerModel
    {
        private readonly ILogFileWriter logFileWriter;
        private readonly IStraddleManager straddleManager;
        private readonly IFeed feed;

        public ApplicationManagerModel(ILogFileWriter logFileWriter, IStraddleManager straddleManager, IFeed feed)
        {
            this.feed = feed;
            this.logFileWriter = logFileWriter;
            this.straddleManager = straddleManager;

        }

        public async Task<bool> ApplicationStartUpRequirement()
        {
            try
            {
                ContractDetails.LoadContractDetails();
                var feedStarted = feed.InitializeFeedDll();//Feed start
                await Task.Delay(1000);
                var daat = straddleManager.StraddleStartUP(); // File Load          
                var firsttimeload = await straddleManager.FirstTimeDataLoadingOnGUI();// GUI Load
                await Task.Delay(1000);
                await straddleManager.DataUpdateRequest();// Fire The Orders
                return true;
            }
            catch (Exception ex)
            {
                logFileWriter.DisplayLog(EnumLogType.Error, " Application StartUp Block failed.");
                logFileWriter.WriteLog(EnumLogType.Error, ex.ToString());
                return false;
            }

        }
        public void ApplicationStopRequirement()
        {
            try
            {
                //Save ALL Data of Dicc
                //Code Here....



                var feedStarted = feed.FeedToStop();//Feed start
                LoginViewModel.NNAPIRequest.LogOutRequest();
                Environment.Exit(0);

            }
            catch (Exception ex)
            {
                logFileWriter.WriteLog(EnumLogType.Error, ex.ToString());
            }

        }
    }
}
