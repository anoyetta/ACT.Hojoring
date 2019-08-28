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
    public partial class SAPI5ConfigView :
        UserControl,
        ILocalizable
    {
        public SAPI5ConfigView(VoicePalettes voicePalette = VoicePalettes.Default)
        {
            this.InitializeComponent();
            this.DataContext = new SAPI5ConfigViewModel(voicePalette);

            this.SetLocale(Settings.Default.UILocale);
        }

        public SAPI5ConfigViewModel ViewModel => this.DataContext as SAPI5ConfigViewModel;

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);
    }
}
