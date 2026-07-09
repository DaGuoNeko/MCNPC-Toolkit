using System;
using System.IO;
using Newtonsoft.Json;

namespace NpcSkinMaker
{
    /// <summary>
    /// 应用配置 — 所有配置集中在此
    /// 保存位置：%LocalAppData%\NPC_SkinMaker\settings.json（仅保存运行时偏好）
    /// </summary>
    public class AppSettings
    {
        // ===== 基本（编译时默认值，不会被 settings.json 覆盖） =====
        public string WindowTitle { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string RepoUrl { get; set; }

        // ===== 导航文字 =====
        public string NavHome { get; set; }
        public string Nav3DText { get; set; }
        public string NavDevTools { get; set; }
        public string NavSettings { get; set; }
        public string NavAbout { get; set; }

        // ===== 顶部标签 =====
        public string TabSkin { get; set; }
        public string TabModel { get; set; }

        // ===== 链接 =====
        public string Cube3DUrl { get; set; }
        public string PclRepoUrl { get; set; }

        // ===== 运行时（会被 settings.json 覆盖） =====
        public string LastOutputDir { get; set; }
        public int ThemeHue { get; set; }
        public int ThemeSat { get; set; }
        public bool UseSystemAccent { get; set; }
        public string BgImagePath { get; set; }
        public string McPath { get; set; }
        public string ModScriptPath { get; set; }    // MOD 生成脚本路径
        public string ModOutDir { get; set; }         // MOD 输出目录
        public string ModName { get; set; }           // 模组名称（保持输入不丢失）
        public bool ModHelp { get; set; }
        public bool ModHud { get; set; }
        public bool ModWorldData { get; set; }
        public bool ModSetting { get; set; }
        public string ItemScriptPath { get; set; }    // 物品模板生成脚本路径
        public string FontFamilyName { get; set; }    // 软件字体（系统字体名或文件路径）
        public string FeverGamePath { get; set; }     // FeverGames 游戏安装路径
        public string FeverPlayerId { get; set; }     // FeverGames 上次选中的玩家ID
        public string FeverChannel { get; set; }      // FeverGames 端选择: PC=正式端, PE=测试端

        public AppSettings()
        {
            WindowTitle = "MCNPC 拓展制作工具箱";
            Version = "1.1.0";
            Author = "大果喵 (DaGuoNeko)";
            RepoUrl = "https://github.com/DaGuoNeko/MCNPC-Toolkit";
            NavHome = "首页";
            Nav3DText = "3D 文字";
            NavDevTools = "开发者工具箱";
            NavSettings = "设置";
            NavAbout = "关于";
            TabSkin = "皮肤拓展制作";
            TabModel = "模型拓展制作";
            Cube3DUrl = "https://3dtext.easecation.net/";
            PclRepoUrl = "https://github.com/Meloong-Git/PCL";
            LastOutputDir = "";
            ThemeHue = 210;
            ThemeSat = 85;
            UseSystemAccent = true;
            BgImagePath = "";
            McPath = "";
            ModScriptPath = "";
            ModOutDir = "";
            ModName = "";
            ModHelp = false;
            ModHud = false;
            ModWorldData = false;
            ModSetting = false;
            ItemScriptPath = "";
            FontFamilyName = "Microsoft YaHei UI";
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
            var settings = new AppSettings();
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var user = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (user != null)
                    {
                        if (!string.IsNullOrEmpty(user.LastOutputDir)) settings.LastOutputDir = user.LastOutputDir;
                        settings.ThemeHue = user.ThemeHue;
                        settings.ThemeSat = user.ThemeSat;
                        settings.UseSystemAccent = user.UseSystemAccent;
                        if (!string.IsNullOrEmpty(user.BgImagePath)) settings.BgImagePath = user.BgImagePath;
                        if (!string.IsNullOrEmpty(user.McPath)) settings.McPath = user.McPath;
                        if (!string.IsNullOrEmpty(user.ModScriptPath)) settings.ModScriptPath = user.ModScriptPath;
                        if (!string.IsNullOrEmpty(user.ModOutDir)) settings.ModOutDir = user.ModOutDir;
                        if (!string.IsNullOrEmpty(user.ModName)) settings.ModName = user.ModName;
                        settings.ModHelp = user.ModHelp;
                        settings.ModHud = user.ModHud;
                        settings.ModWorldData = user.ModWorldData;
                        settings.ModSetting = user.ModSetting;
                        if (!string.IsNullOrEmpty(user.ItemScriptPath)) settings.ItemScriptPath = user.ItemScriptPath;
                        if (!string.IsNullOrEmpty(user.FontFamilyName)) settings.FontFamilyName = user.FontFamilyName;
                        if (!string.IsNullOrEmpty(user.FeverGamePath)) settings.FeverGamePath = user.FeverGamePath;
                        if (!string.IsNullOrEmpty(user.FeverPlayerId)) settings.FeverPlayerId = user.FeverPlayerId;
                        if (!string.IsNullOrEmpty(user.FeverChannel)) settings.FeverChannel = user.FeverChannel;
                    }
                }
            }
            catch (Exception e) { Logger.Error("加载设置失败: " + e.Message); }
            return settings;
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // 只保存运行时偏好，不保存编译配置
                var save = new AppSettings
                {
                    LastOutputDir = this.LastOutputDir,
                    ThemeHue = this.ThemeHue,
                    ThemeSat = this.ThemeSat,
                    UseSystemAccent = this.UseSystemAccent,
                    BgImagePath = this.BgImagePath,
                    McPath = this.McPath,
                    ModScriptPath = this.ModScriptPath,
                    ModOutDir = this.ModOutDir,
                    ModName = this.ModName,
                    ModHelp = this.ModHelp,
                    ModHud = this.ModHud,
                    ModWorldData = this.ModWorldData,
                    ModSetting = this.ModSetting,
                    ItemScriptPath = this.ItemScriptPath,
                    FontFamilyName = this.FontFamilyName,
                    FeverGamePath = this.FeverGamePath,
                    FeverPlayerId = this.FeverPlayerId,
                    FeverChannel = this.FeverChannel,
                };
                string json = JsonConvert.SerializeObject(save, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception e)
            {
                Logger.Error("保存设置失败: " + e.Message);
            }
        }
    }
}
