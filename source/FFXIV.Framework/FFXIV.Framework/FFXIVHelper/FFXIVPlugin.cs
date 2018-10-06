using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using Microsoft.VisualBasic.FileIO;
using TamanegiMage.FFXIV_MemoryReader.Model;

namespace FFXIV.Framework.FFXIVHelper
{
    public class FFXIVPlugin
    {
        #region Singleton

        private static FFXIVPlugin instance;

        public static FFXIVPlugin Instance =>
            instance ?? (instance = new FFXIVPlugin());

        public static void Free() => instance = null;

        private FFXIVPlugin()
        {
        }

        #endregion Singleton

        #region Logger

        private static NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        /// <summary>FFXIV_ACT_Plugin</summary>
        private dynamic plugin;

        /// <summary>FFXIV_ACT_Plugin.MemoryScanSettings</summary>
        private dynamic pluginConfig;

        /// <summary>FFXIV_ACT_Plugin.Memory.Memory</summary>
        private dynamic pluginMemory;

        /// <summary>FFXIV_ACT_Plugin.Memory.ScanCombatants</summary>
        private dynamic pluginScancombat;

        /// <summary>FFXIV_ACT_Plugin.Overlays.Overlay</summary>
        private dynamic overlay;

        /// <summary>FFXIV_ACT_Plugin.OverlayConfig</summary>
        private dynamic overlayConfig;

        /// <summary>FFXIV_ACT_Plugin.Overlays.OverlayData</summary>
        private dynamic overlayData;

        private MethodInfo readCombatantMethodInfo;

        /// <summary>
        /// ACTプラグイン型のプラグインオブジェクトのインスタンス
        /// </summary>
        private IActPluginV1 ActPlugin => (IActPluginV1)this.plugin;

        public Locales FFXIVLocale
        {
            get;
            set;
        } = Locales.JA;

        internal bool IsAvilableFFXIVPlugin => this.plugin != null;

        public bool IsAvailable
        {
            get
            {
                if (ActGlobals.oFormActMain == null ||
                    this.plugin == null ||
                    this.pluginConfig == null ||
                    this.pluginScancombat == null ||
                    this.overlay == null ||
                    this.overlayConfig == null ||
                    this.overlayData == null ||
                    this.Process == null)
                {
                    return false;
                }

                return true;
            }
        }

        public Process Process => (Process)this.pluginConfig?.Process;

        #region Start/End

        private System.Timers.Timer attachFFXIVPluginWorker;
        private ThreadWorker scanFFXIVWorker;
        private volatile bool isStarted = false;

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

            this.scanFFXIVWorker?.Abort();

            this.attachFFXIVPluginWorker?.Stop();
            this.attachFFXIVPluginWorker.Dispose();
            this.attachFFXIVPluginWorker = null;

            // PC名記録をセーブする
            PCNameDictionary.Instance.Save();
            PCNameDictionary.Free();
            PCOrder.Free();
        }

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
                        this.MergeSkillList();
                        this.MergeBuffList();
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
#if !DEBUG
                    return;
#endif
                }

                this.RefreshCombatantList();
                this.RefreshCurrentPartyIDList();
            },
            pollingInteval,
            nameof(this.attachFFXIVPluginWorker));

            await Task.Run(() =>
            {
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

        #endregion Start/End

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
            if ((DateTime.Now - this.detectedActiveTimestamp).TotalSeconds <= 5.0)
            {
                return;
            }

            try
            {
                this.detectedActiveTimestamp = DateTime.Now;

                // フォアグラウンドWindowのハンドルを取得する
                var hWnd = GetForegroundWindow();

                // プロセスIDに変換する
                int pid;
                GetWindowThreadProcessId(hWnd, out pid);

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

        #endregion Refresh Active

        #region Refresh Combatants

        private readonly IReadOnlyList<Combatant> EmptyCombatantList = new List<Combatant>();

        private object combatantListLock = new object();
        private volatile IReadOnlyDictionary<uint, Combatant> combatantDictionary;
        private volatile IReadOnlyList<Combatant> combatantList;

        private object currentPartyIDListLock = new object();
        private volatile List<uint> currentPartyIDList = new List<uint>();

        private Combatant previousBoss = new Combatant();

        private readonly object playerLocker = new object();
        private Combatant player;

#if DEBUG

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
            type = ObjectType.PC,
            Player = DummyPlayer,
        };

        private readonly IReadOnlyList<Combatant> DummyCombatants = new List<Combatant>()
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
                type = ObjectType.PC,
                Player = DummyPlayer,
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
                type = ObjectType.PC,
                Player = DummyPlayer,
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
                type = ObjectType.PC,
                Player = DummyPlayer,
            },
        };

        private readonly uint[] DummyPartyList = new uint[]
        {
            1, 2, 3, 4, 5, 6, 7, 8
        };

        #endregion Dummy Combatants

#endif

        public class AddedCombatantsEventArgs :
            EventArgs
        {
            public AddedCombatantsEventArgs(
                IEnumerable<Combatant> newCombatants)
            {
                this.NewCombatants = newCombatants.ToList();
            }

            public IReadOnlyList<Combatant> NewCombatants { get; private set; }
        }

        public event EventHandler<AddedCombatantsEventArgs> AddedCombatants;

        private void OnAddedCombatants(
            AddedCombatantsEventArgs e) => this?.AddedCombatants?.Invoke(this, e);

        public void RefreshCombatantList()
        {
            if (!this.IsAvailable)
            {
#if DEBUG
                lock (this.combatantListLock)
                {
                    foreach (var entity in this.DummyCombatants)
                    {
                        entity.SetName(entity.Name);
                    }

                    var addedCombatants =
                        this.combatantList == null ?
                        this.DummyCombatants :
                        this.DummyCombatants
                            .Where(x => !this.combatantList.Any(y => y.ID == x.ID))
                            .ToArray();

                    this.player = DummyPlayer;
                    this.combatantList = this.DummyCombatants;
                    this.combatantDictionary = this.DummyCombatants.ToDictionary(x => x.ID);

                    if (addedCombatants.Any())
                    {
                        Task.Run(() => this.OnAddedCombatants(new AddedCombatantsEventArgs(addedCombatants)));
                    }
                }
#endif

                return;
            }

            if (!FFXIVReader.Instance.IsAvailable)
            {
                this.RefreshCombatantListFromFFXIVPlugin();
            }
            else
            {
                this.RefreshCombatantListFromFFXIVReader();
            }
        }

        /// <summary>
        /// 扱うプレイヤー情報の最大数
        /// </summary>
        private const int MaxPCCount = 32;

        private void RefreshCombatantListFromFFXIVPlugin()
        {
            dynamic list = this.pluginScancombat.GetCombatantList();
            var count = (int)list.Count;

            var newList = new List<Combatant>(count);
            var newDictionary = new Dictionary<uint, Combatant>(count);
            var player = default(Combatant);

            var pcCount = 0;

            foreach (dynamic item in list.ToArray())
            {
                if (item == null)
                {
                    continue;
                }

                var combatant = new Combatant();

                combatant.ID = (uint)item.ID;
                combatant.OwnerID = (uint)item.OwnerID;
                combatant.Job = (byte)item.Job;
                combatant.type = (ObjectType)((byte)item.type);
                combatant.Level = (byte)item.Level;
                combatant.CurrentHP = (int)item.CurrentHP;
                combatant.MaxHP = (int)item.MaxHP;
                combatant.CurrentMP = (int)item.CurrentMP;
                combatant.MaxMP = (int)item.MaxMP;
                combatant.CurrentTP = (short)item.CurrentTP;

                combatant.IsCasting = (bool)item.IsCasting;
                combatant.CastBuffID = (int)item.CastBuffID;
                combatant.CastDurationCurrent = (float)item.CastDurationCurrent;
                combatant.CastDurationMax = (float)item.CastDurationMax;

                // 扱うプレイヤー数の最大数を超えたらカットする
                if (combatant.type == ObjectType.PC)
                {
                    pcCount++;
                    if (pcCount >= MaxPCCount)
                    {
                        continue;
                    }
                }

                this.SetSkillName(combatant);

                combatant.PosX = (float)item.PosX;
                combatant.PosY = (float)item.PosY;
                combatant.PosZ = (float)item.PosZ;

                var name = (string)item.Name;

                // 名前を登録する
                // TYPEによって分岐するため先にTYPEを設定しておくこと
                combatant.SetName(name);

                if (player == null)
                {
                    player = combatant;
                }

                combatant.Player = player;

                newList.Add(combatant);
                newDictionary.Add(combatant.ID, combatant);
            }

            lock (this.combatantListLock)
            {
                var addedCombatants =
                    this.combatantList == null ?
                    newList.ToArray() :
                    newList
                        .Where(x => !this.combatantList.Any(y => y.ID == x.ID))
                        .ToArray();

                this.combatantList = newList;
                this.combatantDictionary = newDictionary;

                // TargetOfTargetを設定する
                if (player != null)
                {
                    var tot = this.GetTargetInfo(OverlayType.TargetOfTarget);
                    if (tot != null)
                    {
                        player.TargetOfTargetID = tot.ID;
                    }
                }

                if (addedCombatants.Any())
                {
                    Task.Run(() => this.OnAddedCombatants(new AddedCombatantsEventArgs(addedCombatants)));
                }
            }

            lock (this.playerLocker)
            {
                this.player = player;
            }
        }

        private void RefreshCombatantListFromFFXIVReader()
        {
            var query = FFXIVReader.Instance.GetCombatantsV1()
                .Select(x => new Combatant(x));
            var player = default(Combatant);

            var pcCount = 0;

            var list = new List<Combatant>(query.Count());
            foreach (var combatant in query)
            {
                // 扱うプレイヤー数の最大数を超えたらカットする
                if (combatant.type == ObjectType.PC)
                {
                    pcCount++;
                    if (pcCount >= MaxPCCount)
                    {
                        continue;
                    }
                }

                this.SetSkillName(combatant);
                combatant.SetName(combatant.Name);

                if (player == null)
                {
                    player = combatant;
                }

                combatant.Player = player;

                list.Add(combatant);
            }

            lock (this.combatantListLock)
            {
                var addedCombatants =
                    this.combatantList == null ?
                    list.ToArray() :
                    list
                        .Where(x => !this.combatantList.Any(y => y.ID == x.ID))
                        .ToArray();

                this.combatantList = list;
                this.combatantDictionary = list.ToDictionary(x => x.ID);

                // TargetOfTargetを設定する
                if (player != null)
                {
                    var tot = this.GetTargetInfo(OverlayType.TargetOfTarget);
                    if (tot != null)
                    {
                        player.TargetOfTargetID = tot.ID;
                    }
                }

                if (addedCombatants.Any())
                {
                    Task.Run(() => this.OnAddedCombatants(new AddedCombatantsEventArgs(addedCombatants)));
                }
            }

            lock (this.playerLocker)
            {
                this.player = player;
            }
        }

        private DateTime currentPartyIDListTimestamp = DateTime.MinValue;

        public void RefreshCurrentPartyIDList()
        {
            if ((DateTime.Now - this.currentPartyIDListTimestamp).TotalSeconds <= 1.0)
            {
                return;
            }

            this.currentPartyIDListTimestamp = DateTime.Now;

            if (!this.IsAvailable)
            {
#if DEBUG
                lock (this.currentPartyIDListLock)
                {
                    this.currentPartyIDList = new List<uint>(this.DummyPartyList);
                }
#endif
                return;
            }

            var dummyBuffer = new byte[51200];
            var partyList = pluginScancombat?.GetCurrentPartyList(
                dummyBuffer,
                out int partyCount) as List<uint>;

            if (partyList == null)
            {
                return;
            }

            lock (this.currentPartyIDListLock)
            {
                this.currentPartyIDList = partyList;
            }
        }

        public void SetSkillName(
            Combatant combatant)
        {
            if (combatant == null)
            {
                return;
            }

            if (combatant.IsCasting)
            {
                if (this.skillList != null &&
                    this.skillList.ContainsKey(combatant.CastBuffID))
                {
                    combatant.CastSkillName =
                        this.skillList[combatant.CastBuffID].Name;
                }
                else
                {
                    combatant.CastSkillName =
                        $"UNKNOWN:{combatant.CastBuffID}";
                }
            }
            else
            {
                combatant.CastSkillName = string.Empty;
            }
        }

        #endregion Refresh Combatants

        #region Get Combatants

        public Combatant GetPlayer()
        {
            lock (this.playerLocker)
            {
                return this.player?.Clone();
            }
        }

        public IReadOnlyDictionary<uint, Combatant> GetCombatantDictionaly()
        {
            if (this.combatantDictionary == null)
            {
                return null;
            }

            lock (this.combatantListLock)
            {
                return new Dictionary<uint, Combatant>(
                    (Dictionary<uint, Combatant>)this.combatantDictionary);
            }
        }

        public IReadOnlyList<Combatant> GetCombatantList()
        {
            if (this.combatantList == null)
            {
                return this.EmptyCombatantList;
            }

            lock (this.combatantListLock)
            {
                return new List<Combatant>(this.combatantList);
            }
        }

        public IReadOnlyList<Combatant> GetPartyList()
        {
            var combatants = this.GetCombatantDictionaly();

            if (combatants == null ||
                combatants.Count < 1)
            {
                return this.EmptyCombatantList;
            }

            var partyIDs = default(List<uint>);

            lock (this.currentPartyIDListLock)
            {
                partyIDs = new List<uint>(this.currentPartyIDList);
            }

            // PartyIDリストで絞り込みつつソートをかける
            var sortedPartyList = (
                from x in (
                    from id in partyIDs
                    where
                    combatants.ContainsKey(id)
                    select
                    combatants[id]
                )
                orderby
                x.IsPlayer ? 0 : 1,
                x.DisplayOrder,
                x.Role.ToSortOrder(),
                x.Job,
                x.ID descending
                select
                x).ToList();

            return sortedPartyList;
        }

        /// <summary>
        /// 名前に該当するCombatantを返す
        /// </summary>
        /// <param name="name">名前</param>
        /// <returns>Combatant</returns>
        public Combatant GetCombatant(
            string name)
            => this.GetCombatantList().FirstOrDefault(x =>
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.NameFI, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.NameIF, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.NameII, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// パーティをロールで分類して取得する
        /// </summary>
        /// <returns>
        /// ロールで分類したパーティリスト</returns>
        public IReadOnlyList<CombatantsByRole> GetPatryListByRole()
        {
            var list = new List<CombatantsByRole>();

            var partyList = this.GetPartyList();

            var tanks = partyList
                .Where(x => x.AsJob().Role == Roles.Tank)
                .ToList();

            var dpses = partyList
                .Where(x =>
                    x.AsJob().Role == Roles.MeleeDPS ||
                    x.AsJob().Role == Roles.RangeDPS ||
                    x.AsJob().Role == Roles.MagicDPS)
                .ToList();

            var melees = partyList
                .Where(x => x.AsJob().Role == Roles.MeleeDPS)
                .ToList();

            var ranges = partyList
                .Where(x => x.AsJob().Role == Roles.RangeDPS)
                .ToList();

            var magics = partyList
                .Where(x => x.AsJob().Role == Roles.MagicDPS)
                .ToList();

            var healers = partyList
                .Where(x => x.AsJob().Role == Roles.Healer)
                .ToList();

            if (tanks.Any())
            {
                list.Add(new CombatantsByRole(
                    Roles.Tank,
                    "TANK",
                    tanks));
            }

            if (dpses.Any())
            {
                list.Add(new CombatantsByRole(
                    Roles.DPS,
                    "DPS",
                    dpses));
            }

            if (melees.Any())
            {
                list.Add(new CombatantsByRole(
                    Roles.MeleeDPS,
                    "MELEE",
                    melees));
            }

            if (ranges.Any())
            {
                list.Add(new CombatantsByRole(
                    Roles.RangeDPS,
                    "RANGE",
                    ranges));
            }

            if (magics.Any())
            {
                list.Add(new CombatantsByRole(
                    Roles.MagicDPS,
                    "MAGIC",
                    magics));
            }

            if (healers.Any())
            {
                list.Add(new CombatantsByRole(
                    Roles.Healer,
                    "HEALER",
                    healers));
            }

            return list;
        }

        #endregion Get Combatants

        #region Get Targets

        public Combatant GetBossInfo(
            double bossHPThreshold)
        {
            if (!this.IsAvailable)
            {
                return null;
            }

            var party = this.GetPartyList();
            var combatants = this.GetCombatantList();

            if (party == null ||
                combatants == null ||
                party.Count < 1 ||
                combatants.Count < 1)
            {
                return null;
            }

            // パーティのHP平均値を算出する
            var players = party.Where(x => x.type == ObjectType.PC);
            if (!players.Any())
            {
                return null;
            }

            var avg = players.Average(x => x.MaxHP);

            // BOSSを検出する
            var boss = (
                from x in combatants
                where
                x.MaxHP >= (avg * bossHPThreshold) &&
                x.type == ObjectType.Monster &&
                x.CurrentHP > 0
                orderby
                x.Level descending,
                (x.MaxHP != x.CurrentHP ? 0 : 1) ascending,
                x.MaxHP descending,
                x.ID descending
                select
                x).FirstOrDefault();

            if (boss != null)
            {
                boss.Player = combatants.FirstOrDefault();
            }

            this.SetSkillName(boss);

            #region Logger

            if (boss != null)
            {
                if (this.previousBoss == null ||
                    this.previousBoss.ID != boss.ID)
                {
                    var ratio =
                        avg != 0 ?
                        boss.MaxHP / avg :
                        boss.MaxHP;

                    var player = combatants.FirstOrDefault();

                    var message =
                        $"BOSS " +
                        $"name={boss.Name}, " +
                        $"maxhp={boss.MaxHP}, " +
                        $"ptavg={avg.ToString("F0")}, " +
                        $"ratio={ratio.ToString("F1")}, " +
                        $"BOSS_pos={boss.PosX},{boss.PosY},{boss.PosZ}, " +
                        $"player_pos={player.PosX},{player.PosY},{player.PosZ}";

                    AppLogger.Info(message);
                }
            }

            this.previousBoss = boss;

            #endregion Logger

            return boss;
        }

        public Combatant GetTargetInfo(
            OverlayType type)
        {
            if (!this.IsAvailable ||
                this.readCombatantMethodInfo == null)
            {
                return null;
            }

            dynamic data = this.readCombatantMethodInfo?.Invoke(
                this.overlayData,
                new object[] { type });

            if (data == null)
            {
                return null;
            }

            uint id = data.id;

            Combatant combatant = null;
            lock (this.combatantListLock)
            {
                if (this.combatantDictionary != null &&
                    this.combatantDictionary.ContainsKey(id))
                {
                    combatant = this.combatantDictionary[id];
                    combatant.Player = this.combatantList.FirstOrDefault();
                }
            }

            this.SetSkillName(combatant);

            return combatant;
        }

        #endregion Get Targets

        #region Get Misc

        public int GetCurrentZoneID() =>
            this.IsAvailable ?
            (this.pluginScancombat?.GetCurrentZoneId() ?? 0) :
            0;

        /// <summary>
        /// 文中に含まれるパーティメンバの名前を設定した形式に置換する
        /// </summary>
        /// <param name="text">置換対象のテキスト</param>
        /// <returns>
        /// 置換後のテキスト</returns>
        public string ReplacePartyMemberName(
            string text,
            NameStyles style)
        {
            var r = text;

            var party = this.GetPartyList();

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

        #region Attach FFXIV Plugin

        private void Attach()
        {
            lock (this)
            {
                this.AttachPlugin();
                this.AttachScanMemory();
                this.AttachOverlay();

                this.LoadSkillList();
            }
        }

        private void AttachPlugin()
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
                AppLogger.Trace("attached ffxiv plugin.");
            }
        }

        private void AttachScanMemory()
        {
            if (this.plugin == null)
            {
                return;
            }

            FieldInfo fi;

            if (this.pluginMemory == null)
            {
                fi = this.plugin?.GetType().GetField(
                    "_Memory",
                    BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                this.pluginMemory = fi?.GetValue(this.plugin);
            }

            if (this.pluginMemory == null)
            {
                return;
            }

            if (this.pluginConfig == null)
            {
                fi = this?.pluginMemory?.GetType().GetField(
                    "_config",
                    BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                this.pluginConfig = fi?.GetValue(this.pluginMemory);
            }

            if (this.pluginConfig == null)
            {
                return;
            }

            if (this.pluginScancombat == null)
            {
                fi = this.pluginConfig?.GetType().GetField(
                    "ScanCombatants",
                    BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                this.pluginScancombat = fi?.GetValue(this.pluginConfig);

                AppLogger.Trace("attached ffxiv plugin ScanCombatants.");
            }
        }

        private void AttachOverlay()
        {
            if (this.plugin == null)
            {
                return;
            }

            FieldInfo fi;

            if (this.overlay == null)
            {
                fi = this.plugin?.GetType().GetField(
                    "_Overlays",
                    BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                this.overlay = fi?.GetValue(this.plugin);
            }

            if (this.overlay == null)
            {
                return;
            }

            if (this.overlayConfig == null)
            {
                fi = this.overlay?.GetType().GetField(
                    "_config",
                    BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                this.overlayConfig = fi?.GetValue(this.overlay);
            }

            if (this.overlayConfig == null)
            {
                return;
            }

            if (this.overlayData == null)
            {
                this.overlayData = this.overlayConfig?.OverlayData;
            }

            if (this.overlayData == null)
            {
                return;
            }

            if (this.readCombatantMethodInfo == null)
            {
                var t = (this.overlayData as object)?.GetType();
                this.readCombatantMethodInfo = t?.GetMethod("ReadCombatant");
            }
        }

        #endregion Attach FFXIV Plugin

        #region Resources

        private static readonly object ResourcesLock = new object();

        private Dictionary<int, Buff> buffList = new Dictionary<int, Buff>();
        private Dictionary<int, Skill> skillList = new Dictionary<int, Skill>();
        private List<Zone> zoneList = new List<Zone>();

        public IReadOnlyList<Zone> ZoneList => this.zoneList;
        public IReadOnlyDictionary<int, Skill> SkillList => this.skillList;

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

            var asm = this.plugin.GetType().Assembly;

            var language = this.FFXIVLocale.ToString().Replace("JA", "JP");
            var resourcesName = $"FFXIV_ACT_Plugin.Resources.BuffList_{language.ToUpper()}.txt";

            using (var st = asm.GetManifestResourceStream(resourcesName))
            {
                if (st == null)
                {
                    return;
                }

                var newList = new Dictionary<int, Buff>();

                using (var sr = new StreamReader(st))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var values = line.Split('|');
                            if (values.Length >= 2)
                            {
                                var buff = new Buff()
                                {
                                    ID = int.Parse(values[0], NumberStyles.HexNumber),
                                    Name = values[1].Trim()
                                };

                                newList.Add(buff.ID, buff);
                            }
                        }
                    }
                }

                this.buffList = newList;
                AppLogger.Trace("buff list loaded.");
            }
        }

        private void LoadSkillList()
        {
            if (this.skillList.Any())
            {
                return;
            }

            if (this.plugin == null)
            {
                return;
            }

            var asm = this.plugin.GetType().Assembly;

            var language = this.FFXIVLocale.ToString().Replace("JA", "JP");
            var resourcesName = $"FFXIV_ACT_Plugin.Resources.SkillList_{language.ToUpper()}.txt";

            using (var st = asm.GetManifestResourceStream(resourcesName))
            {
                if (st == null)
                {
                    return;
                }

                var newList = new Dictionary<int, Skill>();

                using (var sr = new StreamReader(st))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var values = line.Split('|');
                            if (values.Length >= 2)
                            {
                                var skill = new Skill()
                                {
                                    ID = int.Parse(values[0], NumberStyles.HexNumber),
                                    Name = values[1].Trim()
                                };

                                newList.Add(skill.ID, skill);
                            }
                        }
                    }
                }

                this.skillList = newList;
                AppLogger.Trace("skill list loaded.");
            }
        }

        private void LoadZoneList()
        {
            if (this.zoneList.Any())
            {
                return;
            }

            if (this.plugin == null)
            {
                return;
            }

            var newList = new List<Zone>();

            var asm = this.plugin.GetType().Assembly;

            var language = "EN";
            var resourcesName = $"FFXIV_ACT_Plugin.Resources.ZoneList_{language.ToUpper()}.txt";

            using (var st = asm.GetManifestResourceStream(resourcesName))
            {
                if (st == null)
                {
                    return;
                }

                using (var sr = new StreamReader(st))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var values = line.Split('|');
                            if (values.Length >= 2)
                            {
                                var zone = new Zone()
                                {
                                    ID = int.Parse(values[0]),
                                    Name = values[1].Trim()
                                };

                                newList.Add(zone);
                            }
                        }
                    }
                }

                AppLogger.Trace("zone list loaded.");

                // ユーザで任意に追加したゾーンを読み込む
                this.LoadZoneListAdded(newList);

                // 新しいゾーンリストをセットする
                this.zoneList = newList;

                // ゾーンリストを翻訳する
                this.TranslateZoneList();
            }
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

        private void TranslateZoneList()
        {
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
        }

        private volatile bool isMergedSkillList = false;
        private volatile bool isLoadedSkillToFFXIV = false;
        private volatile bool isMergedBuffList = false;

        /// <summary>
        /// XIVDBのスキルリストとFFXIVプラグインのスキルリストをマージする
        /// </summary>
        private void MergeSkillList()
        {
            if (!this.isMergedSkillList)
            {
                if (this.skillList.Any() &&
                    XIVDB.Instance.ActionList.Any())
                {
                    foreach (var action in XIVDB.Instance.ActionList)
                    {
                        var skill = new Skill()
                        {
                            ID = action.Key,
                            Name = action.Value.Name
                        };

                        this.skillList[action.Key] = skill;
                    }

                    this.isMergedSkillList = true;
                    AppLogger.Trace("XIVDB Action list merged.");
                }
            }

            // FFXIVプラグインにスキルリストを反映する
            this.LoadSkillToFFXIVPlugin();
        }

        private void LoadSkillToFFXIVPlugin()
        {
            if (this.isLoadedSkillToFFXIV)
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

            var pluginSkillList = list as SortedDictionary<int, string>;

            foreach (var entry in XIVDB.Instance.ActionList)
            {
                if (!pluginSkillList.ContainsKey(entry.Key))
                {
                    pluginSkillList[entry.Key] = entry.Value.Name;
                }
            }

            if (XIVDB.Instance.ActionList.Any())
            {
                this.isLoadedSkillToFFXIV = true;
                AppLogger.Trace("XIVDB Action list -> FFXIV Plugin");
            }
        }

        /// <summary>
        /// XIVDBのBuff(Status)リストとFFXIVプラグインのBuffリストをマージする
        /// </summary>
        private void MergeBuffList()
        {
            if (this.isMergedBuffList)
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

            var pluginList = list as SortedDictionary<int, string>;

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

            this.isMergedBuffList = true;
        }

        #endregion Resources

        #region Sub classes

        public class CombatantsByRole
        {
            public CombatantsByRole(
                Roles roleType,
                string roleLabel,
                IReadOnlyList<Combatant> combatants)
            {
                this.RoleType = roleType;
                this.RoleLabel = roleLabel;
                this.Combatants = combatants;
            }

            public IReadOnlyList<Combatant> Combatants { get; set; }
            public string RoleLabel { get; set; }
            public Roles RoleType { get; set; }
        }

        #endregion Sub classes
    }
}
