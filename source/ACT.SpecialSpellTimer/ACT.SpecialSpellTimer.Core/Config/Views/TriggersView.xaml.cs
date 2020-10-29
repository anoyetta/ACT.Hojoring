using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using ACT.SpecialSpellTimer.Config.Models;
using ACT.SpecialSpellTimer.Config.ViewModels;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// TriggersView.xaml の相互作用ロジック
    /// </summary>
    public partial class TriggersView :
        UserControl,
        ILocalizable
    {
        public TriggersView()
        {
            this.InitializeComponent();

            this.SetLocale(Settings.Default.UILocale);
            this.LoadConfigViewResources();

            // タグツリービューへのイベントの割当
            this.TagsTreeView.SelectedItemChanged += this.TriggersTreeViewOnSelectedItemChanged;
            this.TagsTreeView.PreviewKeyUp += this.TriggersTreeViewOnPreviewKeyUp;
            this.TagsTreeView.PreviewMouseRightButtonDown += this.OnPreviewMouseRightButtonDown;

            // スペルツリービューへのイベントの割当
            this.SpellsTreeView.SelectedItemChanged += this.TriggersTreeViewOnSelectedItemChanged;
            this.SpellsTreeView.PreviewKeyUp += this.TriggersTreeViewOnPreviewKeyUp;
            this.SpellsTreeView.PreviewMouseRightButtonDown += this.OnPreviewMouseRightButtonDown;

            // ティッカーツリービューへのイベントの割当
            this.TickersTreeView.SelectedItemChanged += this.TriggersTreeViewOnSelectedItemChanged;
            this.TickersTreeView.PreviewKeyUp += this.TriggersTreeViewOnPreviewKeyUp;
            this.TickersTreeView.PreviewMouseRightButtonDown += this.OnPreviewMouseRightButtonDown;
        }

        public TriggersViewModel ViewModel => this.DataContext as TriggersViewModel;

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        #region コンテンツエリアの切り替え

        private async void TriggersTreeViewOnSelectedItemChanged(
            object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            var treeView = sender as TreeView;

            if (treeView.IsLoaded)
            {
                await WPFHelper.InvokeAsync(() => this.ShowContent(e.NewValue));
            }
        }

        private SpellPanelConfigViewModel spellPanelViewModel;
        private SpellConfigViewModel spellViewModel;
        private TickerConfigViewModel tickerViewModel;

        private object previousModel;

        private void ShowContent(
            object model)
        {
            if (this.previousModel != null)
            {
                if (this.previousModel is Spell s)
                {
                    s.IsRealtimeCompile = false;
                    s.IsDesignMode = false;
                }

                if (this.previousModel is Ticker t)
                {
                    t.IsRealtimeCompile = false;
                    t.IsDesignMode = false;
                }

                if (this.previousModel is SpellPanel)
                {
                    this.spellPanelViewModel.ClearFirstSpellChanged();
                }
            }

            switch (model)
            {
                case SpellPanel panel:
                    if (this.spellPanelViewModel == null)
                    {
                        this.spellPanelViewModel = new SpellPanelConfigViewModel(panel);
                        this.SpellPanelView.DataContext = this.spellPanelViewModel;
                    }
                    else
                    {
                        this.spellPanelViewModel.Model = panel;
                    }

                    this.ContentBorder.BorderBrush = new SolidColorBrush(Colors.DarkViolet);
                    this.SpellPanelView.Visibility = Visibility.Visible;
                    this.SpellView.Visibility = Visibility.Collapsed;
                    this.TickerView.Visibility = Visibility.Collapsed;
                    break;

                case Spell spell:
                    spell.IsRealtimeCompile = true;
                    if (this.spellViewModel == null)
                    {
                        this.spellViewModel = new SpellConfigViewModel(spell);
                        this.SpellView.DataContext = this.spellViewModel;
                    }
                    else
                    {
                        this.spellViewModel.Model = spell;
                    }

                    this.ContentBorder.BorderBrush = new SolidColorBrush(Colors.MediumBlue);
                    this.SpellView.Visibility = Visibility.Visible;
                    this.SpellPanelView.Visibility = Visibility.Collapsed;
                    this.TickerView.Visibility = Visibility.Collapsed;
                    break;

                case Ticker ticker:
                    ticker.IsRealtimeCompile = true;
                    if (this.tickerViewModel == null)
                    {
                        this.tickerViewModel = new TickerConfigViewModel(ticker);
                        this.TickerView.DataContext = this.tickerViewModel;
                    }
                    else
                    {
                        this.tickerViewModel.Model = ticker;
                    }

                    this.ContentBorder.BorderBrush = new SolidColorBrush(Colors.OliveDrab);
                    this.TickerView.Visibility = Visibility.Visible;
                    this.SpellPanelView.Visibility = Visibility.Collapsed;
                    this.SpellView.Visibility = Visibility.Collapsed;
                    break;

                default:
                    this.ContentBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
                    this.SpellPanelView.Visibility = Visibility.Collapsed;
                    this.SpellView.Visibility = Visibility.Collapsed;
                    this.TickerView.Visibility = Visibility.Collapsed;
                    break;
            }

            this.previousModel = model;
        }

        #endregion コンテンツエリアの切り替え

        #region キーボードショートカット

        private void TriggersTreeViewOnPreviewKeyUp(
            object sender,
            KeyEventArgs e)
        {
            if ((sender as TreeView)?.SelectedItem is not TreeItemBase item)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.F2:
                    this.inEditNameMode = true;
                    item.RenameCommand.Execute(item);
                    break;

                case Key.Delete:
                    if (!this.inEditNameMode)
                    {
                        item.DeleteCommand.Execute(item);
                    }
                    break;
            }
        }

        #endregion キーボードショートカット

        #region 右クリックで選択アイテムを変更する

        private static TreeViewItem VisualUpwardSearch(
            DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            return source as TreeViewItem;
        }

        private void OnPreviewMouseRightButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }

        #endregion 右クリックで選択アイテムを変更する

        #region 名前の編集

        private bool inEditNameMode = false;

        private void RenameTextBoxOnLostFocus(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is TextBox t)
            {
                if (t.Tag is Tag tag)
                {
                    this.inEditNameMode = false;
                    tag.IsInEditMode = false;
                    tag.Name = t.Text;
                }
            }
        }

        private void RenameTextBoxOnKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Escape)
            {
                if (sender is TextBox t &&
                    t.Tag is Tag tag)
                {
                    this.inEditNameMode = false;
                    tag.IsInEditMode = false;

                    if (e.Key == Key.Enter)
                    {
                        tag.Name = t.Text;
                    }

                    if (e.Key == Key.Escape)
                    {
                        t.Text = tag.Name;
                    }
                }
            }
        }

        private void RenameTextBoxOnIsVisibleChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (sender is TextBox t)
            {
                t.SelectAll();
                t.Focus();
            }
        }

        #endregion 名前の編集

        #region 動的コンテキストメニュー

        private void ContextMenu_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            const string TagMenuItemName = "TagMenu";
            const string PanelMenuItemName = "PanelMenu";

            var context = sender as ContextMenu;
            var data = context?.DataContext as ITreeItem;

            var tagMenuItem = default(MenuItem);
            var panelMenuItem = default(MenuItem);

            foreach (var item in context.Items)
            {
                if (item is MenuItem menuItem)
                {
                    if (menuItem.Name == TagMenuItemName)
                    {
                        tagMenuItem = menuItem;
                    }

                    if (menuItem.Name == PanelMenuItemName)
                    {
                        panelMenuItem = menuItem;
                    }
                }
            }

            if (tagMenuItem == null &&
                panelMenuItem == null)
            {
                return;
            }

            if (tagMenuItem != null)
            {
                tagMenuItem.Items.Clear();

                var tags =
                    from x in TagTable.Instance.Tags
                    orderby
                    x.SortPriority ascending,
                    x.Name
                    select
                    x;

                foreach (var tag in tags)
                {
                    var menuItem = new MenuItem()
                    {
                        Header = tag.Name
                    };

                    menuItem.Click += (x, y) =>
                    {
                        if (!TagTable.Instance.ItemTags.Any(z =>
                            z.ItemID == data.GetID() &&
                            z.TagID == tag.GetID()))
                        {
                            TagTable.Instance.ItemTags.Add(new ItemTags(
                                data.GetID(),
                                tag.GetID()));
                        }
                    };

                    tagMenuItem.Items.Add(menuItem);
                }
            }

            if (panelMenuItem != null)
            {
                panelMenuItem.Items.Clear();

                var panels =
                    from x in SpellPanelTable.Instance.Table
                    orderby
                    x.SortPriority ascending,
                    x.PanelName
                    select
                    x;

                foreach (var panel in panels)
                {
                    var menuItem = new MenuItem()
                    {
                        Header = panel.PanelName
                    };

                    menuItem.Click += (x, y) =>
                    {
                        if (data is Spell spell)
                        {
                            var oldPanel = spell.Panel;
                            spell.PanelID = panel.ID;

                            oldPanel.SetupChildrenSource();
                            panel.SetupChildrenSource();
                        }
                    };

                    panelMenuItem.Items.Add(menuItem);
                }
            }
        }

        #endregion 動的コンテキストメニュー

        /// <summary>
        /// シミュレータを開くボタン click
        /// </summary>
        /// <param name="sender">イベント発生元</param>
        /// <param name="e">イベント引数</param>
        private void OpenSimulatorButton_Click(object sender, RoutedEventArgs e)
        {
            var view = new TriggerTesterView();

            // これがないとTextBoxに半角が入力できない
            ElementHost.EnableModelessKeyboardInterop(view);

            view.Show();
        }
    }
}
