using System.Windows;
using System.Windows.Controls;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Globalization;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// OptionsView.xaml の相互作用ロジック
    /// </summary>
    public partial class OptionsView :
        UserControl,
        ILocalizable
    {
        public OptionsView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Default.UILocale);
            this.LoadConfigViewResources();

            this.Loaded += (x, y) =>
            {
                if (this.MenuTreeView.SelectedItem == null)
                {
                    this.StartingTab.IsSelected = true;
                }
            };
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        private UserControl[] contentViews;

        private UserControl[] ContentViews =>
            this.contentViews ?? (this.contentViews = new UserControl[]
            {
                this.LanguageView,
                this.GeneralView,
                this.VisualView,
                this.TriggerView,
                this.LogFilterView,
                this.LogView,
                this.TTSDictionaryView,
                this.MiscView
            });

        private void TreeView_SelectedItemChanged(
            object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            foreach (var view in this.ContentViews)
            {
                view.Visibility = Visibility.Collapsed;
            }

            this.ContentViews[(e.NewValue as TreeViewItem).TabIndex].Visibility = Visibility.Visible;
        }
    }
}
