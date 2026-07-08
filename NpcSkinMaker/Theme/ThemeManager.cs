using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace NpcSkinMaker
{
    /// <summary>
    /// 主题管理器 - 仿 PCL ModSecret 的 HSL 颜色生成
    /// 通过 HSL 生成 8 级色阶，运行时替换 Application.Resources
    /// 支持从 Windows 系统主题色自动检测
    /// </summary>
    public static class ThemeManager
    {
        public static int Hue { get; private set; }
        public static int Sat { get; private set; }
        public static bool UseSystemAccent { get; private set; }

        static ThemeManager()
        {
            Hue = 210;
            Sat = 85;
            UseSystemAccent = false;
        }

        /// <summary>检测并返回 Windows 系统主题色 (hue, sat)</summary>
        public static bool DetectSystemAccent(out int hue, out int sat)
        {
            try
            {
                // Windows 10 1903+：HKCU\Software\Microsoft\Windows\DWM\AccentColor（ABGR 格式）
                object value = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM",
                    "AccentColor", null);

                if (value is int)
                {
                    int abgr = (int)value;
                    byte a = (byte)((abgr >> 24) & 0xFF);
                    if (a == 0) { hue = 210; sat = 85; return false; }

                    // ABGR 转 RGB
                    byte r = (byte)(abgr & 0xFF);
                    byte g = (byte)((abgr >> 8) & 0xFF);
                    byte b = (byte)((abgr >> 16) & 0xFF);

                    RgbToHsl(r, g, b, out double h, out double s, out double l);
                    hue = (int)Math.Round(h);
                    sat = (int)Math.Round(s * 100);
                    return true;
                }
            }
            catch { }

            hue = 210;
            sat = 85;
            return false;
        }

        /// <summary>应用系统主题色（如果存在），否则保持默认蓝色</summary>
        public static void ApplySystemAccent()
        {
            int hue, sat;
            if (DetectSystemAccent(out hue, out sat))
            {
                UseSystemAccent = true;
                Apply(hue, sat);
            }
        }

        /// <summary>RGB 转 HSL</summary>
        private static void RgbToHsl(byte r, byte g, byte b, out double h, out double s, out double l)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            l = (max + min) / 2.0;

            if (delta == 0)
            {
                h = 0;
                s = 0;
            }
            else
            {
                s = l > 0.5 ? delta / (2.0 - max - min) : delta / (max + min);

                if (max == rd)
                    h = ((gd - bd) / delta + (gd < bd ? 6 : 0)) * 60;
                else if (max == gd)
                    h = ((bd - rd) / delta + 2) * 60;
                else
                    h = ((rd - gd) / delta + 4) * 60;
            }
        }

        /// <summary>应用主题</summary>
        public static void Apply(int hue, int sat)
        {
            Hue = hue;
            Sat = sat;

            var res = Application.Current.Resources;

            // Color1: 低饱和度，暗色 — 文字/图标
            res["ColorObject1"] = HslToColor(hue, sat * 0.2, 25);
            // Color2: 标题栏强调
            res["ColorObject2"] = HslToColor(hue, sat, 45);
            // Color3：焦点/悬停
            res["ColorObject3"] = HslToColor(hue, sat, 55);
            // Color4：悬停色
            res["ColorObject4"] = HslToColor(hue, sat, 65);
            // Color5-8: 渐浅
            res["ColorObject5"] = HslToColor(hue, sat, 80);
            res["ColorObject6"] = HslToColor(hue, sat, 91);
            res["ColorObject7"] = HslToColor(hue, sat, 95);
            res["ColorObject8"] = HslToColor(hue, sat, 97);

            // 更新画刷
            UpdateBrushes(res);

            // 更新标题栏渐变
            UpdateTitleBar(hue, sat);
        }

        private static void UpdateBrushes(ResourceDictionary res)
        {
            for (int i = 1; i <= 8; i++)
            {
                string key = "ColorBrush" + i;
                var color = (Color)res["ColorObject" + i];
                // 总是创建新画刷替换，避免冻结的画刷无法修改
                var newBrush = new SolidColorBrush(color);
                res[key] = newBrush;
            }
        }

        private static void UpdateTitleBar(int hue, int sat)
        {
            // 标题栏 3-stop 渐变
            var res = Application.Current.Resources;
            var c1 = HslToColor(hue - 3, sat, 48);
            var c2 = HslToColor(hue, sat, 52);
            var c3 = HslToColor(hue + 3, sat, 54);

            if (MainWindow.Instance != null && MainWindow.Instance.PanTitleEl != null)
            {
                var brush = new LinearGradientBrush();
                brush.StartPoint = new Point(0, 0);
                brush.EndPoint = new Point(1, 0);
                brush.GradientStops.Add(new GradientStop(c1, 0));
                brush.GradientStops.Add(new GradientStop(c2, 0.5));
                brush.GradientStops.Add(new GradientStop(c3, 1));
                brush.Freeze();
                MainWindow.Instance.PanTitleEl.Background = brush;
            }
        }

        /// <summary>HSL 转 RGB Color</summary>
        public static Color HslToColor(double h, double s, double l)
        {
            h = h % 360;
            if (h < 0) h += 360;
            s = Math.Max(0, Math.Min(100, s)) / 100.0;
            l = Math.Max(0, Math.Min(100, l)) / 100.0;

            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = l - c / 2;

            double r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return Color.FromRgb(
                (byte)Math.Round((r + m) * 255),
                (byte)Math.Round((g + m) * 255),
                (byte)Math.Round((b + m) * 255));
        }
    }
}
