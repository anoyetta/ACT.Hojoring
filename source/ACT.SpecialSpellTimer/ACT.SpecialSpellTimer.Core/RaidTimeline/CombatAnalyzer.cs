using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Utility;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.XIVHelper;
using Microsoft.VisualBasic.FileIO;
using NPOI.SS.UserModel;
using Sharlayan.Core.Enums;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    /// <summary>
    /// コンバットアナライザ
    /// </summary>
    public class CombatAnalyzer
    {
        #region Singleton

        /// <summary>
        /// シングルトンInstance
        /// </summary>
        private static CombatAnalyzer instance;

        /// <summary>
        /// シングルトンInstance
        /// </summary>
        public static CombatAnalyzer Instance =>
            instance ?? (instance = new CombatAnalyzer());

        #endregion Singleton

        public CombatAnalyzer()
        {
            this.CurrentCombatLogList = new ObservableCollection<CombatLog>();
            BindingOperations.EnableCollectionSynchronization(this.CurrentCombatLogList, new object());
        }

        /// <summary>
        /// ログ一時バッファ
        /// </summary>
        private readonly ConcurrentQueue<LogLineEventArgs> logInfoQueue = new ConcurrentQueue<LogLineEventArgs>();

        /// <summary>
        /// ログのID
        /// </summary>
        private long id;

        /// <summary>
        /// ログ格納スレッド
        /// </summary>
        private ThreadWorker storeLogWorker;

        /// <summary>
        /// 戦闘ログのリスト
        /// </summary>
        public ObservableCollection<CombatLog> CurrentCombatLogList
        {
            get;
            private set;
        }

        /// <summary>
        /// アクターのHP率
        /// </summary>
        private Dictionary<string, decimal> ActorHPRate
        {
            get;
            set;
        } = new Dictionary<string, decimal>();

        /// <summary>
        /// 分析を開始する
        /// </summary>
        public void Start()
        {
            this.ClearLogBuffer();

            if (Settings.Default.AutoCombatLogAnalyze)
            {
                this.StartPoller();
                ActGlobals.oFormActMain.OnLogLineRead -= this.FormActMain_OnLogLineRead;
                ActGlobals.oFormActMain.OnLogLineRead += this.FormActMain_OnLogLineRead;
                Logger.Write("Start Timeline Analyze.");
            }
        }

        /// <summary>
        /// 分析を停止する
        /// </summary>
        public void End()
        {
            ActGlobals.oFormActMain.OnLogLineRead -= this.FormActMain_OnLogLineRead;

            this.EndPoller();
            this.ClearLogBuffer();
            Logger.Write("End Timeline Analyze.");
        }

        /// <summary>
        /// ログのポーリングを開始する
        /// </summary>
        private void StartPoller()
        {
            this.ClearLogInfoQueue();
            this.inCombat = false;

            lock (this)
            {
                if (this.storeLogWorker != null)
                {
                    return;
                }

                this.storeLogWorker = new ThreadWorker(
                    this.StoreLogPoller,
                    100,
                    "CombatLog Analyer",
                    ThreadPriority.Lowest);

                this.storeLogWorker.Run();
            }
        }

        /// <summary>
        /// ログのポーリングを終了する
        /// </summary>
        private void EndPoller()
        {
            lock (this)
            {
                this.storeLogWorker?.Abort();
                this.storeLogWorker = null;
            }

            this.ClearLogInfoQueue();
        }

        /// <summary>
        /// ログバッファをクリアする
        /// </summary>
        private void ClearLogBuffer()
        {
            lock (this.CurrentCombatLogList)
            {
                this.ClearLogInfoQueue();
                this.CurrentCombatLogList.Clear();
                this.ActorHPRate.Clear();
            }
        }

        /// <summary>
        /// ログキューを消去する
        /// </summary>
        private void ClearLogInfoQueue()
        {
            while (this.logInfoQueue.TryDequeue(out LogLineEventArgs l))
            {
            }
        }

        /// <summary>
        /// ログを1行読取った
        /// </summary>
        /// <param name="isImport">Importか？</param>
        /// <param name="logInfo">ログ情報</param>
        private async void FormActMain_OnLogLineRead(
            bool isImport,
            LogLineEventArgs logInfo)
        {
            try
            {
                if (!Settings.Default.AutoCombatLogAnalyze)
                {
                    return;
                }

                await Task.Run(() => this.logInfoQueue.Enqueue(logInfo));
            }
            catch (Exception ex)
            {
                Logger.Write(
                    "catch exception at Timeline Analyzer OnLogLineRead.",
                    ex);
            }
        }

        #region PC Name

        private IList<string> partyNames = null;
        private IList<CombatantEx> combatants = null;

        private static readonly Regex PCNameRegex = new Regex(
            @"[a-zA-Z'\.]+ [a-zA-Z'\.]+",
            RegexOptions.Compiled);

        /// <summary>
        /// Combatantsを取得する
        /// </summary>
        /// <returns>Combatants</returns>
        private IList<CombatantEx> GetCombatants()
        {
            lock (this)
            {
                if (this.combatants == null)
                {
                    // プレイヤ情報とパーティリストを取得する
                    var ptlist = CombatantsManager.Instance.GetPartyList();

                    this.combatants = ptlist.Where(x => x != null).ToList();
                }
            }

            return this.combatants;
        }

        /// <summary>
        /// パーティメンバの名前リストを取得する
        /// </summary>
        /// <returns>名前リスト</returns>
        private IList<string> GetPartyMemberNames()
        {
            lock (this)
            {
                if (this.partyNames == null)
                {
                    var names = new List<string>();

                    foreach (var combatant in this.GetCombatants())
                    {
                        names.Add(combatant.Name);
                        names.Add(combatant.NameFI);
                        names.Add(combatant.NameIF);
                        names.Add(combatant.NameII);
                    }

                    names.AddRange(PCNameDictionary.Instance.GetNames());

                    this.partyNames = names;
                }
            }

            return this.partyNames;
        }

        /// <summary>
        /// ログを保存する対象のActorか？
        /// </summary>
        /// <param name="actor">アクター</param>
        /// <returns>保存対象か？</returns>
        private bool ToStoreActor(
            string actor)
        {
            if (string.IsNullOrEmpty(actor))
            {
                return true;
            }

            var names = this.GetPartyMemberNames();

            if (names != null &&
                names.Any() &&
                names.Contains(actor))
            {
                return false;
            }

            if (Settings.Default.FFXIVLocale != Locales.JA)
            {
                return true;
            }

            return !PCNameRegex.Match(actor).Success;
        }

        /// <summary>
        /// PC名をJOB名に置換える
        /// </summary>
        /// <param name="name">PC名</param>
        /// <returns>JOB名</returns>
        private string ToNameToJob(
            string name)
        {
            var jobName = name;

            var combs = this.GetCombatants();

            var com = combs.FirstOrDefault(x =>
                x?.ActorType == Actor.Type.PC &&
                (
                    x?.Name == name ||
                    x?.NameFI == name ||
                    x?.NameIF == name ||
                    x?.NameII == name
                ));

            if (com != null)
            {
                jobName = com.IsPlayer ?
                    $"[mex]" :
                    $"[{com.JobID.ToString()}]";
            }
            else
            {
                if (PCNameRegex.Match(jobName).Success)
                {
                    jobName = "[pc]";
                }
            }

            return jobName;
        }

        #endregion PC Name

        #region Store Log

        private volatile bool inCombat;
        private long no;
        private bool isImporting;

        /// <summary>
        /// ログを格納するスレッド
        /// </summary>
        private void StoreLogPoller()
        {
            if (this.logInfoQueue.IsEmpty)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                return;
            }

            var preLog = new string[3];
            var preLogIndex = 0;

            var logs = new List<LogLineEventArgs>(this.logInfoQueue.Count);

            while (this.logInfoQueue.TryDequeue(out LogLineEventArgs log))
            {
                // 直前とまったく同じ行はカットする
                if (preLog[0] == log.logLine ||
                    preLog[1] == log.logLine ||
                    preLog[2] == log.logLine)
                {
                    continue;
                }

                preLog[preLogIndex++] = log.logLine;
                if (preLogIndex >= 3)
                {
                    preLogIndex = 0;
                }

                logs.Add(log);
            }

            foreach (var log in logs)
            {
                // ダメージ系の不要なログか？
                if (XIVPluginHelper.IsDamageLog(log.logLine))
                {
                    continue;
                }

                // ツールチップシンボル, ワールド名を除去する
                log.logLine = LogBuffer.RemoveTooltipSynbols(log.logLine);
                log.logLine = LogBuffer.RemoveWorldName(log.logLine);

                this.AnalyzeLogLine(log);
                Thread.Yield();
            }
        }

        /// <summary>
        /// ログ行を分析する
        /// </summary>
        /// <param name="logLine">ログ行</param>
        private void AnalyzeLogLine(
            LogLineEventArgs logLine)
        {
            if (logLine == null)
            {
                return;
            }

            // ログを分類する
            var category = analyzeLogLine(logLine.logLine, ConstantKeywords.Keywords);
            switch (category)
            {
                case KewordTypes.Record:
                    if (this.inCombat)
                    {
                        this.StoreRecordLog(logLine);
                    }
                    break;

                case KewordTypes.Pet:
                    break;

                case KewordTypes.Cast:
                    if (this.inCombat)
                    {
                        this.StoreCastLog(logLine);
                    }
                    break;

                case KewordTypes.CastStartsUsing:
                    /*
                    starts using は準備動作とかぶるので無視する
                    if (this.inCombat)
                    {
                        this.StoreCastStartsUsingLog(log);
                    }
                    */
                    break;

                case KewordTypes.Action:
                    if (this.inCombat)
                    {
                        this.StoreActionLog(logLine);
                    }
                    break;

                case KewordTypes.Effect:
                    if (this.inCombat)
                    {
                        this.StoreEffectLog(logLine);
                    }
                    break;

                case KewordTypes.Marker:
                    if (this.inCombat)
                    {
                        this.StoreMarkerLog(logLine);
                    }
                    break;

                case KewordTypes.HPRate:
                    if (this.inCombat)
                    {
                        this.StoreHPRateLog(logLine);
                    }
                    break;

                case KewordTypes.Added:
                    if (this.inCombat)
                    {
                        this.StoreAddedLog(logLine);
                    }
                    break;

                case KewordTypes.NetworkAbility:
                case KewordTypes.NetworkAOEAbility:
                    if (this.inCombat)
                    {
                        this.StoreNewwork(logLine, category);
                    }
                    break;

                case KewordTypes.Dialogue:
                    if (this.inCombat)
                    {
                        this.StoreDialog(logLine);
                    }
                    break;

                case KewordTypes.Start:
                    this.StartCombat(logLine);
                    break;

                case KewordTypes.End:
                case KewordTypes.AnalyzeEnd:
                    this.EndCombat(logLine);
                    break;

                default:
                    break;
            }

            KewordTypes analyzeLogLine(string log, IList<AnalyzeKeyword> keywords)
            {
                var key = (
                    from x in keywords
                    where
                    log.ContainsIgnoreCase(x.Keyword)
                    select
                    x).FirstOrDefault();

                return key != null ?
                    key.Category :
                    KewordTypes.Unknown;
            }
        }

        /// <summary>
        /// 戦闘（分析）を開始する
        /// </summary>
        /// <param name="logLine">
        /// 対象のログ行</param>
        private void StartCombat(
            LogLineEventArgs logLine = null)
        {
            lock (this.CurrentCombatLogList)
            {
                if (!this.inCombat)
                {
                    if (!this.isImporting)
                    {
                        this.CurrentCombatLogList.Clear();
                        this.ActorHPRate.Clear();
                        this.partyNames = null;
                        this.combatants = null;
                        this.no = 1;
                    }

                    Logger.Write("Start Combat");

                    // 自分の座標をダンプする
                    LogBuffer.DumpPosition(true);
                }

                this.inCombat = true;
            }

            this.StoreStartCombat(logLine);
        }

        /// <summary>
        /// 戦闘（分析）を終了する
        /// </summary>
        /// <param name="logLine">
        /// 対象のログ行</param>
        private void EndCombat(
            LogLineEventArgs logLine = null)
        {
            lock (this.CurrentCombatLogList)
            {
                if (this.inCombat)
                {
                    this.inCombat = false;

                    if (logLine != null)
                    {
                        this.StoreEndCombat(logLine);
                    }

                    this.AutoSaveToSpreadsheetAsync();
                    ChatLogWorker.Instance?.Write(true);

                    Logger.Write("End Combat");
                }
            }
        }

        /// <summary>
        /// ログを格納する
        /// </summary>
        /// <param name="log">ログ</param>
        /// <param name="logLine">ログイベント引数</param>
        private void StoreLog(
            CombatLog log,
            LogLineEventArgs logLine)
        {
            var zone = logLine.detectedZone;
            zone = string.IsNullOrEmpty(zone) ?
                "UNKNOWN" :
                zone;

            lock (this.CurrentCombatLogList)
            {
                // IDを発番する
                log.ID = this.id;
                this.id++;

                // 今回の分析の連番を付与する
                log.No = this.no;
                this.no++;

                // 経過秒を求める
                var origin = this.CurrentCombatLogList.FirstOrDefault(x => x.IsOrigin);
                if (origin != null)
                {
                    var ts = log.TimeStamp - origin.TimeStamp;
                    if (ts.TotalMinutes <= 60 &&
                        ts.TotalMinutes >= -60)
                    {
                        log.TimeStampElapted = ts;
                    }
                }

                // アクター別の残HP率をセットする
                if (this.ActorHPRate.ContainsKey(log.Actor))
                {
                    log.HPRate = this.ActorHPRate[log.Actor];
                }

                if (!this.CurrentCombatLogList.Any() &&
                    log.RawWithoutTimestamp != ConstantKeywords.ImportLog)
                {
                    log.IsOrigin = true;
                }

                // ゾーンを保存する
                log.Zone = zone;

                this.CurrentCombatLogList.Add(log);
            }
        }

        /// <summary>
        /// 記録用ログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreRecordLog(
            LogLineEventArgs logInfo)
        {
            var log = new CombatLog()
            {
                TimeStamp = logInfo.detectedTime,
                Raw = logInfo.logLine,
                LogType = LogTypes.Unknown
            };

            this.StoreLog(log, logInfo);
        }

        /// <summary>
        /// アクションログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreActionLog(
            LogLineEventArgs logInfo)
        {
            var match = ConstantKeywords.ActionRegex.Match(logInfo.logLine);
            if (!match.Success)
            {
                return;
            }

            var log = new CombatLog()
            {
                TimeStamp = logInfo.detectedTime,
                Raw = logInfo.logLine,
                Actor = match.Groups["actor"].ToString(),
                Activity = $"{match.Groups["skill"].ToString()}",
                Skill = match.Groups["skill"].ToString(),
                LogType = LogTypes.Action
            };

            log.Text = log.Skill;
            log.SyncKeyword = log.RawWithoutTimestamp.Substring(8);

            if (this.ToStoreActor(log.Actor))
            {
                this.StoreLog(log, logInfo);
            }
        }

        /// <summary>
        /// Addedのログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreAddedLog(
            LogLineEventArgs logInfo)
        {
            var match = ConstantKeywords.AddedRegex.Match(logInfo.logLine);
            if (!match.Success)
            {
                return;
            }

            var log = new CombatLog()
            {
                TimeStamp = logInfo.detectedTime,
                Raw = logInfo.logLine,
                Actor = match.Groups["actor"].ToString(),
                Activity = $"Added",
                LogType = LogTypes.Added,
            };

            log.Text = $"Add {log.Actor}";
            log.SyncKeyword = log.RawWithoutTimestamp.Substring(0, log.RawWithoutTimestamp.IndexOf('.'));

            if (this.ToStoreActor(log.Actor))
            {
                this.StoreLog(log, logInfo);
            }
        }

        /// <summary>
        /// キャストログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreCastLog(
            LogLineEventArgs logInfo)
        {
            var match = ConstantKeywords.CastRegex.Match(logInfo.logLine);
            if (!match.Success)
            {
                match = ConstantKeywords.StartsUsingRegex.Match(logInfo.logLine);
                if (!match.Success)
                {
                    return;
                }
            }

            var log = new CombatLog()
            {
                TimeStamp = logInfo.detectedTime,
                Raw = logInfo.logLine,
                Actor = match.Groups["actor"].ToString(),
                Activity = $"{match.Groups["skill"].ToString()} Start",
                Skill = match.Groups["skill"].ToString(),
                LogType = LogTypes.CastStart
            };

            log.Text = log.Skill;
            log.SyncKeyword = log.RawWithoutTimestamp.Substring(8);

            if (this.ToStoreActor(log.Actor))
            {
                this.StoreLog(log, logInfo);
            }
        }

        /// <summary>
        /// キャストログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreCastStartsUsingLog(
            LogLineEventArgs logInfo)
        {
            var match = ConstantKeywords.StartsUsingRegex.Match(logInfo.logLine);
            if (!match.Success)
            {
                return;
            }

            var log = new CombatLog()
            {
                TimeStamp = logInfo.detectedTime,
                Raw = logInfo.logLine,
                Actor = match.Groups["actor"].ToString(),
                Activity = $"starts using {match.Groups["skill"].ToString()}",
                Skill = match.Groups["skill"].ToString(),
                LogType = LogTypes.CastStart
            };

            log.Text = log.Skill;
            log.SyncKeyword = log.RawWithoutTimestamp.Substring(6);

            if (this.ToStoreActor(log.Actor))
            {
                this.StoreLog(log, logInfo);
            }
        }

        /// <summary>
        /// Effectログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreEffectLog(
            LogLineEventArgs logInfo)
        {
            var match = ConstantKeywords.EffectRegex.Match(logInfo.logLine);
            if (!match.Success)
            {
                return;
            }

            var victim = match.Groups["victim"].ToString();
            var victimJobName = this.ToNameToJob(victim);

            var effect = match.Groups["effect"].ToString();
            var actor = match.Groups["actor"].ToString();
            var duration = match.Groups["duration"].ToString();

            var log = new CombatLog()
            {
                TimeStamp = logInfo.detectedTime,
                Raw = logInfo.logLine
                    .Replace(victim, victimJobName),
                Actor = actor,
                Activity = $"effect {effect}",
                LogType = LogTypes.Effect
            };

            log.Text = log.Activity;
            log.SyncKeyword = log.RawWithoutTimestamp;

            if (victim != actor)
            {
                if (this.ToStoreActor(log.Actor))
                {
                    this.StoreLog(log, logInfo);
                }
            }
        }

        private const string ActorIDPlaceholder = "<id8>";
        private const string PCIDPlaceholder = "<id8>";

        /// <summary>
        /// マーカーログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreMarkerLog(
            LogLineEventArgs logInfo)
        {
            var log = default(CombatLog);

            var match = ConstantKeywords.MarkerRegex.Match(logInfo.logLine);
            if (match.Success)
            {
                // ログなしマーカ
                var id = match.Groups["id"].ToString();
                var target = match.Groups["target"].ToString();
                var targetJobName = this.ToNameToJob(target);

                log = new CombatLog()
                {
                    TimeStamp = logInfo.detectedTime,
                    Raw = logInfo.logLine
                        .Replace(id, PCIDPlaceholder)
                        .Replace(target, targetJobName),
                    Activity = $"Marker:{match.Groups["type"].ToString()}",
                    LogType = LogTypes.Marker
                };
            }
            else
            {
                // マーキング
                match = ConstantKeywords.MarkingRegex.Match(logInfo.logLine);
                if (!match.Success)
                {
                    return;
                }

                var target = match.Groups["target"].ToString();
                var targetJobName = this.ToNameToJob(target);

                log = new CombatLog()
                {
                    TimeStamp = logInfo.detectedTime,
                    Raw = logInfo.logLine
                        .Replace(target, targetJobName),
                    Activity = $"Marking",
                    LogType = LogTypes.Marker
                };
            }

            if (log != null)
            {
                log.Text = log.Activity;
                log.SyncKeyword = log.RawWithoutTimestamp;

                if (this.ToStoreActor(log.Actor))
                {
                    this.StoreLog(log, logInfo);
                }
            }
        }

        /// <summary>
        /// 格納対象外とするアクション名
        /// </summary>
        private static readonly string[] IgnoreNewworkActions = new string[]
        {
            "攻撃",
            "スプリント",
            "Attack",
            "Sprint",
        };

        /// <summary>
        /// ネットワークログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        /// <param name="keywordType">キーワードのタイプ</param>
        private void StoreNewwork(
            LogLineEventArgs logInfo,
            KewordTypes keywordType)
        {
            var log = default(CombatLog);

            var targetLogLine = logInfo.logLine.Substring(15);
            var match = keywordType == KewordTypes.NetworkAbility ?
                ConstantKeywords.NetworkAbility.Match(targetLogLine) :
                ConstantKeywords.NetworkAOEAbility.Match(targetLogLine);

            if (match.Success)
            {
                var actorID = match.Groups["id"].ToString();
                var victimID = match.Groups["victim_id"].ToString();
                var actor = match.Groups["actor"].ToString();
                var action = match.Groups["skill"].ToString();
                var victim = match.Groups["victim"].ToString();
                var victimJobName = this.ToNameToJob(victim);

                if (IgnoreNewworkActions.Any(x =>
                    string.Equals(x, action, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                var raw = logInfo.logLine.Substring(0, 15) + match.Value;
                raw = raw
                    .Replace(actorID, ActorIDPlaceholder)
                    .Replace(victimID, PCIDPlaceholder);

                if (!string.IsNullOrEmpty(victim))
                {
                    raw = raw.Replace(victim, victimJobName);
                }

                log = new CombatLog()
                {
                    TimeStamp = logInfo.detectedTime,
                    Raw = raw,
                    Actor = actor,
                    Skill = action,
                    Activity = keywordType == KewordTypes.NetworkAbility ?
                        $"{action} Sign" :
                        $"{action} Sign-AOE",
                    LogType = keywordType == KewordTypes.NetworkAbility ?
                        LogTypes.NetworkAbility :
                        LogTypes.NetworkAOEAbility,
                };

                if (!this.ToStoreActor(log.Actor))
                {
                    return;
                }
            }

            if (log != null)
            {
                if (this.CurrentCombatLogList.Any(x =>
                    Math.Abs((x.TimeStamp - log.TimeStamp).TotalSeconds) <= 1.0 &&
                    x.RawWithoutTimestamp == log.RawWithoutTimestamp))
                {
                    return;
                }

                log.Text = log.Activity;
                log.SyncKeyword = log.RawWithoutTimestamp;

                this.StoreLog(log, logInfo);
            }
        }

        /// <summary>
        /// HP率のログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreHPRateLog(
            LogLineEventArgs logInfo)
        {
            var match = ConstantKeywords.HPRateRegex.Match(logInfo.logLine);
            if (!match.Success)
            {
                return;
            }

            var actor = match.Groups["actor"].ToString();

            if (this.ToStoreActor(actor))
            {
                decimal hprate;
                if (!decimal.TryParse(match.Groups["hprate"].ToString(), out hprate))
                {
                    hprate = 0m;
                }

                this.ActorHPRate[actor] = hprate;
            }
        }

        /// <summary>
        /// セリフを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreDialog(
            LogLineEventArgs logInfo)
        {
            var match = ConstantKeywords.DialogRegex.Match(logInfo.logLine);
            if (!match.Success)
            {
                return;
            }

            var dialog = match.Groups["dialog"].ToString();

            var isSystem = logInfo.logLine.Contains(":0839");

            var log = new CombatLog()
            {
                TimeStamp = logInfo.detectedTime,
                Raw = logInfo.logLine,
                Actor = string.Empty,
                Activity = isSystem ? "System" : "Dialog",
                LogType = LogTypes.Dialog
            };

            log.Text = null;
            log.SyncKeyword = log.RawWithoutTimestamp.Substring(8);

            this.StoreLog(log, logInfo);
        }

        /// <summary>
        /// 戦闘開始を格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreStartCombat(
            LogLineEventArgs logInfo)
        {
            var match = ConstantKeywords.CombatStartRegex.Match(logInfo.logLine);
            if (!match.Success)
            {
                return;
            }

            var discription = match.Groups["discription"].ToString();

            var log = new CombatLog()
            {
                TimeStamp = logInfo.detectedTime,
                Raw = logInfo.logLine,
                Actor = string.Empty,
                Activity = LogTypes.CombatStart.ToString(),
                LogType = LogTypes.CombatStart
            };

            this.StoreLog(log, logInfo);
        }

        /// <summary>
        /// 戦闘終了を格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreEndCombat(
            LogLineEventArgs logInfo)
        {
            var match = ConstantKeywords.CombatEndRegex.Match(logInfo.logLine);
            if (!match.Success)
            {
                return;
            }

            var discription = match.Groups["discription"].ToString();

            var log = new CombatLog()
            {
                TimeStamp = logInfo.detectedTime,
                Raw = logInfo.logLine,
                Actor = string.Empty,
                Activity = LogTypes.CombatEnd.ToString(),
                LogType = LogTypes.CombatEnd
            };

            this.StoreLog(log, logInfo);
        }

        #endregion Store Log

        /// <summary>
        /// ログに基点をセットする
        /// </summary>
        /// <param name="logs">logs</param>
        /// <param name="origin">origin log</param>
        public void SetOrigin(
            IEnumerable<CombatLog> logs,
            CombatLog origin)
        {
            foreach (var log in logs)
            {
                log.IsOrigin = false;

                var ts = log.TimeStamp - origin.TimeStamp;
                if (ts.TotalMinutes <= 60 &&
                    ts.TotalMinutes >= -60)
                {
                    log.TimeStampElapted = ts;
                }
                else
                {
                    log.TimeStampElapted = TimeSpan.Zero;
                }
            }

            origin.IsOrigin = true;
        }

        /// <summary>
        /// ログ行をインポートして解析する
        /// </summary>
        /// <param name="logLines">インポートするログ行</param>
        public void ImportLogLines(
            List<string> logLines)
        {
            try
            {
                this.isImporting = true;

                // 冒頭にインポートを示すログを発生させる
                this.AnalyzeLogLine(new LogLineEventArgs(
                    $"[00:00:00.000] {ConstantKeywords.ImportLog}",
                    0,
                    DateTime.Now,
                    string.Empty,
                    true));

                var now = DateTime.Now;

                // 各種初期化
                this.inCombat = false;
                this.CurrentCombatLogList.Clear();
                this.ActorHPRate.Clear();
                this.partyNames = null;
                this.combatants = null;
                this.no = 1;

                foreach (var line in logLines)
                {
                    var log = line;
                    var detectTime = default(DateTime);

                    if (log.Length < 14)
                    {
                        continue;
                    }

                    var timeAsText = log.Substring(0, 14)
                        .Replace("[", string.Empty)
                        .Replace("]", string.Empty);

                    DateTime time;
                    if (!DateTime.TryParse(timeAsText, out time))
                    {
                        continue;
                    }

                    detectTime = new DateTime(
                        now.Year,
                        now.Month,
                        now.Day,
                        time.Hour,
                        time.Minute,
                        time.Second,
                        time.Millisecond);

                    var arg = new LogLineEventArgs(
                        log,
                        0,
                        detectTime,
                        string.Empty,
                        true);

                    this.AnalyzeLogLine(arg);
                }

                var startCombat = this.CurrentCombatLogList.FirstOrDefault(x => x.Raw.Contains(ConstantKeywords.CombatStartNow));
                if (startCombat != null)
                {
                    this.SetOrigin(this.CurrentCombatLogList, startCombat);
                }
            }
            finally
            {
                this.isImporting = false;
            }
        }

        /// <summary>
        /// ログ行をインポートして解析する
        /// </summary>
        /// <param name="file">対象のファイル</param>
        public void ImportLogLinesFromCSV(
            string file)
        {
            if (!File.Exists(file))
            {
                return;
            }

            try
            {
                this.isImporting = true;

                // 冒頭にインポートを示すログを発生させる
                this.AnalyzeLogLine(new LogLineEventArgs(
                    $"[00:00:00.000] {ConstantKeywords.ImportLog}",
                    0,
                    DateTime.Now,
                    string.Empty,
                    true));

                // 各種初期化
                this.inCombat = false;
                this.CurrentCombatLogList.Clear();
                this.ActorHPRate.Clear();
                this.partyNames = null;
                this.combatants = null;
                this.no = 1;

                var preLog = new string[3];
                var preLogIndex = 0;

                using (var reader = new StreamReader(
                    new FileStream(
                        file,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite),
                    new UTF8Encoding(false)))
                using (var parser = new TextFieldParser(reader)
                {
                    TextFieldType = FieldType.Delimited,
                    Delimiters = new[] { "," },
                    HasFieldsEnclosedInQuotes = true,
                })
                {
                    while (!parser.EndOfData)
                    {
                        Thread.Yield();

                        var fields = default(string[]);

                        try
                        {
                            fields = parser.ReadFields();
                        }
                        catch
                        {
                            continue;
                        }

                        if (fields.Length < 6)
                        {
                            continue;
                        }

                        var detectTime = DateTime.Parse(fields[1]);
                        var log = fields[4];
                        var zone = fields[5];

                        // 直前とまったく同じ行はカットする
                        if (preLog[0] == log ||
                            preLog[1] == log ||
                            preLog[2] == log)
                        {
                            continue;
                        }

                        preLog[preLogIndex++] = log;
                        if (preLogIndex >= 3)
                        {
                            preLogIndex = 0;
                        }

                        // 無効なログ？
                        // ログ種別だけのゴミ？, 不要なログキーワード？, TLシンボルあり？, ダメージ系ログ？
                        if (log.Length <= 3 ||
                            log.Contains(TimelineConstants.LogSymbol) ||
                            XIVPluginHelper.IsDamageLog(log))
                        {
                            continue;
                        }

                        // ツールチップシンボル, ワールド名を除去する
                        log = LogBuffer.RemoveTooltipSynbols(log);
                        log = LogBuffer.RemoveWorldName(log);

                        // タイムスタンプを付与し直す
                        log = $"[{detectTime.ToString("HH:mm:ss.fff")}] {log}";

                        var arg = new LogLineEventArgs(
                            log,
                            0,
                            detectTime,
                            zone,
                            true);

                        this.AnalyzeLogLine(arg);
                    }
                }

                var startCombat = this.CurrentCombatLogList.FirstOrDefault(x => x.Raw.Contains(ConstantKeywords.CombatStartNow));
                if (startCombat != null)
                {
                    this.SetOrigin(this.CurrentCombatLogList, startCombat);
                }
            }
            finally
            {
                this.isImporting = false;
            }
        }

        private async void AutoSaveToSpreadsheetAsync() =>
            await Task.Run(() => this.AutoSaveToSpreadsheet());

        private void AutoSaveToSpreadsheet()
        {
            if (!Settings.Default.AutoCombatLogSave ||
                string.IsNullOrEmpty(Settings.Default.CombatLogSaveDirectory))
            {
                return;
            }

            var logs = default(IList<CombatLog>);
            lock (this.CurrentCombatLogList)
            {
                logs = this.CurrentCombatLogList.ToArray();
            }

            if (!logs.Any())
            {
                return;
            }

            var startCombat = logs.FirstOrDefault(x => x.Raw.Contains(ConstantKeywords.CombatStartNow));
            if (startCombat != null)
            {
                this.SetOrigin(logs, startCombat);
            }

            var timeStamp = logs.Last().TimeStamp;
            var zone = startCombat != null ?
                startCombat.Zone :
                logs.First().Zone;

            zone = zone.Replace(" ", "_");
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                zone = zone.Replace(c, '_');
            }

            var xlsx = $"{timeStamp.ToString("yyyy-MM-dd_HHmm")}.{zone}.auto.xlsx";
            var log = $"{timeStamp.ToString("yyyy-MM-dd_HHmm")}.{zone}.auto.log";

            this.SaveToSpreadsheet(
                Path.Combine(Settings.Default.CombatLogSaveDirectory, xlsx),
                logs);

            this.SaveToTestLog(
                Path.Combine(Settings.Default.CombatLogSaveDirectory, log),
                logs);
        }

        private static readonly object SpreadsheetLocker = new object();

        public void SaveToSpreadsheet(
            string file,
            IList<CombatLog> combatLogs)
        {
            lock (SpreadsheetLocker)
            {
                var master = Path.Combine(
                    DirectoryHelper.FindSubDirectory("resources"),
                    "CombatLogBase.xlsx");

                var work = Path.Combine(
                    DirectoryHelper.FindSubDirectory("resources"),
                    "CombatLogBase_work.xlsx");

                if (!File.Exists(master))
                {
                    throw new FileNotFoundException(
                       $"CombatLog Master File Not Found. {master}");
                }

                File.Copy(master, work, true);

                var book = WorkbookFactory.Create(work);
                var sheet = book.GetSheet("CombatLog");

                try
                {
                    // セルの書式を生成する
                    var noStyle = book.CreateCellStyle();
                    var timestampStyle = book.CreateCellStyle();
                    var timeStyle = book.CreateCellStyle();
                    var textStyle = book.CreateCellStyle();
                    var perStyle = book.CreateCellStyle();
                    noStyle.DataFormat = book.CreateDataFormat().GetFormat("#,##0_ ");
                    timestampStyle.DataFormat = book.CreateDataFormat().GetFormat("yyyy-MM-dd HH:mm:ss.000");
                    timeStyle.DataFormat = book.CreateDataFormat().GetFormat("mm:ss");
                    textStyle.DataFormat = book.CreateDataFormat().GetFormat("@");
                    perStyle.DataFormat = book.CreateDataFormat().GetFormat("0.0%");

                    var now = DateTime.Now;

                    var row = 1;
                    foreach (var data in combatLogs)
                    {
                        var timeAsDateTime = new DateTime(
                            now.Year,
                            now.Month,
                            now.Day,
                            0,
                            data.TimeStampElapted.Minutes >= 0 ? data.TimeStampElapted.Minutes : 0,
                            data.TimeStampElapted.Seconds >= 0 ? data.TimeStampElapted.Seconds : 0);

                        var col = 0;

                        writeCell<long>(sheet, row, col++, data.No, noStyle);
                        writeCell<DateTime>(sheet, row, col++, timeAsDateTime, timeStyle);
                        writeCell<double>(sheet, row, col++, data.TimeStampElapted.TotalSeconds, noStyle);
                        writeCell<string>(sheet, row, col++, data.LogTypeName, textStyle);
                        writeCell<string>(sheet, row, col++, data.Actor, textStyle);
                        writeCell<decimal>(sheet, row, col++, data.HPRate, perStyle);
                        writeCell<string>(sheet, row, col++, data.Activity, textStyle);
                        writeCell<string>(sheet, row, col++, data.RawWithoutTimestamp, textStyle);
                        writeCell<string>(sheet, row, col++, data.Text, textStyle);
                        writeCell<string>(sheet, row, col++, data.SyncKeyword, textStyle);
                        writeCell<DateTime>(sheet, row, col++, data.TimeStamp, timestampStyle);
                        writeCell<string>(sheet, row, col++, data.Zone, textStyle);

                        row++;
                    }

                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }

                    FileHelper.CreateDirectory(file);

                    using (var fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        book.Write(fs);
                    }
                }
                finally
                {
                    book?.Close();
                    File.Delete(work);
                }
            }

            // セルの編集内部メソッド
            void writeCell<T>(ISheet sh, int r, int c, T v, ICellStyle style)
            {
                var rowObj = sh.GetRow(r) ?? sh.CreateRow(r);
                var cellObj = rowObj.GetCell(c) ?? rowObj.CreateCell(c);

                switch (Type.GetTypeCode(typeof(T)))
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        cellObj.SetCellValue(Convert.ToDouble(v));
                        cellObj.CellStyle = style;
                        break;

                    case TypeCode.DateTime:
                        cellObj.SetCellValue(Convert.ToDateTime(v));
                        cellObj.CellStyle = style;
                        break;

                    case TypeCode.String:
                        cellObj.SetCellValue(Convert.ToString(v));
                        cellObj.CellStyle = style;
                        break;
                }
            }
        }

        public void SaveToDraftTimeline(
            string file,
            IList<CombatLog> combatLogs)
        {
            if (!combatLogs.Any())
            {
                return;
            }

            var outputTypes = new[]
            {
                LogTypes.CastStart,
                LogTypes.Action,
                LogTypes.Dialog,
                LogTypes.Added,
                LogTypes.Marker,
                LogTypes.Effect,
                LogTypes.NetworkAbility,
                LogTypes.NetworkAOEAbility,
            };

            var timeline = new TimelineModel();

            timeline.Zone = combatLogs.First().Zone;
            timeline.Locale = Settings.Default.FFXIVLocale;
            timeline.TimelineName = $"{timeline.Zone} draft timeline";
            timeline.Revision = "draft";
            timeline.Description =
                "自動生成によるドラフト版タイムラインです。タイムライン定義の作成にご活用ください。" + Environment.NewLine +
                "なお未編集のままで運用できるようには設計されていません。";
            timeline.Author = Environment.UserName;
            timeline.License = TimelineModel.CC_BY_SALicense;

            foreach (var log in combatLogs.Where(x =>
                outputTypes.Contains(x.LogType)))
            {
                var a = new TimelineActivityModel()
                {
                    Time = log.TimeStampElapted,
                    Text = log.Text,
                    SyncKeyword = log.SyncKeyword,
                };

                switch (log.LogType)
                {
                    case LogTypes.CastStart:
                        a.Notice = $"次は、{log.Skill}。";
                        break;

                    case LogTypes.Action:
                        // 構えのないアクションか？
                        if (!combatLogs.Any(x =>
                            x.ID < log.ID &&
                            x.Skill == log.Skill &&
                            x.LogType == LogTypes.CastStart &&
                            (log.TimeStamp - x.TimeStamp).TotalSeconds <= 12))
                        {
                            a.Notice = $"次は、{log.Skill}。";
                        }

                        break;

                    case LogTypes.Added:
                        a.Notice = $"次は、{log.Actor}。";

                        if (timeline.Activities.Any(x =>
                            x.Time == a.Time &&
                            x.Text == a.Text &&
                            x.SyncKeyword == a.SyncKeyword))
                        {
                            continue;
                        }

                        break;

                    case LogTypes.Marker:
                    case LogTypes.Effect:
                        a.Enabled = false;

                        if (timeline.Activities.Any(x =>
                            x.Time == a.Time &&
                            x.Text == a.Text &&
                            x.SyncKeyword == a.SyncKeyword))
                        {
                            continue;
                        }

                        a.SyncKeyword = $"^{a.SyncKeyword}";
                        break;

                    case LogTypes.NetworkAbility:
                    case LogTypes.NetworkAOEAbility:
                        a.Enabled = false;

                        if (timeline.Activities.Any(x =>
                            x.Time == a.Time &&
                            x.Text == a.Text &&
                            x.SyncKeyword == a.SyncKeyword))
                        {
                            continue;
                        }

                        break;
                }

                a.SyncKeyword = a.SyncKeyword
                    .Replace("<id8>", "[id8]")
                    .Replace("<id4>", "[id4]");

                timeline.Add(a);
            }

            timeline.Save(file);
        }

        public void SaveToTestLog(
            string file,
            IList<CombatLog> combatLogs)
        {
            if (!combatLogs.Any())
            {
                return;
            }

            var startIndex = 0;

            var origin = combatLogs.FirstOrDefault(x => x.IsOrigin);
            if (origin != null)
            {
                startIndex = combatLogs.IndexOf(origin);
            }
            else
            {
                var startCombat = combatLogs.FirstOrDefault(x => x.Raw.Contains(ConstantKeywords.CombatStartNow));
                if (startCombat != null)
                {
                    startIndex = combatLogs.IndexOf(startCombat);
                }
            }

            FileHelper.CreateDirectory(file);

            var sb = new StringBuilder(5012);

            using (var sw = new StreamWriter(file, false, new UTF8Encoding(false)))
            {
                foreach (var log in combatLogs.Skip(startIndex))
                {
                    sb.AppendLine(log.Raw
                        .Replace("(?<pcid>.{8})", "00000000")
                        .Replace("(?<actor_id>.{8})", "00000000"));

                    if (sb.Length > 5012)
                    {
                        sw.Write(sb.ToString());
                        sb.Clear();
                    }
                }

                if (sb.Length > 0)
                {
                    sw.Write(sb.ToString());
                    sb.Clear();
                }

                sw.Flush();
            }
        }
    }

    /// <summary>
    /// 分析キーワード
    /// </summary>
    public class AnalyzeKeyword
    {
        /// <summary>
        /// キーワードの分類
        /// </summary>
        public KewordTypes Category
        {
            get;
            set;
        } = KewordTypes.Unknown;

        /// <summary>
        /// キーワード
        /// </summary>
        public string Keyword
        {
            get;
            set;
        } = string.Empty;

        /// <summary>
        /// 同一カテゴリのキーワードをまとめて生成する
        /// </summary>
        /// <param name="keywords">キーワード</param>
        /// <param name="category">カテゴリ</param>
        /// <returns>生成したキーワードオブジェクト</returns>
        public static AnalyzeKeyword[] CreateKeywords(
            string[] keywords,
            KewordTypes category)
        {
            var list = new List<AnalyzeKeyword>();

            foreach (var k in keywords)
            {
                list.Add(new AnalyzeKeyword()
                {
                    Keyword = k,
                    Category = category
                });
            }

            return list.ToArray();
        }
    }
}
