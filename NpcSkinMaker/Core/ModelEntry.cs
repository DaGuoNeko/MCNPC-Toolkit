using System;
using System.Collections.Generic;
using System.IO;

namespace NpcSkinMaker
{
    /// <summary>
    /// 模型数据条目 - 1:1 移植自 Python ModelEntry
    /// 每个条目对应 npcmodelslist 里的一项
    /// </summary>
    public class ModelEntry
    {
        public string DisplayName { get; set; }
        public string CustomName { get; set; }
        public string SourceLabel { get; set; }
        public string GeoPath { get; set; }
        public string PreviewImagePath { get; set; }
        public List<ModelTexture> Textures { get; set; }
        public List<string> AnimationList { get; set; }
        public List<ModelSkin> SkinList { get; set; }
        public double CollisionWidth { get; set; }
        public double CollisionHeight { get; set; }
        public string IdleAnimation { get; set; }
        public string WalkAnimation { get; set; }
        public string WalkaAnimation { get; set; }
        public string AttackAnimation { get; set; }
        public string DeathAnimation { get; set; }
        public bool EnableAttachables { get; set; }

        public ModelEntry()
        {
            DisplayName = "";
            CustomName = "";
            SourceLabel = "原版";
            GeoPath = "";
            PreviewImagePath = "";
            Textures = new List<ModelTexture>();
            AnimationList = new List<string>();
            SkinList = new List<ModelSkin>();
            CollisionWidth = 0.6;
            CollisionHeight = 1.8;
            IdleAnimation = "";
            WalkAnimation = "";
            WalkaAnimation = "";
            AttackAnimation = "";
            DeathAnimation = "";
            EnableAttachables = true;
        }

        /// <summary>自动拼接实体标识符 customnpc:{custom_name}_dlcnpc</summary>
        public string Identifier
        {
            get
            {
                string name = (CustomName ?? "").Trim();
                return string.IsNullOrEmpty(name) ? "" : "customnpc:" + name + "_dlcnpc";
            }
        }

        /// <summary>获取实体 ID 部分，如 my_wolf_dlcnpc</summary>
        public string GetEntityId()
        {
            string name = (CustomName ?? "").Trim();
            return string.IsNullOrEmpty(name) ? "unknown_dlcnpc" : name + "_dlcnpc";
        }

        /// <summary>转换为 npcmodelslist 的一项</summary>
        public Dictionary<string, object> ToConfigItem()
        {
            var item = new Dictionary<string, object>();
            item["name"] = DisplayName ?? "";
            item["image_id"] = "textures/ui/dlcnpc_models/" + GetEntityId();
            item["identifier"] = Identifier;
            item["l"] = SourceLabel ?? "原版";
            item["animation_list"] = new List<string>(AnimationList);
            if (SkinList != null && SkinList.Count > 0)
            {
                var skins = new List<Dictionary<string, object>>();
                foreach (var s in SkinList)
                {
                    var sk = new Dictionary<string, object>();
                    sk["skinid"] = s.SkinId;
                    sk["name"] = s.Name ?? "";
                    sk["by"] = s.By ?? "";
                    skins.Add(sk);
                }
                item["skin_list"] = skins;
            }
            return item;
        }

        /// <summary>验证必填项，返回 (bool, error_msg)</summary>
        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                error = "显示名称不能为空";
                return false;
            }
            if (string.IsNullOrWhiteSpace(CustomName))
            {
                error = "自定义名称不能为空";
                return false;
            }
            if (string.IsNullOrEmpty(GeoPath) || !File.Exists(GeoPath))
            {
                error = "模型 .geo.json 文件必须选择且文件存在";
                return false;
            }
            if (Textures == null || Textures.Count == 0)
            {
                error = "至少需要提供一张贴图";
                return false;
            }
            for (int i = 0; i < Textures.Count; i++)
            {
                var t = Textures[i];
                if (t == null || string.IsNullOrEmpty(t.Path) || !File.Exists(t.Path))
                {
                    error = "贴图 " + i + " 文件不存在: " + (t != null ? t.Path : "");
                    return false;
                }
            }
            if (!string.IsNullOrEmpty(PreviewImagePath) && !File.Exists(PreviewImagePath))
            {
                error = "UI 预览图文件不存在";
                return false;
            }
            error = "";
            return true;
        }
    }

    /// <summary>模型贴图项</summary>
    public class ModelTexture
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public ModelTexture()
        {
            Name = "";
            Path = "";
        }

        public ModelTexture(string name, string path)
        {
            Name = name ?? "";
            Path = path ?? "";
        }
    }

    /// <summary>模型皮肤变体项</summary>
    public class ModelSkin
    {
        public int SkinId { get; set; }
        public string Name { get; set; }
        public string By { get; set; }

        public ModelSkin()
        {
            Name = "";
            By = "Minecraft";
        }

        public ModelSkin(int skinId, string name, string by)
        {
            SkinId = skinId;
            Name = name ?? "";
            By = by ?? "";
        }
    }
}
