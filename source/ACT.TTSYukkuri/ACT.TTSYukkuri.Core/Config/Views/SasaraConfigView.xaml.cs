using System.Windows.Controls;
using ACT.TTSYukkuri.Config.ViewModels;
using ACT.TTSYukkuri.resources;
using FFXIV.Framework.Globalization;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// SasaraConfigViewxaml.xaml の相互作用ロジック
    /// </summary>
    public partial class SasaraConfigView : UserControl, ILocalizable
    {
        public SasaraConfigView()
        {
            this.InitializeComponent();
            this.DataContext = new SasaraConfigViewModel();

            this.SetLocale(Settings.Default.UILocale);
        }

        public SasaraConfigViewModel ViewModel => this.DataContext as SasaraConfigViewModel;

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);
    }
}
