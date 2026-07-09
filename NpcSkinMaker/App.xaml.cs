using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace NpcSkinMaker
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ExtractWebView2Loader();
            base.OnStartup(e);
            Logger.Info("应用启动");
        }

        private static void ExtractWebView2Loader()
        {
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string dllPath = Path.Combine(exeDir, "WebView2Loader.dll");
                var assembly = Assembly.GetExecutingAssembly();

                using (var stream = assembly.GetManifestResourceStream("WebView2Loader.dll"))
                {
                    if (stream == null) return;

                    if (!File.Exists(dllPath) || stream.Length != new FileInfo(dllPath).Length)
                    {
                        using (var fs = new FileStream(dllPath, FileMode.Create))
                            stream.CopyTo(fs);
                    }
                }

                // 新版 SDK 支持：告知 WebView2 从 EXE 目录加载
                if (File.Exists(dllPath))
                    Microsoft.Web.WebView2.Core.CoreWebView2Environment.SetLoaderDllFolderPath(exeDir);
            }
            catch { }
        }
    }
}
