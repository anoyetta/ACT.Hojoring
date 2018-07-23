using System.Windows.Controls;
using ACT.TTSYukkuri.Config.ViewModels;
using ACT.TTSYukkuri.resources;
using FFXIV.Framework.Globalization;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// PollyConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class PollyConfigView :
        UserControl,
        ILocalizable
    {
        public PollyConfigView()
        {
            this.InitializeComponent();
            this.DataContext = new PollyConfigViewModel();

            this.SetLocale(Settings.Default.UILocale);
        }

        public PollyConfigViewModel ViewModel => this.DataContext as PollyConfigViewModel;

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);
    }
}
