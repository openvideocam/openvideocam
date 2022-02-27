using openvcamapp.winos;
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
using System.Windows.Threading;

namespace openvcamapp.WinOS.windows
{
    /// <summary>
    /// Interaction logic for FullScreenVideoWindow.xaml
    /// </summary>
    public partial class FullScreenVideoWindow : WindowBase
    {
        public delegate void timerTick();
        DispatcherTimer ticks = new DispatcherTimer();
        timerTick tick;

        public FullScreenVideoWindow(string VideoFileName, Window Owner)
        {
            InitializeComponent();

            this.Owner = Owner;

            if (File.Exists(VideoFileName))
            {
                mediaElement.Source = new Uri(VideoFileName);
                mediaElement.ScrubbingEnabled = true;
                mediaElement.Play();
                mediaElement.Pause();
            }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnCloseApp_Click(object sender, RoutedEventArgs e)
        {            
            this.Close();
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

        void OnMouseDownPlayMedia(object sender, MouseButtonEventArgs args)
        {
            mediaElement.Play();
        }

        void OnMouseDownPauseMedia(object sender, MouseButtonEventArgs args)
        {
            mediaElement.Pause();
        }

        void OnMouseDownStopMedia(object sender, MouseButtonEventArgs args)
        {
            mediaElement.Stop();
        }

        private void Element_MediaOpened(object sender, EventArgs e)
        {
            timelineSlider.Minimum = 0;
            timelineSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
            TotalTime.Text = Milliseconds_to_Minute((long)mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds);
            ticks.Interval = TimeSpan.FromMilliseconds(1);
            ticks.Tick += ticks_Tick;
            tick = new timerTick(changeStatus);
            ticks.Start();
        }
        void ticks_Tick(object sender, object e)
        {
            Dispatcher.Invoke(tick);
        }
        void changeStatus()
        {
            Duration.Text = Milliseconds_to_Minute((long)mediaElement.Position.TotalMilliseconds);
        }
        public string Milliseconds_to_Minute(long milliseconds)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(milliseconds);
            return (t.ToString(@"hh\:mm\:ss"));
        }

        private void DurationSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            TimeSpan ts = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Value);
            mediaElement.Position = ts;
        }

        private void Element_MediaEnded(object sender, EventArgs e)
        {
            timelineSlider.Value = 0;
            mediaElement.Stop();
        }

        private void SeekToMediaPosition(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            int SliderValue = (int)timelineSlider.Value;

            TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
            mediaElement.Position = ts;
        }
    }
}
