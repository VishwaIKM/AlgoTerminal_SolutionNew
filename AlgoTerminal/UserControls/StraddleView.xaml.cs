using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VishwaDockLibNew.Interface;

namespace AlgoTerminal.UserControls
{
    /// <summary>
    /// Interaction logic for StraddleView.xaml
    /// </summary>
    public partial class StraddleView : UserControl, IDockSource
    {
        public StraddleView(string header)
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
