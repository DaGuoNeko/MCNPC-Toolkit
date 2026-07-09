using System;
using System.Windows;

namespace NpcSkinMaker
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Logger.Info("应用启动");
        }
    }
}
