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
    /// GoogleCloudTextToSpeechConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class GoogleCloudTextToSpeechConfigView : UserControl, ILocalizable
    {
        public GoogleCloudTextToSpeechConfigView(VoicePalettes voicePalette = VoicePalettes.Default)
        {
            this.InitializeComponent();
            this.DataContext = new GoogleCloudTextToSpeechConfigViewModel(voicePalette);

            this.SetLocale(Settings.Default.UILocale);
        }

        public GoogleCloudTextToSpeechConfigViewModel ViewModel => this.DataContext as GoogleCloudTextToSpeechConfigViewModel;

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
