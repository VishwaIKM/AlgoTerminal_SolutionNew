using System.Windows.Controls;
using System.Windows.Media;
using VishwaDockLibNew.Interface;

namespace AlgoTerminal.UserControls
{
    /// <summary>
    /// Interaction logic for NetPositionView.xaml
    /// </summary>
    public partial class NetPositionView : UserControl, IDockSource
    {
        public NetPositionView(string header)
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
    }
}
