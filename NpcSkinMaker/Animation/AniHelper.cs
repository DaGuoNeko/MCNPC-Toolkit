using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NpcSkinMaker
{
    /// <summary>
    /// 动画工具类 — 仿 PCL ModAnimation，用 WPF Storyboard 封装
    /// </summary>
    public static class AniHelper
    {
        /// <summary>动画速度控制（>0 时禁用动画，直接设值）</summary>
        public static int ControlEnabled { get; set; }

        static AniHelper()
        {
            ControlEnabled = 0;
        }

        #region Color Animation

        /// <summary>颜色渐变动画（针对 SolidColorBrush）</summary>
        public static void AniColor(FrameworkElement target, string propertyName, Color toColor, int ms = 200, int delay = 0)
        {
            if (ControlEnabled > 0)
            {
                SetSolidColorBrush(target, propertyName, toColor);
                return;
            }

            var dp = GetDependencyProperty(target, propertyName);
            if (dp == null) return;

            var currentBrush = target.GetValue(dp) as SolidColorBrush;
            Color fromColor = (currentBrush != null) ? currentBrush.Color : Colors.Transparent;

            // 如果当前 brush 是冻结的，先用可变副本替换
            if (currentBrush != null && currentBrush.IsFrozen)
                target.SetValue(dp, new SolidColorBrush(fromColor));

            var animation = new ColorAnimation
            {
                From = fromColor,
                To = toColor,
                Duration = TimeSpan.FromMilliseconds(ms),
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };
            animation.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            target.BeginAnimation(dp, animation);
        }

        /// <summary>颜色渐变（按资源 key 名获取目标颜色）</summary>
        public static void AniColorByResource(FrameworkElement target, string propertyName, string resourceKey, int ms = 200, int delay = 0)
        {
            object raw = Application.Current.TryFindResource(resourceKey);
            Color? color = raw as Color?;
            if (color.HasValue)
                AniColor(target, propertyName, color.Value, ms, delay);
        }

        #endregion

        #region Opacity Animation

        public static void AniOpacity(FrameworkElement target, double toValue, int ms = 200, int delay = 0)
        {
            if (ControlEnabled > 0)
            {
                target.Opacity = toValue;
                return;
            }

            var animation = new DoubleAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(ms),
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };
            animation.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            target.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        #endregion

        #region Scale Animation

        /// <summary>缩放动画（使用 ScaleTransform）</summary>
        public static void AniScale(FrameworkElement target, double toScaleX, double toScaleY, int ms = 200, int delay = 0, bool easeBack = false)
        {
            if (ControlEnabled > 0)
            {
                var st = EnsureScaleTransform(target);
                st.ScaleX = toScaleX;
                st.ScaleY = toScaleY;
                return;
            }

            var transform = EnsureScaleTransform(target);

            var aniX = new DoubleAnimation
            {
                To = toScaleX,
                Duration = TimeSpan.FromMilliseconds(ms),
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };
            var aniY = new DoubleAnimation
            {
                To = toScaleY,
                Duration = TimeSpan.FromMilliseconds(ms),
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };

            if (easeBack)
            {
                aniX.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
                aniY.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
            }
            else
            {
                aniX.EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 4 };
                aniY.EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 4 };
            }

            transform.BeginAnimation(ScaleTransform.ScaleXProperty, aniX);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, aniY);
        }

        public static void AniScale(FrameworkElement target, double toScale, int ms = 200, int delay = 0, bool easeBack = false)
        {
            AniScale(target, toScale, toScale, ms, delay, easeBack);
        }

        #endregion

        #region Translate Animation

        /// <summary>位移动画（使用 TranslateTransform）</summary>
        public static void AniTranslate(FrameworkElement target, double toX, double toY, int ms = 250, int delay = 0, bool easeBack = false)
        {
            if (ControlEnabled > 0)
            {
                var tt = EnsureTranslateTransform(target);
                tt.X = toX;
                tt.Y = toY;
                return;
            }

            var transform = EnsureTranslateTransform(target);

            var aniX = new DoubleAnimation
            {
                To = toX,
                Duration = TimeSpan.FromMilliseconds(ms),
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };
            var aniY = new DoubleAnimation
            {
                To = toY,
                Duration = TimeSpan.FromMilliseconds(ms),
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };

            if (easeBack)
            {
                aniX.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
                aniY.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
            }
            else
            {
                aniX.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
                aniY.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            }

            transform.BeginAnimation(TranslateTransform.XProperty, aniX);
            transform.BeginAnimation(TranslateTransform.YProperty, aniY);
        }

        #endregion

        #region Height Animation

        public static void AniHeight(FrameworkElement target, double toHeight, int ms = 200, int delay = 0)
        {
            if (ControlEnabled > 0)
            {
                target.Height = toHeight;
                return;
            }

            var animation = new DoubleAnimation
            {
                To = toHeight,
                Duration = TimeSpan.FromMilliseconds(ms),
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };
            animation.EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 4 };
            target.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }

        #endregion

        #region Page Entrance Animation

        /// <summary>页面入场动画：顶层子元素按行下落 + 淡入（仿 PCL MyPageRight）
        /// 只动画容器的直接子元素（卡片/区块），不递归深入内部控件
        /// 每个元素从 Y=-20 下落，带回弹，错峰 80ms </summary>
        public static void PageEnterAnimation(FrameworkElement container, int baseDelay = 0)
        {
            // 先确保容器可见
            container.Opacity = 1;

            if (ControlEnabled > 0)
                return;

            int delay = baseDelay;
            var children = GetTopLevelChildren(container);
            bool hasChildren = false;
            foreach (var child in children)
            {
                var fe = child as FrameworkElement;
                if (fe == null) continue;
                hasChildren = true;

                // 初始状态：上方 20px + 透明
                var transform = EnsureTranslateTransform(fe);
                transform.X = 0;
                transform.Y = -20;
                fe.Opacity = 0;

                // 淡入 150ms
                AniOpacity(fe, 1, 150, delay);
                // Y 下落到 0，400ms，带回弹
                AniTranslateY(fe, 0, 400, delay, easeBack: true);

                // 按行错峰 80ms
                delay += 80;
            }

            // 如果没有子元素，不动画，直接显示
            if (!hasChildren)
                container.Opacity = 1;
        }

        /// <summary>页面退场动画：顶层子元素整体淡出 + 微微上飘 </summary>
        public static void PageExitAnimation(FrameworkElement container, int baseDelay = 0)
        {
            if (ControlEnabled > 0)
                return;

            // 退场不逐个延迟，整体同时淡出更快
            foreach (var child in GetTopLevelChildren(container))
            {
                var fe = child as FrameworkElement;
                if (fe == null) continue;

                AniOpacity(fe, 0, 100);
                AniTranslateY(fe, -8, 100);
            }
        }

        /// <summary>左侧栏入场动画：整体从左侧滑出 + 淡入（仿 PCL MyPageLeft）
        /// 每个导航按钮从 X=-25 滑入，带回弹，错峰延迟递减 </summary>
        public static void LeftPanelEnterAnimation(System.Collections.IEnumerable children, int baseDelay = 0)
        {
            if (ControlEnabled > 0)
                return;

            int delay = baseDelay;
            int id = 0;
            foreach (var child in children)
            {
                var fe = child as FrameworkElement;
                if (fe == null) continue;

                // 初始状态：左侧 25px + 透明
                var transform = EnsureTranslateTransform(fe);
                transform.X = -25;
                transform.Y = 0;
                fe.Opacity = 0;

                // 淡入 100ms
                AniOpacity(fe, 1, 100, delay);
                // X 滑入到 0，300ms，带回弹
                AniTranslateX(fe, 0, 300, delay, easeBack: true);

                // 错峰：延迟随 id 递减（PCL 风格），最小 7ms
                delay += Math.Max(15 - id, 7) * 2;
                id++;
            }
        }

        #endregion

        #region Dialog Animation

        /// <summary>弹窗入场动画（仿 PCL MyMsgBox）
        /// 旋转 -4° -> 0° + Y=40 -> 0 + 淡入，带遮罩变暗 </summary>
        /// <param name="dialog">弹窗内容根元素（需要有 RenderTransform）</param>
        /// <param name="backdrop">遮罩元素（可选，会从透明变到半透明黑）</param>
        public static void DialogEnterAnimation(FrameworkElement dialog, FrameworkElement backdrop = null)
        {
            if (ControlEnabled > 0)
                return;

            // 准备 TransformGroup：RotateTransform + TranslateTransform
            var group = new TransformGroup();
            var rotate = new RotateTransform(-4);
            var translate = new TranslateTransform(0, 40);
            group.Children.Add(rotate);
            group.Children.Add(translate);
            dialog.RenderTransformOrigin = new Point(0, 0.5);
            dialog.RenderTransform = group;
            dialog.Opacity = 0;

            // 遮罩变暗
            if (backdrop != null)
            {
                backdrop.Opacity = 0;
                AniOpacity(backdrop, 1, 200);
            }

            // 淡入 120ms，延迟 60ms
            AniOpacity(dialog, 1, 120, 60);

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

        /// <summary>弹窗退场动画（仿 PCL MyMsgBox）
        /// 旋转 -> +6° + Y -> 20 + 淡出 </summary>
        public static void DialogExitAnimation(FrameworkElement dialog, System.Action onComplete = null,
            FrameworkElement backdrop = null)
        {
            if (ControlEnabled > 0)
            {
                if (onComplete != null) onComplete();
                return;
            }

            var group = dialog.RenderTransform as TransformGroup;
            if (group != null && group.Children.Count >= 2)
            {
                var rotate = group.Children[0] as RotateTransform;
                var translate = group.Children[1] as TranslateTransform;

                if (translate != null)
                {
                    // Y -> 20，150ms
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
                    // 旋转 -> +6°，150ms
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
            aniFade.Completed += (s, e) => { if (onComplete != null) onComplete(); };
            dialog.BeginAnimation(UIElement.OpacityProperty, aniFade);

            // 遮罩恢复
            if (backdrop != null)
            {
                AniOpacity(backdrop, 0, 200);
            }
        }

        #endregion

        #region Single-axis Translate Helpers

        /// <summary>仅 X 轴位移</summary>
        public static void AniTranslateX(FrameworkElement target, double toX, int ms = 250, int delay = 0, bool easeBack = false)
        {
            if (ControlEnabled > 0)
            {
                EnsureTranslateTransform(target).X = toX;
                return;
            }

            var transform = EnsureTranslateTransform(target);
            var ani = new DoubleAnimation
            {
                To = toX,
                Duration = TimeSpan.FromMilliseconds(ms),
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };
            ani.EasingFunction = easeBack
                ? (IEasingFunction)new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                : new QuadraticEase { EasingMode = EasingMode.EaseOut };
            transform.BeginAnimation(TranslateTransform.XProperty, ani);
        }

        /// <summary>仅 Y 轴位移</summary>
        public static void AniTranslateY(FrameworkElement target, double toY, int ms = 250, int delay = 0, bool easeBack = false)
        {
            if (ControlEnabled > 0)
            {
                EnsureTranslateTransform(target).Y = toY;
                return;
            }

            var transform = EnsureTranslateTransform(target);
            var ani = new DoubleAnimation
            {
                To = toY,
                Duration = TimeSpan.FromMilliseconds(ms),
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };
            ani.EasingFunction = easeBack
                ? (IEasingFunction)new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                : new QuadraticEase { EasingMode = EasingMode.EaseOut };
            transform.BeginAnimation(TranslateTransform.YProperty, ani);
        }

        #endregion

        #region Double / Thickness Helpers

        /// <summary>对任意 DependencyProperty 做 Double 动画</summary>
        public static void AniDouble(FrameworkElement target, DependencyProperty dp, double toValue, int ms = 200, int delay = 0)
        {
            if (ControlEnabled > 0)
            {
                target.SetValue(dp, toValue);
                return;
            }
            var ani = new DoubleAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(ms),
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };
            ani.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            target.BeginAnimation(dp, ani);
        }

        /// <summary>Margin 厚度动画（用于滑块位置）</summary>
        public static void AniThickness(FrameworkElement target, Thickness toValue, int ms = 200, int delay = 0)
        {
            if (ControlEnabled > 0)
            {
                target.Margin = toValue;
                return;
            }
            var ani = new ThicknessAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(ms),
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };
            ani.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            target.BeginAnimation(FrameworkElement.MarginProperty, ani);
        }

        #endregion

        #region Helpers

        private static ScaleTransform EnsureScaleTransform(FrameworkElement target)
        {
            ScaleTransform st = target.RenderTransform as ScaleTransform;
            if (st != null)
                return st;

            var group = target.RenderTransform as TransformGroup;
            if (group != null)
            {
                foreach (var t in group.Children)
                {
                    ScaleTransform s = t as ScaleTransform;
                    if (s != null) return s;
                }
            }

            var newTransform = new ScaleTransform(1, 1);
            target.RenderTransformOrigin = new Point(0.5, 0.5);

            if (target.RenderTransform == null || target.RenderTransform == Transform.Identity)
            {
                target.RenderTransform = newTransform;
            }
            else
            {
                TransformGroup tg = target.RenderTransform as TransformGroup;
                if (tg != null)
                {
                    tg.Children.Add(newTransform);
                }
                else
                {
                    var newGroup = new TransformGroup();
                    newGroup.Children.Add(target.RenderTransform);
                    newGroup.Children.Add(newTransform);
                    target.RenderTransform = newGroup;
                }
            }

            return newTransform;
        }

        private static TranslateTransform EnsureTranslateTransform(FrameworkElement target)
        {
            TranslateTransform tt = target.RenderTransform as TranslateTransform;
            if (tt != null)
                return tt;

            var group = target.RenderTransform as TransformGroup;
            if (group != null)
            {
                foreach (var t in group.Children)
                {
                    TranslateTransform tr = t as TranslateTransform;
                    if (tr != null) return tr;
                }
            }

            var newTransform = new TranslateTransform(0, 0);

            if (target.RenderTransform == null || target.RenderTransform == Transform.Identity)
            {
                target.RenderTransform = newTransform;
            }
            else
            {
                TransformGroup tg = target.RenderTransform as TransformGroup;
                if (tg != null)
                {
                    tg.Children.Add(newTransform);
                }
                else
                {
                    var newGroup = new TransformGroup();
                    newGroup.Children.Add(target.RenderTransform);
                    newGroup.Children.Add(newTransform);
                    target.RenderTransform = newGroup;
                }
            }

            return newTransform;
        }

        private static DependencyProperty GetDependencyProperty(DependencyObject obj, string propertyName)
        {
            // 优先在 obj 的实际类型上查找（兼容 Control、Border、TextBlock 等子类）
            var type = obj.GetType();
            var field = type.GetField(propertyName + "Property",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.FlattenHierarchy);
            if (field != null)
                return field.GetValue(null) as DependencyProperty;

            // 后备映射
            switch (propertyName)
            {
                case "Background":
                    return System.Windows.Controls.Control.BackgroundProperty;
                case "BorderBrush":
                    return System.Windows.Controls.Control.BorderBrushProperty;
                case "Foreground":
                    return System.Windows.Controls.Control.ForegroundProperty;
                default:
                    return null;
            }
        }

        private static void SetSolidColorBrush(DependencyObject obj, string propertyName, Color color)
        {
            var dp = GetDependencyProperty(obj, propertyName);
            if (dp != null)
                obj.SetValue(dp, new SolidColorBrush(color));
        }

        private static System.Collections.Generic.IEnumerable<DependencyObject> GetLogicalChildren(DependencyObject parent)
        {
            var children = LogicalTreeHelper.GetChildren(parent);
            foreach (var child in children)
            {
                DependencyObject d = child as DependencyObject;
                if (d != null)
                    yield return d;
            }
        }

        /// <summary>递归查找可动画的子控件（仿 PCL _GetAllAnimControls）
        /// 只收集叶子级元素（Border/TextBlock/Image/MyButton 等），容器类型会深入查找 </summary>
        private static System.Collections.Generic.IEnumerable<DependencyObject> GetAnimControls(DependencyObject parent)
        {
            // 判断是否为容器类型（需要深入查找）
            bool isContainer = parent is Panel || parent is System.Windows.Controls.ContentControl
                || parent is System.Windows.Controls.Decorator
                || parent is System.Windows.Controls.Border;

            var children = LogicalTreeHelper.GetChildren(parent);
            foreach (var child in children)
            {
                DependencyObject d = child as DependencyObject;
                if (d == null) continue;

                // 如果子元素本身是容器，递归
                bool childIsContainer = d is Panel || d is System.Windows.Controls.ContentControl
                    || d is System.Windows.Controls.Decorator
                    || d is System.Windows.Controls.Border;

                if (childIsContainer)
                {
                    // 容器本身也可能是可见元素（如 Border 带背景），也加入
                    yield return d;
                    foreach (var sub in GetAnimControls(d))
                        yield return sub;
                }
                else
                {
                    yield return d;
                }
            }
        }

        /// <summary>获取容器的直接子元素（不递归深入），用于按行入场动画
        /// 会跳过 ScrollViewer 取其内容，跳过透明/不可见元素 </summary>
        private static System.Collections.Generic.IEnumerable<DependencyObject> GetTopLevelChildren(DependencyObject container)
        {
            // 如果是 ScrollViewer，取其内容
            var scrollViewer = container as System.Windows.Controls.ScrollViewer;
            if (scrollViewer != null)
            {
                var content = scrollViewer.Content as DependencyObject;
                if (content != null)
                    container = content;
            }

            // 如果是 Border，取其 Child
            var border = container as System.Windows.Controls.Border;
            if (border != null && border.Child != null)
            {
                container = border.Child;
            }

            // 如果是 ContentControl，取其 Content
            var cc = container as System.Windows.Controls.ContentControl;
            if (cc != null && cc.Content is DependencyObject)
            {
                container = cc.Content as DependencyObject;
            }

            // 现在 container 应该是 Panel（StackPanel/Grid/WrapPanel 等），遍历直接子元素
            var children = LogicalTreeHelper.GetChildren(container);
            foreach (var child in children)
            {
                var fe = child as FrameworkElement;
                if (fe == null) continue;

                // 跳过不可见元素
                if (fe.Visibility == Visibility.Collapsed) continue;

                yield return fe;
            }
        }

        #endregion
    }
}
