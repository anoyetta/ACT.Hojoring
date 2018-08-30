using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FFXIV.Framework.WPF.Views
{
    /// <summary>
    /// NotSupportedView.xaml の相互作用ロジック
    /// </summary>
    public partial class NotSupportedView : UserControl
    {
        public NotSupportedView()
        {
            this.InitializeComponent();

#if !DEBUG
            this.Background = System.Windows.Media.Brushes.Transparent;
#endif
        }

        public static void AddAndShow(
            System.Windows.Forms.TabPage pluginTabPage)
        {
            pluginTabPage.Controls.Add(new System.Windows.Forms.Integration.ElementHost()
            {
                Child = new NotSupportedView(),
                Dock = System.Windows.Forms.DockStyle.Fill,
            });
        }

        private async void Hyperlink_RequestNavigate(
            object sender,
            System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)));
            e.Handled = true;
        }
    }
}
