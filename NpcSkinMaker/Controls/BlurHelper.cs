using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace NpcSkinMaker
{
    /// <summary>
    /// Win32 窗口毛玻璃/亚克力效果辅助类
    /// 通过 SetWindowCompositionAttribute 实现系统级背景模糊
    /// </summary>
    public static class BlurHelper
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,       // 毛玻璃模糊
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4, // 亚克力效果 (Win10 1803+)
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;  // AABBGGRR 格式
            public int AnimationId;
        }

        /// <summary>
        /// 为窗口启用毛玻璃背景模糊
        /// </summary>
        /// <param name="window">目标窗口（必须已初始化句柄）</param>
        /// <param name="alpha">遮罩不透明度 0-255</param>
        /// <param name="useAcrylic">是否使用亚克力效果（Win10 1803+），false 则用传统毛玻璃</param>
        public static void EnableBlur(Window window, byte alpha = 80, bool useAcrylic = false)
        {
            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero)
            {
                window.SourceInitialized += (s, e) => EnableBlur(window, alpha, useAcrylic);
                return;
            }
            EnableBlur(helper.Handle, alpha, useAcrylic);
        }

        private static void EnableBlur(IntPtr hwnd, byte alpha, bool useAcrylic)
        {
            var accent = new AccentPolicy
            {
                AccentState = useAcrylic ? AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND : AccentState.ACCENT_ENABLE_BLURBEHIND,
                // 渐变颜色（AABBGGRR 格式）
                GradientColor = (alpha << 24) | 0x000000
            };

            int accentSize = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(accentSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    Data = accentPtr,
                    SizeOfData = accentSize
                };

                SetWindowCompositionAttribute(hwnd, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }

        /// <summary>禁用窗口模糊</summary>
        public static void DisableBlur(Window window)
        {
            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero) return;

            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_DISABLED
            };

            int accentSize = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(accentSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    Data = accentPtr,
                    SizeOfData = accentSize
                };
                SetWindowCompositionAttribute(helper.Handle, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }
    }
}
