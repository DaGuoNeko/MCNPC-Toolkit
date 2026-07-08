using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NpcSkinMaker
{
    /// <summary>
    /// 模型拓展包打包器 - 1:1 移植自 Python package_builder.py
    /// 核心打包流程：解压模板 -> 重命名 -> 更新UUID -> 清空sounds -> 写配置
    /// -> 处理每个模型 -> 更新脚本 -> 清理占位 -> 打ZIP
    /// </summary>
    public class ModelPackageBuilder
    {
        private const string TEMPLATE_IDENTIFIER = "customnpc:id_dlcnpc";
        private const string ANIM_PASS = "animation.customnpc.default.pass";
        private const string ANIM_DEATH = "animation.customnpc.default.death";

        // 渲染控制器映射（按贴图数量）
        private static readonly Dictionary<int, string> RenderControllerMap = new Dictionary<int, string>
        {
            {1, "controller.render.dlcnpc_default_cont"},
            {2, "controller.render.dlcnpc_default_2_cont"},
            {3, "controller.render.dlcnpc_default_3_cont"},
            {4, "controller.render.dlcnpc_default_4_cont"},
            {5, "controller.render.dlcnpc_default_5_cont"},
            {6, "controller.render.dlcnpc_default_6_cont"},
            {7, "controller.render.dlcnpc_default_7_cont"},
            {8, "controller.render.dlcnpc_default_8_cont"},
        };

        private readonly string _templateSource;
        private readonly bool _isZip;

        public ModelPackageBuilder(string templateSource)
        {
            _templateSource = templateSource;
            _isZip = templateSource.EndsWith(".zip");
        }

        public string BuildPackage(List<ModelEntry> models, string outputDir)
        {
            try
            {
                if (models == null || models.Count == 0)
                    throw new Exception("模型列表为空，无法打包");
                if (!Directory.Exists(outputDir))
                    throw new Exception("输出目录不存在: " + outputDir);

                string packageName = Utils.GeneratePackageName("npc_model");
                Logger.Info("[模型] 生成包名: " + packageName);

                string workDir = Path.Combine(outputDir, "_build_" + packageName);
                Directory.CreateDirectory(workDir);
                Logger.Info("[模型] 创建工作目录: " + workDir);

                try
                {
                    // ① 解压模板
                    if (_isZip)
                    {
                        Logger.Info("[模型] 从 ZIP 解压模板: " + _templateSource);
                        ZipFile.ExtractToDirectory(_templateSource, workDir);
                    }

                    // 在重命名前读取模板到内存
                    string bTmplDir = Path.Combine(workDir, "ModName_npc_modelsB", "entities", "ex_dlcnpc");
                    string rTmplDir = Path.Combine(workDir, "ModName_npc_modelsR", "entity", "ex_dlcnpc");

                    // 通用回退模板
                    string bFallbackText = File.ReadAllText(Path.Combine(bTmplDir, "id_dlcnpc.json"), System.Text.Encoding.UTF8);
                    var rFallbackData = ReadJson(Path.Combine(rTmplDir, "id_dlcnpc.entity.json"));

                    // 预读所有模板
                    var bTemplates = LoadTemplatesText(bTmplDir);
                    var rTemplates = LoadTemplatesJson(rTmplDir);

                    string bPath = Path.Combine(workDir, "ModName_npc_modelsB");
                    string rPath = Path.Combine(workDir, "ModName_npc_modelsR");

                    // ② 重命名包文件夹
                    bPath = Utils.RenameDirectory(bPath, packageName + "_npc_modelsB");
                    rPath = Utils.RenameDirectory(rPath, packageName + "_npc_modelsR");
                    Logger.Info("[模型] 重命名完成");

                    // ③ 更新 UUID
                    UpdateUuids(bPath);
                    UpdateUuids(rPath);

                    // ③.5 清空 sounds.json
                    string soundsPath = Path.Combine(rPath, "sounds.json");
                    if (File.Exists(soundsPath))
                    {
                        try
                        {
                            var soundsData = ReadJson(soundsPath);
                            if (soundsData["entity_sounds"] != null && soundsData["entity_sounds"]["entities"] != null)
                            {
                                soundsData["entity_sounds"]["entities"] = new JObject();
                                WriteJson(soundsPath, soundsData);
                                Logger.Info("[模型] 清空 sounds.json 模板残留条目");
                            }
                        }
                        catch (Exception ex) { Logger.Warning("[模型] 清空 sounds.json 失败: " + ex.Message); }
                    }

                    // ④ 生成模型配置 JSON
                    var configList = new JArray();
                    foreach (var m in models)
                        configList.Add(JObject.FromObject(m.ToConfigItem()));

                    var configData = new JObject();
                    configData["npcmodelslist"] = configList;

                    string oldConfig = Path.Combine(rPath, "modconfigs", "ModName_modelsdlc.json");
                    string newConfig = Path.Combine(rPath, "modconfigs", packageName + "_modelsdlc.json");
                    WriteJson(oldConfig, configData);
                    if (oldConfig != newConfig)
                        File.Move(oldConfig, newConfig);
                    Logger.Info("[模型] 写入模型配置: " + newConfig);

                    // ⑤ 处理每个模型
                    foreach (var entry in models)
                    {
                        ProcessModel(entry, bPath, rPath, bTemplates, bFallbackText, rTemplates, rFallbackData);
                    }

                    // ⑥ 更新脚本
                    UpdateScripts(bPath, packageName);

                    // ⑦ 清理模板占位文件
                    CleanupTemplateFiles(bPath, rPath);
                    Logger.Info("[模型] 清理模板占位文件完成");

                    // ⑧ 打 ZIP
                    string zipPath = Path.Combine(outputDir, packageName + ".zip");
                    CreateZip(workDir, zipPath, packageName);
                    Logger.Info("[模型] 打包完成: " + zipPath);
                    return zipPath;
                }
                finally
                {
                    if (Directory.Exists(workDir))
                        Directory.Delete(workDir, true);
                }
            }
            catch (Exception e)
            {
                Logger.Error("[模型] 打包失败: " + e);
                throw new Exception("打包失败: " + e.Message);
            }
        }

        private void UpdateUuids(string packPath)
        {
            string manifestPath = Path.Combine(packPath, "manifest.json");
            var data = ReadJson(manifestPath);
            if (data["header"] != null)
                data["header"]["uuid"] = Utils.GenerateUuid();
            if (data["modules"] != null)
            {
                foreach (var module in data["modules"])
                    module["uuid"] = Utils.GenerateUuid();
            }
            WriteJson(manifestPath, data);
        }

        private void ProcessModel(ModelEntry entry, string bPath, string rPath,
            Dictionary<string, string> bTemplates, string bFallbackText,
            Dictionary<string, JObject> rTemplates, JObject rFallbackData)
        {
            string entityId = entry.GetEntityId();
            Logger.Info("[模型] 处理模型: " + entry.DisplayName + " (" + entityId + ")");

            // 选择模板
            string bTmplText = bTemplates.ContainsKey(entityId) ? bTemplates[entityId] : bFallbackText;
            JObject rTmplData = rTemplates.ContainsKey(entityId) ? rTemplates[entityId] : rFallbackData;
            if (bTemplates.ContainsKey(entityId))
                Logger.Info("[模型]   使用专属行为包模板: " + entityId + ".json");
            else
                Logger.Info("[模型]   使用通用回退行为包模板: id_dlcnpc.json");

            // ① 行为包实体
            BuildBehaviorEntity(entry, entityId, bPath, bTmplText);

            // ② 资源包实体（深拷贝）
            BuildResourceEntity(entry, entityId, rPath, (JObject)rTmplData.DeepClone());

            // ③ sounds.json 注入
            InjectSounds(entry.Identifier, rPath);

            // ③ 贴图
            string texDir = Path.Combine(rPath, "textures", "entity", "npc_dlcnpc", entityId);
            if (!Directory.Exists(texDir)) Directory.CreateDirectory(texDir);
            foreach (var t in entry.Textures)
            {
                if (!string.IsNullOrEmpty(t.Path) && File.Exists(t.Path))
                {
                    string dst = Path.Combine(texDir, Path.GetFileName(t.Path));
                    File.Copy(t.Path, dst, true);
                    Logger.Info("[模型]   复制贴图: " + Path.GetFileName(t.Path));
                }
            }

            // ④ UI 预览图
            if (!string.IsNullOrEmpty(entry.PreviewImagePath) && File.Exists(entry.PreviewImagePath))
            {
                string uiDir = Path.Combine(rPath, "textures", "ui", "dlcnpc_models");
                if (!Directory.Exists(uiDir)) Directory.CreateDirectory(uiDir);
                File.Copy(entry.PreviewImagePath, Path.Combine(uiDir, entityId + ".png"), true);
                Logger.Info("[模型]   复制 UI 预览图");
            }

            // ⑤ 模型文件
            string geoDir = Path.Combine(rPath, "models", "entity", "ex_dlcnpc");
            if (!Directory.Exists(geoDir)) Directory.CreateDirectory(geoDir);
            string dstGeo = Path.Combine(geoDir, entityId + ".geo.json");

            string geoContent = File.ReadAllText(entry.GeoPath, System.Text.Encoding.UTF8);
            // 替换 geometry identifier（只替换第一个）
            var regex = new Regex(@"""identifier""\s*:\s*""geometry\.[^""]+""");
            geoContent = regex.Replace(geoContent, "\"identifier\": \"geometry." + entityId + "\"", 1);
            File.WriteAllText(dstGeo, geoContent, System.Text.Encoding.UTF8);
            Logger.Info("[模型]   复制并修改模型文件: " + Path.GetFileName(entry.GeoPath) + " -> " + entityId + ".geo.json");
        }

        private void BuildBehaviorEntity(ModelEntry entry, string entityId, string bPath, string tmplText)
        {
            string content = tmplText;

            // 替换 identifier
            content = content.Replace(
                "\"identifier\": \"" + TEMPLATE_IDENTIFIER + "\"",
                "\"identifier\": \"" + entry.Identifier + "\"");

            // 替换 collision_box
            content = ReplaceCollisionBox(content, entry.CollisionWidth, entry.CollisionHeight);

            string dstDir = Path.Combine(bPath, "entities", "ex_dlcnpc");
            if (!Directory.Exists(dstDir)) Directory.CreateDirectory(dstDir);
            File.WriteAllText(Path.Combine(dstDir, entityId + ".json"), content, System.Text.Encoding.UTF8);
            Logger.Info("[模型]   写出行为包实体: " + entityId);
        }

        private void BuildResourceEntity(ModelEntry entry, string entityId, string rPath, JObject data)
        {
            var desc = data["minecraft:client_entity"]["description"] as JObject;

            // 标识符
            desc["identifier"] = entry.Identifier;

            // 几何模型
            var geo = new JObject();
            geo["default"] = "geometry." + entityId;
            desc["geometry"] = geo;

            // 贴图：default / default1 / default2 ...
            var textures = new JObject();
            for (int i = 0; i < entry.Textures.Count; i++)
            {
                string key = i == 0 ? "default" : "default" + i;
                string filename = Path.GetFileNameWithoutExtension(entry.Textures[i].Path);
                textures[key] = "textures/entity/npc_dlcnpc/" + entityId + "/" + filename;
            }
            desc["textures"] = textures;

            // 渲染控制器
            int texCount = entry.Textures.Count;
            string rc = RenderControllerMap.ContainsKey(texCount) ? RenderControllerMap[texCount] : RenderControllerMap[1];
            var rcArr = new JArray();
            rcArr.Add(rc);
            desc["render_controllers"] = rcArr;

            // 动画
            var anims = desc["animations"] as JObject;
            if (anims == null) anims = new JObject();
            string idle = !string.IsNullOrEmpty(entry.IdleAnimation) ? entry.IdleAnimation : ANIM_PASS;
            string walk = !string.IsNullOrEmpty(entry.WalkAnimation) ? entry.WalkAnimation : idle;
            string walka = !string.IsNullOrEmpty(entry.WalkaAnimation) ? entry.WalkaAnimation : walk;
            string attack = !string.IsNullOrEmpty(entry.AttackAnimation) ? entry.AttackAnimation : ANIM_PASS;
            string death = !string.IsNullOrEmpty(entry.DeathAnimation) ? entry.DeathAnimation : ANIM_DEATH;
            anims["idle"] = idle;
            anims["walk"] = walk;
            anims["walka"] = walka;
            anims["attack"] = attack;
            anims["death"] = death;
            desc["animations"] = anims;

            // 允许穿戴物
            desc["enable_attachables"] = entry.EnableAttachables;

            string dstDir = Path.Combine(rPath, "entity", "ex_dlcnpc");
            if (!Directory.Exists(dstDir)) Directory.CreateDirectory(dstDir);
            WriteJson(Path.Combine(dstDir, entityId + ".entity.json"), data);
            Logger.Info("[模型]   写出资源包实体: " + entityId);
        }

        private void InjectSounds(string identifier, string rPath)
        {
            string soundsPath = Path.Combine(rPath, "sounds.json");
            if (!File.Exists(soundsPath))
            {
                Logger.Warning("[模型]   sounds.json 不存在，跳过音效注入");
                return;
            }

            var soundsData = ReadJson(soundsPath);
            if (soundsData["entity_sounds"] == null)
                soundsData["entity_sounds"] = new JObject();
            if (soundsData["entity_sounds"]["entities"] == null)
                soundsData["entity_sounds"]["entities"] = new JObject();

            var entities = soundsData["entity_sounds"]["entities"] as JObject;
            if (entities.Property(identifier) == null)
            {
                var entry = new JObject();
                entry["volume"] = 1.0;
                entry["pitch"] = new JArray(0.8, 1.2);
                var events = new JObject();
                events["ambient"] = "customnpc.ambient";
                events["hurt"] = "customnpc.hurt";
                events["death"] = "customnpc.death";
                events["step"] = "customnpc.step";
                events["attack"] = "customnpc.attack";
                events["fall.big"] = "customnpc.fall.big";
                events["fall.small"] = "customnpc.fall.small";
                events["splash"] = "customnpc.splash";
                events["roar"] = "customnpc.roar";
                entry["events"] = events;

                entities[identifier] = entry;
                WriteJson(soundsPath, soundsData);
                Logger.Info("[模型]   注入 sounds.json: " + identifier);
            }
            else
            {
                Logger.Info("[模型]   sounds.json 已有条目，跳过: " + identifier);
            }
        }

        private void UpdateScripts(string bPath, string packageName)
        {
            string oldScript = Path.Combine(bPath, "ModName_npcdlcmodels_Scripts");
            string newScriptName = packageName + "_npcdlcmodels_Scripts";
            string newScript = Utils.RenameDirectory(oldScript, newScriptName);
            Logger.Info("[模型] 重命名脚本目录: " + newScript);

            string clientPy = Path.Combine(newScript, "init_modnpcmodelsClientSystem.py");
            if (File.Exists(clientPy))
            {
                Utils.ReplaceInFile(clientPy, "ModName_modelsdlc.json", packageName + "_modelsdlc.json");
                Logger.Info("[模型] 更新脚本配置文件名引用");
            }
        }

        private void CleanupTemplateFiles(string bPath, string rPath)
        {
            string[] toDelete = {
                Path.Combine(bPath, "entities", "ex_dlcnpc", "id_dlcnpc.json"),
                Path.Combine(rPath, "entity", "ex_dlcnpc", "id_dlcnpc.entity.json"),
            };
            foreach (string path in toDelete)
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private void CreateZip(string workDir, string zipPath, string packageName)
        {
            string bFolder = packageName + "_npc_modelsB";
            string rFolder = packageName + "_npc_modelsR";

            using (var zipf = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                AddAllToZip(zipf, Path.Combine(workDir, bFolder), workDir);
                AddAllToZip(zipf, Path.Combine(workDir, rFolder), workDir);
            }
        }

        private void AddAllToZip(ZipArchive zipf, string packPath, string basePath)
        {
            foreach (string filePath in Directory.GetFiles(packPath, "*", SearchOption.AllDirectories))
            {
                string arcname = GetRelativePath(filePath, basePath).Replace('\\', '/');
                zipf.CreateEntryFromFile(filePath, arcname);
            }
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

        // ===== JSON 辅助 =====

        private JObject ReadJson(string path)
        {
            string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            return JObject.Parse(json);
        }

        private void WriteJson(string path, JObject data)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string json = data.ToString(Formatting.Indented);
            File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        }

        // ===== 模板预读 =====

        private Dictionary<string, string> LoadTemplatesText(string tmplDir)
        {
            var result = new Dictionary<string, string>();
            if (!Directory.Exists(tmplDir)) return result;
            foreach (string f in Directory.GetFiles(tmplDir, "*.json"))
            {
                string entityId = Path.GetFileNameWithoutExtension(f);
                try { result[entityId] = File.ReadAllText(f, System.Text.Encoding.UTF8); }
                catch { }
            }
            return result;
        }

        private Dictionary<string, JObject> LoadTemplatesJson(string tmplDir)
        {
            var result = new Dictionary<string, JObject>();
            if (!Directory.Exists(tmplDir)) return result;
            foreach (string f in Directory.GetFiles(tmplDir, "*.entity.json"))
            {
                string entityId = Path.GetFileNameWithoutExtension(f);
                // 去掉 .entity 后缀
                if (entityId.EndsWith(".entity"))
                    entityId = entityId.Substring(0, entityId.Length - 7);
                try
                {
                    string json = File.ReadAllText(f, System.Text.Encoding.UTF8);
                    result[entityId] = JObject.Parse(json);
                }
                catch { }
            }
            return result;
        }

        // ===== collision_box 替换 =====

        private string ReplaceCollisionBox(string content, double width, double height)
        {
            // 正则匹配 collision_box 区块
            string pattern = @"(""collision_box""\s*:\s*\{[^}]*?""minecraft:collision_box""\s*:\s*\{[^}]*?""width""\s*:\s*)([0-9.]+)([^}]*?""height""\s*:\s*)([0-9.]+)";
            string replaced = Regex.Replace(content, pattern,
                m => m.Groups[1].Value + width.ToString() + m.Groups[3].Value + height.ToString(),
                RegexOptions.Singleline);

            if (replaced == content)
            {
                // 宽松模式：直接文本替换
                replaced = content.Replace("\"width\": 0.4", "\"width\": " + width.ToString())
                                  .Replace("\"height\": 0.4", "\"height\": " + height.ToString());
            }
            return replaced;
        }
    }
}
