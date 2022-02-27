using OpenVCam.DataAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

namespace openvcam.WinOS.pages
{
    /// <summary>
    /// Interaction logic for SummaryPage.xaml
    /// </summary>
    public partial class SummaryPage : Page
    {
        private MainWindow m_parent;
                
        public SummaryPage(MainWindow parent)
        {
            m_parent = parent;

            m_parent.pnlNavBar.Visibility = Visibility.Visible;

            InitializeComponent();

            this.DataContext = m_parent.VideoMetaData.MetaData.Summary;
        }
    }
}
