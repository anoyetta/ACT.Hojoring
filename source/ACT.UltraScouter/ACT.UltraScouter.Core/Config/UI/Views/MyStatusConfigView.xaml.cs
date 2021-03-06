using System.Windows.Controls;
using ACT.UltraScouter.resources;
using FFXIV.Framework.Globalization;

namespace ACT.UltraScouter.Config.UI.Views
{
    /// <summary>
    /// MyStatusConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class MyStatusConfigView : Page, ILocalizable
    {
        public MyStatusConfigView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Instance.UILocale);
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);
    }
}
