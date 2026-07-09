using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NpcSkinMaker
{
    /// <summary>
    /// 主窗口 — 仿 PCL FormMain
    /// 无边框圆角窗口 + 缩放 + 左右分栏导航
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        public Border PanTitleEl { get { return PanTitle; } }
        public Border BorderFormEl { get { return BorderForm; } }

        private readonly List<NavButton> _navButtons = new List<NavButton>();
        private int _currentPageIndex = -1;
        private int _currentTool = 0;
        private bool _isTransitioning = false; // 防止快速点击导致的动画叠加

        // 页面缓存（避免每次切换都重新创建）
        private PageHome _pageHome;
        private PageModelHome _pageModelHome;
        private PageMcTools _pageMcTools;
        private Page3DText _page3DText;
        private PageSettings _pageSettings;
        private PageAbout _pageAbout; // 0=皮肤, 1=模型

        private readonly List<TabButton> _tabButtons = new List<TabButton>();

        private readonly SkinManager _skinManager = new SkinManager();
        private readonly ModelManager _modelManager = new ModelManager();
        private readonly AppSettings _settings;
        private PackageBuilder _packageBuilder;
        private ModelPackageBuilder _modelPackageBuilder;

        public SkinManager SkinManager { get { return _skinManager; } }
        public ModelManager ModelManager { get { return _modelManager; } }
        public AppSettings Settings { get { return _settings; } }
        public PackageBuilder PackageBuilder { get { return _packageBuilder; } }
        public ModelPackageBuilder ModelPackageBuilder { get { return _modelPackageBuilder; } }

        private bool _isMaximized = false;
        private double _savedLeft, _savedTop, _savedWidth, _savedHeight;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            _settings = AppSettings.Load();
            Title = _settings.WindowTitle;

            Loaded += MainWindow_Loaded;

            // 标题栏按钮事件 - 使用 Click 事件（MyIconButton 内部先触发 Click 再设 e.Handled=true）
            BtnMin.Click += (s, e) => { WindowState = WindowState.Minimized; };
            BtnMax.Click += (s, e) => ToggleMaximize();

            BtnClose.Click += (s, e) => { Close(); };

            // 标题栏拖拽移动 - 只在点击非按钮区域时触发
            PanTitle.MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            };

            // 初始化缩放（需要窗口句柄，所以延迟到 Loaded）
            Loaded += (s, e) =>
            {
                var resizer = new MyResizer(this, 900, 550);
                resizer.AddResizerLeft(ResizerL);
                resizer.AddResizerRight(ResizerR);
                resizer.AddResizerUp(ResizerT);
                resizer.AddResizerDown(ResizerB);
                resizer.AddResizerLeftUp(ResizerLT);
                resizer.AddResizerLeftDown(ResizerLB);
                resizer.AddResizerRightUp(ResizerRT);
                resizer.AddResizerRightDown(ResizerRB);
            };

            // 初始化模板
            InitializeTemplate();
            InitializeModelTemplate();

            // 初始化功能切换栏
            InitializeTopTabs();

            // 初始化导航
            InitializeNavigation();
        }

        private void InitializeTemplate()
        {
            // 从嵌入资源提取 template.zip 到临时目录
            string tempDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NPC_SkinMaker");
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            string templatePath = Path.Combine(tempDir, "template.zip");

            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = "NpcSkinMaker.Resources.template.zip";

                // 始终从嵌入资源覆盖提取（避免旧版残留）
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var fs = new FileStream(templatePath, FileMode.Create))
                        {
                            stream.CopyTo(fs);
                        }
                        Logger.Info("已提取模板到: " + templatePath + " (" + new FileInfo(templatePath).Length + " bytes)");
                    }
                    else
                    {
                        Logger.Error("嵌入资源未找到: " + resourceName);
                        // 列出所有资源名以便调试
                        foreach (var name in assembly.GetManifestResourceNames())
                            Logger.Error("  可用资源: " + name);
                    }
                }

                if (File.Exists(templatePath) && new FileInfo(templatePath).Length > 0)
                    _packageBuilder = new PackageBuilder(templatePath);
                else
                    Logger.Error("模板文件提取失败或为空");
            }
            catch (Exception e)
            {
                Logger.Error("初始化模板失败: " + e);
            }
        }

        private void InitializeModelTemplate()
        {
            string tempDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NPC_SkinMaker");
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            string templatePath = Path.Combine(tempDir, "template_models.zip");

            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = "NpcSkinMaker.Resources.template_models.zip";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var fs = new FileStream(templatePath, FileMode.Create))
                        {
                            stream.CopyTo(fs);
                        }
                        Logger.Info("[模型] 已提取模板到: " + templatePath + " (" + new FileInfo(templatePath).Length + " bytes)");
                    }
                    else
                    {
                        Logger.Error("[模型] 嵌入资源未找到: " + resourceName);
                    }
                }

                if (File.Exists(templatePath) && new FileInfo(templatePath).Length > 0)
                    _modelPackageBuilder = new ModelPackageBuilder(templatePath);
                else
                    Logger.Error("[模型] 模板文件提取失败或为空");
            }
            catch (Exception e)
            {
                Logger.Error("[模型] 初始化模板失败: " + e);
            }
        }

        private void AddTabButton(string title, int index)
        {
            var btn = new TabButton
            {
                Title = title,
                Index = index
            };
            btn.Selected += (i) => SwitchTool(i);
            _tabButtons.Add(btn);
            PanTopTabs.Children.Add(btn);
        }

        private void SwitchTool(int tool)
        {
            if (tool == _currentTool) return;

            _currentTool = tool;

            _pageHome = null;
            _pageModelHome = null;

            foreach (var btn in _tabButtons)
                btn.IsSelected = btn.Index == tool;

            _currentPageIndex = -1;
            NavigateToPage(0);
        }

        private void InitializeNavigation()
        {
            AddNavButton(_settings.NavHome, MyIconButton.IconHome, 0);
            AddNavButton(_settings.Nav3DText, MyIconButton.IconCreeper, 1);
            AddNavButton(_settings.NavDevTools, MyIconButton.IconSettings, 2);
            AddNavButton(_settings.NavSettings, MyIconButton.IconSettings, 3);
            AddNavButton(_settings.NavAbout, MyIconButton.IconInfo, 4);
        }

        private void InitializeTopTabs()
        {
            AddTabButton(_settings.TabSkin, 0);
            AddTabButton(_settings.TabModel, 1);
        }

        private void AddNavButton(string title, string icon, int index)
        {
            var btn = new NavButton
            {
                Title = title,
                IconData = icon,
                Index = index
            };
            btn.Margin = new Thickness(0, 0, 0, 6);
            btn.Selected += (i) => NavigateToPage(i);
            _navButtons.Add(btn);
            PanLeftItems.Children.Add(btn);
        }

        public void NavigateToPage(int index)
        {
            if (index == _currentPageIndex) return;

            foreach (var btn in _navButtons)
                btn.IsSelected = btn.Index == index;

            // 滑动指示条动画
            if (index >= 0 && index < _navButtons.Count)
            {
                var targetBtn = _navButtons[index];
                // 计算目标位置：按钮在 PanLeftItems 中的实际位置
                targetBtn.UpdateLayout();
                var transform = targetBtn.TransformToAncestor(PanMainLeft);
                var pos = transform.Transform(new System.Windows.Point(0, 0));
                double targetY = pos.Y;
                double targetH = targetBtn.ActualHeight;

                AniHelper.AniThickness(NavIndicator,
                    new Thickness(5, targetY + (targetH - 28) / 2, 0, 0), 100, 0);
            }

            FrameworkElement newPage = null;
            switch (index)
            {
                case 0:
                    newPage = _currentTool == 0 ? (FrameworkElement)new PageHome() : (FrameworkElement)new PageModelHome();
                    break;
                case 1:
                    if (_page3DText == null) _page3DText = new Page3DText();
                    newPage = _page3DText;
                    break;
                case 2: newPage = new PageMcTools(); break;
                case 3: newPage = new PageSettings(); break;
                case 4: newPage = new PageAbout(); break;
            }

            if (newPage != null)
            {
                PanRightContent.Children.Clear();
                PanRightContent.Children.Add(newPage);

                // 直接播动画
                newPage.Opacity = 1;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try { AniHelper.PageEnterAnimation(newPage); }
                    catch (Exception ex) { Logger.Error("页面动画异常: " + ex.Message); }
                }), DispatcherPriority.Render);
            }

            _currentPageIndex = index;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 更新圆角裁剪区域
            UpdateClipRegion();
            SizeChanged += (s, args) => UpdateClipRegion();

            // 加载自定义背景图
            LoadBackground();

            // 应用主题
            LabTitle.Text = _settings.WindowTitle;
            if (_settings.UseSystemAccent)
                ThemeManager.ApplySystemAccent();
            else
                ThemeManager.Apply(_settings.ThemeHue, _settings.ThemeSat);

            // 入场动画：旋转+位移归位
            var rotateAni = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500)
            };
            rotateAni.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
            TransformRotate.BeginAnimation(RotateTransform.AngleProperty, rotateAni);

            var posYAni = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500)
            };
            posYAni.EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 3 };
            TransformPos.BeginAnimation(TranslateTransform.YProperty, posYAni);

            // 默认导航到首页
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AniHelper.ControlEnabled++;
                NavigateToPage(0);
                AniHelper.ControlEnabled--;

                // 默认选中第一个 Tab
                foreach (var tab in _tabButtons)
                    tab.IsSelected = tab.Index == _currentTool;

                // 左侧导航栏滑出动画（仿 PCL MyPageLeft）
                AniHelper.LeftPanelEnterAnimation(PanLeftItems.Children, baseDelay: 200);

                // 首次定位指示条（无动画）
                if (_navButtons.Count > 0)
                {
                    var btn = _navButtons[0];
                    btn.UpdateLayout();
                    var transform = btn.TransformToAncestor(PanMainLeft);
                    var pos = transform.Transform(new System.Windows.Point(0, 0));
                    NavIndicator.Margin = new Thickness(5, pos.Y + (btn.ActualHeight - 28) / 2, 0, 0);
                }
            }), DispatcherPriority.Render);
        }

        private void UpdateClipRegion()
        {
            RectForm.Rect = new Rect(0, 0, BorderForm.ActualWidth, BorderForm.ActualHeight);
        }

        private void ToggleMaximize()
        {
            if (_isMaximized)
            {
                // 还原
                _isMaximized = false;
                PanMain.Margin = new Thickness(10);
                BorderForm.Margin = new Thickness(8);
                Left = _savedLeft;
                Top = _savedTop;
                Width = _savedWidth;
                Height = _savedHeight;
                BtnMax.Logo = MyIconButton.IconMaximize;
            }
            else
            {
                // 最大化
                _isMaximized = true;
                _savedLeft = Left;
                _savedTop = Top;
                _savedWidth = Width;
                _savedHeight = Height;

                var screen = System.Windows.Forms.Screen.FromHandle(
                    new System.Windows.Interop.WindowInteropHelper(this).Handle);
                var wa = screen.WorkingArea;

                var source = System.Windows.PresentationSource.FromVisual(this);
                double dpiX = source != null ? source.CompositionTarget.TransformToDevice.M11 : 1.0;
                double dpiY = source != null ? source.CompositionTarget.TransformToDevice.M22 : 1.0;

                PanMain.Margin = new Thickness(0);
                BorderForm.Margin = new Thickness(0);

                Left = wa.Left / dpiX;
                Top = wa.Top / dpiY;
                Width = wa.Width / dpiX;
                Height = wa.Height / dpiY;
                BtnMax.Logo = MyIconButton.IconRestore;
            }
        }

        private void LoadBackground()
        {
            string path = _settings.BgImagePath;
            SetBackground(path);
        }

        public void SetBackground(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                ImgBackground.Source = null;
                return;
            }
            try
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(path);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                ImgBackground.Source = img;
            }
            catch (Exception ex)
            {
                Logger.Error("加载背景图失败: " + ex.Message);
                ImgBackground.Source = null;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _settings.Save();
            base.OnClosed(e);
        }
    }

    /// <summary>
    /// 导航按钮 — 左侧栏
    /// </summary>
    public class NavButton : Border
    {
        public string Title { get; set; }
        public string IconData { get; set; }
        public int Index { get; set; }
        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; Refresh(); }
        }

        public event Action<int> Selected;

        private bool _isSelected;
        private bool _isHover;
        private TextBlock _label;
        private System.Windows.Shapes.Path _icon;

        public NavButton()
        {
            CornerRadius = new CornerRadius(5);
            Padding = new Thickness(14, 10, 14, 10);
            Cursor = Cursors.Hand;
            Margin = new Thickness(0, 0, 0, 6);

            var panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;

            _icon = new System.Windows.Shapes.Path();
            _icon.Width = 18;
            _icon.Height = 18;
            _icon.Stretch = Stretch.Uniform;
            _icon.Margin = new Thickness(0, 0, 10, 0);
            _icon.VerticalAlignment = VerticalAlignment.Center;
            panel.Children.Add(_icon);

            _label = new TextBlock();
            _label.FontSize = 14;
            _label.VerticalAlignment = VerticalAlignment.Center;
            _label.FontFamily = (FontFamily)Application.Current.TryFindResource("FontDefault");
            panel.Children.Add(_label);

            Child = panel;

            MouseEnter += (s, e) => { _isHover = true; Refresh(); };
            MouseLeave += (s, e) => { _isHover = false; Refresh(); };
            MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                if (Selected != null)
                    Selected(Index);
            };

            // 使用 Loaded 而不是 OnInitialized，确保属性已设置
            Loaded += NavButton_Loaded;
        }

        private void NavButton_Loaded(object sender, RoutedEventArgs e)
        {
            _label.Text = Title;
            try { _icon.Data = Geometry.Parse(IconData); } catch { }
            Refresh();
        }

        private void Refresh()
        {
            if (_isSelected)
            {
                Background = (Brush)Application.Current.TryFindResource("ColorBrush7");
                _icon.Fill = (Brush)Application.Current.TryFindResource("ColorBrush2");
                _label.Foreground = (Brush)Application.Current.TryFindResource("ColorBrush2");
                _label.FontWeight = FontWeights.Bold;
            }
            else if (_isHover)
            {
                Background = (Brush)Application.Current.TryFindResource("ColorBrushGray7");
                _icon.Fill = (Brush)Application.Current.TryFindResource("ColorBrush1");
                _label.Foreground = (Brush)Application.Current.TryFindResource("ColorBrush1");
                _label.FontWeight = FontWeights.Normal;
            }
            else
            {
                Background = Brushes.Transparent;
                _icon.Fill = (Brush)Application.Current.TryFindResource("ColorBrushGray2");
                _label.Foreground = (Brush)Application.Current.TryFindResource("ColorBrushGray2");
                _label.FontWeight = FontWeights.Normal;
            }
        }
    }

    /// <summary>
    /// 功能切换按钮 - 顶部 Tab 栏
    /// </summary>
    public class TabButton : Border
    {
        public string Title { get; set; }
        public int Index { get; set; }
        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; Refresh(); }
        }

        public event Action<int> Selected;

        private bool _isSelected;
        private bool _isHover;
        private TextBlock _label;

        public TabButton()
        {
            Padding = new Thickness(16, 6, 16, 6);
            Cursor = Cursors.Hand;
            Margin = new Thickness(0, 0, 4, 0);
            CornerRadius = new CornerRadius(4);

            _label = new TextBlock();
            _label.FontSize = 13;
            _label.VerticalAlignment = VerticalAlignment.Center;
            _label.FontFamily = (FontFamily)Application.Current.TryFindResource("FontDefault");
            Child = _label;

            MouseEnter += (s, e) => { _isHover = true; Refresh(); };
            MouseLeave += (s, e) => { _isHover = false; Refresh(); };
            // 阻止 MouseLeftButtonDown 冒泡到 PanTitle 的 DragMove
            MouseLeftButtonDown += (s, e) => { e.Handled = true; };
            MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                if (Selected != null)
                    Selected(Index);
            };

            Loaded += (s, e) =>
            {
                _label.Text = Title;
                Refresh();
            };
        }

        private void Refresh()
        {
            if (_isSelected)
            {
                // 选中：半透明白色背景 + 白色文字 + 加粗
                Background = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF));
                _label.Foreground = Brushes.White;
                _label.FontWeight = FontWeights.Bold;
            }
            else if (_isHover)
            {
                // 悬停：浅半透明白色背景
                Background = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF));
                _label.Foreground = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF));
                _label.FontWeight = FontWeights.Normal;
            }
            else
            {
                // 默认：透明背景 + 半透明白色文字
                Background = Brushes.Transparent;
                _label.Foreground = new SolidColorBrush(Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF));
                _label.FontWeight = FontWeights.Normal;
            }
        }
    }
}
