using System.Windows.Controls;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Globalization;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// OptionsGeneralView.xaml の相互作用ロジック
    /// </summary>
    public partial class OptionsGeneralView :
        UserControl,
        ILocalizable
    {
        public OptionsGeneralView()
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
