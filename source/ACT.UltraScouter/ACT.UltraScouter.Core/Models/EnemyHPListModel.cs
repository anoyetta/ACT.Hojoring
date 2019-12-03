using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using ACT.UltraScouter.Config;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;
using Sharlayan.Core.Enums;

namespace ACT.UltraScouter.Models
{
    public class EnemyHPListModel :
        TargetInfoModel
    {
        #region Lazy Singleton

        private static readonly Lazy<EnemyHPListModel> LazyInstance = new Lazy<EnemyHPListModel>(() => new EnemyHPListModel());

        public new static EnemyHPListModel Instance => LazyInstance.Value;

        private EnemyHPListModel()
        {
            this.InitCollectionViewSource();
        }

        #endregion Lazy Singleton

        private readonly ObservableCollection<EnemyHPModel> enemyHPList = new ObservableCollection<EnemyHPModel>();

        private CollectionViewSource enemyHPViewSource;

        public ICollectionView EnemyHPListView => this.enemyHPViewSource?.View;

        public bool IsExists => this.enemyHPList.Any();

        private void InitCollectionViewSource()
        {
            if (WPFHelper.IsDesignMode)
            {
                this.enemyHPList.AddRange(DesignModeEnemyList);
            }

            var src = new CollectionViewSource()
            {
                Source = this.enemyHPList,
                IsLiveSortingRequested = true,
            };

            src.SortDescriptions.Add(new SortDescription(
                nameof(EnemyHPModel.ID),
                ListSortDirection.Ascending));

            this.enemyHPViewSource = src;
        }

        private volatile bool isDesignMode;

        public void Update()
        {
            if (Settings.Instance.EnemyHP.IsDesignMode)
            {
                this.UpdateDesignMode();
                this.RaisePropertyChanged(nameof(this.IsExists));
                return;
            }

            this.isDesignMode = false;

            var combatants =
                from x in CombatantsManager.Instance.GetCombatants()
                where
                x.ActorType == Actor.Type.Monster &&
                x.MaxHP > 0 &&
                x.CurrentHP > 0 &&
                x.CurrentHP < x.MaxHP &&
                (x.PosX + x.PosY + x.PosZ) != 0
                group x by x.Name into g
                select (
                    from y in g
                    orderby
                    y.CurrentHP ascending,
                    y.ID descending
                    select
                    y).First();

            if (!combatants.Any())
            {
                this.enemyHPList.Clear();
                this.RaisePropertyChanged(nameof(this.IsExists));
                return;
            }

            var player = CombatantsManager.Instance.Player;

            var previousEnemyListCount = this.enemyHPList.Count;

            foreach (var c in combatants)
            {
                var entry = this.enemyHPList
                    .FirstOrDefault(x => x.ID == c.ID);

                if (entry == null)
                {
                    entry = new EnemyHPModel();
                    this.enemyHPList.Add(entry);
                }

                entry.ID = c.ID;
                entry.IsCurrentTarget = c.ID == player?.TargetID;
                entry.Name = c.Name;
                entry.MaxHP = c.MaxHP;
                entry.CurrentHP = c.CurrentHP;
                entry.Distance = c.HorizontalDistanceByPlayer;

                var diffTarget = (
                    from x in combatants
                    where
                    x.MaxHP == c.MaxHP &&
                    x.ID != c.ID
                    orderby
                    Math.Abs(x.CurrentHP - c.CurrentHP) descending
                    select
                    x).FirstOrDefault();

                if (diffTarget != null)
                {
                    entry.DeltaHP = c.CurrentHP - diffTarget.CurrentHP;
                    entry.DeltaHPRate = c.CurrentHPRate - diffTarget.CurrentHPRate;
                    entry.IsExistsDelta = true;
                }
                else
                {
                    entry.DeltaHP = 0;
                    entry.DeltaHPRate = 0;
                    entry.IsExistsDelta = false;
                }
            }

            var toRemove = this.enemyHPList
                .Where(x => !combatants.Any(y => x.ID == y.ID))
                .ToArray();

            foreach (var item in toRemove)
            {
                this.enemyHPList.Remove(item);
            }

            if (previousEnemyListCount != this.enemyHPList.Count)
            {
                this.RaisePropertyChanged(nameof(this.IsExists));
            }
        }

        private void UpdateDesignMode()
        {
            if (!this.isDesignMode)
            {
                this.enemyHPList.Clear();
                this.enemyHPList.AddRange(DesignModeEnemyList);
                this.RaisePropertyChanged(nameof(this.IsExists));
            }

            this.isDesignMode = true;

            var liquid = DesignModeEnemyList[0];
            var hand = DesignModeEnemyList[1];

            var rate = (double)(60 - DateTime.Now.Second) / 60d;

            liquid.CurrentHP = (uint)(liquid.MaxHP * rate);
            hand.CurrentHP = (uint)(hand.MaxHP * (rate * 0.9d));

            var self = liquid;
            var diff = hand;
            liquid.DeltaHP = self.CurrentHP - diff.CurrentHP;
            liquid.DeltaHPRate = self.CurrentHPRate - diff.CurrentHPRate;
            liquid.IsExistsDelta = true;

            self = hand;
            diff = liquid;
            hand.DeltaHP = self.CurrentHP - diff.CurrentHP;
            hand.DeltaHPRate = self.CurrentHPRate - diff.CurrentHPRate;
            hand.IsExistsDelta = true;
        }

        private static readonly EnemyHPModel[] DesignModeEnemyList = new[]
        {
            new EnemyHPModel()
            {
                ID = 1,
                IsCurrentTarget = true,
                Name = "リビングリキッド",
                MaxHP = 12889320,
            },
            new EnemyHPModel()
            {
                ID = 2,
                Name = "リキッドハンド",
                MaxHP = 12889320,
                CurrentHP = (uint)(12889320 * 0.9),
            }
        };
    }
}
