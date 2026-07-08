using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NpcSkinMaker
{
    /// <summary>
    /// 圆角输入框 — 仿 PCL MyTextBox
    /// 悬停/聚焦时边框变色
    /// </summary>
    public partial class MyTextBox : Border
    {
        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register("Hint", typeof(string), typeof(MyTextBox),
                new PropertyMetadata("", OnHintChanged));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MyTextBox),
                new PropertyMetadata("", OnTextChanged));

        public string Hint
        {
            get { return (string)GetValue(HintProperty); }
            set { SetValue(HintProperty, value); }
        }

        public new string Text
        {
            get { return InnerBox.Text; }
            set { InnerBox.Text = value ?? ""; }
        }

        public bool IsPassword
        {
            get { return _isPassword; }
            set
            {
                _isPassword = value;
                InnerBox.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
                _passwordBox.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private bool _isPassword;
        private PasswordBox _passwordBox;

        public MyTextBox()
        {
            InitializeComponent();

            // 添加 PasswordBox
            _passwordBox = new PasswordBox
            {
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                FontSize = 13,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrush1"),
                CaretBrush = (Brush)Application.Current.TryFindResource("ColorBrush3"),
                SelectionBrush = (Brush)Application.Current.TryFindResource("ColorBrush5"),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            (InnerBox.Parent as Grid).Children.Add(_passwordBox);

            InnerBox.GotFocus += InnerBox_GotFocus;
            InnerBox.LostFocus += InnerBox_LostFocus;
            InnerBox.TextChanged += InnerBox_TextChanged;
            _passwordBox.GotFocus += InnerBox_GotFocus;
            _passwordBox.LostFocus += InnerBox_LostFocus;

            MouseEnter += MyTextBox_MouseEnter;
            MouseLeave += MyTextBox_MouseLeave;

            UpdateHintVisibility();
        }

        private static void OnHintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MyTextBox tb = d as MyTextBox;
            if (tb != null)
            {
                tb.LabHint.Text = e.NewValue != null ? (e.NewValue.ToString() ?? "") : "";
                tb.UpdateHintVisibility();
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MyTextBox tb = d as MyTextBox;
            if (tb != null)
            {
                tb.InnerBox.Text = e.NewValue != null ? (e.NewValue.ToString() ?? "") : "";
            }
        }

        private void InnerBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateHintVisibility();
        }

        private void UpdateHintVisibility()
        {
            LabHint.Visibility = string.IsNullOrEmpty(InnerBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void InnerBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AniHelper.AniColorByResource(this, "BorderBrush", "ColorBrush3", 100);
        }

        private void InnerBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AniHelper.AniColorByResource(this, "BorderBrush", "ColorBrushGray4", 200);
        }

        private void MyTextBox_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!InnerBox.IsFocused && !_passwordBox.IsFocused)
                AniHelper.AniColorByResource(this, "BorderBrush", "ColorBrush4", 100);
        }

        private void MyTextBox_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!InnerBox.IsFocused && !_passwordBox.IsFocused)
                AniHelper.AniColorByResource(this, "BorderBrush", "ColorBrushGray4", 200);
        }

        public string GetText() { return _isPassword ? _passwordBox.Password : InnerBox.Text; }

        public void SetFocus() { InnerBox.Focus(); }
    }
}
