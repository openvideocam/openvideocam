using Microsoft.Win32;
using OpenVCam.DataAccess;
using openvcamapp.winos.models;
using openvcamapp.WinOS.windows;
using openvcamlibnet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
using openvcamapp.shared.support;

namespace openvcam.WinOS.pages
{
    /// <summary>
    /// Interaction logic for InputVideo.xaml
    /// </summary>
    public partial class InputVideoPage : Page
    {
        private MainWindow m_parent;
        private int m_op_id;
        private ProcessingStatusHandler m_psh;
        private int m_last_perc;
        private System.Timers.Timer m_timer;
        private Point startPoint = new Point();
        private int startIndex = -1;                

        private BackgroundWorker backgroundWorker = new BackgroundWorker();

        public delegate void UpdateProgressBarDelegate(int video_id, int perc);
        public delegate void CloseFormDelegate();

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            System.Drawing.Image snapshot = ((Video)((Border)((System.Windows.Controls.Image)sender).Parent).DataContext).Snapshot;

            if (snapshot != null)
            {
                var stream = new System.IO.MemoryStream();
                snapshot.Save(stream, ImageFormat.Png);
                stream.Position = 0;
                var snapshot_bitmap = new System.Windows.Media.Imaging.BitmapImage();
                snapshot_bitmap.BeginInit();
                snapshot_bitmap.CacheOption = BitmapCacheOption.OnLoad;
                snapshot_bitmap.StreamSource = stream;
                snapshot_bitmap.EndInit();
                ((System.Windows.Controls.Image)sender).Source = snapshot_bitmap;
            }
        }

        private void btnAddVideo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Multiselect = true;            
            bool? result = openFile.ShowDialog();

            if (result.HasValue && result.Value)
            {
                foreach (string file_name in openFile.FileNames)                                  
                    ViewModel.MetaData.Videos.Add(new Video(file_name));                
            }
        }

        private void btnDelVideo_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("The selected videos will be deleted only from OpenVideoCam database. Are you sure?", "Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                List<Video> selected_videos = new List<Video>();
                for (int i = 0; i < ListViewItems.SelectedItems.Count; i++)
                {
                    selected_videos.Add((Video)ListViewItems.SelectedItems[i]);
                }

                foreach (Video video in selected_videos)
                {
                    m_parent.VideoMetaData.MetaData.Videos.Remove(video);
                }
            }
        }

        private void btnProcessVideo_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("The selected videos will be processed. Are you sure?", "Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                //Build list with video ids to process
                List<int> selected_video_ids = new List<int>();
                for (int i = 0; i < ListViewItems.SelectedItems.Count; i++)
                {
                    selected_video_ids.Add(((Video)ListViewItems.SelectedItems[i]).Id);
                }

                //Create progressbar dialog box
                //ProgressBarWindow pbw = new ProgressBarWindow(m_parent);            
                pnlProgressOverlay.Visibility = Visibility.Visible;
                pnlDetails.Visibility = Visibility.Hidden;
                pnlDetailsBorder.Visibility = Visibility.Hidden;
                btnCancelProcess.Visibility = Visibility.Visible;
                
                m_psh = new ProcessingStatusHandler(ProcessingStatusChanged);
                m_op_id = -1;
                m_last_perc = -1;
                m_timer = new System.Timers.Timer(1000);
                m_timer.Elapsed += TimerSecondElapsed;
                m_timer.Start();

                //Transfer data from memory to temp VMD file as C++ code needs to access updated data
                m_parent.VideoMetaData.FlushMetaDataToDisk();

                //Start metadata creation
                int op_id = OpenVCamLib.StartCreateVideoMetaData(
                    selected_video_ids,
                    m_parent.VideoMetaData.TempFileName,
                    AppDomain.CurrentDomain.BaseDirectory + @"\saved_model",
                    StatusHandler);

                //Set operation id in progressbar dialog box, so it can be used later to cancel the operation from the "Close" button
                SetOperationId(op_id);

                ButtonState();
            }
        }

        private void mnuSetFieldArea_Click(object sender, RoutedEventArgs e)
        {
            List<Video> selected_videos = new List<Video>();
            for (int i = 0; i < ListViewItems.SelectedItems.Count; i++)
            {
                selected_videos.Add((Video)ListViewItems.SelectedItems[i]);
            }            

            SetFieldAreaWindow popup = new SetFieldAreaWindow(selected_videos, m_parent);
            popup.ShowDialog();
        }

        private void mnuSetCameraPosition_Click(object sender, RoutedEventArgs e)
        {
            Video video_selected = (Video)ListViewItems.SelectedItem;

            SetCameraPositionWindow popup = new SetCameraPositionWindow(video_selected, m_parent);
            popup.ShowDialog();
        }

        // Helper to search up the VisualTree
        private static T FindAnchestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        public InputVideoPage(MainWindow parent)
        {
            m_parent = parent;

            m_parent.pnlNavBar.Visibility = Visibility.Visible;

            InitializeComponent();

            ListViewItems.ItemsSource = (m_parent.VideoMetaData == null) ? null : m_parent.VideoMetaData.MetaData.Videos;

            lblTeam1Name.Text = (!String.IsNullOrEmpty(m_parent.VideoMetaData.MetaData.Summary.Team1) ? m_parent.VideoMetaData.MetaData.Summary.Team1 : "Not defined in the Game Summary");
            lblTeam2Name.Text = (!String.IsNullOrEmpty(m_parent.VideoMetaData.MetaData.Summary.Team2) ? m_parent.VideoMetaData.MetaData.Summary.Team2 : "Not defined in the Game Summary");
        }

        public VideoMetaDataModel ViewModel
        {
            get
            {
                return m_parent.VideoMetaData;
            }
        }

        private void ListViewItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Video item = ((FrameworkElement)e.OriginalSource).DataContext as Video;
            if (item != null)
            {
                FullScreenVideoWindow window = new FullScreenVideoWindow(item.FileName, m_parent);
                window.ShowDialog();
            }
        }

        private void mnuCropVideo_Click(object sender, RoutedEventArgs e)
        {
            Video video_selected = (Video)ListViewItems.SelectedItem;
            if (video_selected != null)
            {
                CropVideoModel crop_video_model = new CropVideoModel(video_selected.SelectionStart, video_selected.SelectionEnd);

                CropVideoWindow window = new CropVideoWindow(video_selected.FileName, crop_video_model, m_parent);
                window.ShowDialog();

                if ((crop_video_model.Start.HasValue) && (crop_video_model.End.HasValue))
                {
                    video_selected.SelectionStart = Convert.ToInt32(crop_video_model.Start.Value / 1000);
                    video_selected.SelectionEnd = Convert.ToInt32(crop_video_model.End.Value / 1000);
                }
            }
        }

        private void ListViewItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewItems.SelectedItems.Count == 1)
            {
                Video video_selected = (Video)ListViewItems.SelectedItem;

                lblFilename.Text = video_selected.FileName;
                lblSize.Text = String.Format("{0:n0} MB", video_selected.Size);

                TimeSpan time = TimeSpan.FromSeconds(video_selected.Length);                
                string length_str = time.ToString(@"hh\:mm\:ss");
                lblLength.Text = length_str;                

                cbxTeam1Position.SelectedIndex = ((video_selected.Team1FieldPosition == "R") ? 1 : 0);                

                switch (video_selected.GameHalf)
                {
                    case GameHalf.Undefined:
                        cbxGameHalf.SelectedIndex = 0;
                        break;
                    case GameHalf.FirstHalf:
                        cbxGameHalf.SelectedIndex = 1;
                        break;
                    case GameHalf.SecondHalf:
                        cbxGameHalf.SelectedIndex = 2;
                        break;
                    default:
                        cbxGameHalf.SelectedIndex = -1;
                        break;
                }                
            }
            else
            {
                lblFilename.Text = "";
                lblSize.Text = "";
                lblLength.Text = "00:00:00";
                cbxGameHalf.SelectedIndex = -1;
                cbxTeam1Position.SelectedIndex = -1;
                cbxTeam2Position.SelectedIndex = -1;
            }
        }

        private void cbxGameHalf_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewItems.SelectedItems.Count == 1)
            {
                Video video = (Video)ListViewItems.SelectedItem;                
                if (((ComboBoxItem)cbxGameHalf.SelectedItem).Tag != null)
                    video.GameHalf = (GameHalf)Convert.ToInt32(((ComboBoxItem)cbxGameHalf.SelectedItem).Tag);                
            }
        }

        private void cbxTeam1Position_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewItems.SelectedItems.Count == 1)
            {
                cbxTeam2Position.SelectedIndex = (cbxTeam1Position.SelectedIndex == 0) ? 1 : 0;

                Video video = (Video)ListViewItems.SelectedItem;
                if (((ComboBoxItem)cbxTeam1Position.SelectedItem).Tag != null)
                    video.Team1FieldPosition = (string)((ComboBoxItem)cbxTeam1Position.SelectedItem).Tag;                
            }
        }

        private void cbxTeam2Position_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewItems.SelectedItems.Count == 1)
            {                
                cbxTeam1Position.SelectedIndex = (cbxTeam2Position.SelectedIndex == 0) ? 1 : 0;

                Video video = (Video)ListViewItems.SelectedItem;
                if (((ComboBoxItem)cbxTeam1Position.SelectedItem).Tag != null)
                    video.Team1FieldPosition = (string)((ComboBoxItem)cbxTeam1Position.SelectedItem).Tag;
            }
        }



        private void btnCancelProcess_Click(object sender, RoutedEventArgs e)
        {
            OpenVCamLib.StopOperation(m_op_id);
            ClosePanelProgress();
        }

        private void UpdateProgressBar(int video_id, int perc)
        {
            foreach (Video v in m_parent.VideoMetaData.MetaData.Videos)
            {
                if (v.Id == video_id)
                {
                    txtVideo.Text = "Processing video " + v.Name;
                }
            }
            pbProcess.Value = perc;
        }

        private void ClosePanelProgress()
        {
            pnlProgressOverlay.Visibility = Visibility.Hidden;            
            pnlDetails.Visibility = Visibility.Visible;
            pnlDetailsBorder.Visibility = Visibility.Visible;
            btnCancelProcess.Visibility = Visibility.Hidden;
            
            //Transfer data from temp VMD file back to memory (as C++ code might have updated the temp VMD file)
            m_parent.VideoMetaData.Reaload();

            ButtonState();
        }

        private void ClosePanelUpload()
        {
            pnlProgressOverlay.Visibility = Visibility.Hidden;
            pnlDetails.Visibility = Visibility.Visible;
            pnlDetailsBorder.Visibility = Visibility.Visible;
            btnCancelProcess.Visibility = Visibility.Hidden;
        
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
            ListViewItems.IsEnabled = (pnlProgressOverlay.Visibility != Visibility.Visible);
            m_parent.pnlNavBar.IsEnabled = (pnlProgressOverlay.Visibility != Visibility.Visible);            
            btnAddVideo.IsEnabled = (pnlProgressOverlay.Visibility != Visibility.Visible);
            btnDelVideo.IsEnabled = (pnlProgressOverlay.Visibility != Visibility.Visible);
            btnOthers.IsEnabled = (pnlProgressOverlay.Visibility != Visibility.Visible);            
        }

        private void ListViewItems_Drop(object sender, DragEventArgs e)
        {   
            if (e.Data.GetDataPresent("Video") && sender == e.Source)
            {
                int index = -1;

                // Get the drop ListViewItem destination
                ListView listView = sender as ListView;
                ListViewItem listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
                if (listViewItem == null)
                {
                    // Abort
                    e.Effects = DragDropEffects.None;
                    return;
                }
                // Find the data behind the ListViewItem
                Video item = (Video)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
                // Move item into observable collection 
                // (this will be automatically reflected to lstView.ItemsSource)
                e.Effects = DragDropEffects.Move;
                index = ViewModel.MetaData.Videos.IndexOf(item);
                if (startIndex >= 0 && index >= 0)
                {
                    ViewModel.MetaData.Videos.Move(startIndex, index);
                }
                startIndex = -1;
            }
            else
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file_name in files)
                    ViewModel.MetaData.Videos.Add(new Video(file_name));
            }
        }

        private void ListViewItems_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get current mouse position
            startPoint = e.GetPosition(null);
        }        

        private void ListViewItems_MouseMove(object sender, MouseEventArgs e)
        {
            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                // Get the dragged ListViewItem
                ListView listView = sender as ListView;
                ListViewItem listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
                if (listViewItem == null) 
                    return;           

                // Find the data behind the ListViewItem
                Video item = (Video)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
                if (item == null) 
                    return;

                // Initialize the drag & drop operation
                startIndex = ListViewItems.SelectedIndex;
                DataObject dragData = new DataObject("Video", item);
                DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Copy | DragDropEffects.Move);
            }
        }

        private void ListViewItems_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("Video") || sender != e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }
    }
}
