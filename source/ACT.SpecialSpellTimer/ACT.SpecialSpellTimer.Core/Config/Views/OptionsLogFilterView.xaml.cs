using System.Windows.Controls;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Globalization;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// OptionsLogFilterView.xaml の相互作用ロジック
    /// </summary>
    public partial class OptionsLogFilterView :
        UserControl,
        ILocalizable
    {
        public OptionsLogFilterView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Default.UILocale);
            this.LoadConfigViewResources();
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        public Settings Config => Settings.Default;

        public FFXIV.Framework.Config FrameworkConfig => FFXIV.Framework.Config.Instance;
    }
}
