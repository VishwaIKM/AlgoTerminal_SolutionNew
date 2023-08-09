using AlgoTerminal.Command;
using AlgoTerminal.Model;
using AlgoTerminal.Request;
using AlgoTerminal.Response;
using AlgoTerminal.Services;
using AlgoTerminal.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;

namespace AlgoTerminal.ViewModel
{
    public sealed class LoginViewModel : BaseViewModel
    {
        #region Members and Inst.
        LoginModel _login = new();
        private bool _isloginvisibile = true;
        private string _loginStatusGUILbl;
        private readonly DashboardView dashboardView1;
        private readonly IApplicationManagerModel applicationManagerModel;

        //As i have Used the OLD code ine new Porject the dependency inversion principal fail (I have to re-write the old code in order to fix the Code Design Issue.)
        public static NNAPIRequest NNAPIRequest;
        public static LoginViewModel login;


        #endregion

        #region Properties
        public LoginModel UserDetails
        {
            get => _login;
            set { _login = value; }
        }
        public int? UserID
        {
            get => UserDetails.UserID;
            set
            {
                if (UserDetails.UserID != value)
                {
                    UserDetails.UserID = value;
                    RaisePropertyChanged(nameof(UserID));
                }
            }
        }
        public int? Password
        {
            get => UserDetails.Password;
            set
            {
                if (UserDetails.Password != value)
                {
                    UserDetails.Password = value;
                    RaisePropertyChanged(nameof(Password));
                }
            }
        }
        public bool IsLoginButtonEnable
        {
            get => _isloginvisibile;
            set
            {
                if (_isloginvisibile != value)
                {
                    _isloginvisibile = value;
                    RaisePropertyChanged(nameof(IsLoginButtonEnable));
                }
            }
        }
        public string LoginStatusGUILbl
        {
            get => _loginStatusGUILbl;
            set
            {
                if (_loginStatusGUILbl != value)
                {
                    _loginStatusGUILbl = value;
                    RaisePropertyChanged(nameof(LoginStatusGUILbl));
                }
            }
        }
        #endregion

        #region Methods
        public LoginViewModel(DashboardView dashboardView1, IApplicationManagerModel applicationManagerModel, NNAPIRequest nNAPIRequest)
        {
            login = this;
            NNAPIRequest = nNAPIRequest;
            this.dashboardView1 = dashboardView1;
            this.applicationManagerModel = applicationManagerModel;

            bool a = Convert.ToBoolean(NNAPIRequest.InitializeServer());
            if (!a)
                MessageBox.Show("Not able to connect the server.");

        }

        #endregion

        #region ICommand BUTTON Method 
        public void ErrorResponse(string message)
        {
            LoginStatusGUILbl = message;
        }
        public void LoginResponse(bool loginSuccess, string messageText)
        {

            Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
            {
                if (loginSuccess)
                {
                    FeedCB_C._dashboard._connected = true;
                    dashboardView1.Show();
                    App.Current.MainWindow.Close();
                    App.Current.MainWindow = dashboardView1;

                    await applicationManagerModel.ApplicationStartUpRequirement();

                    //BuySellView buySellView = App.AppHost!.Services.GetRequiredService<BuySellView>();
                    //buySellView.Show();
                }
                else
                {
                    LoginStatusGUILbl = messageText;
                }

            }),
            DispatcherPriority.Background,
            null);

        }
        async void LoginCommandMethodExcute()
        {
            IsLoginButtonEnable = false;
            try
            {

                NNAPIRequest.LoginRequest((int)UserID, (int)Password);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Not Able to Connect SQL Server. " + ex.ToString());
                IsLoginButtonEnable = true;
            }


            IsLoginButtonEnable = true;
        }
        void ExitCommandMethodExcute()
        {
            Application.Current.Shutdown();
        }
        bool CanThisMethodExecute() { return true; }

        #endregion

        #region ICommand
        public ICommand LoginCommand => new RelayCommand2(LoginCommandMethodExcute, CanThisMethodExecute);
        public ICommand ExitAppCommand => new RelayCommand2(ExitCommandMethodExcute, CanThisMethodExecute);
        #endregion
    }
}
