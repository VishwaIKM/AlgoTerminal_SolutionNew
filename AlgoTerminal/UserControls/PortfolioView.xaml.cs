using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VishwaDockLibNew.Interface;

namespace AlgoTerminal.UserControls
{
    /// <summary>
    /// Interaction logic for PortfolioView.xaml
    /// </summary>
    public partial class PortfolioView : UserControl, IDockSource
    {
        public PortfolioView(string header)
        {
            InitializeComponent();
            _header = header;
        }
        private IDockControl _dockControl;
        public IDockControl DockControl
        {
            get
            {
                return _dockControl;
            }

            set
            {
                _dockControl = value;
            }
        }

        private string _header;
        public string Header
        {
            get
            {
                return _header;
            }
        }
        public ImageSource Icon
        {
            get
            {
                return null;
            }
        }
        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            DataGridRow row = FindVisualParent<DataGridRow>(sender as Expander);
            row.DetailsVisibility = System.Windows.Visibility.Visible;
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            DataGridRow row = FindVisualParent<DataGridRow>(sender as Expander);
            row.DetailsVisibility = System.Windows.Visibility.Collapsed;
        }

        public T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindVisualParent<T>(parentObject);
        }
    }
}
