using System.Windows;
using System.Windows.Controls;
using ACT.UltraScouter.resources;
using ACT.UltraScouter.ViewModels;
using FFXIV.Framework.Globalization;

namespace ACT.UltraScouter.Config.UI.Views
{
    /// <summary>
    /// GeneralView.xaml の相互作用ロジック
    /// </summary>
    public partial class MPTickerConfigView : Page, ILocalizable
    {
        public MPTickerConfigView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Instance.UILocale);

            this.Loaded += this.MPTickerConfigView_Loaded;
        }

        private void MPTickerConfigView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            var vm = this.DataContext as MPTickerViewModel;
            if (vm == null)
            {
                return;
            }

            var config = vm.Config;

            config.OnSyncTargetChanged += (_, _) =>
            {
                try
                {
                    config.IsSuspendPropertyChanged = true;

                    if (config.IsSyncDoT)
                    {
                        config.IsSyncHoT = false;
                        config.IsSyncMP = false;
                    }

                    if (config.IsSyncHoT)
                    {
                        config.IsSyncDoT = false;
                        config.IsSyncMP = false;
                    }

                    if (config.IsSyncMP)
                    {
                        config.IsSyncHoT = false;
                        config.IsSyncDoT = false;
                    }
                }
                finally
                {
                    config.IsSuspendPropertyChanged = false;
                }
            };
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);
    }
}
