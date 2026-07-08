using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace NpcSkinMaker
{
    /// <summary>
    /// 批量添加皮肤对话框
    /// </summary>
    public partial class BatchAddSkinDialog : Window
    {
        private readonly List<string> _textureFiles = new List<string>();

        public BatchAddSkinDialog()
        {
            InitializeComponent();

            BtnClose.Click += (s, e) => DialogAnimationHelper.PlayExitAnimationAndClose(this);
            BtnSelect.Click += (s, e) => SelectFiles();
            BtnClear.Click += (s, e) => ClearFiles();
            BtnConfirm.Click += (s, e) => Confirm();
            BtnCancel.Click += (s, e) => { DialogResult = false; DialogAnimationHelper.PlayExitAnimationAndClose(this); };

            // 动画 + 标题栏拖拽 + 圆角裁剪
            DialogAnimationHelper.Setup(this);
        }

        private void SelectFiles()
        {
            var ofd = new OpenFileDialog
            {
                Title = "选择 PNG 贴图文件（可多选）",
                Filter = "PNG 文件 (*.png)|*.png|所有文件 (*.*)|*.*",
                Multiselect = true
            };

            if (ofd.ShowDialog() != true) return;

            int validCount = 0;
            var invalidFiles = new List<string>();

            foreach (string filePath in ofd.FileNames)
            {
                string msg;
                if (Utils.ValidatePngFile(filePath, out msg))
                {
                    if (!_textureFiles.Contains(filePath))
                    {
                        _textureFiles.Add(filePath);
                        validCount++;
                    }
                }
                else
                {
                    invalidFiles.Add(Path.GetFileName(filePath) + ": " + msg);
                }
            }

            RefreshList();

            if (invalidFiles.Count > 0 && validCount > 0)
                MyMsgBox.Show("成功添加 " + validCount + " 个文件\n\n以下文件无效:\n" + string.Join("\n", invalidFiles.ToArray(), 0, System.Math.Min(5, invalidFiles.Count)),
                    "部分文件无效", MyMsgBox.MsgType.Warning);
            else if (validCount > 0)
                MyMsgBox.Show("成功添加 " + validCount + " 个文件", "成功", MyMsgBox.MsgType.Info);
        }

        private void ClearFiles()
        {
            _textureFiles.Clear();
            RefreshList();
        }

        private void RefreshList()
        {
            PanFileList.Children.Clear();
            foreach (string filePath in _textureFiles)
            {
                var border = new Border
                {
                    Padding = new Thickness(8, 6, 8, 6),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    BorderBrush = (Brush)Application.Current.TryFindResource("ColorBrushGray6")
                };
                var text = new TextBlock
                {
                    Text = Path.GetFileName(filePath),
                    FontSize = 12,
                    Foreground = (Brush)Application.Current.TryFindResource("ColorBrush1"),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                border.Child = text;
                PanFileList.Children.Add(border);
            }
        }

        private void Confirm()
        {
            if (_textureFiles.Count == 0)
            {
                MyMsgBox.Show("请至少选择一个贴图文件", "错误", MyMsgBox.MsgType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtAuthor.GetText()))
            {
                MyMsgBox.Show("请输入作者名称", "错误", MyMsgBox.MsgType.Warning);
                return;
            }

            DialogResult = true;
        }

        public List<SkinEntry> GetData()
        {
            string author = TxtAuthor.GetText().Trim();
            var result = new List<SkinEntry>();

            foreach (string filePath in _textureFiles)
            {
                string fileName = Path.GetFileName(filePath);
                string cleanFileName = Utils.SanitizeFilename(fileName);
                string name = Path.GetFileNameWithoutExtension(cleanFileName);
                result.Add(new SkinEntry(filePath, name, author));
            }

            return result;
        }
    }

    /// <summary>C# 5 皮肤数据返回辅助类</summary>
    public class SkinEntry
    {
        public string TexturePath;
        public string Name;
        public string Author;

        public SkinEntry(string texturePath, string name, string author)
        {
            TexturePath = texturePath;
            Name = name;
            Author = author;
        }
    }
}
