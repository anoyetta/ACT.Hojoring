using System.Windows.Controls;
using ACT.TTSYukkuri.Config.ViewModels;
using ACT.TTSYukkuri.resources;
using FFXIV.Framework.Globalization;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// CevioAIConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class CevioAIConfigView : UserControl, ILocalizable
    {
        public CevioAIConfigView()
        {
            this.InitializeComponent();
            this.DataContext = new CevioAIConfigViewModel();

            this.SetLocale(Settings.Default.UILocale);
        }

        public CevioAIConfigViewModel ViewModel => this.DataContext as CevioAIConfigViewModel;

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);
    }
}
