using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NpcSkinMaker
{
    /// <summary>
    /// 自定义滑块 — 仿 PCL2 MySlider
    /// 用 Line + Ellipse 绘制，无 WPF Slider 的丑陋默认样式
    /// </summary>
    public partial class MySlider : Border
    {
        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(double), typeof(MySlider),
                new PropertyMetadata(100.0, OnMaxChanged));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(MySlider),
                new PropertyMetadata(0.0, OnValueChanged));

        public double MaxValue
        {
            get { return (double)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, Math.Max(0, Math.Min(value, MaxValue))); }
        }

        /// <summary>用户拖动或点击改变值时触发</summary>
        public event Action<double> ValueChanged;

        private bool _isDragging;
        private double _dragStartValue;
        private Point _dragStartPos;

        public MySlider()
        {
            InitializeComponent();

            MouseLeftButtonDown += MySlider_MouseLeftButtonDown;
            MouseMove += MySlider_MouseMove;
            MouseLeftButtonUp += MySlider_MouseLeftButtonUp;
            MouseLeave += MySlider_MouseLeave;

            // 键盘支持
            KeyDown += MySlider_KeyDown;

            Loaded += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() => RefreshWidth(false)), DispatcherPriority.Loaded);
            };
            SizeChanged += (s, e) => RefreshWidth(false);
        }

        private static void OnMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MySlider).RefreshWidth(false);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = d as MySlider;
            slider.RefreshWidth(true);
            if (slider.ValueChanged != null)
                slider.ValueChanged(slider.Value);
        }

        private void MySlider_KeyDown(object sender, KeyEventArgs e)
        {
            double step = MaxValue / 100.0;
            if (step < 1) step = 1;

            if (e.Key == Key.Left || e.Key == Key.Down)
                Value -= step;
            else if (e.Key == Key.Right || e.Key == Key.Up)
                Value += step;
        }

        private void MySlider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            _isDragging = true;
            _dragStartPos = e.GetPosition(this);
            _dragStartValue = Value;
            CaptureMouse();

            // 缩放动画
            AniHelper.AniScale(ShapeDot, 1.3, 100);

            UpdateValueFromMouse(e.GetPosition(this));
        }

        private void MySlider_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            UpdateValueFromMouse(e.GetPosition(this));
        }

        private void MySlider_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDragging)
                UpdateValueFromMouse(e.GetPosition(this));
        }

        private void MySlider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            ReleaseMouseCapture();

            AniHelper.AniScale(ShapeDot, 1.0, 200, easeBack: true);
        }

        private void UpdateValueFromMouse(Point pos)
        {
            double totalW = ActualWidth - ShapeDot.Width;
            if (totalW <= 0) return;

            double ratio = Math.Max(0, Math.Min(1, pos.X / totalW));
            double newValue = Math.Round(ratio * MaxValue);

            if (Math.Abs(newValue - Value) > 0.01)
            {
                Value = newValue;
            }
        }

        private void RefreshWidth(bool animated)
        {
            double totalW = ActualWidth - ShapeDot.Width;
            if (totalW <= 0) return;

            double ratio = MaxValue > 0 ? Value / MaxValue : 0;
            double fillW = totalW * ratio;
            double backW = totalW - fillW;

            // 拖拽时不播动画（即时响应），属性变化时用短动画（100ms）
            if (animated)
            {
                AniHelper.AniDouble(LineFore, FrameworkElement.WidthProperty, fillW, 50, 0);
                AniHelper.AniDouble(LineBack, FrameworkElement.WidthProperty, backW, 50, 0);
                AniHelper.AniThickness(ShapeDot, new Thickness(fillW, 0, 0, 0), 50, 0);
            }
            else
            {
                LineFore.Width = fillW;
                LineBack.Width = backW;
                ShapeDot.Margin = new Thickness(fillW, 0, 0, 0);
            }
        }
    }
}
