using openvcamapp.winos;
using openvcamapp.winos.controls;
using openvcamapp.winos.models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    public partial class CropVideoWindow : WindowBase
    {
        public static readonly DependencyProperty MinProperty = DependencyProperty.Register("Min", typeof(double), typeof(CropVideoWindow), new PropertyMetadata(0d));
        public static readonly DependencyProperty MaxProperty = DependencyProperty.Register("Max", typeof(double), typeof(CropVideoWindow), new PropertyMetadata(100d));
        public static readonly DependencyProperty StartProperty = DependencyProperty.Register("Start", typeof(double), typeof(CropVideoWindow), new PropertyMetadata(20d));
        public static readonly DependencyProperty EndProperty = DependencyProperty.Register("End", typeof(double), typeof(CropVideoWindow), new PropertyMetadata(85d));

        private CropVideoModel m_model;
        private bool m_start_thumb_selected;
        private bool m_end_thumb_selected;
        private bool m_paused;

        public delegate void timerTick();
        private DispatcherTimer m_ticks = new DispatcherTimer();
        private timerTick m_tick;

        public CropVideoWindow(string VideoFileName, CropVideoModel Model, Window Owner)
        {
            InitializeComponent();

            this.Owner = Owner;

            m_model = Model;            

            if (File.Exists(VideoFileName))
            {
                mediaElement.Source = new Uri(VideoFileName);
                mediaElement.ScrubbingEnabled = true;
                mediaElement.Play();
                mediaElement.Pause();
                m_paused = true;
            }
        }

        public double Max
        {
            get => (double)GetValue(MaxProperty);
            set => SetValue(MaxProperty, value);
        }

        public double Min
        {
            get => (double)GetValue(MinProperty);
            set => SetValue(MinProperty, value);
        }

        public double Start
        {
            get => (double)GetValue(StartProperty);
            set => SetValue(StartProperty, value);
        }

        public double End
        {
            get => (double)GetValue(EndProperty);
            set => SetValue(EndProperty, value);
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
            if (!m_paused)
            {
                if (m_start_thumb_selected)
                {
                    mediaElement.Position = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Start);
                }
                else if (m_end_thumb_selected)
                {
                    mediaElement.Position = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.End);
                }
                else
                {
                    mediaElement.Position = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Start);
                }
            }

            m_paused = false;
            mediaElement.Play();
        }

        void OnMouseDownPauseMedia(object sender, MouseButtonEventArgs args)
        {
            m_paused = true;
            mediaElement.Pause();
        }

        void OnMouseDownStopMedia(object sender, MouseButtonEventArgs args)
        {
            m_paused = false;
            mediaElement.Stop();
            mediaElement.Position = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Start);            

            timelineSlider.StartThumb.Background = new SolidColorBrush(Color.FromArgb(255, 146, 208, 80));
            timelineSlider.EndThumb.Background = new SolidColorBrush(Color.FromArgb(255, 118, 113, 113));

            m_start_thumb_selected = true;
            m_end_thumb_selected = false;
        }

        void OnMouseDownBackMedia(object sender, MouseButtonEventArgs args)
        {
            m_paused = false;

            if (m_start_thumb_selected)
            {
                mediaElement.Position = mediaElement.Position.Subtract(TimeSpan.FromSeconds(1));

                timelineSlider.Start = mediaElement.Position.TotalMilliseconds;

                if (timelineSlider.Start < 0)
                    timelineSlider.Start = 0;
            }
            else if (m_end_thumb_selected)
            {
                mediaElement.Position = mediaElement.Position.Subtract(TimeSpan.FromSeconds(1));

                timelineSlider.End = mediaElement.Position.TotalMilliseconds;

                if (timelineSlider.Start > timelineSlider.End)
                {
                    mediaElement.Position = mediaElement.Position.Add(TimeSpan.FromSeconds(1));
                    timelineSlider.End = mediaElement.Position.TotalMilliseconds;
                }
            }
            else
            {
                m_start_thumb_selected = true;
                timelineSlider.StartThumb.Background = new SolidColorBrush(Color.FromArgb(255, 146, 208, 80));

                mediaElement.Position = mediaElement.Position.Subtract(TimeSpan.FromSeconds(1));

                timelineSlider.Start = mediaElement.Position.TotalMilliseconds;

                if (timelineSlider.Start < 0)
                {
                    timelineSlider.Start = 0;
                    mediaElement.Position = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Start);
                }
            }
        }

        void OnMouseDownForwardMedia(object sender, MouseButtonEventArgs args)
        {
            m_paused = false;

            if (m_start_thumb_selected)
            {
                mediaElement.Position = mediaElement.Position.Add(TimeSpan.FromSeconds(1));

                timelineSlider.Start = mediaElement.Position.TotalMilliseconds;

                if (timelineSlider.Start > timelineSlider.End)
                {
                    mediaElement.Position = mediaElement.Position.Subtract(TimeSpan.FromSeconds(1));
                    timelineSlider.Start = mediaElement.Position.TotalMilliseconds;
                }
            }
            else if (m_end_thumb_selected)
            {
                mediaElement.Position = mediaElement.Position.Add(TimeSpan.FromSeconds(1));

                timelineSlider.End = mediaElement.Position.TotalMilliseconds;

                if (timelineSlider.End > timelineSlider.Maximum)
                {
                    mediaElement.Position = mediaElement.Position.Subtract(TimeSpan.FromSeconds(1));
                    timelineSlider.End = mediaElement.Position.TotalMilliseconds;
                }
            }
            else
            {
                m_start_thumb_selected = true;
                timelineSlider.StartThumb.Background = new SolidColorBrush(Color.FromArgb(255, 146, 208, 80));

                mediaElement.Position = mediaElement.Position.Add(TimeSpan.FromSeconds(1));

                timelineSlider.Start = mediaElement.Position.TotalMilliseconds;

                if (timelineSlider.Start > timelineSlider.End)
                {
                    mediaElement.Position = mediaElement.Position.Subtract(TimeSpan.FromSeconds(1));
                    timelineSlider.Start = mediaElement.Position.TotalMilliseconds;
                }
            }
        }

        private void Element_MediaOpened(object sender, EventArgs e)
        {
            Min = 0;
            Max = mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;            
            Start = ((m_model.Start.HasValue) && (m_model.Start.Value > 0)) ? (m_model.Start.Value * 1000) : 0;
            End = ((m_model.End.HasValue) && (m_model.End.Value > 0)) ? (m_model.End.Value * 1000) : mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;

            timelineSlider.StartThumb.Background = new SolidColorBrush(Color.FromArgb(255, 146, 208, 80));
            mediaElement.Position = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Start);

            EndTime.Text = Milliseconds_to_Minute((long)mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds);

            m_ticks.Interval = TimeSpan.FromMilliseconds(1);
            m_ticks.Tick += ticks_Tick;
            m_tick = new timerTick(changeStatus);
            m_ticks.Start();
        }
        void ticks_Tick(object sender, object e)
        {
            Dispatcher.Invoke(m_tick);
        }
        void changeStatus()
        {
            long end_millisecs = (long)new TimeSpan(0, 0, 0, 0, (int)timelineSlider.End).TotalMilliseconds;
            long current_millisecs = (long)mediaElement.Position.TotalMilliseconds;

            if ((current_millisecs == 0) && (timelineSlider.Start > 0))
                mediaElement.Position = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Start);

            //if (current_millisecs > end_millisecs)
            //{
            //    mediaElement.Stop();
            //    mediaElement.Position = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Start);
            //}

            if (m_start_thumb_selected)
            {
                StartTime.Text = Milliseconds_to_Minute((long)mediaElement.Position.TotalMilliseconds);
                EndTime.Text = Milliseconds_to_Minute(end_millisecs);
            }
            else if (m_end_thumb_selected)
            {
                EndTime.Text = Milliseconds_to_Minute((long)mediaElement.Position.TotalMilliseconds);
            }
            else
            {
                StartTime.Text = Milliseconds_to_Minute((long)mediaElement.Position.TotalMilliseconds);
                EndTime.Text = Milliseconds_to_Minute(end_millisecs);
            }
        }
        public string Milliseconds_to_Minute(long milliseconds)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(milliseconds);
            return (t.ToString(@"hh\:mm\:ss"));
        }

        private void Element_MediaEnded(object sender, EventArgs e)
        {   
            mediaElement.Stop();
        }

        private void timelineSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            timelineSlider.StartThumb.Background = new SolidColorBrush(Color.FromArgb(255, 118, 113, 113));
            timelineSlider.EndThumb.Background = new SolidColorBrush(Color.FromArgb(255, 118, 113, 113));
            ((Thumb)e.OriginalSource).Background = new SolidColorBrush(Color.FromArgb(255, 146, 208, 80));

            TimeSpan ts = new TimeSpan();
            if (((Thumb)e.OriginalSource).Name == "PART_StartThumb")
            {
                m_start_thumb_selected = true;
                m_end_thumb_selected = false;
                ts = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Start);
            }
            else if (((Thumb)e.OriginalSource).Name == "PART_EndThumb")
            {
                m_start_thumb_selected = false;
                m_end_thumb_selected = true;
                ts = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.End);
            }

            mediaElement.Position = ts;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            m_model.Start = Start;
            m_model.End = End;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            m_model.Start = null;
            m_model.End = null;
            this.Close();
        }
    }
}
