using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.RaidTimeline.Views;
using ACT.SpecialSpellTimer.Sound;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.FFXIVHelper;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    public enum TimelineStatus
    {
        Unloaded = 0,
        Loading,
        Loaded,
        Runnning
    }

    public static class TimelineStatusEx
    {
        public static string ToText(
            this TimelineStatus s)
            => new[]
            {
                string.Empty,
                "Loading...",
                "Standby",
                "Running",
            }[(int)s];

        public static string ToIndicator(
            this TimelineStatus s)
            => new[]
            {
                string.Empty,
                "Ｒ",
                "⬛",
                "▶",
            }[(int)s];
    }

    public partial class TimelineController :
        BindableBase
    {
        #region Logger

        private NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        private static readonly object Locker = new object();

        /// <summary>
        /// タイムラインから発生するログのSymbol
        /// </summary>
        private const string TLSymbol = "[TL]";

        public static void Init()
        {
            ActGlobals.oFormActMain.OnLogLineRead -= OnLogLineRead;
            ActGlobals.oFormActMain.OnLogLineRead += OnLogLineRead;
        }

        public static void Free()
        {
            ActGlobals.oFormActMain.OnLogLineRead -= OnLogLineRead;
        }

        private static void OnLogLineRead(
            bool isImport,
            LogLineEventArgs logInfo)
        {
            try
            {
                if (isImport)
                {
                    return;
                }

                CurrentController?.ArrivalLogLine(
                    CurrentController,
                    logInfo);
            }
            catch (Exception ex)
            {
                AppLog.DefaultLogger?.Error(
                    ex,
                    $"[TL] Error OnLoglineRead.");
            }
        }

        /// <summary>
        /// 現在のController
        /// </summary>
        public static TimelineController CurrentController
        {
            get;
            private set;
        }

        /// <summary>
        /// グローバルトリガのコントローラを取得する
        /// </summary>
        /// <returns>
        /// グローバルトリガのコントローラ</returns>
        public static TimelineController GetGlobalTriggerController()
            => TimelineManager.Instance.TimelineModels
                .FirstOrDefault(x => x.IsGlobalZone)?
                .Controller;

        public TimelineController(
            TimelineModel model)
        {
            this.Model = model;
        }

        public readonly ObservableCollection<TimelineActivityModel> ActivityLine
            = new ObservableCollection<TimelineActivityModel>();

        public TimelineModel Model
        {
            get;
            private set;
        } = null;

        /// <summary>
        /// アクティブなアクティビティライン（タイムライン）
        /// </summary>
        public IReadOnlyList<TimelineActivityModel> ActiveActivityLine
        {
            get
            {
                lock (this)
                {
                    return this.Model.ActivityView?.Cast<TimelineActivityModel>().ToList();
                }
            }
        }

        /// <summary>
        /// アクティブな視覚通知のリスト
        /// </summary>
        public IReadOnlyList<TimelineVisualNoticeModel> ActiveVisualNoticeList
        {
            get
            {
                if (TimelineNoticeOverlay.NoticeView == null)
                {
                    return null;
                }

                lock (TimelineNoticeOverlay.NoticeView)
                {
                    return TimelineNoticeOverlay.NoticeView?.NoticeList.Cast<TimelineVisualNoticeModel>().ToList();
                }
            }
        }

        public bool IsAvailable
        {
            get
            {
                if (Settings.Default.FFXIVLocale != this.Model.Locale)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(this.Model.Zone))
                {
                    return false;
                }

                if (string.Equals(
                        ActGlobals.oFormActMain.CurrentZone.Trim(),
                        this.Model.Zone.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var zones = this.Model.Zone
                    .Replace(Environment.NewLine, string.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                return zones.Any(x =>
                    string.Equals(
                        ActGlobals.oFormActMain.CurrentZone.Trim(),
                        x.Trim(),
                        StringComparison.OrdinalIgnoreCase));
            }
        }

        private TimelineStatus status = TimelineStatus.Unloaded;

        public TimelineStatus Status
        {
            get => this.status;
            set
            {
                if (this.SetProperty(ref status, value))
                {
                    this.RaisePropertyChanged(nameof(this.StatusText));
                    this.RaisePropertyChanged(nameof(this.StatusIndicator));
                }
            }
        }

        public string StatusText => this.status.ToText();

        public string StatusIndicator => this.status.ToIndicator();

        private TimeSpan currentTime = TimeSpan.Zero;

        /// <summary>
        /// 現在の経過時間
        /// </summary>
        public TimeSpan CurrentTime
        {
            get => this.currentTime;
            private set
            {
                if (this.SetProperty(ref this.currentTime, value))
                {
                    this.CurrentTimeText = this.currentTime.ToTLString();
                }
            }
        }

        private string currentTimeText = "00:00";

        public string CurrentTimeText
        {
            get => this.currentTimeText;
            set => this.SetProperty(ref this.currentTimeText, value);
        }

        /// <summary>
        /// 前回の判定時刻
        /// </summary>
        private DateTime PreviouseDetectTime
        {
            get;
            set;
        }

        public void Load()
        {
            lock (Locker)
            {
                this.Status = TimelineStatus.Loading;

                CurrentController = this;

                this.LoadActivityLine();

                if (!this.Model.IsGlobalZone)
                {
                    TimelineOverlay.ShowTimeline(this.Model);
                }

                TimelineNoticeOverlay.ShowNotice();

                this.LogWorker = new Thread(this.DetectLogLoop)
                {
                    IsBackground = true
                };

                this.isLogWorkerRunning = true;
                this.LogWorker.Start();

                this.logInfoQueue = new ConcurrentQueue<LogLineEventArgs>();

                this.StartNotifyWorker();

                this.Status = TimelineStatus.Loaded;
                this.AppLogger.Trace($"[TL] Timeline loaded. name={this.Model.TimelineName}");
            }
        }

        public void Unload()
        {
            lock (Locker)
            {
                this.isLogWorkerRunning = false;

                TimelineOverlay.CloseTimeline();
                TimelineNoticeOverlay.CloseNotice();

                this.CurrentTime = TimeSpan.Zero;
                this.ClearActivity();

                if (this.LogWorker != null)
                {
                    if (this.LogWorker.IsAlive)
                    {
                        this.LogWorker.Join(TimeSpan.FromSeconds(0.2));
                    }

                    if (this.LogWorker.IsAlive)
                    {
                        this.LogWorker.Abort();
                    }

                    this.LogWorker = null;
                }

                this.logInfoQueue = null;

                this.StopNotifyWorker();

                CurrentController = null;

                this.Status = TimelineStatus.Unloaded;
                this.AppLogger.Trace($"[TL] Timeline unloaded. name={this.Model.TimelineName}");
            }
        }

        private void LoadActivityLine()
        {
            this.CurrentTime = TimeSpan.Zero;
            TimelineActivityModel.CurrentTime = TimeSpan.Zero;
            this.ClearActivity();

            // 初期化する
            TimelineManager.Instance.InitElements(this.Model);

            var acts = new List<TimelineActivityModel>();
            int seq = 1;

            // entryポイントの指定がある？
            var entry = string.IsNullOrEmpty(this.Model.Entry) ?
                null :
                this.Model.Subroutines.FirstOrDefault(x =>
                    x.Enabled.GetValueOrDefault() &&
                    string.Equals(
                        x.Name,
                        this.Model.Entry,
                        StringComparison.OrdinalIgnoreCase));

            // entryサブルーチンをロードする
            var srcs = entry?.Activities ?? this.Model.Activities;
            foreach (var src in srcs
                .Where(x => x.Enabled.GetValueOrDefault()))
            {
                var act = src.Clone();
                act.Init(seq++);
                act.RefreshProgress();
                acts.Add(act);
            }

            // 一括して登録する
            this.AddRangeActivity(acts);

            // toHideリストを初期化する
            TimelineVisualNoticeModel.ClearSyncToHideList();
            TimelineImageNoticeModel.ClearSyncToHideList();

            // 表示設定を更新しておく
            this.RefreshActivityLineVisibility();
        }

        #region Activityライン捌き

        public void AddActivity(
            TimelineActivityModel activity)
        {
            lock (this)
            {
                this.ActivityLine.Add(activity);
            }
        }

        public void AddRangeActivity(
            IEnumerable<TimelineActivityModel> activities)
        {
            lock (this)
            {
                this.ActivityLine.AddRange(activities);
            }
        }

        public void RemoveActivity(
            TimelineActivityModel activity)
        {
            lock (this)
            {
                this.ActivityLine.Remove(activity);
            }
        }

        public int RemoveAllActivity(
            Func<TimelineActivityModel, bool> condition)
        {
            var count = 0;

            lock (this)
            {
                var itemsToRemove = this.ActivityLine.Where(condition).ToList();

                foreach (var itemToRemove in itemsToRemove)
                {
                    this.ActivityLine.Remove(itemToRemove);
                }

                count = itemsToRemove.Count;
            }

            return count;
        }

        public void ClearActivity()
        {
            lock (this)
            {
                this.ActivityLine.Clear();
            }
        }

        public bool CallActivity(
            TimelineActivityModel currentActivity,
            string destination = null)
        {
            var name = string.IsNullOrEmpty(destination) ?
                currentActivity?.CallTarget ?? string.Empty :
                destination;

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            lock (this)
            {
                var currentIndex = 0;
                var currnetSeq = 1;

                if (currentActivity != null)
                {
                    currentIndex = this.ActivityLine.IndexOf(currentActivity);
                    currnetSeq = currentActivity.Seq;
                }

                // 対象のサブルーチンを取得する
                var targetSub = this.Model.Subroutines.FirstOrDefault(x =>
                    x.Enabled.GetValueOrDefault() &&
                    string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

                if (targetSub == null)
                {
                    return false;
                }

                // サブルーチン中のトリガのカウンタを初期化する
                foreach (var tri in targetSub.Triggers.Where(x =>
                    x.Enabled.GetValueOrDefault()))
                {
                    tri.Init();
                }

                // サブルーチン配下のActivityを取得する
                var acts = targetSub.Activities
                    .Where(x => x.Enabled.GetValueOrDefault())
                    .Select(x => x.Clone());

                if (!acts.Any())
                {
                    return false;
                }

                try
                {
                    this.Model.StopLive();

                    // 差し込まれる次のシーケンスを取得する
                    var nextSeq = currnetSeq + 1;

                    // 差し込まれる後のActivityのシーケンスを振り直す
                    var seq = nextSeq + acts.Count();
                    foreach (var item in this.ActivityLine.Where(x =>
                        x.Seq > currnetSeq))
                    {
                        item.Seq = seq++;
                    }

                    // 差し込むActivityにシーケンスをふる
                    var toInsert = new List<TimelineActivityModel>();
                    foreach (var act in acts)
                    {
                        act.Init(nextSeq++);
                        act.Time += this.CurrentTime;
                        toInsert.Add(act);
                    }

                    this.ActivityLine.AddRange(toInsert);
                }
                finally
                {
                    this.Model.ResumeLive();
                }

                return true;
            }
        }

        public bool GoToActivity(
            TimelineActivityModel currentActivity,
            string destination = null)
        {
            var name = string.IsNullOrEmpty(destination) ?
                currentActivity?.GoToDestination ?? string.Empty :
                destination;

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            lock (this)
            {
                var currentIndex = this.ActivityLine.IndexOf(currentActivity);
                var currnetSeq = 1;

                if (currentActivity != null)
                {
                    currentIndex = this.ActivityLine.IndexOf(currentActivity);
                    currnetSeq = currentActivity.Seq;
                }

                // 対象のActivityを探す
                var targetAct = this.ActivityLine.FirstOrDefault(x =>
                    string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

                if (targetAct != null)
                {
                    try
                    {
                        this.Model.StopLive();

                        // ジャンプ後のアクティビティを最初期化する
                        foreach (var item in this.ActivityLine.Where(x =>
                            x.IsDone &&
                            x.Seq >= targetAct.Seq))
                        {
                            item.Init();
                        }

                        this.CurrentTime = targetAct.Time;
                    }
                    finally
                    {
                        this.Model.ResumeLive();
                    }

                    return true;
                }

                // サブルーチンに飛ぶ
                var targetSub = this.Model.Subroutines.FirstOrDefault(x =>
                    x.Enabled.GetValueOrDefault() &&
                    string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

                if (targetSub == null)
                {
                    return false;
                }

                // サブルーチン中のトリガのカウンタを初期化する
                foreach (var tri in targetSub.Triggers.Where(x =>
                    x.Enabled.GetValueOrDefault()))
                {
                    tri.Init();
                }

                // サブルーチン配下のActivityを取得する
                var acts = targetSub.Activities
                    .Where(x => x.Enabled.GetValueOrDefault())
                    .Select(x => x.Clone());

                if (!acts.Any())
                {
                    return false;
                }

                try
                {
                    this.Model.StopLive();

                    // 差し込まれる次のシーケンスを取得する
                    var nextSeq = currnetSeq + 1;

                    // 後のActivityを削除する
                    this.RemoveAllActivity(x => x.Seq > currnetSeq);

                    // 差し込むActivityにシーケンスをふる
                    var toInsert = new List<TimelineActivityModel>();
                    foreach (var act in acts)
                    {
                        act.Init(nextSeq++);
                        act.Time += this.CurrentTime;
                        toInsert.Add(act);
                    }

                    this.ActivityLine.AddRange(toInsert);
                }
                finally
                {
                    this.Model.ResumeLive();
                }

                return true;
            }
        }

        private void LoadSubs(
            TimelineTriggerModel parent)
        {
            lock (this)
            {
                foreach (var item in parent.LoadStatements
                    .Where(x => x.Enabled.GetValueOrDefault()))
                {
                    this.LoadSub(item);
                    Thread.Yield();
                }
            }
        }

        private void LoadSub(
            TimelineLoadModel load)
        {
            if (string.IsNullOrEmpty(load.Target))
            {
                return;
            }

            var sub = this.Model.Subroutines.FirstOrDefault(x =>
                x.Enabled.GetValueOrDefault() &&
                string.Equals(
                    x.Name,
                    load.Target,
                    StringComparison.OrdinalIgnoreCase));

            if (sub == null)
            {
                return;
            }

            // サブルーチン中のトリガのカウンタを初期化する
            foreach (var tri in sub.Triggers.Where(x =>
                x.Enabled.GetValueOrDefault()))
            {
                tri.Init();
            }

            var acts = sub.Activities
                .Where(x => x.Enabled.GetValueOrDefault())
                .Select(x => x.Clone());

            if (!acts.Any())
            {
                return;
            }

            try
            {
                this.Model.StopLive();

                // truncateする？
                if (load.IsTruncate)
                {
                    this.ActivityLine.Clear();
                }

                // 最後のアクティビティを取得する
                var last = this.ActivityLine
                    .OrderBy(x => x.Seq)
                    .LastOrDefault();

                var nextSeq = 1;
                var originTime = this.CurrentTime;
                if (last != null)
                {
                    nextSeq = last.Seq + 1;

                    if (last.Time > originTime)
                    {
                        originTime = last.Time;
                    }
                }

                // 差し込むActivityにシーケンスをふる
                var toInsert = new List<TimelineActivityModel>();
                foreach (var act in acts)
                {
                    act.Init(nextSeq++);
                    act.Time += originTime;
                    toInsert.Add(act);
                }

                this.ActivityLine.AddRange(toInsert);
            }
            finally
            {
                this.Model.ResumeLive();
            }
        }

        #endregion Activityライン捌き

        #region Log 関係のスレッド

        private ConcurrentQueue<LogLineEventArgs> logInfoQueue;
        private volatile bool isLogWorkerRunning = false;

        private Thread LogWorker
        {
            get;
            set;
        } = null;

        public void EnqueueLog(
            LogLineEventArgs logInfo)
        {
            if (!this.isLogWorkerRunning)
            {
                return;
            }

            this.logInfoQueue?.Enqueue(logInfo);
        }

        private void ArrivalLogLine(
            TimelineController controller,
            LogLineEventArgs logInfo)
        {
            try
            {
                if (!TimelineSettings.Instance.Enabled)
                {
                    return;
                }

                if (!this.isLogWorkerRunning)
                {
                    return;
                }

                // 18文字以下のログは読み捨てる
                // なぜならば、タイムスタンプ＋ログタイプのみのログだから
                if (logInfo.logLine.Length <= 18)
                {
                    return;
                }

                this.logInfoQueue?.Enqueue(logInfo);
            }
            catch (Exception ex)
            {
                this.AppLogger.Error(
                    ex,
                    $"[TL] Error ArrivalLogLine. name={this.Model.TimelineName}, zone={this.Model.Zone}, file={this.Model.SourceFile}");
            }
        }

        private void DetectLogLoop()
        {
            while (this.isLogWorkerRunning)
            {
                var isExistsLog = false;

                try
                {
                    if (!TimelineSettings.Instance.Enabled)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                        continue;
                    }

                    // P-Syncを判定する
                    this.DetectPSyncTriggers();

                    // 以後ログに対して判定する
                    if (this.logInfoQueue == null ||
                        this.logInfoQueue.IsEmpty)
                    {
                        continue;
                    }

                    var logs = this.GetLogs();
                    if (!logs.Any())
                    {
                        continue;
                    }

                    isExistsLog = true;
                    this.DetectLogs(logs);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    this.AppLogger.Error(
                        ex,
                        $"[TL] Error DetectLog. name={this.Model.TimelineName}, zone={this.Model.Zone}, file={this.Model.SourceFile}");
                }
                finally
                {
                    if (isExistsLog)
                    {
                        Thread.Yield();
                    }
                    else
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(Settings.Default.LogPollSleepInterval));
                    }
                }
            }
        }

        private long no = 0L;

        private IReadOnlyList<XIVLog> GetLogs()
        {
            var list = new List<XIVLog>(this.logInfoQueue.Count);

            if (this.logInfoQueue != null)
            {
                var prelog = new string[3];
                var prelogIndex = 0;

                var ignores = TimelineSettings.Instance.IgnoreLogTypes.Where(x => x.IsIgnore);

                while (this.logInfoQueue.TryDequeue(out LogLineEventArgs logInfo))
                {
                    var logLine = logInfo.logLine;

                    // 直前とまったく同じログはスキップする
                    if (prelog[0] == logLine ||
                        prelog[1] == logLine ||
                        prelog[2] == logLine)
                    {
                        continue;
                    }

                    prelog[prelogIndex++] = logLine;
                    if (prelogIndex >= 3)
                    {
                        prelogIndex = 0;
                    }

                    // [TL]キーワードが含まれていればスキップする
                    if (logLine.Contains(TLSymbol))
                    {
                        continue;
                    }

                    // ダメージ系ログをカットする
                    if (Settings.Default.IgnoreDamageLogs &&
                        LogBuffer.DamageLogPattern.IsMatch(logLine))
                    {
                        continue;
                    }

                    // 無効キーワードが含まれていればスキップする
                    if (ignores.Any(x => logLine.Contains(x.Keyword)))
                    {
                        continue;
                    }

                    // パーティメンバに対するHPログならばスキップする
                    if (LogBuffer.IsHPLogByPartyMember(logLine))
                    {
                        continue;
                    }

                    // エフェクトに付与されるツールチップ文字を除去する
                    // 4文字分のツールチップ文字を除去する
                    int index;
                    if ((index = logLine.IndexOf(
                        LogBuffer.TooltipSuffix,
                        0,
                        StringComparison.Ordinal)) > -1)
                    {
                        logLine = logLine.Remove(index - 1, 4);
                    }

                    // 残ったReplacementCharを除去する
                    logLine = logLine.Replace(LogBuffer.TooltipReplacementChar, string.Empty);

                    list.Add(new XIVLog(logLine)
                    {
                        No = this.no++
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// ログに対して判定する
        /// </summary>
        /// <param name="logs">ログ</param>
        private void DetectLogs(
            IReadOnlyList<XIVLog> logs)
        {
            var detectTime = DateTime.Now;
            var detectors = new List<TimelineBase>(64);

            lock (this)
            {
                // グローバルトリガを登録する
                detectors.AddRange(TimelineManager.Instance.GlobalTriggers);

                // ToHideトリガを登録する
                detectors.AddRange(TimelineVisualNoticeModel.GetSyncToHideList());
                detectors.AddRange(TimelineImageNoticeModel.GetSyncToHideList());

                if (!this.Model.IsGlobalZone)
                {
                    // タイムラインスコープのトリガを登録する
                    detectors.AddRange(
                        from x in this.Model.Triggers
                        where
                        x.Enabled.GetValueOrDefault() &&
                        !string.IsNullOrEmpty(x.SyncKeyword) &&
                        x.SyncRegex != null
                        select
                        x);

                    // 判定期間中のアクティビティを登録する
                    var acts =
                        from x in this.ActivityLine
                        where
                        x.Enabled.GetValueOrDefault() &&
                        !string.IsNullOrEmpty(x.SyncKeyword) &&
                        x.SyncRegex != null &&
                        this.CurrentTime >= x.Time + TimeSpan.FromSeconds(x.SyncOffsetStart.Value) &&
                        this.CurrentTime <= x.Time + TimeSpan.FromSeconds(x.SyncOffsetEnd.Value) &&
                        !x.IsSynced
                        select
                        x;

                    detectors.AddRange(acts);

                    // カレントサブルーチンのトリガを登録する
                    if (this.CurrentSubroutine != null)
                    {
                        var triggers =
                            from x in this.CurrentSubroutine.Triggers
                            where
                            x.Enabled.GetValueOrDefault() &&
                            !string.IsNullOrEmpty(x.SyncKeyword) &&
                            x.SyncRegex != null
                            select
                            x;

                        detectors.AddRange(triggers);
                    }
                }
            }

            // 開始・終了判定のキーワードを取得する
            var keywords = CombatAnalyzer.Keywords.Where(x =>
                x.Category == KewordTypes.TimelineStart ||
                x.Category == KewordTypes.End);

            // ログに対して判定する
            logs.AsParallel().ForAll(xivlog =>
            {
                // 開始・終了を判定する
                var key = (
                    from x in keywords
                    where
                    xivlog.Log.ContainsIgnoreCase(x.Keyword)
                    select
                    x).FirstOrDefault();

                if (key != null)
                {
                    switch (key.Category)
                    {
                        case KewordTypes.TimelineStart:
                            if (this.Model.StartTriggerRegex == null)
                            {
                                WPFHelper.BeginInvoke(() =>
                                {
                                    Thread.Sleep(TimeSpan.FromSeconds(4.8));
                                    this.StartActivityLine();
                                });
                            }
                            break;

                        case KewordTypes.End:
                            WPFHelper.BeginInvoke(this.EndActivityLine);
                            break;
                    }
                }

                // StartTriggerがある？
                if (!this.isRunning)
                {
                    if (this.Model.StartTriggerRegex != null)
                    {
                        var match = this.Model.StartTriggerRegex.Match(xivlog.Log);
                        if (match.Success)
                        {
                            WPFHelper.BeginInvoke(this.StartActivityLine);
                        }
                    }
                }

                // アクティビティ・トリガとマッチングする
                detectors.AsParallel().ForAll(detector =>
                {
                    switch (detector)
                    {
                        case TimelineActivityModel act:
                            detectActivity(xivlog, act);
                            break;

                        case TimelineTriggerModel tri:
                            detectTrigger(xivlog, tri);
                            break;

                        case TimelineVisualNoticeModel vnotice:
                            lock (vnotice)
                            {
                                vnotice.TryHide(xivlog.Log);
                            }
                            break;

                        case TimelineImageNoticeModel inotice:
                            lock (inotice)
                            {
                                inotice.TryHide(xivlog.Log);
                            }
                            break;
                    }
                });
            });

            // アクティビティに対して判定する
            bool detectActivity(
                XIVLog xivlog,
                TimelineActivityModel act)
            {
                var match = default(Match);

                lock (act)
                {
                    if (act.IsSynced)
                    {
                        return false;
                    }

                    match = act.TryMatch(xivlog.Log);
                    if (match == null ||
                        !match.Success)
                    {
                        return false;
                    }

                    act.IsSynced = true;
                }

                WPFHelper.BeginInvoke(() =>
                {
                    lock (this)
                    {
                        foreach (var item in this.ActivityLine.Where(x =>
                            x.IsDone &&
                            x.Seq >= act.Seq))
                        {
                            // 表示の制御に関するフラグを初期化する
                            item.IsActive = false;
                            item.IsDone = false;

                            // まだ通知時間が到来していないことになるアクティビティの通知済みフラグを落とす
                            if (item.Time.Add(TimeSpan.FromSeconds(item.NoticeOffset.GetValueOrDefault()))
                                > act.Time)
                            {
                                item.IsNotified = false;
                            }
                        }

                        foreach (var item in ActivityLine.Where(x =>
                            !x.IsDone &&
                            x.Seq < act.Seq))
                        {
                            item.IsDone = true;
                            item.IsNotified = true;
                        }

                        this.CurrentTime = act.Time;

                        // ログを発生させる
                        RaiseLog(act);
                    }
                });

                return true;
            }

            // トリガに対して判定する
            bool detectTrigger(
                XIVLog xivlog,
                TimelineTriggerModel tri)
            {
                // P-Syncならば対象外なので抜ける
                if (tri.IsPositionSyncAvailable)
                {
                    return false;
                }

                lock (tri)
                {
                    var match = tri.TryMatch(xivlog.Log);
                    if (match == null ||
                        !match.Success)
                    {
                        return false;
                    }

                    tri.TextReplaced = match.Result(tri.Text ?? string.Empty);
                    tri.NoticeReplaced = match.Result(tri.Notice ?? string.Empty);

                    tri.MatchedCounter++;

                    if (tri.SyncCount.Value != 0)
                    {
                        if (tri.SyncCount.Value != tri.MatchedCounter)
                        {
                            return false;
                        }
                    }

                    var toNotice = tri.Clone();
                    toNotice.LogSeq = xivlog.No;

                    var vnotices = toNotice.VisualNoticeStatements.Where(x => x.Enabled.GetValueOrDefault());
                    if (vnotices.Any())
                    {
                        foreach (var vnotice in vnotices)
                        {
                            vnotice.Timestamp = detectTime;
                        }
                    }

                    var inotices = toNotice.ImageNoticeStatements.Where(x => x.Enabled.GetValueOrDefault());
                    if (inotices.Any())
                    {
                        foreach (var inotice in inotices)
                        {
                            inotice.Timestamp = detectTime;
                        }
                    }

                    this.NotifyQueue.Enqueue(toNotice);
                }

                WPFHelper.BeginInvoke(() =>
                {
                    lock (this)
                    {
                        var active = (
                            from x in this.ActivityLine
                            where
                            x.IsActive &&
                            !x.IsDone &&
                            x.Time <= this.CurrentTime
                            orderby
                            x.Seq descending
                            select
                            x).FirstOrDefault();

                        try
                        {
                            this.Model.StopLive();

                            // jumpを判定する
                            if (!this.CallActivity(active, tri.CallTarget))
                            {
                                if (!this.GoToActivity(active, tri.GoToDestination))
                                {
                                    this.LoadSubs(tri);
                                }
                            }

                            // ログを発生させる
                            RaiseLog(tri);
                        }
                        finally
                        {
                            this.Model.ResumeLive();
                        }
                    }
                });

                return true;
            }
        }

        /// <summary>
        /// P-Syncトリガを判定する
        /// </summary>
        private void DetectPSyncTriggers()
        {
            var detectTime = DateTime.Now;
            var detectors = new List<TimelineTriggerModel>(64);

            lock (this)
            {
                // グローバルトリガを登録する
                detectors.AddRange(TimelineManager.Instance.GlobalTriggers);

                if (!this.Model.IsGlobalZone)
                {
                    // タイムラインスコープのトリガを登録する
                    detectors.AddRange(
                        from x in this.Model.Triggers
                        where
                        x.IsAvailable() &&
                        x.IsPositionSyncAvailable
                        select
                        x);

                    // カレントサブルーチンのトリガを登録する
                    if (this.CurrentSubroutine != null)
                    {
                        var triggers =
                            from x in this.CurrentSubroutine.Triggers
                            where
                            x.IsAvailable() &&
                            x.IsPositionSyncAvailable
                            select
                            x;

                        detectors.AddRange(triggers);
                    }
                }
            }

            // P-Syncトリガに対して判定する
            var psyncs = detectors
                .Where(x => x.IsPositionSyncAvailable)
                .ToArray();

            if (!psyncs.Any())
            {
                return;
            }

            // Combatantsを取得する
            var combatants = FFXIVPlugin.Instance.GetCombatantList();

            if (!combatants.Any())
            {
                return;
            }

            psyncs.AsParallel().ForAll(tri => detectPSync(tri));

            // P-Syncトリガに対して判定する
            void detectPSync(
                TimelineTriggerModel tri)
            {
                lock (tri)
                {
                    var psync = tri.PositionSyncStatements
                        .FirstOrDefault(x => x.Enabled.GetValueOrDefault());

                    if (psync == null)
                    {
                        return;
                    }

                    if ((DateTime.Now - psync.LastSyncTimestamp).TotalSeconds <= psync.Interval)
                    {
                        return;
                    }

                    var conditions = psync.Combatants
                        .Where(x =>
                            x.Enabled.GetValueOrDefault() &&
                            !string.IsNullOrEmpty(x.Name));

                    if (!conditions.Any())
                    {
                        return;
                    }

                    foreach (var con in conditions)
                    {
                        var target = combatants.FirstOrDefault(x =>
                        {
                            var r = false;

                            if (con.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                if (con.X == TimelineCombatantModel.InvalidPosition ||
                                    (con.X - con.Tolerance) <= x.PosXMap && x.PosXMap <= (con.X + con.Tolerance))
                                {
                                    if (con.Y == TimelineCombatantModel.InvalidPosition ||
                                        (con.Y - con.Tolerance) <= x.PosYMap && x.PosYMap <= (con.Y + con.Tolerance))
                                    {
                                        if (con.Z == TimelineCombatantModel.InvalidPosition ||
                                            (con.Z - con.Tolerance) <= x.PosZMap && x.PosZMap <= (con.Z + con.Tolerance))
                                        {
                                            r = true;
                                        }
                                    }
                                }
                            }

                            return r;
                        });

                        con.ActualCombatant = target;
                    }

                    if (conditions.Count(x => x.ActualCombatant != null) <
                        conditions.Count())
                    {
                        return;
                    }

                    tri.TextReplaced = tri.Text ?? string.Empty;
                    tri.NoticeReplaced = tri.Notice ?? string.Empty;

                    var i = 1;
                    foreach (var con in conditions)
                    {
                        string replace(string text)
                        {
                            text = text.Replace("{name" + i + "}", con.ActualCombatant.Name);
                            text = text.Replace("{X" + i + "}", con.ActualCombatant.PosXMap.ToString("N1"));
                            text = text.Replace("{Y" + i + "}", con.ActualCombatant.PosYMap.ToString("N1"));
                            text = text.Replace("{Z" + i + "}", con.ActualCombatant.PosZMap.ToString("N1"));

                            return text;
                        }

                        tri.TextReplaced = replace(tri.TextReplaced);
                        tri.NoticeReplaced = replace(tri.NoticeReplaced);

                        i++;
                    }

                    tri.MatchedCounter++;

                    if (tri.SyncCount.Value != 0)
                    {
                        if (tri.SyncCount.Value != tri.MatchedCounter)
                        {
                            return;
                        }
                    }

                    psync.LastSyncTimestamp = DateTime.Now;

                    var toNotice = tri.Clone();

                    var vnotices = toNotice.VisualNoticeStatements.Where(x => x.Enabled.GetValueOrDefault());
                    if (vnotices.Any())
                    {
                        foreach (var vnotice in vnotices)
                        {
                            vnotice.Timestamp = detectTime;
                        }
                    }

                    var inotices = toNotice.ImageNoticeStatements.Where(x => x.Enabled.GetValueOrDefault());
                    if (inotices.Any())
                    {
                        foreach (var inotice in inotices)
                        {
                            inotice.Timestamp = detectTime;
                        }
                    }

                    this.NotifyQueue.Enqueue(toNotice);
                }

                WPFHelper.BeginInvoke(() =>
                {
                    lock (this)
                    {
                        var active = (
                            from x in this.ActivityLine
                            where
                            x.IsActive &&
                            !x.IsDone &&
                            x.Time <= this.CurrentTime
                            orderby
                            x.Seq descending
                            select
                            x).FirstOrDefault();

                        try
                        {
                            this.Model.StopLive();

                            // jumpを判定する
                            if (!this.CallActivity(active, tri.CallTarget))
                            {
                                if (!this.GoToActivity(active, tri.GoToDestination))
                                {
                                    this.LoadSubs(tri);
                                }
                            }

                            // ログを発生させる
                            RaiseLog(tri);
                        }
                        finally
                        {
                            this.Model.ResumeLive();
                        }
                    }
                });
            }
        }

        /// <summary>
        /// ログを発生させる
        /// </summary>
        /// <param name="element"></param>
        private static void RaiseLog(
            TimelineBase element)
        {
            var now = DateTime.Now;
            var log = string.Empty;

            var sub = element.Parent as TimelineSubroutineModel;

            switch (element)
            {
                case TimelineActivityModel act:
                    log =
                        $"{TLSymbol} Synced to activity. " +
                        $"name={act.Name}, sub={sub?.Name}";
                    break;

                case TimelineTriggerModel tri:
                    log =
                        $"{TLSymbol} Synced to trigger. " +
                        $"name={tri.Name}, sync-count={tri.MatchedCounter}, sub={sub?.Name}";
                    break;

                default:
                    return;
            }

            TimelineController.RaiseLog(log);
        }

        #endregion Log 関係のスレッド

        #region 時間進行関係のスレッド

        private DispatcherTimer TimelineTimer
        {
            get;
            set;
        } = null;

        private volatile bool isRunning = false;

        public bool IsRunning => this.isRunning;

        public TimelineActivityModel ActiveActivity
        {
            get;
            private set;
        }

        public TimelineSubroutineModel CurrentSubroutine
        {
            get;
            private set;
        }

        public void StartActivityLine()
        {
            if (!TimelineSettings.Instance.Enabled)
            {
                return;
            }

            lock (this)
            {
                if (this.isRunning)
                {
                    return;
                }

                // 有効なActivityが存在しない？
                if (!this.Model.ExistsActivities())
                {
                    return;
                }

                this.CurrentTime = TimeSpan.Zero;

                if (this.TimelineTimer == null)
                {
                    this.TimelineTimer = new DispatcherTimer()
                    {
                        Interval = TimeSpan.FromSeconds(0.02),
                    };

                    this.TimelineTimer.Tick += this.TimelineTimer_Tick;
                }

                this.PreviouseDetectTime = DateTime.Now;
                this.TimelineTimer.Start();

                this.isRunning = true;
                this.Status = TimelineStatus.Runnning;
                this.AppLogger.Trace($"{TLSymbol} Timeline started. name={this.Model.TimelineName}");
            }
        }

        public void EndActivityLine()
        {
            lock (this)
            {
                if (!this.isRunning)
                {
                    return;
                }

                this.TimelineTimer.Stop();
                this.LoadActivityLine();

                TimelineNoticeOverlay.NoticeView?.ClearNotice();

                this.isRunning = false;
                this.Status = TimelineStatus.Loaded;
                this.AppLogger.Trace($"{TLSymbol} Timeline stoped. name={this.Model.TimelineName}");
            }
        }

        private volatile bool isTimelineTicking = false;

        private void TimelineTimer_Tick(
            object sender,
            EventArgs e)
        {
            if (this.isTimelineTicking)
            {
                return;
            }

            this.isTimelineTicking = true;

            try
            {
                if (!TimelineSettings.Instance.Enabled)
                {
                    return;
                }

                lock (this)
                {
                    var now = DateTime.Now;
                    this.CurrentTime += now - this.PreviouseDetectTime;
                    this.PreviouseDetectTime = now;

                    this.RefreshActivityLine();
                }
            }
            catch (Exception ex)
            {
                this.AppLogger.Error(
                    ex,
                    $"[TL] Error Timeline ticker. name={this.Model.TimelineName}, zone={this.Model.Zone}, file={this.Model.SourceFile}");
            }
            finally
            {
                this.isTimelineTicking = false;
            }
        }

        private void RefreshActivityLine()
        {
            if (this.CurrentTime == TimeSpan.Zero)
            {
                return;
            }

            // 現在の時間を更新する
            TimelineActivityModel.CurrentTime = this.CurrentTime;

            // 通知を判定する
            var toNotify =
                from x in this.ActivityLine
                where
                !x.IsNotified &&
                x.Time + TimeSpan.FromSeconds(x.NoticeOffset.Value) <= this.CurrentTime
                select
                x;

            // 通知キューを登録する
            var now = DateTime.Now;
            foreach (var act in toNotify)
            {
                var vnotices = act.VisualNoticeStatements.Where(x => x.Enabled.GetValueOrDefault());
                foreach (var vnotice in vnotices)
                {
                    vnotice.Timestamp = now;
                }

                var inotices = act.ImageNoticeStatements.Where(x => x.Enabled.GetValueOrDefault());
                foreach (var inotice in inotices)
                {
                    inotice.Timestamp = now;
                }

                this.NotifyQueue.Enqueue(act);
            }

            // 表示を終了させる
            var toDoneTop = (
                from x in this.ActivityLine
                where
                !x.IsDone &&
                x.Time <= this.CurrentTime - TimeSpan.FromSeconds(1)
                orderby
                x.Seq descending
                select
                x).FirstOrDefault();

            if (toDoneTop != null)
            {
                foreach (var act in this.ActivityLine
                    .Where(x =>
                        !x.IsDone &&
                        x.Seq <= toDoneTop.Seq))
                {
                    act.IsDone = true;
                }
            }

            // Activeなアクティビティを決める
            var active = (
                from x in this.ActivityLine
                where
                !x.IsActive &&
                !x.IsDone &&
                x.Time <= this.CurrentTime
                orderby
                x.Seq descending
                select
                x).FirstOrDefault();

            if (active != null)
            {
                // アクティブなアクティビティを設定する
                this.ActiveActivity = active;
                active.IsActive = true;

                // jumpを判定する
                if (!this.CallActivity(active))
                {
                    this.GoToActivity(active);
                }
            }

            // 表示を更新する
            this.RefreshActivityLineVisibility();
        }

        public void RefreshActivityLineVisibility()
        {
            var maxTime = this.CurrentTime.Add(TimeSpan.FromSeconds(
                TimelineSettings.Instance.ShowActivitiesTime));

            var toShow =
                from x in this.ActivityLine
                where
                x.Enabled.GetValueOrDefault() &&
                !string.IsNullOrEmpty(x.Text)
                orderby
                x.Seq ascending
                select
                x;

            var count = 0;
            foreach (var x in toShow)
            {
                if (count < TimelineSettings.Instance.ShowActivitiesCount &&
                    !x.IsDone &&
                    x.Time <= maxTime)
                {
                    if (count == 0)
                    {
                        var sub = x.Parent as TimelineSubroutineModel;
                        if (this.CurrentSubroutine != sub)
                        {
                            this.CurrentSubroutine = sub;
                        }

                        this.Model.SubName = this.CurrentSubroutine?.Name ?? string.Empty;

                        x.IsTop = true;
                        x.Opacity = 1.0d;
                        x.Scale = TimelineSettings.Instance.NearestActivityScale;
                    }
                    else
                    {
                        x.IsTop = false;
                        x.Opacity = TimelineSettings.Instance.NextActivityBrightness;
                        x.Scale = 1.0d;
                    }

                    x.IsVisible = true;
                    x.RefreshProgress();
                    count++;
                }
                else
                {
                    x.IsVisible = false;
                }
            }
        }

        #endregion 時間進行関係のスレッド

        #region 通知に関するメソッド

        private static readonly object NoticeLocker = new object();
        private readonly ConcurrentQueue<TimelineBase> NotifyQueue = new ConcurrentQueue<TimelineBase>();

        private ThreadWorker notifyWorker;
        private volatile bool isNotifyExcuting = false;

        private void StartNotifyWorker()
        {
            lock (NoticeLocker)
            {
                if (this.notifyWorker == null)
                {
                    this.notifyWorker = new ThreadWorker(
                        this.DoNotify,
                        TimelineSettings.Instance.NotifyInterval,
                        "TimelineNotifyWorker",
                        ThreadPriority.Normal);
                }
            }

            this.notifyWorker.Run();
        }

        public void StopNotifyWorker()
        {
            lock (NoticeLocker)
            {
                if (this.notifyWorker != null)
                {
                    this.notifyWorker.Abort();
                    while (this.NotifyQueue.TryDequeue(out TimelineBase q)) ;
                    this.notifyWorker = null;
                }
            }
        }

        private void DoNotify()
        {
            this.notifyWorker.Interval = TimelineSettings.Instance.NotifyInterval;

            if (this.NotifyQueue.IsEmpty)
            {
                return;
            }

            try
            {
                if (this.isNotifyExcuting)
                {
                    return;
                }

                this.isNotifyExcuting = true;

                while (this.NotifyQueue.TryDequeue(out TimelineBase element))
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
                }

                this.notifyWorker.Interval = 0;
            }
            finally
            {
                this.isNotifyExcuting = false;
            }
        }

        private void NotifyActivity(
            TimelineActivityModel act)
        {
            if (string.IsNullOrEmpty(act.Name) &&
                string.IsNullOrEmpty(act.Text) &&
                string.IsNullOrEmpty(act.Notice))
            {
                return;
            }

            lock (act)
            {
                if (act.IsNotified)
                {
                    return;
                }

                act.IsNotified = true;
            }

            var now = DateTime.Now;
            var offset = this.CurrentTime - act.Time;
            var log =
                $"{TLSymbol} Notice from TL. " +
                $"name={act.Name}, text={act.TextReplaced}, notice={act.NoticeReplaced}, offset={offset.TotalSeconds:N1}";

            var notice = act.NoticeReplaced;
            if (string.Equals(notice, "auto", StringComparison.OrdinalIgnoreCase))
            {
                notice = !string.IsNullOrEmpty(act.TextReplaced) ?
                    act.TextReplaced :
                    act.Name;

                if (offset.TotalSeconds <= -1.0)
                {
                    var ofsetText = (offset.TotalSeconds * -1).ToString("N0");
                    notice += $" まで、あと{ofsetText}秒";
                }

                if (!string.IsNullOrEmpty(notice))
                {
                    notice += "。";
                }
            }

            var isSync = TimelineModel.RazorModel?.SyncTTS ?? false;

            RaiseLog(log);
            NotifySound(notice, act.NoticeDevice.GetValueOrDefault(), isSync);

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

            WPFHelper.BeginInvoke(() =>
            {
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
                            v.TextToDisplay = v.Text;
                            break;
                    }

                    if (string.IsNullOrEmpty(v.TextToDisplay))
                    {
                        continue;
                    }

                    // PC名をルールに従って置換する
                    v.TextToDisplay = FFXIVPlugin.Instance.ReplacePartyMemberName(
                        v.TextToDisplay,
                        Settings.Default.PCNameInitialOnDisplayStyle);

                    TimelineNoticeOverlay.NoticeView?.AddNotice(v);

                    v.SetSyncToHide(placeholders);
                    v.AddSyncToHide();
                }

                foreach (var i in inotices)
                {
                    i.StartNotice();
                    i.SetSyncToHide(placeholders);
                    i.AddSyncToHide();
                }
            });
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
                $"{TLSymbol} Notice from TL. " +
                $"name={tri.Name}, text={tri.TextReplaced}, notice={tri.NoticeReplaced}";

            var notice = tri.NoticeReplaced;
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

            var isSync = TimelineModel.RazorModel?.SyncTTS ?? false;

            RaiseLog(log);
            NotifySound(notice, tri.NoticeDevice.GetValueOrDefault(), isSync);

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

            WPFHelper.BeginInvoke(() =>
            {
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
                            v.TextToDisplay = v.Text;
                            break;
                    }

                    if (string.IsNullOrEmpty(v.TextToDisplay))
                    {
                        continue;
                    }

                    // PC名をルールに従って置換する
                    v.TextToDisplay = FFXIVPlugin.Instance.ReplacePartyMemberName(
                        v.TextToDisplay,
                        Settings.Default.PCNameInitialOnDisplayStyle);

                    TimelineNoticeOverlay.NoticeView?.AddNotice(v);

                    v.SetSyncToHide(placeholders);
                    v.AddSyncToHide();
                }

                foreach (var i in inotices)
                {
                    i.StartNotice();
                    i.SetSyncToHide(placeholders);
                    i.AddSyncToHide();
                }
            });
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

        private static void NotifySound(
            string notice,
            NoticeDevices device,
            bool isSync = false)
        {
            if (string.IsNullOrEmpty(notice))
            {
                return;
            }

            if (TimelineSettings.Instance.IsMute)
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
                    SoundController.Instance.Play(notice, isSync);
                    break;

                case NoticeDevices.Main:
                    PlayBridge.Instance.PlayMain(notice, isSync);
                    break;

                case NoticeDevices.Sub:
                    PlayBridge.Instance.PlaySub(notice, isSync);
                    break;
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now:HH:mm:ss.fff} notify={notice}");
#endif
        }

        #endregion 通知に関するメソッド
    }
}
