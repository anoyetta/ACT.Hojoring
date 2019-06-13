using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Data;
using System.Windows.Media;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models.Enmity;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.FFXIVHelper;
using Sharlayan.Core.Enums;

namespace ACT.UltraScouter.Models
{
    public partial class TargetInfoModel
    {
        private static ObservableCollection<EnmityModel> designtimeEnmityList;

        private static readonly double DesignTopEnmity = 3214405;

        private static ObservableCollection<EnmityModel> DesigntimeEnmityList => designtimeEnmityList ?? (designtimeEnmityList = new ObservableCollection<EnmityModel>()
        {
            new EnmityModel()
            {
                Index = 1,
                ID = 1,
                Name = "Taro Yamada",
                JobID = JobIDs.WAR,
                HateRate = 1.0f,
                Enmity = DesignTopEnmity,
                IsTop = true,
            },
            new EnmityModel()
            {
                Index = 2,
                ID = 2,
                Name = "Jiro Suzuki",
                JobID = JobIDs.PLD,
                HateRate = 0.85f,
                Enmity = DesignTopEnmity * 0.85f,
            },
            new EnmityModel()
            {
                Index = 3,
                ID = 3,
                Name = "Hanako Hime",
                JobID = JobIDs.WHM,
                HateRate = 0.52f,
                Enmity = DesignTopEnmity * 0.52f,
            },
            new EnmityModel()
            {
                Index = 4,
                ID = 4,
                Name = "Cookie Cream",
                JobID = JobIDs.SCH,
                HateRate = 0.48f,
                Enmity = DesignTopEnmity * 0.48f,
                IsMe = true,
            },
            new EnmityModel()
            {
                Index = 5,
                ID = 5,
                Name = "Ryusan Sky",
                JobID = JobIDs.DRG,
                HateRate = 0.32f,
                Enmity = DesignTopEnmity * 0.32f,
            },
            new EnmityModel()
            {
                Index = 6,
                ID = 6,
                Name = "Utako Song",
                JobID = JobIDs.BRD,
                HateRate = 0.31f,
                Enmity = DesignTopEnmity * 0.31f,
            },
            new EnmityModel()
            {
                Index = 7,
                ID = 7,
                Name = "Red Yoshida",
                JobID = JobIDs.RDM,
                HateRate = 0.29f,
                Enmity = DesignTopEnmity * 0.29f,
            },
            new EnmityModel()
            {
                Index = 8,
                ID = 8,
                Name = "Ridea Numako",
                JobID = JobIDs.SMN,
                HateRate = 0.10f,
                Enmity = DesignTopEnmity * 0.10f,
            },
        });

        private static List<EnmityModel> CloneDesigntimeEnmityList()
        {
            var original = DesigntimeEnmityList;

            var list = new List<EnmityModel>(original.Count);

            var pre = default(EnmityModel);
            foreach (var item in original)
            {
                if (pre != null)
                {
                    item.EnmityDifference = pre.Enmity - item.Enmity;
                }

                list.Add(item.Clone());
                pre = item;
            }

            return list;
        }

        private bool isExistsEnmityList = false;

        public bool IsExistsEnmityList
        {
            get => this.isExistsEnmityList;
            set => this.SetProperty(ref this.isExistsEnmityList, value);
        }

        private ObservableCollection<EnmityModel> enmityList = WPFHelper.IsDesignMode ?
            DesigntimeEnmityList :
            new ObservableCollection<EnmityModel>();

        private CollectionViewSource EnmityViewSource { get; set; }

        public ICollectionView EnmityView => this.EnmityViewSource?.View;

        private async void CreateEnmityViewSource() => await WPFHelper.InvokeAsync(() =>
        {
            var source = new CollectionViewSource()
            {
                Source = this.enmityList,
                IsLiveSortingRequested = true,
            };

            source.SortDescriptions.AddRange(new[]
            {
                new SortDescription(nameof(EnmityModel.Index), ListSortDirection.Ascending),
            });

            source.LiveFilteringProperties.AddRange(new[]
            {
                nameof(EnmityModel.Index),
            });

            this.EnmityViewSource = source;
            this.EnmityView.Refresh();
            this.RaisePropertyChanged(nameof(this.EnmityView));
            this.IsExistsEnmityList = this.enmityList.Any();

            this.enmityList.Walk(x => x.RaiseAllPropertiesChanged());
        });

        private volatile bool isEnmityRefreshing = false;

        private double tankEPS;

        public double TankEPS
        {
            get => this.tankEPS;
            set => this.SetProperty(ref this.tankEPS, value);
        }

        private double nearThreshold;

        public double NearThreshold
        {
            get => this.nearThreshold;
            set => this.SetProperty(ref this.nearThreshold, value);
        }

        public void RefreshEnmityList(
            IEnumerable<EnmityModel> newEnmityList,
            double tankEPS = 0,
            double nearThreshold = 0)
        {
            if (this.isEnmityRefreshing)
            {
                return;
            }

            try
            {
                this.isEnmityRefreshing = true;

                var config = Settings.Instance.Enmity;
                if (!config.Visible)
                {
                    this.enmityList.Clear();
                    this.IsExistsEnmityList = false;
                    return;
                }

                if (!config.IsDesignMode &&
                    this.ObjectType != Actor.Type.Monster)
                {
                    this.enmityList.Clear();
                    this.IsExistsEnmityList = false;
                    return;
                }

                if (config.IsDesignMode)
                {
                    newEnmityList = CloneDesigntimeEnmityList().Take(config.MaxCountOfDisplay);

                    var pcNameStyle = ConfigBridge.Instance.PCNameStyle;
                    foreach (var item in newEnmityList)
                    {
                        item.Name = CombatantEx.NameToInitial(item.Name, pcNameStyle);
                    }
                }
                else
                {
                    if (config.HideInNotCombat &&
                        !FFXIVPlugin.Instance.InCombat)
                    {
                        this.enmityList.Clear();
                        this.IsExistsEnmityList = false;
                        return;
                    }

                    var partyCount = CombatantsManager.Instance.PartyCount;
                    if (config.HideInSolo)
                    {
                        if (partyCount <= 1)
                        {
                            this.enmityList.Clear();
                            this.IsExistsEnmityList = false;
                            return;
                        }
                    }
                }

                if (newEnmityList == null ||
                    newEnmityList.Count() < 1)
                {
                    this.enmityList.Clear();
                    this.IsExistsEnmityList = false;
                    return;
                }

                var currentEnmityDictionary = this.enmityList.ToDictionary(x => x.ID);

                if (config.IsDesignMode)
                {
                    nearThreshold = 500000;
                }

                this.TankEPS = tankEPS;
                this.NearThreshold = nearThreshold;

                using (this.EnmityViewSource.DeferRefresh())
                {
                    var enmityMe = newEnmityList.FirstOrDefault(x => x.IsMe)?.Enmity ?? 0;

                    var pre = default(EnmityModel);
                    foreach (var src in newEnmityList)
                    {
                        Thread.Yield();

                        var dest = currentEnmityDictionary.ContainsKey(src.ID) ?
                            currentEnmityDictionary[src.ID] :
                            null;
                        if (dest == null)
                        {
                            this.enmityList.Add(src);
                            continue;
                        }

                        dest.Index = src.Index;
                        dest.Name = src.Name;
                        dest.JobID = src.JobID;
                        dest.Enmity = src.Enmity;
                        dest.EnmityDifference = src.Enmity - enmityMe;
                        dest.HateRate = src.HateRate;
                        dest.IsMe = src.IsMe;
                        dest.IsPet = src.IsPet;
                        dest.IsTop = src.IsTop;
                        dest.IsNear = Math.Abs(dest.EnmityDifference) <= nearThreshold;

                        pre = dest;
                    }

                    foreach (var item in this.enmityList.Except(newEnmityList, EnmityModel.EnmityModelComparer).ToArray())
                    {
                        this.enmityList.Remove(item);
                    }
                }

                this.IsExistsEnmityList = this.enmityList.Count > 0;
                this.RefreshEnmtiyHateRateBarWidth();
            }
            finally
            {
                this.isEnmityRefreshing = false;
            }
        }

        private double previousBarWidthMax = 0d;
        private bool previousIsNearIndicator = false;
        private Color previousNearColor = Colors.Transparent;

        private void RefreshEnmtiyHateRateBarWidth()
        {
            if (!this.enmityList.Any())
            {
                return;
            }

            var config = Settings.Instance.Enmity;

            if (this.previousBarWidthMax != config.BarWidth ||
                this.previousIsNearIndicator != config.IsNearIndicator ||
                this.previousNearColor != config.NearColor)
            {
                this.previousBarWidthMax = config.BarWidth;
                this.previousIsNearIndicator = config.IsNearIndicator;
                this.previousNearColor = config.NearColor;

                foreach (var item in this.enmityList)
                {
                    item.RefreshBarWidth();
                    item.RefreshBarColor();
                }
            }
        }

        public void ClearEnmity()
        {
            this.enmityList.Clear();
            this.IsExistsEnmityList = false;
        }
    }
}
