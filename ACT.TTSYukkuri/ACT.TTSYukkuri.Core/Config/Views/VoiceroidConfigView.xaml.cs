using System.Windows.Controls;
using ACT.TTSYukkuri.Config.ViewModels;
using ACT.TTSYukkuri.resources;
using FFXIV.Framework.Globalization;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// VoiceroidConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class VoiceroidConfigView : UserControl, ILocalizable
    {
        public VoiceroidConfigView()
        {
            this.InitializeComponent();
            this.DataContext = new VoiceroidConfigViewModel();

            this.SetLocale(Settings.Default.UILocale);
        }

        public VoiceroidConfigViewModel ViewModel => this.DataContext as VoiceroidConfigViewModel;

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);
    }
}
