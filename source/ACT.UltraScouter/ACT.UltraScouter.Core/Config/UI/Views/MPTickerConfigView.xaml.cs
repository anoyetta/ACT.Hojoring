using System.Windows;
using System.Windows.Controls;
using ACT.UltraScouter.Config.UI.ViewModels;
using ACT.UltraScouter.resources;
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
            var vm = this.DataContext as MPTickerConfigViewModel;
            if (vm == null)
            {
                return;
            }

            var config = vm.MPTicker;

            config.OnSyncTargetChanged += (changedPropertyName) =>
            {
                try
                {
                    config.IsSuspendPropertyChanged = true;

                    switch (changedPropertyName)
                    {
                        case nameof(MPTicker.IsSyncDoT):
                            if (config.IsSyncDoT)
                            {
                                config.IsSyncHoT = false;
                                config.IsSyncMP = false;
                            }

                            break;

                        case nameof(MPTicker.IsSyncHoT):
                            if (config.IsSyncHoT)
                            {
                                config.IsSyncDoT = false;
                                config.IsSyncMP = false;
                            }

                            break;

                        case nameof(MPTicker.IsSyncMP):
                            if (config.IsSyncMP)
                            {
                                config.IsSyncHoT = false;
                                config.IsSyncDoT = false;
                            }

                            break;
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
