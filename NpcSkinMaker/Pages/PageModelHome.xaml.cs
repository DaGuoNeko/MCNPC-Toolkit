using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;

namespace NpcSkinMaker
{
    /// <summary>
    /// 模型管理首页
    /// </summary>
    public partial class PageModelHome : UserControl
    {
        private int _selectedIndex = -1;
        private bool _isExporting;

        public PageModelHome()
        {
            InitializeComponent();

            BtnAdd.Click += BtnAdd_Click;
            BtnMoveUp.Click += BtnMoveUp_Click;
            BtnMoveDown.Click += BtnMoveDown_Click;
            BtnClear.Click += BtnClear_Click;
            BtnExport.Click += BtnExport_Click;

            RefreshList();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EditModelDialog(null) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var entry = dialog.GetEntry();
                    MainWindow.Instance.ModelManager.AddModel(entry);
                    RefreshList();
                    ShowToast("模型添加成功", MyHint.HintTheme.Blue);
                }
                catch (Exception ex)
                {
                    MyMsgBox.Show("添加失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error);
                }
            }
        }

        private void BtnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIndex > 0)
            {
                MainWindow.Instance.ModelManager.MoveUp(_selectedIndex);
                _selectedIndex--;
                RefreshList();
            }
        }

        private void BtnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIndex >= 0 && _selectedIndex < MainWindow.Instance.ModelManager.GetCount() - 1)
            {
                MainWindow.Instance.ModelManager.MoveDown(_selectedIndex);
                _selectedIndex++;
                RefreshList();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.Instance.ModelManager.GetCount() == 0) return;

            var result = MyMsgBox.ShowYesNo("确定要清空所有模型吗？此操作不可撤销。", "确认清空",
                MyMsgBox.MsgType.Question);
            if (result == MyMsgBox.MsgResult.Yes)
            {
                MainWindow.Instance.ModelManager.Clear();
                _selectedIndex = -1;
                RefreshList();
                ShowToast("模型列表已清空", MyHint.HintTheme.Blue);
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.Instance.ModelManager.GetCount() == 0)
            {
                ShowToast("模型列表为空，无法导出", MyHint.HintTheme.Yellow);
                return;
            }
            if (_isExporting) return;

            string outputDir = MainWindow.Instance.Settings.LastOutputDir;
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (!string.IsNullOrEmpty(outputDir) && Directory.Exists(outputDir))
                dialog.SelectedPath = outputDir;
            if (dialog.ShowDialog() != true) return;
            outputDir = dialog.SelectedPath;

            MainWindow.Instance.Settings.LastOutputDir = outputDir;
            MainWindow.Instance.Settings.Save();

            _isExporting = true;
            BtnExport.Text = "打包中...";

            var models = MainWindow.Instance.ModelManager.GetAllModels();
            var builder = MainWindow.Instance.ModelPackageBuilder;

            if (builder == null)
            {
                _isExporting = false;
                BtnExport.Text = "导出 ZIP 拓展包";
                MyMsgBox.Show("打包器初始化失败，模板文件可能未正确加载。\n请查看日志: " + Logger.GetLogFile(),
                    "错误", MyMsgBox.MsgType.Error);
                return;
            }

            var thread = new Thread(() =>
            {
                try
                {
                    string zipPath = builder.BuildPackage(models, outputDir);
                    Dispatcher.Invoke(() =>
                    {
                        _isExporting = false;
                        BtnExport.Text = "导出 ZIP 拓展包";
                        ShowToast("打包完成！文件: " + zipPath, MyHint.HintTheme.Blue);
                        Logger.Info("[模型] 打包完成: " + zipPath);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _isExporting = false;
                        BtnExport.Text = "导出 ZIP 拓展包";
                        Logger.Error("[模型] 打包失败: " + ex);
                        MyMsgBox.Show("打包失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error);
                    });
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        public void RefreshList()
        {
            PanModelList.Children.Clear();

            var models = MainWindow.Instance.ModelManager.GetAllModels();
            for (int i = 0; i < models.Count; i++)
            {
                var row = new ModelRow(models[i], i);
                row.EditRequested += (idx) => EditModel(idx);
                row.DeleteRequested += (idx) => DeleteModel(idx);
                row.SelectionChanged += (idx) =>
                {
                    _selectedIndex = idx;
                    foreach (var child in PanModelList.Children.OfType<ModelRow>())
                        child.IsSelected = child.Index == idx;
                };
                PanModelList.Children.Add(row);
            }

            PanEmpty.Visibility = models.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            int count = MainWindow.Instance.ModelManager.GetCount();
            LabCount.Text = "模型数量: " + count + "/500";
        }

        private void EditModel(int index)
        {
            try
            {
                var model = MainWindow.Instance.ModelManager.GetModel(index);
                var dialog = new EditModelDialog(model) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() == true)
                {
                    var entry = dialog.GetEntry();
                    MainWindow.Instance.ModelManager.UpdateModel(index, entry);
                    RefreshList();
                    ShowToast("模型更新成功", MyHint.HintTheme.Blue);
                }
            }
            catch (Exception ex)
            {
                MyMsgBox.Show("编辑失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error);
            }
        }

        private void DeleteModel(int index)
        {
            try
            {
                var result = MyMsgBox.ShowYesNo("确定要删除这个模型吗？", "确认删除",
                    MyMsgBox.MsgType.Question);
                if (result == MyMsgBox.MsgResult.Yes)
                {
                    MainWindow.Instance.ModelManager.RemoveModel(index);
                    if (_selectedIndex == index) _selectedIndex = -1;
                    else if (_selectedIndex > index) _selectedIndex--;
                    RefreshList();
                    ShowToast("模型删除成功", MyHint.HintTheme.Blue);
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
    /// 模型列表行
    /// </summary>
    public class ModelRow : Border
    {
        public int Index { get; set; }
        public bool IsSelected { get; set; }

        public event Action<int> EditRequested;
        public event Action<int> DeleteRequested;
        public event Action<int> SelectionChanged;

        private bool _isHover;

        public ModelRow(ModelEntry model, int index)
        {
            Index = index;
            CornerRadius = new CornerRadius(4);
            Padding = new Thickness(6);
            Margin = new Thickness(0, 0, 0, 2);
            BorderThickness = new Thickness(0, 0, 0, 1);
            BorderBrush = (Brush)Application.Current.TryFindResource("ColorBrushGray6");

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

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

            // 显示名称
            var nameLabel = new TextBlock
            {
                Text = model.DisplayName,
                FontSize = 13,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrush1"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameLabel, 1);
            grid.Children.Add(nameLabel);

            // 标识符
            var idLabel = new TextBlock
            {
                Text = model.Identifier,
                FontSize = 11,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrushGray2"),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(idLabel, 2);
            grid.Children.Add(idLabel);

            // 来源
            var sourceLabel = new TextBlock
            {
                Text = model.SourceLabel,
                FontSize = 12,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrushGray2"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(sourceLabel, 3);
            grid.Children.Add(sourceLabel);

            // 贴图数
            var texLabel = new TextBlock
            {
                Text = model.Textures.Count.ToString(),
                FontSize = 12,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrushGray2"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(texLabel, 4);
            grid.Children.Add(texLabel);

            // 动画数
            var animLabel = new TextBlock
            {
                Text = model.AnimationList.Count.ToString(),
                FontSize = 12,
                Foreground = (Brush)Application.Current.TryFindResource("ColorBrushGray2"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(animLabel, 5);
            grid.Children.Add(animLabel);

            // 操作按钮
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var editBtn = new MyButton { Text = "编辑", Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(10, 3, 10, 3) };
            editBtn.Click += delegate(object s, RoutedEventArgs e) { if (EditRequested != null) EditRequested(Index); };
            btnPanel.Children.Add(editBtn);

            var deleteBtn = new MyButton { Text = "删除", ColorType = MyButton.ColorState.Red, Padding = new Thickness(10, 3, 10, 3) };
            deleteBtn.Click += delegate(object s, RoutedEventArgs e) { if (DeleteRequested != null) DeleteRequested(Index); };
            btnPanel.Children.Add(deleteBtn);

            Grid.SetColumn(btnPanel, 6);
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
                    SelectionChanged(Index);
            };
        }
    }
}
