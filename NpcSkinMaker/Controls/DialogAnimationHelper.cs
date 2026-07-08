using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NpcSkinMaker
{
    /// <summary>
    /// 对话框动画辅助类 - 为无边框对话框添加 PCL2 风格的入场/退场动画
    /// 同时添加黑色半透明遮罩背景（跟随主窗口大小）
    /// 使用方式：在对话框构造函数末尾调用 DialogAnimationHelper.Setup(this);
    /// </summary>
    public static class DialogAnimationHelper
    {
        /// <summary>
        /// 为对话框设置动画 + 遮罩 + 标题栏拖拽 + 圆角裁剪
        /// 对话框窗口会自动匹配主窗口大小和位置，内容居中显示
        /// </summary>
        public static void Setup(Window dialog)
        {
            // 获取内容 Border（Window.Content 是 Border）
            var contentBorder = dialog.Content as Border;
            if (contentBorder == null)
            {
                contentBorder = FindFirstChild<Border>(dialog.Content as DependencyObject);
            }
            if (contentBorder == null) return;

            // ===== 创建遮罩层 =====
            var overlayGrid = new Grid();
            var backdrop = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0)), // 黑色 50%
                Opacity = 0
            };
            overlayGrid.Children.Add(backdrop);
            dialog.Content = overlayGrid;
            overlayGrid.Children.Add(contentBorder);
            contentBorder.HorizontalAlignment = HorizontalAlignment.Center;
            contentBorder.VerticalAlignment = VerticalAlignment.Center;

            // ===== 设置 TransformGroup：旋转 + 位移 =====
            var group = new TransformGroup();
            var rotate = new RotateTransform(-4);
            var translate = new TranslateTransform(0, 40);
            group.Children.Add(rotate);
            group.Children.Add(translate);
            contentBorder.RenderTransformOrigin = new Point(0, 0.5);
            contentBorder.RenderTransform = group;
            contentBorder.Opacity = 0;

            // ===== 标题栏拖拽 =====
            // 对话框不再支持拖拽移动，统一固定在主窗口上方

            // ===== 同步主窗口大小和位置 =====
            SyncWithMainWindow(dialog);

            // ===== Loaded 事件 =====
            dialog.Loaded += (s, e) =>
            {
                // 给遮罩加圆角裁剪（匹配主窗口 BorderForm 的 CornerRadius=6）
                overlayGrid.Clip = new RectangleGeometry(new Rect(0, 0, overlayGrid.ActualWidth, overlayGrid.ActualHeight), 6, 6);
                // 遮罩淡入
                var aniBackdrop = new DoubleAnimation
                {
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(200)
                };
                backdrop.BeginAnimation(UIElement.OpacityProperty, aniBackdrop);

                // 圆角裁剪
                var clip = contentBorder.Clip as RectangleGeometry;
                if (clip != null)
                    clip.Rect = new Rect(0, 0, contentBorder.ActualWidth, contentBorder.ActualHeight);

                // 入场动画
                PlayEnterAnimation(contentBorder, rotate, translate);
            };

            // ===== 退场动画时同时淡出遮罩 =====
            // 存储引用供 PlayExitAnimationAndClose 使用
            dialog.Tag = backdrop;
        }

        /// <summary>将对话框大小和位置同步为主窗口可见区域（用 BorderForm 精确匹配，排除透明边距）</summary>
        private static void SyncWithMainWindow(Window dialog)
        {
            var main = MainWindow.Instance;
            if (main == null) return;

            dialog.WindowStartupLocation = WindowStartupLocation.Manual;

            // 用 BorderForm 的实际屏幕位置和大小（不含透明边距和缩放手柄）
            var borderForm = main.BorderFormEl;
            var topLeft = borderForm.PointToScreen(new Point(0, 0));
            dialog.Left = topLeft.X;
            dialog.Top = topLeft.Y;
            dialog.Width = borderForm.ActualWidth;
            dialog.Height = borderForm.ActualHeight;
        }

        /// <summary>播放入场动画</summary>
        private static void PlayEnterAnimation(FrameworkElement target, RotateTransform rotate, TranslateTransform translate)
        {
            if (AniHelper.ControlEnabled > 0)
            {
                target.Opacity = 1;
                rotate.Angle = 0;
                translate.Y = 0;
                return;
            }

            // 淡入 120ms，延迟 60ms
            var aniOpacity = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(120),
                BeginTime = TimeSpan.FromMilliseconds(60)
            };
            aniOpacity.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            target.BeginAnimation(UIElement.OpacityProperty, aniOpacity);

            // Y 从 40 -> 0，300ms，延迟 60ms，带回弹
            var aniY = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(60)
            };
            aniY.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
            translate.BeginAnimation(TranslateTransform.YProperty, aniY);

            // 旋转从 -4 -> 0，300ms，延迟 60ms
            var aniRot = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(60)
            };
            aniRot.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            rotate.BeginAnimation(RotateTransform.AngleProperty, aniRot);
        }

        /// <summary>播放退场动画并在完成后关闭窗口</summary>
        public static void PlayExitAnimationAndClose(Window dialog)
        {
            // 找内容 Border
            var overlayGrid = dialog.Content as Grid;
            Border contentBorder = null;
            Border backdrop = null;
            if (overlayGrid != null)
            {
                foreach (var child in overlayGrid.Children)
                {
                    if (child is Border b)
                    {
                        if (b.Background is SolidColorBrush sb && sb.Color.A == 0x80 && sb.Color.R == 0)
                            backdrop = b;
                        else
                            contentBorder = b;
                    }
                }
            }
            // 回退处理
            if (contentBorder == null)
                contentBorder = dialog.Content as Border;
            if (contentBorder == null) { dialog.Close(); return; }

            if (AniHelper.ControlEnabled > 0)
            {
                dialog.Close();
                return;
            }

            var group = contentBorder.RenderTransform as TransformGroup;
            if (group != null && group.Children.Count >= 2)
            {
                var rotate = group.Children[0] as RotateTransform;
                var translate = group.Children[1] as TranslateTransform;

                if (translate != null)
                {
                    var aniY = new DoubleAnimation
                    {
                        To = 20,
                        Duration = TimeSpan.FromMilliseconds(150)
                    };
                    aniY.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn };
                    translate.BeginAnimation(TranslateTransform.YProperty, aniY);
                }

                if (rotate != null)
                {
                    var aniRot = new DoubleAnimation
                    {
                        To = 6,
                        Duration = TimeSpan.FromMilliseconds(150)
                    };
                    aniRot.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn };
                    rotate.BeginAnimation(RotateTransform.AngleProperty, aniRot);
                }
            }

            // 淡出 80ms，延迟 20ms
            var aniFade = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(80),
                BeginTime = TimeSpan.FromMilliseconds(20)
            };
            aniFade.Completed += (s, e) => dialog.Close();
            contentBorder.BeginAnimation(UIElement.OpacityProperty, aniFade);

            // 遮罩淡出
            if (backdrop != null)
            {
                var aniBackdrop = new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(200)
                };
                backdrop.BeginAnimation(UIElement.OpacityProperty, aniBackdrop);
            }
        }

        private static T FindFirstChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T) return (T)child;
                var result = FindFirstChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
