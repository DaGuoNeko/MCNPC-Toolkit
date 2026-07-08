using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.ComponentModel;
using System.Threading;
using System.IO.Compression;

namespace NpcSkinMaker
{
    /// <summary>
    /// 首页 — 皮肤列表管理
    /// </summary>
    public partial class PageHome : UserControl
    {
        private bool _isExporting;

        public PageHome()
        {
            InitializeComponent();
            AllowDrop = true;

            BtnAdd.Click += BtnAdd_Click;
            BtnBatchAdd.Click += BtnBatchAdd_Click;
            BtnBatchEdit.Click += BtnBatchEdit_Click;
            BtnImport.Click += BtnImport_Click;
            BtnExport.Click += BtnExport_Click;
            BtnClear.Click += BtnClear_Click;

            Drop += PageHome_Drop;
            PreviewDragOver += PageHome_PreviewDragOver;

            RefreshList();
        }

        private void PageHome_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void PageHome_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var pngFiles = files.Where(f => f.ToLower().EndsWith(".png")).ToList();

            if (pngFiles.Count == 0) return;

            int success = 0, failed = 0;
            string author = "Unknown";

            foreach (var file in pngFiles)
            {
                try
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    name = Utils.SanitizeFilename(name);
                    name = Path.GetFileNameWithoutExtension(name);

                    MainWindow.Instance.SkinManager.AddSkin(file, name, author);
                    success++;
                }
                catch (Exception ex)
                {
                    Logger.Error("拖拽导入失败: " + file + " - " + ex.Message);
                    failed++;
                }
            }

            RefreshList();

            if (failed == 0)
                ShowToast("成功拖拽导入 " + success + " 个皮肤", MyHint.HintTheme.Blue);
            else
                ShowToast("成功 " + success + " 个，失败 " + failed + " 个", MyHint.HintTheme.Red);
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddSkinDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string texturePath, name, author;
                    dialog.GetData(out texturePath, out name, out author);
                    MainWindow.Instance.SkinManager.AddSkin(texturePath, name, author);
                    RefreshList();
                    ShowToast("皮肤添加成功", MyHint.HintTheme.Blue);
                }
                catch (Exception ex)
                {
                    MyMsgBox.Show("添加失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error);
                }
            }
        }

        private void BtnBatchAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new BatchAddSkinDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                var dataList = dialog.GetData();
                int success = 0, failed = 0;

                foreach (var entry in dataList)
                {
                    try
                    {
                        MainWindow.Instance.SkinManager.AddSkin(entry.TexturePath, entry.Name, entry.Author);
                        success++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("批量添加失败: " + ex.Message);
                        failed++;
                    }
                }

                RefreshList();

                if (failed == 0)
                    ShowToast("成功添加 " + success + " 个皮肤", MyHint.HintTheme.Blue);
                else
                    ShowToast("成功 " + success + " 个，失败 " + failed + " 个", MyHint.HintTheme.Red);
            }
        }

        private void BtnBatchEdit_Click(object sender, RoutedEventArgs e)
        {
            // 获取选中的行
            var selectedIndices = PanSkinList.Children
                .OfType<SkinRow>()
                .Where(r => r.IsSelected)
                .Select(r => r.Index)
                .ToList();

            if (selectedIndices.Count == 0)
            {
                ShowToast("请先选择要编辑的皮肤（点击行左侧选中）", MyHint.HintTheme.Yellow);
                return;
            }

            var dialog = new BatchEditDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                string newName, newAuthor;
                dialog.GetData(out newName, out newAuthor);

                foreach (int idx in selectedIndices.OrderBy(i => i))
                {
                    var skin = MainWindow.Instance.SkinManager.GetSkin(idx);
                    string name = string.IsNullOrEmpty(newName) ? skin.Name : newName;
                    string author = string.IsNullOrEmpty(newAuthor) ? skin.Author : newAuthor;
                    MainWindow.Instance.SkinManager.UpdateSkin(idx, name, author);
                }

                RefreshList();
                ShowToast("成功批量编辑 " + selectedIndices.Count + " 个皮肤", MyHint.HintTheme.Blue);
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "选择 ZIP 文件",
                Filter = "ZIP 文件 (*.zip)|*.zip|所有文件 (*.*)|*.*"
            };

            if (ofd.ShowDialog() != true) return;

            try
            {
                ImportZip(ofd.FileName);
                ShowToast("导入成功", MyHint.HintTheme.Blue);
            }
            catch (Exception ex)
            {
                MyMsgBox.Show("导入失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error);
            }
        }

        private void ImportZip(string zipPath)
        {
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                // 查找皮肤配置文件
                var configEntry = archive.Entries.FirstOrDefault(x => x.Name.EndsWith("_skindlc.json"));
                if (configEntry == null)
                    throw new Exception("ZIP 文件中未找到皮肤配置文件");

                string configJson;
                using (var reader = new StreamReader(configEntry.Open(), System.Text.Encoding.UTF8))
                    configJson = reader.ReadToEnd();

                var configData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);

                // 导入皮肤列表
                MainWindow.Instance.SkinManager.ImportFromDict(configData);

                // 创建临时目录用于提取贴图
                string tempDir = Path.Combine(Path.GetTempPath(), "npc_skin_import_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                // 提取贴图文件
                foreach (var skin in MainWindow.Instance.SkinManager.GetAllSkins())
                {
                    if (!skin.FromImport) continue;

                    string texturePath = skin.OriginalTexture ?? "";
                    string textureBasename = texturePath.Contains("/")
                        ? texturePath.Substring(texturePath.LastIndexOf('/') + 1)
                        : texturePath;

                    if (string.IsNullOrEmpty(textureBasename))
                        throw new Exception("无法解析贴图文件名: " + texturePath);

                    string textureFilename = textureBasename + ".png";

                    // 在 ZIP 中查找贴图文件
                    var textureEntry = archive.Entries.FirstOrDefault(x =>
                        x.FullName.EndsWith("npc_dlcskin/" + textureFilename, StringComparison.OrdinalIgnoreCase));

                    if (textureEntry == null)
                        throw new Exception("未找到贴图文件: " + textureFilename);

                    string tempTexturePath = Path.Combine(tempDir, textureFilename);
                    textureEntry.ExtractToFile(tempTexturePath, true);

                    skin.TexturePath = tempTexturePath;
                }
            }

            RefreshList();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.Instance.SkinManager.GetSkinCount() == 0)
            {
                ShowToast("皮肤列表为空，无法导出", MyHint.HintTheme.Yellow);
                return;
            }

            if (_isExporting) return;

            string outputDir = MainWindow.Instance.Settings.LastOutputDir;
            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                if (dialog.ShowDialog() != true) return;
                outputDir = dialog.SelectedPath;
            }
            else
            {
                // 让用户选择输出目录
                var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                dialog.SelectedPath = outputDir;
                if (dialog.ShowDialog() != true) return;
                outputDir = dialog.SelectedPath;
            }

            MainWindow.Instance.Settings.LastOutputDir = outputDir;
            MainWindow.Instance.Settings.Save();

            _isExporting = true;
            BtnExport.Text = "打包中...";

            var skins = MainWindow.Instance.SkinManager.GetAllSkins();
            var builder = MainWindow.Instance.PackageBuilder;

            if (builder == null)
            {
                _isExporting = false;
                BtnExport.Text = "导出 ZIP";
                MyMsgBox.Show("打包器初始化失败，模板文件可能未正确加载。\n请查看日志: " + Logger.GetLogFile(),
                    "错误", MyMsgBox.MsgType.Error);
                return;
            }

            // 后台线程打包
            var thread = new Thread(() =>
            {
                try
                {
                    string zipPath = builder.BuildPackage(skins, outputDir);
                    Dispatcher.Invoke(() =>
                    {
                        _isExporting = false;
                        BtnExport.Text = "导出 ZIP";
                        ShowToast("打包完成！文件: " + zipPath, MyHint.HintTheme.Blue);
                        Logger.Info("打包完成: " + zipPath);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _isExporting = false;
                        BtnExport.Text = "导出 ZIP";
                        Logger.Error("打包失败: " + ex);
                        MyMsgBox.Show("打包失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error);
                    });
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.Instance.SkinManager.GetSkinCount() == 0) return;

            var result = MyMsgBox.ShowYesNo("确定要清空所有皮肤吗？此操作不可撤销。", "确认清空",
                MyMsgBox.MsgType.Question);

            if (result == MyMsgBox.MsgResult.Yes)
            {
                MainWindow.Instance.SkinManager.ClearSkins();
                RefreshList();
                ShowToast("皮肤列表已清空", MyHint.HintTheme.Blue);
            }
        }

        public void RefreshList()
        {
            PanSkinList.Children.Clear();

            var skins = MainWindow.Instance.SkinManager.GetAllSkins();
            for (int i = 0; i < skins.Count; i++)
            {
                var row = new SkinRow(skins[i], i);
                row.EditRequested += (idx) => EditSkin(idx);
                row.PreviewRequested += (idx) => PreviewSkin(idx);
                row.DeleteRequested += (idx) => DeleteSkin(idx);
                row.SelectionChanged += () => { };
                PanSkinList.Children.Add(row);
            }

            // 空列表提示
            PanEmpty.Visibility = skins.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // 更新计数
            int count = MainWindow.Instance.SkinManager.GetSkinCount();
            LabCount.Text = "皮肤数量: " + count + "/1000";
        }

        private void EditSkin(int index)
        {
            try
            {
                var skin = MainWindow.Instance.SkinManager.GetSkin(index);
                var dialog = new EditSkinDialog(skin) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() == true)
                {
                    string name, author;
                    dialog.GetData(out name, out author);
                    MainWindow.Instance.SkinManager.UpdateSkin(index, name, author);
                    RefreshList();
                    ShowToast("皮肤更新成功", MyHint.HintTheme.Blue);
                }
            }
            catch (Exception ex)
            {
                MyMsgBox.Show("编辑失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error);
            }
        }

        private void PreviewSkin(int index)
        {
            try
            {
                var skin = MainWindow.Instance.SkinManager.GetSkin(index);
                var dialog = new PreviewSkinDialog(skin) { Owner = Window.GetWindow(this) };
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MyMsgBox.Show("预览失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error);
            }
        }

        private void DeleteSkin(int index)
        {
            try
            {
                var result = MyMsgBox.ShowYesNo("确定要删除这个皮肤吗？", "确认删除",
                    MyMsgBox.MsgType.Question);
                if (result == MyMsgBox.MsgResult.Yes)
                {
                    MainWindow.Instance.SkinManager.RemoveSkin(index);
                    RefreshList();
                    ShowToast("皮肤删除成功", MyHint.HintTheme.Blue);
                }
            }
            catch (Exception ex)
            {
                MyMsgBox.Show("删除失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error);
            }
        }

        public void ShowToast(string message, MyHint.HintTheme theme = MyHint.HintTheme.Blue)
        {
            HintToast.Show(message, theme);
        }
    }

    /// <summary>
    /// 皮肤列表行
    /// </summary>
    public class SkinRow : Border
    {
        public int Index { get; set; }
        public bool IsSelected { get; set; }

        public event Action<int> EditRequested;
        public event Action<int> PreviewRequested;
        public event Action<int> DeleteRequested;
        public event Action SelectionChanged;

        private bool _isHover;

        public SkinRow(SkinData skin, int index)
        {
            Index = index;
            CornerRadius = new CornerRadius(4);
            Padding = new Thickness(6, 6, 6, 6);
            Margin = new Thickness(0, 0, 0, 2);
            BorderThickness = new Thickness(0, 0, 0, 1);
            BorderBrush = (Brush)Application.Current.TryFindResource("ColorBrushGray6");

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });

            // 序号
            var numLabel = new TextBlock
            {
                Text = (index + 1).ToString(),
                FontSize = 12,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrushGray3"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(numLabel, 0);
            grid.Children.Add(numLabel);

            // 缩略图
            var thumbBorder = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(4),
                Background = (Brush)Application.Current.TryFindResource("ColorBrushGray7"),
                Clip = new RectangleGeometry(new Rect(0, 0, 32, 32), 4, 4)
            };
            if (!string.IsNullOrEmpty(skin.TexturePath) && File.Exists(skin.TexturePath))
            {
                try
                {
                    var img = new Image
                    {
                        Source = new BitmapImage(new Uri(skin.TexturePath)),
                        Stretch = Stretch.UniformToFill,
                        Width = 32,
                        Height = 32
                    };
                    thumbBorder.Child = img;
                }
                catch { }
            }
            Grid.SetColumn(thumbBorder, 1);
            grid.Children.Add(thumbBorder);

            // 名称
            var nameLabel = new TextBlock
            {
                Text = skin.Name,
                FontSize = 13,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrush1"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameLabel, 2);
            grid.Children.Add(nameLabel);

            // 作者
            var authorLabel = new TextBlock
            {
                Text = skin.Author,
                FontSize = 12,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrushGray2"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(authorLabel, 3);
            grid.Children.Add(authorLabel);

            // 操作按钮
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var editBtn = new MyButton { Text = "编辑", Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(10, 3, 10, 3) };
            editBtn.Click += delegate(object s, RoutedEventArgs e) { if (EditRequested != null) EditRequested(Index); };
            btnPanel.Children.Add(editBtn);

            var previewBtn = new MyButton { Text = "预览", Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(10, 3, 10, 3) };
            previewBtn.Click += delegate(object s, RoutedEventArgs e) { if (PreviewRequested != null) PreviewRequested(Index); };
            btnPanel.Children.Add(previewBtn);

            var deleteBtn = new MyButton { Text = "删除", ColorType = MyButton.ColorState.Red, Padding = new Thickness(10, 3, 10, 3) };
            deleteBtn.Click += delegate(object s, RoutedEventArgs e) { if (DeleteRequested != null) DeleteRequested(Index); };
            btnPanel.Children.Add(deleteBtn);

            Grid.SetColumn(btnPanel, 4);
            grid.Children.Add(btnPanel);

            Child = grid;

            MouseEnter += (s, e) => { _isHover = true; Background = (Brush)Application.Current.TryFindResource("ColorBrushGray7"); };
            MouseLeave += (s, e) => { _isHover = false; if (!IsSelected) Background = Brushes.Transparent; };
            MouseLeftButtonUp += (s, e) =>
            {
                IsSelected = !IsSelected;
                Background = IsSelected
                    ? (Brush)Application.Current.TryFindResource("ColorBrush7")
                    : (_isHover ? (Brush)Application.Current.TryFindResource("ColorBrushGray7") : Brushes.Transparent);
                if (SelectionChanged != null)
                    SelectionChanged();
            };
        }
    }
}
