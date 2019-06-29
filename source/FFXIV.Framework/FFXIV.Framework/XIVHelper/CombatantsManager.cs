using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FFXIV.Framework.Common;
using FFXIV_ACT_Plugin.Common.Models;
using Sharlayan.Core.Enums;

namespace FFXIV.Framework.XIVHelper
{
    public class CombatantsManager
    {
        #region Singleton

        private static readonly Lazy<CombatantsManager> LazyInstance = new Lazy<CombatantsManager>(() => new CombatantsManager());

        public static CombatantsManager Instance => LazyInstance.Value;

        private CombatantsManager()
        {
        }

        #endregion Singleton

        #region Logger

        private static NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        private readonly IReadOnlyList<CombatantEx> EmptyCombatantList = new List<CombatantEx>();

        private static readonly object LockObject = new object();

        private readonly List<CombatantEx> Combatants = new List<CombatantEx>(5120);

        private readonly Dictionary<uint, CombatantEx> MainDictionary = new Dictionary<uint, CombatantEx>(2560);

        private readonly Dictionary<uint, CombatantEx> OtherDictionary = new Dictionary<uint, CombatantEx>(2560);

        private readonly List<CombatantEx> PartyList = new List<CombatantEx>(8);

        public int PartyCount { get; private set; } = 0;

        public PartyCompositions PartyComposition { get; private set; } = PartyCompositions.Unknown;

        public int CombatantsPCCount { get; private set; } = 0;

        public int CombatantsMainCount { get; private set; } = 0;

        public int CombatantsOtherCount { get; private set; } = 0;

        public CombatantEx Player { get; } = new CombatantEx()
        {
            Name = "Hojoring Hojo",
            Job = (int)JobIDs.PLD,
            IsDummy = true,
        };

        public IEnumerable<CombatantEx> GetCombatants()
        {
            lock (LockObject)
            {
                return this.Combatants.ToArray();
            }
        }

        public IDictionary<uint, CombatantEx> GetCombatantMainDictionary()
        {
            lock (LockObject)
            {
                return this.MainDictionary.Clone();
            }
        }

        public IDictionary<uint, CombatantEx> GetCombatantOtherDictionary()
        {
            lock (LockObject)
            {
                return this.OtherDictionary.Clone();
            }
        }

        public CombatantEx GetCombatant(
            string name)
            => this.GetCombatants().FirstOrDefault(x =>
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.NameFI, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.NameIF, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.NameII, name, StringComparison.OrdinalIgnoreCase));

        public CombatantEx GetCombatantMain(
            uint id)
        {
            lock (LockObject)
            {
                return this.MainDictionary.ContainsKey(id) ?
                    this.MainDictionary[id] :
                    null;
            }
        }

        public CombatantEx GetCombatantOther(
            uint npcid)
        {
            lock (LockObject)
            {
                return this.OtherDictionary.ContainsKey(npcid) ?
                    this.MainDictionary[npcid] :
                    null;
            }
        }

        public IEnumerable<CombatantEx> GetPartyList()
        {
            lock (LockObject)
            {
                return this.PartyList.ToArray();
            }
        }

        private volatile bool isWorking = false;

        private static readonly List<CombatantEx> EmptyCombatants = new List<CombatantEx>();

        public IEnumerable<CombatantEx> Refresh(
            IEnumerable<Combatant> source,
            bool isDummy = false)
        {
            if (this.isWorking)
            {
                return EmptyCombatants;
            }

            try
            {
                this.isWorking = true;

                lock (LockObject)
                {
                    return this.RefreshCore(source, isDummy);
                }
            }
            finally
            {
                this.isWorking = false;
            }
        }

        private IEnumerable<CombatantEx> RefreshCore(
            IEnumerable<Combatant> source,
            bool isDummy = false)
        {
            var isFirstCombatant = true;
            var partyIDList = new List<uint>(32);
            var addeds = new List<CombatantEx>(128);

            foreach (var combatant in source)
            {
                switch (combatant.GetActorType())
                {
                    // 必要なCombatant
                    case Actor.Type.PC:
                    case Actor.Type.Monster:
                    case Actor.Type.NPC:
                    case Actor.Type.Aetheryte:
                    case Actor.Type.Gathering:
                    case Actor.Type.EventObject:
                        break;

                    // 上記以外は捨てる
                    default:
                        continue;
                }

                var isMain = combatant.GetActorType() switch
                {
                    Actor.Type.PC => true,
                    Actor.Type.Monster => true,
                    _ => false,
                };

                var dic = isMain ? this.MainDictionary : this.OtherDictionary;
                var key = isMain ? combatant.ID : combatant.BNpcID;

                var isNew = !dic.ContainsKey(key);

                var ex = isNew ?
                    new CombatantEx() :
                    dic[key];

                CombatantEx.CopyToEx(combatant, ex);

                if (isDummy)
                {
                    ex.IsDummy = true;
                    ex.PartyType = PartyTypeEnum.Party;
                }

                if (isFirstCombatant)
                {
                    isFirstCombatant = false;
                    CombatantEx.CopyToEx(ex, this.Player);
                }

                if (isNew)
                {
                    addeds.Add(ex);
                    this.Combatants.Add(ex);
                    dic[key] = ex;
                }

                if (ex.PartyType == PartyTypeEnum.Party)
                {
                    partyIDList.Add(ex.ID);
                }

                Thread.Yield();
            }

            if (this.Player != null &&
                this.Player.TargetID != 0 &&
                this.MainDictionary.ContainsKey(this.Player.TargetID))
            {
                var target = this.MainDictionary[this.Player.TargetID];
                this.Player.TargetOfTargetID = target.TargetID;
            }

            if (partyIDList.Any())
            {
                if (this.PartyCount <= 0 ||
                    this.PartyCount != partyIDList.Count)
                {
                    this.RefreshPartyList(partyIDList);
                }
            }

            this.TryGarbage();

            this.CombatantsPCCount = this.MainDictionary.Count(x => x.Value.ActorType == Actor.Type.PC);
            this.CombatantsMainCount = this.MainDictionary.Count;
            this.CombatantsOtherCount = this.OtherDictionary.Count;

            return addeds;
        }

        public void RefreshPartyList(
            IEnumerable<uint> partyIDList)
        {
            lock (LockObject)
            {
                var dic = this.GetCombatantMainDictionary();
                var party = partyIDList
                    .Where(x => dic.ContainsKey(x))
                    .Select(x => dic[x])
                    .ToArray();

                var sortedPartyList =
                    from x in party
                    orderby
                    x.IsPlayer ? 0 : 1,
                    x.DisplayOrder,
                    x.Role.ToSortOrder(),
                    x.Job,
                    x.ID descending
                    select
                    x;

                this.PartyList.Clear();
                this.PartyList.AddRange(sortedPartyList);

                this.PartyCount = party.Length;

                var composition = PartyCompositions.Unknown;

                if (this.PartyCount == 4)
                {
                    composition = PartyCompositions.LightParty;
                }
                else
                {
                    if (this.PartyCount >= 8)
                    {
                        var tanks = party.Count(x => x.Role == Roles.Tank);
                        switch (tanks)
                        {
                            case 1:
                            case 3:
                                composition = PartyCompositions.FullPartyT1;
                                break;

                            case 2:
                            case 6:
                                composition = PartyCompositions.FullPartyT2;
                                break;
                        }
                    }
                }

                if (this.PartyComposition != composition)
                {
                    this.PartyComposition = composition;
                    AppLogger.Info($"party composition changed. current={composition} party_count={party.Length}");
                }
            }
        }

        public IReadOnlyList<CombatantsByRole> GetPatryListByRole()
        {
            var list = new List<CombatantsByRole>(8);

            var player = this.Player;
            var partyList = this.GetPartyList();
            if (!partyList.Any() && player != null)
            {
                partyList = partyList.Concat(new[] { player });
            }

            var tanks = partyList
                .Where(x => x.Role == Roles.Tank)
                .ToList();

            var dpses = partyList
                .Where(x =>
                    x.Role == Roles.MeleeDPS ||
                    x.Role == Roles.RangeDPS ||
                    x.Role == Roles.MagicDPS)
                .ToList();

            var melees = partyList
                .Where(x => x.Role == Roles.MeleeDPS)
                .ToList();

            var ranges = partyList
                .Where(x => x.Role == Roles.RangeDPS)
                .ToList();

            var magics = partyList
                .Where(x => x.Role == Roles.MagicDPS)
                .ToList();

            var healers = partyList
                .Where(x => x.Role == Roles.Healer)
                .ToList();

            if (tanks.Any())
            {
                list.Add(new CombatantsByRole(
                    Roles.Tank,
                    "TANK",
                    tanks));
            }

            if (dpses.Any())
            {
                list.Add(new CombatantsByRole(
                    Roles.DPS,
                    "DPS",
                    dpses));
            }

            if (melees.Any())
            {
                list.Add(new CombatantsByRole(
                    Roles.MeleeDPS,
                    "MELEE",
                    melees));
            }

            if (ranges.Any())
            {
                list.Add(new CombatantsByRole(
                    Roles.RangeDPS,
                    "RANGE",
                    ranges));
            }

            if (magics.Any())
            {
                list.Add(new CombatantsByRole(
                    Roles.MagicDPS,
                    "MAGIC",
                    magics));
            }

            if (healers.Any())
            {
                list.Add(new CombatantsByRole(
                    Roles.Healer,
                    "HEALER",
                    healers));
            }

            return list;
        }

        public void Clear()
        {
            if (this.CombatantsMainCount <= 0)
            {
                return;
            }

            lock (LockObject)
            {
                this.CombatantsPCCount = 0;
                this.CombatantsMainCount = 0;
                this.CombatantsOtherCount = 0;
                this.PartyCount = 0;

                this.Combatants.Clear();
                this.MainDictionary.Clear();
                this.OtherDictionary.Clear();
                this.PartyList.Clear();
                this.PartyComposition = PartyCompositions.Unknown;
            }
        }

        private static readonly double GarbageThreshold = 3.0d;
        private DateTime lastGarbageTimestamp = DateTime.Now;

        private void TryGarbage()
        {
            var now = DateTime.Now;

            if ((now - this.lastGarbageTimestamp).TotalSeconds >= GarbageThreshold)
            {
                this.lastGarbageTimestamp = now;
                this.Garbage();
            }
        }

        public void Garbage()
        {
            lock (LockObject)
            {
                var targets = this.Combatants
                    .Where(x => (DateTime.Now - x.LastUpdateTimestamp).TotalSeconds >= GarbageThreshold)
                    .ToArray();

                foreach (var target in targets)
                {
                    var isMain = target.ActorType switch
                    {
                        Actor.Type.PC => true,
                        Actor.Type.Monster => true,
                        _ => false,
                    };

                    var dic = isMain ? this.MainDictionary : this.OtherDictionary;

                    if (dic.ContainsKey(target.ID))
                    {
                        dic.Remove(target.ID);
                    }

                    this.Combatants.Remove(target);
                    Thread.Yield();
                }
            }
        }
    }

    public class CombatantsByRole
    {
        public CombatantsByRole(
            Roles roleType,
            string roleLabel,
            IReadOnlyList<CombatantEx> combatants)
        {
            this.RoleType = roleType;
            this.RoleLabel = roleLabel;
            this.Combatants = combatants;
        }

        public IReadOnlyList<CombatantEx> Combatants { get; set; }

        public string RoleLabel { get; set; }

        public Roles RoleType { get; set; }
    }
}
