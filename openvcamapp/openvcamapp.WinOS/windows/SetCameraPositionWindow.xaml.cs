using OpenVCam.DataAccess;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
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

namespace openvcamapp.WinOS.windows
{
    /// <summary>
    /// Interaction logic for SetFieldAreaWindow.xaml
    /// </summary>
    public partial class SetCameraPositionWindow : Window
    {
        private Video m_video;
        private int m_camera_position;

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public SetCameraPositionWindow(Video video, Window Owner)
        {
            InitializeComponent();

            this.Owner = Owner;

            m_video = video;

            m_camera_position = -1;
            foreach (Button button in FindVisualChildren<Button>(gridMain))
            {
                if (m_video.CameraPosition == Convert.ToInt32(button.Tag))
                {                    
                    button.Background = new SolidColorBrush(Color.FromArgb(255, 146, 208, 80));
                    m_camera_position = Convert.ToInt32(button.Tag);
                    break;
                }
            }

            if (m_camera_position == -1)
            {
                btn1.Background = new SolidColorBrush(Color.FromArgb(255, 146, 208, 80));
                m_camera_position = 1;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {            
            m_video.CameraPosition = m_camera_position;
            
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void imgSnapshot_Loaded(object sender, RoutedEventArgs e)
        {
            if (m_video.Snapshot != null)
            {
                var stream = new System.IO.MemoryStream();
                m_video.Snapshot.Save(stream, ImageFormat.Png);
                stream.Position = 0;
                var snapshot_bitmap = new System.Windows.Media.Imaging.BitmapImage();
                snapshot_bitmap.BeginInit();
                snapshot_bitmap.CacheOption = BitmapCacheOption.OnLoad;
                snapshot_bitmap.StreamSource = stream;
                snapshot_bitmap.EndInit();
                ((System.Windows.Controls.Image)sender).Source = snapshot_bitmap;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            Grid parent_grid = (Grid)button.Parent;

            foreach (UIElement c in parent_grid.Children)
            {
                if (c is Button)
                    ((Button)c).Background = new SolidColorBrush(Color.FromArgb(255, 208, 206, 206));
            }

            button.Background = new SolidColorBrush(Color.FromArgb(255, 146, 208, 80));

            m_camera_position = Convert.ToInt32(button.Tag);
        }
    }
}
