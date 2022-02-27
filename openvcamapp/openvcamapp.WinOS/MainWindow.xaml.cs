using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using Microsoft.Win32;
using openvcam.WinOS.pages;
using OpenVCam.Common.Support;
using OpenVCam.DataAccess;
using openvcamapp.winos;
using openvcamapp.winos.pages;
using openvcamapp.winos.windows;

namespace openvcam.WinOS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowBase
    {
        private string m_cmd_line_filename;

        private VideoMetaDataModel m_vmd;

        private void SaveRecentFile(string FileName)
        {
            try
            {
                if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\OpenVideoCamera\Settings"))
                {
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\OpenVideoCamera\Settings");
                }

                string ini_file = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\OpenVideoCamera\Settings" + @"\OpenVideoCamera.ini";
                if (!File.Exists(ini_file))
                {
                    File.Copy(System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + @"\OpenVideoCamera.ini", ini_file);
                }

                IniFile ini;
                ini = new IniFile(ini_file);

                List<string> recent_files = new List<string>();
                recent_files.Add(ini.ReadString("RecentFiles", "VMD_FILE1", ""));
                recent_files.Add(ini.ReadString("RecentFiles", "VMD_FILE2", ""));
                recent_files.Add(ini.ReadString("RecentFiles", "VMD_FILE3", ""));
                recent_files.Add(ini.ReadString("RecentFiles", "VMD_FILE4", ""));
                recent_files.Add(ini.ReadString("RecentFiles", "VMD_FILE5", ""));

                int index_found = recent_files.FindIndex(item => item == FileName);
                if (index_found != -1)
                {
                    recent_files.RemoveAt(index_found);                    
                }

                if (!recent_files.Contains(FileName))
                {
                    ini.WriteString("RecentFiles", "VMD_FILE1", FileName);
                    ini.WriteString("RecentFiles", "VMD_FILE2", recent_files[0]);
                    ini.WriteString("RecentFiles", "VMD_FILE3", recent_files[1]);
                    ini.WriteString("RecentFiles", "VMD_FILE4", recent_files[2]);
                    ini.WriteString("RecentFiles", "VMD_FILE5", recent_files[3]);

                    ini.Save();
                }
            }
            catch (Exception)
            {

            }
        }

        private void contentGrid_ContentRendered(object sender, EventArgs e)
        {
            contentGrid.NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden;
        }

        public bool VideoMetaDataActive {  get { return (m_vmd != null) ? true : false;  } }

        public bool VideoMetaDataIsNew { get { return (m_vmd.HasFileName) ? false : true; } }

        public VideoMetaDataModel VideoMetaData {  get { return m_vmd;  } }        

        public MainWindow(string FileName)
        {
            InitializeComponent();

            m_cmd_line_filename = FileName;

            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        }

        public bool CreateNewVmdfile()
        {
            SaveFormPosition();
            SetFormPosition();

            m_vmd = new VideoMetaDataModel();

            TitleFileName.Text = " - Untitled.vmd";

            if (ShowTutorial())
            {
                TutorialWindow wnd = new TutorialWindow(this);
                wnd.Show();
            }
            
            return true;
        }

        public async Task<bool> OpenVmdFile(string FileName)
        {
            SaveFormPosition();
            SetFormPosition();

            contentGrid.Content = new LoadingPage(this);
            pnlNavBar.Visibility = Visibility.Hidden;

            await Task.Run(() =>
            {
                m_vmd = new VideoMetaDataModel(FileName);
            });

            TitleFileName.Text = " - " + System.IO.Path.GetFileName(m_vmd.FileName);

            SaveRecentFile(FileName);
            return true;
        }

        public bool SaveVmdFile()
        {
            m_vmd.Save();
            return true;
        }

        public bool SaveVmdFile(string FileName)
        {
            m_vmd.Save(FileName);
            SaveRecentFile(FileName);
            return true;
        }

        public bool CloseVmdFile()
        {
            if (m_vmd != null)
            {
                m_vmd.Close();
                m_vmd = null;
            }
            return true;
        }        

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (m_vmd.HasFileName)
                SaveVmdFile();
            else
                btnSaveAs_Click(sender, e);
        }

        private void btnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "Video Meta Data Files (*.vmd)|*.vmd";
            bool? result = saveFile.ShowDialog();

            if (result.HasValue && result.Value)
            {
                SaveVmdFile(saveFile.FileName);
                TitleFileName.Text = " - " + System.IO.Path.GetFileName(m_vmd.FileName);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            SaveFormPosition();

            if (CloseVmdFile())
            {
                Window_Loaded(sender, e);
            }
        }

        private void btnInputVideo_Click(object sender, RoutedEventArgs e)
        {
            contentGrid.Content = new InputVideoPage(this);
        }

        private void btnOutputVideo_Click(object sender, RoutedEventArgs e)
        {
            contentGrid.Content = new OutputVideoPage(this);
        }

        private void btnSummary_Click(object sender, RoutedEventArgs e)
        {
            contentGrid.Content = new SummaryPage(this);
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnCloseApp_Click(object sender, RoutedEventArgs e)
        {
            SaveFormPosition();

            if ((m_vmd != null) && (m_vmd.MetaData.Changed))
            {
                MessageBoxResult msg_result = MessageBox.Show("File has changed. Do you want to save it?", "OpenVCam", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);
                if (msg_result == MessageBoxResult.Yes)
                {
                    if (m_vmd.HasFileName)
                    {
                        if (!SaveVmdFile())
                        {
                            return;
                        }
                    }
                    else
                    {
                        SaveFileDialog saveFile = new SaveFileDialog();
                        saveFile.Filter = "Video Meta Data Files (*.vmd)|*.vmd";
                        bool? result = saveFile.ShowDialog();

                        if (result.HasValue && result.Value)
                        {
                            if (!SaveVmdFile(saveFile.FileName))
                                return;
                            TitleFileName.Text = " - " + System.IO.Path.GetFileName(m_vmd.FileName);
                        }
                        else
                        {
                            return;
                        }
                    }
                    
                } else if (msg_result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            CloseVmdFile();

            Application.Current.Shutdown();
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }                
            else
            {
                WindowState = WindowState.Maximized;
            }                
        }

        private void panelHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetFormPosition();

            if (m_cmd_line_filename != null)
            {
                if (System.IO.File.Exists(m_cmd_line_filename))
                {
                    try
                    {
                        if (await OpenVmdFile(m_cmd_line_filename))
                        {
                            contentGrid.Content = new SummaryPage(this);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("Error opening {0}. Details: {1}", m_cmd_line_filename, ex.Message));
                    }
                }
                else
                {
                    MessageBox.Show(string.Format("Could not find file {0}", m_cmd_line_filename));
                }
            }
            //contentGrid.Content = new LoginPage(this);
            contentGrid.Content = new WelcomePage(this);
        }
    }
}
