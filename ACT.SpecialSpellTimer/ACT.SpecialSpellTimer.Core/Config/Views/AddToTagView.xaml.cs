using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Globalization;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// AddToTagView.xaml の相互作用ロジック
    /// </summary>
    public partial class AddToTagView :
        Window,
        ILocalizable,
        INotifyPropertyChanged
    {
        public AddToTagView() : this(new Tag()
        {
            Name = "DUMMY TAG"
        })
        {
        }

        public AddToTagView(
            Tag targetTag)
        {
            this.TargetTag = targetTag;

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
                    this.Close();
                }
            };

            this.CloseButton.Click += (x, y) =>
            {
                this.Close();
            };

            this.ApplyButton.Click += this.ApplyButton_Click;

            foreach (var item in SpellPanelTable.Instance.Table)
            {
                item.IsChecked = false;
            }

            foreach (var item in SpellTable.Instance.Table)
            {
                item.IsChecked = false;
            }

            foreach (var item in TickerTable.Instance.Table)
            {
                item.IsChecked = false;
            }

            this.SetupTreeSource();
        }

        private void ApplyButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var items = new List<ItemTags>();

            foreach (var item in this.Spells)
            {
                if (item is SpellPanel panel)
                {
                    if (!panel.IsChecked)
                    {
                        continue;
                    }

                    if (!TagTable.Instance.ItemTags.Any(x =>
                        x.ItemID == panel.ID &&
                        x.TagID == this.TargetTag.ID))
                    {
                        items.Add(new ItemTags(panel.ID, this.TargetTag.ID));
                    }
                }

                if (item is Spell spell)
                {
                    if (!spell.IsChecked)
                    {
                        continue;
                    }

                    if (spell.Panel?.IsChecked ?? false)
                    {
                        continue;
                    }

                    if (!TagTable.Instance.ItemTags.Any(x =>
                        x.ItemID == spell.Guid &&
                        x.TagID == this.TargetTag.ID))
                    {
                        items.Add(new ItemTags(spell.Guid, this.TargetTag.ID));
                    }
                }
            }

            foreach (var item in this.Tickers)
            {
                var ticker = item as Ticker;
                if (!ticker.IsChecked)
                {
                    continue;
                }

                if (!TagTable.Instance.ItemTags.Any(x =>
                    x.ItemID == ticker.Guid &&
                    x.TagID == this.TargetTag.ID))
                {
                    items.Add(new ItemTags(ticker.Guid, this.TargetTag.ID));
                }
            }

            TagTable.Instance.ItemTags.AddRange(items);
            TagTable.Instance.Save();

            this.Close();
        }

        public Tag TargetTag { get; set; }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        private CollectionViewSource spellsSource = new CollectionViewSource()
        {
            Source = SpellPanelTable.Instance.Table,
            IsLiveFilteringRequested = true,
            IsLiveSortingRequested = true,
        };

        private CollectionViewSource tickersSource = new CollectionViewSource()
        {
            Source = TickerTable.Instance.Table,
            IsLiveFilteringRequested = true,
            IsLiveSortingRequested = true,
        };

        public ICollectionView Spells => this.spellsSource.View;
        public ICollectionView Tickers => this.tickersSource.View;

        private void SetupTreeSource()
        {
            this.spellsSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription()
                {
                    PropertyName = nameof(SpellPanel.SortPriority),
                    Direction = ListSortDirection.Ascending,
                },
                new SortDescription()
                {
                    PropertyName = nameof(SpellPanel.PanelName),
                    Direction = ListSortDirection.Ascending,
                },
                new SortDescription()
                {
                    PropertyName = nameof(SpellPanel.ID),
                    Direction = ListSortDirection.Ascending,
                },
            });

            this.tickersSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription()
                {
                    PropertyName = nameof(Ticker.Title),
                    Direction = ListSortDirection.Ascending,
                },
                new SortDescription()
                {
                    PropertyName = nameof(Ticker.ID),
                    Direction = ListSortDirection.Ascending,
                },
            });

            this.RaisePropertyChanged(nameof(this.Spells));
            this.RaisePropertyChanged(nameof(this.Tickers));
        }

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(
            [CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName]string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

            return true;
        }

        #endregion INotifyPropertyChanged
    }
}
