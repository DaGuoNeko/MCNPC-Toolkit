using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NpcSkinMaker
{
    /// <summary>
    /// 浮动提示/通知 — 仿 PCL MyHint
    /// 左侧色条 + 背景色，用于显示提示信息
    /// </summary>
    public partial class MyHint : Border
    {
        public enum HintTheme { Blue, Yellow, Red }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MyHint),
                new PropertyMetadata("", OnTextChanged));

        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register("Theme", typeof(HintTheme), typeof(MyHint),
                new PropertyMetadata(HintTheme.Blue, OnThemeChanged));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public new HintTheme Theme
        {
            get { return (HintTheme)GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        public MyHint()
        {
            InitializeComponent();
            RefreshTheme();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MyHint hint = d as MyHint;
            if (hint != null)
                hint.LabText.Text = e.NewValue != null ? (e.NewValue.ToString() ?? "") : "";
        }

        private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MyHint hint = d as MyHint;
            if (hint != null)
                hint.RefreshTheme();
        }

        private void RefreshTheme()
        {
            string bgKey, borderKey;
            switch (Theme)
            {
                case HintTheme.Yellow:
                    bgKey = "ColorBrush8";
                    borderKey = "ColorBrush2";
                    break;
                case HintTheme.Red:
                    bgKey = "ColorBrushRedBack";
                    borderKey = "ColorBrushRedDark";
                    break;
                default:
                    bgKey = "ColorBrush8";
                    borderKey = "ColorBrush3";
                    break;
            }

            Background = (Brush)Application.Current.TryFindResource(bgKey);
            BorderBrush = (Brush)Application.Current.TryFindResource(borderKey);
        }

        /// <summary>显示提示（带入场动画）</summary>
        public void Show(string text, HintTheme theme = HintTheme.Blue)
        {
            Text = text;
            Theme = theme;
            RefreshTheme();
            Visibility = Visibility.Visible;
            Opacity = 0;
            AniHelper.AniOpacity(this, 1, 200);
        }

        /// <summary>隐藏提示</summary>
        public void Hide()
        {
            AniHelper.AniOpacity(this, 0, 150);
            System.Threading.Tasks.Task.Delay(150).ContinueWith(delegate(System.Threading.Tasks.Task t)
            {
                Dispatcher.Invoke(new Action(() => Visibility = Visibility.Collapsed));
            });
        }
    }
}
