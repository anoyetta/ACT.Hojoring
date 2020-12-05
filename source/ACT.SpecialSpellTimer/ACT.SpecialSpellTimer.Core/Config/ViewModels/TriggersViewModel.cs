using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using ACT.SpecialSpellTimer.Config.Models;
using ACT.SpecialSpellTimer.Models;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Config.ViewModels
{
    public class TriggersViewModel :
        BindableBase
    {
        public TriggersViewModel()
        {
            this.SetupTreeSource();
        }

        public Settings RootConfig => Settings.Default;

        private readonly CollectionViewSource spellsSource = new CollectionViewSource()
        {
            Source = SpellPanelTable.Instance.Table,
            IsLiveFilteringRequested = true,
            IsLiveSortingRequested = true,
        };

        private readonly CollectionViewSource tickersSource = new CollectionViewSource()
        {
            Source = TickerTable.Instance.Table,
            IsLiveFilteringRequested = true,
            IsLiveSortingRequested = true,
        };

        private readonly CollectionViewSource tagsSource = new CollectionViewSource()
        {
            Source = TagTable.Instance.Tags,
            IsLiveFilteringRequested = true,
            IsLiveSortingRequested = true,
        };

        public TriggersTreeRoot TagsTreeRoot { get; private set; }
        public TriggersTreeRoot SpellsTreeRoot { get; private set; }
        public TriggersTreeRoot TickersTreeRoot { get; private set; }

        public ICollectionView Spells => this.spellsSource.View;
        public ICollectionView Tickers => this.tickersSource.View;
        public ICollectionView Tags => this.tagsSource.View;

        private void SetupTreeSource()
        {
            var spells = new TriggersTreeRoot(
                ItemTypes.SpellsRoot,
                "All Spells",
                this.spellsSource.View);

            var tickers = new TriggersTreeRoot(
                ItemTypes.TickersRoot,
                "All Tickers",
                this.tickersSource.View);

            var tags = new TriggersTreeRoot(
                ItemTypes.TagsRoot,
                "Tags",
                this.tagsSource.View);

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
                    PropertyName = nameof(Tag.Name),
                    Direction = ListSortDirection.Ascending,
                },
                new SortDescription()
                {
                    PropertyName = nameof(Tag.ID),
                    Direction = ListSortDirection.Ascending,
                },
            });

            this.SpellsTreeRoot = spells;
            this.TickersTreeRoot = tickers;
            this.TagsTreeRoot = tags;
        }
    }
}
