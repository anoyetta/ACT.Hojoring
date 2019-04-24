using System.Windows.Controls;
using ACT.UltraScouter.resources;
using FFXIV.Framework.Globalization;

namespace ACT.UltraScouter.Config.UI.Views
{
    /// <summary>
    /// TacticalRadarView.xaml の相互作用ロジック
    /// </summary>
    public partial class TacticalRadarConfigView : Page, ILocalizable
    {
        public TacticalRadarConfigView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Instance.UILocale);
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);
    }
}
