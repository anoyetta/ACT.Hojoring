using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ACT.SpecialSpellTimer.Models;
using FFXIV.Framework.Common;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Config.Models
{
    public class PreconditionSelectors :
        BindableBase
    {
        #region Singleton

        private static PreconditionSelectors instance;

        public static PreconditionSelectors Instance => instance ?? (instance = new PreconditionSelectors());

        #endregion Singleton

        public ObservableCollection<PreconditionSelector> ActiveSpells
        {
            get;
            private set;
        } = new ObservableCollection<PreconditionSelector>();

        public ObservableCollection<PreconditionSelector> InactiveSpells
        {
            get;
            private set;
        } = new ObservableCollection<PreconditionSelector>();

        public ObservableCollection<PreconditionSelector> ActiveTickers
        {
            get;
            private set;
        } = new ObservableCollection<PreconditionSelector>();

        public ObservableCollection<PreconditionSelector> InactiveTickers
        {
            get;
            private set;
        } = new ObservableCollection<PreconditionSelector>();

        public PreconditionSelectors()
        {
            this.RefreshSpells();
            this.RefreshTickers();

            SpellTable.Instance.Table.CollectionChanged += (x, y) =>
            {
                this.RefreshSpells();
                this.RefreshSelected();
            };

            TickerTable.Instance.Table.CollectionChanged += (x, y) =>
            {
                this.RefreshTickers();
                this.RefreshSelected();
            };
        }

        private bool isInitialize;

        private ITrigger model;
        private Spell Spell => model as Spell;
        private Ticker Ticker => model as Ticker;

        public bool IsFiltered =>
            (this.Spell?.TimersMustRunningForStart.Any() ?? false) ||
            (this.Spell?.TimersMustStoppingForStart.Any() ?? false) ||
            (this.Ticker?.TimersMustRunningForStart.Any() ?? false) ||
            (this.Ticker?.TimersMustStoppingForStart.Any() ?? false);

        public void SetModel(
            ITrigger model)
        {
            this.model = model;
            this.RefreshSelected();
        }

        public void ClearSelect()
        {
            try
            {
                this.isInitialize = true;

                foreach (var x in this.ActiveSpells)
                {
                    x.IsSelected = false;
                }

                foreach (var x in this.InactiveSpells)
                {
                    x.IsSelected = false;
                }

                foreach (var x in this.ActiveTickers)
                {
                    x.IsSelected = false;
                }

                foreach (var x in this.InactiveTickers)
                {
                    x.IsSelected = false;
                }

                if (this.Spell != null)
                {
                    this.Spell.TimersMustRunningForStart = new Guid[0];
                    this.Spell.TimersMustStoppingForStart = new Guid[0];
                }
                else
                {
                    if (this.Ticker != null)
                    {
                        this.Ticker.TimersMustRunningForStart = new Guid[0];
                        this.Ticker.TimersMustStoppingForStart = new Guid[0];
                    }
                }

                this.RaisePropertyChanged(nameof(this.IsFiltered));
            }
            finally
            {
                this.isInitialize = false;
            }
        }

        public void SelectedChanged()
        {
            if (this.isInitialize)
            {
                return;
            }

            Task.Run(() =>
            {
                var mustStartIDs = new List<Guid>();
                mustStartIDs.AddRange(this.ActiveSpells.Where(x => x.IsSelected).Select(x => x.ID));
                mustStartIDs.AddRange(this.ActiveTickers.Where(x => x.IsSelected).Select(x => x.ID));

                var mustStopIDs = new List<Guid>();
                mustStopIDs.AddRange(this.InactiveSpells.Where(x => x.IsSelected).Select(x => x.ID));
                mustStopIDs.AddRange(this.InactiveTickers.Where(x => x.IsSelected).Select(x => x.ID));

                if (this.Spell != null)
                {
                    this.Spell.TimersMustRunningForStart = mustStartIDs.ToArray();
                    this.Spell.TimersMustStoppingForStart = mustStopIDs.ToArray();

                    WPFHelper.BeginInvoke(() => this.RaisePropertyChanged(nameof(this.IsFiltered)));
                }
                else
                {
                    if (this.Ticker != null)
                    {
                        this.Ticker.TimersMustRunningForStart = mustStartIDs.ToArray();
                        this.Ticker.TimersMustStoppingForStart = mustStopIDs.ToArray();

                        WPFHelper.BeginInvoke(() => this.RaisePropertyChanged(nameof(this.IsFiltered)));
                    }
                }
            });
        }

        public void RefreshSelected()
        {
            if (this.model == null)
            {
                return;
            }

            try
            {
                this.isInitialize = true;

                var mustStartIDs =
                    this.Spell?.TimersMustRunningForStart ??
                    this.Ticker?.TimersMustRunningForStart;

                var mustStopIDs =
                    this.Spell?.TimersMustStoppingForStart ??
                    this.Ticker?.TimersMustStoppingForStart;

                foreach (var x in this.ActiveSpells)
                {
                    x.IsSelected = mustStartIDs.Contains(x.ID);
                }

                foreach (var x in this.InactiveSpells)
                {
                    x.IsSelected = mustStopIDs.Contains(x.ID);
                }

                foreach (var x in this.ActiveTickers)
                {
                    x.IsSelected = mustStartIDs.Contains(x.ID);
                }

                foreach (var x in this.InactiveTickers)
                {
                    x.IsSelected = mustStopIDs.Contains(x.ID);
                }

                this.RaisePropertyChanged(nameof(this.IsFiltered));
            }
            finally
            {
                this.isInitialize = false;
            }
        }

        public void RefreshSpells()
        {
            var spellLists = new[]
            {
                this.ActiveSpells,
                this.InactiveSpells,
            };

            foreach (var spells in spellLists)
            {
                spells.Clear();
                spells.AddRange(
                    from x in SpellTable.Instance.Table
                    where
                    !x.IsInstance
                    orderby
                    x.Panel?.PanelName,
                    x.DisplayNo,
                    x.SpellTitle,
                    x.ID
                    select
                    new PreconditionSelector()
                    {
                        Trigger = x,
                        SelectedChangeDelegate = this.SelectedChanged
                    });
            }
        }

        public void RefreshTickers()
        {
            var tickerLists = new[]
            {
                this.ActiveTickers,
                this.InactiveTickers,
            };

            foreach (var tickers in tickerLists)
            {
                tickers.Clear();
                tickers.AddRange(
                    from x in TickerTable.Instance.Table
                    orderby
                    x.Title,
                    x.ID
                    select
                    new PreconditionSelector()
                    {
                        Trigger = x,
                        SelectedChangeDelegate = this.SelectedChanged
                    });
            }
        }
    }

    public class PreconditionSelector :
        BindableBase
    {
        private bool isSelected;

        public bool IsSelected
        {
            get => this.isSelected;
            set
            {
                if (this.SetProperty(ref this.isSelected, value))
                {
                    this.SelectedChangeDelegate?.Invoke();
                }
            }
        }

        public Action SelectedChangeDelegate { get; set; }

        public ITreeItem Trigger { get; set; }

        public Guid ID => (Guid)(this.Trigger as dynamic)?.Guid;

        public string Text
        {
            get
            {
                switch (this.Trigger)
                {
                    case Spell s:
                        return $"{s.Panel?.PanelName}-{s.SpellTitle}";

                    case Ticker t:
                        return t.Title;

                    default:
                        return string.Empty;
                }
            }
        }

        public override string ToString() => this.Text;
    }
}
