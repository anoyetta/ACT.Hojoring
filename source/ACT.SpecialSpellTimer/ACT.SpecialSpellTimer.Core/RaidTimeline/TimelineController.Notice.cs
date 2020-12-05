using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.RaidTimeline.Views;
using ACT.SpecialSpellTimer.RazorModel;
using ACT.SpecialSpellTimer.Sound;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    public partial class TimelineController :
        BindableBase
    {
        private static readonly object NoticeLocker = new object();
        private static readonly TimeSpan NotifySleepInterval = TimeSpan.FromSeconds(5);
        private static readonly ConcurrentQueue<TimelineBase> NotifyQueue = new ConcurrentQueue<TimelineBase>();

        private static readonly ThreadWorker NotifyWorker = new ThreadWorker(
            null,
            NotifySleepInterval.TotalMilliseconds,
            "TimelineNotifyWorker",
            TimelineSettings.Instance.NotifyThreadPriority);

        private static volatile bool isNotifyRunning = false;

        private void StartNotifyWorker()
        {
            lock (NoticeLocker)
            {
                if (!NotifyWorker.IsRunning)
                {
                    NotifyWorker.Run();
                }

                NotifyWorker.DoWorkAction = this.DoNotify;
                NotifyWorker.Interval = TimelineSettings.Instance.NotifyInterval;

                isNotifyRunning = true;
            }
        }

        public void StopNotifyWorker()
        {
            lock (NoticeLocker)
            {
                isNotifyRunning = false;
                NotifyWorker.DoWorkAction = null;
                NotifyWorker.Interval = NotifySleepInterval.TotalMilliseconds;
                this.ClearNotifyQueue();
            }
        }

        private void ClearNotifyQueue()
        {
            while (NotifyQueue.TryDequeue(out TimelineBase q))
            {
                if (q is TimelineImageNoticeModel i)
                {
                    i.CloseNotice();
                }
            }
        }

        private void DoNotify()
        {
            lock (NoticeLocker)
            {
                if (!isNotifyRunning)
                {
                    NotifyWorker.Interval = NotifySleepInterval.TotalMilliseconds;
                    return;
                }

                NotifyWorker.Interval = TimelineSettings.Instance.NotifyInterval;

                if (NotifyQueue.IsEmpty)
                {
                    return;
                }

                var exists = false;
                while (NotifyQueue.TryDequeue(out TimelineBase element))
                {
                    switch (element)
                    {
                        case TimelineActivityModel act:
                            this.NotifyActivity(act);
                            break;

                        case TimelineTriggerModel tri:
                            this.NotifyTrigger(tri);
                            break;
                    }

                    exists = true;
                }

                if (exists)
                {
                    NotifyWorker.Interval = 0;
                }
            }
        }

        private void NotifyActivity(
            TimelineActivityModel act)
        {
            if (string.IsNullOrEmpty(act.Name) &&
                string.IsNullOrEmpty(act.Text) &&
                string.IsNullOrEmpty(act.Notice))
            {
                act.IsNotified = true;
                return;
            }

            var now = DateTime.Now;
            var offset = this.CurrentTime - act.Time;
            var log =
                $"{TimelineConstants.LogSymbol} Notice from TL. " +
                $"name={act.Name}, text={act.TextReplaced}, notice={act.NoticeReplaced}, offset={offset.TotalSeconds:N1}";

            var notice = act.NoticeReplaced ?? string.Empty;
            notice = TimelineExpressionsModel.ReplaceText(notice);

            if (string.Equals(notice, "auto", StringComparison.OrdinalIgnoreCase))
            {
                notice = !string.IsNullOrEmpty(act.TextReplaced) ?
                    act.TextReplaced :
                    act.Name;

                if (offset.TotalSeconds <= -1.0)
                {
                    var offsetText = (offset.TotalSeconds * -1).ToString("N0");
                    notice += $" まで、あと{offsetText}秒";
                }

                if (!string.IsNullOrEmpty(notice))
                {
                    notice += "。";
                }
            }

            var isSync =
                (TimelineRazorModel.Instance?.SyncTTS ?? false) ||
                act.NoticeSync.Value;

            RaiseLog(log);
            NotifySoundAsync(notice, act.NoticeDevice.GetValueOrDefault(), isSync, act.NoticeVolume);

            var vnotices = act.VisualNoticeStatements
                .Where(x => x.Enabled.GetValueOrDefault())
                .Select(x => x.Clone());

            var inotices = act.ImageNoticeStatements
                .Where(x => x.Enabled.GetValueOrDefault());

            if (!vnotices.Any() &&
                !inotices.Any())
            {
                return;
            }

            var placeholders = TimelineManager.Instance.GetPlaceholders();

            foreach (var v in vnotices)
            {
                // ソート用にログ番号を格納する
                // 優先順位は最後尾とする
                v.LogSeq = long.MaxValue;
                v.Timestamp = DateTime.Now;

                switch (v.Text)
                {
                    case TimelineVisualNoticeModel.ParentTextPlaceholder:
                        v.TextToDisplay = act.TextReplaced;
                        break;

                    case TimelineVisualNoticeModel.ParentNoticePlaceholder:
                        v.TextToDisplay = act.NoticeReplaced;
                        break;

                    default:
                        v.TextToDisplay = act.SyncMatch != null && act.SyncMatch.Success ?
                            act.SyncMatch.Result(v.Text) :
                            v.Text;
                        break;
                }

                if (string.IsNullOrEmpty(v.TextToDisplay))
                {
                    continue;
                }

                v.TextToDisplay = TimelineExpressionsModel.ReplaceText(v.TextToDisplay);

                // PC名をルールに従って置換する
                v.TextToDisplay = XIVPluginHelper.Instance.ReplacePartyMemberName(
                    v.TextToDisplay,
                    Settings.Default.PCNameInitialOnDisplayStyle);

                WPFHelper.BeginInvoke(async () =>
                {
                    if (v.Delay > 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(v.Delay ?? 0));
                    }

                    TimelineNoticeOverlay.NoticeView?.AddNotice(v);
                    v.SetSyncToHide(placeholders);
                    v.AddSyncToHide();
                });
            }

            foreach (var i in inotices)
            {
                WPFHelper.BeginInvoke(async () =>
                {
                    if (i.Delay > 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(i.Delay ?? 0));
                    }

                    i.StartNotice();
                    i.SetSyncToHide(placeholders);
                    i.AddSyncToHide();
                });
            }
        }

        private void NotifyTrigger(
            TimelineTriggerModel tri)
        {
            if (string.IsNullOrEmpty(tri.Name) &&
                string.IsNullOrEmpty(tri.Text) &&
                string.IsNullOrEmpty(tri.Notice))
            {
                return;
            }

            var now = DateTime.Now;
            var log =
                $"{TimelineConstants.LogSymbol} Notice from TL. " +
                $"name={tri.Name}, text={tri.TextReplaced}, notice={tri.NoticeReplaced}";

            var notice = tri.NoticeReplaced ?? string.Empty;
            notice = TimelineExpressionsModel.ReplaceText(notice);

            if (string.Equals(notice, "auto", StringComparison.OrdinalIgnoreCase))
            {
                notice = !string.IsNullOrEmpty(tri.Text) ?
                    tri.Text :
                    tri.Name;

                if (!string.IsNullOrEmpty(notice))
                {
                    notice += "。";
                }
            }

            var isSync =
                (TimelineRazorModel.Instance?.SyncTTS ?? false) ||
                tri.NoticeSync.Value;

            RaiseLog(log);
            NotifySoundAsync(notice, tri.NoticeDevice.GetValueOrDefault(), isSync, tri.NoticeVolume, tri.NoticeOffset);

            var vnotices = tri.VisualNoticeStatements
                .Where(x => x.Enabled.GetValueOrDefault());

            var inotices = tri.ImageNoticeStatements
                .Where(x => x.Enabled.GetValueOrDefault());

            if (!vnotices.Any() &&
                !inotices.Any())
            {
                return;
            }

            var placeholders = TimelineManager.Instance.GetPlaceholders();

            foreach (var v in vnotices)
            {
                // ヒットしたログのシーケンスを格納する
                // ソート用
                v.LogSeq = tri.LogSeq;

                switch (v.Text)
                {
                    case TimelineVisualNoticeModel.ParentTextPlaceholder:
                        v.TextToDisplay = tri.TextReplaced;
                        break;

                    case TimelineVisualNoticeModel.ParentNoticePlaceholder:
                        v.TextToDisplay = tri.NoticeReplaced;
                        break;

                    default:
                        v.TextToDisplay = tri.SyncMatch != null && tri.SyncMatch.Success ?
                            tri.SyncMatch.Result(v.Text) :
                            v.Text;
                        break;
                }

                if (string.IsNullOrEmpty(v.TextToDisplay))
                {
                    continue;
                }

                v.TextToDisplay = TimelineExpressionsModel.ReplaceText(v.TextToDisplay);

                // PC名をルールに従って置換する
                v.TextToDisplay = XIVPluginHelper.Instance.ReplacePartyMemberName(
                    v.TextToDisplay,
                    Settings.Default.PCNameInitialOnDisplayStyle);

                WPFHelper.BeginInvoke(async () =>
                {
                    if (v.Delay > 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(v.Delay ?? 0));
                    }

                    TimelineNoticeOverlay.NoticeView?.AddNotice(v);
                    v.SetSyncToHide(placeholders);
                    v.AddSyncToHide();
                });
            }

            foreach (var i in inotices)
            {
                WPFHelper.BeginInvoke(async () =>
                {
                    if (i.Delay > 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(i.Delay ?? 0));
                    }

                    i.StartNotice();
                    i.SetSyncToHide(placeholders);
                    i.AddSyncToHide();
                });
            }
        }

        private static string lastRaisedLog = string.Empty;
        private static DateTime lastRaisedLogTimestamp = DateTime.MinValue;

        public static void RaiseLog(
            string log)
        {
            if (string.IsNullOrEmpty(log))
            {
                return;
            }

            lock (NoticeLocker)
            {
                if (lastRaisedLog == log)
                {
                    if ((DateTime.Now - lastRaisedLogTimestamp).TotalSeconds <= 0.1)
                    {
                        return;
                    }
                }

                lastRaisedLog = log;
                lastRaisedLogTimestamp = DateTime.Now;
            }

            log = log.Replace(Environment.NewLine, "\\n");

            LogParser.RaiseLog(DateTime.Now, log);
        }

        private static string lastNotice = string.Empty;
        private static DateTime lastNoticeTimestamp = DateTime.MinValue;

        private static Task NotifySoundAsync(
            string notice,
            NoticeDevices device,
            bool isSync = false,
            float? volume = null,
            double? delay = null)
            => Task.Run(async () =>
            {
                if (delay.HasValue &&
                    delay.Value > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(delay.Value));
                }

                NotifySound(notice, device, isSync, volume);
            });

        private static void NotifySound(
            string notice,
            NoticeDevices device,
            bool isSync = false,
            float? volume = null)
        {
            if (string.IsNullOrEmpty(notice))
            {
                return;
            }

            if (TimelineSettings.Instance.IsMute)
            {
                return;
            }

            if (volume.HasValue &&
                volume <= 0)
            {
                return;
            }

            lock (NoticeLocker)
            {
                if (lastNotice == notice)
                {
                    if ((DateTime.Now - lastNoticeTimestamp).TotalSeconds <= 0.1)
                    {
                        return;
                    }
                }

                lastNotice = notice;
                lastNoticeTimestamp = DateTime.Now;
            }

            var isWave =
                notice.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                notice.EndsWith(".wave", StringComparison.OrdinalIgnoreCase) ||
                notice.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase);

            if (isWave)
            {
                if (!File.Exists(notice))
                {
                    notice = Path.Combine(
                        SoundController.Instance.WaveDirectory,
                        notice);
                }
            }
            else
            {
                notice = TTSDictionary.Instance.ReplaceWordsTTS(notice);
            }

            switch (device)
            {
                case NoticeDevices.Both:
                    if (PlayBridge.Instance.IsAvailable)
                    {
                        PlayBridge.Instance.Play(notice, isSync, volume);
                        break;
                    }

                    if (isWave)
                    {
                        ActGlobals.oFormActMain.PlaySound(notice);
                    }
                    else
                    {
                        ActGlobals.oFormActMain.TTS(notice);
                    }

                    break;

                case NoticeDevices.Main:
                    PlayBridge.Instance.PlayMain(notice, isSync, volume);
                    break;

                case NoticeDevices.Sub:
                    PlayBridge.Instance.PlaySub(notice, isSync, volume);
                    break;
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now:HH:mm:ss.fff} notify={notice}");
#endif
        }
    }
}
