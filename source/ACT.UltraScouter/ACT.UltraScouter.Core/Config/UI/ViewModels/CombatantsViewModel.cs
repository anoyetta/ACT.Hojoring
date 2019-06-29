using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;
using NLog;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class CombatantsViewModel :
        BindableBase
    {
        #region Logger

        private Logger logger = AppLog.DefaultLogger;

        #endregion Logger

        public CombatantsViewModel()
        {
            combatants = null;

            this.combatantsSource = new CollectionViewSource()
            {
                Source = Combatants,
                IsLiveFilteringRequested = true,
                IsLiveSortingRequested = true,
            };

            this.combatantsSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription(nameof(CombatantEx.Order), ListSortDirection.Ascending),
                new SortDescription(nameof(CombatantEx.ID), ListSortDirection.Ascending),
            });
        }

        public MobList Config => Settings.Instance.MobList;

        private static readonly object Locker = new object();
        private static ObservableCollection<CombatantEx> combatants;

        private static ObservableCollection<CombatantEx> Combatants
        {
            get
            {
                if (combatants == null)
                {
                    combatants = new ObservableCollection<CombatantEx>();
                    BindingOperations.EnableCollectionSynchronization(combatants, Locker);
                }

                return combatants;
            }
        }

        private static DateTime lastUpdateDatetime = DateTime.MinValue;

        public static void RefreshCombatants(
            IEnumerable<CombatantEx> source)
        {
            if (!Settings.Instance.MobList.DumpCombatants)
            {
                return;
            }

            if ((DateTime.Now - lastUpdateDatetime).TotalSeconds <= 1.0d)
            {
                return;
            }

            lastUpdateDatetime = DateTime.Now;

            var combatants = Combatants;

            if (source == null)
            {
                combatants.Clear();
                return;
            }

            source = source.Where(x => !string.IsNullOrEmpty(x.Name));

            if (!source.Any())
            {
                combatants.Clear();
                return;
            }

            var toAdds = source.Where(x => !combatants.Any(y => y.UUID == x.UUID));
            combatants.AddRange(toAdds);

            var toRemoves = combatants.Where(x => !source.Any(y => y.UUID == x.UUID)).ToArray();
            foreach (var item in toRemoves)
            {
                combatants.Remove(item);
            }
        }

        private CollectionViewSource combatantsSource;

        public ICollectionView CombatantsView => this.combatantsSource?.View;
    }
}
