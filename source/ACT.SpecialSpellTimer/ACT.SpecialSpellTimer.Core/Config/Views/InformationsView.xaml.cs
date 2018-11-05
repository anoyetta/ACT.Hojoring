using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Common;
using FFXIV.Framework.FFXIVHelper;
using FFXIV.Framework.Globalization;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using TamanegiMage.FFXIV_MemoryReader.Model;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// InformationsView.xaml の相互作用ロジック
    /// </summary>
    public partial class InformationsView :
        UserControl,
        ILocalizable,
        INotifyPropertyChanged
    {
        public InformationsView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Default.UILocale);
            this.LoadConfigViewResources();

            this.hotbarInfoListSource.Source = this.HotbarInfoList;
            this.hotbarInfoListSource.IsLiveSortingRequested = true;
            this.hotbarInfoListSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription()
                {
                    PropertyName = nameof(HotbarInfoContainer.Type),
                    Direction = ListSortDirection.Descending,
                },
                new SortDescription()
                {
                    PropertyName = nameof(HotbarInfoContainer.DisplayOrder),
                    Direction = ListSortDirection.Ascending,
                },
                new SortDescription()
                {
                    PropertyName = nameof(HotbarInfoContainer.Remain),
                    Direction = ListSortDirection.Ascending,
                },
                new SortDescription()
                {
                    PropertyName = nameof(HotbarInfoContainer.ID),
                    Direction = ListSortDirection.Ascending,
                },
            });

            this.RaisePropertyChanged(nameof(this.HotbarInfoListView));

            this.timer.Interval = TimeSpan.FromSeconds(5);
            this.timer.Tick += (x, y) =>
            {
                if (this.IsLoaded)
                {
                    this.RefreshPlaceholderList();
                    this.RefreshTriggerList();
                    this.RefreshHotbarInfo();
                }
            };

            this.timer.Start();
        }

        private DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.ContextIdle);

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        public Settings Config => Settings.Default;

        public ObservableCollection<TableCompiler.PlaceholderContainer> PlaceholderList
        {
            get;
            private set;
        } = new ObservableCollection<TableCompiler.PlaceholderContainer>();

        public ObservableCollection<TriggerContainer> TriggerList
        {
            get;
            private set;
        } = new ObservableCollection<TriggerContainer>();

        public ICollectionView HotbarInfoListView => this.hotbarInfoListSource?.View;

        private CollectionViewSource hotbarInfoListSource = new CollectionViewSource();

        private ObservableCollection<HotbarInfoContainer> HotbarInfoList
        {
            get;
            set;
        } = new ObservableCollection<HotbarInfoContainer>();

        public long ActiveTriggerCount { get; private set; } = 0;

        private bool placeholderRefreshed = false;

        private void RefreshPlaceholderList()
        {
            this.placeholderRefreshed = false;

            var newList = TableCompiler.Instance.PlaceholderList;

            var toRefresh = false;
            if (newList.Count != this.PlaceholderList.Count)
            {
                toRefresh = true;
            }
            else
            {
                do
                {
                    toRefresh |= this.PlaceholderList.Any(x => !newList.Any(y => y.Placeholder == x.Placeholder));
                    if (toRefresh)
                    {
                        break;
                    }

                    toRefresh |= newList.Any(x => !this.PlaceholderList.Any(y => y.Placeholder == x.Placeholder));
                    if (toRefresh)
                    {
                        break;
                    }

                    foreach (var x in this.PlaceholderList)
                    {
                        var y = newList.FirstOrDefault(z => z.Placeholder == x.Placeholder);
                        if (x.ReplaceString != y.ReplaceString)
                        {
                            toRefresh |= true;
                            break;
                        }
                    }
                } while (false);
            }

            if (toRefresh)
            {
                this.PlaceholderList.Clear();
                this.PlaceholderList.AddRange(
                    from x in TableCompiler.Instance.PlaceholderList
                    orderby
                    x.Type,
                    x.Placeholder
                    select
                    x);

                this.placeholderRefreshed = true;
            }
        }

        private void RefreshTriggerList()
        {
            var toRefresh = this.placeholderRefreshed;

            var newList = (
                from x in TableCompiler.Instance.TriggerList
                where
                (x as Ticker) != null ||
                (x as Spell)?.IsInstance == false
                select
                x).ToList();

            do
            {
                if (toRefresh)
                {
                    break;
                }

                if (newList.Count != this.TriggerList.Count)
                {
                    toRefresh |= true;
                }
                else
                {
                    toRefresh |= this.TriggerList.Any(x => !newList.Any(y => y.GetID() == x.ID));
                    if (toRefresh)
                    {
                        break;
                    }

                    toRefresh |= newList.Any(x => !this.TriggerList.Any(y => y.ID == x.GetID()));
                    if (toRefresh)
                    {
                        break;
                    }

                    foreach (var x in this.TriggerList)
                    {
                        var y = newList.FirstOrDefault(z => z.GetID() == x.ID);
                        var yc = new TriggerContainer() { Trigger = y };

                        if (x.Pattern != yc.Pattern)
                        {
                            toRefresh |= true;
                            break;
                        }
                    }
                }
            } while (false);

            if (toRefresh)
            {
                this.TriggerList.Clear();

                var i = 1;
                var query =
                    from x in newList
                    orderby
                    x.ItemType
                    select new TriggerContainer()
                    {
                        Trigger = x,
                        No = i++,
                    };

                this.TriggerList.AddRange(query);

                this.ActiveTriggerCount = newList.LongCount() + SpellTable.Instance.GetInstanceSpells().Count;
                this.RaisePropertyChanged(nameof(this.ActiveTriggerCount));
            }
        }

        private void RefreshHotbarInfo()
        {
            if (!FFXIVReader.Instance.IsAvailable)
            {
                if (this.HotbarInfoList.Any())
                {
                    this.HotbarInfoList.Clear();
                }

                return;
            }

            var newList = FFXIVReader.Instance.GetHotbarRecastV1();
            if (newList == null ||
                !newList.Any())
            {
                if (this.HotbarInfoList.Any())
                {
                    this.HotbarInfoList.Clear();
                }

                return;
            }

            // 名前でグループ化する
            var newSource = newList
                .GroupBy(x => x.Name)
                .Select(g => g.First());

            // 更新する
            newSource.Walk(x =>
            {
                var toUpdate = this.HotbarInfoList.FirstOrDefault(y => y.ID == x.ID);
                toUpdate?.UpdateSourceInfo(x);
            });

            // 追加と削除を実施する
            var toAdds = newSource.Where(x => !this.HotbarInfoList.Any(y => y.ID == x.ID)).ToArray();
            var toRemoves = this.HotbarInfoList.Where(x => !newSource.Any(y => y.ID == x.ID)).ToArray();

            this.HotbarInfoList.AddRange(toAdds.Select(x => new HotbarInfoContainer(x)));
            toRemoves.Walk(x => this.HotbarInfoList.Remove(x));
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

        public class TriggerContainer
        {
            public ITrigger Trigger { get; set; }

            public long No { get; set; }

            public Guid ID => this.Trigger.GetID();

            public string TriggerType => this.Trigger?.ItemType.ToString();

            public string Name
            {
                get
                {
                    switch (this.Trigger)
                    {
                        case Spell s:
                            return s.SpellTitle;

                        case Ticker t:
                            return t.Title;

                        default:
                            return string.Empty;
                    }
                }
            }

            public string Pattern
            {
                get
                {
                    switch (this.Trigger)
                    {
                        case Spell s:
                            return
                                !string.IsNullOrEmpty(s.KeywordReplaced) ?
                                s.KeywordReplaced :
                                s.Keyword;

                        case Ticker t:
                            return
                                !string.IsNullOrEmpty(t.KeywordReplaced) ?
                                t.KeywordReplaced :
                                t.Keyword;

                        default:
                            return string.Empty;
                    }
                }
            }

            public string UseRegexText => this.UseRegex ? "✔" : string.Empty;

            public bool UseRegex
            {
                get
                {
                    switch (this.Trigger)
                    {
                        case Spell s:
                            return s.RegexEnabled && s.Regex != null;

                        case Ticker t:
                            return t.RegexEnabled && t.Regex != null;

                        default:
                            return false;
                    }
                }
            }
        }

        public class HotbarInfoContainer :
            BindableBase
        {
            /*
            public class HotbarRecastV1
            {
                public HotbarRecastV1();

                public HotbarType HotbarType { get; set; }
                public int ID { get; set; }
                public int Slot { get; set; }
                public string Name { get; set; }
                public int Category { get; set; }
                public int Type { get; set; }
                public int Icon { get; set; }
                public int CoolDownPercent { get; set; }
                public bool IsAvailable { get; set; }
                public int RemainingOrCost { get; set; }
                public int Amount { get; set; }
                public bool InRange { get; set; }
                public bool IsProcOrCombo { get; set; }
            }
            */

            public HotbarInfoContainer(
                HotbarRecastV1 source)
                => this.UpdateSourceInfo(source);

            public void UpdateSourceInfo(
                HotbarRecastV1 source)
            {
                this.ID = source.ID;
                this.Name = source.Name;
                this.Type = source.Type;

                if (this.Remain != source.RemainingOrCost)
                {
                    this.Remain = source.RemainingOrCost;
                    this.DisplayOrder = 0;
                }
                else
                {
                    this.DisplayOrder = 1;
                }
            }

            private int id;

            public int ID
            {
                get => this.id;
                set => this.SetProperty(ref this.id, value);
            }

            private string name;

            public string Name
            {
                get => this.name;
                set => this.SetProperty(ref this.name, value);
            }

            private int type;

            public int Type
            {
                get => this.type;
                set => this.SetProperty(ref this.type, value);
            }

            private int remain;

            public int Remain
            {
                get => this.remain;
                set => this.SetProperty(ref this.remain, value);
            }

            private int displayOrder = 0;

            public int DisplayOrder
            {
                get => this.displayOrder;
                set => this.SetProperty(ref this.displayOrder, value);
            }
        }
    }
}
