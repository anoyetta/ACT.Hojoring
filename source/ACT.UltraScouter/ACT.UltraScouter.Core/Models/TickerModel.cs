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

            this.mpSubscriber = new ThreadWorker(
                this.DetectMP,
                Settings.Instance.MPTicker.DetectMPInterval,
                "3s ticker MP subscriber",
                ThreadPriority.BelowNormal);
        }

        #endregion Lazy Singleton

        #region Logger

        private NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        private static readonly double TickerGlobalInterval = 3.0d;

        private static readonly double DetectInterval = 1000;
        private static readonly double DetectIdelInterval = 5000;
        private ThreadWorker detectTargetSubscriber;
        private ThreadWorker mpSubscriber;

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

            if (!this.mpSubscriber.IsRunning)
            {
                this.mpSubscriber.Run();
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

            if (Settings.Instance.MPTicker.IsSyncMP &&
                Settings.Instance.MPTicker.IsUnlockMPSync)
            {
                var playerStatus = XIVPluginHelper.Instance.GetPlayerStatus();
                if (playerStatus == null)
                {
                    return;
                }

                // 標準回復量200 + (PIE - PIE初期値340) / PIE22ごと
                HealerInCombatMPRecoverValue = 200 + ((playerStatus.Pie - 340) / 22);

                if (this.mpSubscriber != null)
                {
                    this.mpSubscriber.Interval = Settings.Instance.MPTicker.DetectMPInterval;
                }
            }
        }

        private volatile uint previousMP = 0;

        private void DetectMP()
        {
            var config = Settings.Instance.MPTicker;

            if (!this.isRunnning ||
                !config.IsSyncMP ||
                !config.IsUnlockMPSync)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(DetectIdelInterval));
                return;
            }

            if ((DateTime.Now - this.lastSyncTimestamp).TotalSeconds <= config.ResyncInterval)
            {
                return;
            }

            var player = CombatantsManager.Instance.Player;

            if (player.CurrentHP <= 0)
            {
                return;
            }

            var mpDiff = player.CurrentMP - this.previousMP;
            if (mpDiff <= 0)
            {
                return;
            }

            if (mpDiff == HealerInCombatMPRecoverValue ||
                StandardMPRecoveryValues.Contains(mpDiff))
            {
                this.lastSyncTimestamp = DateTime.Now;
                this.RestartTickerCallback?.Invoke();
                this.AppLogger.Trace($"3s ticker synced to MP. diff={mpDiff}");
            }

            this.previousMP = player.CurrentMP;
        }

        private volatile string syncKeywordToHoT = string.Empty;
        private volatile string syncKeywordToDoT = string.Empty;

        private volatile bool semaphore = false;

        private static readonly uint[] StandardMPRecoveryValues = new[]
        {
            // 戦闘時のMP自然回復量
            (uint)200,      // 2%
            (uint)3200,     // 32%  UI1
            (uint)4700,     // 47%  UI2
            (uint)6200,     // 62%  UI3

            // 非戦闘時のMP自然回復量
            (uint)600,      // 6%
            (uint)3600,     // 36%  UI1
            (uint)5100,     // 51%  UI2
            (uint)6600,     // 66%  UI3
        };

        private static volatile uint HealerInCombatMPRecoverValue;

        private async void OnLogLineRead(bool isImport, LogLineEventArgs logInfo)
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

                await Task.Run(() =>
                {
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
                });
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
