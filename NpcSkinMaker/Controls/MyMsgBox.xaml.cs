using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NpcSkinMaker
{
    /// <summary>
    /// 自定义消息框 - 仿 PCL2 MyMsgBox
    /// 替代系统 MessageBox，拥有 PCL2 风格的圆角窗口 + 动画
    /// 窗口大小和位置跟随主窗口，毛玻璃遮罩只覆盖主窗口区域
    /// </summary>
    public partial class MyMsgBox : Window
    {
        /// <summary>消息类型，决定标题栏颜色</summary>
        public enum MsgType { Info, Warning, Error, Question }

        /// <summary>返回结果</summary>
        public enum MsgResult { OK, Yes, No, Cancel }

        private MsgResult _result = MsgResult.OK;
        private RotateTransform _rotate;
        private TranslateTransform _translate;
        private bool _isYesNo;

        /// <summary>
        /// 显示消息框（确认按钮）
        /// </summary>
        public static MsgResult Show(string message, string title = "提示", MsgType type = MsgType.Info)
        {
            var box = new MyMsgBox(message, title, type, false);
            box.Owner = MainWindow.Instance;
            box.SyncWithMainWindow();
            box.ShowDialog();
            return box._result;
        }

        /// <summary>
        /// 显示消息框（是/否按钮）
        /// </summary>
        public static MsgResult ShowYesNo(string message, string title = "确认", MsgType type = MsgType.Question)
        {
            var box = new MyMsgBox(message, title, type, true);
            box.Owner = MainWindow.Instance;
            box.SyncWithMainWindow();
            box.ShowDialog();
            return box._result;
        }

        /// <summary>将消息框的大小和位置同步为主窗口可见区域</summary>
        private void SyncWithMainWindow()
        {
            var main = MainWindow.Instance;
            if (main == null) return;

            WindowStartupLocation = WindowStartupLocation.Manual;

            var borderForm = main.BorderFormEl;
            var topLeft = borderForm.PointToScreen(new Point(0, 0));
            Left = topLeft.X;
            Top = topLeft.Y;
            Width = borderForm.ActualWidth;
            Height = borderForm.ActualHeight;
        }

        private MyMsgBox(string message, string title, MsgType type, bool isYesNo)
        {
            InitializeComponent();

            _isYesNo = isYesNo;

            // 设置标题和消息
            LabTitle.Text = title;
            LabMessage.Text = message;

            // 根据类型设置标题颜色和分隔线颜色
            Brush titleBrush;
            switch (type)
            {
                case MsgType.Warning:
                    titleBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0x8E, 0x00));
                    break;
                case MsgType.Error:
                    titleBrush = new SolidColorBrush(Color.FromRgb(0xCE, 0x21, 0x11));
                    break;
                default:
                    titleBrush = (Brush)Application.Current.TryFindResource("ColorBrush2");
                    break;
            }
            LabTitle.Foreground = titleBrush;
            PanTitleLine.Fill = titleBrush;

            // 添加按钮
            if (isYesNo)
            {
                var btnNo = CreateButton("否", false);
                btnNo.Click += (s, e) => { _result = MsgResult.No; CloseWithAnimation(); };
                PanButtons.Children.Add(btnNo);

                var btnYes = CreateButton("是", true);
                btnYes.Click += (s, e) => { _result = MsgResult.Yes; CloseWithAnimation(); };
                PanButtons.Children.Add(btnYes);
            }
            else
            {
                var btnOK = CreateButton("确定", true);
                btnOK.Click += (s, e) => { _result = MsgResult.OK; CloseWithAnimation(); };
                PanButtons.Children.Add(btnOK);
            }

            // 设置 TransformGroup
            var group = new TransformGroup();
            _rotate = new RotateTransform(-4);
            _translate = new TranslateTransform(0, 40);
            group.Children.Add(_rotate);
            group.Children.Add(_translate);
            PanContent.RenderTransformOrigin = new Point(0, 0.5);
            PanContent.RenderTransform = group;
            PanContent.Opacity = 0;

            // 标题栏拖拽（不影响，因为是最大化窗口不动）
            PanTitle.MouseLeftButtonDown += (s, e) => { e.Handled = true; };

            // 点击遮罩区域关闭（仅 YesNo 模式点遮罩 = 否，OK 模式点遮罩 = 确定）
            Backdrop.MouseLeftButtonDown += (s, e) =>
            {
                _result = isYesNo ? MsgResult.No : MsgResult.OK;
                CloseWithAnimation();
            };

            Loaded += (s, e) =>
            {
                // 给遮罩加圆角裁剪（匹配主窗口 BorderForm 的 CornerRadius=6）
                PanRoot.Clip = new RectangleGeometry(new Rect(0, 0, PanRoot.ActualWidth, PanRoot.ActualHeight), 6, 6);
                PlayEnterAnimation();
            };

            // 键盘
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    _result = isYesNo ? MsgResult.No : MsgResult.Cancel;
                    CloseWithAnimation();
                }
                else if (e.Key == Key.Enter)
                {
                    _result = isYesNo ? MsgResult.Yes : MsgResult.OK;
                    CloseWithAnimation();
                }
            };
        }

        private MyButton CreateButton(string text, bool isHighlight)
        {
            var btn = new MyButton
            {
                Text = text,
                ColorType = isHighlight ? MyButton.ColorState.Highlight : MyButton.ColorState.Normal,
                Margin = isHighlight ? new Thickness(0) : new Thickness(0, 0, 8, 0),
                Padding = new Thickness(20, 6, 20, 6)
            };
            return btn;
        }

        private void PlayEnterAnimation()
        {
            // 遮罩淡入
            var aniBackdrop = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            Backdrop.BeginAnimation(OpacityProperty, aniBackdrop);

            // 弹窗淡入 120ms，延迟 60ms
            var aniOpacity = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(120),
                BeginTime = TimeSpan.FromMilliseconds(60)
            };
            aniOpacity.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            PanContent.BeginAnimation(OpacityProperty, aniOpacity);

            // Y 从 40 -> 0，300ms，延迟 60ms，带回弹
            var aniY = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(60)
            };
            aniY.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
            _translate.BeginAnimation(TranslateTransform.YProperty, aniY);

            // 旋转从 -4 -> 0，300ms，延迟 60ms
            var aniRot = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(60)
            };
            aniRot.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            _rotate.BeginAnimation(RotateTransform.AngleProperty, aniRot);
        }

        public void CloseWithAnimation()
        {
            // 旋转 -> +6°，150ms
            var aniRot = new DoubleAnimation
            {
                To = 6,
                Duration = TimeSpan.FromMilliseconds(150)
            };
            aniRot.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn };
            _rotate.BeginAnimation(RotateTransform.AngleProperty, aniRot);

            // Y -> 20，150ms
            var aniY = new DoubleAnimation
            {
                To = 20,
                Duration = TimeSpan.FromMilliseconds(150)
            };
            aniY.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn };
            _translate.BeginAnimation(TranslateTransform.YProperty, aniY);

            // 淡出 80ms，延迟 20ms
            var aniFade = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(80),
                BeginTime = TimeSpan.FromMilliseconds(20)
            };
            aniFade.Completed += (s, e) => Close();
            PanContent.BeginAnimation(OpacityProperty, aniFade);

            // 遮罩淡出
            var aniBackdrop = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            Backdrop.BeginAnimation(OpacityProperty, aniBackdrop);
        }

        /// <summary>输入对话框（和 MyMsgBox 同一遮罩风格）</summary>
        public static string Prompt(string label, string defaultValue = "")
        {
            var box = new MyMsgBox(label, label, MsgType.Info, false);
            box.SyncWithMainWindow();
            box.Owner = MainWindow.Instance;

            // 替换消息内容区为输入框
            box.LabMessage.Visibility = Visibility.Collapsed;
            var tb = new MyTextBox { Hint = defaultValue, Text = defaultValue, Margin = new Thickness(0, 4, 0, 0) };
            var contentPanel = box.FindName("LabMessage") as FrameworkElement;
            var parent = contentPanel?.Parent as StackPanel;
            if (parent != null)
            {
                int idx = parent.Children.IndexOf(contentPanel);
                parent.Children.Insert(idx + 1, tb);
            }

            // 替换按钮为 确定/取消
            box.PanButtons.Children.Clear();
            string result = null;
            var btnOk = new MyButton { Text = "确定", ColorType = MyButton.ColorState.Highlight, Margin = new Thickness(0, 0, 12, 0), Padding = new Thickness(20, 6, 20, 6) };
            btnOk.Click += (s, e) => { result = tb.GetText(); box.CloseWithAnimation(); };
            var btnCancel = new MyButton { Text = "取消", Padding = new Thickness(20, 6, 20, 6) };
            btnCancel.Click += (s, e) => { box.CloseWithAnimation(); };
            box.PanButtons.Children.Add(btnCancel);
            box.PanButtons.Children.Add(btnOk);
            tb.InnerBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) { result = tb.GetText(); box.CloseWithAnimation(); } };

            box.ShowDialog();
            return result ?? defaultValue;
        }
    }
}
