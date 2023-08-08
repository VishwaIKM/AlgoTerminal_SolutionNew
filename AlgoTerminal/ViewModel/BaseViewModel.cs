using System.ComponentModel;

namespace AlgoTerminal.ViewModel
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        #region Methods
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region INotifyPropertyChanged Members 
        public event PropertyChangedEventHandler? PropertyChanged;
        public bool CanThisMethodExecute() { return true; }
        #endregion
    }
}
