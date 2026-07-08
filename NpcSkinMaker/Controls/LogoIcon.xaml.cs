using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NpcSkinMaker
{
    /// <summary>
    /// Minecraft 方块 Logo — 自带浮动动画
    /// 使用方式：<local:LogoIcon Width="20" Height="20" Stroke="White" Fill="#22FFFFFF" />
    /// </summary>
    public partial class LogoIcon : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Brush), typeof(LogoIcon),
                new PropertyMetadata(Brushes.White, OnStrokeChanged));

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill", typeof(Brush), typeof(LogoIcon),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)), OnFillChanged));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(LogoIcon),
                new PropertyMetadata(1.2, OnStrokeThicknessChanged));

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public new double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        private static void OnStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as LogoIcon).Root.Stroke = e.NewValue as Brush;
        }

        private static void OnFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as LogoIcon).Root.Fill = e.NewValue as Brush;
        }

        private static void OnStrokeThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as LogoIcon).Root.StrokeThickness = (double)e.NewValue;
        }

        public LogoIcon()
        {
            InitializeComponent();

            Loaded += LogoIcon_Loaded;
        }

        private void LogoIcon_Loaded(object sender, RoutedEventArgs e)
        {
            if (AniHelper.ControlEnabled > 0) return;

            var floatAni = new DoubleAnimation
            {
                From = -1.5,
                To = 1.5,
                Duration = TimeSpan.FromMilliseconds(1700),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            floatAni.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            FloatTransform.BeginAnimation(TranslateTransform.YProperty, floatAni);
        }
    }
}
