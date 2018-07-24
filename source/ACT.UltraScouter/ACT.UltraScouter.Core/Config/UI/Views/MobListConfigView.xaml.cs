using System.Windows.Controls;
using ACT.UltraScouter.Config.UI.ViewModels;
using ACT.UltraScouter.resources;
using FFXIV.Framework.Globalization;

namespace ACT.UltraScouter.Config.UI.Views
{
    /// <summary>
    /// GeneralView.xaml の相互作用ロジック
    /// </summary>
    public partial class MobListConfigView : Page, ILocalizable
    {
        public MobListConfigView()
        {
            this.InitializeComponent();

            this.SetLocale(Settings.Instance.UILocale);

            this.DisplayCountSlider.ValueChanged += (s, e) =>
            {
                var vm = this.DataContext as MobListConfigViewModel;
                if (vm != null)
                {
                    vm.RefreshMobListCommand.Execute(null);
                }
            };
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);
    }
}
