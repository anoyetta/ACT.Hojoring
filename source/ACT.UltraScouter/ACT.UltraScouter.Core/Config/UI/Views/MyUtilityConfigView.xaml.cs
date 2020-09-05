using System.Windows.Controls;
using System.Windows.Input;
using ACT.UltraScouter.resources;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;

namespace ACT.UltraScouter.Config.UI.Views
{
    /// <summary>
    /// MyUtilityConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class MyUtilityConfigView : Page, ILocalizable
    {
        public MyUtilityConfigView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Instance.UILocale);
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        private void KeyCaptureTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var shortcut = (sender as TextBox).Tag as KeyShortcut;
            shortcut.Key = e.Key;
            e.Handled = true;
        }
    }
}
