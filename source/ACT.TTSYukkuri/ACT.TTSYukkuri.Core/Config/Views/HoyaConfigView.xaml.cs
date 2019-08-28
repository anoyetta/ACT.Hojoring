using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;
using ACT.TTSYukkuri.Config.ViewModels;
using ACT.TTSYukkuri.resources;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// HoyaConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class HoyaConfigView : UserControl, ILocalizable
    {
        public HoyaConfigView(VoicePalettes voicePalette = VoicePalettes.Default)
        {
            InitializeComponent();
            this.DataContext = new HoyaConfigViewModel(voicePalette);

            this.SetLocale(Settings.Default.UILocale);
        }

        public HoyaConfigViewModel ViewModel => this.DataContext as HoyaConfigViewModel;

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
