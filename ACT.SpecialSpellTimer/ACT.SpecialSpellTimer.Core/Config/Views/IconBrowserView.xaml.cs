using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ACT.SpecialSpellTimer.Image;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.Globalization;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// TagView.xaml の相互作用ロジック
    /// </summary>
    public partial class IconBrowserView :
        Window,
        ILocalizable
    {
        public IconBrowserView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Default.UILocale);
            this.LoadConfigViewResources();

            // ウィンドウのスタート位置を決める
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            this.MouseLeftButtonDown += (x, y) => this.DragMove();

            this.PreviewKeyUp += (x, y) =>
            {
                if (y.Key == Key.Escape)
                {
                    this.DialogResult = false;
                    this.Close();
                }
            };

            this.CloseButton.Click += (x, y) =>
            {
                this.DialogResult = false;
                this.Close();
            };

            this.ClearButton.Click += (x, y) =>
            {
                this.SelectedIcon = null;
                this.SelectedIconName = string.Empty;

                this.DialogResult = true;
                this.Close();
            };

            this.Loaded += this.IconBrowserView_Loaded;
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        public IconController.IconFile SelectedIcon { get; set; } = null;

        public string SelectedIconName { get; set; } = string.Empty;

        public IconController.IconFile[] Icons => IconController.Instance.EnumerateIcon();

        public IReadOnlyList<IGrouping<string, IconController.IconFile>> IconGroups => (
            from x in this.Icons
            where
            !string.IsNullOrEmpty(x.DirectoryName)
            group x by
            x.DirectoryName).ToList();

        private async void IconBrowserView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            var selectedGroup = default(IGrouping<string, IconController.IconFile>);

            await Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(this.SelectedIconName))
                {
                    this.SelectedIcon = this.Icons.FirstOrDefault(x =>
                        x.Name == this.SelectedIconName ||
                        x.FullPath.ContainsIgnoreCase(this.SelectedIconName));
                }

                if (this.SelectedIcon != null)
                {
                    selectedGroup = (
                        from x in this.IconGroups
                        where
                        x.Any(y => y == this.SelectedIcon)
                        select
                        x).FirstOrDefault();
                }
            });

            if (this.SelectedIcon == null ||
                selectedGroup == null)
            {
                this.DirectoryListView.SelectedIndex = 0;
                return;
            }

            this.DirectoryListView.SelectedValue = selectedGroup?.Key;
            this.IconsListView.SelectedItem = this.SelectedIcon;

            this.DirectoryListView.ScrollIntoView(this.DirectoryListView.SelectedItem);
            this.IconsListView.ScrollIntoView(this.SelectedIcon);
            this.IconsListView.Focus();
        }

        private void ListViewItem_PreviewMouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            var item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {
                this.SelectItem(item);
            }
        }

        private void ListViewItem_PreviewKeyUp(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var item = sender as ListViewItem;
                if (item != null && item.IsSelected)
                {
                    this.SelectItem(item);
                }
            }
        }

        private void SelectItem(
            ListViewItem item)
        {
            var icon = item.DataContext as IconController.IconFile;
            if (icon != null)
            {
                this.SelectedIcon = icon;
                this.SelectedIconName = icon.Name;

                this.DialogResult = true;
                this.Close();
            }
        }
    }
}
