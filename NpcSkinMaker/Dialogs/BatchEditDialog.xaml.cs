using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NpcSkinMaker
{
    /// <summary>
    /// 批量编辑皮肤对话框
    /// </summary>
    public partial class BatchEditDialog : Window
    {
        public BatchEditDialog()
        {
            InitializeComponent();

            BtnClose.Click += (s, e) => DialogAnimationHelper.PlayExitAnimationAndClose(this);
            BtnConfirm.Click += (s, e) => Confirm();
            BtnCancel.Click += (s, e) => { DialogResult = false; DialogAnimationHelper.PlayExitAnimationAndClose(this); };

            // 动画 + 标题栏拖拽 + 圆角裁剪
            DialogAnimationHelper.Setup(this);
        }

        private void Confirm()
        {
            string name = TxtName.GetText().Trim();
            string author = TxtAuthor.GetText().Trim();

            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(author))
            {
                MyMsgBox.Show("请至少填写一个字段", "错误", MyMsgBox.MsgType.Warning);
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
