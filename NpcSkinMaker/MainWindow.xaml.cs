using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        // 自定义 SVG 图标：人物剪影（皮肤拓展）
        public static readonly string IconSkin = "F1 M7,0 C5.34,0 4,1.34 4,3 C4,4.66 5.34,6 7,6 C8.66,6 10,4.66 10,3 C10,1.34 8.66,0 7,0 Z M2,8 L2,15 12,15 12,8 C9.5,6.5 4.5,6.5 2,8 Z";
        // 自定义 SVG 图标：方块轮廓（模型拓展）
        public static readonly string IconModel = "F1 M9,0 L17,4 L17,12 L9,16 L1,12 L1,4 Z M9,0 L9,16 M1,4 L9,12 L17,4";

        private readonly List<NavButton> _modeButtons = new List<NavButton>();
        private readonly List<TitleBarNavButton> _titleNavButtons = new List<TitleBarNavButton>();
        private readonly List<NavButton> _sidebarNavButtons = new List<NavButton>();
        private int _currentPageIndex = -1;
        private int _currentTool = 0; // 0=皮肤, 1=模型
        private bool _isTransitioning = false; // 防止快速点击导致的动画叠加

        // 页面缓存（避免每次切换都重新创建）
        private PageHome _pageHome;
        private PageModelHome _pageModelHome;
        private PageMcTools _pageMcTools;
        private Page3DText _page3DText;
        private PageSettings _pageSettings;
        private PageAbout _pageAbout;

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

            // 初始化功能切换栏（左侧模式按钮）
            InitializeModeButtons();

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

        /// <summary>
        /// 初始化左侧栏模式切换按钮（皮肤拓展制作 / 模型拓展制作）
        /// </summary>
        private void InitializeModeButtons()
        {
            AddModeButton(_settings.TabSkin, IconSkin, 0);
            AddModeButton(_settings.TabModel, IconModel, 1);
        }

        private void AddModeButton(string title, string icon, int tool)
        {
            var btn = new NavButton
            {
                Title = title,
                IconData = icon,
                Index = tool
            };
            btn.Selected += (i) => SwitchTool(i);
            btn.IsSelected = tool == 0; // 默认选中皮肤
            _modeButtons.Add(btn);
            PanLeftModes.Children.Add(btn);
        }

        private void SwitchTool(int tool)
        {
            if (tool == _currentTool) return;

            _currentTool = tool;

            _pageHome = null;
            _pageModelHome = null;

            foreach (var btn in _modeButtons)
                btn.IsSelected = btn.Index == tool;

            _currentPageIndex = -1;
            NavigateToPage(0);
        }

        private void InitializeNavigation()
        {
            // 标题栏导航按钮（首页 / 设置 / 关于）
            AddTitleNavButton(_settings.NavHome, 0);
            AddTitleNavButton(_settings.NavSettings, 3);
            AddTitleNavButton(_settings.NavAbout, 4);

            // 左侧栏导航按钮（3D 文字 / 开发者工具箱 / MCStudio配置）
            AddSidebarNavButton(_settings.Nav3DText, MyIconButton.IconCreeper, 1);
            AddSidebarNavButton(_settings.NavDevTools, MyIconButton.IconSettings, 2);
            AddSidebarNavButton("MCStudio项目配置管理", MyIconButton.IconSettings, 5);
            AddSidebarNavButton("存档全局配置", MyIconButton.IconSettings, 6);
        }

        private void AddTitleNavButton(string title, int index)
        {
            var btn = new TitleBarNavButton
            {
                Title = title,
                Index = index
            };
            btn.Selected += (i) => NavigateToPage(i);
            _titleNavButtons.Add(btn);
            PanTitleNav.Children.Add(btn);
        }

        private void AddSidebarNavButton(string title, string icon, int index)
        {
            var btn = new NavButton
            {
                Title = title,
                IconData = icon,
                Index = index
            };
            btn.Margin = new Thickness(0, 0, 0, 6);
            btn.Selected += (i) => NavigateToPage(i);
            _sidebarNavButtons.Add(btn);
            PanLeftItems.Children.Add(btn);
        }

        public void NavigateToPage(int index)
        {
            if (index == _currentPageIndex) return;

            // 更新标题栏按钮选中态
            foreach (var btn in _titleNavButtons)
                btn.IsSelected = btn.Index == index;

            // 更新侧栏导航按钮选中态
            foreach (var btn in _sidebarNavButtons)
                btn.IsSelected = btn.Index == index;

            // 滑动指示条动画 — 跟踪侧栏所有按钮（模式按钮 + 导航按钮）
            FrameworkElement indicatorTarget = null;

            // 优先匹配侧栏导航按钮（3D文字 / 开发者工具箱）
            var sidebarBtn = _sidebarNavButtons.Find(b => b.Index == index);
            if (sidebarBtn != null)
                indicatorTarget = sidebarBtn;

            // 首页 (index=0) 没有侧栏导航按钮，则跟踪当前激活的模式按钮（皮肤 / 模型）
            if (indicatorTarget == null && index == 0)
            {
                var modeBtn = _modeButtons.Find(b => b.Index == _currentTool);
                if (modeBtn != null)
                    indicatorTarget = modeBtn;
            }

            if (indicatorTarget != null)
            {
                NavIndicator.Opacity = 1;
                indicatorTarget.UpdateLayout();
                var transform = indicatorTarget.TransformToAncestor(PanMainLeft);
                var pos = transform.Transform(new System.Windows.Point(0, 0));
                double targetY = pos.Y;
                double targetH = indicatorTarget.ActualHeight;

                AniHelper.AniThickness(NavIndicator,
                    new Thickness(5, targetY + (targetH - 28) / 2, 0, 0), 100, 0);
            }
            else
            {
                // 当前页面不在侧栏中（标题栏页面），隐藏指示条
                NavIndicator.Opacity = 0;
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
                case 5: newPage = new PageMcStudioConfig(); break;
                case 6: newPage = new PageSaveConfig(); break;
                case 7: newPage = new PageTestSaves(); break;
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

            // 应用保存的字体
            ApplySavedFont();

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
                foreach (var btn in _modeButtons)
                    btn.IsSelected = btn.Index == _currentTool;

                // 左侧栏滑出动画（仿 PCL MyPageLeft）：模式按钮 + 导航按钮分别动画
                AniHelper.LeftPanelEnterAnimation(PanLeftModes.Children, baseDelay: 200);
                AniHelper.LeftPanelEnterAnimation(PanLeftItems.Children, baseDelay: 300);

                // 首页在标题栏，侧栏无对应按钮，初始隐藏指示条
                NavIndicator.Opacity = 0;
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

        /// <summary>
        /// 从设置加载字体并应用到全局资源
        /// </summary>
        private void ApplySavedFont()
        {
            string fontName = _settings.FontFamilyName;
            if (string.IsNullOrEmpty(fontName))
                return;

            try
            {
                System.Windows.Media.FontFamily font;
                if (System.IO.File.Exists(fontName))
                {
                    var families = System.Windows.Media.Fonts.GetFontFamilies(fontName);
                    font = families.Count > 0 ? families.First() : new System.Windows.Media.FontFamily("Microsoft YaHei UI");
                }
                else
                {
                    font = new System.Windows.Media.FontFamily(fontName);
                }
                Application.Current.Resources["FontDefault"] = font;
            }
            catch
            {
                // 字体加载失败，保持默认
            }
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
    /// 标题栏导航按钮 — 暗色主题，用于标题栏中的页面导航
    /// </summary>
    public class TitleBarNavButton : Border
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

        public TitleBarNavButton()
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
            MouseLeftButtonUp += delegate (object s, MouseButtonEventArgs e)
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
