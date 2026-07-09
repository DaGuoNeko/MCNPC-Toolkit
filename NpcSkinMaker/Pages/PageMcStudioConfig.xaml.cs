using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ookii.Dialogs.Wpf;

namespace NpcSkinMaker
{
    /// <summary>
    /// MCStudio 项目配置管理 — 扫描 MCStudio 下载目录中的项目，支持搜索和修改工作目录
    /// </summary>
    public partial class PageMcStudioConfig : UserControl
    {
        private string _downloadRoot;
        private List<McProjectInfo> _allProjects = new List<McProjectInfo>();
        private bool _initializing = true;

        public PageMcStudioConfig()
        {
            InitializeComponent();

            // 根路径只读
            TxtRootPath.InnerBox.IsReadOnly = true;

            // 禁用自动滚动（避免点击按钮时页面跳动）
            ScrollProjects.RequestBringIntoView += (s, e) => { e.Handled = true; };

            // 账号切换时自动加载项目（必须在 DetectMcStudioConfig 之前绑定）
            CmbAccount.SelectionChanged += (_, _) => LoadProjects();

            // 刷新按钮
            BtnRefresh.Click += (_, _) =>
            {
                DetectMcStudioConfig();
                if (CmbAccount.SelectedItem != null)
                    LoadProjects();
            };

            // 搜索框实时过滤
            TxtSearch.InnerBox.TextChanged += (_, _) => FilterProjects();

            // 打开项目配置文件夹
            BtnOpenConfigDir.Click += (_, _) =>
            {
                string account = CmbAccount.SelectedItem as string;
                if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(_downloadRoot))
                {
                    MyMsgBox.Show("请先选择 MCStudio 账号", "提示", MyMsgBox.MsgType.Warning);
                    return;
                }
                string addonDir = Path.Combine(_downloadRoot, "work", account, "Cpp", "AddOn");
                if (Directory.Exists(addonDir))
                    System.Diagnostics.Process.Start("explorer.exe", addonDir);
                else
                    MyMsgBox.Show("项目目录不存在: " + addonDir, "提示", MyMsgBox.MsgType.Warning);
            };

            _initializing = false;

            // 延迟加载数据（让页面动画先播放，避免卡顿）
            Loaded += (_, _) =>
            {
                Dispatcher.BeginInvoke(new Action(DetectMcStudioConfig),
                    System.Windows.Threading.DispatcherPriority.Background);
            };
        }

        #region 配置检测

        /// <summary>
        /// 从 app.conf 读取 MCStudio 下载根目录，并扫描账号列表
        /// </summary>
        private void DetectMcStudioConfig()
        {
            try
            {
                string confPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Netease", "MCStudio", "config", "app", "app.conf");

                if (!File.Exists(confPath))
                {
                    TxtRootPath.Text = "未找到配置文件";
                    return;
                }

                string json = File.ReadAllText(confPath, System.Text.Encoding.UTF8);
                var config = JObject.Parse(json);

                string editorPath = config["X64EditorPath"]?.ToString();
                if (string.IsNullOrEmpty(editorPath))
                {
                    TxtRootPath.Text = "配置中无 X64EditorPath 字段";
                    return;
                }

                // 反推下载根目录：去掉 \MCX64Editor 及之后的部分
                int idx = editorPath.IndexOf("\\MCX64Editor", StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                {
                    TxtRootPath.Text = "无法解析下载根目录";
                    return;
                }

                _downloadRoot = editorPath.Substring(0, idx);
                TxtRootPath.Text = _downloadRoot;

                // 扫描账号
                ScanAccounts();
            }
            catch (Exception ex)
            {
                TxtRootPath.Text = "检测失败: " + ex.Message;
            }
        }

        /// <summary>
        /// 扫描 <downloadRoot>\work\ 下的子文件夹作为账号列表
        /// </summary>
        private void ScanAccounts()
        {
            CmbAccount.Items.Clear();

            if (string.IsNullOrEmpty(_downloadRoot))
                return;

            string workDir = Path.Combine(_downloadRoot, "work");
            if (!Directory.Exists(workDir))
                return;

            string savedAccount = CmbAccount.Text;
            foreach (string dir in Directory.GetDirectories(workDir))
            {
                string name = Path.GetFileName(dir);
                // 跳过非账号目录（如临时目录）
                if (name.StartsWith(".") || name.Equals("temp", StringComparison.OrdinalIgnoreCase))
                    continue;

                string addonDir = Path.Combine(dir, "Cpp", "AddOn");
                if (Directory.Exists(addonDir))
                    CmbAccount.Items.Add(name);
            }

            // 恢复之前选中的账号
            if (!string.IsNullOrEmpty(savedAccount) && CmbAccount.Items.Contains(savedAccount))
                CmbAccount.SelectedItem = savedAccount;
            else if (CmbAccount.Items.Count > 0)
                CmbAccount.SelectedIndex = 0;
        }

        #endregion

        #region 项目加载

        /// <summary>
        /// 加载选中账号下的所有项目
        /// </summary>
        private void LoadProjects()
        {
            _allProjects.Clear();
            PanProjectList.Children.Clear();

            string account = CmbAccount.SelectedItem as string;
            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(_downloadRoot))
            {
                LabEmpty.Visibility = Visibility.Visible;
                LabCount.Text = "共 0 个项目";
                return;
            }

            string addonDir = Path.Combine(_downloadRoot, "work", account, "Cpp", "AddOn");
            if (!Directory.Exists(addonDir))
            {
                LabEmpty.Text = "未找到 AddOn 目录";
                LabEmpty.Visibility = Visibility.Visible;
                LabCount.Text = "共 0 个项目";
                return;
            }

            foreach (string projectDir in Directory.GetDirectories(addonDir))
            {
                try
                {
                    string configPath = Path.Combine(projectDir, "work.mcscfg");
                    if (!File.Exists(configPath))
                        continue;

                    string json = File.ReadAllText(configPath, System.Text.Encoding.UTF8);
                    var cfg = JObject.Parse(json);

                    var project = new McProjectInfo
                    {
                        FolderName = Path.GetFileName(projectDir),
                        ProjectDir = projectDir,
                        Name = cfg["Name"]?.ToString() ?? Path.GetFileName(projectDir),
                        CustomWorkDir = cfg["CustomWorkDir"]?.ToString() ?? "",
                        ConfigPath = configPath
                    };

                    _allProjects.Add(project);
                }
                catch
                {
                    // 跳过解析失败的项目
                }
            }

            LabEmpty.Visibility = _allProjects.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            FilterProjects();
        }

        /// <summary>
        /// 根据搜索框过滤项目列表
        /// </summary>
        private void FilterProjects()
        {
            PanProjectList.Children.Clear();

            string keyword = (TxtSearch.GetText() ?? "").Trim().ToLower();
            var filtered = string.IsNullOrEmpty(keyword)
                ? _allProjects
                : _allProjects.Where(p =>
                    p.Name.ToLower().Contains(keyword) ||
                    p.FolderName.ToLower().Contains(keyword)).ToList();

            LabCount.Text = string.Format("共 {0} 个项目", filtered.Count);

            foreach (var project in filtered)
            {
                PanProjectList.Children.Add(CreateProjectRow(project));
            }
        }

        #endregion

        #region 项目行 UI

        /// <summary>
        /// 创建单行项目 UI：名称 + 路径 + 查看 + 更改路径 + 配置目录
        /// </summary>
        private Border CreateProjectRow(McProjectInfo project)
        {
            var row = new Border
            {
                CornerRadius = new CornerRadius(5),
                Background = Brushes.Transparent,
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 4)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130, GridUnitType.Auto) });

            // 项目名称
            var nameBlock = new TextBlock
            {
                Text = project.Name,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrush1"),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameBlock, 0);
            grid.Children.Add(nameBlock);

            // 当前路径
            string displayPath = string.IsNullOrEmpty(project.CustomWorkDir)
                ? "(默认路径)" : project.CustomWorkDir;
            var pathBlock = new TextBlock
            {
                Text = displayPath,
                FontSize = 12,
                Foreground = string.IsNullOrEmpty(project.CustomWorkDir)
                    ? (Brush)Application.Current.TryFindResource("ColorBrushGray3")
                    : (Brush)Application.Current.TryFindResource("ColorBrushGray2"),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(pathBlock, 1);
            grid.Children.Add(pathBlock);

            // 查看按钮 — 用文件管理器打开当前路径
            var viewBtn = new MyButton
            {
                Text = "查看",
                Padding = new Thickness(8, 3, 8, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            viewBtn.Click += (_, _) =>
            {
                string openPath = string.IsNullOrEmpty(project.CustomWorkDir)
                    ? project.ProjectDir : project.CustomWorkDir;
                if (Directory.Exists(openPath))
                    System.Diagnostics.Process.Start("explorer.exe", openPath);
                else
                    MyMsgBox.Show("路径不存在: " + openPath, "提示", MyMsgBox.MsgType.Warning);
            };
            Grid.SetColumn(viewBtn, 2);
            grid.Children.Add(viewBtn);

            // 更改路径按钮
            var editBtn = new MyButton
            {
                Text = "更改项目源代码路径",
                Padding = new Thickness(12, 3, 12, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            editBtn.Click += (_, _) => EditProjectPath(project, pathBlock);
            Grid.SetColumn(editBtn, 3);
            grid.Children.Add(editBtn);

            // 查看配置目录按钮
            var dirBtn = new MyButton
            {
                Text = "查看配置目录",
                Padding = new Thickness(12, 3, 12, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            dirBtn.Click += (_, _) =>
            {
                if (Directory.Exists(project.ProjectDir))
                    System.Diagnostics.Process.Start("explorer.exe", project.ProjectDir);
                else
                    MyMsgBox.Show("目录不存在: " + project.ProjectDir, "提示", MyMsgBox.MsgType.Warning);
            };
            Grid.SetColumn(dirBtn, 4);
            grid.Children.Add(dirBtn);

            // 查看测试存档列表
            var testSaveBtn = new MyButton
            {
                Text = "查看测试存档列表",
                Padding = new Thickness(12, 3, 12, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            testSaveBtn.Click += (_, _) =>
            {
                PageTestSaves.CurrentContext = project;
                MainWindow.Instance.NavigateToPage(7);
            };
            Grid.SetColumn(testSaveBtn, 5);
            grid.Children.Add(testSaveBtn);

            row.Child = grid;
            return row;
        }

        #endregion

        #region 编辑项目路径

        /// <summary>
        /// 弹出文件夹选择器，修改 CustomWorkDir 并保存
        /// </summary>
        private void EditProjectPath(McProjectInfo project, TextBlock pathBlock)
        {
            var dlg = new VistaFolderBrowserDialog
            {
                Description = "选择项目 " + project.Name + " 的新工作目录",
                SelectedPath = string.IsNullOrEmpty(project.CustomWorkDir)
                    ? project.ProjectDir : project.CustomWorkDir,
                ShowNewFolderButton = true
            };

            if (dlg.ShowDialog() == true)
            {
                string newPath = dlg.SelectedPath;
                try
                {
                    // 读取、修改、保存 work.mcscfg
                    string json = File.ReadAllText(project.ConfigPath, System.Text.Encoding.UTF8);
                    var cfg = JObject.Parse(json);
                    cfg["CustomWorkDir"] = newPath;
                    File.WriteAllText(project.ConfigPath, cfg.ToString(Formatting.Indented), System.Text.Encoding.UTF8);

                    // 更新内存和 UI
                    project.CustomWorkDir = newPath;
                    pathBlock.Text = newPath;
                    pathBlock.Foreground = (Brush)Application.Current.TryFindResource("ColorBrushGray2");

                    MyMsgBox.Show("项目路径已更新!", "成功", MyMsgBox.MsgType.Info);
                }
                catch (Exception ex)
                {
                    MyMsgBox.Show("保存失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// MCStudio 项目信息
    /// </summary>
    public class McProjectInfo
    {
        public string FolderName { get; set; }      // AddOn 下的文件夹名
        public string ProjectDir { get; set; }       // 项目完整路径
        public string Name { get; set; }             // work.mcscfg 中的 Name
        public string CustomWorkDir { get; set; }    // work.mcscfg 中的 CustomWorkDir
        public string ConfigPath { get; set; }       // work.mcscfg 完整路径
    }
}
