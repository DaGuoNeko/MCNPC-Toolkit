using System;
using System.IO;
using System.Text.RegularExpressions;

namespace NpcSkinMaker
{
    /// <summary>
    /// 工具函数 — 移植自 Python core/utils.py
    /// </summary>
    public static class Utils
    {
        private static readonly Random _random = new Random();
        private const string Chars = "abcdefghijklmnopqrstuvwxyz0123456789";

        /// <summary>生成标准 UUID</summary>
        public static string GenerateUuid()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>生成短 UID（随机字符串）</summary>
        public static string GenerateShortUid(string prefix = "", int length = 6)
        {
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
                buffer[i] = Chars[_random.Next(Chars.Length)];
            string randomPart = new string(buffer);
            return string.IsNullOrEmpty(prefix) ? randomPart : prefix + "_" + randomPart;
        }

        /// <summary>生成唯一包名 npc_skin_XXXXXXXX</summary>
        public static string GeneratePackageName(string baseName = "npc_skin")
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string ts = (timestamp % 100000000).ToString("D8");
            return baseName + "_" + ts;
        }

        /// <summary>递归复制目录（包括空目录）</summary>
        public static void CopyDirectory(string src, string dst)
        {
            if (Directory.Exists(dst))
                Directory.Delete(dst, true);
            Directory.CreateDirectory(dst);

            foreach (string file in Directory.GetFiles(src, "*", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine(dst, fileName), true);
            }

            foreach (string dir in Directory.GetDirectories(src, "*", SearchOption.TopDirectoryOnly))
            {
                string dirName = Path.GetFileName(dir);
                CopyDirectory(dir, Path.Combine(dst, dirName));
            }
        }

        /// <summary>复制文件，自动创建目录</summary>
        public static void CopyFile(string src, string dst)
        {
            string dir = Path.GetDirectoryName(dst);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.Copy(src, dst, true);
        }

        /// <summary>读取 JSON 文件</summary>
        public static string ReadTextFile(string path)
        {
            try
            {
                return File.ReadAllText(path, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                throw new Exception("读取文件失败: " + path + "\n错误: " + e.Message);
            }
        }

        /// <summary>写入文本文件</summary>
        public static void WriteTextFile(string path, string content)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, content, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                throw new Exception("写入文件失败: " + path + "\n错误: " + e.Message);
            }
        }

        /// <summary>替换文件内容</summary>
        public static void ReplaceInFile(string path, string oldText, string newText)
        {
            try
            {
                string content = File.ReadAllText(path, System.Text.Encoding.UTF8);
                content = content.Replace(oldText, newText);
                File.WriteAllText(path, content, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                throw new Exception("替换文件内容失败: " + path + "\n错误: " + e.Message);
            }
        }

        /// <summary>重命名目录</summary>
        public static string RenameDirectory(string oldPath, string newName)
        {
            string parentDir = Path.GetDirectoryName(oldPath);
            string newPath = Path.Combine(parentDir, newName);

            if (Directory.Exists(newPath))
                Directory.Delete(newPath, true);

            try
            {
                Directory.Move(oldPath, newPath);
            }
            catch (UnauthorizedAccessException)
            {
                CopyDirectory(oldPath, newPath);
                Directory.Delete(oldPath, true);
            }

            return newPath;
        }

        /// <summary>验证 PNG 文件</summary>
        public static bool ValidatePngFile(string filePath, out string message)
        {
            try
            {
                if (!filePath.ToLower().EndsWith(".png"))
                {
                    message = "文件必须是 PNG 格式";
                    return false;
                }

                if (!File.Exists(filePath))
                {
                    message = "文件不存在";
                    return false;
                }

                byte[] header = new byte[8];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (fs.Read(header, 0, 8) < 8)
                    {
                        message = "无效的 PNG 文件";
                        return false;
                    }
                }

                byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                for (int i = 0; i < 8; i++)
                {
                    if (header[i] != pngHeader[i])
                    {
                        message = "无效的 PNG 文件";
                        return false;
                    }
                }

                message = "验证成功";
                return true;
            }
            catch (Exception e)
            {
                message = "验证失败: " + e.Message;
                return false;
            }
        }

        /// <summary>清理文件名，移除中文和特殊符号</summary>
        public static string SanitizeFilename(string filename)
        {
            string name = Path.GetFileNameWithoutExtension(filename);
            string ext = Path.GetExtension(filename);

            name = Regex.Replace(name, @"[^a-zA-Z0-9_-]", "_");
            name = Regex.Replace(name, @"_+", "_");
            name = name.Trim('_', '-');

            if (string.IsNullOrEmpty(name) || name.Length < 2)
                name = "skin_" + GenerateShortUid("", 8);

            return name + ext;
        }

        /// <summary>获取资源文件路径（支持打包后的资源）</summary>
        public static string GetResourcePath(string relativePath)
        {
            string baseDir = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            return Path.Combine(baseDir, relativePath);
        }

        /// <summary>获取嵌入资源流</summary>
        public static System.IO.Stream GetEmbeddedResource(string resourceName)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string full = "NpcSkinMaker.Resources." + resourceName;
            return assembly.GetManifestResourceStream(full);
        }
    }
}
