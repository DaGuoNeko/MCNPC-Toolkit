using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NpcSkinMaker
{
    /// <summary>
    /// 自定义按钮 — 仿 PCL MyButton
    /// 圆角，hover/press 颜色渐变 + 缩放动画
    /// </summary>
    public partial class MyButton : Border
    {
        public enum ColorState { Normal, Highlight, Red }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MyButton),
                new PropertyMetadata("", OnTextChanged));

        public static readonly DependencyProperty ColorTypeProperty =
            DependencyProperty.Register("ColorType", typeof(ColorState), typeof(MyButton),
                new PropertyMetadata(ColorState.Normal, OnColorTypeChanged));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public ColorState ColorType
        {
            get { return (ColorState)GetValue(ColorTypeProperty); }
            set { SetValue(ColorTypeProperty, value); }
        }

        public new event RoutedEventHandler Click;

        private bool _isHover;
        private bool _isPressed;

        public MyButton()
        {
            InitializeComponent();
            MouseEnter += MyButton_MouseEnter;
            MouseLeave += MyButton_MouseLeave;
            MouseLeftButtonDown += MyButton_MouseLeftButtonDown;
            MouseLeftButtonUp += MyButton_MouseLeftButtonUp;
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MyButton btn = d as MyButton;
            if (btn != null)
                btn.LabText.Text = e.NewValue != null ? (e.NewValue.ToString() ?? "") : "";
        }

        private static void OnColorTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MyButton btn = d as MyButton;
            if (btn != null)
                btn.RefreshColor();
        }

        private void RefreshColor()
        {
            string borderKey, hoverBorderKey, hoverBgKey;
            switch (ColorType)
            {
                case ColorState.Highlight:
                    borderKey = "ColorBrush2";
                    hoverBorderKey = "ColorBrush3";
                    hoverBgKey = "ColorBrush7";
                    break;
                case ColorState.Red:
                    borderKey = "ColorBrushRedDark";
                    hoverBorderKey = "ColorBrushRedLight";
                    hoverBgKey = "ColorBrushRedBack";
                    break;
                default:
                    borderKey = "ColorBrush1";
                    hoverBorderKey = "ColorBrush3";
                    hoverBgKey = "ColorBrush7";
                    break;
            }

            if (_isHover)
            {
                AniHelper.AniColorByResource(this, "BorderBrush", hoverBorderKey, 100);
                AniHelper.AniColorByResource(this, "Background", hoverBgKey, 100);
            }
            else
            {
                AniHelper.AniColorByResource(this, "BorderBrush", borderKey, 200);
                AniHelper.AniColorByResource(this, "Background", "ColorBrushHalfWhite", 200);
            }
        }

        private void MyButton_MouseEnter(object sender, MouseEventArgs e)
        {
            _isHover = true;
            RefreshColor();
        }

        private void MyButton_MouseLeave(object sender, MouseEventArgs e)
        {
            _isHover = false;
            _isPressed = false;

            // 恢复缩放
            if (ScaleTrans.ScaleX != 1)
                AniHelper.AniScale(this, 1, 300, easeBack: true);

            RefreshColor();
        }

        private void MyButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isPressed = true;
            AniHelper.AniScale(this, 0.955, 80);
        }

        private void MyButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPressed)
            {
                _isPressed = false;
                AniHelper.AniScale(this, 1, 700, easeBack: true);
                if (Click != null)
                    Click(this, new RoutedEventArgs());
            }
        }
    }
}
