using System;
using System.Collections.Generic;
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
        private static readonly double MemorySubscribeInterval = 1000;

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
            lock (this.npcs)
            {
                this.npcs.Clear();
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

        private readonly Dictionary<uint, ActorItem> npcs = new Dictionary<uint, ActorItem>(512);

        public IEnumerable<ActorItem> NPCs
        {
            get
            {
                lock (this.npcs)
                {
                    return this.npcs.Values.ToArray();
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
                if (this.npcs.Any())
                {
                    lock (this.npcs)
                    {
                        this.npcs.Clear();
                    }
                }
            }
            else
            {
                lock (this.npcs)
                {
                    this.GetActors();
                }
            }
        }

        public bool IsSkipActor { get; set; } = false;

        private void GetActors()
        {
            var result = Reader.GetActors();
            if (result == null)
            {
                this.npcs.Clear();
                return;
            }

            var news = result.NewNPCs;
            foreach (var entry in news)
            {
                if (!this.npcs.ContainsKey(entry.Key))
                {
                    this.npcs.Add(entry.Key, entry.Value);
                }

                Thread.Yield();
            }

            var removes = result.RemovedNPCs;
            foreach (var entry in removes)
            {
                if (this.npcs.ContainsKey(entry.Key))
                {
                    this.npcs.Remove(entry.Key);
                }

                Thread.Yield();
            }

            var currents = result.CurrentNPCs;
            foreach (var entry in currents)
            {
                if (this.npcs.ContainsKey(entry.Key))
                {
                    this.npcs[entry.Key] = entry.Value;
                }

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
}
