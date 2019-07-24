using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ACT.UltraScouter.Config;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;
using FFXIV_ACT_Plugin.Common;
using Prism.Mvvm;

namespace ACT.UltraScouter.Models
{
    public class TickerModel : BindableBase
    {
        #region Lazy Singleton

        private static readonly Lazy<TickerModel> LazyInstance = new Lazy<TickerModel>(() => new TickerModel());

        public static TickerModel Instance => LazyInstance.Value;

        private TickerModel()
        {
            this.detectTargetSubscriber = new ThreadWorker(
                this.DetectTarget,
                DetectInterval,
                "3s ticker detect target subscriber",
                ThreadPriority.Lowest);
        }

        #endregion Lazy Singleton

        #region Logger

        private NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        private static readonly double TickerGlobalInterval = 3.0d;

        private static readonly double DetectInterval = 1000;
        private static readonly double DetectIdelInterval = 5000;
        private ThreadWorker detectTargetSubscriber;

        private volatile bool isRunnning = false;

        private bool inTargetJob;

        public bool InTargetJob
        {
            get => WPFHelper.IsDesignMode ? true : this.inTargetJob;
            private set
            {
                if (this.SetProperty(ref this.inTargetJob, value))
                {
                    if (value)
                    {
                        this.StartSync();
                    }
                    else
                    {
                        this.StopSync();
                    }
                }
            }
        }

        public void Update(
            CombatantEx player)
        {
            if (Settings.Instance.MPTicker.TestMode)
            {
                this.InTargetJob = true;
            }
            else
            {
                var targets = Settings.Instance.MPTicker.TargetJobs;
                if (targets == null ||
                    targets.Count < 1)
                {
                    this.InTargetJob = true;
                }
                else
                {
                    this.InTargetJob = targets.Any(x =>
                        x.Job == player.JobID &&
                        x.Available);
                }
            }
        }

        public void StartSync()
        {
            this.isRunnning = true;

            if (!this.detectTargetSubscriber.IsRunning)
            {
                this.detectTargetSubscriber.Run();
            }

            ActGlobals.oFormActMain.OnLogLineRead -= this.OnLogLineRead;
            ActGlobals.oFormActMain.OnLogLineRead += this.OnLogLineRead;

            this.AppLogger.Trace("3s ticker start sync.");
        }

        public void StopSync()
        {
            this.isRunnning = false;
            ActGlobals.oFormActMain.OnLogLineRead -= this.OnLogLineRead;
        }

        private void DetectTarget()
        {
            if (!this.isRunnning)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(DetectIdelInterval));
                return;
            }

            var player = CombatantsManager.Instance.Player;
            var target = XIVPluginHelper.Instance.GetTargetInfo(OverlayType.Target);

            this.syncKeywordToHoT = player != null ?
                $"18:HoT Tick on {player.Name}" :
                string.Empty;

            this.syncKeywordToDoT = target != null ?
                $"18:DoT Tick on {target.Name}" :
                string.Empty;
        }

        private volatile string syncKeywordToHoT = string.Empty;
        private volatile string syncKeywordToDoT = string.Empty;

        private volatile bool semaphore = false;

        private void OnLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            if (isImport)
            {
                return;
            }

            try
            {
                if (this.semaphore)
                {
                    return;
                }

                this.semaphore = true;

                var config = Settings.Instance.MPTicker;

                if ((DateTime.Now - this.lastSyncTimestamp).TotalSeconds <= config.ResyncInterval)
                {
                    return;
                }

                var sync = false;
                var target = string.Empty;

                if (!string.IsNullOrEmpty(this.syncKeywordToHoT) &&
                    config.IsSyncHoT)
                {
                    sync = logInfo.logLine.Contains(this.syncKeywordToHoT);
                    target = "HoT";
                }

                if (!string.IsNullOrEmpty(this.syncKeywordToDoT) &&
                    config.IsSyncDoT)
                {
                    sync = logInfo.logLine.Contains(this.syncKeywordToDoT);
                    target = "DoT";
                }

                if (sync)
                {
                    this.Sync(logInfo.logLine, target);
                }
            }
            finally
            {
                this.semaphore = false;
            }
        }

        private DateTime lastSyncTimestamp;

        public Action RestartTickerCallback { get; set; }

        public void RestartTicker()
        {
            this.lastSyncTimestamp = DateTime.MinValue;
            this.RestartTickerCallback?.Invoke();
        }

        private void Sync(
            string logline,
            string syncTarget)
        {
            if (logline.Length < 14)
            {
                return;
            }

            var text = logline.Substring(0, 14)
                .Replace("[", string.Empty)
                .Replace("]", string.Empty);
            if (!DateTime.TryParse(text, out DateTime timestamp))
            {
                return;
            }

            var nextTick = timestamp.AddSeconds(TickerGlobalInterval);
            if (nextTick <= DateTime.Now)
            {
                return;
            }

            this.lastSyncTimestamp = DateTime.Now;

            Task.Run(() =>
            {
                Thread.Sleep(nextTick - DateTime.Now);
                this.RestartTickerCallback?.Invoke();
                this.AppLogger.Trace($"3s ticker synced to {syncTarget}.");
            });
        }
    }
}
