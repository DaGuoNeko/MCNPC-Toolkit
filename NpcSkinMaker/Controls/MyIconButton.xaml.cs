using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NpcSkinMaker
{
    /// <summary>
    /// 图标按钮 — 仿 PCL MyIconButton
    /// SVG path 图标，悬停缩放 + 颜色变化
    /// </summary>
    public partial class MyIconButton : Border
    {
        public enum IconTheme { Color, White, Black, Red }

        public static readonly DependencyProperty LogoProperty =
            DependencyProperty.Register("Logo", typeof(string), typeof(MyIconButton),
                new PropertyMetadata("", OnLogoChanged));

        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register("Theme", typeof(IconTheme), typeof(MyIconButton),
                new PropertyMetadata(IconTheme.Color, OnThemeChanged));

        public string Logo
        {
            get { return (string)GetValue(LogoProperty); }
            set { SetValue(LogoProperty, value); }
        }

        public new IconTheme Theme
        {
            get { return (IconTheme)GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        public new event RoutedEventHandler Click;

        private bool _isHover;
        private bool _isPressed;

        // 常用 SVG path 数据
        public static readonly string IconClose = "F1 M2,0 L0,2 8,10 0,18 2,20 10,12 18,20 20,18 12,10 20,2 18,0 10,8 2,0Z";
        public static readonly string IconMinimize = "F1 M0,0 h15 v2 h-15 v-2 Z";
        public static readonly string IconMaximize = "F1 M0,0 h15 v15 h-15 Z M2,2 h11 v11 h-11 Z";
        public static readonly string IconRestore = "F1 M0,3 h12 v12 h-12 Z M3,0 h12 v12 M3,3 v-3 M12,0 h3 v3";
        public static readonly string IconBack = "F1 M10,2 L2,10 10,18 12,16 6,10 12,4 10,2 Z";
        public static readonly string IconSearch = "F1 M7,0 C3.13,0 0,3.13 0,7 C0,10.87 3.13,14 7,14 C8.65,14 10.17,13.43 11.38,12.47 L11.38,12.47 L16.96,18.05 L18.05,16.96 L12.47,11.38 C13.43,10.17 14,8.65 14,7 C14,3.13 10.87,0 7,0 Z M7,2 C9.76,2 12,4.24 12,7 C12,9.76 9.76,12 7,12 C4.24,12 2,9.76 2,7 C2,4.24 4.24,2 7,2 Z";
        public static readonly string IconAdd = "F1 M6,0 L6,6 0,6 0,8 6,8 6,14 8,14 8,8 14,8 14,6 8,6 8,0 6,0 Z";
        public static readonly string IconCreeper = "F1 M2,2 L8,2 L8,8 L2,8 Z M8,2 L14,2 L14,8 L8,8 Z M5,8 L11,8 L11,14 L5,14 Z";
        public static readonly string IconDelete = "F1 M5,0 L5,1 0,1 0,2 1,2 1,12 2,12 2,13 3,13 3,12 8,12 8,13 9,13 9,12 10,12 10,2 11,2 11,1 6,1 6,0 5,0 Z M2,2 L9,2 L9,11 L2,11 L2,2 Z";
        public static readonly string IconEdit = "F1 M0,11 L0,14 3,14 11,6 8,3 0,11 Z M9,2 L12,5 13,4 14,3 11,0 10,1 9,2 Z";
        public static readonly string IconSettings = "F1 M8,4 C5.79,4 4,5.79 4,8 C4,10.21 5.79,12 8,12 C10.21,12 12,10.21 12,8 C12,5.79 10.21,4 8,4 Z M8,6 C9.1,6 10,6.9 10,8 C10,9.1 9.1,10 8,10 C6.9,10 6,9.1 6,8 C6,6.9 6.9,6 8,6 Z";
        public static readonly string IconHome = "F1 M8,0 L0,7 1,8 2,7.5 2,14 6,14 6,10 10,10 10,14 14,14 14,7.5 15,8 16,7 8,0 Z";
        public static readonly string IconInfo = "F1 M7,0 C3.13,0 0,3.13 0,7 C0,10.87 3.13,14 7,14 C10.87,14 14,10.87 14,7 C14,3.13 10.87,0 7,0 Z M7,2 C9.76,2 12,4.24 12,7 C12,9.76 9.76,12 7,12 C4.24,12 2,9.76 2,7 C2,4.24 4.24,2 7,2 Z M6,5 L8,5 8,11 6,11 6,5 Z M6,3 L8,3 8,4 6,4 6,3 Z";
        public static readonly string IconExport = "F1 M2,0 L2,10 0,10 3,14 6,10 4,10 4,0 2,0 Z M7,0 L7,2 12,2 12,8 7,8 7,10 14,10 14,0 7,0 Z";
        public static readonly string IconImport = "F1 M2,0 L2,10 0,10 3,14 6,10 4,10 4,0 2,0 Z M7,2 L7,4 9,4 9,6 7,6 7,8 11,8 11,2 7,2 Z";

        public MyIconButton()
        {
            InitializeComponent();
            // 用 MouseLeftButtonUp（冒泡事件）来触发 Click
            // 按下时不标记 Handled，而是让 MainWindow 判断鼠标是否在按钮上
            MouseLeftButtonUp += MyIconButton_MouseLeftButtonUp;
            MouseLeftButtonDown += MyIconButton_MouseLeftButtonDown;
            MouseEnter += MyIconButton_MouseEnter;
            MouseLeave += MyIconButton_MouseLeave;
        }

        private static void OnLogoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MyIconButton btn = d as MyIconButton;
            string pathData = e.NewValue as string;
            if (btn != null && pathData != null)
            {
                try
                {
                    btn.IconPath.Data = Geometry.Parse(pathData);
                }
                catch { }
            }
        }

        private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MyIconButton btn = d as MyIconButton;
            if (btn != null)
                btn.RefreshColor();
        }

        private void RefreshColor()
        {
            string colorKey;
            switch (Theme)
            {
                case IconTheme.White: colorKey = "ColorBrushWhite"; break;
                case IconTheme.Black: colorKey = "ColorBrush1"; break;
                case IconTheme.Red: colorKey = "ColorBrushRedLight"; break;
                default: colorKey = _isHover ? "ColorBrush3" : "ColorBrush1"; break;
            }
            object raw = Application.Current.TryFindResource(colorKey);
            SolidColorBrush brush = raw as SolidColorBrush;
            if (brush != null)
            {
                Color targetColor = brush.Color;
                var currentBrush = IconPath.Fill as SolidColorBrush;
                if (currentBrush != null && !currentBrush.IsFrozen)
                {
                    var animation = new ColorAnimation
                    {
                        To = targetColor,
                        Duration = System.TimeSpan.FromMilliseconds(100)
                    };
                    currentBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
                }
                else
                {
                    IconPath.Fill = new SolidColorBrush(targetColor);
                }
            }
            else
            {
                Color? color = raw as Color?;
                if (color.HasValue)
                    IconPath.Fill = new SolidColorBrush(color.Value);
            }
        }

        private void MyIconButton_MouseEnter(object sender, MouseEventArgs e)
        {
            _isHover = true;
            RefreshColor();
        }

        private void MyIconButton_MouseLeave(object sender, MouseEventArgs e)
        {
            _isHover = false;
            _isPressed = false;
            AniHelper.AniScale(this, 1, 300, easeBack: true);
            RefreshColor();
        }

        private void MyIconButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isPressed = true;
            AniHelper.AniScale(this, 0.9, 80);
            // 标记已处理，阻止 DragMove 吞掉
            e.Handled = true;
        }

        private void MyIconButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPressed)
            {
                _isPressed = false;
                AniHelper.AniScale(this, 1, 700, easeBack: true);
                if (Click != null)
                    Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }
    }
}
