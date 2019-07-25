using System.Windows.Controls;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Globalization;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// OptionsLanguageView.xaml の相互作用ロジック
    /// </summary>
    public partial class OptionsLanguageView :
        UserControl,
        ILocalizable
    {
        public OptionsLanguageView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Default.UILocale);
            this.LoadConfigViewResources();
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        public FFXIV.Framework.Config FrameworkConfig => FFXIV.Framework.Config.Instance;

        public Settings Config => Settings.Default;
    }
}
