using System;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Interop;
using System.Windows.Forms;
namespace WpfAppControl
{
    public partial class AppControl : System.Windows.Controls.UserControl, IDisposable
    {
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct HWND__ {
            public int unused;
        }

        public AppControl()
        {
            InitializeComponent();
            // gan cac su kien thay doi kich thuoc
            this.SizeChanged += new SizeChangedEventHandler(OnSizeChanged);
            this.Loaded += new RoutedEventHandler(OnVisibleChanged);
            this.SizeChanged += new SizeChangedEventHandler(OnResize);
        }

        ~AppControl()
        {
            this.Dispose();
        }
        private bool _iscreated = false;
        private bool _isdisposed = false;
        IntPtr _appWin;
        private Process _childp;
        private string exeName = "";

        public string ExeName
        {
            get
            {
                return exeName;
            }
            set
            {
                exeName = value;				
            }
        }


        [DllImport("user32.dll", EntryPoint="GetWindowThreadProcessId",  SetLastError=true,
             CharSet=CharSet.Unicode, ExactSpelling=true,
             CallingConvention=CallingConvention.StdCall)]
        private static extern long GetWindowThreadProcessId(long hWnd, long lpdwProcessId); 
            
        [DllImport("user32.dll", SetLastError=true)]
        private static extern IntPtr FindWindow (string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError=true)]
        private static extern long SetParent (IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", EntryPoint="GetWindowLongA", SetLastError=true)]
        private static extern long GetWindowLong (IntPtr hwnd, int nIndex);

        [DllImport("user32.dll", EntryPoint="SetWindowLongA", SetLastError=true)]
        public static extern int SetWindowLongA([System.Runtime.InteropServices.InAttribute()] System.IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError=true)]
        private static extern long SetWindowPos(IntPtr hwnd, long hWndInsertAfter, long x, long y, long cx, long cy, long wFlags);
        
        [DllImport("user32.dll", SetLastError=true)]
        private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);

        private const int SWP_NOOWNERZORDER = 0x200;
        private const int SWP_NOREDRAW = 0x8;
        private const int SWP_NOZORDER = 0x4;
        private const int SWP_SHOWWINDOW = 0x0040;
        private const int WS_EX_MDICHILD = 0x40;
        private const int SWP_FRAMECHANGED = 0x20;
        private const int SWP_NOACTIVATE = 0x10;
        private const int SWP_ASYNCWINDOWPOS = 0x4000;
        private const int SWP_NOMOVE = 0x2;
        private const int SWP_NOSIZE = 0x1;
        private const int GWL_STYLE = (-16);
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CHILD = 0x40000000;

        protected void OnSizeChanged(object s, SizeChangedEventArgs e)
        {
            this.InvalidateVisual();
        }
        protected void OnVisibleChanged(object s, RoutedEventArgs e)
        {
            if (_iscreated == false)
            {
                _iscreated = true;
                _appWin = IntPtr.Zero;

                try
                {
                    var procInfo = new System.Diagnostics.ProcessStartInfo(this.exeName);
                    procInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(this.exeName);
                    _childp = System.Diagnostics.Process.Start(procInfo);
                    _childp.WaitForInputIdle();
                    _appWin = _childp.MainWindowHandle;
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message + "Error");
                }
                var helper = new WindowInteropHelper(Window.GetWindow(this.AppContainer));
                SetParent(_appWin, helper.Handle);
                SetWindowLongA(_appWin, GWL_STYLE, WS_VISIBLE);
                var nWidth = this.ActualWidth * Screen.PrimaryScreen.WorkingArea.Width / SystemParameters.WorkArea.Width;
                var nHeight = this.ActualHeight * Screen.PrimaryScreen.WorkingArea.Height / SystemParameters.WorkArea.Height; 
                MoveWindow(_appWin, 0, 0, (int)nWidth, (int)nHeight + 25, true);
        
                
            }
        }
        protected void OnResize(object s, SizeChangedEventArgs e)
        {
            if (this._appWin != IntPtr.Zero)
            {
                var nWidth = this.ActualWidth * Screen.PrimaryScreen.WorkingArea.Width / SystemParameters.WorkArea.Width;
                var nHeight = this.ActualHeight * Screen.PrimaryScreen.WorkingArea.Height / SystemParameters.WorkArea.Height;
                MoveWindow(_appWin, 0, 0, (int)nWidth, (int)nHeight + 25, true);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isdisposed)
            {
                if (disposing)
                {
                    if (_iscreated && _appWin != IntPtr.Zero && !_childp.HasExited)
                    {
                        _childp.Kill();
                        _appWin = IntPtr.Zero;
                    }
                }
                _isdisposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
