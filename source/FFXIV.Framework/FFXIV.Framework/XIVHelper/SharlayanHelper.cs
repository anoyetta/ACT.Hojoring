using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using Sharlayan;
using Sharlayan.Core;
using Sharlayan.Core.Enums;
using Sharlayan.Models;
using Sharlayan.Models.ReadResults;
using Sharlayan.Models.Structures;

using ActorItem = Sharlayan.Core.ActorItem;
using EnmityItem = Sharlayan.Core.EnmityItem;
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
            ReaderEx.GetActorCallback = id =>
            {
                if (this.ActorDictionary.ContainsKey(id))
                {
                    return this.ActorDictionary[id];
                }

                return null;
            };

            ReaderEx.GetNPCActorCallback = id =>
            {
                if (this.NPCActorDictionary.ContainsKey(id))
                {
                    return this.NPCActorDictionary[id];
                }

                return null;
            };
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

        private void ClearData()
        {
            lock (this.ActorList)
            {
                this.ActorList.Clear();
            }
        }

        private Process currentFFXIVProcess;
        private string currentFFXIVLanguage;

        private static readonly string[] LocalCacheFiles = new[]
        {
            "actions.json",
            "signatures-x64.json",
            "statuses.json",
            "structures-x64.json",
            "zones.json"
        };

        private static void ClearLocalCaches()
        {
            var dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            foreach (var f in LocalCacheFiles)
            {
                var file = Path.Combine(dir, f);
                if (File.Exists(file))
                {
                    File.Delete(file);
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
                Locales.EN => "English",
                Locales.JA => "Japanese",
                Locales.FR => "French",
                Locales.DE => "German",
                _ => "English",
            };

            lock (this)
            {
                if (!MemoryHandler.Instance.IsAttached ||
                    this.currentFFXIVProcess == null ||
                    this.currentFFXIVProcess.Id != ffxiv.Id ||
                    this.currentFFXIVLanguage != ffxivLanguage)
                {
                    this.currentFFXIVProcess = ffxiv;
                    this.currentFFXIVLanguage = ffxivLanguage;

                    if (MemoryHandler.Instance.IsAttached)
                    {
                        MemoryHandler.Instance.UnsetProcess();
                    }

                    var model = new ProcessModel
                    {
                        Process = ffxiv,
                        IsWin64 = true
                    };

                    ClearLocalCaches();

                    MemoryHandler.Instance.SetProcess(
                        model,
                        gameLanguage: ffxivLanguage,
                        useLocalCache: true);

                    ReaderEx.SetProcessModel(model);

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

        public ActorItem CurrentPlayer => ReaderEx.CurrentPlayer;

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
            if (!MemoryHandler.Instance.IsAttached ||
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
                        ReaderEx.GetActorSimple(
                            isScanNPC: false,
                            isPlayerOnly: true);
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
                    this.GetActorsSimple();
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

        private void GetActorsSimple()
        {
            var actors = ReaderEx.GetActorSimple(this.IsScanNPC);

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
            var result = ReaderEx.GetTargetInfoSimple(this.IsSkipEnmity);

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
                if (ReaderEx.CurrentPlayer != null)
                {
                    newPartyList.Add(ReaderEx.CurrentPlayer);
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

            var result = ReaderEx.GetPartyMemberIDs();

            foreach (var id in result)
            {
                var actor = this.GetActor(id);
                if (actor != null)
                {
                    newPartyList.Add(actor);
                }
            }

            if (!newPartyList.Any() &&
                ReaderEx.CurrentPlayer != null)
            {
                newPartyList.Add(ReaderEx.CurrentPlayer);
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

            if (!Reader.CanGetActions())
            {
                clearActions();
                return;
            }

            var result = Reader.GetActions();
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
            c.CurrentTP = (uint)actor.TPCurrent;
            c.MaxTP = (uint)actor.TPMax;

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

    public static class ReaderEx
    {
        public static ActorItem CurrentPlayer { get; private set; }

        public static CombatantEx CurrentPlayerCombatant { get; private set; }

        public static ProcessModel ProcessModel { get; private set; }

        public static Func<uint, ActorItem> GetActorCallback { get; set; }

        public static Func<uint, ActorItem> GetNPCActorCallback { get; set; }

        private static Func<StructuresContainer> getStructuresDelegate;

        private static Func<StructuresContainer> GetStructuresDelegate => getStructuresDelegate ??= CreateGetStructuresDelegate();

        private static Func<StructuresContainer> CreateGetStructuresDelegate()
        {
            var property = MemoryHandler.Instance.GetType().GetProperty(
                "Structures",
                BindingFlags.NonPublic | BindingFlags.Instance);

            return (Func<StructuresContainer>)Delegate.CreateDelegate(
                typeof(Func<StructuresContainer>),
                MemoryHandler.Instance,
                property.GetMethod);
        }

#if false
        private static readonly Lazy<Func<byte[], bool, ActorItem, ActorItem>> LazyResolveActorFromBytesDelegate = new Lazy<Func<byte[], bool, ActorItem, ActorItem>>(() =>
        {
            var asm = MemoryHandler.Instance.GetType().Assembly;
            var type = asm.GetType("Sharlayan.Utilities.ActorItemResolver");

            var method = type.GetMethod(
                "ResolveActorFromBytes",
                BindingFlags.Static | BindingFlags.Public);

            return (Func<byte[], bool, ActorItem, ActorItem>)Delegate.CreateDelegate(
                typeof(Func<byte[], bool, ActorItem, ActorItem>),
                null,
                method);
        });

        private static ActorItem InvokeActorItemResolver(
            byte[] source,
            bool isCurrentUser = false,
            ActorItem entry = null)
            => LazyResolveActorFromBytesDelegate.Value.Invoke(
                source,
                isCurrentUser,
                entry);

        private static readonly Lazy<Func<byte[], ActorItem, PartyMember>> LazyResolvePartyMemberFromBytesDelegate = new Lazy<Func<byte[], ActorItem, PartyMember>>(() =>
        {
            var asm = MemoryHandler.Instance.GetType().Assembly;
            var type = asm.GetType("Sharlayan.Utilities.PartyMemberResolver");

            var method = type.GetMethod(
                "ResolvePartyMemberFromBytes",
                BindingFlags.Static | BindingFlags.Public);

            return (Func<byte[], ActorItem, PartyMember>)Delegate.CreateDelegate(
                typeof(Func<byte[], ActorItem, PartyMember>),
                null,
                method);
        });

        private static PartyMember InvokePartyMemberResolver(
            byte[] source,
            ActorItem entry = null)
            => LazyResolvePartyMemberFromBytesDelegate.Value.Invoke(
                source,
                entry);
#endif

        public static void SetProcessModel(
            ProcessModel model)
        {
            ProcessModel = model;
            getStructuresDelegate = null;
        }

        public static List<ActorItem> GetActorSimple(
            bool isScanNPC = false,
            bool isPlayerOnly = false)
        {
            var result = new List<ActorItem>(256);

            if (!Reader.CanGetActors() || !MemoryHandler.Instance.IsAttached)
            {
                return result;
            }

            var isWin64 = ProcessModel?.IsWin64 ?? true;

            var targetAddress = IntPtr.Zero;
            var endianSize = isWin64 ? 8 : 4;

            var structures = GetStructuresDelegate.Invoke();
            var sourceSize = structures.ActorItem.SourceSize;
            var limit = structures.ActorItem.EntityCount;
            var characterAddressMap = MemoryHandler.Instance.GetByteArray(Scanner.Instance.Locations[Signatures.CharacterMapKey], endianSize * limit);
            var uniqueAddresses = new Dictionary<IntPtr, IntPtr>();
            var firstAddress = IntPtr.Zero;

            var firstTime = true;
            for (var i = 0; i < limit; i++)
            {
                Thread.Yield();

                var characterAddress = isWin64 ?
                    new IntPtr(BitConverter.TryToInt64(characterAddressMap, i * endianSize)) :
                    new IntPtr(BitConverter.TryToInt32(characterAddressMap, i * endianSize));

                if (characterAddress == IntPtr.Zero)
                {
                    continue;
                }

                if (firstTime)
                {
                    firstAddress = characterAddress;
                    firstTime = false;
                }

                uniqueAddresses[characterAddress] = characterAddress;
            }

            foreach (var kvp in uniqueAddresses)
            {
                Thread.Yield();

                var characterAddress = new IntPtr(kvp.Value.ToInt64());
                var source = MemoryHandler.Instance.GetByteArray(characterAddress, sourceSize);

                var ID = BitConverter.TryToUInt32(source, structures.ActorItem.ID);
                var NPCID2 = BitConverter.TryToUInt32(source, structures.ActorItem.NPCID2);
                var Type = (Actor.Type)source[structures.ActorItem.Type];

                var existing = default(ActorItem);
                switch (Type)
                {
                    case Actor.Type.Aetheryte:
                    case Actor.Type.EventObject:
                    case Actor.Type.NPC:
                        if (!isScanNPC)
                        {
                            continue;
                        }

                        existing = GetNPCActorCallback?.Invoke(NPCID2);
                        break;

                    default:
                        existing = GetActorCallback?.Invoke(ID);
                        break;
                }

                var isFirstEntry = kvp.Value.ToInt64() == firstAddress.ToInt64();
                var entry = ResolveActorFromBytes(structures, source, isFirstEntry, existing);

                if (isFirstEntry)
                {
                    CurrentPlayer = entry;
                    CurrentPlayerCombatant = SharlayanHelper.Instance.ToCombatant(entry);

                    if (targetAddress.ToInt64() > 0)
                    {
                        var targetInfoSource = MemoryHandler.Instance.GetByteArray(targetAddress, 128);
                        entry.TargetID = (int)BitConverter.TryToInt32(targetInfoSource, structures.ActorItem.ID);
                    }

                    if (isPlayerOnly)
                    {
                        result.Add(entry);
                        break;
                    }
                }

                result.Add(entry);
            }

            return result;
        }

        public static uint[] GetPartyMemberIDs()
        {
            var result = new List<uint>(8);

            if (!Reader.CanGetPartyMembers() || !MemoryHandler.Instance.IsAttached)
            {
                return result.ToArray();
            }

            var structures = GetStructuresDelegate.Invoke();

            var PartyInfoMap = (IntPtr)Scanner.Instance.Locations[Signatures.PartyMapKey];
            var PartyCountMap = Scanner.Instance.Locations[Signatures.PartyCountKey];

            var partyCount = MemoryHandler.Instance.GetByte(PartyCountMap);
            var sourceSize = structures.PartyMember.SourceSize;

            if (partyCount > 1 && partyCount < 9)
            {
                for (uint i = 0; i < partyCount; i++)
                {
                    var address = PartyInfoMap.ToInt64() + i * (uint)sourceSize;
                    var source = MemoryHandler.Instance.GetByteArray(new IntPtr(address), sourceSize);
                    var ID = BitConverter.TryToUInt32(source, structures.PartyMember.ID);

                    result.Add(ID);
                    Thread.Yield();
                }
            }

            return result.ToArray();
        }

#if false
        public static List<PartyMember> GetPartyMemberSimple()
        {
            var result = new List<PartyMember>(8);

            if (!Reader.CanGetPartyMembers() || !MemoryHandler.Instance.IsAttached)
            {
                return result;
            }

            var structures = LazyGetStructuresDelegate.Value.Invoke();

            var PartyInfoMap = (IntPtr)Scanner.Instance.Locations[Signatures.PartyMapKey];
            var PartyCountMap = Scanner.Instance.Locations[Signatures.PartyCountKey];

            var partyCount = MemoryHandler.Instance.GetByte(PartyCountMap);
            var sourceSize = structures.PartyMember.SourceSize;

            if (partyCount > 1 && partyCount < 9)
            {
                for (uint i = 0; i < partyCount; i++)
                {
                    var address = PartyInfoMap.ToInt64() + i * (uint)sourceSize;
                    var source = MemoryHandler.Instance.GetByteArray(new IntPtr(address), sourceSize);

                    var ID = BitConverter.TryToUInt32(source, structures.ActorItem.ID);
                    var NPCID2 = BitConverter.TryToUInt32(source, structures.ActorItem.NPCID2);
                    var Type = (Actor.Type)source[structures.ActorItem.Type];

                    var existing = default(ActorItem);
                    switch (Type)
                    {
                        case Actor.Type.Aetheryte:
                        case Actor.Type.EventObject:
                        case Actor.Type.NPC:
                            existing = GetNPCActorCallback?.Invoke(NPCID2);
                            break;

                        default:
                            existing = GetActorCallback?.Invoke(ID);
                            break;
                    }

                    var entry = InvokePartyMemberResolver(source, existing);

                    result.Add(entry);
                    Thread.Yield();
                }
            }

            if (partyCount <= 1)
            {
                if (CurrentPlayer != null)
                {
                    result.Add(InvokePartyMemberResolver(Array.Empty<byte>(), CurrentPlayer));
                }
            }

            return result;
        }
#endif

        public static ActorItem ResolveActorFromBytes(StructuresContainer structures, byte[] source, bool isCurrentUser = false, ActorItem entry = null)
        {
            entry = entry ?? new ActorItem();

            var defaultBaseOffset = structures.ActorItem.DefaultBaseOffset;
            var defaultStatOffset = structures.ActorItem.DefaultStatOffset;
            var defaultStatusEffectOffset = structures.ActorItem.DefaultStatusEffectOffset;

            entry.MapTerritory = 0;
            entry.MapIndex = 0;
            entry.MapID = 0;
            entry.TargetID = 0;
            entry.Name = MemoryHandler.Instance.GetStringFromBytes(source, structures.ActorItem.Name);
            entry.ID = BitConverter.TryToUInt32(source, structures.ActorItem.ID);
            entry.UUID = string.IsNullOrEmpty(entry.UUID)
                             ? Guid.NewGuid().ToString()
                             : entry.UUID;
            entry.NPCID1 = BitConverter.TryToUInt32(source, structures.ActorItem.NPCID1);
            entry.NPCID2 = BitConverter.TryToUInt32(source, structures.ActorItem.NPCID2);
            entry.OwnerID = BitConverter.TryToUInt32(source, structures.ActorItem.OwnerID);
            entry.TypeID = source[structures.ActorItem.Type];
            entry.Type = (Actor.Type)entry.TypeID;

            entry.TargetTypeID = source[structures.ActorItem.TargetType];
            entry.TargetType = (Actor.TargetType)entry.TargetTypeID;

            entry.GatheringStatus = source[structures.ActorItem.GatheringStatus];
            entry.Distance = source[structures.ActorItem.Distance];

            entry.X = BitConverter.TryToSingle(source, structures.ActorItem.X + defaultBaseOffset);
            entry.Z = BitConverter.TryToSingle(source, structures.ActorItem.Z + defaultBaseOffset);
            entry.Y = BitConverter.TryToSingle(source, structures.ActorItem.Y + defaultBaseOffset);
            entry.Heading = BitConverter.TryToSingle(source, structures.ActorItem.Heading + defaultBaseOffset);
            entry.HitBoxRadius = BitConverter.TryToSingle(source, structures.ActorItem.HitBoxRadius + defaultBaseOffset);
            entry.Fate = BitConverter.TryToUInt32(source, structures.ActorItem.Fate + defaultBaseOffset); // ??
            entry.TargetFlags = source[structures.ActorItem.TargetFlags]; // ??
            entry.GatheringInvisible = source[structures.ActorItem.GatheringInvisible]; // ??
            entry.ModelID = BitConverter.TryToUInt32(source, structures.ActorItem.ModelID);
            entry.ActionStatusID = source[structures.ActorItem.ActionStatus];
            entry.ActionStatus = (Actor.ActionStatus)entry.ActionStatusID;

            // 0x17D - 0 = Green name, 4 = non-agro (yellow name)
            entry.IsGM = BitConverter.TryToBoolean(source, structures.ActorItem.IsGM); // ?
            entry.IconID = source[structures.ActorItem.Icon];
            entry.Icon = (Actor.Icon)entry.IconID;

            entry.StatusID = source[structures.ActorItem.Status];
            entry.Status = (Actor.Status)entry.StatusID;

            entry.ClaimedByID = BitConverter.TryToUInt32(source, structures.ActorItem.ClaimedByID);
            var targetID = BitConverter.TryToUInt32(source, structures.ActorItem.TargetID);
            var pcTargetID = targetID;

            entry.JobID = source[structures.ActorItem.Job + defaultStatOffset];
            entry.Job = (Actor.Job)entry.JobID;

            entry.Level = source[structures.ActorItem.Level + defaultStatOffset];
            entry.GrandCompany = source[structures.ActorItem.GrandCompany + defaultStatOffset];
            entry.GrandCompanyRank = source[structures.ActorItem.GrandCompanyRank + defaultStatOffset];
            entry.Title = source[structures.ActorItem.Title + defaultStatOffset];
            entry.HPCurrent = BitConverter.TryToInt32(source, structures.ActorItem.HPCurrent + defaultStatOffset);
            entry.HPMax = BitConverter.TryToInt32(source, structures.ActorItem.HPMax + defaultStatOffset);
            entry.MPCurrent = BitConverter.TryToInt32(source, structures.ActorItem.MPCurrent + defaultStatOffset);
            entry.MPMax = BitConverter.TryToInt32(source, structures.ActorItem.MPMax + defaultStatOffset);
            entry.TPCurrent = BitConverter.TryToInt16(source, structures.ActorItem.TPCurrent + defaultStatOffset);
            entry.TPMax = 1000;
            entry.GPCurrent = BitConverter.TryToInt16(source, structures.ActorItem.GPCurrent + defaultStatOffset);
            entry.GPMax = BitConverter.TryToInt16(source, structures.ActorItem.GPMax + defaultStatOffset);
            entry.CPCurrent = BitConverter.TryToInt16(source, structures.ActorItem.CPCurrent + defaultStatOffset);
            entry.CPMax = BitConverter.TryToInt16(source, structures.ActorItem.CPMax + defaultStatOffset);

            // entry.Race = source[0x2578]; // ??
            // entry.Sex = (Actor.Sex) source[0x2579]; //?
            entry.AgroFlags = source[structures.ActorItem.AgroFlags];
            entry.CombatFlags = source[structures.ActorItem.CombatFlags];
            entry.DifficultyRank = source[structures.ActorItem.DifficultyRank];
            entry.CastingID = BitConverter.TryToInt16(source, structures.ActorItem.CastingID); // 0x2C94);
            entry.CastingTargetID = BitConverter.TryToUInt32(source, structures.ActorItem.CastingTargetID); // 0x2CA0);
            entry.CastingProgress = BitConverter.TryToSingle(source, structures.ActorItem.CastingProgress); // 0x2CC4);
            entry.CastingTime = BitConverter.TryToSingle(source, structures.ActorItem.CastingTime); // 0x2DA8);
            entry.Coordinate = new Coordinate(entry.X, entry.Z, entry.Y);
            if (targetID > 0)
            {
                entry.TargetID = (int)targetID;
            }
            else
            {
                if (pcTargetID > 0)
                {
                    entry.TargetID = (int)pcTargetID;
                }
            }

            if (entry.CastingTargetID == 3758096384)
            {
                entry.CastingTargetID = 0;
            }

            entry.MapIndex = 0;

            // handle empty names
            if (string.IsNullOrEmpty(entry.Name))
            {
                if (entry.Type == Actor.Type.EventObject)
                {
                    entry.Name = $"{nameof(entry.EventObjectTypeID)}: {entry.EventObjectTypeID}";
                }
                else
                {
                    entry.Name = $"{nameof(entry.TypeID)}: {entry.TypeID}";
                }
            }

            CleanXPValue(ref entry);

            return entry;
        }

        private static void CleanXPValue(ref ActorItem entity)
        {
            if (entity.HPCurrent < 0 || entity.HPMax < 0)
            {
                entity.HPCurrent = 1;
                entity.HPMax = 1;
            }

            if (entity.HPCurrent > entity.HPMax)
            {
                if (entity.HPMax == 0)
                {
                    entity.HPCurrent = 1;
                    entity.HPMax = 1;
                }
                else
                {
                    entity.HPCurrent = entity.HPMax;
                }
            }

            if (entity.MPCurrent < 0 || entity.MPMax < 0)
            {
                entity.MPCurrent = 1;
                entity.MPMax = 1;
            }

            if (entity.MPCurrent > entity.MPMax)
            {
                if (entity.MPMax == 0)
                {
                    entity.MPCurrent = 1;
                    entity.MPMax = 1;
                }
                else
                {
                    entity.MPCurrent = entity.MPMax;
                }
            }

            if (entity.GPCurrent < 0 || entity.GPMax < 0)
            {
                entity.GPCurrent = 1;
                entity.GPMax = 1;
            }

            if (entity.GPCurrent > entity.GPMax)
            {
                if (entity.GPMax == 0)
                {
                    entity.GPCurrent = 1;
                    entity.GPMax = 1;
                }
                else
                {
                    entity.GPCurrent = entity.GPMax;
                }
            }

            if (entity.CPCurrent < 0 || entity.CPMax < 0)
            {
                entity.CPCurrent = 1;
                entity.CPMax = 1;
            }

            if (entity.CPCurrent > entity.CPMax)
            {
                if (entity.CPMax == 0)
                {
                    entity.CPCurrent = 1;
                    entity.CPMax = 1;
                }
                else
                {
                    entity.CPCurrent = entity.CPMax;
                }
            }
        }

        public static TargetResult GetTargetInfoSimple(
            bool isSkipEnmity = false)
        {
            var result = new TargetResult();

            if (!Reader.CanGetTargetInfo() || !MemoryHandler.Instance.IsAttached)
            {
                return result;
            }

            var structures = GetStructuresDelegate.Invoke();
            var targetAddress = (IntPtr)Scanner.Instance.Locations[Signatures.TargetKey];

            if (targetAddress.ToInt64() > 0)
            {
                byte[] targetInfoSource = MemoryHandler.Instance.GetByteArray(targetAddress, structures.TargetInfo.SourceSize);

                var currentTarget = MemoryHandler.Instance.GetPlatformIntFromBytes(targetInfoSource, structures.TargetInfo.Current);
                var mouseOverTarget = MemoryHandler.Instance.GetPlatformIntFromBytes(targetInfoSource, structures.TargetInfo.MouseOver);
                var focusTarget = MemoryHandler.Instance.GetPlatformIntFromBytes(targetInfoSource, structures.TargetInfo.Focus);
                var previousTarget = MemoryHandler.Instance.GetPlatformIntFromBytes(targetInfoSource, structures.TargetInfo.Previous);

                var currentTargetID = BitConverter.TryToUInt32(targetInfoSource, structures.TargetInfo.CurrentID);

                if (currentTarget > 0)
                {
                    ActorItem entry = GetTargetActorItemFromSource(structures, currentTarget);
                    currentTargetID = entry.ID;
                    if (entry.IsValid)
                    {
                        result.TargetsFound = true;
                        result.TargetInfo.CurrentTarget = entry;
                    }
                }

                if (mouseOverTarget > 0)
                {
                    ActorItem entry = GetTargetActorItemFromSource(structures, mouseOverTarget);
                    if (entry.IsValid)
                    {
                        result.TargetsFound = true;
                        result.TargetInfo.MouseOverTarget = entry;
                    }
                }

                if (focusTarget > 0)
                {
                    ActorItem entry = GetTargetActorItemFromSource(structures, focusTarget);
                    if (entry.IsValid)
                    {
                        result.TargetsFound = true;
                        result.TargetInfo.FocusTarget = entry;
                    }
                }

                if (previousTarget > 0)
                {
                    ActorItem entry = GetTargetActorItemFromSource(structures, previousTarget);
                    if (entry.IsValid)
                    {
                        result.TargetsFound = true;
                        result.TargetInfo.PreviousTarget = entry;
                    }
                }

                if (currentTargetID > 0)
                {
                    result.TargetsFound = true;
                    result.TargetInfo.CurrentTargetID = currentTargetID;
                }
            }

            if (isSkipEnmity)
            {
                return result;
            }

            if (result.TargetInfo.CurrentTargetID > 0)
            {
                if (Reader.CanGetEnmityEntities())
                {
                    const int EnmityLimit = 16;

                    var enmityCount = MemoryHandler.Instance.GetInt16(Scanner.Instance.Locations[Signatures.EnmityCountKey]);
                    var enmityStructure = (IntPtr)Scanner.Instance.Locations[Signatures.EnmityMapKey];

                    if (enmityCount > EnmityLimit)
                    {
                        enmityCount = EnmityLimit;
                    }

                    if (enmityCount > 0 && enmityStructure.ToInt64() > 0)
                    {
                        var enmitySourceSize = structures.EnmityItem.SourceSize;
                        for (uint i = 0; i < enmityCount; i++)
                        {
                            Thread.Yield();

                            var address = new IntPtr(enmityStructure.ToInt64() + (i * enmitySourceSize));
                            var enmityEntry = new EnmityItem
                            {
                                ID = (uint)MemoryHandler.Instance.GetPlatformInt(address, structures.EnmityItem.ID),
                                Name = MemoryHandler.Instance.GetString(address + structures.EnmityItem.Name),
                                Enmity = MemoryHandler.Instance.GetUInt32(address + structures.EnmityItem.Enmity)
                            };

                            if (enmityEntry.ID <= 0)
                            {
                                continue;
                            }

                            result.TargetInfo.EnmityItems.Add(enmityEntry);
                        }
                    }
                }
            }

            return result;
        }

        private static ActorItem GetTargetActorItemFromSource(StructuresContainer structures, long address)
        {
            var targetAddress = new IntPtr(address);

            byte[] source = MemoryHandler.Instance.GetByteArray(targetAddress, structures.TargetInfo.Size);
            ActorItem entry = ResolveActorFromBytes(structures, source);

            return entry;
        }
    }

    internal static class BitConverter
    {
        public static bool TryToBoolean(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToBoolean(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static char TryToChar(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToChar(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static double TryToDouble(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToDouble(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static long TryToDoubleToInt64Bits(double value)
        {
            try
            {
                return System.BitConverter.DoubleToInt64Bits(value);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static short TryToInt16(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToInt16(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static int TryToInt32(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToInt32(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static long TryToInt64(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToInt64(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static double TryToInt64BitsToDouble(long value)
        {
            try
            {
                return System.BitConverter.Int64BitsToDouble(value);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static float TryToSingle(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToSingle(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static string TryToString(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToString(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static ushort TryToUInt16(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToUInt16(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static uint TryToUInt32(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToUInt32(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static ulong TryToUInt64(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToUInt64(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
