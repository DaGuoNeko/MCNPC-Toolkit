using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace NpcSkinMaker
{
    /// <summary>3D 文字 - Cube 3D Text 内嵌版</summary>
    public partial class Page3DText : UserControl
    {
        public Page3DText()
        {
            InitializeComponent();
            Loaded += Page3DText_Loaded;
        }

        private async void Page3DText_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var env = await CoreWebView2Environment.CreateAsync();
                await WebView.EnsureCoreWebView2Async(env);

                WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                string cubeDir = FindDistDir();

                if (!string.IsNullOrEmpty(cubeDir))
                {
                    WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "cube3d.local", cubeDir, CoreWebView2HostResourceAccessKind.Allow);
                    WebView.CoreWebView2.Navigate("https://cube3d.local/index.html");
                }
                else
                {
                    WebView.CoreWebView2.Navigate("https://3dtext.easecation.net/");
                }

                LabLoading.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                LabLoading.Text = "加载失败: " + ex.Message;
            }
        }

        private string FindDistDir()
        {
            string[] paths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "cube3d", "dist"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "newexe", "NpcSkinMaker", "NpcSkinMaker", "Resources", "cube3d", "dist"),
            };
            foreach (string p in paths)
                if (Directory.Exists(p)) return p;
            return null;
        }
    }
}
