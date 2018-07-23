using System.Windows.Controls;
using ACT.TTSYukkuri.Config.ViewModels;
using ACT.TTSYukkuri.resources;
using FFXIV.Framework.Globalization;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// BoyomiConfigViewModel.xaml の相互作用ロジック
    /// </summary>
    public partial class BoyomiConfigView : UserControl, ILocalizable
    {
        public BoyomiConfigView()
        {
            this.InitializeComponent();
            this.DataContext = new BoyomiConfigViewModel();

            this.SetLocale(Settings.Default.UILocale);
        }

        public BoyomiConfigViewModel ViewModel => this.DataContext as BoyomiConfigViewModel;

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);
    }
}
