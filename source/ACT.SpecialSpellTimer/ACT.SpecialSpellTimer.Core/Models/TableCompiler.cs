using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ACT.SpecialSpellTimer.Config.Models;
using ACT.SpecialSpellTimer.Sound;
using ACT.SpecialSpellTimer.Utility;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;
using Prism.Mvvm;
using static ACT.SpecialSpellTimer.Sound.TTSDictionary;

namespace ACT.SpecialSpellTimer.Models
{
    public partial class TableCompiler :
        BindableBase
    {
        #region Singleton

        private static TableCompiler instance;

        public static TableCompiler Instance => instance ?? (instance = new TableCompiler());

        public static void Free() => instance = null;

        #endregion Singleton

        #region Worker

        private static readonly double CompileHandlerInterval = 6000;
        private ThreadWorker compileWorker;

        private static readonly double CombatantsSubscriber = 2000;
        private ThreadWorker combatantsSubscriber;

        #endregion Worker

        #region Begin / End

        public void Begin()
        {
            this.CompileSpells();
            this.CompileTickers();

            this.SubscribeXIVPluginEvents();

            this.compileWorker = new ThreadWorker(
                this.TryCompile,
                CompileHandlerInterval,
                "Trigger compiler service",
                ThreadPriority.Lowest);

            this.compileWorker.Run();

            this.combatantsSubscriber = new ThreadWorker(
                this.TryWork,
                CombatantsSubscriber,
                "Combatants subscriber",
                ThreadPriority.Lowest);

            this.combatantsSubscriber.Run();

            Logger.Write("start trigger compiler.");
        }

        public void End()
        {
            if (this.compileWorker != null)
            {
                this.compileWorker.Abort();
                this.compileWorker = null;
            }

            if (this.combatantsSubscriber != null)
            {
                this.combatantsSubscriber.Abort();
                this.combatantsSubscriber = null;
            }
        }

        #endregion Begin / End

        public event EventHandler ZoneChanged;

        public event EventHandler CompileConditionChanged;

        private DateTime lastDumpPositionTimestamp = DateTime.MinValue;

        private bool isQueueRecompile = false;
        private bool isQueueZoneChange = false;
        private DateTime lastQueueTimestamp = DateTime.MaxValue;

        private void SubscribeXIVPluginEvents()
        {
            Task.Run(async () =>
            {
                var helper = XIVPluginHelper.Instance;

                for (int i = 0; i < 60; i++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));

                    if (helper.IsAttached)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(0.2));

                        helper.OnPrimaryPlayerChanged += () => enqueueRecompile();
                        helper.OnPlayerJobChanged += () => enqueueRecompile();
                        helper.OnPartyListChanged += (_, _) => enqueueRecompile();
                        helper.OnPlayerJobChanged += () => enqueueRecompile();
                        helper.OnZoneChanged += (_, _) => enqueueByZoneChanged();

                        enqueueByZoneChanged();
                        break;
                    }
                }
            });

            void enqueueRecompile()
            {
                lock (this)
                {
                    this.isQueueRecompile = true;
                    this.lastQueueTimestamp = DateTime.Now;
                }
            }

            void enqueueByZoneChanged()
            {
                lock (this)
                {
                    this.isQueueRecompile = true;
                    this.isQueueZoneChange = true;
                    this.lastQueueTimestamp = DateTime.Now;
                }
            }
        }

        private void TryCompile()
        {
            lock (this)
            {
                if ((DateTime.Now - this.lastQueueTimestamp).TotalMilliseconds <= CompileHandlerInterval)
                {
                    return;
                }

                this.lastQueueTimestamp = DateTime.MaxValue;

                this.RefreshCombatants();

                var isSimulationChanged = false;

                if (this.previousInSimulation != this.InSimulation)
                {
                    this.previousInSimulation = this.InSimulation;
                    isSimulationChanged = true;
                }

                if (isSimulationChanged || this.isQueueRecompile)
                {
                    if (this.isQueueRecompile)
                    {
                        this.RefreshPlayerPlacceholder();
                        this.RefreshPartyPlaceholders();
                        this.RefreshPetPlaceholder();
                    }

                    this.RecompileSpells();
                    this.RecompileTickers();

                    var fromEvent = isSimulationChanged ? "simulation changed" : "condition changed";
                    Logger.Write($"recompiled triggers on {fromEvent}");

                    TickersController.Instance.GarbageWindows(this.TickerList);
                    SpellsController.Instance.GarbageSpellPanelWindows(this.SpellList);

                    this.CompileConditionChanged?.Invoke(this, new EventArgs());

                    this.isQueueRecompile = false;
                }

                if (this.isQueueZoneChange)
                {
                    // インスタンススペルを消去する
                    SpellTable.Instance.RemoveInstanceSpellsAll();

                    // 設定を保存する
                    PluginCore.Instance?.SaveSettingsAsync();

                    var zone = ActGlobals.oFormActMain.CurrentZone;
                    var zoneID = XIVPluginHelper.Instance?.GetCurrentZoneID();
                    Logger.Write($"zone changed. zone={ActGlobals.oFormActMain.CurrentZone}, zone_id={XIVPluginHelper.Instance?.GetCurrentZoneID()}");
                    this.ZoneChanged?.Invoke(this, new EventArgs());

                    Task.Run(() =>
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(6));

                        // 自分の座標をダンプする
                        LogBuffer.DumpPosition(true);

                        // ETを出力する
                        var nowET = EorzeaTime.Now;
                        LogParser.RaiseLog(
                            DateTime.Now,
                            $"[EX] ZoneChanged ET{nowET.Hour:00}:00 Zone:{zoneID:000} {zone}");
                    });

                    this.isQueueZoneChange = false;
                }
            }
        }

        private void TryWork()
        {
            // 自分のペットとの距離をダンプする
            LogBuffer.DumpMyPetDistance();

            // 定期的に自分の座標をダンプする
            if ((DateTime.Now - this.lastDumpPositionTimestamp).TotalSeconds >= 60.0)
            {
                this.lastDumpPositionTimestamp = DateTime.Now;
                LogBuffer.DumpPosition(true);
            }
        }

        #region Compilers

        public CombatantEx Player => this.player;

        public IList<CombatantEx> SortedPartyList => this.partyList;

        private List<CombatantEx> partyList = new List<CombatantEx>();

        private CombatantEx player = new CombatantEx();

        private List<Spell> spellList = new List<Spell>();

        private object spellListLocker = new object();

        private List<Ticker> tickerList = new List<Ticker>();

        private object tickerListLocker = new object();

        private readonly List<ITrigger> triggerList = new List<ITrigger>(128);

        public event EventHandler OnTableChanged;

        public List<Spell> SpellList
        {
            get
            {
                lock (this.spellListLocker)
                {
                    return new List<Spell>(this.spellList);
                }
            }
        }

        public List<Ticker> TickerList
        {
            get
            {
                lock (this.tickerListLocker)
                {
                    return new List<Ticker>(this.tickerList);
                }
            }
        }

        public IReadOnlyList<ITrigger> TriggerList
        {
            get
            {
                lock (this.triggerList)
                {
                    return this.triggerList.ToList();
                }
            }
        }

        public void AddTestTrigger(
            ITrigger testTrigger)
        {
            lock (this.triggerList)
            {
                if (!this.triggerList.Any(x =>
                    x.GetID() == testTrigger.GetID()))
                {
                    this.triggerList.Add(testTrigger);
                }
            }
        }

        public void AddSpell(
            Spell instancedSpell)
        {
            lock (this.spellListLocker)
            {
                this.spellList.Add(instancedSpell);
            }

            lock (this.triggerList)
            {
                this.triggerList.Add(instancedSpell);
            }
        }

        public void RemoveSpell(
            Spell instancedSpell)
        {
            lock (this.spellListLocker)
            {
                this.spellList.Remove(instancedSpell);
            }

            lock (this.triggerList)
            {
                this.triggerList.Remove(instancedSpell);
            }
        }

        public void RemoveInstanceSpells()
        {
            lock (this.spellListLocker)
            {
                this.spellList.RemoveAll(x => x.IsInstance);
            }

            lock (this.triggerList)
            {
                this.triggerList.RemoveAll(x =>
                    (x is Spell spell) &&
                    spell.IsInstance);
            }
        }

        /// <summary>
        /// トリガのコンパイル（フィルタ）向けの現在情報を取得する
        /// </summary>
        /// <returns>
        /// プレイヤー, パーティリスト, ゾーンID</returns>
        private (CombatantEx Player, IEnumerable<CombatantEx> PartyList, int? ZoneID) GetCurrentFilterCondition()
        {
            var currentPlayer = this.player ?? new CombatantEx()
            {
                ID = 0,
                Name = "Dummy Player",
                Job = (byte)JobIDs.ADV,
            };

            var currentPartyList = this.partyList;

            var currentZoneID = default(int?);
            lock (this.SimulationLocker)
            {
                if (this.InSimulation &&
                    this.SimulationZoneID != 0)
                {
                    currentZoneID = this.SimulationZoneID;
                }
                else
                {
                    currentZoneID = XIVPluginHelper.Instance?.GetCurrentZoneID();
                }
            }

            return (currentPlayer, currentPartyList, currentZoneID);
        }

        /// <summary>
        /// スペルをコンパイルする
        /// </summary>
        public void CompileSpells()
        {
            var current = this.GetCurrentFilterCondition();

            var query =
                from x in SpellTable.Instance.Table
                where
                x.IsDesignMode ||
                (
                    x.Enabled &&

                    // フィルタを判定する
                    x.PredicateFilters(current.Player, current.PartyList, current.ZoneID)
                )
                orderby
                x.Panel?.PanelName,
                x.DisplayNo,
                x.ID
                select
                x;

            var prevSpells = default(Spell[]);
            lock (this.spellListLocker)
            {
                prevSpells = this.spellList.ToArray();
                this.spellList.Clear();
                this.spellList.AddRange(query);
            }

            // 統合トリガリストに登録する
            lock (this.triggerList)
            {
                this.triggerList.RemoveAll(x => x.ItemType == ItemTypes.Spell);
                this.triggerList.AddRange(this.spellList);
            }

            // コンパイルする
            this.spellList.AsParallel().ForAll(spell =>
            {
                var ex1 = spell.CompileRegex();
                Thread.Yield();
                var ex2 = spell.CompileRegexExtend1();
                Thread.Yield();
                var ex3 = spell.CompileRegexExtend2();
                Thread.Yield();
                var ex4 = spell.CompileRegexExtend3();
                Thread.Yield();

                var ex = ex1 ?? ex2 ?? ex3 ?? ex4 ?? null;
                if (ex != null)
                {
                    Logger.Write(
                        $"Regex compile error! spell={spell.SpellTitle}",
                        ex);
                }

                Thread.Sleep(1);
            });

            // 無効になったスペルを停止する
            prevSpells?.Where(x => !this.spellList.Contains(x)).AsParallel().ForAll(spell =>
            {
                spell.MatchDateTime = DateTime.MinValue;
                spell.UpdateDone = false;
                spell.OverDone = false;
                spell.BeforeDone = false;
                spell.TimeupDone = false;
                spell.CompleteScheduledTime = DateTime.MinValue;

                spell.StartOverSoundTimer();
                spell.StartBeforeSoundTimer();
                spell.StartTimeupSoundTimer();

                Thread.Yield();
            });

            this.RaiseTableChenged();
        }

        /// <summary>
        /// テロップをコンパイルする
        /// </summary>
        public void CompileTickers()
        {
            var current = this.GetCurrentFilterCondition();

            var query =
                from x in TickerTable.Instance.Table
                where
                x.IsDesignMode ||
                (
                    x.Enabled &&

                    // フィルタを判定する
                    x.PredicateFilters(current.Player, current.PartyList, current.ZoneID)
                )
                orderby
                x.MatchDateTime descending,
                x.ID
                select
                x;

            var prevSpells = default(Ticker[]);
            lock (this.tickerListLocker)
            {
                prevSpells = this.tickerList.ToArray();
                this.tickerList.Clear();
                this.tickerList.AddRange(query);
            }

            // 統合トリガリストに登録する
            lock (this.triggerList)
            {
                this.triggerList.RemoveAll(x => x.ItemType == ItemTypes.Ticker);
                this.triggerList.AddRange(this.tickerList);
            }

            // コンパイルする
            this.tickerList.AsParallel().ForAll(spell =>
            {
                var ex1 = spell.CompileRegex();
                Thread.Yield();
                var ex2 = spell.CompileRegexToHide();
                Thread.Yield();

                var ex = ex1 ?? ex2 ?? null;
                if (ex != null)
                {
                    Logger.Write(
                        $"Regex compile error! ticker={spell.Title}",
                        ex);
                }

                Thread.Sleep(1);
            });

            // 無効になったスペルを停止する
            prevSpells?.Where(x => !this.tickerList.Contains(x)).AsParallel().ForAll(spell =>
            {
                spell.MatchDateTime = DateTime.MinValue;
                spell.Delayed = false;
                spell.ForceHide = false;

                spell.StartDelayedSoundTimer();

                Thread.Yield();
            });

            this.RaiseTableChenged();
        }

        public void RaiseTableChenged() =>
            this.OnTableChanged?.Invoke(this, new EventArgs());

        public void RecompileSpells()
        {
            lock (this)
            {
                var rawTable = new List<Spell>(SpellTable.Instance.Table);
                rawTable.AsParallel().ForAll(spell =>
                {
                    spell.KeywordReplaced = string.Empty;
                    spell.KeywordForExtendReplaced1 = string.Empty;
                    spell.KeywordForExtendReplaced2 = string.Empty;
                    spell.KeywordForExtendReplaced3 = string.Empty;
                    spell.Regex = null;
                    spell.RegexPattern = string.Empty;
                    spell.RegexForExtend1 = null;
                    spell.RegexForExtendPattern1 = string.Empty;
                    spell.RegexForExtend2 = null;
                    spell.RegexForExtendPattern2 = string.Empty;
                    spell.RegexForExtend3 = null;
                    spell.RegexForExtendPattern3 = string.Empty;
                });

                this.CompileSpells();

                // スペルタイマの描画済みフラグを落とす
                SpellTable.Instance.ClearUpdateFlags();
            }
        }

        public void RecompileTickers()
        {
            lock (this)
            {
                var rawTable = new List<Ticker>(TickerTable.Instance.Table);
                rawTable.AsParallel().ForAll(spell =>
                {
                    spell.KeywordReplaced = string.Empty;
                    spell.KeywordToHideReplaced = string.Empty;
                    spell.Regex = null;
                    spell.RegexPattern = string.Empty;
                    spell.RegexToHide = null;
                    spell.RegexPatternToHide = string.Empty;
                });

                this.CompileTickers();
            }
        }

        public string GetMatchingKeyword(
            string destinationKeyword,
            string sourceKeyword)
        {
            if (string.IsNullOrEmpty(sourceKeyword))
            {
                return string.Empty;
            }

            if (!sourceKeyword.Contains("<") ||
                !sourceKeyword.Contains(">"))
            {
                return sourceKeyword;
            }

            var placeholders = this.GetPlaceholders(this.InSimulation, false);

            string replace(string text)
            {
                var r = text;

                foreach (var p in placeholders)
                {
                    r = r.Replace(p.Placeholder, p.ReplaceString);
                }

                return r;
            }

            if (string.IsNullOrEmpty(destinationKeyword))
            {
                var newKeyword = sourceKeyword;
                newKeyword = replace(newKeyword);

                return newKeyword;
            }

            return destinationKeyword;
        }

        private RegexEx GetRegex(
            Regex destinationRegex,
            string destinationPattern,
            string sourceKeyword)
        {
            var newRegex = destinationRegex;
            var newPattern = destinationPattern;

            var sourcePattern = sourceKeyword.ToRegexPattern();

            if (!string.IsNullOrEmpty(sourcePattern))
            {
                if (destinationRegex == null ||
                    string.IsNullOrEmpty(destinationPattern) ||
                    destinationPattern != sourcePattern)
                {
                    newRegex = sourcePattern.ToRegex();
                    newPattern = sourcePattern;
                }
            }

            return new RegexEx(newRegex, newPattern);
        }

        #endregion Compilers

        #region 条件の変更を判定するメソッド群

        private volatile bool previousInSimulation = false;

        public readonly object SimulationLocker = new object();

        public bool InSimulation
        {
            get;
            set;
        } = false;

        public CombatantEx SimulationPlayer
        {
            get;
            set;
        }

        public List<CombatantEx> SimulationParty
        {
            get;
            private set;
        } = new List<CombatantEx>();

        public int SimulationZoneID
        {
            get;
            set;
        }

#if false
        public bool IsPartyChanged()
        {
            var r = false;

            var party = this.partyList
                .Where(x => x.ActorType == Actor.Type.PC)
                .ToList();

            if (this.previousPartyCondition.Count !=
                party.Count)
            {
                r = true;
            }
            else
            {
                // 前のパーティと名前とジョブが一致するか検証する
                var count = party
                    .Where(x =>
                        this.previousPartyCondition.Any(y =>
                            y.Name == x.Name &&
                            y.Job == x.Job))
                    .Count();

                if (party.Count != count)
                {
                    r = true;
                }
            }

            this.previousPartyCondition.Clear();
            this.previousPartyCondition.AddRange(party.Select(x => new CharacterCondition()
            {
                Name = x.Name,
                Job = (byte)x.Job,
            }));

            return r;
        }
#endif

#if false
        public bool IsPlayerChanged()
        {
            var r = false;

            if (this.previousPlayerCondition.Name != this.player.Name ||
                this.previousPlayerCondition.Job != this.player.Job)
            {
                r = true;
            }

            this.previousPlayerCondition.Name = this.player.Name;
            this.previousPlayerCondition.Job = (byte)this.player.Job;

            return r;
        }
#endif

#if false
        public bool IsZoneChanged()
        {
            var r = false;

            var zoneID = default(int?);
            var zoneName = string.Empty;

            lock (this.SimulationLocker)
            {
                if (this.InSimulation &&
                    this.SimulationZoneID != 0)
                {
                    zoneID = this.SimulationZoneID;
                    zoneName = "In Simulation";
                }
                else
                {
                    zoneID = FFXIVPlugin.Instance?.GetCurrentZoneID();
                    zoneName = ActGlobals.oFormActMain.CurrentZone;
                }
            }

            if (zoneID != null &&
                this.previousZoneID != zoneID &&
                this.previousZoneName != zoneName)
            {
                r = true;

                this.previousZoneID = zoneID ?? 0;
                this.previousZoneName = zoneName;
            }

            return r;
        }
#endif

        public void RefreshCombatants()
        {
            var player = default(CombatantEx);
            var party = default(IEnumerable<CombatantEx>);

            lock (SimulationLocker)
            {
                if (this.InSimulation &&
                    this.SimulationPlayer != null &&
                    this.SimulationParty.Any())
                {
                    player = this.SimulationPlayer;
                    party = this.SimulationParty;
                }
                else
                {
                    player = CombatantsManager.Instance.Player;
                    party = CombatantsManager.Instance.GetPartyList();
                }
            }

            if (player != null)
            {
                this.player = player;
            }

            if (party != null)
            {
                var newList = new List<CombatantEx>(party);

                if (newList.Count < 1 &&
                    !string.IsNullOrEmpty(this.player?.Name))
                {
                    newList.Add(this.player);
                }

                // パーティリストを入れ替える
                this.partyList.Clear();
                this.partyList.AddRange(newList);

                // 読み仮名リストをメンテナンスする
                var newPhonetics =
                    from x in newList
                    select new PCPhonetic()
                    {
                        ID = x.ID,
                        NameFI = x.NameFI,
                        NameIF = x.NameIF,
                        NameII = x.NameII,
                        Name = x.Name,
                        JobID = x.JobID,
                    };

                WPFHelper.BeginInvoke(() =>
                {
                    var phonetics = TTSDictionary.Instance.Phonetics;

                    var toAdd = newPhonetics.Where(x => !phonetics.Any(y => y.Name == x.Name));
                    phonetics.AddRange(toAdd);

                    var toRemove = phonetics.Where(x => !newPhonetics.Any(y => y.Name == x.Name)).ToArray();
                    foreach (var item in toRemove)
                    {
                        phonetics.Remove(item);
                    }
                });
            }
        }

        #endregion 条件の変更を判定するメソッド群

        #region Sub classes

        public class CharacterCondition
        {
            public string Name { get; set; }
            public byte Job { get; set; }
        }

        #endregion Sub classes
    }

    public class RegexEx
    {
        public RegexEx(
            Regex regex,
            string regexPattern)
        {
            this.Regex = regex;
            this.RegexPattern = regexPattern;
        }

        public Regex Regex { get; set; }
        public string RegexPattern { get; set; }
    }
}
