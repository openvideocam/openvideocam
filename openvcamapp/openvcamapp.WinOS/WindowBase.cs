using OpenVCam.Common.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace openvcamapp.winos
{
    public class WindowBase : Window
    {
        protected void SetFormPosition()
        {
            int w;
            int h;
            int x;
            int y;

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
                w = ini.ReadInteger("MainWindowCoordinates", "WIDTH", Convert.ToInt32(this.Width));
                h = ini.ReadInteger("MainWindowCoordinates", "HEIGHT", Convert.ToInt32(this.Height));
                x = ini.ReadInteger("MainWindowCoordinates", "X", Convert.ToInt32(this.Left));
                y = ini.ReadInteger("MainWindowCoordinates", "Y", Convert.ToInt32(this.Top));

                if ((x == 0) && (y == 0))
                {
                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                if ((x == -1) && (y == -1) && (w == -1) && (h == -1))
                {
                    this.WindowState = WindowState.Maximized;
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    this.Width = w;
                    this.Height = h;
                    this.Left = x;
                    this.Top = y;
                }
            }
            catch (Exception)
            {
                this.WindowState = WindowState.Maximized;
            }
        }
        protected void SaveFormPosition()
        {
            int w;
            int h;
            int x;
            int y;

            if ((this.WindowState == WindowState.Maximized) | (this.WindowState == WindowState.Minimized))
            {
                w = -1;
                h = -1;
                x = -1;
                y = -1;
            }
            else
            {
                w = Convert.ToInt32(this.Width);
                h = Convert.ToInt32(this.Height);
                x = Convert.ToInt32(this.Left);
                y = Convert.ToInt32(this.Top);
            }

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
                ini.WriteInteger("MainWindowCoordinates", "WIDTH", w);
                ini.WriteInteger("MainWindowCoordinates", "HEIGHT", h);
                ini.WriteInteger("MainWindowCoordinates", "X", x);
                ini.WriteInteger("MainWindowCoordinates", "Y", y);

                ini.Save();
            }
            catch (Exception)
            {

            }
        }

        protected bool ShowTutorial()
        {
            bool result = false;

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
                result = ini.ReadBool("Tutorial", "SHOW_WINDOW", false);                
            }
            catch (Exception)
            {
                
            }

            return (result);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ((HwndSource)PresentationSource.FromVisual(this)).AddHook(HookProc);
        }

        private const int WM_GETMINMAXINFO = 0x0024;

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.Left = left;
                this.Top = top;
                this.Right = right;
                this.Bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        public static IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                // We need to tell the system what our size should be when maximized. Otherwise it will cover the whole screen,
                // including the task bar.
                MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

                // Adjust the maximized size and position to fit the work area of the correct monitor
                IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                if (monitor != IntPtr.Zero)
                {
                    MONITORINFO monitorInfo = new MONITORINFO();
                    monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                    GetMonitorInfo(monitor, ref monitorInfo);
                    RECT rcWorkArea = monitorInfo.rcWork;
                    RECT rcMonitorArea = monitorInfo.rcMonitor;
                    mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                    mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);
                    mmi.ptMaxSize.X = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
                    mmi.ptMaxSize.Y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top);
                }

                Marshal.StructureToPtr(mmi, lParam, true);
            }

            return IntPtr.Zero;
        }
    }
}
