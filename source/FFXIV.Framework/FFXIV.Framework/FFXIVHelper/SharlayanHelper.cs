using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using Sharlayan;
using Sharlayan.Core;
using Sharlayan.Models;

namespace FFXIV.Framework.FFXIVHelper
{
    public class SharlayanHelper
    {
        private static readonly Lazy<SharlayanHelper> LazyInstance = new Lazy<SharlayanHelper>();

        public static SharlayanHelper Instance => LazyInstance.Value;

        private ThreadWorker ffxivSubscriber;
        private static readonly double ProcessSubscribeInterval = 5000;

        private ThreadWorker memorySubscriber;

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
                        FFXIVPlugin.Instance.MemorySubscriberInterval,
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
            this.Actors.Clear();
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

                    MemoryHandler.Instance.SetProcess(
                        new ProcessModel
                        {
                            Process = ffxiv,
                            IsWin64 = true
                        },
                        ffxivLanguage);

                    this.ClearData();
                }
            }
        }

        public ConcurrentDictionary<uint, ActorItem> Actors { get; } = new ConcurrentDictionary<uint, ActorItem>();

        private void ScanMemory()
        {
            if (!MemoryHandler.Instance.IsAttached)
            {
                Thread.Sleep((int)ProcessSubscribeInterval);
                return;
            }

            this.GetActors();
        }

        private void GetActors()
        {
            var result = Reader.GetActors();
            if (result == null)
            {
                this.Actors.Clear();
                return;
            }

            var news = result.NewMonsters
                .Concat(result.NewNPCs)
                .Concat(result.NewPCs);

            foreach (var entry in news)
            {
                this.Actors.TryAdd(entry.Key, entry.Value);
            }

            var removes = result.RemovedMonsters
                .Concat(result.RemovedNPCs)
                .Concat(result.RemovedPCs);

            foreach (var entry in removes)
            {
                this.Actors.TryRemove(entry.Key, out ActorItem o);
            }

            var currents = result.CurrentMonsters
                .Concat(result.CurrentNPCs)
                .Concat(result.CurrentPCs);

            foreach (var entry in currents)
            {
                if (this.Actors.ContainsKey(entry.Key))
                {
                    this.Actors[entry.Key] = entry.Value;
                }
            }
        }
    }

    public static class ActorItemExtensions
    {
        public static Combatant ToCombatant(
            this ActorItem actor)
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

            return c;
        }
    }
}
