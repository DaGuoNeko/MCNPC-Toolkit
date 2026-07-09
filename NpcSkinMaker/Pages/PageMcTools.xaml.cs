using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace NpcSkinMaker
{
    /// <summary>开发者工具箱</summary>
    public partial class PageMcTools : UserControl
    {
        public PageMcTools()
        {
            InitializeComponent();

            var s = MainWindow.Instance.Settings;
            TxtMcPath.Text = s.McPath;
            TxtScriptPath.Text = s.ModScriptPath;
            TxtModName.Text = s.ModName;
            ChkHelp.IsChecked = s.ModHelp;
            ChkHud.IsChecked = s.ModHud;
            ChkWorldData.IsChecked = s.ModWorldData;
            ChkSetting.IsChecked = (bool)s.ModSetting;
            TxtItemScript.Text = s.ItemScriptPath;

            BtnBrowseMc.Click += (_, _) =>
            {
                var ofd = new OpenFileDialog { Title = "选择 MC 启动器程序", Filter = "可执行文件|*.exe|所有文件|*.*" };
                if (ofd.ShowDialog() == true) { TxtMcPath.Text = ofd.FileName; s.McPath = ofd.FileName; s.Save(); }
            };

            BtnLaunchMc.Click += (_, _) => LaunchMc();

            BtnBrowseScript.Click += (_, _) =>
            {
                var ofd = new OpenFileDialog { Title = "选择一键生成完整MOD.py", Filter = "Python 脚本|*.py|所有文件|*.*" };
                if (ofd.ShowDialog() == true) { TxtScriptPath.Text = ofd.FileName; s.ModScriptPath = ofd.FileName; s.Save(); }
            };

            // 输入时自动保存
            TxtModName.InnerBox.TextChanged += (_, _) => { s.ModName = TxtModName.GetText(); s.Save(); };
            ChkHelp.Click += (_, _) => { s.ModHelp = ChkHelp.IsChecked == true; s.Save(); };
            ChkHud.Click += (_, _) => { s.ModHud = ChkHud.IsChecked == true; s.Save(); };
            ChkWorldData.Click += (_, _) => { s.ModWorldData = ChkWorldData.IsChecked == true; s.Save(); };
            ChkSetting.Click += (_, _) => { s.ModSetting = ChkSetting.IsChecked == true; s.Save(); };

            BtnGenMod.Click += (_, _) => GenerateMod();
            BtnGenItem.Click += (_, _) => GenerateItem();
            BtnBrowseItemScript.Click += (_, _) =>
            {
                var ofd = new OpenFileDialog { Title = "选择 autoMinecraftitem.py", Filter = "Python 脚本|*.py|所有文件|*.*" };
                if (ofd.ShowDialog() == true) { TxtItemScript.Text = ofd.FileName; s.ItemScriptPath = ofd.FileName; s.Save(); }
            };        }

        private async void LaunchMc()
        {
            string path = TxtMcPath.GetText().Trim();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            { MyMsgBox.Show("请先选择有效的 MC 启动器程序路径", "提示", MyMsgBox.MsgType.Warning); return; }
            await LaunchWithLoading(path);
        }

        private async Task LaunchWithLoading(string exePath)
        {
            var loadingWin = new Window
            {
                Title = "启动中", Width = 300, Height = 100,
                WindowStyle = WindowStyle.None, AllowsTransparency = true, Background = null,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = false, Topmost = true, Owner = MainWindow.Instance,
                Content = new Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF)),
                    CornerRadius = new System.Windows.CornerRadius(7),
                    Child = new StackPanel
                    {
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Children = { new TextBlock { Text = "正在启动网易开发版互通 MC...", FontSize = 14, Foreground = System.Windows.Media.Brushes.DimGray, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) } }
                    }
                }
            };
            loadingWin.Show();
            var psi = new ProcessStartInfo(exePath) { WorkingDirectory = Path.GetDirectoryName(exePath), UseShellExecute = true };
            var process = Process.Start(psi);
            if (process == null) { loadingWin.Close(); return; }
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                await Task.Run(() =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        try { process.Refresh(); if (process.MainWindowHandle != IntPtr.Zero) { Thread.Sleep(500); break; } } catch { break; }
                        Thread.Sleep(300);
                    }
                }, cts.Token);
            }
            catch { }
            loadingWin.Dispatcher.Invoke(() => loadingWin.Close());
        }

        // ===== MOD 框架生成 =====

        private void GenerateMod()
        {
            string scriptPath = TxtScriptPath.GetText().Trim();
            if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
            { MyMsgBox.Show("请先选择「一键生成完整MOD.py」脚本文件", "提示", MyMsgBox.MsgType.Warning); return; }

            string name = TxtModName.GetText().Trim();
            if (string.IsNullOrEmpty(name))
            { MyMsgBox.Show("请输入模组名称", "提示", MyMsgBox.MsgType.Warning); return; }
            if (!Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
            { MyMsgBox.Show("模组名称必须以字母开头，只能包含字母、数字和下划线", "提示", MyMsgBox.MsgType.Warning); return; }
            if (name == "custom_warehouse")
            { MyMsgBox.Show("模组名称不能为 custom_warehouse", "提示", MyMsgBox.MsgType.Warning); return; }

            string outDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            var dlg = new VistaFolderBrowserDialog { SelectedPath = outDir };
            if (dlg.ShowDialog() != true) return;
            outDir = dlg.SelectedPath;

            MainWindow.Instance.Settings.ModOutDir = outDir;
            MainWindow.Instance.Settings.Save();

            // 拼接传给脚本的输入（模拟 stdin）
            string inputs = name + "\n";
            inputs += (ChkHelp.IsChecked == true ? "y" : "n") + "\n";
            inputs += (ChkHud.IsChecked == true ? "y" : "n") + "\n";
            inputs += (ChkWorldData.IsChecked == true ? "y" : "n") + "\n";
            inputs += (ChkSetting.IsChecked == true ? "y" : "n") + "\n";

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "\"" + scriptPath + "\" --no-interactive --output \"" + outDir + "\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = Process.Start(psi);
                process.StandardInput.Write(inputs);
                process.StandardInput.Close();
                process.WaitForExit(30000);

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (process.ExitCode != 0 || error.Contains("Traceback"))
                    MyMsgBox.Show("生成失败:\n" + error, "错误", MyMsgBox.MsgType.Error);
                else
                    MyMsgBox.Show("MOD 框架生成完成!\n\n输出目录: " + outDir, "成功", MyMsgBox.MsgType.Info);
            }
            catch (Exception ex)
            {
                MyMsgBox.Show("生成失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error);
            }
        }

        // ===== 批量生成物品模板 =====

        private void GenerateItem()
        {
            string ns = TxtItemNs.GetText().Trim();
            string name = TxtItemName.GetText().Trim();
            string cnname = TxtItemCnName.GetText().Trim();
            string tab = TxtItemTab.GetText().Trim();

            if (string.IsNullOrEmpty(ns) || string.IsNullOrEmpty(name))
            { MyMsgBox.Show("请填写命名空间和前缀名字", "提示", MyMsgBox.MsgType.Warning); return; }

            if (!int.TryParse(TxtItemStart.GetText().Trim(), out int start))
            { MyMsgBox.Show("起始序号必须是数字", "提示", MyMsgBox.MsgType.Warning); return; }
            if (!int.TryParse(TxtItemEnd.GetText().Trim(), out int end))
            { MyMsgBox.Show("结束序号必须是数字", "提示", MyMsgBox.MsgType.Warning); return; }
            if (start > end)
            { MyMsgBox.Show("起始序号不能大于结束序号", "提示", MyMsgBox.MsgType.Warning); return; }

            int itemtype = 1;
            if (RbWeapon.IsChecked == true) itemtype = 2;
            else if (RbPickaxe.IsChecked == true) itemtype = 4;

            bool cnametoindex = ChkItemIndex.IsChecked == true;
            bool isCategory = true;
            bool isCustom = ChkItemCustom.IsChecked == true;
            string customName = isCustom ? "weapon" : "";

            // 根据类型收集额外参数
            int stackSize = 64, maxDamage = 0, damage = 0, walevel = 0;
            if (itemtype == 1)
            {
                if (!int.TryParse(PromptInput("堆叠数量", "64"), out stackSize)) stackSize = 64;
            }
            else if (itemtype == 2)
            {
                if (!int.TryParse(PromptInput("耐久值", "100"), out maxDamage)) maxDamage = 100;
                if (!int.TryParse(PromptInput("攻击伤害", "5"), out damage)) damage = 5;
            }
            else if (itemtype == 4)
            {
                if (!int.TryParse(PromptInput("挖掘等级 (0木板 1石头 2铁 3钻石 4铁砧)", "2"), out walevel)) walevel = 2;
                if (!int.TryParse(PromptInput("耐久值", "100"), out maxDamage)) maxDamage = 100;
                if (!int.TryParse(PromptInput("攻击伤害", "5"), out damage)) damage = 5;
            }

            // 选择输出目录
            var dlg = new VistaFolderBrowserDialog
            {
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (dlg.ShowDialog() != true) return;
            string outDir = dlg.SelectedPath;

            // 拼接 stdin 输入（必须和 Python 脚本的读取顺序完全一致）
            string inputs = ns + "\n";
            inputs += name + "\n";
            inputs += cnname + "\n";
            inputs += (cnametoindex ? "true" : "false") + "\n";
            inputs += (string.IsNullOrEmpty(tab) ? "Items" : tab) + "\n";
            inputs += (isCategory ? "true" : "false") + "\n";
            inputs += (isCustom ? "true" : "false") + "\n";
            // is_custom_name 只有 isCustom=true 时 Python 才读，所以 false 时不发
            if (isCustom)
                inputs += customName + "\n";
            inputs += start + "\n";
            inputs += end + "\n";
            inputs += itemtype + "\n";

            if (itemtype == 1)
                inputs += stackSize + "\n";
            else if (itemtype == 2)
                inputs += maxDamage + "\n" + damage + "\n";
            else if (itemtype == 4)
                inputs += walevel + "\n" + maxDamage + "\n" + damage + "\n";

            string scriptPath = TxtItemScript.GetText().Trim();
            if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
            { MyMsgBox.Show("请先选择 autoMinecraftitem.py 脚本文件", "提示", MyMsgBox.MsgType.Warning); return; }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "\"" + scriptPath + "\" --no-interactive --output \"" + outDir + "\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = Process.Start(psi);
                process.StandardInput.Write(inputs);
                process.StandardInput.Close();
                process.WaitForExit(30000);

                string error = process.StandardError.ReadToEnd();
                if (process.ExitCode != 0 || error.Contains("Traceback"))
                    MyMsgBox.Show("生成失败:\n" + error, "错误", MyMsgBox.MsgType.Error);
                else
                    MyMsgBox.Show("物品模板生成完成!\n\n输出目录: " + outDir, "成功", MyMsgBox.MsgType.Info);
            }
            catch (Exception ex)
            {
                MyMsgBox.Show("生成失败: " + ex.Message, "错误", MyMsgBox.MsgType.Error);
            }
        }

        private string PromptInput(string label, string defaultValue)
        {
            return MyMsgBox.Prompt(label, defaultValue);
        }
    }
}
