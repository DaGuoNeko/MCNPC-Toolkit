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
            // 必须最先执行：提取 WebView2Loader.dll 到 EXE 同目录
            // （WebView2 在类型加载时就尝试加载原生 DLL，比 MainWindow 构造更早）
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
                    if (stream != null)
                    {
                        if (!File.Exists(dllPath) || stream.Length != new FileInfo(dllPath).Length)
                        {
                            using (var fs = new FileStream(dllPath, FileMode.Create))
                            {
                                stream.CopyTo(fs);
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
