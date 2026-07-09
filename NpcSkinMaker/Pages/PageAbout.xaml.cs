using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace NpcSkinMaker
{
    /// <summary>关于页</summary>
    public partial class PageAbout : UserControl
    {
        public PageAbout()
        {
            InitializeComponent();

            var s = MainWindow.Instance.Settings;
            LabVersion.Text = "版本 " + s.Version;
            LabAuthor.Text = "作者: " + s.Author;

            LinkPCL.NavigateUri = new System.Uri(s.PclRepoUrl);
            LinkRepo.NavigateUri = new System.Uri(s.RepoUrl);

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
