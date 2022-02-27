using openvcam.WinOS;
using OpenVCam.Common.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

namespace openvcamapp.winos.windows
{
    /// <summary>
    /// Interaction logic for TutorialWindow.xaml
    /// </summary>
    public partial class TutorialWindow : Window
    {
        private void SaveShowTutorial()
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

                ini.WriteBool("Tutorial", "SHOW_WINDOW", (!ckbDontShowAgain.IsChecked.Value));

                ini.Save();                
            }
            catch (Exception)
            {

            }
        }

        public TutorialWindow(MainWindow owner)
        {
            this.Owner = owner;            

            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            SaveShowTutorial();
            this.Close();
        }
    }
}
