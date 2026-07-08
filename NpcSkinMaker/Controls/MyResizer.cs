using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace NpcSkinMaker
{
    /// <summary>
    /// 窗口缩放器 - 使用 Win32 发送 WM_SYSCOMMAND SC_SIZE
    /// 让系统处理缩放，并通过 WM_GETMINMAXINFO 限制 16:9 比例
    /// </summary>
    public class MyResizer
    {
        private const int WM_SYSCOMMAND = 0x112;
        private const int SC_SIZE = 0xF000;
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int WM_SIZING = 0x0214;

        // 方向常量
        private const int WMSZ_LEFT = 1;
        private const int WMSZ_RIGHT = 2;
        private const int WMSZ_TOP = 3;
        private const int WMSZ_TOPLEFT = 4;
        private const int WMSZ_TOPRIGHT = 5;
        private const int WMSZ_BOTTOM = 6;
        private const int WMSZ_BOTTOMLEFT = 7;
        private const int WMSZ_BOTTOMRIGHT = 8;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private readonly Window _window;
        private readonly double _minWidth;
        private readonly double _minHeight;
        private readonly double _aspectRatio; // 宽/高 = 16/9

        // 当前缩放方向，用于 WM_GETMINMAXINFO 中判断哪条边固定
        private int _currentResizeMode;

        public MyResizer(Window window, double minWidth = 900, double minHeight = 550, double aspectRatio = 16.0 / 9.0)
        {
            _window = window;
            _minWidth = minWidth;
            _minHeight = minHeight;
            _aspectRatio = aspectRatio;
            _currentResizeMode = 0;

            // 注册 WM_GETMINMAXINFO 钩子，限制比例
            var helper = new WindowInteropHelper(window);
            if (helper.Handle != IntPtr.Zero)
            {
                InstallHook(helper.Handle);
            }
            else
            {
                window.SourceInitialized += (s, e) =>
                {
                    InstallHook(new WindowInteropHelper(window).Handle);
                };
            }
        }

        private HwndSource _hookSource;

        private void InstallHook(IntPtr hwnd)
        {
            _hookSource = HwndSource.FromHwnd(hwnd);
            if (_hookSource != null)
                _hookSource.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                // 最大化时不限制尺寸，让窗口占满工作区
                if (_window.WindowState != WindowState.Maximized)
                    HandleGetMinMaxInfo(lParam);
                handled = true;
            }
            else if (msg == WM_SIZING)
            {
                HandleSizing(wParam, lParam);
                handled = true;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// WM_SIZING：用户正在拖拽缩放，实时修正窗口矩形保持 16:9
        /// wParam = 缩放方向 (WMSZ_xxx)，lParam = RECT 指针
        /// </summary>
        private void HandleSizing(IntPtr wParam, IntPtr lParam)
        {
            // 不再强制 16:9 比例，只确保不小于最小尺寸
            int side = wParam.ToInt32();
            var rect = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0) return;

            var source = PresentationSource.FromVisual(_window);
            double dpiX = source != null ? source.CompositionTarget.TransformToDevice.M11 : 1.0;
            if (dpiX == 0) dpiX = 1.0;

            int minW = (int)(_minWidth * dpiX);
            int minH = (int)(_minHeight * dpiX);

            // 如果小于最小尺寸，修正
            if (width < minW)
            {
                if (side == WMSZ_LEFT || side == WMSZ_TOPLEFT || side == WMSZ_BOTTOMLEFT)
                    rect.Left = rect.Right - minW;
                else
                    rect.Right = rect.Left + minW;
            }
            if (height < minH)
            {
                if (side == WMSZ_TOP || side == WMSZ_TOPLEFT || side == WMSZ_TOPRIGHT)
                    rect.Top = rect.Bottom - minH;
                else
                    rect.Bottom = rect.Top + minH;
            }

            Marshal.StructureToPtr(rect, lParam, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>
        /// 在系统计算窗口最大/最小尺寸时介入，强制保持 16:9 比例
        /// </summary>
        private void HandleGetMinMaxInfo(IntPtr lParam)
        {
            var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            var hwnd = new WindowInteropHelper(_window).Handle;
            var monitor = System.Windows.Forms.Screen.FromHandle(hwnd);
            var workArea = monitor.WorkingArea;

            var source = PresentationSource.FromVisual(_window);
            double dpiX = source != null ? source.CompositionTarget.TransformToDevice.M11 : 1.0;
            double dpiY = source != null ? source.CompositionTarget.TransformToDevice.M22 : 1.0;
            if (dpiX == 0) dpiX = 1.0;
            if (dpiY == 0) dpiY = 1.0;

            // 最小尺寸
            mmi.ptMinTrackSize.x = (int)(_minWidth * dpiX);
            mmi.ptMinTrackSize.y = (int)(_minHeight * dpiY);

            // 最大化时占满工作区（自动避开任务栏）
            mmi.ptMaxSize.x = workArea.Width;
            mmi.ptMaxSize.y = workArea.Height;
            mmi.ptMaxPosition.x = workArea.X;
            mmi.ptMaxPosition.y = workArea.Y;
            mmi.ptMaxTrackSize.x = workArea.Width;
            mmi.ptMaxTrackSize.y = workArea.Height;

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        private void ResizeWindow(int resizeMode)
        {
            var hwnd = new WindowInteropHelper(_window).Handle;
            if (hwnd == IntPtr.Zero) return;

            _currentResizeMode = resizeMode;
            ReleaseCapture();
            SendMessage(hwnd, WM_SYSCOMMAND, (IntPtr)(SC_SIZE + resizeMode), IntPtr.Zero);
        }

        public void AddResizerLeft(FrameworkElement element)
        {
            element.PreviewMouseLeftButtonDown += (s, e) => { ResizeWindow(WMSZ_LEFT); };
        }

        public void AddResizerRight(FrameworkElement element)
        {
            element.PreviewMouseLeftButtonDown += (s, e) => { ResizeWindow(WMSZ_RIGHT); };
        }

        public void AddResizerUp(FrameworkElement element)
        {
            element.PreviewMouseLeftButtonDown += (s, e) => { ResizeWindow(WMSZ_TOP); };
        }

        public void AddResizerDown(FrameworkElement element)
        {
            element.PreviewMouseLeftButtonDown += (s, e) => { ResizeWindow(WMSZ_BOTTOM); };
        }

        public void AddResizerLeftUp(FrameworkElement element)
        {
            element.PreviewMouseLeftButtonDown += (s, e) => { ResizeWindow(WMSZ_TOPLEFT); };
        }

        public void AddResizerLeftDown(FrameworkElement element)
        {
            element.PreviewMouseLeftButtonDown += (s, e) => { ResizeWindow(WMSZ_BOTTOMLEFT); };
        }

        public void AddResizerRightUp(FrameworkElement element)
        {
            element.PreviewMouseLeftButtonDown += (s, e) => { ResizeWindow(WMSZ_TOPRIGHT); };
        }

        public void AddResizerRightDown(FrameworkElement element)
        {
            element.PreviewMouseLeftButtonDown += (s, e) => { ResizeWindow(WMSZ_BOTTOMRIGHT); };
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }
    }
}
