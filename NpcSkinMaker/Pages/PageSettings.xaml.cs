using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace NpcSkinMaker
{
    /// <summary>
    /// 设置页
    /// </summary>
    public partial class PageSettings : UserControl
    {
        private bool _initializing = true;
        private string _selectedFontName;

        public PageSettings()
        {
            InitializeComponent();

            var settings = MainWindow.Instance.Settings;

            TxtOutputDir.Text = settings.LastOutputDir;
            TxtBgImage.Text = settings.BgImagePath;
            SliderHue.Value = settings.ThemeHue;
            SliderSat.Value = settings.ThemeSat;
            LabHue.Text = settings.ThemeHue.ToString();
            LabSat.Text = settings.ThemeSat.ToString();
            ChkSystemAccent.IsChecked = settings.UseSystemAccent;

            UpdateSliderState(settings.UseSystemAccent);

            BtnBrowseDir.Click += BtnBrowseDir_Click;
            BtnBrowseBg.Click += BtnBrowseBg_Click;
            BtnClearBg.Click += BtnClearBg_Click;

            SliderHue.ValueChanged += OnHueChanged;
            SliderSat.ValueChanged += OnSatChanged;

            // 预设主题
            AddThemePreset("蓝色 (默认)", 210, 85);
            AddThemePreset("紫色", 270, 70);
            AddThemePreset("粉色", 330, 80);
            AddThemePreset("红色", 0, 75);
            AddThemePreset("橙色", 30, 90);
            AddThemePreset("绿色", 140, 70);
            AddThemePreset("青色", 180, 70);

            // 字体设置
            InitFontSettings(settings);

            _initializing = false;
        }

        private void UpdateSliderState(bool useSystem)
        {
            SliderHue.IsEnabled = !useSystem;
            SliderSat.IsEnabled = !useSystem;
            PanThemes.IsEnabled = !useSystem;
        }

        private void AddThemePreset(string name, int hue, int sat)
        {
            var color = ThemeManager.HslToColor(hue, sat, 55);
            var btn = new MyButton
            {
                Text = name,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(12, 5, 12, 5)
            };
            btn.Click += (s, e) =>
            {
                SliderHue.Value = hue;
                SliderSat.Value = sat;
            };
            PanThemes.Children.Add(btn);
        }

        private void ChkSystemAccent_Changed(object sender, RoutedEventArgs e)
        {
            if (_initializing) return;

            bool useSystem = ChkSystemAccent.IsChecked == true;
            MainWindow.Instance.Settings.UseSystemAccent = useSystem;
            MainWindow.Instance.Settings.Save();

            UpdateSliderState(useSystem);

            if (useSystem)
                ThemeManager.ApplySystemAccent();
            else
                ThemeManager.Apply((int)SliderHue.Value, (int)SliderSat.Value);
        }

        private void BtnBrowseDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (!string.IsNullOrEmpty(TxtOutputDir.GetText()))
                dialog.SelectedPath = TxtOutputDir.GetText();

            if (dialog.ShowDialog() == true)
            {
                TxtOutputDir.Text = dialog.SelectedPath;
                MainWindow.Instance.Settings.LastOutputDir = dialog.SelectedPath;
                MainWindow.Instance.Settings.Save();
            }
        }

        private void OnHueChanged(double value)
        {
            if (_initializing) return;
            int hue = (int)value;
            int sat = (int)SliderSat.Value;
            LabHue.Text = hue.ToString();

            MainWindow.Instance.Settings.ThemeHue = hue;
            MainWindow.Instance.Settings.ThemeSat = sat;
            MainWindow.Instance.Settings.Save();

            ThemeManager.Apply(hue, sat);
        }

        private void OnSatChanged(double value)
        {
            if (_initializing) return;
            int hue = (int)SliderHue.Value;
            int sat = (int)value;
            LabSat.Text = sat.ToString();

            MainWindow.Instance.Settings.ThemeHue = hue;
            MainWindow.Instance.Settings.ThemeSat = sat;
            MainWindow.Instance.Settings.Save();

            ThemeManager.Apply(hue, sat);
        }

        private void BtnBrowseBg_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择背景图片",
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*"
            };
            if (ofd.ShowDialog() == true)
            {
                TxtBgImage.Text = ofd.FileName;
                MainWindow.Instance.Settings.BgImagePath = ofd.FileName;
                MainWindow.Instance.Settings.Save();
                MainWindow.Instance.SetBackground(ofd.FileName);
            }
        }

        private void BtnClearBg_Click(object sender, RoutedEventArgs e)
        {
            TxtBgImage.Text = "";
            MainWindow.Instance.Settings.BgImagePath = "";
            MainWindow.Instance.Settings.Save();
            MainWindow.Instance.SetBackground(null);
        }

        #region 字体设置

        /// <summary>
        /// 初始化字体下拉列表：系统字体 + 当前保存的字体
        /// </summary>
        private void InitFontSettings(AppSettings settings)
        {
            _selectedFontName = settings.FontFamilyName;

            // 收集系统字体（按名称排序）
            var systemFonts = new List<string>();
            foreach (var family in Fonts.SystemFontFamilies)
            {
                string name = family.Source;
                if (!systemFonts.Contains(name))
                    systemFonts.Add(name);
            }
            systemFonts.Sort();

            // 先加一个「自定义字体文件」占位项
            CmbFont.Items.Add("（自定义字体文件）");

            foreach (string name in systemFonts)
                CmbFont.Items.Add(name);

            // 选中当前字体
            SelectCurrentFont(settings.FontFamilyName);

            // 切换字体时立即应用
            CmbFont.SelectionChanged += (_, _) =>
            {
                if (_initializing) return;
                if (CmbFont.SelectedIndex <= 0) return; // 跳过占位项

                string fontName = CmbFont.SelectedItem as string;
                if (string.IsNullOrEmpty(fontName)) return;

                _selectedFontName = fontName;
                ApplyFontGlobally(fontName, false);
                LabFontPreview.FontFamily = new FontFamily(fontName);

                settings.FontFamilyName = fontName;
                settings.Save();
            };

            // 选择字体文件按钮
            BtnBrowseFont.Click += (_, _) =>
            {
                var ofd = new OpenFileDialog
                {
                    Title = "选择字体文件",
                    Filter = "字体文件|*.ttf;*.otf;*.ttc|所有文件|*.*"
                };
                if (ofd.ShowDialog() == true)
                {
                    _selectedFontName = ofd.FileName;
                    ApplyFontGlobally(ofd.FileName, true);
                    LabFontPreview.FontFamily = LoadFontFromFile(ofd.FileName);

                    // 取消系统字体选中
                    CmbFont.SelectedIndex = 0;
                    CmbFont.Text = Path.GetFileName(ofd.FileName);

                    settings.FontFamilyName = ofd.FileName;
                    settings.Save();
                }
            };

            // 设置预览字体
            LabFontPreview.FontFamily = GetCurrentFontFamily(settings.FontFamilyName);
        }

        /// <summary>
        /// 在 ComboBox 中选中对应字体
        /// </summary>
        private void SelectCurrentFont(string fontName)
        {
            if (string.IsNullOrEmpty(fontName))
            {
                CmbFont.SelectedIndex = 0;
                return;
            }

            // 检查是否是文件路径
            if (File.Exists(fontName))
            {
                CmbFont.SelectedIndex = 0;
                CmbFont.Text = "(自定义) " + Path.GetFileName(fontName);
                return;
            }

            // 在列表中查找
            for (int i = 1; i < CmbFont.Items.Count; i++)
            {
                if (string.Equals(CmbFont.Items[i] as string, fontName, StringComparison.OrdinalIgnoreCase))
                {
                    CmbFont.SelectedIndex = i;
                    return;
                }
            }

            CmbFont.SelectedIndex = 0;
            CmbFont.Text = fontName;
        }

        /// <summary>
        /// 获取当前字体 FontFamily 对象
        /// </summary>
        private FontFamily GetCurrentFontFamily(string fontName)
        {
            if (string.IsNullOrEmpty(fontName))
                return new FontFamily("Microsoft YaHei UI");

            if (File.Exists(fontName))
                return LoadFontFromFile(fontName);

            return new FontFamily(fontName);
        }

        /// <summary>
        /// 从字体文件加载 FontFamily
        /// </summary>
        private static FontFamily LoadFontFromFile(string filePath)
        {
            try
            {
                var families = Fonts.GetFontFamilies(filePath);
                if (families != null && families.Count > 0)
                    return families.First();
            }
            catch { }
            return new FontFamily("Microsoft YaHei UI");
        }

        /// <summary>
        /// 全局应用字体到所有控件
        /// </summary>
        private static void ApplyFontGlobally(string fontName, bool isFile)
        {
            FontFamily font;
            if (isFile)
                font = LoadFontFromFile(fontName);
            else
                font = new FontFamily(fontName);

            Application.Current.Resources["FontDefault"] = font;
        }

        #endregion
    }
}
