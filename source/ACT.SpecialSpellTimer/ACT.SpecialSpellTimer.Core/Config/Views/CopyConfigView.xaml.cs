using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ACT.SpecialSpellTimer.Config.Models;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.WPF.Views;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// TagView.xaml の相互作用ロジック
    /// </summary>
    public partial class CopyConfigView :
        Window,
        ILocalizable,
        INotifyPropertyChanged
    {
        public CopyConfigView(
            ITreeItem source)
        {
            this.SourceConfig = source;

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

            this.SetupTreeSource();

            this.RaisePropertyChanged(nameof(this.IsSpell));
            this.RaisePropertyChanged(nameof(this.IsTicker));

            if (this.IsSpell)
            {
                this.DestinationTab.SelectedItem = this.TagSpellTab;
            }

            if (this.IsTicker)
            {
                this.DestinationTab.SelectedItem = this.TagTickerTab;
            }
        }

        private void ApplyButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            void ifdo(bool b, Action a)
            {
                if (b)
                {
                    a.Invoke();
                }
            }

            void copySpell(Spell d, Spell s)
            {
                ifdo(this.CopyFont, () => d.Font = s.Font.Clone() as FontInfo);
                ifdo(this.CopyFontFill, () => d.FontColor = s.FontColor);
                ifdo(this.CopyFontStrokke, () => d.FontOutlineColor = s.FontOutlineColor);
                ifdo(this.CopyFontWarningFill, () => d.WarningFontColor = s.WarningFontColor);
                ifdo(this.CopyFontWarningStroke, () => d.WarningFontOutlineColor = s.WarningFontOutlineColor);
                ifdo(this.CopyProgressBarSize, () => d.BarHeight = s.BarHeight);
                ifdo(this.CopyProgressBarSize, () => d.BarWidth = s.BarWidth);
                ifdo(this.CopyProgressBarFill, () => d.BarColor = s.BarColor);
                ifdo(this.CopyProgressBarStroke, () => d.BarOutlineColor = s.BarOutlineColor);
                ifdo(this.CopyBackground, () => d.BackgroundColor = s.BackgroundColor);
                ifdo(this.CopyBackground, () => d.BackgroundAlpha = s.BackgroundAlpha);
                ifdo(this.CopyIconSize, () => d.SpellIconSize = s.SpellIconSize);
                ifdo(this.CopyIconOverlapRecastTime, () => d.OverlapRecastTime = s.OverlapRecastTime);
                ifdo(this.CopyIconToDarkness, () => d.ReduceIconBrightness = s.ReduceIconBrightness);
                ifdo(this.CopyIconHideSpellName, () => d.HideSpellName = s.HideSpellName);
            }

            void copyTicker(Ticker d, Ticker s)
            {
                ifdo(this.CopyFont, () => d.Font = s.Font.Clone() as FontInfo);
                ifdo(this.CopyFontFill, () => d.FontColor = s.FontColor);
                ifdo(this.CopyFontStrokke, () => d.FontOutlineColor = s.FontOutlineColor);
                ifdo(this.CopyBackground, () => d.BackgroundColor = s.BackgroundColor);
                ifdo(this.CopyBackground, () => d.BackgroundAlpha = s.BackgroundAlpha);
                ifdo(this.CopyX, () => d.Left = s.Left);
                ifdo(this.CopyY, () => d.Top = s.Top);
            }

            if (this.IsSpell)
            {
                var src = this.SourceConfig as Spell;

                foreach (var item in this.Spells)
                {
                    switch (item)
                    {
                        case SpellPanel panel:
                            foreach (Spell spell in panel.Children)
                            {
                                if (spell.IsChecked)
                                {
                                    copySpell(spell, src);
                                }
                            }
                            break;

                        case Spell spell:
                            if (spell.IsChecked)
                            {
                                copySpell(spell, src);
                            }
                            break;
                    }

                    (item as ITreeItem).IsChecked = false;
                }
            }

            if (this.IsTicker)
            {
                var src = this.SourceConfig as Ticker;

                foreach (var item in this.Tickers)
                {
                    if (item is Ticker ticker)
                    {
                        if (ticker.IsChecked)
                        {
                            copyTicker(ticker, src);
                        }
                    }

                    (item as ITreeItem).IsChecked = false;
                }
            }

            this.Close();

            ModernMessageBox.ShowDialog(
                "Done!",
                "ACT.Hojoring");
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var data = (e.Source as CheckBox)?.DataContext;
            if (data is ITreeItem item)
            {
                if (item.Children != null)
                {
                    foreach (ITreeItem child in item.Children)
                    {
                        child.IsChecked = true;
                    }
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var data = (e.Source as CheckBox)?.DataContext;
            if (data is ITreeItem item)
            {
                if (item.Children != null)
                {
                    foreach (ITreeItem child in item.Children)
                    {
                        child.IsChecked = false;
                    }
                }
            }
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        public ITreeItem SourceConfig { get; private set; } = null;

        public string SourceName
        {
            get
            {
                var text = string.Empty;

                switch (this.SourceConfig)
                {
                    case Spell s:
                        text = s.Panel?.PanelName + " - " + s.SpellTitle;
                        break;

                    case Ticker t:
                        text = t.Title;
                        break;
                }

                return text;
            }
        }

        public bool IsSpell => this.SourceConfig is Spell;

        public bool IsTicker => this.SourceConfig is Ticker;

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

        private CollectionViewSource tagsSource = new CollectionViewSource()
        {
            Source = TagTable.Instance.Tags,
            IsLiveFilteringRequested = true,
            IsLiveSortingRequested = true,
        };

        public ICollectionView Spells => this.spellsSource.View;

        public ICollectionView Tickers => this.tickersSource.View;

        public ICollectionView Tags => this.tagsSource.View;

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

            this.tagsSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription()
                {
                    PropertyName = nameof(Ticker.SortPriority),
                    Direction = ListSortDirection.Ascending,
                },
                new SortDescription()
                {
                    PropertyName = nameof(ACT.SpecialSpellTimer.Models.Tag.Name),
                    Direction = ListSortDirection.Ascending,
                },
                new SortDescription()
                {
                    PropertyName = nameof(ACT.SpecialSpellTimer.Models.Tag.ID),
                    Direction = ListSortDirection.Ascending,
                },
            });

            this.RaisePropertyChanged(nameof(this.Spells));
            this.RaisePropertyChanged(nameof(this.Tickers));
            this.RaisePropertyChanged(nameof(this.Tags));
        }

        #region Copy設定

        public bool CopyFont { get; set; }
        public bool CopyFontFill { get; set; }
        public bool CopyFontStrokke { get; set; }
        public bool CopyFontWarningFill { get; set; }
        public bool CopyFontWarningStroke { get; set; }
        public bool CopyProgressBarSize { get; set; }
        public bool CopyProgressBarFill { get; set; }
        public bool CopyProgressBarStroke { get; set; }
        public bool CopyBackground { get; set; }
        public bool CopyIconSize { get; set; }
        public bool CopyIconOverlapRecastTime { get; set; }
        public bool CopyIconToDarkness { get; set; }
        public bool CopyIconHideSpellName { get; set; }
        public bool CopyX { get; set; }
        public bool CopyY { get; set; }

        #endregion Copy設定

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
