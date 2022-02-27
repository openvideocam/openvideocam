using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;
using openvcam.WinOS;
using OpenVCam.DataAccess;
using openvcamlibnet;

namespace openvcamapp.WinOS.windows
{
    /// <summary>
    /// Interaction logic for ProgressBarWindow.xaml
    /// </summary>
    /// 
    public delegate void UpdateProgressBarDelegate(int video_id, int perc);
    public delegate void CloseFormDelegate();

    public partial class ProgressBarWindow : Window
    {
        private MainWindow m_parent;
        private int m_op_id;
        private ProcessingStatusHandler m_psh;
        private int m_last_perc;
        private System.Timers.Timer m_timer;

        private void UpdateProgressBar(int video_id, int perc)
        {
            foreach(Video v in m_parent.VideoMetaData.MetaData.Videos)
            {
                if (v.Id == video_id)
                {
                    tbVideo.Text = "Processing video " + v.Name;
                }
            }
            pbLoad.Value = perc;
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

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            OpenVCamLib.StopOperation(m_op_id);
            this.DialogResult = false;
            Close();
        }

        private void CloseForm()
        {
            if (this.Visibility == Visibility.Visible)
                this.DialogResult = true;
            Close();
        }

        private void TimerSecondElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            int status = OpenVCamLib.GetOperationStatus(m_op_id);

            if (status == 2)
            {
                m_timer.Stop();
                Dispatcher.BeginInvoke(new CloseFormDelegate(CloseForm), new object[] { });
            }
        }

        public ProgressBarWindow(MainWindow owner)
        {
            InitializeComponent();
            this.Owner = owner;
            m_parent = owner;
            m_psh = new ProcessingStatusHandler(ProcessingStatusChanged);
            m_op_id = -1;
            m_last_perc = -1;
            m_timer = new System.Timers.Timer(1000);
            m_timer.Elapsed += TimerSecondElapsed;
            m_timer.Start();
        }

        public void SetOperationId(int id)
        {
            m_op_id = id;
        }

        public ProcessingStatusHandler StatusHandler {  get { return m_psh;  } }
    }
}
