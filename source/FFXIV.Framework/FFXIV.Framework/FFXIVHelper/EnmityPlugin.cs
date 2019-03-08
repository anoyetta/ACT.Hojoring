#if false

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FFXIV.Framework.Common;
using Sharlayan;

namespace FFXIV.Framework.FFXIVHelper
{
    public partial class EnmityPlugin :
        IDisposable
    {
#region Singleton

        private static EnmityPlugin instance;

        public static EnmityPlugin Instance => instance ?? (instance = new EnmityPlugin());

        private EnmityPlugin()
        {
        }

        public static void Free()
        {
            instance?.Dispose();
            instance = null;
        }

#endregion Singleton

#region Logger

        private static NLog.Logger AppLogger => AppLog.DefaultLogger;

#endregion Logger

        private volatile bool isInitialized = false;
        private ThreadWorker scanWorker;

        public void Initialize()
        {
            if (this.isInitialized)
            {
                return;
            }

            this.isInitialized = true;

            lock (this)
            {
                this.scanWorker = ThreadWorker.Run(
                    this.ScanEnmity,
                    100d,
                    "ScanEnmityWorker",
                    ThreadPriority.BelowNormal);
            }
        }

        public void Dispose()
        {
            this.isInitialized = false;

            lock (this)
            {
                this.scanWorker?.Abort();
                this.scanWorker = null;
            }
        }

        private List<EnmityEntry> enmityList;

        private void ScanEnmity()
        {
            var actors = SharlayanHelper.Instance.Actors
                .Where(x => !x.IsNPC())
                .ToDictionary(x => x.ID);

            var player = SharlayanHelper.Instance.CurrentPlayer;

            lock (this)
            {
                var result = Reader.GetTargetInfo();
                if (!result.TargetsFound ||
                    !result.TargetInfo.EnmityItems.Any())
                {
                    this.enmityList = null;
                    return;
                }

                var max = result.TargetInfo.EnmityItems.Max(x => x.Enmity);

                this.enmityList.Clear();
                foreach (var source in result.TargetInfo.EnmityItems)
                {
                    Thread.Yield();

                    var enmity = new EnmityEntry();

                    enmity.ID = source.ID;
                    enmity.Name = source.Name;
                    enmity.Enmity = source.Enmity;

                    var actor = actors.ContainsKey(enmity.ID) ?
                        actors[enmity.ID] :
                        null;

                    enmity.IsMe = enmity.ID == player?.ID;
                    enmity.OwnerID = actor?.OwnerID ?? 0;
                    enmity.Job = (byte)(actor?.Job ?? 0);
                    enmity.HateRate = (int)(((double)enmity.Enmity / (double)max) * 100d);

                    this.enmityList.Add(enmity);
                }
            }
        }

        public List<EnmityEntry> EnmityEntryList
        {
            get
            {
                lock (this)
                {
                    return this.enmityList?.Clone();
                }
            }
        }
    }
}
#endif
