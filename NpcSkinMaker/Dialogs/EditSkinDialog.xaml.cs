using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NpcSkinMaker
{
    /// <summary>
    /// 编辑皮肤对话框
    /// </summary>
    public partial class EditSkinDialog : Window
    {
        public EditSkinDialog(SkinData skinData)
        {
            InitializeComponent();

            TxtName.Text = (skinData != null ? skinData.Name : null) ?? "";
            TxtAuthor.Text = (skinData != null ? skinData.Author : null) ?? "";

            BtnClose.Click += (s, e) => DialogAnimationHelper.PlayExitAnimationAndClose(this);
            BtnConfirm.Click += (s, e) => Confirm();
            BtnCancel.Click += (s, e) => { DialogResult = false; DialogAnimationHelper.PlayExitAnimationAndClose(this); };

            // 动画 + 标题栏拖拽 + 圆角裁剪
            DialogAnimationHelper.Setup(this);
        }

        private void Confirm()
        {
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

        public void GetData(out string name, out string author)
        {
            name = TxtName.GetText().Trim();
            author = TxtAuthor.GetText().Trim();
        }
    }
}
