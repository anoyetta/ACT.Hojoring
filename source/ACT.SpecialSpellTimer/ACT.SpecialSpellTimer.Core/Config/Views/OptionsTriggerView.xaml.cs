using System.Windows.Controls;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Globalization;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// OptionsTrigger.xaml の相互作用ロジック
    /// </summary>
    public partial class OptionsTriggerView :
        UserControl,
        ILocalizable
    {
        public OptionsTriggerView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Default.UILocale);
            this.LoadConfigViewResources();

            this.Loaded += (_, _) =>
            {
                // InCombatの検出を簡易版に限定する
                this.FrameworkConfig.IsSimplifiedInCombat = true;
            };
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        public Settings Config => Settings.Default;
        public FFXIV.Framework.Config FrameworkConfig => FFXIV.Framework.Config.Instance;
    }
}
