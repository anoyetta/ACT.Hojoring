using System.Windows;
using System.Windows.Controls;
using ACT.UltraScouter.Config.UI.ViewModels;
using ACT.UltraScouter.resources;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;

namespace ACT.UltraScouter.Config.UI.Views
{
    /// <summary>
    /// ConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigView : UserControl, ILocalizable
    {
        #region Singleton

        private static ConfigView instance;
        public static ConfigView Instance => instance;

        #endregion Singleton

        public ConfigView()
        {
            instance = this;

            this.InitializeComponent();
            this.SetLocale(Settings.Instance.UILocale);
            this.DataContext = new ConfigViewModel();

            this.MenuTreeView.SelectedItemChanged += this.MenuTreeView_SelectedItemChanged;
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        public ConfigViewModel ViewModel => this.DataContext as ConfigViewModel;

        private void MenuTreeView_SelectedItemChanged(
            object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            var src = e.NewValue as TreeSource;
            if (src == null)
            {
                return;
            }

            src.IsExpanded = true;

            if (src.Content != null)
            {
                var page = src.Content as Page;
                page.FontFamily = this.FontFamily;
                page.FontSize = this.FontSize;
                this.ContentFrame.Navigate(src.Content);
            }
        }
    }
}
