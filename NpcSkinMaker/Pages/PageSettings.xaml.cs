using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NpcSkinMaker
{
    /// <summary>
    /// 设置页
    /// </summary>
    public partial class PageSettings : UserControl
    {
        private bool _initializing = true;

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
    }
}
