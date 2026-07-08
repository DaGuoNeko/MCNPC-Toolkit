using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace NpcSkinMaker
{
    /// <summary>
    /// 添加皮肤对话框
    /// </summary>
    public partial class AddSkinDialog : Window
    {
        private string _texturePath = "";

        public AddSkinDialog()
        {
            InitializeComponent();

            BtnClose.Click += (s, e) => DialogAnimationHelper.PlayExitAnimationAndClose(this);
            BtnBrowse.Click += (s, e) => BrowseTexture();
            BtnConfirm.Click += (s, e) => Confirm();
            BtnCancel.Click += (s, e) => { DialogResult = false; DialogAnimationHelper.PlayExitAnimationAndClose(this); };

            // 动画 + 标题栏拖拽 + 圆角裁剪
            DialogAnimationHelper.Setup(this);
        }

        private void BrowseTexture()
        {
            var ofd = new OpenFileDialog
            {
                Title = "选择 PNG 贴图文件",
                Filter = "PNG 文件 (*.png)|*.png|所有文件 (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                string msg;
                if (Utils.ValidatePngFile(ofd.FileName, out msg))
                {
                    _texturePath = ofd.FileName;
                    TxtTexture.Text = ofd.FileName;
                }
                else
                {
                    MyMsgBox.Show("验证失败: " + msg, "错误", MyMsgBox.MsgType.Warning);
                }
            }
        }

        private void Confirm()
        {
            if (string.IsNullOrEmpty(_texturePath))
            {
                MyMsgBox.Show("请选择贴图文件", "错误", MyMsgBox.MsgType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtName.GetText()))
            {
                MyMsgBox.Show("请输入人物名称", "错误", MyMsgBox.MsgType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtAuthor.GetText()))
            {
                MyMsgBox.Show("请输入作者名称", "错误", MyMsgBox.MsgType.Warning);
                return;
            }

            DialogResult = true;
        }

        public void GetData(out string texturePath, out string name, out string author)
        {
            texturePath = _texturePath;
            name = TxtName.GetText().Trim();
            author = TxtAuthor.GetText().Trim();
        }
    }
}
