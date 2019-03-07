using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using Sharlayan;
using Sharlayan.Core.Enums;
using Sharlayan.Models;
using Sharlayan.Models.Structures;

using ActorItem = Sharlayan.Core.ActorItem;
using PartyMember = Sharlayan.Core.PartyMember;
using TargetInfo = Sharlayan.Core.TargetInfo;

namespace FFXIV.Framework.FFXIVHelper
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
        private static readonly double ProcessSubscribeInterval = 5000;

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
                        ThreadPriority.Lowest);

                    this.memorySubscriber.Run();
                }
            }
        }

        public void End()
        {
            lock (this)
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

        private void DetectFFXIVProcess()
        {
            var ffxiv = FFXIVPlugin.Instance.Process;
            var ffxivLanguage = string.Empty;

            switch (FFXIVPlugin.Instance.LanguageID)
            {
                case Locales.EN:
                    ffxivLanguage = "English";
                    break;

                case Locales.JA:
                    ffxivLanguage = "Japanese";
                    break;

                case Locales.FR:
                    ffxivLanguage = "French";
                    break;

                case Locales.DE:
                    ffxivLanguage = "German";
                    break;

                default:
                    ffxivLanguage = "English";
                    break;
            }

            if (ffxiv == null)
            {
                return;
            }

            lock (this)
            {
                if (!MemoryHandler.Instance.IsAttached ||
                    this.currentFFXIVProcess != ffxiv ||
                    this.currentFFXIVLanguage != ffxivLanguage)
                {
                    this.currentFFXIVProcess = ffxiv;
                    this.currentFFXIVLanguage = ffxivLanguage;

                    var model = new ProcessModel
                    {
                        Process = ffxiv,
                        IsWin64 = true
                    };

                    MemoryHandler.Instance.SetProcess(
                        model,
                        ffxivLanguage);

                    ReaderEx.ProcessModel = model;

                    this.ClearData();
                }
            }

            this.GarbageCombatantsDictionary();
        }

        private readonly List<ActorItem> ActorList = new List<ActorItem>(512);
        private readonly Dictionary<uint, ActorItem> ActorDictionary = new Dictionary<uint, ActorItem>(512);
        private readonly Dictionary<uint, ActorItem> NPCActorDictionary = new Dictionary<uint, ActorItem>(512);

        private static readonly object CombatantLock = new object();
        private readonly Dictionary<uint, Combatant> CombatantsDictionary = new Dictionary<uint, Combatant>(5120);
        private readonly Dictionary<uint, Combatant> NPCCombatantsDictionary = new Dictionary<uint, Combatant>(5120);

        public Func<uint, (int WorldID, string WorldName)> GetWorldInfoCallback { get; set; }

        public ActorItem CurrentPlayer => ReaderEx.CurrentPlayer;

        public List<ActorItem> Actors
        {
            get
            {
                return this.ActorList.ToList();
            }
        }

        public ActorItem GetActor(uint id)
        {
            lock (this.ActorList)
            {
                if (this.ActorDictionary.ContainsKey(id))
                {
                    return this.ActorDictionary[id];
                }
            }

            return null;
        }

        public ActorItem GetNPCActor(uint id)
        {
            lock (this.ActorList)
            {
                if (this.NPCActorDictionary.ContainsKey(id))
                {
                    return this.NPCActorDictionary[id];
                }
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

        private readonly List<ActorItem> PartyMemberList = new List<ActorItem>(8);

        public List<ActorItem> PartyMembers
        {
            get
            {
                lock (this.PartyMemberList)
                {
                    return this.PartyMemberList.ToList();
                }
            }
        }

        public int PartyMemberCount { get; private set; }

        public PartyCompositions PartyComposition { get; private set; } = PartyCompositions.Unknown;

        private void ScanMemory()
        {
            if (!MemoryHandler.Instance.IsAttached ||
                FFXIVPlugin.Instance.Process == null)
            {
                Thread.Sleep((int)ProcessSubscribeInterval);
                return;
            }

            if (this.IsSkipActor)
            {
                if (this.ActorList.Any())
                {
                    lock (this.ActorList)
                    {
                        this.ActorList.Clear();
                        this.ActorDictionary.Clear();
                        this.NPCActorDictionary.Clear();
                    }
                }
            }
            else
            {
                lock (this.ActorList)
                {
                    this.GetActorsSimple();
                }
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
                    lock (this.PartyMemberList)
                    {
                        this.PartyMemberList.Clear();
                    }
                }
            }
            else
            {
                lock (this.PartyMemberList)
                {
                    this.GetPartyInfo();
                }
            }

            if (this.IsSkips.All(x => x))
            {
                Thread.Sleep((int)ProcessSubscribeInterval);
            }
        }

        public bool IsSkipActor { get; set; } = false;

        public bool IsSkipTarget { get; set; } = false;

        public bool IsSkipParty { get; set; } = false;

        private bool[] IsSkips => new[]
        {
            this.IsSkipActor,
            this.IsSkipTarget,
            this.IsSkipParty,
        };

        private void GetActorsSimple()
        {
            var actors = ReaderEx.GetActorSimple();

            if (!actors.Any())
            {
                this.ActorList.Clear();
                this.ActorDictionary.Clear();
                this.NPCActorDictionary.Clear();
                return;
            }

            this.ActorList.Clear();
            this.ActorList.AddRange(actors);

            this.ActorDictionary.Clear();
            this.NPCActorDictionary.Clear();

            foreach (var actor in this.ActorList)
            {
                if (actor.IsNPC())
                {
                    this.NPCActorDictionary[actor.GetKey()] = actor;
                }
                else
                {
                    this.ActorDictionary[actor.GetKey()] = actor;
                }
            }
        }

        private void GetTargetInfo()
        {
            var result = Reader.GetTargetInfo();

            this.TargetInfo = result.TargetsFound ?
                result.TargetInfo :
                null;

            this.GetEnmity();
        }

        private void GetEnmity()
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
            var player = this.CurrentPlayer;

            foreach (var source in this.TargetInfo.EnmityItems)
            {
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

                if (!existing)
                {
                    enmity.Name = source.Name;

                    var actor = this.ActorDictionary.ContainsKey(enmity.ID) ?
                        this.ActorDictionary[enmity.ID] :
                        null;

                    enmity.OwnerID = actor?.OwnerID ?? 0;
                    enmity.Job = (byte)(actor?.Job ?? 0);

                    this.EnmityDictionary[enmity.ID] = enmity;
                }
            }
        }

        private uint[] previousPartyMemberIDs = new uint[0];

        private void GetPartyInfo()
        {
            var result = ReaderEx.GetPartyMemberIDs();

            if (!result.SequenceEqual(previousPartyMemberIDs))
            {
                this.PartyMemberList.Clear();

                foreach (var id in result)
                {
                    var actor = this.GetActor(id);
                    if (actor != null)
                    {
                        this.PartyMemberList.Add(actor);
                    }
                }

                if (!this.PartyMemberList.Any() &&
                    ReaderEx.CurrentPlayer != null)
                {
                    this.PartyMemberList.Add(ReaderEx.CurrentPlayer);
                }

                this.PartyMemberCount = this.PartyMemberList.Count();

                var composition = PartyCompositions.Unknown;

                if (this.PartyMemberCount == 4)
                {
                    this.PartyComposition = PartyCompositions.LightParty;
                }
                else
                {
                    if (this.PartyMemberCount == 8)
                    {
                        var tanks = this.PartyMemberList.Count(x => x.GetJobInfo().Role == Roles.Tank);
                        switch (tanks)
                        {
                            case 1:
                                this.PartyComposition = PartyCompositions.FullPartyT1;
                                break;

                            case 2:
                                this.PartyComposition = PartyCompositions.FullPartyT2;
                                break;
                        }
                    }
                }

                this.PartyComposition = composition;
            }

            this.previousPartyMemberIDs = result;
        }

        public List<Combatant> ToCombatantList(
            IEnumerable<ActorItem> actors)
        {
            var combatantList = new List<Combatant>(actors.Count());

            lock (CombatantLock)
            {
                foreach (var actor in actors)
                {
                    var combatant = this.TryGetOrNewCombatant(actor);
                    combatantList.Add(combatant);
                    Thread.Yield();
                }
            }

            return combatantList;
        }

        public Combatant ToCombatant(
            ActorItem actor)
        {
            if (actor == null)
            {
                return null;
            }

            lock (CombatantLock)
            {
                return this.TryGetOrNewCombatant(actor);
            }
        }

        private Combatant TryGetOrNewCombatant(
            ActorItem actor)
        {
            if (actor == null)
            {
                return null;
            }

            var combatant = default(Combatant);
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
                dictionary.Add(key, combatant);
            }

            return combatant;
        }

        private Combatant CreateCombatant(
            ActorItem actor,
            Combatant current = null)
        {
            var player = ReaderEx.CurrentPlayerCombatant;

            var c = current ?? new Combatant();

            c.ActorItem = actor;
            c.ID = actor.ID;
            c.ObjectType = actor.Type;
            c.Name = actor.Name;
            c.Level = actor.Level;
            c.Job = (byte)actor.Job;
            c.TargetID = (uint)actor.TargetID;
            c.OwnerID = actor.OwnerID;

            c.EffectiveDistance = actor.Distance;
            c.IsAvailableEffectiveDictance = true;

            c.Heading = actor.Heading;
            c.PosX = (float)actor.X;
            c.PosY = (float)actor.Y;
            c.PosZ = (float)actor.Z;

            c.CurrentHP = actor.HPCurrent;
            c.MaxHP = actor.HPMax;
            c.CurrentMP = actor.MPCurrent;
            c.MaxMP = actor.MPMax;
            c.CurrentTP = (short)actor.TPCurrent;
            c.MaxTP = (short)actor.TPMax;

            c.CurrentCP = actor.CPCurrent;
            c.MaxCP = actor.CPMax;
            c.CurrentGP = actor.GPCurrent;
            c.MaxGP = actor.GPMax;

            c.IsCasting = actor.IsCasting;
            c.CastTargetID = actor.CastingTargetID;
            c.CastBuffID = actor.CastingID;
            c.CastDurationCurrent = actor.CastingProgress;
            c.CastDurationMax = actor.CastingTime;

            c.Player = player;
            this.SetTargetOfTarget(c);

            FFXIVPlugin.Instance.SetSkillName(c);
            c.SetName(actor.Name);

            var worldInfo = GetWorldInfoCallback?.Invoke(c.ID);
            c.WorldID = worldInfo?.WorldID ?? 0;
            c.WorldName = worldInfo?.WorldName ?? string.Empty;

            c.Timestamp = DateTime.Now;

            return c;
        }

        public void SetTargetOfTarget(
            Combatant player)
        {
            if (!player.IsPlayer ||
                player.TargetID == 0)
            {
                return;
            }

            if (this.CombatantsDictionary.ContainsKey(player.TargetID))
            {
                var target = this.CombatantsDictionary[player.TargetID];
                player.TargetOfTargetID = target.TargetID;
            }
        }

        private void GarbageCombatantsDictionary()
        {
            var threshold = CommonHelper.Random.Next(10 * 60, 15 * 60);
            var now = DateTime.Now;

            lock (CombatantLock)
            {
                foreach (var dictionary in new[]
                {
                    this.CombatantsDictionary,
                    this.NPCCombatantsDictionary
                })
                {
                    dictionary
                        .Where(x => (now - x.Value.Timestamp).TotalSeconds > threshold)
                        .ToArray()
                        .Walk(x =>
                        {
                            this.CombatantsDictionary.Remove(x.Key);
                            Thread.Yield();
                        });
                }
            }
        }
    }

    public static class ActorItemExtensions
    {
        public static List<Combatant> ToCombatantList(
            this IEnumerable<ActorItem> actors)
            => SharlayanHelper.Instance.ToCombatantList(actors);

        public static uint GetKey(
            this ActorItem actor)
            => actor.IsNPC() ? actor.NPCID2 : actor.ID;

        public static bool IsNPC(
            this ActorItem actor)
        {
            switch (actor.Type)
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

            if (Enum.IsDefined(typeof(JobIDs), id))
            {
                jobEnum = (JobIDs)Enum.ToObject(typeof(JobIDs), id);
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

        public static Combatant CurrentPlayerCombatant { get; private set; }

        public static ProcessModel ProcessModel { get; set; }

        public static Func<uint, ActorItem> GetActorCallback { get; set; }

        public static Func<uint, ActorItem> GetNPCActorCallback { get; set; }

        private static readonly Lazy<Func<StructuresContainer>> LazyGetStructuresDelegate = new Lazy<Func<StructuresContainer>>(() =>
        {
            var property = MemoryHandler.Instance.GetType().GetProperty(
                "Structures",
                BindingFlags.NonPublic | BindingFlags.Instance);

            return (Func<StructuresContainer>)Delegate.CreateDelegate(
                typeof(Func<StructuresContainer>),
                MemoryHandler.Instance,
                property.GetMethod);
        });

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

        public static List<ActorItem> GetActorSimple()
        {
            var result = new List<ActorItem>(256);

            if (!Reader.CanGetActors() || !MemoryHandler.Instance.IsAttached)
            {
                return result;
            }

            var isWin64 = ProcessModel?.IsWin64 ?? true;

            var targetAddress = IntPtr.Zero;
            var endianSize = isWin64 ? 8 : 4;

            var structures = LazyGetStructuresDelegate.Value.Invoke();
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
                    new IntPtr(TryToInt64(characterAddressMap, i * endianSize)) :
                    new IntPtr(TryToInt32(characterAddressMap, i * endianSize));

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

                var ID = TryToUInt32(source, structures.ActorItem.ID);
                var NPCID2 = TryToUInt32(source, structures.ActorItem.NPCID2);
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

                var isFirstEntry = kvp.Value.ToInt64() == firstAddress.ToInt64();
                var entry = InvokeActorItemResolver(source, isFirstEntry, existing);

                if (isFirstEntry)
                {
                    CurrentPlayer = entry;
                    CurrentPlayerCombatant = SharlayanHelper.Instance.ToCombatant(entry);

                    if (targetAddress.ToInt64() > 0)
                    {
                        var targetInfoSource = MemoryHandler.Instance.GetByteArray(targetAddress, 128);
                        entry.TargetID = (int)TryToInt32(targetInfoSource, structures.ActorItem.ID);
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
                    var ID = TryToUInt32(source, structures.PartyMember.ID);

                    result.Add(ID);
                    Thread.Yield();
                }
            }

            return result.ToArray();
        }

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

                    var ID = TryToUInt32(source, structures.ActorItem.ID);
                    var NPCID2 = TryToUInt32(source, structures.ActorItem.NPCID2);
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

        public static int TryToInt32(byte[] value, int index)
        {
            try
            {
                return BitConverter.ToInt32(value, index);
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
                return BitConverter.ToUInt32(value, index);
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
                return BitConverter.ToInt64(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
