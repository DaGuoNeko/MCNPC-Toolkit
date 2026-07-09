using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

            BtnGenMod.Click += (_, _) => GenerateMod();
        }

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
    }
}
