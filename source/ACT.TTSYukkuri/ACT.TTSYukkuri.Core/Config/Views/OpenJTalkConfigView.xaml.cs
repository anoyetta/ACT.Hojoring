using System.Windows.Controls;
using ACT.TTSYukkuri.Config.ViewModels;
using ACT.TTSYukkuri.resources;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// OpenJTalkConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class OpenJTalkConfigView : UserControl, ILocalizable
    {
        public OpenJTalkConfigView(VoicePalettes voicePalette = VoicePalettes.Default)
        {
            this.InitializeComponent();
            this.DataContext = new OpenJTalkConfigViewModel(voicePalette);

            this.SetLocale(Settings.Default.UILocale);
        }

        public OpenJTalkConfigViewModel ViewModel => this.DataContext as OpenJTalkConfigViewModel;

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);
    }
}
