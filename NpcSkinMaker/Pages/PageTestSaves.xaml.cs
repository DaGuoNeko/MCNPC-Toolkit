using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json.Linq;

namespace NpcSkinMaker
{
    /// <summary>
    /// 测试存档列表 — 根据项目 UID 匹配 MC_GAME 配置，显示测试存档
    /// </summary>
    public partial class PageTestSaves : UserControl
    {
        /// <summary>导航上下文：由 McStudioConfig 页面设置</summary>
        public static McProjectInfo CurrentContext { get; set; }

        private List<TestSaveInfo> _allSaves = new List<TestSaveInfo>();

        public PageTestSaves()
        {
            InitializeComponent();

            // 返回按钮
            BtnBack.Click += (_, _) => MainWindow.Instance.NavigateToPage(5);

            if (CurrentContext == null)
            {
                LabTitle.Text = "未选择项目";
                return;
            }

            var ctx = CurrentContext;
            LabTitle.Text = "测试存档列表 - " + ctx.Name;
            LabProjectInfo.Text = string.Format("项目: {0}  |  UID: {1}", ctx.Name, "加载中...");

            // 延迟加载
            Loaded += (_, _) =>
            {
                Dispatcher.BeginInvoke(new Action(() => LoadTestSaves(ctx)),
                    System.Windows.Threading.DispatcherPriority.Background);
            };

            TxtSearch.InnerBox.TextChanged += (_, _) => FilterSaves();
        }

        /// <summary>
        /// 加载测试存档列表
        /// </summary>
        private void LoadTestSaves(McProjectInfo project)
        {
            _allSaves.Clear();
            PanSaveList.Children.Clear();

            try
            {
                // 读取项目的 work.mcscfg 获取 UID
                string uid = null;
                if (File.Exists(project.ConfigPath))
                {
                    var cfg = JObject.Parse(File.ReadAllText(project.ConfigPath));
                    uid = cfg["UID"]?.ToString();
                }

                if (string.IsNullOrEmpty(uid))
                {
                    LabEmpty.Text = "项目的 work.mcscfg 中未找到 UID 字段";
                    LabEmpty.Visibility = Visibility.Visible;
                    LabProjectInfo.Text = string.Format("项目: {0}  |  UID: (无)", project.Name);
                    return;
                }

                LabProjectInfo.Text = string.Format("项目: {0}  |  UID: {1}", project.Name, uid);

                // 从 app.conf 获取 MCStudio 下载根目录
                string downloadRoot = "";
                string confPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Netease", "MCStudio", "config", "app", "app.conf");
                if (File.Exists(confPath))
                {
                    var conf = JObject.Parse(File.ReadAllText(confPath));
                    string editorPath = conf["X64EditorPath"]?.ToString() ?? "";
                    int idx = editorPath.IndexOf("\\MCX64Editor", StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0) downloadRoot = editorPath.Substring(0, idx);
                }

                if (string.IsNullOrEmpty(downloadRoot))
                {
                    LabEmpty.Text = "未找到 MCStudio 下载根目录";
                    LabEmpty.Visibility = Visibility.Visible;
                    return;
                }

                // 从项目路径推断账号
                string account = "";
                int workIdx = project.ProjectDir.IndexOf("\\work\\", StringComparison.OrdinalIgnoreCase);
                if (workIdx >= 0)
                {
                    string afterWork = project.ProjectDir.Substring(workIdx + 6);
                    int nextSlash = afterWork.IndexOf('\\');
                    if (nextSlash > 0)
                        account = afterWork.Substring(0, nextSlash);
                }

                if (string.IsNullOrEmpty(account))
                {
                    LabEmpty.Text = "无法确定 MCStudio 账号";
                    LabEmpty.Visibility = Visibility.Visible;
                    return;
                }

                // 扫描 MC_GAME 目录
                string mcGameDir = Path.Combine(downloadRoot, "game", "config", account, "Cpp", "MC_GAME");
                if (!Directory.Exists(mcGameDir))
                {
                    LabEmpty.Text = "MC_GAME 目录不存在: " + mcGameDir;
                    LabEmpty.Visibility = Visibility.Visible;
                    return;
                }

                foreach (string filePath in Directory.GetFiles(mcGameDir, "*.*", SearchOption.AllDirectories))
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        var data = JObject.Parse(json);

                        string mainComponentId = data["MainComponentId"]?.ToString() ?? "";
                        if (mainComponentId != uid) continue;

                        string levelId = data["world_info"]?["level_id"]?.ToString() ?? "";
                        string version = data["version"]?.ToString() ?? "";

                        // 获取存档名
                        string saveName = levelId;
                        string worldsDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "MinecraftPE_Netease", "minecraftWorlds", levelId);
                        string nameFile = Path.Combine(worldsDir, "levelname.txt");
                        if (File.Exists(nameFile))
                        {
                            string name = File.ReadAllText(nameFile).Trim();
                            if (!string.IsNullOrEmpty(name))
                                saveName = name;
                        }

                        _allSaves.Add(new TestSaveInfo
                        {
                            ConfigPath = filePath,
                            LevelId = levelId,
                            SaveName = saveName,
                            Version = version,
                            WorldDir = worldsDir
                        });
                    }
                    catch { }
                }

                LabEmpty.Visibility = _allSaves.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                FilterSaves();
            }
            catch (Exception ex)
            {
                LabEmpty.Text = "加载失败: " + ex.Message;
                LabEmpty.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 搜索过滤
        /// </summary>
        private void FilterSaves()
        {
            PanSaveList.Children.Clear();

            string kw = (TxtSearch.GetText() ?? "").Trim().ToLower();
            var list = string.IsNullOrEmpty(kw)
                ? _allSaves
                : _allSaves.Where(s => s.SaveName.ToLower().Contains(kw) || s.LevelId.ToLower().Contains(kw)).ToList();

            LabCount.Text = string.Format("共 {0} 个存档", list.Count);

            foreach (var save in list)
                PanSaveList.Children.Add(CreateRow(save));
        }

        /// <summary>
        /// 单行: 存档名 | 版本 | 修改游戏版本 | 打开存档
        /// </summary>
        private Border CreateRow(TestSaveInfo save)
        {
            var row = new Border
            {
                CornerRadius = new CornerRadius(5),
                Background = Brushes.Transparent,
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 2)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80, GridUnitType.Auto) });

            // 存档名
            var nameBlock = new TextBlock
            {
                Text = save.SaveName, FontSize = 13, FontWeight = FontWeights.Bold,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrush1"),
                VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameBlock, 0); grid.Children.Add(nameBlock);

            // 版本号
            var verBlock = new TextBlock
            {
                Text = string.IsNullOrEmpty(save.Version) ? "(无)" : save.Version,
                FontSize = 12,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrushGray2"),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(verBlock, 1); grid.Children.Add(verBlock);

            // 修改游戏版本
            var editVerBtn = new MyButton
            {
                Text = "修改游戏版本", Padding = new Thickness(10, 3, 10, 3),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0)
            };
            editVerBtn.Click += (_, _) =>
            {
                string newVer = MyMsgBox.Prompt("输入新版本号", save.Version);
                if (newVer != null && newVer != save.Version)
                {
                    try
                    {
                        string json = File.ReadAllText(save.ConfigPath);
                        var data = JObject.Parse(json);
                        data["version"] = newVer;
                        File.WriteAllText(save.ConfigPath, data.ToString());
                        save.Version = newVer;
                        verBlock.Text = newVer;
                        MyMsgBox.Show("版本号已更新!", "成功", MyMsgBox.MsgType.Info);
                    }
                    catch (Exception ex)
                    {
                        MyMsgBox.Show("修改失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error);
                    }
                }
            };
            Grid.SetColumn(editVerBtn, 2); grid.Children.Add(editVerBtn);

            // 打开存档
            var openBtn = new MyButton
            {
                Text = "打开存档", Padding = new Thickness(10, 3, 10, 3),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0)
            };
            openBtn.Click += (_, _) =>
            {
                if (Directory.Exists(save.WorldDir))
                    Process.Start("explorer.exe", save.WorldDir);
                else
                    MyMsgBox.Show("存档目录不存在: " + save.WorldDir, "提示", MyMsgBox.MsgType.Warning);
            };
            Grid.SetColumn(openBtn, 3); grid.Children.Add(openBtn);

            row.Child = grid;
            return row;
        }
    }

    /// <summary>
    /// 测试存档信息
    /// </summary>
    public class TestSaveInfo
    {
        public string ConfigPath { get; set; }
        public string LevelId { get; set; }
        public string SaveName { get; set; }
        public string Version { get; set; }
        public string WorldDir { get; set; }
    }
}
