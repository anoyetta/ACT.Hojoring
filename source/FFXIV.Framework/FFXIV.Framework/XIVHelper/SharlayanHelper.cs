using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using Sharlayan;
using Sharlayan.Core;
using Sharlayan.Core.Enums;
using Sharlayan.Enums;
using Sharlayan.Models;
using ActorItem = Sharlayan.Core.ActorItem;
using TargetInfo = Sharlayan.Core.TargetInfo;

namespace FFXIV.Framework.XIVHelper
{
    [Flags]
    public enum PartyCompositions
    {
        Unknown = 0,
        LightParty,
        FullPartyT1,
        FullPartyT2
    }

    public class SharlayanHelper
    {
        #region Logger

        private static NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        private static readonly Lazy<SharlayanHelper> LazyInstance = new Lazy<SharlayanHelper>();

        public static SharlayanHelper Instance => LazyInstance.Value;

        public SharlayanHelper()
        {
        }

        private ThreadWorker ffxivSubscriber;
        private static readonly double ProcessSubscribeInterval = 10 * 1000;

        private ThreadWorker memorySubscriber;
        private static readonly double MemorySubscribeDefaultInterval = 500;

        public void Start(
            double? memoryScanInterval = null)
        {
            lock (this)
            {
                if (this.ffxivSubscriber == null)
                {
                    this.ffxivSubscriber = new ThreadWorker(
                        this.DetectFFXIVProcess,
                        ProcessSubscribeInterval,
                        "sharlayan Process Subscriber",
                        ThreadPriority.Lowest);

                    Task.Run(() =>
                    {
                        Thread.Sleep((int)ProcessSubscribeInterval / 2);
                        this.ffxivSubscriber.Run();
                    });
                }

                if (this.memorySubscriber == null)
                {
                    this.memorySubscriber = new ThreadWorker(
                        this.ScanMemory,
                        memoryScanInterval ?? MemorySubscribeDefaultInterval,
                        "sharlayan Memory Subscriber",
                        ThreadPriority.BelowNormal);

                    this.memorySubscriber.Run();
                }
            }
        }

        public void End()
        {
            lock (this)
            {
                try
                {
                    if (this.ffxivSubscriber != null)
                    {
                        this.ffxivSubscriber.Abort();
                        this.ffxivSubscriber = null;
                    }

                    if (this.memorySubscriber != null)
                    {
                        this.memorySubscriber.Abort();
                        this.memorySubscriber = null;
                    }
                }
                finally
                {
                    ClearLocalCaches();
                }
            }
        }

        private bool enqueueReload;

        public void EnqueueReload()
        {
            lock (this)
            {
                this.enqueueReload = true;
            }
        }

        private void ClearData()
        {
            lock (this.ActorList)
            {
                this.ActorList.Clear();
            }
        }

        private Process currentFFXIVProcess;
        private GameLanguage currentFFXIVLanguage;
        private MemoryHandler _memoryHandler;

        private static readonly string[] LocalCacheFiles = new[]
        {
            "actions*.json",
            "signatures*.json",
            "statuses*.json",
            "structures*.json",
            "zones*.json",
        };

        private static void ClearLocalCaches()
        {
            if (!Config.Instance.IsForceFlushSharlayanResources)
            {
                return;
            }

            var dir = Directory.GetCurrentDirectory();
            foreach (var f in LocalCacheFiles)
            {
                foreach (var file in Directory.EnumerateFiles(dir, f))
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
            }
        }

        private void DetectFFXIVProcess()
        {
            var ffxiv = XIVPluginHelper.Instance.CurrentFFXIVProcess;
            if (ffxiv == null ||
                ffxiv.HasExited)
            {
                return;
            }

            var ffxivLanguage = XIVPluginHelper.Instance.LanguageID switch
            {
                Locales.EN => GameLanguage.English,
                Locales.JA => GameLanguage.Japanese,
                Locales.FR => GameLanguage.French,
                Locales.DE => GameLanguage.German,
                Locales.KO => GameLanguage.Korean,
                Locales.CN => GameLanguage.Chinese,
                Locales.TW => GameLanguage.Chinese,
                _ => GameLanguage.English
            };

            lock (this)
            {
                if (this._memoryHandler == null ||
                    this.currentFFXIVProcess == null ||
                    this.currentFFXIVProcess.Id != ffxiv.Id ||
                    this.currentFFXIVLanguage != ffxivLanguage ||
                    this.enqueueReload)
                {
                    this.enqueueReload = false;

                    if (this._memoryHandler != null)
                    {
                        SharlayanMemoryManager.Instance.RemoveHandler(this.currentFFXIVProcess.Id);
                    }

                    ClearLocalCaches();

                    this.currentFFXIVProcess = ffxiv;
                    this.currentFFXIVLanguage = ffxivLanguage;

                    var config = new SharlayanConfiguration()
                    {
                        ProcessModel = new ProcessModel()
                        {
                            Process = ffxiv
                        },
                        GameLanguage = this.currentFFXIVLanguage,
                        GameRegion = this.currentFFXIVLanguage switch
                        {
                            GameLanguage.Korean => GameRegion.Korea,
                            GameLanguage.Chinese => GameRegion.China,
                            _ => GameRegion.Global
                        },
                        UseLocalCache = true,
                    };

                    this._memoryHandler = SharlayanMemoryManager.Instance.AddHandler(config);

                    this.ClearData();
                }
            }
        }

        private readonly List<ActorItem> ActorList = new List<ActorItem>(512);
        private readonly List<CombatantEx> ActorPCCombatantList = new List<CombatantEx>(512);
        private readonly List<CombatantEx> ActorCombatantList = new List<CombatantEx>(512);
        private readonly Dictionary<uint, ActorItem> ActorDictionary = new Dictionary<uint, ActorItem>(512);
        private readonly Dictionary<uint, ActorItem> NPCActorDictionary = new Dictionary<uint, ActorItem>(512);

        private readonly Dictionary<uint, CombatantEx> CombatantsDictionary = new Dictionary<uint, CombatantEx>(5120);
        private readonly Dictionary<uint, CombatantEx> NPCCombatantsDictionary = new Dictionary<uint, CombatantEx>(5120);

        public Func<uint, (int WorldID, string WorldName)> GetWorldInfoCallback { get; set; }

        public ActorItem CurrentPlayer { get; private set; }

        public List<ActorItem> Actors => this.ActorList.ToList();

        private static readonly object CombatantsLock = new object();

        public List<CombatantEx> PCCombatants
        {
            get
            {
                lock (CombatantsLock)
                {
                    return this.ActorPCCombatantList.ToList();
                }
            }
        }

        public List<CombatantEx> Combatants
        {
            get
            {
                lock (CombatantsLock)
                {
                    return this.ActorCombatantList.ToList();
                }
            }
        }

        public bool IsExistsActors { get; private set; } = false;

        public ActorItem GetActor(uint id)
        {
            if (this.ActorDictionary.ContainsKey(id))
            {
                return this.ActorDictionary[id];
            }

            return null;
        }

        public ActorItem GetNPCActor(uint id)
        {
            if (this.NPCActorDictionary.ContainsKey(id))
            {
                return this.NPCActorDictionary[id];
            }

            return null;
        }

        public TargetInfo TargetInfo { get; private set; }

        private readonly Dictionary<uint, EnmityEntry> EnmityDictionary = new Dictionary<uint, EnmityEntry>(128);

        public List<EnmityEntry> EnmityList
        {
            get
            {
                lock (this.EnmityDictionary)
                {
                    return this.EnmityDictionary.Values.OrderByDescending(x => x.Enmity).ToList();
                }
            }
        }

        public bool IsFirstEnmityMe { get; private set; }

        public int EnmityMaxCount { get; set; } = 16;

        private readonly List<ActorItem> PartyMemberList = new List<ActorItem>(8);

        public List<ActorItem> PartyMembers => this.PartyMemberList.ToList();

        public int PartyMemberCount { get; private set; }

        public PartyCompositions PartyComposition { get; private set; } = PartyCompositions.Unknown;

        public string CurrentZoneName { get; private set; } = string.Empty;

        public DateTime ZoneChangedTimestamp { get; private set; } = DateTime.MinValue;

        private static readonly object ActionLock = new object();
        private readonly List<ActionItem> actionList = new List<ActionItem>(128);
        private readonly Dictionary<string, ActionItem> actionDictionary = new Dictionary<string, ActionItem>(128);

        public List<ActionItem> Actions
        {
            get
            {
                lock (ActionLock)
                {
                    return this.actionList.ToList();
                }
            }
        }

        public Dictionary<string, ActionItem> ActionDictionary
        {
            get
            {
                lock (ActionLock)
                {
                    return this.actionDictionary.Clone();
                }
            }
        }

        public bool IsScanning
        {
            get => this.isScanningSemaphore > 0;
            set => Interlocked.Exchange(ref this.isScanningSemaphore, value ? 1 : 0);
        }

        public bool TryScanning() => Interlocked.CompareExchange(ref this.isScanningSemaphore, 1, 0) < 1;

        private int isScanningSemaphore = 0;

        private DateTime playerScanTimestamp = DateTime.MinValue;

        private void ScanMemory()
        {
            if (this._memoryHandler == null ||
                !XIVPluginHelper.Instance.IsAvailable)
            {
                Thread.Sleep((int)ProcessSubscribeInterval);
                return;
            }

            if (this.TryScanning())
            {
                try
                {
                    doScan();
                    this.TryGarbageCombatantsDictionary();
                }
                finally
                {
                    this.IsScanning = false;
                }
            }

            void doScan()
            {
                var currentZoneName = XIVPluginHelper.Instance.GetCurrentZoneName();
                if (this.CurrentZoneName != currentZoneName)
                {
                    this.CurrentZoneName = currentZoneName;
                    this.ZoneChangedTimestamp = DateTime.Now;

                    this.IsExistsActors = false;
                    this.ActorList.Clear();
                    this.ActorDictionary.Clear();
                    this.NPCActorDictionary.Clear();
                    this.CombatantsDictionary.Clear();
                    this.NPCCombatantsDictionary.Clear();
                }

                if (!this.IsSkipPlayer &&
                    this.IsSkipActor)
                {
                    if ((DateTime.Now - this.playerScanTimestamp).TotalMilliseconds > 500)
                    {
                        this.playerScanTimestamp = DateTime.Now;
                        this.CurrentPlayer = this._memoryHandler.Reader.GetCurrentPlayer().Entity;
                    }
                }

                if (this.IsSkipActor)
                {
                    if (this.ActorList.Any())
                    {
                        this.IsExistsActors = false;
                        this.ActorList.Clear();
                        this.ActorDictionary.Clear();
                        this.NPCActorDictionary.Clear();
                        this.CombatantsDictionary.Clear();
                        this.NPCCombatantsDictionary.Clear();
                    }
                }
                else
                {
                    this.GetActors();
                }

                if (this.IsSkipTarget)
                {
                    this.TargetInfo = null;
                }
                else
                {
                    this.GetTargetInfo();
                }

                if (this.IsSkipParty)
                {
                    if (this.PartyMemberList.Any())
                    {
                        this.PartyMemberList.Clear();
                    }
                }
                else
                {
                    this.GetPartyInfo();
                }

                this.GetActions();
            }

            if (this.IsSkips.All(x => x))
            {
                Thread.Sleep((int)ProcessSubscribeInterval);
            }
        }

        public bool IsScanNPC { get; set; } = false;

        public bool IsSkipPlayer { get; set; } = false;

        public bool IsSkipActor { get; set; } = false;

        public bool IsSkipTarget { get; set; } = false;

        public bool IsSkipEnmity { get; set; } = false;

        public bool IsSkipParty { get; set; } = false;

        public bool IsSkipActions { get; set; } = false;

        private bool[] IsSkips => new[]
        {
            this.IsSkipActor,
            this.IsSkipTarget,
            this.IsSkipParty,
            this.IsSkipActions,
        };

        private void GetActors()
        {
            var result = this._memoryHandler.Reader.GetActors();

            var actors = !this.IsScanNPC ?
                result.CurrentPCs.Values
                    .Concat(result.CurrentMonsters.Values) :
                result.CurrentPCs.Values
                    .Concat(result.CurrentMonsters.Values)
                        .Concat(result.CurrentNPCs.Values);

            this.CurrentPlayer = this._memoryHandler.Reader.GetCurrentPlayer().Entity;

            if (!actors.Any())
            {
                this.IsExistsActors = false;
                this.ActorList.Clear();
                this.ActorDictionary.Clear();
                this.NPCActorDictionary.Clear();
                this.CombatantsDictionary.Clear();
                this.NPCCombatantsDictionary.Clear();

                lock (CombatantsLock)
                {
                    this.ActorCombatantList.Clear();
                    this.ActorPCCombatantList.Clear();
                }

                return;
            }

            this.IsExistsActors = false;

            this.ActorList.Clear();
            this.ActorList.AddRange(actors);

            lock (CombatantsLock)
            {
                this.ActorDictionary.Clear();
                this.NPCActorDictionary.Clear();

                this.ActorCombatantList.Clear();
                this.ActorPCCombatantList.Clear();

                foreach (var actor in this.ActorList)
                {
                    var combatatnt = this.ToCombatant(actor);
                    this.ActorCombatantList.Add(combatatnt);

                    if (actor.IsNPC())
                    {
                        this.NPCActorDictionary[actor.GetKey()] = actor;
                    }
                    else
                    {
                        this.ActorPCCombatantList.Add(combatatnt);
                        this.ActorDictionary[actor.GetKey()] = actor;
                    }
                }
            }

            this.IsExistsActors = this.ActorList.Count > 0;
        }

        private void GetTargetInfo()
        {
            var result = this._memoryHandler.Reader.GetTargetInfo();

            this.TargetInfo = result.TargetsFound ?
                result.TargetInfo :
                null;

            this.GetEnmity();
        }

        private void GetEnmity()
        {
            if (this.IsSkipEnmity)
            {
                if (this.EnmityDictionary.Count > 0)
                {
                    lock (this.EnmityDictionary)
                    {
                        this.EnmityDictionary.Clear();
                    }
                }

                return;
            }

            lock (this.EnmityDictionary)
            {
                if (this.TargetInfo == null ||
                    !this.TargetInfo.EnmityItems.Any())
                {
                    this.EnmityDictionary.Clear();
                    return;
                }

                var currents = this.EnmityDictionary.Values.ToArray();
                foreach (var current in currents)
                {
                    if (!this.TargetInfo.EnmityItems.Any(x => x.ID == current.ID))
                    {
                        this.EnmityDictionary.Remove(current.ID);
                    }
                }

                var max = this.TargetInfo.EnmityItems.Max(x => x.Enmity);
                var player = CombatantsManager.Instance.Player;
                var first = default(EnmityEntry);

                var combatantDictionary = CombatantsManager.Instance.GetCombatantMainDictionary();

                var count = 0;
                foreach (var source in this.TargetInfo.EnmityItems)
                {
                    if (count >= this.EnmityMaxCount)
                    {
                        break;
                    }

                    Thread.Yield();

                    var existing = false;
                    var enmity = default(EnmityEntry);

                    if (this.EnmityDictionary.ContainsKey(source.ID))
                    {
                        existing = true;
                        enmity = this.EnmityDictionary[source.ID];
                    }
                    else
                    {
                        existing = false;
                        enmity = new EnmityEntry() { ID = source.ID };
                    }

                    enmity.Enmity = source.Enmity;
                    enmity.HateRate = (int)(((double)enmity.Enmity / (double)max) * 100d);
                    enmity.IsMe = enmity.ID == player?.ID;

                    if (first == null)
                    {
                        first = enmity;
                    }

                    if (!existing)
                    {
                        var combatant = combatantDictionary.ContainsKey(enmity.ID) ?
                            combatantDictionary[enmity.ID] :
                            null;

                        enmity.Name = combatant?.Name;
                        enmity.OwnerID = combatant?.OwnerID ?? 0;
                        enmity.Job = (byte)(combatant?.Job ?? 0);

                        if (string.IsNullOrEmpty(enmity.Name))
                        {
                            enmity.Name = CombatantEx.UnknownName;
                        }

                        this.EnmityDictionary[enmity.ID] = enmity;
                    }

                    count++;
                }

                if (first != null)
                {
                    this.IsFirstEnmityMe = first.IsMe;
                }
            }
        }

        private DateTime partyListTimestamp = DateTime.MinValue;

        public DateTime PartyListChangedTimestamp { get; private set; } = DateTime.MinValue;

        private void GetPartyInfo()
        {
            var newPartyList = new List<ActorItem>(8);

            if (!this.IsExistsActors ||
                string.IsNullOrEmpty(this.CurrentZoneName))
            {
                if (this.CurrentPlayer != null)
                {
                    newPartyList.Add(this.CurrentPlayer);
                }

                this.PartyMemberList.Clear();
                this.PartyMemberList.AddRange(newPartyList);
                this.PartyMemberCount = newPartyList.Count();
                this.PartyComposition = PartyCompositions.Unknown;

                return;
            }

            var now = DateTime.Now;
            if ((now - this.partyListTimestamp).TotalSeconds <= 0.5)
            {
                return;
            }

            this.partyListTimestamp = now;

            var result = this._memoryHandler.Reader.GetPartyMembers().PartyMembers.Keys;

            foreach (var id in result)
            {
                var actor = this.GetActor(id);
                if (actor != null)
                {
                    newPartyList.Add(actor);
                }
            }

            if (!newPartyList.Any() &&
                this.CurrentPlayer != null)
            {
                newPartyList.Add(this.CurrentPlayer);
            }

            if (this.PartyMemberList.Count != newPartyList.Count ||
                newPartyList.Except(this.PartyMemberList).Any() ||
                this.PartyMemberList.Except(newPartyList).Any())
            {
                this.PartyListChangedTimestamp = DateTime.Now;
            }

            this.PartyMemberList.Clear();
            this.PartyMemberList.AddRange(newPartyList);
            this.PartyMemberCount = newPartyList.Count();

            var composition = PartyCompositions.Unknown;

            var partyPCCount = newPartyList.Count(x => x.Type == Actor.Type.PC);
            if (partyPCCount == 4)
            {
                composition = PartyCompositions.LightParty;
            }
            else
            {
                if (partyPCCount >= 8)
                {
                    var tanks = this.PartyMemberList.Count(x => x.GetJobInfo().Role == Roles.Tank);
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
                AppLogger.Info($"party composition changed. current={composition} party_count={partyPCCount}");
            }
        }

        public List<CombatantEx> ToCombatantList(
            IEnumerable<ActorItem> actors)
        {
            var combatantList = new List<CombatantEx>(actors.Count());

            foreach (var actor in actors)
            {
                var combatant = this.TryGetOrNewCombatant(actor);
                combatantList.Add(combatant);
                Thread.Yield();
            }

            return combatantList;
        }

        private DateTime lastActionsTimestamp = DateTime.MinValue;

        public double ActionsPollingInterval { get; set; } = 100;

        private void GetActions()
        {
            if (this.IsSkipActions)
            {
                clearActions();
                return;
            }

            var now = DateTime.Now;
            if ((now - this.lastActionsTimestamp).TotalMilliseconds <= this.ActionsPollingInterval)
            {
                return;
            }

            this.lastActionsTimestamp = now;

            if (!this._memoryHandler.Reader.CanGetActions())
            {
                clearActions();
                return;
            }

            var result = this._memoryHandler.Reader.GetActions();
            if (result == null ||
                result.ActionContainers == null ||
                result.ActionContainers.Count < 1)
            {
                clearActions();
                return;
            }

            lock (ActionLock)
            {
                foreach (var container in result.ActionContainers)
                {
                    foreach (var action in container.ActionItems)
                    {
                        this.actionDictionary[action.Name] = action;
                        Thread.Yield();
                    }
                }

                this.actionList.Clear();
                this.actionList.AddRange(this.actionDictionary.Values);
            }

            void clearActions()
            {
                if (this.actionList.Count > 0)
                {
                    lock (ActionLock)
                    {
                        this.actionList.Clear();
                        this.actionDictionary.Clear();
                    }
                }
            }
        }

        public CombatantEx ToCombatant(
            ActorItem actor)
        {
            if (actor == null)
            {
                return null;
            }

            return this.TryGetOrNewCombatant(actor);
        }

        private CombatantEx TryGetOrNewCombatant(
            ActorItem actor)
        {
            if (actor == null)
            {
                return null;
            }

            var combatant = default(CombatantEx);
            var dictionary = !actor.IsNPC() ?
                this.CombatantsDictionary :
                this.NPCCombatantsDictionary;
            var key = actor.GetKey();

            if (dictionary.ContainsKey(key))
            {
                combatant = dictionary[key];
                this.CreateCombatant(actor, combatant);
            }
            else
            {
                combatant = this.CreateCombatant(actor);
                dictionary[key] = combatant;
            }

            return combatant;
        }

        private CombatantEx CreateCombatant(
            ActorItem actor,
            CombatantEx current = null)
        {
            var c = current ?? new CombatantEx();

            c.ActorItem = actor;
            c.ID = actor.ID;
            c.Type = (byte)actor.Type;
            c.Name = actor.Name;
            c.Level = actor.Level;
            c.Job = (byte)actor.Job;
            c.TargetID = (uint)actor.TargetID;
            c.OwnerID = actor.OwnerID;

            c.EffectiveDistance = actor.Distance;

            c.Heading = actor.Heading;
            c.PosX = (float)actor.X;
            c.PosY = (float)actor.Y;
            c.PosZ = (float)actor.Z;

            c.CurrentHP = (uint)actor.HPCurrent;
            c.MaxHP = (uint)actor.HPMax;
            c.CurrentMP = (uint)actor.MPCurrent;

            c.CurrentCP = (uint)actor.CPCurrent;
            c.MaxCP = (uint)actor.CPMax;
            c.CurrentGP = (uint)actor.GPCurrent;
            c.MaxGP = (uint)actor.GPMax;

            c.IsCasting = actor.IsCasting;
            c.CastTargetID = actor.CastingTargetID;
            c.CastBuffID = (uint)actor.CastingID;
            c.CastDurationCurrent = actor.CastingProgress;
            c.CastDurationMax = actor.CastingTime;

            this.SetTargetOfTarget(c);

            CombatantEx.SetSkillName(c);
            c.SetName(actor.Name);

            var worldInfo = GetWorldInfoCallback?.Invoke(c.ID);
            if (worldInfo.HasValue)
            {
                c.WorldID = (uint)worldInfo.Value.WorldID;
                c.WorldName = worldInfo.Value.WorldName ?? string.Empty;
            }

            return c;
        }

        public void SetTargetOfTarget(
            CombatantEx player)
        {
            if (!player.IsPlayer ||
                player.TargetID == 0)
            {
                return;
            }

            if (this.TargetInfo != null &&
                this.TargetInfo.CurrentTarget != null)
            {
                player.TargetOfTargetID = (uint)this.TargetInfo.CurrentTarget?.TargetID;
            }
            else
            {
                player.TargetOfTargetID = 0;
            }
        }

        private DateTime lastestGarbageTimestamp = DateTime.MinValue;

        private void TryGarbageCombatantsDictionary()
        {
            if (XIVPluginHelper.Instance.InCombat)
            {
                return;
            }

            if ((DateTime.Now - this.lastestGarbageTimestamp) >= TimeSpan.FromMinutes(3))
            {
                this.lastestGarbageTimestamp = DateTime.Now;
                this.GarbageCombatantsDictionary();
            }
        }

        private void GarbageCombatantsDictionary()
        {
            Task.WaitAll(
                Task.Run(() =>
                {
                    var dic = this.CombatantsDictionary;
                    var keys = dic
                        .Where(x => (DateTime.Now - x.Value.Timestamp) >= TimeSpan.FromMinutes(2.9))
                        .Select(x => x.Key)
                        .ToArray();

                    foreach (var key in keys)
                    {
                        dic.Remove(key);
                    }
                }),
                Task.Run(() =>
                {
                    var dic = this.NPCCombatantsDictionary;
                    var keys = dic
                        .Where(x => (DateTime.Now - x.Value.Timestamp) >= TimeSpan.FromMinutes(2.9))
                        .Select(x => x.Key)
                        .ToArray();

                    foreach (var key in keys)
                    {
                        dic.Remove(key);
                    }
                }));
        }
    }

    public static class ActorItemExtensions
    {
        public static List<CombatantEx> ToCombatantList(
            this IEnumerable<ActorItem> actors)
            => SharlayanHelper.Instance.ToCombatantList(actors);

        public static uint GetKey(
            this ActorItem actor)
            => actor.IsNPC() ? actor.NPCID2 : actor.ID;

        public static bool IsNPC(
            this ActorItem actor)
            => IsNPC(actor?.Type);

        public static bool IsNPC(
            this CombatantEx actor)
            => IsNPC(actor?.ActorType);

        private static bool IsNPC(
            Actor.Type? actorType)
        {
            if (!actorType.HasValue)
            {
                return true;
            }

            switch (actorType)
            {
                case Actor.Type.NPC:
                case Actor.Type.Aetheryte:
                case Actor.Type.EventObject:
                    return true;

                default:
                    return false;
            }
        }

        public static JobIDs GetJobID(
            this ActorItem actor)
        {
            var id = actor.JobID;
            var jobEnum = JobIDs.Unknown;

            if (Enum.IsDefined(typeof(JobIDs), (int)id))
            {
                jobEnum = (JobIDs)Enum.ToObject(typeof(JobIDs), (int)id);
            }

            return jobEnum;
        }

        public static Job GetJobInfo(
            this ActorItem actor)
        {
            var jobEnum = actor.GetJobID();
            return Jobs.Find(jobEnum);
        }
    }
}
