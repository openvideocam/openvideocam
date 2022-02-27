using Microsoft.Win32;
using openvcam.WinOS;
using openvcam.WinOS.pages;
using OpenVCam.Common.Support;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace openvcamapp.winos.pages
{
    /// <summary>
    /// Interaction logic for WelcomePage.xaml
    /// </summary>
    public partial class WelcomePage : Page
    {
        private MainWindow m_parent;

        private void LoadRecentFiles()
        {
            try
            {
                string arquivo_ini = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\OpenVideoCamera\Settings" + @"\OpenVideoCamera.ini";
                if (!File.Exists(arquivo_ini))
                {
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\OpenVideoCamera\Settings");
                    File.Copy(System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + @"\OpenVideoCamera.ini", arquivo_ini);
                }

                IniFile ini;
                ini = new IniFile(arquivo_ini);
                lblRecentFile1.Text = ini.ReadString("RecentFiles", "VMD_FILE1", "");
                lblRecentFile2.Text = ini.ReadString("RecentFiles", "VMD_FILE2", "");
                lblRecentFile3.Text = ini.ReadString("RecentFiles", "VMD_FILE3", "");
                lblRecentFile4.Text = ini.ReadString("RecentFiles", "VMD_FILE4", "");
                lblRecentFile5.Text = ini.ReadString("RecentFiles", "VMD_FILE5", "");
            }
            catch (Exception)
            {

            }
        }

        private void btnCreateProject_Click(object sender, RoutedEventArgs e)
        {
            m_parent.CreateNewVmdfile();
            
            m_parent.contentGrid.Content = new SummaryPage(m_parent);
        }

        private async void btnOpenProject_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Video Meta Data Files (*.vmd)|*.vmd";
            bool? result = openFile.ShowDialog();

            if (result.HasValue && result.Value)
            {
                await m_parent.OpenVmdFile(openFile.FileName);
                
                m_parent.contentGrid.Content = new SummaryPage(m_parent);
            }
        }

        private async void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink hyperlink = e.Source as Hyperlink;
            TextBlock myTextBlock = (TextBlock)this.FindName(hyperlink.Name.Replace("lnk", "lbl"));

            await m_parent.OpenVmdFile(myTextBlock.Text);
            
            m_parent.contentGrid.Content = new SummaryPage(m_parent);
        }

        public WelcomePage(MainWindow parent)
        {
            m_parent = parent;

            m_parent.pnlNavBar.Visibility = Visibility.Hidden;

            InitializeComponent();

            LoadRecentFiles();
        }
    }
}
