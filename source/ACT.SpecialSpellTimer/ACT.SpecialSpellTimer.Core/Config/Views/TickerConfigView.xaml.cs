using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ACT.SpecialSpellTimer.Config.ViewModels;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Globalization;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// TickerConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class TickerConfigView : UserControl, ILocalizable
    {
        public TickerConfigView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Default.UILocale);
            this.LoadConfigViewResources();
        }

        public TickerConfigViewModel ViewModel => this.DataContext as TickerConfigViewModel;

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        private void TextBoxSelect(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!textBox.IsKeyboardFocusWithin)
                {
                    textBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private void TextBoxOnGotFocus(
            object sender,
            RoutedEventArgs e)
        {
            (sender as TextBox)?.SelectAll();
        }

        private void TabControl_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            this.ViewModel.IsActiveVisualTab = this.VisualTab.IsSelected;
        }

        private void FilterExpander_Expanded(object sender, RoutedEventArgs e)
        {
            this.BaseScrollViewer.ScrollToEnd();
        }
    }
}
