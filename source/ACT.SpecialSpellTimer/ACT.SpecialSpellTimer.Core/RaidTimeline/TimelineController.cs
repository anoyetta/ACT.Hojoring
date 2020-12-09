using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.RaidTimeline.Views;
using ACT.SpecialSpellTimer.RazorModel;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.XIVHelper;
using Microsoft.CodeAnalysis.Scripting;
using Prism.Mvvm;
using Sharlayan.Core.Enums;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    public enum TimelineStatus
    {
        Unloaded = 0,
        Loading,
        Loaded,
        Runnning,
        Error
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
                "Error",
            }[(int)s];

        public static string ToIndicator(
            this TimelineStatus s)
            => new[]
            {
                string.Empty,
                "Ｒ",
                "⬛",
                "▶",
                "Ｅ"
            }[(int)s];
    }

    public partial class TimelineController :
        BindableBase
    {
        #region Logger

        private NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        private static readonly object Locker = new object();

        public static void Init()
        {
            lock (Locker)
            {
                TimelineOverlay.LoadResourcesDictionary();

                // テキストコマンドを登録する
                TimelineTextCommands.SetSubscribeTextCommands();

                InitScriptingHost();

                isDetectLogWorking = true;

                if (LogWorker == null)
                {
                    LogWorker = new Thread(DetectLogLoopRoot)
                    {
                        IsBackground = true
                    };
                }

                if (!LogWorker.IsAlive)
                {
                    LogWorker.Start();
                }
            }
        }

        public static void Free()
        {
            lock (Locker)
            {
                TimelineOverlay.CloseTimeline();
                TimelineNoticeOverlay.CloseNotice();
                TimelineImageNoticeModel.Collect();

                isDetectLogWorking = false;

                if (LogWorker != null)
                {
                    LogWorker.Join(100);
                    if (LogWorker.IsAlive)
                    {
                        LogWorker.Abort();
                    }

                    LogWorker = null;
                }
            }
        }

        private static void InitScriptingHost()
        {
            // タイムラインスクリプトにデリゲートを設定する
            var scriptGlobal = TimelineScriptGlobalModel.Instance;

            scriptGlobal.RaiseLogLineDelegate =
                (logLine) => RaiseLog($"{TimelineConstants.TLXLogSymbol} {logLine}");

            scriptGlobal.TraseDelegate =
                (message) => RaiseLog($"{TimelineConstants.TLXTraceLogSymbol} {message}");

            scriptGlobal.GetCurrentSubRoutineNameDelegate =
                () => CurrentController?.CurrentSubroutine?.Name ?? string.Empty;

            scriptGlobal.GetPlayerDelegate =
                () => CombatantsManager.Instance.Player;

            scriptGlobal.GetPartyDelegate =
                () => CombatantsManager.Instance.GetPartyList().ToArray();

            scriptGlobal.GetCombatantsDelegate =
                () => CombatantsManager.Instance.GetCombatants().ToArray();

            scriptGlobal.TTSDelegate = (
                tts,
                device,
                sync,
                volume,
                delay) =>
            {
                if (string.IsNullOrEmpty(tts))
                {
                    return;
                }

                var noticeDevice = device.ToLower() switch
                {
                    "main" => NoticeDevices.Main,
                    "sub" => NoticeDevices.Main,
                    "both" => NoticeDevices.Both,
                    _ => NoticeDevices.Main
                };

                NotifySoundAsync(
                    tts,
                    noticeDevice,
                    sync,
                    volume,
                    delay);
            };

            scriptGlobal.ShowTickerDelegate = (
                message,
                icon,
                order,
                delay,
                duration,
                durationVisible,
                syncToHide,
                fontScale) =>
            {
                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                WPFHelper.BeginInvoke(async () =>
                {
                    var v = new TimelineVisualNoticeModel();

                    v.Enabled = true;
                    v.LogSeq = long.MaxValue;
                    v.Timestamp = DateTime.Now;

                    if (!string.IsNullOrEmpty(icon))
                    {
                        var c = CombatantsManager.Instance.GetCombatant(icon);
                        if (c != null)
                        {
                            v.Icon = $"{c.JobID}.png";
                        }
                        else
                        {
                            v.Icon = icon;
                        }
                    }

                    v.StyleModel = TimelineSettings.Instance.DefaultNoticeStyle;
                    v.Order = order;
                    v.Delay = delay;
                    v.Duration = duration;
                    v.DurationVisible = durationVisible;
                    v.FontScale = fontScale;

                    var text = message;
                    text = TimelineExpressionsModel.ReplaceText(text);
                    text = XIVPluginHelper.Instance.ReplacePartyMemberName(
                        text,
                        Settings.Default.PCNameInitialOnDisplayStyle);

                    v.TextToDisplay = text;

                    if (!string.IsNullOrEmpty(syncToHide))
                    {
                        v.SyncToHideKeyword = syncToHide ?? string.Empty;
                    }

                    if (v.Delay > 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(v.Delay ?? 0));
                    }

                    TimelineNoticeOverlay.NoticeView?.AddNotice(v);
                    v.SetSyncToHide(TimelineManager.Instance.GetPlaceholders());
                    v.AddSyncToHide();
                });
            };
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

        public string CurrentZoneName => ActGlobals.oFormActMain.CurrentZone.Trim();

        public bool IsAvailable
        {
            get
            {
                if (this.Model.HasError)
                {
                    return false;
                }

                if (!this.Model.IsEnabled)
                {
                    return false;
                }

                if (Settings.Default.FFXIVLocale != this.Model.Locale)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(this.Model.Zone))
                {
                    return false;
                }

                if (string.Equals(
                        XIVPluginHelper.Instance?.GetCurrentZoneID().ToString().Trim(),
                        this.Model.Zone.Trim()))
                {
                    return true;
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
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        XIVPluginHelper.Instance?.GetCurrentZoneID().ToString().Trim(),
                        x.Trim()));
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
                this.Model.RefreshActivitiesView();

                if (!this.Model.IsGlobalZone)
                {
                    TimelineOverlay.ShowTimeline(this.Model);
                }

                TimelineNoticeOverlay.ShowNotice();

                this.StartNotifyWorker();

                Task.Run(() => TimelineScriptGlobalModel.Instance.ScriptingHost.ExecuteOnLoad());

                this.Status = TimelineStatus.Loaded;
                this.IsReady = true;
                this.AppLogger.Trace($"{TimelineConstants.LogSymbol} Timeline loaded. name={this.Model.TimelineName}");
            }
        }

        public void Unload()
        {
            lock (Locker)
            {
                TimelineTickCallback = null;

                TimelineOverlay.CloseTimeline();
                TimelineNoticeOverlay.CloseNotice();
                TimelineImageNoticeModel.Collect();

                this.CurrentTime = TimeSpan.Zero;
                this.ClearActivity();
                this.Model.RefreshActivitiesView();

                this.StopNotifyWorker();

                CurrentController = null;

                this.Status = TimelineStatus.Unloaded;
                this.IsReady = false;
                this.AppLogger.Trace($"{TimelineConstants.LogSymbol} Timeline unloaded. name={this.Model.TimelineName}");

                // GC
                GC.Collect();
            }
        }

        private void LoadActivityLine()
        {
            this.CurrentTime = TimeSpan.Zero;
            TimelineActivityModel.CurrentTime = TimeSpan.Zero;
            this.ClearActivity();

            // Script を初期化する
            TimelineScriptGlobalModel.Instance.ScriptingHost.Clear();

            // 初期化する
            TimelineManager.Instance.ReloadGlobalTriggers();
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
                acts.Add(act);
            }

            // 一括して登録する
            this.AddRangeActivity(acts);

            // toHideリストを初期化する
            TimelineVisualNoticeModel.ClearSyncToHideList();
            TimelineImageNoticeModel.ClearSyncToHideList();
            TimelineVisualNoticeModel.ClearToHideEntry();

            // タイムライン制御フラグを初期化する
            TimelineExpressionsModel.Clear(this.CurrentZoneName);

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

                        // ジャンプ後のアクティビティを初期化する
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

        private static readonly Lazy<ConcurrentQueue<XIVLog>> LazyXIVLogBuffer = new Lazy<ConcurrentQueue<XIVLog>>(()
            => XIVPluginHelper.Instance.SubscribeXIVLog(() =>
                TimelineManager.Instance.IsLoading ||
                (CurrentController != null && CurrentController.IsReady)));

        public static ConcurrentQueue<XIVLog> XIVLogQueue => LazyXIVLogBuffer.Value;

        private static volatile bool isDetectLogWorking = false;

        private static Thread LogWorker;

        private static void DetectLogLoopRoot()
        {
            while (isDetectLogWorking)
            {
                var existsLog = false;

                try
                {
                    if (!ActGlobals.oFormActMain.InitActDone ||
                        TimelineManager.Instance.IsLoading)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(3));
                        continue;
                    }

                    if (CurrentController != null &&
                        CurrentController.IsReady)
                    {
                        existsLog = CurrentController?.DetectLogCore() ?? false;
                    }
                    else
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    var model = CurrentController?.Model;
                    AppLog.DefaultLogger.Error(
                        ex,
                        $"{TimelineConstants.LogSymbol} Error DetectLog. name={model?.TimelineName}, zone={model?.Zone}, file={model?.SourceFile}");
                }
                finally
                {
                    if (existsLog)
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

        private DateTime lastPSyncDetectTimestamp = DateTime.MinValue;
        private DateTime lastResidentScriptTimestamp = DateTime.MinValue;

        private bool IsReady { get; set; } = false;

        private bool DetectLogCore()
        {
            var existsLog = false;

            if (!TimelineSettings.Instance.Enabled)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
                return existsLog;
            }

            // グローバル変数を更新する
            TimelineExpressionsModel.RefreshGlobalVariables();

            var detectPSyncTask = default(Task);
            var scriptingTask = default(Task);
            var optionTasks = new[] { detectPSyncTask, scriptingTask };

            try
            {
                if ((DateTime.Now - this.lastPSyncDetectTimestamp).TotalMilliseconds
                    > TimelineSettings.Instance.PSyncDetectInterval)
                {
                    this.lastPSyncDetectTimestamp = DateTime.Now;
                    detectPSyncTask = Task.Run(() => this.DetectPSyncTriggers());
                }

                if ((DateTime.Now - this.lastResidentScriptTimestamp).TotalMilliseconds
                    > TimelineSettings.Instance.ResidentScriptInterval)
                {
                    this.lastResidentScriptTimestamp = DateTime.Now;
                    scriptingTask = Task.Run(() =>
                        TimelineScriptGlobalModel.Instance.ScriptingHost.ExecuteResidents(this.CurrentSubroutine?.Name ?? string.Empty));
                }

                // 以後ログに対して判定する
                if (XIVLogQueue == null ||
                    XIVLogQueue.IsEmpty)
                {
                    return existsLog;
                }

                var logs = this.GetLogs();
                if (!logs.Any())
                {
                    return existsLog;
                }

                existsLog = true;
                this.DetectLogs(logs);
            }
            finally
            {
                Task.WaitAll(
                    optionTasks.Where(x => x != null).ToArray());
            }

            return existsLog;
        }

        private IReadOnlyList<XIVLog> GetLogs()
        {
            var list = new List<XIVLog>(XIVLogQueue.Count);

            if (XIVLogQueue == null)
            {
                return list;
            }

            var prelog = new string[3];
            var prelogIndex = 0;

            while (XIVLogQueue.TryDequeue(out XIVLog xivlog))
            {
                var logLine = xivlog.LogLine;

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
                // [TLX-Trace]キーワードが含まれていればスキップする
                if (logLine.Contains(TimelineConstants.LogSymbol) ||
                    logLine.Contains(TimelineConstants.TLXTraceLogSymbol))
                {
                    continue;
                }

                // 自動カット対象ならばスキップする
                if (LogBuffer.IsAutoIgnoreLog(logLine))
                {
                    continue;
                }

                // パーティメンバに対するHPログならばスキップする
                if (LogBuffer.IsHPLogByPartyMember(logLine))
                {
                    continue;
                }

                // ツールチップシンボル, ワールド名を除去する
                logLine = LogBuffer.RemoveTooltipSynbols(logLine);
                logLine = LogBuffer.RemoveWorldName(logLine);
                xivlog.LogLine = logLine;

                list.Add(xivlog);
            }

            return list;
        }

        private static readonly TimelineActivityModel[] EmptyActivities = new TimelineActivityModel[0];
        private static readonly TimelineTriggerModel[] EmptyTiggers = new TimelineTriggerModel[0];

        /// <summary>
        /// ログに対して判定する
        /// </summary>
        /// <param name="logs">ログ</param>
        private void DetectLogs(
            IReadOnlyList<XIVLog> logs)
        {
            var detectTime = DateTime.Now;

            var acts = default(TimelineBase[]);
            var tris = default(TimelineBase[]);
            var hides = default(TimelineBase[]);

            lock (this)
            {
                // 現在判定期間中のアクティビティ
                acts = this.Model.IsGlobalZone ? EmptyActivities : (
                    from x in this.ActivityLine
                    where
                    x.Enabled.GetValueOrDefault() &&
                    !string.IsNullOrEmpty(x.SyncKeyword) &&
                    x.SyncRegex != null &&
                    this.CurrentTime >= (x.Time + TimeSpan.FromSeconds(x.SyncOffsetStart.Value)) &&
                    this.CurrentTime < (x.Time + TimeSpan.FromSeconds(x.SyncOffsetEnd.Value)) &&
                    !x.IsSynced
                    select
                    x).ToArray();

                tris = mergeDetectors(
                    // グローバルトリガ
                    TimelineManager.Instance.GlobalTriggers.Cast<TimelineBase>(),

                    // タイムラインスコープのトリガ
                    (this.Model.IsGlobalZone ? EmptyTiggers : (
                        from x in this.Model.Triggers
                        where
                        x.Enabled.GetValueOrDefault() &&
                        !string.IsNullOrEmpty(x.SyncKeyword) &&
                        x.SyncRegex != null
                        select
                        x)),

                    // カレントサブルーチンスコープのトリガ
                    (this.CurrentSubroutine == null ? EmptyTiggers : (
                        from x in this.CurrentSubroutine.Triggers
                        where
                        x.Enabled.GetValueOrDefault() &&
                        !string.IsNullOrEmpty(x.SyncKeyword) &&
                        x.SyncRegex != null
                        select
                        x))).ToArray();

                // 非表示判定対象のイメージ通知トリガ
                hides = mergeDetectors(
                    TimelineVisualNoticeModel.GetSyncToHideList(),
                    TimelineImageNoticeModel.GetSyncToHideList())
                    .ToArray();
            }

            // 開始・終了判定のキーワードを取得する
            var keywords = ConstantKeywords.Keywords.Where(x =>
                x.Category == KewordTypes.TimelineStart ||
                x.Category == KewordTypes.End);

            // 開始・終了の判定とスタートトリガの判定行う
            // 非表示判定も合わせて実施する
            var background = Task.Run(() =>
            {
                foreach (var xivlog in logs)
                {
                    // 開始・終了のトリガの判定
                    this.DetectStartEnd(xivlog, keywords);
                    this.DetectStartTrigger(xivlog);
                    this.DetectEndTrigger(xivlog);

                    // 非表示待ち判定
                    foreach (var hide in hides)
                    {
                        this.Detect(xivlog, hide, detectTime);
                        Thread.Yield();
                    }
                }
            });

            // Activityを判定する
            var t1 = Task.Run(() =>
            {
                foreach (var xivlog in logs)
                {
                    foreach (var act in acts)
                    {
                        this.Detect(xivlog, act, detectTime);
                        Thread.Yield();
                    }
                }
            });

            // Triggerを判定する
            var t2 = Task.Run(() =>
            {
                foreach (var xivlog in logs)
                {
                    foreach (var tri in tris)
                    {
                        this.Detect(xivlog, tri, detectTime);
                        Thread.Yield();
                    }
                }
            });

            // Scriptを判定する
            var t3 = Task.Run(() =>
                TimelineScriptGlobalModel.Instance.ScriptingHost.ExecuteOnLogs(
                    this.CurrentSubroutine?.Name ?? string.Empty,
                    logs));

            // タスクの完了を待つ
            Task.WaitAll(t1, t2, t3, background);

            // 判定オブジェクトをマージするためのメソッド
            IEnumerable<TimelineBase> mergeDetectors(
                params IEnumerable<TimelineBase>[] detectorsSets)
            {
                foreach (var detectors in detectorsSets)
                {
                    foreach (var detector in detectors)
                    {
                        yield return detector;
                    }
                }
            }
        }

        // 開始と終了の判定
        private void DetectStartEnd(
            XIVLog xivlog,
            IEnumerable<AnalyzeKeyword> keywords)
        {
            var key = (
                from x in keywords
                where
                xivlog.LogLine.ContainsIgnoreCase(x.Keyword)
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
                        TimelineScriptGlobalModel.Instance.ScriptingHost.ExecuteOnWipeout();
                        WPFHelper.BeginInvoke(this.EndActivityLine);
                        break;
                }
            }
        }

        // スタートトリガを判定する
        private void DetectStartTrigger(
            XIVLog xivlog)
        {
            if (!this.isRunning)
            {
                if (this.Model.StartTriggerRegex != null)
                {
                    var match = this.Model.StartTriggerRegex.Match(xivlog.LogLine);
                    if (match.Success)
                    {
                        WPFHelper.BeginInvoke(this.StartActivityLine);
                    }
                }
            }
        }

        // エンドトリガを判定する
        private void DetectEndTrigger(
            XIVLog xivlog)
        {
            if (this.isRunning)
            {
                if (this.Model.EndTriggerRegex != null)
                {
                    var match = this.Model.EndTriggerRegex.Match(xivlog.LogLine);
                    if (match.Success)
                    {
                        WPFHelper.BeginInvoke(this.EndActivityLine);
                        PluginMainWorker.Instance.Wipeout(false);
                        TimelineController.RaiseLog($"{TimelineConstants.LogSymbol} End-of-Timeline has been detected.");
                    }
                }
            }
        }

        // トリガでログを判定する
        private void Detect(
            XIVLog xivlog,
            TimelineBase detector,
            DateTime detectTime)
        {
            switch (detector)
            {
                case TimelineActivityModel act:
                    this.DetectActivity(xivlog, act);
                    break;

                case TimelineTriggerModel tri:
                    this.DetectTrigger(xivlog, tri, detectTime);
                    break;

                case TimelineVisualNoticeModel vnotice:
                    vnotice.TryHide(xivlog.LogLine);
                    break;

                case TimelineImageNoticeModel inotice:
                    inotice.TryHide(xivlog.LogLine);
                    break;
            }
        }

        // アクティビティに対して判定する
        private bool DetectActivity(
            XIVLog xivlog,
            TimelineActivityModel act)
        {
            var match = default(System.Text.RegularExpressions.Match);

            lock (act)
            {
                if (act.IsSynced)
                {
                    return false;
                }

                match = act.TryMatch(xivlog.LogLine);
                if (match == null ||
                    !match.Success)
                {
                    return false;
                }

                act.IsSynced = true;
                act.SetExpressions(match);
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
        private bool DetectTrigger(
            XIVLog xivlog,
            TimelineTriggerModel tri,
            DateTime detectTime)
        {
            // P-Sync or HP-Syncならば対象外なので抜ける
            if (tri.IsPositionSyncAvailable || tri.IsHPSyncAvailable)
            {
                return false;
            }

            lock (tri)
            {
                var match = tri.TryMatch(xivlog.LogLine);
                if (match == null ||
                    !match.Success)
                {
                    return false;
                }

                tri.TextReplaced = match.Result(tri.Text ?? string.Empty);
                tri.NoticeReplaced = match.Result(tri.Notice ?? string.Empty);

                tri.MatchedCounter++;

                if (!tri.IsAvailableSyncCount())
                {
                    return false;
                }

                if (tri.SyncInterval != 0 &&
                    (detectTime - tri.MatchedTimestamp).TotalSeconds <= tri.SyncInterval)
                {
                    return false;
                }

                if (!tri.ExecuteExpressions(match))
                {
                    return false;
                }

                if (!tri.ExecuteScripts())
                {
                    return false;
                }

                tri.MatchedTimestamp = detectTime;

                var toNotice = tri.Clone();
                toNotice.LogSeq = xivlog.Seq;

                var durationOverride = (double?)null;
                if (RegexExtensions.TryGetDuration(match, out double d))
                {
                    durationOverride = d;
                }

                var vnotices = toNotice.VisualNoticeStatements.Where(x => x.Enabled.GetValueOrDefault());
                if (vnotices.Any())
                {
                    foreach (var vnotice in vnotices)
                    {
                        if (durationOverride.HasValue)
                        {
                            vnotice.Duration = durationOverride;
                        }

                        vnotice.Timestamp = detectTime;

                        // 自動ジョブアイコンをセットする
                        vnotice.SetJobIcon();
                    }
                }

                var inotices = toNotice.ImageNoticeStatements.Where(x => x.Enabled.GetValueOrDefault());
                if (inotices.Any())
                {
                    foreach (var inotice in inotices)
                    {
                        if (durationOverride.HasValue)
                        {
                            inotice.Duration = durationOverride;
                        }

                        inotice.Timestamp = detectTime;
                    }
                }

                // 外部ツールの起動引数を正規表現で置換する
                if (!string.IsNullOrEmpty(toNotice.Arguments))
                {
                    toNotice.Arguments = match.Result(toNotice.Arguments);
                }

                NotifyQueue.Enqueue(toNotice);
                tri.Execute();
                tri.Dump();
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
            });

            return true;
        }

        /// <summary>
        /// P-Syncトリガを判定する
        /// </summary>
        private void DetectPSyncTriggers()
        {
            // starts using 時のポジションをダンプする
            this.DumpStartsUsingPosition();

            var localDetectTime = DateTime.Now;
            var targets = default((TimelineTriggerModel Trigger, TimelineElementTypes TriggerType)[]);

            lock (this)
            {
                // P-Syncトリガを抽出する
                targets = (
                    from x in
                        TimelineManager.Instance.GlobalTriggers
                        .Concat(!this.Model.IsGlobalZone ? this.Model.Triggers : EmptyTiggers)
                        .Concat(this.CurrentSubroutine != null ? this.CurrentSubroutine.Triggers : EmptyTiggers)
                    where
                    x.IsAvailable() &&
                    (x.IsPositionSyncAvailable || x.IsHPSyncAvailable)
                    select
                    (
                        Trigger: x,
                        TriggerType: x.IsPositionSyncAvailable ?
                            TimelineElementTypes.PositionSync :
                            TimelineElementTypes.HPSync
                    )).ToArray();
            }

            if (!targets.Any())
            {
                return;
            }

            // Combatantsを取得する
            var combatants = CombatantsManager.Instance.GetCombatants();
            if (!combatants.Any())
            {
                return;
            }

            foreach (var target in targets)
            {
                switch (target.TriggerType)
                {
                    case TimelineElementTypes.PositionSync:
                        var psync = target.Trigger.PositionSyncStatements
                            .FirstOrDefault(x => x.Enabled.GetValueOrDefault());

                        if (psync != null)
                        {
                            lock (target.Trigger)
                            {
                                detectPSync(target.Trigger, psync);
                            }
                        }
                        break;

                    case TimelineElementTypes.HPSync:
                        var hpsync = target.Trigger.HPSyncStatements
                            .FirstOrDefault(x => x.Enabled.GetValueOrDefault());

                        if (hpsync != null)
                        {
                            lock (target.Trigger)
                            {
                                detectHPSync(target.Trigger, hpsync);
                            }
                        }
                        break;
                }
            }

            // P-Syncトリガに対して判定する
            void detectPSync(
                TimelineTriggerModel tri,
                TimelinePositionSyncModel psync)
            {
                if ((DateTime.Now - psync.LastSyncTimestamp).TotalSeconds <= psync.Interval)
                {
                    return;
                }

                var conditions = psync.Combatants
                    .Where(x =>
                        x.Enabled.GetValueOrDefault());

                if (!conditions.Any())
                {
                    return;
                }

                foreach (var con in conditions)
                {
                    var target = combatants.FirstOrDefault(x =>
                    {
                        var r = false;
                        var isTarget = false;

                        if (!string.IsNullOrEmpty(x.Name))
                        {
                            isTarget = con.IsMatchName(x.Name);

                            if (isTarget)
                            {
                                if (!string.IsNullOrEmpty(x.CastSkillName) &&
                                    x.IsCasting)
                                {
                                    isTarget = con.IsMatchAction(x.CastSkillName);
                                }
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(x.CastSkillName) &&
                                x.IsCasting)
                            {
                                isTarget = con.IsMatchAction(x.CastSkillName);
                            }
                        }

                        if (isTarget)
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

                var text = tri.Text ?? string.Empty;
                var notice = tri.Notice ?? string.Empty;

                var i = 1;
                foreach (var con in conditions)
                {
                    string replace(string t)
                    {
                        t = t.Replace("{name" + i + "}", con.ActualCombatant.Name);
                        t = t.Replace("{X" + i + "}", con.ActualCombatant.PosXMap.ToString("N1"));
                        t = t.Replace("{Y" + i + "}", con.ActualCombatant.PosYMap.ToString("N1"));
                        t = t.Replace("{Z" + i + "}", con.ActualCombatant.PosZMap.ToString("N1"));

                        return t;
                    }

                    text = replace(text);
                    notice = replace(notice);

                    i++;
                }

                if (tryExecuteTrigger(tri, text, notice))
                {
                    psync.LastSyncTimestamp = DateTime.Now;
                }
            }

            // HP-Syncトリガに対して判定する
            void detectHPSync(
                TimelineTriggerModel tri,
                TimelineHPSyncModel hpsync)
            {
                if (hpsync.IsSynced)
                {
                    return;
                }

                if (!hpsync.IsMatch(combatants))
                {
                    return;
                }

                var text = tri.Text ?? string.Empty;
                var notice = tri.Notice ?? string.Empty;

                if (tryExecuteTrigger(tri, text, notice))
                {
                    hpsync.IsSynced = true;
                }
            }

            // トリガを実行する
            bool tryExecuteTrigger(
                TimelineTriggerModel tri,
                string text,
                string notice)
            {
                tri.TextReplaced = text;
                tri.NoticeReplaced = notice;

                tri.MatchedCounter++;

                if (!tri.IsAvailableSyncCount())
                {
                    return false;
                }

                if (!tri.ExecuteExpressions())
                {
                    return false;
                }

                if (!tri.ExecuteScripts())
                {
                    return false;
                }

                var toNotice = tri.Clone();

                var vnotices = toNotice.VisualNoticeStatements.Where(x => x.Enabled.GetValueOrDefault());
                if (vnotices.Any())
                {
                    foreach (var vnotice in vnotices)
                    {
                        vnotice.Timestamp = localDetectTime;
                    }
                }

                var inotices = toNotice.ImageNoticeStatements.Where(x => x.Enabled.GetValueOrDefault());
                if (inotices.Any())
                {
                    foreach (var inotice in inotices)
                    {
                        inotice.Timestamp = localDetectTime;
                    }
                }

                NotifyQueue.Enqueue(toNotice);
                tri.Execute();
                tri.Dump();

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
                });

                return true;
            }
        }

        private static readonly object DumpStartsUsingLock = new object();
        private IEnumerable<uint> previouseCastingCombatantIDs;

        private void DumpStartsUsingPosition()
        {
            lock (DumpStartsUsingLock)
            {
                var castingCombatants = CombatantsManager.Instance.GetCombatants()
                    .Where(x =>
                        x.ActorType == Actor.Type.Monster &&
                        x.IsCasting)
                    .ToArray();

                foreach (var combatant in castingCombatants)
                {
                    if (this.previouseCastingCombatantIDs == null ||
                        !this.previouseCastingCombatantIDs.Contains(combatant.ID))
                    {
                        TimelineController.RaiseLog(
                            $"{TimelineConstants.LogSymbol} {combatant.Name} starts using {combatant.CastSkillName}. X={combatant.PosXMap:N2} Y={combatant.PosYMap:N2} Z={combatant.PosZMap:N2}. ID={combatant.CastBuffID:X4} duration={combatant.CastDurationMax:N1}");
                    }
                }

                this.previouseCastingCombatantIDs = castingCombatants.Select(x => x.ID).ToArray();
            }
        }

        /// <summary>
        /// ログを発生させる
        /// </summary>
        /// <param name="element"></param>
        private static void RaiseLog(
            TimelineBase element)
        {
            var log = string.Empty;

            var sub = element.Parent as TimelineSubroutineModel;

            var name = !string.IsNullOrEmpty(element.Name) ? $"name={element.Name}" : string.Empty;
            var text = string.Empty;
            var subName = sub != null ? $"sub={sub.Name}" : string.Empty;

            var parts = new[] { name, text, subName };

            switch (element)
            {
                case TimelineActivityModel act:
                    text = !string.IsNullOrEmpty(act.Text) ? $"text={act.Text}" : string.Empty; ;
                    log = $"{TimelineConstants.LogSymbol} synced-to-activity {string.Join(" ", parts.Where(x => !string.IsNullOrWhiteSpace(x)))}";
                    break;

                case TimelineTriggerModel tri:
                    text = !string.IsNullOrEmpty(tri.Text) ? $"text={tri.Text}" : string.Empty; ;
                    log = $"{TimelineConstants.LogSymbol} synced-to-trigger {string.Join(" ", parts.Where(x => !string.IsNullOrWhiteSpace(x)))}";
                    break;

                default:
                    return;
            }

            TimelineController.RaiseLog(log);
        }

        #endregion Log 関係のスレッド

        #region 時間進行関係のスレッド

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

                // 変数をクリアする
                TimelineExpressionsModel.Clear(this.CurrentZoneName);

                // 有効なActivityが存在しない？
                if (!this.Model.ExistsActivities())
                {
                    return;
                }

                this.CurrentTime = TimeSpan.Zero;
                this.PreviouseDetectTime = DateTime.Now;

                TimelineTickCallback = this.DoTimelineTick;
                TimelineTimer.Interval = TimelineDefaultInterval;
                if (!TimelineTimer.IsEnabled)
                {
                    timelineViewLock = 0;
                    TimelineTimer.Start();
                }

                this.isRunning = true;
                this.Status = TimelineStatus.Runnning;

                RaiseLog($"{TimelineConstants.LogSymbol} start-timeline name={this.Model.TimelineName}");
                this.AppLogger.Trace($"{TimelineConstants.LogSymbol} Timeline started. name={this.Model.TimelineName}");
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

                TimelineTickCallback = null;

                this.Status = TimelineStatus.Loading;

                // リソースを開放する
                TimelineExpressionsModel.Clear(this.CurrentZoneName);
                TimelineNoticeOverlay.NoticeView?.ClearNotice();
                TimelineImageNoticeModel.Collect();
                this.ClearNotifyQueue();
                GC.Collect();

                this.LoadActivityLine();
                this.Model.RefreshActivitiesView();

                this.isRunning = false;
                this.Status = TimelineStatus.Loaded;

                RaiseLog($"{TimelineConstants.LogSymbol} stop-timeline name={this.Model.TimelineName}");
                this.AppLogger.Trace($"{TimelineConstants.LogSymbol} Timeline stoped. name={this.Model.TimelineName}");
            }
        }

        private static int timelineViewLock;
        private static volatile System.Action TimelineTickCallback;
        private static TimeSpan TimelineDefaultInterval => TimeSpan.FromMilliseconds(TimelineSettings.Instance.ProgressBarRefreshInterval);
        private static readonly TimeSpan TimelineIdleInterval = TimeSpan.FromSeconds(5);

        private static DispatcherTimer TimelineTimer => LazyTimelineTimer.Value;

        private static readonly Lazy<DispatcherTimer> LazyTimelineTimer = new Lazy<DispatcherTimer>(() =>
        {
            var timer = new DispatcherTimer(TimelineSettings.Instance.TimelineThreadPriority);
            timer.Tick += TimelineTimer_Tick;
            return timer;
        });

        private static void TimelineTimer_Tick(
            object sender,
            EventArgs e)
        {
            if (Interlocked.CompareExchange(ref timelineViewLock, 1, 0) < 1)
            {
                var interval = TimelineDefaultInterval;

                try
                {
                    if (!TimelineSettings.Instance.Enabled)
                    {
                        return;
                    }

                    if (TimelineTickCallback == null)
                    {
                        interval = TimelineIdleInterval;
                        return;
                    }

                    TimelineTickCallback.Invoke();
                }
                finally
                {
                    if (TimelineTimer.Interval != interval)
                    {
                        TimelineTimer.Interval = interval;
                    }

                    Interlocked.Exchange(ref timelineViewLock, 0);
                }
            }
        }

        private void DoTimelineTick()
        {
            try
            {
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
                    $"{TimelineConstants.LogSymbol} Error Timeline ticker. name={this.Model.TimelineName}, zone={this.Model.Zone}, file={this.Model.SourceFile}");
            }
        }

        private DateTime lastTimelineRefreshTimestamp = DateTime.MinValue;

        private async void RefreshActivityLine()
        {
            if (this.CurrentTime == TimeSpan.Zero)
            {
                return;
            }

            // 現在の時間を更新する
            TimelineActivityModel.CurrentTime = this.CurrentTime;

            // タイムライン進行の時間が経っていない？
            if ((DateTime.Now - this.lastTimelineRefreshTimestamp).TotalMilliseconds
                < TimelineSettings.Instance.TimelineRefreshInterval)
            {
                // プログレスバーだけ更新して抜ける
                this.RefreshProgress();
                return;
            }

            this.lastTimelineRefreshTimestamp = DateTime.Now;

            var currentActivityLine = this.ActivityLine.ToArray();

            // 通知を判定する
            await Task.Run(() =>
            {
                var toNotify = (
                from x in currentActivityLine
                where
                !x.IsNotified &&
                x.Time + TimeSpan.FromSeconds(x.NoticeOffset.Value) <= this.CurrentTime
                select
                x).ToArray();

                // 通知キューを登録する
                var now = DateTime.Now;
                foreach (var act in toNotify)
                {
                    act.IsNotified = true;

                    if (!act.PredicateExpressions(act.SyncMatch))
                    {
                        continue;
                    }

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

                    NotifyQueue.Enqueue(act);
                }
            });

            // カウントアップ後の消去までの猶予時間
            // 1秒 - リフレッシュレートの補正値
            var timeToHide =
                TimeSpan.FromSeconds(1) -
                TimeSpan.FromMilliseconds(TimelineSettings.Instance.TimelineRefreshInterval / 2);

            if (timeToHide < TimeSpan.Zero)
            {
                timeToHide = TimeSpan.Zero;
            }

            // 表示を終了させる
            var toDoneTop = await Task.Run(() => (
                from x in currentActivityLine
                where
                !x.IsDone &&
                x.Time <= this.CurrentTime - timeToHide
                orderby
                x.Seq descending
                select
                x).FirstOrDefault());

            if (toDoneTop != null)
            {
                await Task.Run(() =>
                {
                    foreach (var act in this.ActivityLine
                        .Where(x =>
                            !x.IsDone &&
                            x.Seq <= toDoneTop.Seq))
                    {
                        act.IsDone = true;

                        if (act.PredicateExpressions(act.SyncMatch))
                        {
                            act.SetExpressions(act.SyncMatch);
                            act.ExecuteScripts();
                            act.Execute();
                            act.Dump();
                        }
                    }
                });
            }

            // Activeなアクティビティを決める
            var active = await Task.Run(() => (
                from x in currentActivityLine
                where
                !x.IsActive &&
                !x.IsDone &&
                x.Time <= this.CurrentTime &&
                x.PredicateExpressions(x.SyncMatch)
                orderby
                x.Seq descending
                select
                x).FirstOrDefault());

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
            if (this.RefreshActivityLineVisibility())
            {
                this.Model.RefreshActivitiesView();
            }
        }

        /// <summary>
        /// プログレスバーの進捗状況だけ更新する
        /// </summary>
        public async void RefreshProgress()
        {
            var toRefresh = await Task.Run(() => (
                from x in this.ActivityLine
                where
                x.Enabled.GetValueOrDefault() &&
                !string.IsNullOrEmpty(x.Text) &&
                x.IsVisible &&
                x.PredicateExpressions(x.SyncMatch)
                select
                x).ToArray());

            foreach (var x in toRefresh)
            {
                x.RefreshProgress();
                Thread.Yield();
            }
        }

        /// <summary>
        /// アクティビティラインの表示を更新する
        /// </summary>
        /// <returns>
        /// is changed?</returns>
        public bool RefreshActivityLineVisibility()
        {
            var result = false;

            var maxTime = this.CurrentTime.Add(TimeSpan.FromSeconds(
                TimelineSettings.Instance.ShowActivitiesTime));

            var toShow = (
                from x in this.ActivityLine
                where
                x.Enabled.GetValueOrDefault() &&
                !string.IsNullOrEmpty(x.Text)
                orderby
                x.Seq ascending
                select
                x).ToArray();

            var count = 0;
            foreach (var x in toShow)
            {
                if (count < TimelineSettings.Instance.ShowActivitiesCount &&
                    !x.IsDone &&
                    x.PredicateExpressions(x.SyncMatch) &&
                    x.Time <= maxTime)
                {
                    x.RefreshProgress();

                    if (count == 0)
                    {
                        var sub = x.Parent as TimelineSubroutineModel;
                        if (this.CurrentSubroutine != sub)
                        {
                            this.CurrentSubroutine = sub;
                            Task.Run(() => TimelineScriptGlobalModel.Instance.ScriptingHost.ExecuteOnSub(sub.Name));
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

                    if (!x.IsVisible)
                    {
                        x.IsVisible = true;
                        result = true;
                    }

                    count++;
                }
                else
                {
                    if (x.IsVisible)
                    {
                        x.IsVisible = false;
                        result = true;
                    }
                }
            }

            return result;
        }

        #endregion 時間進行関係のスレッド
    }
}
