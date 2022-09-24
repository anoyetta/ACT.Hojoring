using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Common.Models;
using FFXIV_ACT_Plugin.Logfile;
using Microsoft.MinIoC;
using Microsoft.VisualBasic.FileIO;
using Sharlayan.Core;
using Sharlayan.Core.Enums;

namespace FFXIV.Framework.XIVHelper
{
    public enum OverlayType
    {
        Target = 1,
        FocusTarget,
        HoverTarget,
        TargetOfTarget
    }

    public class XIVPluginHelper
    {
#if !DEBUG
        public static readonly bool IsDebug = false;
#else
        public static readonly bool IsDebug = true;
#endif

        #region Singleton

        private static XIVPluginHelper instance;

        public static XIVPluginHelper Instance =>
            instance ?? (instance = new XIVPluginHelper());

        public static void Free() => instance = null;

        private XIVPluginHelper()
        {
        }

        #endregion Singleton

        #region Logger

        private static NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        private dynamic plugin;

        private IDataRepository DataRepository { get; set; }

        private IDataSubscription DataSubscription { get; set; }

        private Microsoft.MinIoC.Container IOCContainer { get; set; }

        public System.Action ActPluginAttachedCallback { get; set; }

        public Locales FFXIVLocale
        {
            get;
            set;
        } = Locales.JA;

        public bool IsAttached =>
            this.plugin != null &&
            this.DataRepository != null &&
            this.DataSubscription != null;

        public Process CurrentFFXIVProcess { get; private set; }

        public bool IsAvailable
        {
            get
            {
                if (ActGlobals.oFormActMain == null ||
                    this.plugin == null ||
                    this.DataRepository == null ||
                    this.DataSubscription == null ||
                    this.CurrentFFXIVProcess == null ||
                    this.CurrentFFXIVProcess.HasExited)
                {
                    return false;
                }

                return true;
            }
        }

        private void RefreshCurrentFFXIVProcess()
        {
            this.CurrentFFXIVProcess = this.DataRepository?.GetCurrentFFXIVProcess();
        }

        public Locales LanguageID => (int)(this.DataRepository?.GetSelectedLanguageID() ?? 0) switch
        {
            1 => Locales.EN,
            2 => Locales.FR,
            3 => Locales.DE,
            4 => Locales.JA,
            _ => Locales.EN,
        };

        private static readonly Dictionary<Locales, (ResourceType Buff, ResourceType Skill)> ResourcesTypeDictionary = new Dictionary<Locales, (ResourceType Buff, ResourceType Skill)>()
        {
            {  Locales.EN, (ResourceType.BuffList_EN, ResourceType.SkillList_EN) },
            {  Locales.FR, (ResourceType.BuffList_FR, ResourceType.SkillList_FR) },
            {  Locales.DE, (ResourceType.BuffList_DE, ResourceType.SkillList_DE) },
            {  Locales.JA, (ResourceType.BuffList_JP, ResourceType.SkillList_JP) },
            {  Locales.KO, (ResourceType.BuffList_EN, ResourceType.SkillList_EN) },
            {  Locales.CN, (ResourceType.BuffList_EN, ResourceType.SkillList_EN) },
            {  Locales.TW, (ResourceType.BuffList_EN, ResourceType.SkillList_EN) },
        };

        public double MemorySubscriberInterval { get; private set; }

        #region Start/End

        private ThreadWorker attachFFXIVPluginWorker;
        private ThreadWorker scanFFXIVWorker;
        private volatile bool isStarted = false;

        public async void Start(
            double pollingInteval,
            Locales ffxivLocale = Locales.JA)
        {
            lock (this)
            {
                if (this.isStarted)
                {
                    return;
                }

                this.isStarted = true;
            }

            // FFXIV.Framework.config を読み込ませる
            lock (Config.ConfigBlocker)
            {
                _ = Config.Instance;
            }

            this.FFXIVLocale = ffxivLocale;
            this.MemorySubscriberInterval = pollingInteval;

            this.attachFFXIVPluginWorker = new ThreadWorker(() =>
            {
                if (!ActGlobals.oFormActMain.InitActDone)
                {
                    return;
                }

                this.RefreshCurrentFFXIVProcess();
                this.Attach();

                if (this.plugin == null ||
                    this.DataRepository == null ||
                    this.DataSubscription == null)
                {
                    return;
                }

                if (this.IsResourcesLoaded)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    return;
                }

                this.LoadSkillList();
                this.LoadZoneList();
                this.LoadWorldList();

                this.MergeSkillList();

                this.TranslateZoneList();
            },
            5000,
            nameof(this.attachFFXIVPluginWorker),
            ThreadPriority.Lowest);

            this.scanFFXIVWorker = new ThreadWorker(() =>
            {
                if (!ActGlobals.oFormActMain.InitActDone)
                {
                    return;
                }

                this.RefreshActive();

                if (!this.IsAvailable)
                {
                    Thread.Sleep(5000);

                    if (!IsDebug)
                    {
                        CombatantsManager.Instance.Clear();
                        return;
                    }
                }

                if (SharlayanHelper.Instance.TryScanning())
                {
                    try
                    {
                        this.RefreshCombatantList();
                    }
                    finally
                    {
                        SharlayanHelper.Instance.IsScanning = false;
                    }
                }
            },
            pollingInteval * 1.05,
            nameof(this.scanFFXIVWorker),
            ThreadPriority.Lowest);

            // XIVApiのロケールを設定する
            XIVApi.Instance.FFXIVLocale = ffxivLocale;

            // sharlayanを設定する
            // Actor を取得しない
            // Party を取得しない
            SharlayanHelper.Instance.IsSkipActor = true;
            SharlayanHelper.Instance.IsSkipParty = true;

            var tasksG1 = new System.Action[]
            {
                () => XIVApi.Instance.Load(),
                () => SharlayanHelper.Instance.Start(pollingInteval),
                () => this.attachFFXIVPluginWorker.Run(),
                () => this.scanFFXIVWorker.Run(),
            };

            var tasksG2 = new System.Action[]
            {
                () => PCNameDictionary.Instance.Load(),
                () => PCOrder.Instance.Load(),
            };

            // その他リソース読み込みを開始する
            await Task.WhenAll(
                CommonHelper.InvokeTasks(tasksG1),
                CommonHelper.InvokeTasks(tasksG2));
        }

        public void End()
        {
            lock (this)
            {
                if (!this.isStarted)
                {
                    return;
                }

                this.isStarted = false;
            }

            // sharlayan を止める
            SharlayanHelper.Instance.End();

            try
            {
                this.scanFFXIVWorker?.Abort();
                this.attachFFXIVPluginWorker?.Abort();
            }
            catch (ThreadAbortException)
            {
            }

            this.UnsubscribeXIVPluginEvents();
            this.UnsubscribeParsedLogLine();
            this.ClearXIVLogBuffers();

            // PC名記録をセーブする
            PCNameDictionary.Instance.Save();
            PCNameDictionary.Free();
            PCOrder.Free();

            this.plugin = null;
            this.DataRepository = null;
            this.DataSubscription = null;
        }

        #endregion Start/End

        #region Attach FFXIV Plugin

        private bool wasAttached;

        private void Attach()
        {
            if (ActGlobals.oFormActMain == null ||
                !ActGlobals.oFormActMain.InitActDone)
            {
                return;
            }

            if (this.plugin == null)
            {
                var ffxivPlugin = (
                    from x in ActGlobals.oFormActMain.ActPlugins
                    where
                    x.pluginFile.Name.ToUpper().Contains("FFXIV_ACT_Plugin".ToUpper())
                    select
                    x.pluginObj).FirstOrDefault();

                this.plugin = ffxivPlugin;
            }

            if (this.plugin != null)
            {
                if (this.DataRepository == null)
                {
                    this.DataRepository = this.plugin.DataRepository;
                }

                if (this.DataSubscription == null)
                {
                    this.DataSubscription = this.plugin.DataSubscription;
                }

                if (this.IOCContainer == null)
                {
                    this.IOCContainer = this.plugin.GetType()
                        .GetField(
                            "_iocContainer",
                            BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(this.plugin) as Microsoft.MinIoC.Container;
                }

                if (this.IOCContainer != null)
                {
                    if (this.LogFormat == null)
                    {
                        this.LogFormat = this.IOCContainer.Resolve<ILogFormat>();
                    }

                    if (this.LogOutput == null)
                    {
                        this.LogOutput = this.IOCContainer.Resolve<ILogOutput>();
                    }
                }
            }

            if (!this.wasAttached)
            {
                if (this.plugin != null &&
                    this.DataRepository != null &&
                    this.DataSubscription != null &&
                    this.IOCContainer != null &&
                    this.LogFormat != null &&
                    this.LogOutput != null)
                {
                    this.wasAttached = true;

                    this.SubscribeXIVPluginEvents();
                    this.SubscribeParsedLogLine();

                    AppLogger.Trace("attached ffxiv plugin.");

                    this.ActPluginAttachedCallback?.Invoke();
                }
            }
        }

        public ILogFormat LogFormat { get; private set; }

        public ILogOutput LogOutput { get; private set; }

        public PrimaryPlayerDelegate OnPrimaryPlayerChanged { get; set; }

        public PartyListChangedDelegate OnPartyListChanged { get; set; }

        public ZoneChangedDelegate OnZoneChanged { get; set; }

        public delegate void PlayerJobChangedDelegate();

        public delegate void PartyJobChangedDelegate();

        public PlayerJobChangedDelegate OnPlayerJobChanged { get; set; }

        public PartyJobChangedDelegate OnPartyJobChanged { get; set; }

        private void SubscribeXIVPluginEvents()
        {
        }

        private void UnsubscribeXIVPluginEvents()
        {
        }

        private void RaisePrimaryPlayerChanged()
        {
            CombatantsManager.Instance.Clear();
            this.OnPrimaryPlayerChanged?.Invoke();
        }

        private void RaiseZoneChanged(uint zoneID, string zoneName)
        {
            if (this.CurrentFFXIVProcess == null)
            {
                this.CurrentFFXIVProcess = this.DataRepository?.GetCurrentFFXIVProcess();
            }

            CombatantsManager.Instance.Clear();
            this.OnZoneChanged?.Invoke(zoneID, zoneName);
        }

        #endregion Attach FFXIV Plugin

        #region Log Subscriber

        private readonly object LogBufferLocker = new object();

        private readonly List<(ConcurrentQueue<XIVLog> Buffer, Func<bool> IsActive)> XIVLogBuffers = new List<(ConcurrentQueue<XIVLog>, Func<bool>)>(16);

        public ConcurrentQueue<XIVLog> SubscribeXIVLog(
            Func<bool> isActiveCallback)
        {
            lock (this.LogBufferLocker)
            {
                var buffer = new ConcurrentQueue<XIVLog>();
                this.XIVLogBuffers.Add((buffer, isActiveCallback));
                return buffer;
            }
        }

        private void SubscribeParsedLogLine()
        {
            ActGlobals.oFormActMain.OnLogLineRead += this.OnLogLineRead;
        }

        private void UnsubscribeParsedLogLine()
        {
            ActGlobals.oFormActMain.OnLogLineRead -= this.OnLogLineRead;
        }

        private void ClearXIVLogBuffers()
        {
            lock (this.LogBufferLocker)
            {
                this.XIVLogBuffers.Clear();
            }
        }

        private uint sequence = 1;

        private void OnLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            if (!this.isActivationAllowed)
            {
                return;
            }

            var line = logInfo.logLine;

            // 18文字未満のログは書式エラーになるため無視する
            if (line.Length < 18)
            {
                return;
            }

            // メッセージタイプを抽出する
            var messagetype = logInfo.detectedType;

            // 255を超えるメッセージタイプは無視する
            // OverlayPluginなどが発生させる
            // ※処理するように変更
            //if (messagetype > 0xFF)
            //{
            //    return;
            //}

            // メッセージタイプの文字列を除去する
            line = LogMessageTypeExtensions.RemoveLogMessageType(messagetype, line);

            // メッセージ部分だけを抽出する
            var message = line.Substring(15);

            this.OnParsedLogLine(
                this.sequence++,
                messagetype,
                message);
        }

        private void OnParsedLogLine(
            uint sequence,
            int messagetype,
            string message)
        {
            var parsedLog = message;

            // 長さによるカット
            if (parsedLog.Length <= 3)
            {
                return;
            }

            var type = (LogMessageType)Enum.ToObject(typeof(LogMessageType), messagetype);
            switch (type)
            {
                case LogMessageType.ChatLog:
                    // 明らかに使用しないダメージ系をカットする
                    if (DamageLogPattern.IsMatch(parsedLog))
                    {
                        return;
                    }

                    break;

                default:
                    if (Config.Instance.IsFilterdLog(type))
                    {
                        return;
                    }

                    // ログの書式をパースする
                    parsedLog = LogParser.FormatLogLine(type, parsedLog);
                    break;
            }

            var currentZoneName = this.GetCurrentZoneName();

            lock (this.LogBufferLocker)
            {
                var xivlog = new XIVLog(
                    sequence,
                    messagetype,
                    parsedLog)
                {
                    Zone = currentZoneName
                };

                foreach (var container in this.XIVLogBuffers)
                {
                    if (container.IsActive())
                    {
                        container.Buffer.Enqueue(xivlog);
                    }
                }
            }

            // LPSを更新する
            this.CountLPS();
        }

        /// <summary>
        /// 設定によらず必ずカットするログのキーワード
        /// </summary>
        public static readonly string[] IgnoreLogKeywords = new[]
        {
            LogMessageType.DoTHoT.ToKeyword(),
        };

        /*
        // ダメージ系ログ
        "] 00:0aa9:",
        "] 00:0b29:",
        "] 00:1129:",
        "] 00:12a9:",
        "] 00:1329:",
        "] 00:28a9:",
        "] 00:2929:",
        "] 00:2c29:",
        "] 00:2ca9:",
        "] 00:30a9:",
        "] 00:3129:",
        "] 00:32a9:",
        "] 00:3429:",
        "] 00:34a9:",
        "] 00:3aa9:",
        "] 00:42a9:",
        "] 00:4aa9:",
        "] 00:4b29:",

        // 回復系ログ
        "] 00:08ad:",
        "] 00:092d:",
        "] 00:0c2d:",
        "] 00:0cad:",
        "] 00:10ad:",
        "] 00:112d:",
        "] 00:142d:",
        "] 00:14ad:",
        "] 00:28ad:",
        "] 00:292d:",
        "] 00:2aad:",
        "] 00:30ad:",
        "] 00:312d:",
        "] 00:412d:",
        "] 00:48ad:",
        "] 00:492d:",
        "] 00:4cad:",
        */

        /// <summary>
        /// ダメージ関係のログを示すキーワード
        /// </summary>
        /// <remarks>
        /// </remarks>
        private static readonly Regex DamageLogPattern =
            new Regex(
                $@"^{LogMessageType.ChatLog.ToHex()}:..(29|a9|2d|ad):",
                RegexOptions.Compiled |
                RegexOptions.IgnoreCase |
                RegexOptions.ExplicitCapture);

        /// <summary>
        /// ダメージ関係のログか？
        /// </summary>
        /// <param name="logLine">判定対象とするログ行</param>
        /// <returns>真/偽</returns>
        public static bool IsDamageLog(
            string logLine)
        {
            if (!logLine.StartsWith($"{LogMessageType.ChatLog.ToHex()}:"))
            {
                return false;
            }

            return DamageLogPattern.IsMatch(logLine);
        }

        private double[] lpss = new double[60];
        private int currentLpsIndex;
        private long currentLineCount;
        private Stopwatch lineCountTimer = new Stopwatch();

        public double LPS => this.GetLPS();

        private double GetLPS()
        {
            var availableLPSs = this.lpss.Where(x => x > 0);
            if (!availableLPSs.Any())
            {
                return 0;
            }

            return availableLPSs.Sum() / availableLPSs.Count();
        }

        private void CountLPS()
        {
            this.currentLineCount++;

            if (!this.lineCountTimer.IsRunning)
            {
                this.lineCountTimer.Restart();
            }

            if (this.lineCountTimer.Elapsed >= TimeSpan.FromSeconds(1))
            {
                this.lineCountTimer.Stop();

                var secounds = this.lineCountTimer.Elapsed.TotalSeconds;
                if (secounds > 0)
                {
                    var lps = this.currentLineCount / secounds;
                    if (lps > 0)
                    {
                        if (this.currentLpsIndex > this.lpss.GetUpperBound(0))
                        {
                            this.currentLpsIndex = 0;
                        }

                        this.lpss[this.currentLpsIndex] = lps;
                        this.currentLpsIndex++;
                    }
                }

                this.currentLineCount = 0;
            }
        }

        #endregion Log Subscriber

        #region Refresh Active

        public bool IsFFXIVActive
        {
            get;
            private set;
        }

        private DateTime detectedActiveTimestamp = DateTime.MinValue;

        /// <summary>
        /// FFXIVまたはACTがアクティブウィンドウか？
        /// </summary>
        private void RefreshActive()
        {
            var now = DateTime.Now;

            if ((now - this.detectedActiveTimestamp).TotalSeconds <= 5.0)
            {
                return;
            }

            try
            {
                this.detectedActiveTimestamp = now;

                // フォアグラウンドWindowのハンドルを取得する
                var hWnd = GetForegroundWindow();

                // プロセスIDに変換する
                GetWindowThreadProcessId(hWnd, out int pid);

                var ffxiv = this.CurrentFFXIVProcess;
                if (ffxiv != null &&
                    !ffxiv.HasExited &&
                    pid == ffxiv.Id)
                {
                    this.IsFFXIVActive = true;
                    return;
                }

                // メインモジュールのファイル名を取得する
                var p = Process.GetProcesses().FirstOrDefault(x => x.Id == pid);
                if (p != null)
                {
                    var fileName = Path.GetFileName(
                        p.MainModule.FileName);

                    var actFileName = Path.GetFileName(
                        Process.GetCurrentProcess().MainModule.FileName);

                    if (fileName.ToLower() == "ffxiv.exe" ||
                        fileName.ToLower() == "ffxiv_dx11.exe" ||
                        fileName.ToLower() == actFileName.ToLower())
                    {
                        this.IsFFXIVActive = true;
                    }
                    else
                    {
                        this.IsFFXIVActive = false;
                    }
                }
            }
            catch (Win32Exception)
            {
                // ignore
            }
        }

        #endregion Refresh Active

        #region Activator

        private volatile bool isActivationAllowed = true;

        private void TryActivation()
        {
            if (CombatantsManager.Instance.PartyCount < 4 &&
                !this.InCombat)
            {
                var player = CombatantsManager.Instance.Player;
                if (player != null)
                {
                    this.isActivationAllowed = EnvironmentHelper.TryActivation(
                        player.Name,
                        player.WorldName,
                        string.Empty);
                }
            }
        }

        #endregion Activator

        #region Refresh Combatants

        #region Dummy Combatants

        private static readonly Combatant DummyPlayer = new Combatant()
        {
            ID = 1,
            Name = "J'ibun T-aro",
            MaxHP = 30000,
            CurrentHP = 30000,
            MaxMP = 12000,
            CurrentMP = 12000,
            Job = (int)JobIDs.PLD,
            type = (byte)Actor.Type.PC,
        };

        private static readonly List<Combatant> DummyCombatants = new List<Combatant>()
        {
            DummyPlayer,

            new Combatant()
            {
                ID = 2,
                Name = "Warrior Jiro",
                MaxHP = 30000,
                CurrentHP = 30000,
                MaxMP = 12000,
                CurrentMP = 12000,
                Job = (int)JobIDs.WAR,
                type = (byte)Actor.Type.PC,
            },

            new Combatant()
            {
                ID = 3,
                Name = "White Hanako",
                MaxHP = 30000,
                CurrentHP = 30000,
                MaxMP = 12000,
                CurrentMP = 12000,
                Job = (int)JobIDs.WHM,
                type = (byte)Actor.Type.PC,
            },

            new Combatant()
            {
                ID = 4,
                Name = "Astro Himeko",
                MaxHP = 30000,
                CurrentHP = 30000,
                MaxMP = 12000,
                CurrentMP = 12000,
                Job = (int)JobIDs.AST,
                type = (byte)Actor.Type.PC,
            },
        };

        private readonly uint[] DummyPartyList = new uint[]
        {
            1, 2, 3, 4, 5, 6, 7, 8
        };

        #endregion Dummy Combatants

        private DateTime inCombatTimestamp = DateTime.MinValue;

        public bool InCombat { get; private set; } = false;

        public class AddedCombatantsEventArgs :
            EventArgs
        {
            public AddedCombatantsEventArgs(
                IEnumerable<CombatantEx> newCombatants)
            {
                this.NewCombatants = newCombatants.ToList();
            }

            public IReadOnlyList<CombatantEx> NewCombatants { get; private set; }
        }

        public event EventHandler<AddedCombatantsEventArgs> AddedCombatants;

        private void OnAddedCombatants(
            AddedCombatantsEventArgs e) => this?.AddedCombatants?.Invoke(this, e);

        public void RefreshCombatantList()
        {
            if (!this.IsAvailable)
            {
#if true
                if (IsDebug)
                {
                    this.TryActivation();
                    /*
                    CombatantsManager.Instance.Refresh(DummyCombatants, IsDebug);
                    raiseFirstCombatants();
                    */
                }
#endif

                return;
            }

            var now = DateTime.Now;
            if ((now - this.inCombatTimestamp).TotalSeconds >= 1.0)
            {
                this.inCombatTimestamp = now;

                var party = CombatantsManager.Instance.GetPartyList();
                this.RefreshInCombat(party);
                this.RefreshBoss(party);
                this.DetectConditionChanges(party);
            }

            var combatants = this.DataRepository.GetCombatantList();
            var addeds = CombatantsManager.Instance.Refresh(combatants);

            if (addeds.Any())
            {
                this.AddedCombatants?.Invoke(
                    this,
                    new AddedCombatantsEventArgs(addeds));
            }

            if (!this.isActivationAllowed)
            {
                CombatantsManager.Instance.Clear();
            }
        }

        private void RefreshInCombat(
            IEnumerable<CombatantEx> party)
        {
            var result = false;

            if (!Config.Instance.IsEnabledSharlayan ||
                Config.Instance.IsSimplifiedInCombat ||
                SharlayanHelper.Instance.CurrentPlayer == null)
            {
                result = refreshInCombatByParty();
            }
            else
            {
                result = SharlayanHelper.Instance.CurrentPlayer?.InCombat ?? false;
            }

            this.InCombat = result;

            bool refreshInCombatByParty()
            {
                const uint MaxMP = 10000;

                var r = ActGlobals.oFormActMain?.InCombat ?? false;

                if (!r)
                {
                    var player = CombatantsManager.Instance.Player;
                    if (player != null)
                    {
                        r = player.CurrentHP != player.MaxHP ||
                            player.CurrentMP != MaxMP;
                    }
                }

                if (!r)
                {
                    if (party != null &&
                        party.Any())
                    {
                        r = (
                            from x in party
                            where
                            x.CurrentHP != x.MaxHP ||
                            x.CurrentMP != MaxMP
                            select
                            x).Any();
                    }
                }

                return r;
            }
        }

        private string previousZoneName = string.Empty;
        private string previousPlayerName = string.Empty;
        private JobIDs previousPlayerJobID = JobIDs.Unknown;
        private Dictionary<uint, JobIDs> previousPartyJobList = new Dictionary<uint, JobIDs>(8);

        private void DetectConditionChanges(
            IEnumerable<CombatantEx> party)
        {
            var currentPlayer = CombatantsManager.Instance.Player;
            var currentZoneName = this.GetCurrentZoneName();

            if (currentPlayer != null)
            {
                // プレイヤーが異なっているか？
                // TBD XIVプラグインバグ対応
                if (this.previousPlayerName != currentPlayer.Name)
                {
                    this.previousPlayerName = currentPlayer.Name;
                    this.RaisePrimaryPlayerChanged();

                    // 再アクティベーション
                    this.TryActivation();

                    // プレイヤーチェンジならば抜ける
                    return;
                }

                // プレイヤーのジョブが異なっているか？
                if (this.previousPlayerJobID != currentPlayer.JobID)
                {
                    this.previousPlayerJobID = currentPlayer.JobID;
                    this.OnPlayerJobChanged?.Invoke();
                }
            }

            // ゾーン名が異なっているか？
            // TBD XIVプラグインバグ対応
            if (this.previousZoneName != currentZoneName)
            {
                this.previousZoneName = currentZoneName;
                this.RaiseZoneChanged(
                    (uint)this.GetCurrentZoneID(),
                    currentZoneName);

                // 再アクティベーション
                this.TryActivation();
            }

            var partyIDs = party
                .ToDictionary(x => x.ID, x => x.JobID);

            if (this.previousPartyJobList.Count != partyIDs.Count)
            {
                this.OnPartyListChanged?.Invoke(
                    new ReadOnlyCollection<uint>(partyIDs.Select(x => x.Key).ToList()),
                    partyIDs.Count);
            }
            else
            {
                var preIDs = this.previousPartyJobList.Select(x => x.Key).ToArray();
                var nowIDs = partyIDs.Select(x => x.Key).ToArray();

                if (preIDs.Any(x => !nowIDs.Contains(x)) ||
                    nowIDs.Any(x => !preIDs.Contains(x)))
                {
                    // パーティが変わっているか？
                    // TBD XIVプラグインバグ対応
                    this.OnPartyListChanged?.Invoke(
                        new ReadOnlyCollection<uint>(partyIDs.Select(x => x.Key).ToList()),
                        partyIDs.Count);
                }
                else
                {
                    // パーティのジョブが異なっているか？
                    var isPartyJobChanged = false;
                    foreach (var pc in partyIDs)
                    {
                        var currentJobID = pc.Value;
                        if (this.previousPartyJobList.ContainsKey(pc.Key))
                        {
                            var previousJobID = this.previousPartyJobList[pc.Key];
                            if (currentJobID != previousJobID)
                            {
                                isPartyJobChanged = true;
                                break;
                            }
                        }
                    }

                    if (isPartyJobChanged)
                    {
                        this.OnPartyJobChanged?.Invoke();
                    }
                }
            }

            this.previousPartyJobList = partyIDs;
        }

        #endregion Refresh Combatants

        #region Get Targets

        private static readonly object BossLock = new object();
        private CombatantEx currentBoss;
        private string currentBossZoneName;

        public Func<double> GetBossHPThresholdCallback { get; set; }

        public Func<bool> GetAvailableBossCallback { get; set; }

        private void RefreshBoss(
            IEnumerable<CombatantEx> party)
        {
            if (!this.IsAvailable ||
                this.GetBossHPThresholdCallback == null ||
                this.GetAvailableBossCallback == null ||
                !this.GetAvailableBossCallback.Invoke())
            {
                return;
            }

            lock (BossLock)
            {
                var combatants = CombatantsManager.Instance.GetCombatants()
                    .Where(x => x.ActorType == Actor.Type.Monster);

                if (party == null ||
                    combatants == null ||
                    party.Count() < 1 ||
                    combatants.Count() < 1)
                {
                    this.currentBoss = null;
                    return;
                }

                var currentZoneName = this.GetCurrentZoneName();
                if (string.IsNullOrEmpty(currentZoneName))
                {
                    this.currentBoss = null;
                    return;
                }
                else
                {
                    if (this.currentBoss != null)
                    {
                        if (this.currentBoss.ActorType != Actor.Type.Monster ||
                            this.currentBossZoneName != currentZoneName)
                        {
                            this.currentBoss = null;
                            this.currentBossZoneName = currentZoneName;
                            return;
                        }
                    }
                }

                // パーティのHP平均値を算出する
                var players = party.Where(x => x.ActorType == Actor.Type.PC);
                if (!players.Any())
                {
                    this.currentBoss = null;
                    return;
                }

                var avg = players.Average(x => x.MaxHP);
                var thresholdRatio = this.GetBossHPThresholdCallback?.Invoke() ?? 140.0d;
                var threshold = avg * thresholdRatio;
                if (threshold <= 0d)
                {
                    this.currentBoss = null;
                    return;
                }

                // BOSSを検出する
                var boss = (
                    from x in combatants
                    where
                    (!x.Name?.Contains("Typeid") ?? false) &&
                    x.MaxHP >= threshold &&
                    x.ActorType == Actor.Type.Monster &&
                    x.CurrentHP > 0
                    orderby
                    x.Level descending,
                    (x.MaxHP != x.CurrentHP ? 0 : 1) ascending,
                    x.MaxHP descending,
                    x.Heading != 0 ? 0 : 1 ascending,
                    (x.PosX + x.PosY + x.PosZ) != 0 ? 0 : 1 ascending,
                    x.ID descending
                    select
                    x).FirstOrDefault();

                if (boss != null)
                {
                    if (this.currentBoss == null ||
                        this.currentBoss.ID != boss.ID)
                    {
                        var ratio =
                            avg != 0 ?
                            boss.MaxHP / avg :
                            boss.MaxHP;

                        var player = combatants.FirstOrDefault();

                        var message =
                            $"BOSS " +
                            $"name={boss.Name}, " +
                            $"level={boss.Level}, " +
                            $"maxhp={boss.MaxHP}, " +
                            $"ptavg={avg.ToString("F0")}, " +
                            $"ratio={ratio.ToString("F1")}, " +
                            $"BOSS_pos={boss.PosX},{boss.PosY},{boss.PosZ}, " +
                            $"player_pos={player.PosX},{player.PosY},{player.PosZ}";

                        AppLogger.Info(message);
                    }
                }

                this.currentBoss = boss;
                this.currentBossZoneName = currentZoneName;
            }
        }

        public CombatantEx GetBossInfo()
        {
            if (!this.IsAvailable ||
                !this.isActivationAllowed)
            {
                return null;
            }

            lock (BossLock)
            {
                return this.currentBoss?.Clone();
            }
        }

        public CombatantEx GetTargetInfo(
            OverlayType type)
        {
            var targetEx = default(CombatantEx);
            var targetInfo = default(TargetInfo);

            if (!this.IsAvailable)
            {
                return targetEx;
            }

            var player = CombatantsManager.Instance.Player;
            if (player == null)
            {
                return targetEx;
            }

            switch (type)
            {
                case OverlayType.Target:
                    if (player.TargetID != 0)
                    {
                        // TargetID に明確な値が入っている場合はそのまま使用する
                        targetEx = CombatantsManager.Instance.GetCombatantMain(player.TargetID);
                    }
                    else
                    {
                        // TargetID が0だった場合はsharlayanのTarget情報も参照する
                        targetInfo = SharlayanHelper.Instance.TargetInfo;
                        if (targetInfo == null)
                        {
                            return targetEx;
                        }

                        targetEx = CombatantsManager.Instance.GetCombatantMain(targetInfo.CurrentTargetID);
                    }

                    break;

                case OverlayType.TargetOfTarget:
                    // TargetOfTarget はXIVプラグインから取得する
                    targetEx = CombatantsManager.Instance.GetCombatantMain(player.TargetOfTargetID);
                    break;

                case OverlayType.FocusTarget:
                case OverlayType.HoverTarget:
                    // FocusTarget, HoverTarget はsharlayan経由で取得する
                    targetInfo = SharlayanHelper.Instance.TargetInfo;
                    if (targetInfo == null)
                    {
                        return targetEx;
                    }

                    targetEx = CombatantsManager.Instance.GetCombatantMain(
                        type == OverlayType.FocusTarget ?
                        targetInfo.FocusTarget?.ID ?? 0 :
                        targetInfo.MouseOverTarget?.ID ?? 0);
                    break;
            }

            return targetEx;
        }

        #endregion Get Targets

        #region Get Misc

        public Player GetPlayerStatus() => this.DataRepository?.GetPlayer();

        public int GetCurrentZoneID() =>
            this.IsAvailable ?
            ((int)this.DataRepository?.GetCurrentTerritoryID()) :
            0;

        public string GetCurrentZoneName() => this.ExecuteGetCurrentZoneName();

        private string ExecuteGetCurrentZoneName()
        {
            var name = ActGlobals.oFormActMain?.CurrentZone;
            if (string.IsNullOrEmpty(name) ||
                name.Contains("Unknown"))
            {
                name = this.GetTerritoryNameByID(this.GetCurrentZoneID());
            }

            return name;
        }

        public string ReplacePartyMemberName(
            string text,
            NameStyles style)
        {
            var r = text;

            var party = CombatantsManager.Instance.GetPartyList();

            foreach (var pc in party)
            {
                if (string.IsNullOrEmpty(pc.Name) ||
                    string.IsNullOrEmpty(pc.NameFI) ||
                    string.IsNullOrEmpty(pc.NameIF) ||
                    string.IsNullOrEmpty(pc.NameII))
                {
                    continue;
                }

                switch (style)
                {
                    case NameStyles.FullName:
                        r = r.Replace(pc.NameFI, pc.Name);
                        r = r.Replace(pc.NameIF, pc.Name);
                        r = r.Replace(pc.NameII, pc.Name);
                        break;

                    case NameStyles.FullInitial:
                        r = r.Replace(pc.Name, pc.NameFI);
                        break;

                    case NameStyles.InitialFull:
                        r = r.Replace(pc.Name, pc.NameIF);
                        break;

                    case NameStyles.InitialInitial:
                        r = r.Replace(pc.Name, pc.NameII);
                        break;
                }
            }

            return r;
        }

        #endregion Get Misc

        #region Resources

        private Dictionary<uint, Buff> buffList = new Dictionary<uint, Buff>();
        private Dictionary<uint, Skill> skillList = new Dictionary<uint, Skill>();
        private Dictionary<uint, World> worldList = new Dictionary<uint, World>();
        private List<Zone> zoneList = new List<Zone>();

        private static readonly int ResourcesRetryLimitCount = 8;
        private int zoneRetryCount = 0;
        private int skillRetryCount = 0;
        private int worldRetryCount = 0;

        public IReadOnlyList<Zone> ZoneList => this.zoneList;
        public IReadOnlyDictionary<uint, Skill> SkillList => this.skillList;
        public IReadOnlyDictionary<uint, World> WorldList => this.worldList;

        private volatile bool isZoneListLoaded = false;
        private volatile bool isSkillListLoaded = false;
        private volatile bool isWorldListLoaded = false;
        private volatile bool isMergedSkillList = false;
        private volatile bool isComplementedSkillList = false;
        private volatile bool isComplementedBuffList = false;
        private volatile bool isZoneListTranslated = false;

        private bool IsResourcesLoaded =>
            this.isZoneListLoaded &&
            this.isSkillListLoaded &&
            this.isWorldListLoaded &&
            this.isMergedSkillList &&
            this.isComplementedSkillList &&
            this.isComplementedBuffList &&
            this.isZoneListTranslated;

        public Regex WorldNameRemoveRegex { get; private set; }

        private void LoadWorldList()
        {
            if (this.worldRetryCount >= ResourcesRetryLimitCount)
            {
                if (this.worldRetryCount == ResourcesRetryLimitCount)
                {
                    AppLogger.Error("world list can't load.");
                    this.worldRetryCount++;
                }

                return;
            }

            if (this.worldList.Any())
            {
                return;
            }

            if (this.plugin == null)
            {
                return;
            }

            var dictionary = this.DataRepository.GetResourceDictionary(ResourceType.WorldList_EN);
            if (dictionary == null)
            {
                this.worldRetryCount++;
                return;
            }

            var newList = new Dictionary<uint, World>();

            foreach (var source in dictionary)
            {
                var entry = new World()
                {
                    ID = source.Key,
                    Name = source.Value,
                };

                newList.Add(entry.ID, entry);
            }

            // 有効なワールド番号の範囲を定義する
            var min = 0;
            var max = 0;

            switch (XIVApi.Instance.FFXIVLocale)
            {
                case Locales.KO:
                    min = 2048;
                    max = 2080;
                    break;

                case Locales.TW:
                case Locales.CN:
                    min = 1040;
                    max = 1179;
                    break;

                default:
                    min = 21;
                    max = 99;
                    break;
            }

            // ワールド名置換用正規表現を生成する
            var names = newList.Values
                .Where(x => x.ID >= min && x.ID <= max)
                .Select(x => x.Name);
            this.WorldNameRemoveRegex = new Regex(
                $@"(?<name>[A-Za-z'\-\.]+ [A-Za-z'\-\.]+)(?<world>{string.Join("|", names)})",
                RegexOptions.Compiled);

            this.worldList = newList;
            AppLogger.Trace("world list loaded.");
            this.isWorldListLoaded = true;
        }

        public string RemoveWorldName(
            string text)
        {
            if (this.WorldNameRemoveRegex == null ||
                string.IsNullOrEmpty(text))
            {
                return text;
            }

            var matches = this.WorldNameRemoveRegex.Matches(text);
            if (matches == null)
            {
                return text;
            }

            foreach (Match match in matches)
            {
                var world = match.Groups["world"];
                if (world != null &&
                    world.Success)
                {
                    if (world.Index >= 0 && (world.Index + world.Length) <= text.Length)
                    {
                        text = text.Remove(world.Index, world.Length);
                    }
                }
            }

            return text;
        }

        private void LoadSkillList()
        {
            if (this.skillRetryCount >= ResourcesRetryLimitCount)
            {
                if (this.skillRetryCount == ResourcesRetryLimitCount)
                {
                    AppLogger.Error("skii list can't load.");
                    this.skillRetryCount++;
                }

                return;
            }

            if (this.skillList.Any())
            {
                return;
            }

            if (this.plugin == null)
            {
                return;
            }

            var dictionary = this.DataRepository.GetResourceDictionary(ResourcesTypeDictionary[this.FFXIVLocale].Skill);
            if (dictionary == null)
            {
                this.skillRetryCount++;
                return;
            }

            var newList = new Dictionary<uint, Skill>();

            foreach (var source in dictionary)
            {
                var entry = new Skill()
                {
                    ID = source.Key,
                    Name = source.Value,
                };

                newList.Add(entry.ID, entry);
            }

            this.skillList = newList;
            AppLogger.Trace("skill list loaded.");
            this.isSkillListLoaded = true;
        }

        private void LoadZoneList()
        {
            if (this.zoneRetryCount >= ResourcesRetryLimitCount)
            {
                if (this.zoneRetryCount == ResourcesRetryLimitCount)
                {
                    AppLogger.Error("zone list can't load.");
                    this.zoneRetryCount++;
                }

                return;
            }

            if (this.zoneList.Any())
            {
                return;
            }

            if (this.plugin == null)
            {
                return;
            }

            var dictionary = this.DataRepository.GetResourceDictionary(ResourceType.ZoneList_EN);
            if (dictionary == null)
            {
                this.zoneRetryCount++;
                return;
            }

            var newList = new List<Zone>();

            foreach (var source in dictionary)
            {
                var entry = new Zone()
                {
                    ID = (int)source.Key,
                    Name = source.Value,
                };

                newList.Add(entry);
            }

            AppLogger.Trace("zone list loaded.");

            this.LoadZoneListAdded(newList);
            this.zoneList = newList;
            this.isZoneListLoaded = true;
        }

        private void LoadZoneListAdded(
            List<Zone> baseZoneList)
        {
            try
            {
                var dir = XIVApi.Instance.ResourcesDirectory;
                var file = Path.Combine(dir, "Zones.csv");

                if (!File.Exists(file))
                {
                    return;
                }

                var isLoaded = false;
                using (var parser = new TextFieldParser(file, new UTF8Encoding(false))
                {
                    TextFieldType = FieldType.Delimited,
                    Delimiters = new[] { "," },
                    HasFieldsEnclosedInQuotes = true,
                    TrimWhiteSpace = true,
                    CommentTokens = new[] { "#" },
                })
                {
                    while (!parser.EndOfData)
                    {
                        var values = parser.ReadFields();

                        if (values.Length >= 2)
                        {
                            var newZone = new Zone()
                            {
                                ID = int.Parse(values[0]),
                                Name = values[1].Trim(),
                                IsAddedByUser = true,
                            };

                            if (!baseZoneList.Any(x =>
                                x.ID == newZone.ID))
                            {
                                baseZoneList.Add(newZone);
                            }

                            isLoaded = true;
                        }
                    }
                }

                if (isLoaded)
                {
                    AppLogger.Trace($"custom zone list loaded. {file}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Trace(ex, "error on load additional zone list.");
            }
        }

        private void TranslateZoneList()
        {
            if (this.isZoneListTranslated ||
                !this.isZoneListLoaded)
            {
                return;
            }

            var d = XIVApi.Instance.TerritoryList.ToDictionary(x => x.ID);
            if (!d.Any())
            {
                return;
            }

            foreach (var zone in this.ZoneList)
            {
                if (d.ContainsKey(zone.ID))
                {
                    var territory = d[zone.ID];

                    if (XIVApi.Instance.FFXIVLocale != Locales.EN)
                    {
                        if (!string.IsNullOrEmpty(territory.Name))
                        {
                            zone.Name = territory.Name;
                        }
                    }

                    zone.IDonDB = territory.IntendedUse;
                    zone.Rank = Zone.ToRank(territory.IntendedUse, zone.Name);
                }
            }

            AppLogger.Trace($"zone list translated.");
            this.isZoneListTranslated = true;
        }

        /// <summary>
        /// XIVApiのスキルリストとFFXIVプラグインのスキルリストをマージする
        /// </summary>
        private void MergeSkillList()
        {
            if (this.isSkillListLoaded &&
                !this.isMergedSkillList)
            {
                lock (XIVApi.Instance.ActionList)
                {
                    if (this.skillList != null &&
                        XIVApi.Instance.ActionList.Any())
                    {
                        this.isMergedSkillList = true;

                        foreach (var action in XIVApi.Instance.ActionList)
                        {
                            var skill = new Skill()
                            {
                                ID = action.Key,
                                Name = action.Value.Name,
                                AttackType = action.Value.AttackType,
                            };

                            this.skillList[action.Key] = skill;
                        }

                        AppLogger.Trace("xivapi action list merged.");
                    }
                }
            }
        }

        public string GetTerritoryNameByID(
            int id) =>
            XIVApi.Instance.TerritoryList.FirstOrDefault(x => x.ID == id)?.NameEn ??
            "Unknown Zone (0x" + Convert.ToString(id, 16) + ")";

        #endregion Resources

        #region NativeMethods

        /// <summary>
        /// フォアグラウンドWindowのハンドルを取得する
        /// </summary>
        /// <returns>
        /// フォアグラウンドWindowのハンドル</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// WindowハンドルからそのプロセスIDを取得する
        /// </summary>
        /// <param name="hWnd">
        /// プロセスIDを取得するWindowハンドル</param>
        /// <param name="lpdwProcessId">
        /// プロセスID</param>
        /// <returns>
        /// Windowを作成したスレッドのID</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        #endregion NativeMethods
    }
}
