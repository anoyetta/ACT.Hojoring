using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;
using ACT.TTSYukkuri.Config.ViewModels;
using ACT.TTSYukkuri.resources;
using FFXIV.Framework.Globalization;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// YukkuriConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class YukkuriConfigView : UserControl, ILocalizable
    {
        public YukkuriConfigView()
        {
            this.InitializeComponent();
            this.DataContext = new YukkuriConfigViewModel();

            this.SetLocale(Settings.Default.UILocale);
        }

        public YukkuriConfigViewModel ViewModel => this.DataContext as YukkuriConfigViewModel;

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        private async void OnRequestNavigate(
            object sender,
            RequestNavigateEventArgs e)
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)));
            e.Handled = true;
        }
    }
}
