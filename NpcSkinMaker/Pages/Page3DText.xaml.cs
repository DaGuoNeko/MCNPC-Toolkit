using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace NpcSkinMaker
{
    /// <summary>3D 文字 - Cube 3D Text</summary>
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

                // 导航完成后隐藏加载提示
                WebView.NavigationCompleted += (s, args) =>
                {
                    LabLoading.Visibility = Visibility.Collapsed;
                };

                WebView.CoreWebView2.Navigate("https://3dtext.easecation.net/");
            }
            catch (Exception ex)
            {
                LabLoading.Text = "加载失败: " + ex.Message;
            }
        }
    }
}
