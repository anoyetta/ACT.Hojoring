using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using ACT.SpecialSpellTimer.Config.Models;
using ACT.SpecialSpellTimer.Config.Views;
using ACT.SpecialSpellTimer.Models;
using FFXIV.Framework.XIVHelper;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Config.ViewModels
{
    public partial class TickerConfigViewModel :
        BindableBase
    {
        public TickerConfigViewModel() : this(new Ticker())
        {
        }

        public TickerConfigViewModel(
            Ticker model)
            => this.Model = model;

        private Ticker model;

        public Ticker Model
        {
            get => this.model;
            set
            {
                if (this.SetProperty(ref this.model, value))
                {
                    try
                    {
                        this.isInitialize = true;

                        // ジョブ・ゾーン・前提条件のセレクタを初期化する
                        this.SetJobSelectors();
                        this.SetPartyCompositionSelectors();
                        this.SetExpressionFilter();
                        this.SetZoneSelectors();
                        PreconditionSelectors.Instance.SetModel(this.model);

                        // Designモード？（Visualタブがアクティブか？）
                        this.model.IsDesignMode = this.IsActiveVisualTab;
                        Task.Run(() => TableCompiler.Instance.CompileTickers());
                        this.SwitchDesignGrid();

                        // タグを初期化する
                        this.SetupTagsSource();
                    }
                    finally
                    {
                        this.isInitialize = false;
                    }

                    this.ClearSoundTestCommands();

                    this.RaisePropertyChanged(nameof(this.IsJobFiltered));
                    this.RaisePropertyChanged(nameof(this.IsPartyCompositionFiltered));
                    this.RaisePropertyChanged(nameof(this.IsExpressionFiltered));
                    this.RaisePropertyChanged(nameof(this.IsZoneFiltered));
                    this.RaisePropertyChanged(nameof(this.PreconditionSelectors));
                    this.RaisePropertyChanged(nameof(this.Model.MatchAdvancedConfig));
                    this.RaisePropertyChanged(nameof(this.Model.DelayAdvancedConfig));
                }
            }
        }

        private bool isInitialize = false;

        private ICommand simulateMatchCommand;

        public ICommand SimulateMatchCommand =>
            this.simulateMatchCommand ?? (this.simulateMatchCommand = new DelegateCommand(() =>
            {
                this.Model.SimulateMatch();
            }));

        private bool isActiveVisualTab;

        public bool IsActiveVisualTab
        {
            get => this.isActiveVisualTab;
            set
            {
                if (this.SetProperty(ref this.isActiveVisualTab, value))
                {
                    this.Model.IsDesignMode = this.isActiveVisualTab;
                    Task.Run(() => TableCompiler.Instance.CompileTickers());
                    this.SwitchDesignGrid();
                }
            }
        }

        private void SwitchDesignGrid()
        {
            var showGrid =
                TableCompiler.Instance.SpellList.Any(x => x.IsDesignMode) ||
                TableCompiler.Instance.TickerList.Any(x => x.IsDesignMode);

            Settings.Default.VisibleDesignGrid = showGrid;
        }

        #region Job filter

        public bool IsJobFiltered => !string.IsNullOrEmpty(this.Model?.JobFilter);

        private static List<JobSelector> jobSelectors;

        public List<JobSelector> JobSelectors => jobSelectors;

        private void SetJobSelectors()
        {
            if (jobSelectors == null)
            {
                jobSelectors = new List<JobSelector>();

                foreach (var job in
                    from x in Jobs.List
                    where
                    x.ID != JobIDs.Unknown &&
                    x.ID != JobIDs.ADV
                    orderby
                    x.Role.ToSortOrder(),
                    x.ID
                    select
                    x)
                {
                    jobSelectors.Add(new JobSelector(job));
                }
            }

            var jobFilters = this.Model.JobFilter?.Split(',');
            foreach (var selector in this.JobSelectors)
            {
                if (jobFilters != null)
                {
                    selector.IsSelected = jobFilters.Contains(((int)selector.Job.ID).ToString());
                }

                selector.SelectedChangedDelegate = this.JobFilterChanged;
            }

            this.RaisePropertyChanged(nameof(this.JobSelectors));
        }

        private void JobFilterChanged()
        {
            if (!this.isInitialize)
            {
                this.Model.JobFilter = string.Join(",",
                    this.JobSelectors
                        .Where(x => x.IsSelected)
                        .Select(x => ((int)x.Job.ID).ToString())
                        .ToArray());

                this.RaisePropertyChanged(nameof(this.IsJobFiltered));
                Task.Run(() => TableCompiler.Instance.CompileTickers());
            }
        }

        private ICommand clearJobFilterCommand;

        public ICommand ClearJobFilterCommand =>
            this.clearJobFilterCommand ?? (this.clearJobFilterCommand = new DelegateCommand(() =>
            {
                try
                {
                    this.isInitialize = true;
                    foreach (var selector in this.JobSelectors)
                    {
                        selector.IsSelected = false;
                    }

                    this.Model.JobFilter = string.Empty;
                    this.RaisePropertyChanged(nameof(this.IsJobFiltered));
                    Task.Run(() => TableCompiler.Instance.CompileTickers());
                }
                finally
                {
                    this.isInitialize = false;
                }
            }));

        #endregion Job filter

        #region Party Composition Filter

        public bool IsPartyCompositionFiltered => !string.IsNullOrEmpty(this.Model?.PartyCompositionFilter);

        public PartyComposiotionSelector[] PartyCompositionSelectors { get; } = new[]
        {
            new PartyComposiotionSelector(PartyCompositions.LightParty, "Light Party"),
            new PartyComposiotionSelector(PartyCompositions.FullPartyT1, "Full Party (T1)"),
            new PartyComposiotionSelector(PartyCompositions.FullPartyT2, "Full Party (T2)"),
        };

        private void SetPartyCompositionSelectors()
        {
            var filters = this.Model.PartyCompositionFilter?.Split(',');
            foreach (var selector in this.PartyCompositionSelectors)
            {
                if (filters != null)
                {
                    selector.IsSelected = filters.Contains(selector.Composition.ToString());
                }

                selector.SelectedChangedDelegate = this.PartyCompositionFilterChanged;
            }

            this.RaisePropertyChanged(nameof(this.PartyCompositionSelectors));
        }

        private void PartyCompositionFilterChanged()
        {
            if (!this.isInitialize)
            {
                this.Model.PartyCompositionFilter = string.Join(",",
                    this.PartyCompositionSelectors
                        .Where(x => x.IsSelected)
                        .Select(x => x.Composition.ToString())
                        .ToArray());

                this.RaisePropertyChanged(nameof(this.IsPartyCompositionFiltered));
                Task.Run(() => TableCompiler.Instance.CompileTickers());
            }
        }

        private ICommand clearPartyCompositionFilterCommand;

        public ICommand ClearPartyCompositionFilterCommand =>
            this.clearPartyCompositionFilterCommand ?? (this.clearPartyCompositionFilterCommand = new DelegateCommand(() =>
            {
                try
                {
                    this.isInitialize = true;
                    foreach (var selector in this.PartyCompositionSelectors)
                    {
                        selector.IsSelected = false;
                    }

                    this.Model.PartyCompositionFilter = string.Empty;
                    this.RaisePropertyChanged(nameof(this.IsPartyCompositionFiltered));
                    Task.Run(() => TableCompiler.Instance.CompileTickers());
                }
                finally
                {
                    this.isInitialize = false;
                }
            }));

        #endregion Party Composition Filter

        #region Expression Filter

        public bool IsExpressionFiltered => this.Model?.ExpressionFilters.Any(x => x.IsAvailable) ?? false;

        private void SetExpressionFilter()
        {
            foreach (var f in this.Model.ExpressionFilters)
            {
                f.PropertyChanged += (_, e) =>
                {
                    this.RaisePropertyChanged(nameof(this.IsExpressionFiltered));
                    Task.Run(() => TableCompiler.Instance.CompileTickers());
                };
            }
        }

        #endregion Expression Filter

        #region Zone filter

        public bool IsZoneFiltered => !string.IsNullOrEmpty(this.Model?.ZoneFilter);

        private static List<ZoneSelector> zoneSelectors;

        public List<ZoneSelector> ZoneSelectors => zoneSelectors;

        private void SetZoneSelectors()
        {
            if (zoneSelectors == null ||
                zoneSelectors.Count <= 0)
            {
                zoneSelectors = new List<ZoneSelector>();

                foreach (var zone in
                    from x in XIVPluginHelper.Instance?.ZoneList
                    where
                    x.Rank > 0
                    orderby
                    x.IsAddedByUser ? 0 : 1,
                    x.Rank,
                    x.ID descending
                    select
                    x)
                {
                    var selector = new ZoneSelector(
                        zone.ID.ToString(),
                        zone.Name);

                    zoneSelectors.Add(selector);
                }
            }

            var zoneFilters = this.Model.ZoneFilter?.Split(',');
            foreach (var selector in this.ZoneSelectors)
            {
                if (zoneFilters != null)
                {
                    selector.IsSelected = zoneFilters.Contains(selector.ID);
                }

                selector.SelectedChangedDelegate = this.ZoneFilterChanged;
            }

            this.RaisePropertyChanged(nameof(this.ZoneSelectors));
        }

        private void ZoneFilterChanged()
        {
            if (!this.isInitialize)
            {
                this.Model.ZoneFilter = string.Join(",",
                    this.ZoneSelectors
                        .Where(x => x.IsSelected)
                        .Select(x => x.ID)
                        .ToArray());

                this.RaisePropertyChanged(nameof(this.IsZoneFiltered));
                Task.Run(() => TableCompiler.Instance.CompileTickers());
            }
        }

        private ICommand clearZoneFilterCommand;

        public ICommand ClearZoneFilterCommand =>
            this.clearZoneFilterCommand ?? (this.clearZoneFilterCommand = new DelegateCommand(() =>
            {
                try
                {
                    this.isInitialize = true;
                    foreach (var selector in this.ZoneSelectors)
                    {
                        selector.IsSelected = false;
                    }

                    this.Model.ZoneFilter = string.Empty;
                    this.RaisePropertyChanged(nameof(this.IsZoneFiltered));
                    Task.Run(() => TableCompiler.Instance.CompileTickers());
                }
                finally
                {
                    this.isInitialize = false;
                }
            }));

        #endregion Zone filter

        #region Precondition selector

        public PreconditionSelectors PreconditionSelectors => PreconditionSelectors.Instance;

        private ICommand clearPreconditionsCommand;

        public ICommand ClearPreconditionsCommand =>
            this.clearPreconditionsCommand ?? (this.clearPreconditionsCommand = new DelegateCommand(() =>
            {
                PreconditionSelectors.Instance.ClearSelect();
            }));

        #endregion Precondition selector

        #region Tags

        private ICommand addTagsCommand;

        public ICommand AddTagsCommand =>
            this.addTagsCommand ?? (this.addTagsCommand = new DelegateCommand<Guid?>(targetItemID =>
            {
                if (!targetItemID.HasValue)
                {
                    return;
                }

                new TagView()
                {
                    TargetItemID = targetItemID.Value,
                }.Show();
            }));

        public ICollectionView Tags => this.TagsSource.View;

        private CollectionViewSource TagsSource;

        private void SetupTagsSource()
        {
            this.TagsSource = new CollectionViewSource()
            {
                Source = TagTable.Instance.ItemTags,
                IsLiveFilteringRequested = true,
                IsLiveSortingRequested = true,
            };

            this.TagsSource.Filter += (x, y) =>
                y.Accepted =
                    (y.Item as ItemTags).ItemID == this.Model.Guid;

            this.TagsSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription()
                {
                    PropertyName = "Tag.SortPriority",
                    Direction = ListSortDirection.Ascending
                },
                new SortDescription()
                {
                    PropertyName = "Tag.Name",
                    Direction = ListSortDirection.Ascending
                },
            });

            this.RaisePropertyChanged(nameof(this.Tags));
        }

        #endregion Tags
    }
}
