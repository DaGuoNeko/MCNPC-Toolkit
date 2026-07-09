using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace NpcSkinMaker
{
    /// <summary>
    /// 网易互通版存档全局配置 — 浏览玩家 config 文件
    /// </summary>
    public partial class PageSaveConfig : UserControl
    {
        private string _gamePath;
        private List<FileInfo> _allFiles = new List<FileInfo>();

        public PageSaveConfig()
        {
            InitializeComponent();

            TxtFeverGamePath.InnerBox.IsReadOnly = true;

            // 禁用自动滚动到焦点元素（避免点击按钮时页面跳动）
            ScrollFiles.RequestBringIntoView += (s, e) => { e.Handled = true; };

            // 恢复上次保存的路径（避免空白闪烁）
            var s = MainWindow.Instance.Settings;
            if (!string.IsNullOrEmpty(s.FeverGamePath))
                TxtFeverGamePath.Text = s.FeverGamePath;

            // 端选择：正式端 / 测试端
            CmbChannel.Items.Add("正式端");
            CmbChannel.Items.Add("测试端");
            CmbChannel.SelectedIndex = (s.FeverChannel == "测试端") ? 1 : 0;
            CmbChannel.SelectionChanged += (_, _) =>
            {
                s.FeverChannel = CmbChannel.SelectedIndex == 1 ? "测试端" : "正式端";
                s.Save();
                DetectGamePath();
            };

            // 事件必须在 DetectGamePath 之前绑定
            CmbPlayerId.SelectionChanged += (_, _) =>
            {
                LoadFiles();
                // 持久化选中玩家ID
                var sid = CmbPlayerId.SelectedItem as string;
                if (!string.IsNullOrEmpty(sid))
                {
                    MainWindow.Instance.Settings.FeverPlayerId = sid;
                    MainWindow.Instance.Settings.Save();
                }
            };
            BtnRefresh.Click += (_, _) => DetectGamePath();
            TxtSearch.InnerBox.TextChanged += (_, _) => FilterFiles();

            // 延迟加载数据（让页面动画先播放，避免卡顿）
            Loaded += (_, _) =>
            {
                Dispatcher.BeginInvoke(new Action(DetectGamePath),
                    System.Windows.Threading.DispatcherPriority.Background);
            };
        }

        /// <summary>
        /// 从注册表读取 FeverGames 安装路径，扫描 %AppData% 下的玩家列表
        /// </summary>
        private void DetectGamePath()
        {
            try
            {
                string installPath = null;
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\FeverGames\FeverGamesInstaller\game\1"))
                {
                    if (key != null)
                        installPath = key.GetValue("InstallPath") as string;
                }

                if (string.IsNullOrEmpty(installPath))
                {
                    TxtFeverGamePath.Text = "未找到 FeverGames 安装路径";
                    return;
                }

                _gamePath = installPath;
                TxtFeverGamePath.Text = installPath;

                // 持久化游戏路径
                var s = MainWindow.Instance.Settings;
                s.FeverGamePath = installPath;
                s.Save();

                // 根据端选择不同数据目录
                string channel = MainWindow.Instance.Settings.FeverChannel;
                string mcFolder = (channel == "测试端") ? "MinecraftPE_Netease" : "MinecraftPC_Netease_PB";

                // 玩家存档在 %AppData%\<mcFolder>\storge\stream\users
                string usersDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    mcFolder, "storge", "stream", "users");

                if (!Directory.Exists(usersDir))
                {
                    TxtFeverGamePath.Text = installPath + " (未找到 users 目录)";
                    CmbPlayerId.Items.Clear();
                    return;
                }

                // 优先恢复上次保存的玩家 ID，其次保留当前选中，最后选第一个
                string savedId = MainWindow.Instance.Settings.FeverPlayerId;
                string previousId = CmbPlayerId.SelectedItem as string;

                CmbPlayerId.Items.Clear();
                foreach (string dir in Directory.GetDirectories(usersDir))
                {
                    string pid = Path.GetFileName(dir);
                    if (Directory.Exists(Path.Combine(dir, "config")))
                        CmbPlayerId.Items.Add(pid);
                }

                if (!string.IsNullOrEmpty(savedId) && CmbPlayerId.Items.Contains(savedId))
                    CmbPlayerId.SelectedItem = savedId;
                else if (!string.IsNullOrEmpty(previousId) && CmbPlayerId.Items.Contains(previousId))
                    CmbPlayerId.SelectedItem = previousId;
                else if (CmbPlayerId.Items.Count > 0)
                    CmbPlayerId.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                TxtFeverGamePath.Text = "检测失败: " + ex.Message;
            }
        }

        /// <summary>
        /// 加载选中玩家 config 目录的所有文件，按修改时间降序
        /// </summary>
        private void LoadFiles()
        {
            _allFiles.Clear();
            PanFileList.Children.Clear();

            string pid = CmbPlayerId.SelectedItem as string;
            if (string.IsNullOrEmpty(pid))
            {
                LabEmpty.Visibility = Visibility.Visible;
                LabCount.Text = "共 0 个文件";
                return;
            }

            // 根据端选择不同数据目录
            string channel = MainWindow.Instance.Settings.FeverChannel;
            string mcFolder = (channel == "测试端") ? "MinecraftPE_Netease" : "MinecraftPC_Netease_PB";

            string configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                mcFolder, "storge", "stream", "users", pid, "config");

            if (!Directory.Exists(configDir))
            {
                LabEmpty.Text = "未找到 config 目录";
                LabEmpty.Visibility = Visibility.Visible;
                LabCount.Text = "共 0 个文件";
                return;
            }

            foreach (var file in new DirectoryInfo(configDir).GetFiles())
                _allFiles.Add(file);

            _allFiles = _allFiles.OrderByDescending(f => f.LastWriteTime).ToList();

            LabEmpty.Visibility = _allFiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            FilterFiles();
        }

        /// <summary>
        /// 搜索过滤
        /// </summary>
        private void FilterFiles()
        {
            PanFileList.Children.Clear();

            string kw = (TxtSearch.GetText() ?? "").Trim().ToLower();
            var list = string.IsNullOrEmpty(kw)
                ? _allFiles
                : _allFiles.Where(f => f.Name.ToLower().Contains(kw)).ToList();

            LabCount.Text = string.Format("共 {0} 个文件", list.Count);

            foreach (var file in list)
                PanFileList.Children.Add(CreateRow(file));
        }

        /// <summary>
        /// 单行: 文件名 | 修改时间 | 大小 | 打开 | 资源管理器
        /// </summary>
        private Border CreateRow(FileInfo file)
        {
            var row = new Border
            {
                CornerRadius = new CornerRadius(5),
                Background = Brushes.Transparent,
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 2),
                Cursor = Cursors.Hand
            };
            row.MouseEnter += (s, e) =>
                row.Background = (Brush)Application.Current.TryFindResource("ColorBrushGray7");
            row.MouseLeave += (s, e) =>
                row.Background = Brushes.Transparent;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120, GridUnitType.Auto) });

            var nameBlock = new TextBlock
            {
                Text = file.Name, FontSize = 13,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrush1"),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameBlock, 0); grid.Children.Add(nameBlock);

            var timeBlock = new TextBlock
            {
                Text = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"), FontSize = 12,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrushGray2"),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(timeBlock, 1); grid.Children.Add(timeBlock);

            string sizeText;
            if (file.Length < 1024) sizeText = file.Length + " B";
            else if (file.Length < 1048576) sizeText = (file.Length / 1024.0).ToString("F1") + " KB";
            else sizeText = (file.Length / 1048576.0).ToString("F2") + " MB";

            var sizeBlock = new TextBlock
            {
                Text = sizeText, FontSize = 12,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrushGray2"),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(sizeBlock, 2); grid.Children.Add(sizeBlock);

            var openBtn = new MyButton
            {
                Text = "打开", Padding = new Thickness(10, 3, 10, 3),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0)
            };
            openBtn.Click += (_, _) =>
            {
                try { Process.Start(file.FullName); }
                catch (Exception ex) { MyMsgBox.Show("打开失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error); }
            };
            Grid.SetColumn(openBtn, 3); grid.Children.Add(openBtn);

            // 在资源管理器显示
            var explorerBtn = new MyButton
            {
                Text = "在资源管理器显示",
                Padding = new Thickness(12, 3, 12, 3),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0)
            };
            explorerBtn.Click += (_, _) =>
            {
                try { Process.Start("explorer.exe", "/select,\"" + file.FullName + "\""); }
                catch (Exception ex) { MyMsgBox.Show("打开失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error); }
            };
            Grid.SetColumn(explorerBtn, 4); grid.Children.Add(explorerBtn);

            row.Child = grid;
            return row;
        }
    }
}
