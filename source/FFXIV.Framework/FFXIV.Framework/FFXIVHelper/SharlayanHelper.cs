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

namespace FFXIV.Framework.FFXIVHelper
{
    public class SharlayanHelper
    {
        private static readonly Lazy<SharlayanHelper> LazyInstance = new Lazy<SharlayanHelper>();

        public static SharlayanHelper Instance => LazyInstance.Value;

        private ThreadWorker ffxivSubscriber;
        private static readonly double ProcessSubscribeInterval = 5000;

        private ThreadWorker memorySubscriber;
        private static readonly double MemorySubscribeInterval = 500;

        public void Start()
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
                        MemorySubscribeInterval,
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
            lock (this.ActorDictionary)
            {
                this.ActorDictionary.Clear();
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
        }

        private readonly Dictionary<uint, ActorItem> ActorDictionary = new Dictionary<uint, ActorItem>(512);

        public IEnumerable<ActorItem> Actors
        {
            get
            {
                lock (this.ActorDictionary)
                {
                    return this.ActorDictionary.Values.ToArray();
                }
            }
        }

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
                if (this.ActorDictionary.Any())
                {
                    lock (this.ActorDictionary)
                    {
                        this.ActorDictionary.Clear();
                    }
                }
            }
            else
            {
                lock (this.ActorDictionary)
                {
                    /*
                    this.GetActors();
                    */
                    this.GetActorsSimple();
                }
            }

            if (this.IsSkips.All(x => x))
            {
                Thread.Sleep((int)ProcessSubscribeInterval);
            }
        }

        public bool IsSkipActor { get; set; } = false;

        private bool[] IsSkips => new[] { this.IsSkipActor };

        private void GetActors()
        {
            var result = Reader.GetActors();
            if (result == null)
            {
                this.ActorDictionary.Clear();
                return;
            }

            var news = result.NewNPCs;
            foreach (var entry in news)
            {
                if (!this.ActorDictionary.ContainsKey(entry.Key))
                {
                    this.ActorDictionary.Add(entry.Key, entry.Value);
                }

                Thread.Yield();
            }

            var removes = result.RemovedNPCs;
            foreach (var entry in removes)
            {
                if (this.ActorDictionary.ContainsKey(entry.Key))
                {
                    this.ActorDictionary.Remove(entry.Key);
                }

                Thread.Yield();
            }

            var currents = result.CurrentNPCs;
            foreach (var entry in currents)
            {
                if (this.ActorDictionary.ContainsKey(entry.Key))
                {
                    this.ActorDictionary[entry.Key] = entry.Value;
                }

                Thread.Yield();
            }
        }

        private void GetActorsSimple()
        {
            var actors = ReaderEx.GetActorSimple();

            if (!actors.Any())
            {
                this.ActorDictionary.Clear();
                return;
            }

            foreach (var entry in actors)
            {
                if (ActorDictionary.ContainsKey(entry.Key))
                {
                    ActorDictionary[entry.Key] = entry.Value;
                }
                else
                {
                    ActorDictionary.Add(entry.Key, entry.Value);
                }

                Thread.Yield();
            }

            var removes = ActorDictionary.Values.Where(x => !actors.ContainsKey(x.GetKey())).ToArray();
            foreach (var entry in removes)
            {
                ActorDictionary.Remove(entry.GetKey());
                Thread.Yield();
            }
        }
    }

    public static class ActorItemExtensions
    {
        public static Combatant ToCombatant(
            this ActorItem actor,
            Combatant player = null)
        {
            var c = new Combatant()
            {
                ActorItem = actor
            };

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

            c.IsCasting = actor.IsCasting;
            c.CastTargetID = actor.CastingTargetID;
            c.CastBuffID = actor.CastingID;
            c.CastDurationCurrent = actor.CastingProgress;
            c.CastDurationMax = actor.CastingTime;

            c.Player = player;

            /*
            FFXIVPlugin.Instance.SetSkillName(c);
            */
            c.SetName(actor.Name);

            return c;
        }
    }

    public static class ReaderEx
    {
        public static ProcessModel ProcessModel { get; set; }

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

        public static Dictionary<uint, ActorItem> GetActorSimple()
        {
            var result = new Dictionary<uint, ActorItem>(256);

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
                var isFirstEntry = kvp.Value.ToInt64() == firstAddress.ToInt64();

                var entry = InvokeActorItemResolver(source, isFirstEntry);

                if (isFirstEntry)
                {
                    if (targetAddress.ToInt64() > 0)
                    {
                        var targetInfoSource = MemoryHandler.Instance.GetByteArray(targetAddress, 128);
                        entry.TargetID = (int)TryToInt32(targetInfoSource, structures.ActorItem.ID);
                    }
                }

                result[entry.GetKey()] = entry;
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

        public static uint GetKey(
            this ActorItem actor)
        {
            switch (actor.Type)
            {
                case Actor.Type.NPC:
                case Actor.Type.Aetheryte:
                case Actor.Type.EventObject:
                    return actor.NPCID2;

                default:
                    return actor.ID;
            }
        }
    }
}
