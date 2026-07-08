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
            int side = wParam.ToInt32();
            var rect = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0) return;

            // 屏幕缩放比
            var source = PresentationSource.FromVisual(_window);
            double dpiX = source != null ? source.CompositionTarget.TransformToDevice.M11 : 1.0;
            if (dpiX == 0) dpiX = 1.0;

            int minW = (int)(_minWidth * dpiX);
            int minH = (int)(_minHeight * dpiX); // 用宽度的缩放比近似（同比例下一致）

            // 根据 16:9 计算修正
            // 判断当前是以宽还是高为主
            switch (side)
            {
                case WMSZ_RIGHT:
                case WMSZ_LEFT:
                    // 左右拖拽：宽度变了，根据宽度修正高度（保持中心位置）
                    {
                        int newHeight = (int)(width / _aspectRatio);
                        if (newHeight < minH) { newHeight = minH; width = (int)(newHeight * _aspectRatio); }
                        int midY = (rect.Top + rect.Bottom) / 2;
                        rect.Top = midY - newHeight / 2;
                        rect.Bottom = rect.Top + newHeight;
                        if (side == WMSZ_LEFT)
                            rect.Left = rect.Right - width;
                        else
                            rect.Right = rect.Left + width;
                        break;
                    }
                case WMSZ_TOP:
                case WMSZ_BOTTOM:
                    // 上下拖拽：高度变了，根据高度修正宽度（保持中心位置）
                    {
                        int newWidth = (int)(height * _aspectRatio);
                        if (newWidth < minW) { newWidth = minW; height = (int)(newWidth / _aspectRatio); }
                        int midX = (rect.Left + rect.Right) / 2;
                        rect.Left = midX - newWidth / 2;
                        rect.Right = rect.Left + newWidth;
                        if (side == WMSZ_TOP)
                            rect.Top = rect.Bottom - height;
                        else
                            rect.Bottom = rect.Top + height;
                        break;
                    }
                case WMSZ_TOPLEFT:
                case WMSZ_TOPRIGHT:
                case WMSZ_BOTTOMLEFT:
                case WMSZ_BOTTOMRIGHT:
                    // 角拖拽：以变化量大的边为准
                    {
                        // 计算保持比例后哪个维度更大
                        double ratioFromWidth = width / _aspectRatio;
                        double ratioFromHeight = height * _aspectRatio;

                        // 取需要较大变化的那个维度
                        if (ratioFromWidth > height)
                        {
                            // 宽度主导，高度偏小 -> 增大高度
                            int newHeight = (int)(width / _aspectRatio);
                            if (newHeight < minH) { newHeight = minH; width = (int)(newHeight * _aspectRatio); }
                            if (side == WMSZ_TOPLEFT || side == WMSZ_TOPRIGHT)
                                rect.Top = rect.Bottom - newHeight;
                            else
                                rect.Bottom = rect.Top + newHeight;
                            // 修正宽度
                            if (side == WMSZ_TOPLEFT || side == WMSZ_BOTTOMLEFT)
                                rect.Left = rect.Right - width;
                            else
                                rect.Right = rect.Left + width;
                        }
                        else
                        {
                            // 高度主导，宽度偏小 -> 增大宽度
                            int newWidth = (int)(height * _aspectRatio);
                            if (newWidth < minW) { newWidth = minW; height = (int)(newWidth / _aspectRatio); }
                            if (side == WMSZ_TOPLEFT || side == WMSZ_BOTTOMLEFT)
                                rect.Left = rect.Right - newWidth;
                            else
                                rect.Right = rect.Left + newWidth;
                            // 修正高度
                            if (side == WMSZ_TOPLEFT || side == WMSZ_TOPRIGHT)
                                rect.Top = rect.Bottom - height;
                            else
                                rect.Bottom = rect.Top + height;
                        }
                        break;
                    }
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
            // MINMAXINFO 结构
            var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // 获取工作区（排除任务栏）
            var monitor = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(_window).Handle);
            var workArea = monitor.WorkingArea;

            int maxW = workArea.Width;
            int maxH = workArea.Height;

            // DPI 缩放（WPF 使用设备无关像素，系统使用物理像素）
            var source = PresentationSource.FromVisual(_window);
            double dpiScaleX = source != null ? source.CompositionTarget.TransformToDevice.M11 : 1.0;
            double dpiScaleY = source != null ? source.CompositionTarget.TransformToDevice.M12 : 1.0;
            if (dpiScaleX == 0) dpiScaleX = 1.0;
            if (dpiScaleY == 0) dpiScaleY = 1.0;

            // 将 WPF 逻辑像素的最小值转换为系统物理像素
            int minW = (int)(_minWidth * dpiScaleX);
            int minH = (int)(_minHeight * dpiScaleY);

            // 限制最大尺寸也保持 16:9
            // 如果工作区太宽，用高度限制宽度；反之用宽度限制高度
            if (maxW / (double)maxH > _aspectRatio)
            {
                // 工作区比 16:9 更宽，以高度为准
                maxW = (int)(maxH * _aspectRatio);
            }
            else
            {
                // 工作区比 16:9 更窄，以宽度为准
                maxH = (int)(maxW / _aspectRatio);
            }

            mmi.ptMaxSize.x = maxW;
            mmi.ptMaxSize.y = maxH;
            mmi.ptMaxTrackSize.x = maxW;
            mmi.ptMaxTrackSize.y = maxH;
            mmi.ptMinTrackSize.x = minW;
            mmi.ptMinTrackSize.y = minH;

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
