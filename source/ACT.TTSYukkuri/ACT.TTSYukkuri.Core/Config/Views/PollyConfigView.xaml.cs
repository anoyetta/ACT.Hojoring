using System.Windows.Controls;
using ACT.TTSYukkuri.Config.ViewModels;
using ACT.TTSYukkuri.resources;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// PollyConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class PollyConfigView :
        UserControl,
        ILocalizable
    {
        public PollyConfigView(VoicePalettes voicePalette = VoicePalettes.Default)
        {
            this.InitializeComponent();
            this.DataContext = new PollyConfigViewModel(voicePalette);

            this.SetLocale(Settings.Default.UILocale);
        }

        public PollyConfigViewModel ViewModel => this.DataContext as PollyConfigViewModel;

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);
    }
}
