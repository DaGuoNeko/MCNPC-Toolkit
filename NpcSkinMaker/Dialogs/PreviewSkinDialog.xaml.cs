using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NpcSkinMaker
{
    /// <summary>
    /// 预览皮肤对话框
    /// </summary>
    public partial class PreviewSkinDialog : Window
    {
        public PreviewSkinDialog(SkinData skinData)
        {
            InitializeComponent();

            LabTitle.Text = "预览皮肤 - " + (skinData != null ? (skinData.Name ?? "") : "");
            LabName.Text = "人物名称: " + (skinData != null ? (skinData.Name ?? "") : "");
            LabAuthor.Text = "作者: " + (skinData != null ? (skinData.Author ?? "") : "");

            if (!string.IsNullOrEmpty(skinData != null ? skinData.TexturePath : null) && File.Exists(skinData.TexturePath))
            {
                try
                {
                    ImgPreview.Source = new BitmapImage(new Uri(skinData.TexturePath));
                }
                catch (Exception ex)
                {
                    Logger.Error("加载贴图失败: " + ex.Message);
                }
            }

            // 标题栏拖拽由 DialogAnimationHelper 处理
            BtnClose.Click += (s, e) => DialogAnimationHelper.PlayExitAnimationAndClose(this);
            BtnCloseBottom.Click += (s, e) => DialogAnimationHelper.PlayExitAnimationAndClose(this);

            // 动画 + 标题栏拖拽 + 圆角裁剪
            DialogAnimationHelper.Setup(this);
        }
    }
}
