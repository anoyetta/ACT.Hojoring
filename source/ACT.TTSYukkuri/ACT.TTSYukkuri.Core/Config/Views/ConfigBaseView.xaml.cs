using System.Windows.Controls;
using ACT.TTSYukkuri.resources;
using FFXIV.Framework.Globalization;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// ConfigBaseView.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigBaseView : UserControl, ILocalizable
    {
        public static ConfigBaseView Instance { get; private set; }

        public ConfigBaseView()
        {
            Instance = this;
            this.InitializeComponent();
            this.SetLocale(Settings.Default.UILocale);
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        public void SetActivationStatus(
            bool isAllow)
            => this.DenyMessageLabel.Visibility = isAllow ?
                System.Windows.Visibility.Collapsed :
                System.Windows.Visibility.Visible;
    }
}
