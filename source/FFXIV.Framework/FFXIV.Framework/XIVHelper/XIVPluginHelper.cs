using System;
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
using Microsoft.VisualBasic.FileIO;
using Sharlayan.Core.Enums;

namespace FFXIV.Framework.XIVHelper
{
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

        public Process Process => this.DataRepository?.GetCurrentFFXIVProcess();

        public bool IsAvailable
        {
            get
            {
                if (ActGlobals.oFormActMain == null ||
                    this.plugin == null ||
                    this.DataRepository == null ||
                    this.DataSubscription == null ||
                    this.Process == null)
                {
                    return false;
                }

                return true;
            }
        }

        public Locales LanguageID => (int)this.DataRepository.GetSelectedLanguageID() switch
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

        private System.Timers.Timer attachFFXIVPluginWorker;
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

            this.FFXIVLocale = ffxivLocale;
            this.MemorySubscriberInterval = pollingInteval;

            this.attachFFXIVPluginWorker = new System.Timers.Timer();
            this.attachFFXIVPluginWorker.AutoReset = true;
            this.attachFFXIVPluginWorker.Interval = 5000;
            this.attachFFXIVPluginWorker.Elapsed += (s, e) =>
            {
                try
                {
                    this.Attach();

                    lock (ResourcesLock)
                    {
                        this.LoadSkillList();
                        this.LoadZoneList();
                        this.LoadWorldList();

                        this.TranslateZoneList();
                        this.MergeSkillList();
#if false
                        this.MergeSkillListToXIVPlugin();
                        this.MergeBuffListToXIVPlugin();
#endif
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Attach FFXIV_ACT_Plugin error");
                }
            };

            this.scanFFXIVWorker = new ThreadWorker(() =>
            {
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

                try
                {
                    if (SharlayanHelper.Instance.IsScanning)
                    {
                        return;
                    }

                    SharlayanHelper.Instance.IsScanning = true;
                    this.RefreshCombatantList();
                }
                finally
                {
                    SharlayanHelper.Instance.IsScanning = false;
                }
            },
            pollingInteval * 1.1,
            nameof(this.scanFFXIVWorker),
            ThreadPriority.Lowest);

            // その他リソース読み込みを開始する
            await Task.Run(() =>
            {
                // sharlayan を開始する
                // Actor を取得しない
                // Party を取得しない
                Thread.Sleep(CommonHelper.GetRandomTimeSpan());
                SharlayanHelper.Instance.IsSkipActor = true;
                SharlayanHelper.Instance.IsSkipParty = true;
                SharlayanHelper.Instance.Start(pollingInteval);

                Thread.Sleep(CommonHelper.GetRandomTimeSpan());
                this.attachFFXIVPluginWorker.Start();

                Thread.Sleep(CommonHelper.GetRandomTimeSpan());
                this.scanFFXIVWorker.Run();

                // XIVDBをロードする
                Thread.Sleep(CommonHelper.GetRandomTimeSpan());
                XIVDB.Instance.FFXIVLocale = ffxivLocale;
                XIVDB.Instance.Load();

                // PC名記録をロードする
                Thread.Sleep(CommonHelper.GetRandomTimeSpan());
                PCNameDictionary.Instance.Load();

                // PTリストの並び順をロードする
                Thread.Sleep(CommonHelper.GetRandomTimeSpan());
                PCOrder.Instance.Load();
            });
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

            this.scanFFXIVWorker?.Abort();

            this.attachFFXIVPluginWorker?.Stop();
            this.attachFFXIVPluginWorker.Dispose();
            this.attachFFXIVPluginWorker = null;

            this.UnsubscribeLog();
            this.UnsubscribeXIVPluginEvents();

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

        private void Attach()
        {
            lock (this)
            {
                if (this.plugin != null ||
                    ActGlobals.oFormActMain == null)
                {
                    return;
                }

                var ffxivPlugin = (
                    from x in ActGlobals.oFormActMain.ActPlugins
                    where
                    x.pluginFile.Name.ToUpper().Contains("FFXIV_ACT_Plugin".ToUpper()) &&
                    x.lblPluginStatus.Text.ToUpper().Contains("FFXIV Plugin Started.".ToUpper())
                    select
                    x.pluginObj).FirstOrDefault();

                if (ffxivPlugin != null)
                {
                    this.plugin = ffxivPlugin;
                    this.DataRepository = this.plugin.DataRepository;
                    this.DataSubscription = this.plugin.DataSubscription;

                    this.SubscribeXIVPluginEvents();

                    AppLogger.Trace("attached ffxiv plugin.");

                    this.ActPluginAttachedCallback?.Invoke();
                }
            }
        }

        public PrimaryPlayerDelegate OnPrimaryPlayerChanged { get; set; }

        public PartyListChangedDelegate OnPartyListChanged { get; set; }

        public ZoneChangedDelegate OnZoneChanged { get; set; }

        public delegate void PlayerJobChangedDelegate();

        public delegate void PartyJobChangedDelegate();

        public PlayerJobChangedDelegate OnPlayerJobChanged { get; set; }

        public PartyJobChangedDelegate OnPartyJobChanged { get; set; }

        private void SubscribeXIVPluginEvents()
        {
            var xivPlugin = this.DataSubscription;

            xivPlugin.PrimaryPlayerChanged += this.XivPlugin_PrimaryPlayerChanged;
            xivPlugin.PartyListChanged += this.XivPlugin_PartyListChanged;
            xivPlugin.ZoneChanged += this.XivPlugin_ZoneChanged;
        }

        private void UnsubscribeXIVPluginEvents()
        {
            var xivPlugin = this.DataSubscription;

            xivPlugin.PrimaryPlayerChanged -= this.XivPlugin_PrimaryPlayerChanged;
            xivPlugin.PartyListChanged -= this.XivPlugin_PartyListChanged;
            xivPlugin.ZoneChanged -= this.XivPlugin_ZoneChanged;
        }

        private void XivPlugin_PrimaryPlayerChanged()
        {
            CombatantsManager.Instance.Clear();
            this.OnPrimaryPlayerChanged?.Invoke();
        }

        private void XivPlugin_PartyListChanged(
            ReadOnlyCollection<uint> partyList,
            int partySize)
        {
            CombatantsManager.Instance.RefreshPartyList(partyList);
            this.OnPartyListChanged?.Invoke(partyList, partySize);
        }

        private void XivPlugin_ZoneChanged(uint ZoneID, string ZoneName)
        {
            CombatantsManager.Instance.Clear();
            this.OnZoneChanged(ZoneID, ZoneName);
        }

        #endregion Attach FFXIV Plugin

        #region Log Subscriber

        private readonly List<LogLinetDelegate> LogSubscribers = new List<LogLinetDelegate>(16);

        public void SubscribeLog(LogLinetDelegate subscriber)
        {
            if (this.DataSubscription == null)
            {
                return;
            }

            lock (this.LogSubscribers)
            {
                this.DataSubscription.LogLine -= subscriber;
                this.DataSubscription.LogLine += subscriber;

                this.LogSubscribers.Add(subscriber);
            }
        }

        private void UnsubscribeLog()
        {
            if (this.DataSubscription == null)
            {
                return;
            }

            lock (this.LogSubscribers)
            {
                foreach (var action in this.LogSubscribers)
                {
                    this.DataSubscription.LogLine -= action;
                }

                this.LogSubscribers.Clear();
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

                if (pid == this.Process?.Id)
                {
                    this.IsFFXIVActive = true;
                    return;
                }

                // メインモジュールのファイル名を取得する
                var p = Process.GetProcessById(pid);
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

        #region Refresh Combatants

        #region Dummy Combatants

        private static readonly Combatant DummyPlayer = new Combatant()
        {
            ID = 1,
            Name = "Me Taro",
            MaxHP = 30000,
            CurrentHP = 30000,
            MaxMP = 12000,
            CurrentMP = 12000,
            MaxTP = 3000,
            CurrentTP = 3000,
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
                MaxTP = 3000,
                CurrentTP = 3000,
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
                MaxTP = 3000,
                CurrentTP = 3000,
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
                MaxTP = 3000,
                CurrentTP = 3000,
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

        private bool isFirst = true;
        private JobIDs previousPlayerJobID = JobIDs.Unknown;
        private readonly Dictionary<uint, JobIDs> previousPartyJobList = new Dictionary<uint, JobIDs>(8);

        public void RefreshCombatantList()
        {
            if (!this.IsAvailable)
            {
                if (IsDebug)
                {
                    CombatantsManager.Instance.Refresh(DummyCombatants, IsDebug);
                    raiseFirstCombatants();
                }

                return;
            }

            var now = DateTime.Now;
            if ((now - this.inCombatTimestamp).TotalSeconds >= 1.0)
            {
                this.inCombatTimestamp = now;
                this.RefreshInCombat();
                this.RefreshBoss();
                this.DetectPartyJobChange();
            }

            var combatants = this.DataRepository.GetCombatantList();
            var addeds = CombatantsManager.Instance.Refresh(combatants);

            if (addeds.Any())
            {
                this.AddedCombatants?.Invoke(
                    this,
                    new AddedCombatantsEventArgs(addeds));
            }

            raiseFirstCombatants();

            if (CombatantsManager.Instance.Player != null)
            {
                if (this.previousPlayerJobID != CombatantsManager.Instance.Player.JobID)
                {
                    this.previousPlayerJobID = CombatantsManager.Instance.Player.JobID;
                    this.OnPlayerJobChanged?.Invoke();
                }
            }

            void raiseFirstCombatants()
            {
                if (this.isFirst &&
                    CombatantsManager.Instance.CombatantsMainCount > 0)
                {
                    this.isFirst = false;
                    this.OnPrimaryPlayerChanged?.Invoke();
                }
            }
        }

        private void RefreshInCombat()
        {
            var result = false;

#if true
            var player = CombatantsManager.Instance.Player;
            if (player != null)
            {
                result =
                    player.CurrentHP != player.MaxHP ||
                    player.CurrentMP != player.MaxMP ||
                    player.CurrentTP != player.MaxTP;
            }

            if (!result)
            {
                var combatants = CombatantsManager.Instance.GetPartyList();
                if (combatants != null &&
                    combatants.Any())
                {
                    result = (
                        from x in combatants
                        where
                        x.CurrentHP != x.MaxHP ||
                        x.CurrentMP != x.MaxMP ||
                        x.CurrentTP != x.MaxTP
                        select
                        x).Any();
                }
            }
#else
            result = SharlayanHelper.Instance.CurrentPlayer?.InCombat ?? false;
#endif

            this.InCombat = result;
        }

        private void DetectPartyJobChange()
        {
            var party = CombatantsManager.Instance.GetPartyList()
                .ToDictionary(x => x.ID, x => x.JobID);

            if (this.previousPartyJobList.Count == party.Count)
            {
                var isPartyJobChanged = false;
                foreach (var pc in party)
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

        #endregion Refresh Combatants

        #region Get Targets

        private static readonly object BossLock = new object();
        private CombatantEx currentBoss;
        private string currentBossZoneName;

        public Func<double> GetBossHPThresholdCallback { get; set; }

        public Func<bool> GetAvailableBossCallback { get; set; }

        private void RefreshBoss()
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
                var party = CombatantsManager.Instance.GetPartyList();
                var combatants = CombatantsManager.Instance.GetCombatants();

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
            if (!this.IsAvailable)
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
            var target = this.DataRepository?.GetCombatantByOverlayType(type);

            if (target == null)
            {
                return null;
            }

            var targetEx = CombatantsManager.Instance.GetCombatantMain(target.ID);

            return targetEx;
        }

        #endregion Get Targets

        #region Get Misc

        public int GetCurrentZoneID() =>
            this.IsAvailable ?
            ((int)this.DataRepository?.GetCurrentTerritoryID()) :
            0;

        public string GetCurrentZoneName() => ActGlobals.oFormActMain?.CurrentZone;

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

        private static readonly object ResourcesLock = new object();

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

        private bool isZoneListLoaded = false;
        private bool isSkillListLoaded = false;

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

            // ワールド名置換用正規表現を生成する
            var names = newList.Values
                .Where(x =>
                    (x.ID >= 23 && x.ID <= 99) ||
                    (x.ID >= 1040 && x.ID <= 1172) ||
                    (x.ID >= 2048 && x.ID <= 2079))
                .Select(x => x.Name);
            this.WorldNameRemoveRegex = new Regex(
                $@"(?<name>[A-Za-z'\-\.]+ [A-Za-z'\-\.]+)(?<world>{string.Join("|", names)})",
                RegexOptions.Compiled);

            this.worldList = newList;
            AppLogger.Trace("world list loaded.");
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
            if (matches == null ||
                matches.Count < 1)
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
                var dir = XIVDB.Instance.ResourcesDirectory;
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

        private bool isZoneListTranslated = false;

        private void TranslateZoneList()
        {
            if (this.isZoneListTranslated ||
                !this.isZoneListLoaded ||
                !XIVDB.Instance.PlacenameList.Any())
            {
                return;
            }

            foreach (var zone in this.ZoneList)
            {
                var place = (
                    from x in XIVDB.Instance.PlacenameList.AsParallel()
                    where
                    string.Equals(x.NameEn, zone.Name, StringComparison.InvariantCultureIgnoreCase)
                    select
                    x).FirstOrDefault();

                if (place != null)
                {
                    zone.Name = place.Name;
                    zone.IDonDB = place.ID;
                }
                else
                {
                    var area = (
                        from x in XIVDB.Instance.AreaList.AsParallel()
                        where
                        string.Equals(x.NameEn, zone.Name, StringComparison.InvariantCultureIgnoreCase)
                        select
                        x).FirstOrDefault();

                    if (area != null)
                    {
                        zone.Name = area.Name;
                        zone.IDonDB = area.ID;
                    }
                }
            }

            AppLogger.Trace($"zone list translated.");
            this.isZoneListTranslated = true;
        }

#if false
        private void LoadBuffList()
        {
            if (this.buffList.Any())
            {
                return;
            }

            if (this.plugin == null)
            {
                return;
            }

            var dictionary = this.dataRepository.GetResourceDictionary(ResourcesTypeDictionary[this.FFXIVLocale].Buff);
            if (dictionary == null)
            {
                return;
            }

            var newList = new Dictionary<uint, Buff>();

            foreach (var source in dictionary)
            {
                var entry = new Buff()
                {
                    ID = source.Key,
                    Name = source.Value,
                };

                newList.Add(entry.ID, entry);
            }

            this.buffList = newList;
            AppLogger.Trace("buff list loaded.");
        }
#endif

        private volatile bool isMergedSkillList = false;
        private volatile bool isMergedSkillToXIVPlugin = false;
        private volatile bool isMergedBuffListToXIVPlugin = false;

        /// <summary>
        /// XIVDBのスキルリストとFFXIVプラグインのスキルリストをマージする
        /// </summary>
        private void MergeSkillList()
        {
            if (this.isSkillListLoaded &&
                !this.isMergedSkillList)
            {
                lock (XIVDB.Instance.ActionList)
                {
                    if (this.skillList != null &&
                        XIVDB.Instance.ActionList.Any())
                    {
                        this.isMergedSkillList = true;

                        foreach (var action in XIVDB.Instance.ActionList)
                        {
                            var skill = new Skill()
                            {
                                ID = action.Key,
                                Name = action.Value.Name
                            };

                            this.skillList[action.Key] = skill;
                        }

                        AppLogger.Trace("XIVDB Action list merged.");
                    }
                }
            }
        }

        private void MergeSkillListToXIVPlugin()
        {
            if (this.isMergedSkillToXIVPlugin)
            {
                return;
            }

            if (this.plugin == null)
            {
                return;
            }

            var t = (this.plugin as object).GetType().Module.Assembly.GetType("FFXIV_ACT_Plugin.Resources.SkillList");
            var obj = t.GetField(
                "_instance",
                BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);

            var list = obj.GetType().GetField(
                "_SkillList",
                BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(obj);

            var pluginSkillList = list as SortedDictionary<uint, string>;

            foreach (var entry in XIVDB.Instance.ActionList)
            {
                if (!pluginSkillList.ContainsKey(entry.Key))
                {
                    pluginSkillList[entry.Key] = entry.Value.Name;
                }
            }

            if (XIVDB.Instance.ActionList.Any())
            {
                this.isMergedSkillToXIVPlugin = true;
                AppLogger.Trace("XIVDB Action list -> FFXIV Plugin");
            }
        }

        /// <summary>
        /// XIVDBのBuff(Status)リストとFFXIVプラグインのBuffリストをマージする
        /// </summary>
        private void MergeBuffListToXIVPlugin()
        {
            if (this.isMergedBuffListToXIVPlugin)
            {
                return;
            }

            if (!XIVDB.Instance.BuffList.Any())
            {
                return;
            }

            if (this.plugin == null)
            {
                return;
            }

            var t = (this.plugin as object).GetType().Module.Assembly.GetType("FFXIV_ACT_Plugin.Resources.BuffList");
            var obj = t.GetField(
                "_instance",
                BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);

            var list = obj.GetType().GetField(
                "_BuffList",
                BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(obj);

            var pluginList = list as SortedDictionary<uint, string>;

            foreach (var entry in XIVDB.Instance.BuffList)
            {
                if (!pluginList.ContainsKey(entry.Key))
                {
                    pluginList[entry.Key] = entry.Value.Name;
                    Debug.WriteLine(entry.ToString());
                }
            }

            if (XIVDB.Instance.BuffList.Any())
            {
                AppLogger.Trace("XIVDB Status list -> FFXIV Plugin");
            }

            this.isMergedBuffListToXIVPlugin = true;
        }

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
