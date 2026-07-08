using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace NpcSkinMaker
{
    /// <summary>
    /// 关于页
    /// </summary>
    public partial class PageAbout : UserControl
    {
        public PageAbout()
        {
            InitializeComponent();

            LinkPCL.RequestNavigate += OpenUrl;
            LinkRepo.RequestNavigate += OpenUrl;
        }

        private void OpenUrl(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
