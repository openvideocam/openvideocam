using openvcam.WinOS;
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
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace openvcamapp.WinOS.windows
{
    /// <summary>
    /// Interaction logic for SetFieldAreaWindow.xaml
    /// </summary>
    public partial class SetFieldAreaWindow : Window
    {
        private List<Video> m_videos;
        private MainWindow m_parent;
        private Point m_first_point;
        private List<Point> m_points;
        private double m_base_value;

        private void CleanPoints()
        {
            m_first_point = new Point();
            m_points.Clear();

            List<Ellipse> ellipses = cnvImage.Children.OfType<Ellipse>().ToList();
            foreach (Ellipse e in ellipses)
                cnvImage.Children.Remove(e);

            List<Line> lines = cnvImage.Children.OfType<Line>().ToList();
            foreach (Line line in lines)
                cnvImage.Children.Remove(line);
        }
        
        private void DrawLines(List<Point> Points)
        {
            for (int i = 0; i < Points.Count - 1; i++)
            {
                if (i == 0)
                {
                    m_first_point = Points[i];

                    Ellipse circle = new Ellipse()
                    {
                        Width = 10,
                        Height = 10,
                        Stroke = Brushes.GreenYellow,
                        StrokeThickness = 2,
                        Fill = Brushes.GreenYellow
                    };

                    cnvImage.Children.Add(circle);

                    circle.SetValue(Canvas.LeftProperty, (double)Points[i].X - 5);
                    circle.SetValue(Canvas.TopProperty, (double)Points[i].Y - 5);
                }

                Ellipse second_circle = new Ellipse()
                {
                    Width = 10,
                    Height = 10,
                    Stroke = Brushes.GreenYellow,
                    StrokeThickness = 2,
                    Fill = Brushes.GreenYellow
                };

                cnvImage.Children.Add(second_circle);

                second_circle.SetValue(Canvas.LeftProperty, (double)Points[i + 1].X - 5);
                second_circle.SetValue(Canvas.TopProperty, (double)Points[i + 1].Y - 5);

                Line line = new Line();
                line.Stroke = Brushes.GreenYellow;
                line.StrokeThickness = 2;
                line.X1 = Points[i].X;
                line.Y1 = Points[i].Y;
                line.X2 = Points[i + 1].X;
                line.Y2 = Points[i + 1].Y;                

                cnvImage.Children.Add(line);
            }

            if (Points.Count == 1)
            {
                Ellipse circle = new Ellipse()
                {
                    Width = 10,
                    Height = 10,
                    Stroke = Brushes.GreenYellow,
                    StrokeThickness = 2,
                    Fill = Brushes.GreenYellow
                };

                cnvImage.Children.Add(circle);

                circle.SetValue(Canvas.LeftProperty, (double)Points[0].X - 5);
                circle.SetValue(Canvas.TopProperty, (double)Points[0].Y - 5);                
            }
            else if (Points.Count > 1)
            {
                Line new_dotted_line = new Line();
                new_dotted_line.Stroke = Brushes.GreenYellow;
                new_dotted_line.StrokeThickness = 2;
                new_dotted_line.StrokeDashArray = new DoubleCollection { 2, 2 };
                new_dotted_line.X1 = Points[Points.Count - 1].X;
                new_dotted_line.Y1 = Points[Points.Count - 1].Y;
                new_dotted_line.X2 = m_first_point.X;
                new_dotted_line.Y2 = m_first_point.Y;

                cnvImage.Children.Add(new_dotted_line);
            }
        }

        private string PointsToString(List<Point> Points)
        {
            if (Points.Count == 0)
                return ("");            

            StringBuilder result = new StringBuilder();            
            foreach (Point p in Points)
            {
                result.Append($"{Convert.ToInt32(p.X / m_base_value)},{Convert.ToInt32(p.Y / m_base_value)}, ");
            }            

            return (result.ToString().Substring(0, result.ToString().Length - 2));
        }

        private List<Point> StringToPoints(string FieldLocation)
        {
            List<Point> result = new List<Point>();

            string[] str_points = FieldLocation.Replace(" ", "").Split(',');

            for (int i = 0; i < str_points.Length; i+=2)
            {
                result.Add(new Point((Convert.ToDouble(str_points[i]) * m_base_value), (Convert.ToDouble(str_points[i + 1]) * m_base_value)));
            }

            return (result);
        }

        public SetFieldAreaWindow(List<Video> videos, MainWindow owner)
        {
            InitializeComponent();

            this.Owner = owner;
            m_parent = owner;

            this.Width = owner.Width;
            this.Height = owner.Height;

            m_videos = videos;

            if (m_videos.Count == 1)
            {
                ckbApplyChanges.IsChecked = false;
                ckbApplyChanges.Visibility = Visibility.Hidden;
            }
            else if (m_videos.Count > 1)
            {
                ckbApplyChanges.IsChecked = true;
                ckbApplyChanges.Visibility = Visibility.Visible;
            }
            else
            {
                throw new Exception("No video selected");
            }

            m_points = new List<Point>();

            double base_height = (owner.Height - 200) / (double)m_videos[0].FrameHeight;
            double base_width = (owner.Width - 40) / (double)m_videos[0].FrameWidth;
            m_base_value = (base_height > base_width) ? base_width : base_height;

            if (m_videos[0].FieldLocation != "")
            {
                rdbSetManually.IsChecked = true;

                m_points = StringToPoints(m_videos[0].FieldLocation);

                DrawLines(m_points);               
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ckbApplyChanges.IsChecked.Value)
            {
                m_videos[0].FieldLocation = (rdbSetManually.IsChecked.Value) ? PointsToString(m_points) : "";
            }
            else
            {
                foreach (Video video in m_videos)
                {
                    video.FieldLocation = (rdbSetManually.IsChecked.Value) ? PointsToString(m_points) : "";
                }
            }

            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void imgSnapshot_Loaded(object sender, RoutedEventArgs e)
        {
            System.Drawing.Image full_snapshot = m_videos[0].GetFullSnapshot();
            if (full_snapshot != null)
            {
                var stream = new System.IO.MemoryStream();
                full_snapshot.Save(stream, ImageFormat.Png);
                stream.Position = 0;
                var snapshot_bitmap = new System.Windows.Media.Imaging.BitmapImage();
                snapshot_bitmap.BeginInit();
                snapshot_bitmap.CacheOption = BitmapCacheOption.OnLoad;
                snapshot_bitmap.StreamSource = stream;
                snapshot_bitmap.EndInit();
                ((System.Windows.Controls.Image)sender).Source = snapshot_bitmap;
                
                double height = m_videos[0].FrameHeight * m_base_value;
                ((System.Windows.Controls.Image)sender).Height = height;                
                cnvImage.Height = height;

                double width = m_videos[0].FrameWidth * m_base_value;
                ((System.Windows.Controls.Image)sender).Width = width;
                cnvImage.Width = width;
            }
        }
        
        private void imgSnapshot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (rdbSetManually.IsChecked.Value)
                {
                    Point current_point = e.GetPosition(cnvImage);

                    if (m_points.Count == 0)
                        m_first_point = current_point;

                    m_points.Add(current_point);

                    Ellipse circle = new Ellipse()
                    {
                        Width = 10,
                        Height = 10,
                        Stroke = Brushes.GreenYellow,
                        StrokeThickness = 2,
                        Fill = Brushes.GreenYellow
                    };

                    cnvImage.Children.Add(circle);

                    circle.SetValue(Canvas.LeftProperty, (double)current_point.X - 5);
                    circle.SetValue(Canvas.TopProperty, (double)current_point.Y - 5);

                    if (m_points.Count > 1)
                    {
                        if (cnvImage.Children.OfType<Line>().Count() > 0)
                            cnvImage.Children.RemoveAt(cnvImage.Children.Count - 2);                        

                        Line line = new Line();
                        line.Stroke = Brushes.GreenYellow;
                        line.StrokeThickness = 2;
                        line.X1 = m_points[m_points.Count - 2].X;
                        line.Y1 = m_points[m_points.Count - 2].Y;
                        line.X2 = current_point.X;
                        line.Y2 = current_point.Y;

                        cnvImage.Children.Add(line);

                        Line dotted_line = new Line();
                        dotted_line.Stroke = Brushes.GreenYellow;
                        dotted_line.StrokeThickness = 2;
                        dotted_line.StrokeDashArray = new DoubleCollection { 2, 2 };
                        dotted_line.X1 = current_point.X;
                        dotted_line.Y1 = current_point.Y;
                        dotted_line.X2 = m_first_point.X;
                        dotted_line.Y2 = m_first_point.Y;

                        cnvImage.Children.Add(dotted_line);
                    }
                }
            }
        }

        private void rdbAutoDetect_Checked(object sender, RoutedEventArgs e)
        {
            if (m_videos != null && m_videos[0] != null)
            {
                m_videos[0].FieldLocation = "";
                CleanPoints();
            }
        }

        private void mnuClearLines_Click(object sender, RoutedEventArgs e)
        {
            CleanPoints();
        }

        private void mnuClearLastLine_Click(object sender, RoutedEventArgs e)
        {   
            if (cnvImage.Children.OfType<Line>().Count() > 0)
            {
                Ellipse ellipse = cnvImage.Children.OfType<Ellipse>().Last();
                if (ellipse != null)
                    cnvImage.Children.Remove(ellipse);
            }

            if (cnvImage.Children.OfType<Line>().Count() == 2)
            {
                Ellipse ellipse = cnvImage.Children.OfType<Ellipse>().Last();
                if (ellipse != null)
                    cnvImage.Children.Remove(ellipse);

                Line dotted_line = cnvImage.Children.OfType<Line>().Last();
                if (dotted_line != null)
                    cnvImage.Children.Remove(dotted_line);

                Line line = cnvImage.Children.OfType<Line>().Last();
                if (line != null)
                    cnvImage.Children.Remove(line);                                

                m_first_point = new Point();
                m_points.Clear();
            }
            else if (cnvImage.Children.OfType<Line>().Count() > 2)
            {
                Line dotted_line = cnvImage.Children.OfType<Line>().Last();
                if (dotted_line != null)
                    cnvImage.Children.Remove(dotted_line);

                Line line = cnvImage.Children.OfType<Line>().Last();
                if (line != null)
                    cnvImage.Children.Remove(line);                             

                if (m_points.Count > 0)
                    m_points.RemoveAt(m_points.Count - 1);

                Line new_dotted_line = new Line();
                new_dotted_line.Stroke = Brushes.GreenYellow;
                new_dotted_line.StrokeThickness = 2;
                new_dotted_line.StrokeDashArray = new DoubleCollection { 2, 2 };
                new_dotted_line.X1 = m_points[m_points.Count - 1].X;
                new_dotted_line.Y1 = m_points[m_points.Count - 1].Y;
                new_dotted_line.X2 = m_first_point.X;
                new_dotted_line.Y2 = m_first_point.Y;

                cnvImage.Children.Add(new_dotted_line);
            }
        }
    }
}
