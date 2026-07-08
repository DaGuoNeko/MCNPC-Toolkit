using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;

namespace NpcSkinMaker
{
    /// <summary>
    /// 打包构建器 — 移植自 Python core/package_builder.py
    /// 核心打包流程 1:1 等价移植，使用 System.IO.Compression
    /// </summary>
    public class PackageBuilder
    {
        private readonly string _templateSource;
        private readonly bool _isZip;

        public PackageBuilder(string templateSource)
        {
            _templateSource = templateSource;
            _isZip = templateSource.EndsWith(".zip");
        }

        /// <summary>
        /// 构建完整的 NPC 皮肤包
        /// </summary>
        public string BuildPackage(List<SkinData> skins, string outputDir)
        {
            try
            {
                if (skins == null || skins.Count == 0)
                    throw new Exception("皮肤列表为空，无法打包");

                if (!Directory.Exists(outputDir))
                    throw new Exception("输出目录不存在: " + outputDir);

                // 生成唯一包名
                string packageName = Utils.GeneratePackageName();
                Logger.Info("生成包名: " + packageName);

                // 创建临时工作目录
                string workDir = Path.Combine(outputDir, "_build_" + packageName);
                Directory.CreateDirectory(workDir);
                Logger.Info("创建工作目录: " + workDir);

                try
                {
                    string behaviorPackPath;
                    string resourcePackPath;

                    // 从 ZIP 或目录复制模板
                    if (_isZip)
                    {
                        Logger.Info("从 ZIP 解压模板: " + _templateSource);
                        ZipFile.ExtractToDirectory(_templateSource, workDir);
                        behaviorPackPath = Path.Combine(workDir, "ModName_npc_skinB");
                        resourcePackPath = Path.Combine(workDir, "ModName_npc_skinR");
                    }
                    else
                    {
                        string bpTemplate = Path.Combine(_templateSource, "ModName_npc_skinB");
                        string rpTemplate = Path.Combine(_templateSource, "ModName_npc_skinR");

                        if (!Directory.Exists(bpTemplate))
                            throw new Exception("行为包模板不存在: " + bpTemplate);
                        if (!Directory.Exists(rpTemplate))
                            throw new Exception("资源包模板不存在: " + rpTemplate);

                        Logger.Info("复制行为包模板...");
                        behaviorPackPath = Path.Combine(workDir, "ModName_npc_skinB");
                        Utils.CopyDirectory(bpTemplate, behaviorPackPath);

                        Logger.Info("复制资源包模板...");
                        resourcePackPath = Path.Combine(workDir, "ModName_npc_skinR");
                        Utils.CopyDirectory(rpTemplate, resourcePackPath);
                    }

                    // 重命名文件夹
                    Logger.Info("重命名包文件夹...");
                    behaviorPackPath = Utils.RenameDirectory(behaviorPackPath, packageName + "_npc_skinB");
                    resourcePackPath = Utils.RenameDirectory(resourcePackPath, packageName + "_npc_skinR");

                    // 更新 UUID
                    Logger.Info("更新行为包 UUID...");
                    UpdateUuids(behaviorPackPath);
                    Logger.Info("更新资源包 UUID...");
                    UpdateUuids(resourcePackPath);

                    // 生成皮肤配置 JSON
                    Logger.Info("生成皮肤配置...");
                    var skinConfigData = GenerateSkinConfig(skins, packageName);

                    string oldConfigPath = Path.Combine(resourcePackPath, "modconfigs", "ModName_skindlc.json");
                    string newConfigPath = Path.Combine(resourcePackPath, "modconfigs", packageName + "_skindlc.json");

                    Logger.Info("写入皮肤配置: " + oldConfigPath);
                    WriteJson(oldConfigPath, skinConfigData);
                    if (oldConfigPath != newConfigPath)
                    {
                        Logger.Info("重命名配置文件: " + newConfigPath);
                        File.Move(oldConfigPath, newConfigPath);
                    }

                    // 复制贴图文件
                    Logger.Info("复制贴图文件...");
                    CopyTextures(skins, resourcePackPath, packageName);

                    // 更新脚本文件
                    Logger.Info("更新脚本文件...");
                    UpdateScripts(behaviorPackPath, packageName);

                    // 创建 ZIP 文件
                    Logger.Info("创建 ZIP 文件...");
                    string zipPath = Path.Combine(outputDir, packageName + ".zip");
                    CreateZip(workDir, zipPath, packageName);
                    Logger.Info("ZIP 文件创建成功: " + zipPath);

                    return zipPath;
                }
                finally
                {
                    Logger.Info("清理临时目录...");
                    if (Directory.Exists(workDir))
                        Directory.Delete(workDir, true);
                }
            }
            catch (Exception e)
            {
                Logger.Error("打包失败: " + e);
                throw new Exception("打包失败: " + e.Message);
            }
        }

        private void UpdateUuids(string packPath)
        {
            string manifestPath = Path.Combine(packPath, "manifest.json");
            var manifest = ReadJson(manifestPath);

            if (manifest["header"] != null)
                manifest["header"]["uuid"] = Utils.GenerateUuid();

            if (manifest["modules"] != null)
            {
                foreach (var module in manifest["modules"])
                    module["uuid"] = Utils.GenerateUuid();
            }

            WriteJson(manifestPath, manifest);
        }

        private Dictionary<string, object> GenerateSkinConfig(List<SkinData> skins, string packageName)
        {
            var npcskinlist = new List<Dictionary<string, object>>();

            foreach (var skin in skins)
            {
                Dictionary<string, object> item = new Dictionary<string, object>();

                if (skin.FromImport)
                {
                    item["ID"] = skin.OriginalId;
                    item["name"] = skin.Name;
                    item["by"] = skin.Author;
                    item["texture"] = skin.OriginalTexture;
                }
                else
                {
                    string skinId = packageName + "_" + Utils.GenerateShortUid("", 6);
                    item["ID"] = skinId;
                    item["name"] = skin.Name;
                    item["by"] = skin.Author;
                    item["texture"] = "textures/entity/npc_dlcskin/" + skin.Id;
                }

                npcskinlist.Add(item);
            }

            var result = new Dictionary<string, object>();
            result["npcskinlist"] = npcskinlist;
            return result;
        }

        private void CopyTextures(List<SkinData> skins, string resourcePackPath, string packageName)
        {
            string textureDir = Path.Combine(resourcePackPath, "textures", "entity", "npc_dlcskin");
            if (!Directory.Exists(textureDir))
                Directory.CreateDirectory(textureDir);

            for (int idx = 0; idx < skins.Count; idx++)
            {
                var skin = skins[idx];
                try
                {
                    if (string.IsNullOrEmpty(skin.TexturePath))
                    {
                        Logger.Warning("皮肤 " + idx + " 没有贴图路径");
                        continue;
                    }

                    if (!File.Exists(skin.TexturePath))
                        throw new Exception("贴图文件不存在: " + skin.TexturePath);

                    string dstPath = Path.Combine(textureDir, skin.Id + ".png");
                    Logger.Info("复制贴图 " + (idx + 1) + "/" + skins.Count + ": " + skin.TexturePath + " -> " + dstPath);
                    Utils.CopyFile(skin.TexturePath, dstPath);
                }
                catch (Exception e)
                {
                    Logger.Error("复制贴图失败 (皮肤 " + idx + "): " + e.Message);
                    throw;
                }
            }
        }

        private void UpdateScripts(string behaviorPackPath, string packageName)
        {
            try
            {
                string oldScriptDir = Path.Combine(behaviorPackPath, "ModName_npcdlcskin_Scripts");
                string newScriptDir = Path.Combine(behaviorPackPath, packageName + "_npcdlcskin_Scripts");

                if (Directory.Exists(oldScriptDir))
                {
                    Logger.Info("重命名脚本目录: " + oldScriptDir + " -> " + newScriptDir);
                    Utils.RenameDirectory(oldScriptDir, packageName + "_npcdlcskin_Scripts");
                }
                else
                {
                    Logger.Warning("脚本目录不存在: " + oldScriptDir);
                }

                string clientSystemPath = Path.Combine(newScriptDir, "init_modnpcskinClientSystem.py");
                if (File.Exists(clientSystemPath))
                {
                    Logger.Info("更新脚本文件: " + clientSystemPath);
                    Utils.ReplaceInFile(clientSystemPath, "ModName_skindlc.json", packageName + "_skindlc.json");
                }
                else
                {
                    Logger.Warning("脚本文件不存在: " + clientSystemPath);
                }
            }
            catch (Exception e)
            {
                Logger.Error("更新脚本失败: " + e.Message);
                throw;
            }
        }

        private void CreateZip(string workDir, string zipPath, string packageName)
        {
            string behaviorPack = Path.Combine(workDir, packageName + "_npc_skinB");
            string resourcePack = Path.Combine(workDir, packageName + "_npc_skinR");

            Logger.Info("添加行为包到 ZIP: " + behaviorPack);

            using (var zipf = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                AddAllToZip(zipf, behaviorPack, workDir);
                Logger.Info("添加资源包到 ZIP: " + resourcePack);
                AddAllToZip(zipf, resourcePack, workDir);
            }
        }

        private void AddAllToZip(ZipArchive zipf, string packPath, string basePath)
        {
            // 添加所有文件
            foreach (string filePath in Directory.GetFiles(packPath, "*", SearchOption.AllDirectories))
            {
                string arcname = GetRelativePath(filePath, basePath).Replace('\\', '/');
                zipf.CreateEntryFromFile(filePath, arcname);
            }

            // 添加所有目录（包括空目录）
            foreach (string dirPath in Directory.GetDirectories(packPath, "*", SearchOption.AllDirectories))
            {
                string arcname = GetRelativePath(dirPath, basePath).Replace('\\', '/') + "/";
                zipf.CreateEntry(arcname);
            }
        }

        private string GetRelativePath(string fullPath, string basePath)
        {
            return fullPath.Substring(basePath.Length).TrimStart('\\', '/');
        }

        // ===== JSON 辅助方法 =====

        private dynamic ReadJson(string path)
        {
            try
            {
                string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                return JsonConvert.DeserializeObject(json);
            }
            catch (Exception e)
            {
                throw new Exception("读取 JSON 文件失败: " + path + "\n错误: " + e.Message);
            }
        }

        private void WriteJson(string path, object data)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                throw new Exception("写入 JSON 文件失败: " + path + "\n错误: " + e.Message);
            }
        }
    }
}
