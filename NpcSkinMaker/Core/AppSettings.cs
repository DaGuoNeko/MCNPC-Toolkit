using System;
using System.IO;
using Newtonsoft.Json;

namespace NpcSkinMaker
{
    /// <summary>
    /// 应用设置持久化
    /// </summary>
    public class AppSettings
    {
        public string LastOutputDir { get; set; }
        public int ThemeHue { get; set; }
        public int ThemeSat { get; set; }
        public bool UseSystemAccent { get; set; }
        public string BgImagePath { get; set; }   // 自定义背景图片路径，空=使用纯色背景

        public AppSettings()
        {
            LastOutputDir = "";
            ThemeHue = 210;
            ThemeSat = 85;
            UseSystemAccent = true;
            BgImagePath = "";
        }

        private static string SettingsPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NPC_SkinMaker", "settings.json");
            }
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception e)
            {
                Logger.Error("加载设置失败: " + e.Message);
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception e)
            {
                Logger.Error("保存设置失败: " + e.Message);
            }
        }
    }
}
