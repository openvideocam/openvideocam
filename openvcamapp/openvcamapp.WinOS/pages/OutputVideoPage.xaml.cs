using Microsoft.Win32;
using openvcamapp.WinOS.windows;
using openvcamlibnet;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace openvcam.WinOS.pages
{
    /// <summary>
    /// Interaction logic for OutputVideoPage.xaml
    /// </summary>
    public partial class OutputVideoPage : Page
    {
        private MainWindow m_parent;
        private int m_op_id;
        private ProcessingStatusHandler m_psh;
        private int m_last_perc;
        private System.Timers.Timer m_timer;
        public delegate void timerTick();

        public delegate void UpdateProgressBarDelegate(int video_id, int perc);
        public delegate void CloseFormDelegate();

        public OutputVideoPage(MainWindow parent)
        {
            m_parent = parent;

            m_parent.pnlNavBar.Visibility = Visibility.Visible;

            InitializeComponent();

            this.DataContext = m_parent.VideoMetaData.MetaData.Output;

            if (m_parent.VideoMetaData.MetaData.Output.LogoImageAsImage != null)
            {
                BitmapImage bitmap = new BitmapImage();

                using (MemoryStream stream = new MemoryStream())
                {
                    m_parent.VideoMetaData.MetaData.Output.LogoImageAsImage.Save(stream, ImageFormat.Png);
                    stream.Seek(0, SeekOrigin.Begin);
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                }

                imgLogo.Source = bitmap;
            }

            if (m_parent.VideoMetaData.MetaData.Output.TargetFrameWidth == 1920 && m_parent.VideoMetaData.MetaData.Output.TargetFrameHeight == 1080)
                cbxResolution.SelectedIndex = 0;
            else if (m_parent.VideoMetaData.MetaData.Output.TargetFrameWidth == 1280 && m_parent.VideoMetaData.MetaData.Output.TargetFrameHeight == 720)
                cbxResolution.SelectedIndex = 1;
            else if (m_parent.VideoMetaData.MetaData.Output.TargetFrameWidth == 852 && m_parent.VideoMetaData.MetaData.Output.TargetFrameHeight == 480)
                cbxResolution.SelectedIndex = 2;
            else
                cbxResolution.SelectedIndex = -1;            
        }

        private void btnCreateVideo_Click(object sender, RoutedEventArgs e)
        {
            //Create progressbar dialog box
            //ProgressBarWindow pbw = new ProgressBarWindow(m_parent);
            pnlProgressOverlay.Visibility = Visibility.Visible;            

            m_psh = new ProcessingStatusHandler(ProcessingStatusChanged);
            m_op_id = -1;
            m_last_perc = -1;
            m_timer = new System.Timers.Timer(1000);
            m_timer.Elapsed += TimerSecondElapsed;
            m_timer.Start();

            //Transfer data from memory to temp VMD file as C++ code needs to access updated data
            m_parent.VideoMetaData.FlushMetaDataToDisk();

            //Start output video creation
            int op_id = OpenVCamLib.StartCreateOutputVideo(
                m_parent.VideoMetaData.TempFileName,
                StatusHandler);

            //Set operation id in progressbar dialog box, so it can be used later to cancel the operation from the "Close" button
            SetOperationId(op_id);

            ButtonState();
        }        

        private void btnFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = ".MP4"; 
            saveFileDialog.Filter = "Video file (.MP4)|*.MP4"; 
            if (saveFileDialog.ShowDialog() == true)
            {
                txtFileName.Text = saveFileDialog.FileName;
                m_parent.VideoMetaData.MetaData.Output.FileName = txtFileName.Text;
            }                
        }                       
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog OpenFile = new OpenFileDialog();
            OpenFile.Multiselect = false;
            OpenFile.Title = "Select Picture";
            OpenFile.Filter = "Supported Graphics| *.jpeg; *.jpg; *.png;";
            if (OpenFile.ShowDialog() == true)
            {
                string img_base64 = OpenVCamLib.GetImageAsBase64(OpenFile.FileName);

                m_parent.VideoMetaData.MetaData.Output.LogoImage = img_base64;

                imgLogo.Source = new BitmapImage(new Uri(OpenFile.FileName));                
            }
        }

        private void cbxResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object resolution = ((ComboBoxItem)cbxResolution.SelectedItem).Tag;
            if (resolution != null)
            {
                if (((string)resolution) == "1080p")
                {
                    m_parent.VideoMetaData.MetaData.Output.TargetFrameWidth = 1920;
                    m_parent.VideoMetaData.MetaData.Output.TargetFrameHeight = 1080;
                }
                else if (((string)resolution) == "720p")
                {
                    m_parent.VideoMetaData.MetaData.Output.TargetFrameWidth = 1280;
                    m_parent.VideoMetaData.MetaData.Output.TargetFrameHeight = 720;
                }
                else if (((string)resolution) == "480p")
                {
                    m_parent.VideoMetaData.MetaData.Output.TargetFrameWidth = 852;
                    m_parent.VideoMetaData.MetaData.Output.TargetFrameHeight = 480;
                }
            }
        }


        private void btnCancelProcess_Click(object sender, RoutedEventArgs e)
        {
            OpenVCamLib.StopOperation(m_op_id);
            ClosePanelProgress();
        }

        private void UpdateProgressBar(int video_id, int perc)
        {
            pbProcess.Value = perc;
        }

        private void ClosePanelProgress()
        {
            pnlProgressOverlay.Visibility = Visibility.Hidden;            

            ButtonState();
        }

        private int ProcessingStatusChanged(int video_id, int frame_idx, int frame_count)
        {
            int perc = Convert.ToInt32((100.0 * ((1.0 * frame_idx) / frame_count)));

            if (m_last_perc != perc)
            {
                m_last_perc = perc;
                Dispatcher.BeginInvoke(new UpdateProgressBarDelegate(UpdateProgressBar), new object[] { video_id, perc });
            }

            return 0;
        }

        private void TimerSecondElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            int status = OpenVCamLib.GetOperationStatus(m_op_id);

            if (status == 2)
            {
                m_timer.Stop();
                Dispatcher.BeginInvoke(new CloseFormDelegate(ClosePanelProgress), new object[] { });
            }
        }

        public void SetOperationId(int id)
        {
            m_op_id = id;
        }

        public ProcessingStatusHandler StatusHandler { get { return m_psh; } }

        private void ButtonState()
        {            
            m_parent.pnlNavBar.IsEnabled = (pnlProgressOverlay.Visibility != Visibility.Visible);
            pnlDetails.IsEnabled = (pnlProgressOverlay.Visibility != Visibility.Visible);
            btnCreateVideo.IsEnabled = (pnlProgressOverlay.Visibility != Visibility.Visible);
        }
    }
}
