using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using FFXIV.Framework.Common;
using FFXIV.Framework.FFXIVHelper;
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

            if (source == null ||
                !source.Any())
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

            foreach (var combatant in combatants)
            {
                var src = source.FirstOrDefault(x => x.UUID == combatant.UUID);
                if (src != null)
                {
                    if (combatant.PosX != src.PosX ||
                        combatant.PosY != src.PosY ||
                        combatant.PosZ != src.PosZ ||
                        combatant.Heading != src.Heading ||
                        combatant.CurrentHP != src.CurrentHP ||
                        combatant.MaxHP != src.MaxHP)
                    {
                        combatant.PosX = src.PosX;
                        combatant.PosY = src.PosY;
                        combatant.PosZ = src.PosZ;
                        combatant.Heading = src.Heading;
                        combatant.CurrentHP = src.CurrentHP;
                        combatant.MaxHP = src.MaxHP;

                        combatant.NotifyProperties();
                    }
                }
            }
        }

        private CollectionViewSource combatantsSource;

        public ICollectionView CombatantsView => this.combatantsSource?.View;
    }
}
