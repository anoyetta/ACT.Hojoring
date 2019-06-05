using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Config.Models;
using ACT.SpecialSpellTimer.RaidTimeline;
using ACT.SpecialSpellTimer.Sound;
using ACT.SpecialSpellTimer.Utility;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.FFXIVHelper;
using Prism.Mvvm;
using Sharlayan.Core.Enums;
using static ACT.SpecialSpellTimer.Sound.TTSDictionary;

namespace ACT.SpecialSpellTimer.Models
{
    public class TableCompiler :
        BindableBase
    {
        #region Singleton

        private static TableCompiler instance;

        public static TableCompiler Instance => instance ?? (instance = new TableCompiler());

        public static void Free() => instance = null;

        #endregion Singleton

        #region Worker

        private readonly double WorkerInterval = 3000;
        private System.Timers.Timer worker;

        #endregion Worker

        #region Begin / End

        public void Begin()
        {
            this.CompileSpells();
            this.CompileTickers();

            this.worker = new System.Timers.Timer();
            this.worker.AutoReset = true;
            this.worker.Interval = WorkerInterval;
            this.worker.Elapsed += (s, e) => this.DoWork();
            this.worker.Start();

            Logger.Write("start spell compiler.");
        }

        public void End()
        {
            this.worker?.Stop();
            this.worker?.Dispose();
            this.worker = null;
        }

        #endregion Begin / End

        public event EventHandler ZoneChanged;

        public event EventHandler CompileConditionChanged;

        private DateTime lastDumpPositionTimestamp = DateTime.MinValue;

        private void DoWork()
        {
            try
            {
                lock (this)
                {
                    this.RefreshCombatants();

                    var isSimulationChanged = false;
                    var isPlayerChanged = this.IsPlayerChanged();
                    var isPartyChanged = this.IsPartyChanged();
                    var isZoneChanged = this.IsZoneChanged();

                    if (this.previousInSimulation != this.InSimulation)
                    {
                        this.previousInSimulation = this.InSimulation;
                        isSimulationChanged = true;
                    }

                    if (isPlayerChanged)
                    {
                        this.RefreshPlayerPlacceholder();
                    }

                    if (isZoneChanged ||
                        isPartyChanged)
                    {
                        this.RefreshPartyPlaceholders();
                        this.RefreshPetPlaceholder();
                    }

                    if (isSimulationChanged ||
                        isPlayerChanged ||
                        isPartyChanged ||
                        isZoneChanged)
                    {
                        this.RecompileSpells();
                        this.RecompileTickers();

                        TickersController.Instance.GarbageWindows(this.TickerList);
                        SpellsController.Instance.GarbageSpellPanelWindows(this.SpellList);

                        this.CompileConditionChanged?.Invoke(this, new EventArgs());
                    }

                    if (isZoneChanged)
                    {
                        // インスタンススペルを消去する
                        SpellTable.Instance.RemoveInstanceSpellsAll();

                        // 設定を保存する
                        PluginCore.Instance?.SaveSettingsAsync();

                        var zone = ActGlobals.oFormActMain.CurrentZone;
                        var zoneID = FFXIVPlugin.Instance?.GetCurrentZoneID();
                        Logger.Write($"zone changed. zone={zone}, zone_id={zoneID}");
                        this.ZoneChanged?.Invoke(this, new EventArgs());

                        // 自分の座標をダンプする
                        Task.Run(() =>
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(5));
                            LogBuffer.DumpPosition(true);
                        });
                    }

                    // 定期的に自分の座標をダンプする
                    if ((DateTime.Now - this.lastDumpPositionTimestamp).TotalSeconds >= 60.0)
                    {
                        this.lastDumpPositionTimestamp = DateTime.Now;
                        LogBuffer.DumpPosition(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("table compiler error:", ex);
            }
        }

        #region Compilers

        public Combatant Player => this.player;

        public IList<Combatant> SortedPartyList => this.partyList;

        private List<Combatant> partyList = new List<Combatant>();

        private Combatant player = new Combatant();

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
        private (Combatant Player, IEnumerable<Combatant> PartyList, uint? ZoneID) GetCurrentFilterCondition()
        {
            var currentPlayer = this.player ?? new Combatant()
            {
                ID = 0,
                Name = "Dummy Player",
                Job = (byte)JobIDs.ADV,
            };

            var currentPartyList = this.partyList;

            var currentZoneID = default(uint?);
            lock (this.SimulationLocker)
            {
                if (this.InSimulation &&
                    this.SimulationZoneID != 0)
                {
                    currentZoneID = this.SimulationZoneID;
                }
                else
                {
                    currentZoneID = FFXIVPlugin.Instance?.GetCurrentZoneID();
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

        private readonly List<CharacterCondition> previousPartyCondition = new List<CharacterCondition>(32);
        private volatile CharacterCondition previousPlayerCondition = new CharacterCondition();
        private volatile uint previousZoneID = 0;
        private volatile string previousZoneName = string.Empty;
        private volatile bool previousInSimulation = false;

        public readonly object SimulationLocker = new object();

        public bool InSimulation
        {
            get;
            set;
        } = false;

        public Combatant SimulationPlayer
        {
            get;
            set;
        }

        public List<Combatant> SimulationParty
        {
            get;
            private set;
        } = new List<Combatant>();

        public uint SimulationZoneID
        {
            get;
            set;
        }

        public bool IsPartyChanged()
        {
            var r = false;

            var party = this.partyList
                .Where(x => x.ObjectType == Actor.Type.PC)
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
                Job = x.Job,
            }));

            return r;
        }

        public bool IsPlayerChanged()
        {
            var r = false;

            if (this.previousPlayerCondition.Name != this.player.Name ||
                this.previousPlayerCondition.Job != this.player.Job)
            {
                r = true;
            }

            this.previousPlayerCondition.Name = this.player.Name;
            this.previousPlayerCondition.Job = this.player.Job;

            return r;
        }

        public bool IsZoneChanged()
        {
            var r = false;

            var zoneID = default(uint?);
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

        private void RefreshCombatants()
        {
            var player = default(Combatant);
            var party = default(IReadOnlyList<Combatant>);

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
                    player = FFXIVPlugin.Instance?.GetPlayer();
                    party = FFXIVPlugin.Instance?.GetPartyList();
                }
            }

            if (player != null)
            {
                this.player = player;
            }

            if (party != null)
            {
                var newList = new List<Combatant>(party);

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

        #region プレースホルダに関するメソッド群

        private readonly PlaceholderContainer[] IDPlaceholders = new[]
        {
            new PlaceholderContainer("<id>", "[0-9a-fA-F]+", PlaceholderTypes.Custom),
            new PlaceholderContainer("<id4>", "[0-9a-fA-F]{4}", PlaceholderTypes.Custom),
            new PlaceholderContainer("<id8>", "[0-9a-fA-F]{8}", PlaceholderTypes.Custom),
        };

        private volatile List<PlaceholderContainer> placeholderList =
            new List<PlaceholderContainer>();

        public IReadOnlyList<PlaceholderContainer> PlaceholderList =>
            this.GetPlaceholders(
                this.InSimulation || TimelineManager.Instance.InSimulation,
                false)
            as List<PlaceholderContainer>;

        private object PlaceholderListSyncRoot =>
            ((ICollection)this.placeholderList)?.SyncRoot;

        public IEnumerable<PlaceholderContainer> GetPlaceholders(
            bool inSimulation = false,
            bool forTimeline = false)
        {
            var placeholders = default(IEnumerable<PlaceholderContainer>);
            lock (this.PlaceholderListSyncRoot)
            {
                placeholders = new List<PlaceholderContainer>(this.placeholderList);
            }

            // Simulationモードでなければ抜ける
            if (!inSimulation)
            {
                if (forTimeline)
                {
                    placeholders = placeholders.Select(x =>
                        new PlaceholderContainer(
                            x.Placeholder
                                .Replace("<", "[")
                                .Replace(">", "]"),
                            x.ReplaceString,
                            x.Type));
                }

                return placeholders;
            }

            // プレースホルダ生成用のメソッド
            string createPH(string name) => !forTimeline ? $"<{name}>" : $"[{name}]";

            // シミュレータ向けプレースホルダのリストを生成する
            var placeholdersInSim = new List<PlaceholderContainer>(placeholders);
#if DEBUG
            placeholdersInSim.Clear();
#endif
            // ID系プレースホルダ
            var idsInSim = new[]
            {
                new PlaceholderContainer(createPH("id"), @"([0-9a-fA-F]+|<id>|\[id\]|<id4>|\[id4\]|<id8>|\[id8\])", PlaceholderTypes.Custom),
                new PlaceholderContainer(createPH("id4"), @"([0-9a-fA-F]{4}|<id4>|\[id4\])", PlaceholderTypes.Custom),
                new PlaceholderContainer(createPH("id8"), @"([0-9a-fA-F]{8}|<id8>|\[id8\])", PlaceholderTypes.Custom)
            };

            var jobs = Enum.GetNames(typeof(JobIDs));
            var jobsPlacement = string.Join("|", jobs.Select(x => $@"\[{x}\]"));

            // JOB系プレースホルダ
            var jobsInSim = new List<PlaceholderContainer>();
            foreach (var job in jobs)
            {
                jobsInSim.Add(new PlaceholderContainer(createPH(job), $@"\[{job}\]", PlaceholderTypes.Party));
            }

            // PC系プレースホルダ
            var pcInSim = new[]
            {
                new PlaceholderContainer(createPH("mex"), @"(?<_mex>\[mex\])", PlaceholderTypes.Me),
                new PlaceholderContainer(createPH("nex"), $@"(?<_nex>{jobsPlacement}|\[nex\])", PlaceholderTypes.Party),
                new PlaceholderContainer(createPH("pc"), $@"(?<_pc>{jobsPlacement}|\[pc\]|\[mex\]|\[nex\])", PlaceholderTypes.Party),
            };

            // ID系を置き換える
            foreach (var ph in idsInSim)
            {
                var old = placeholdersInSim.FirstOrDefault(x => x.Placeholder == ph.Placeholder);
                if (old != null)
                {
                    old.ReplaceString = ph.ReplaceString;
                }
                else
                {
                    placeholdersInSim.Add(ph);
                }
            }

            // JOB系を追加する
            foreach (var ph in jobsInSim)
            {
                var old = placeholdersInSim.FirstOrDefault(x => x.Placeholder == ph.Placeholder);
                if (old != null)
                {
                    // NO-OP
                }
                else
                {
                    placeholdersInSim.Add(ph);
                }
            }

            // PC系を追加する
            foreach (var ph in pcInSim)
            {
                var old = placeholdersInSim.FirstOrDefault(x => x.Placeholder == ph.Placeholder);
                if (old != null)
                {
                    // NO-OP
                }
                else
                {
                    placeholdersInSim.Add(ph);
                }
            }

            return placeholdersInSim;
        }

        public void RefreshPartyPlaceholders()
        {
            // PC名辞書を更新する
            foreach (var pc in this.partyList)
            {
                PCNameDictionary.Instance.Add(pc.Name);
            }

            if (!Settings.Default.EnabledPartyMemberPlaceholder)
            {
                return;
            }

            var newList =
                new List<PlaceholderContainer>();

            // パーティメンバのいずれを示す <pc> を登録する
            var names = string.Join(
                "|",
                this.partyList.Select(x => x.NamesRegex).Concat(new[]
                {
                    @"\<pc\>",
                    @"\[pc\]",
                    @"\<mex\>",
                    @"\[mex\]",
                    @"\<nex\>",
                    @"\[nex\]",
                }));
            var oldValue = $"<pc>";
            var newValue = $"(?<_pc>{names})";
            newList.Add(new PlaceholderContainer(
                oldValue,
                newValue,
                PlaceholderTypes.Party));

            // FF14内部のPTメンバ自動ソート順で並び替える
            var partyListSorted =
                from x in this.SortedPartyList
                where
                x.ID != this.player.ID
                select
                x;

            // 自分以外のPTメンバを示す <nex> を登録する
            names = string.Join(
                "|",
                partyListSorted.Select(x => x.NamesRegex).Concat(new[]
                {
                    @"\<nex\>",
                    @"\[nex\]",
                }));
            oldValue = $"<nex>";
            newValue = $"(?<_nex>{names})";
            newList.Add(new PlaceholderContainer(
                oldValue,
                newValue,
                PlaceholderTypes.Party));

            // 通常のPTメンバ代名詞 <2>～<8> を登録する
            var index = 2;
            foreach (var combatant in partyListSorted)
            {
                newList.Add(new PlaceholderContainer(
                    $"<{index}>",
                    combatant.Name,
                    PlaceholderTypes.Party));

                newList.Add(new PlaceholderContainer(
                    $"<{index}ex>",
                    $"(?<_{index}ex>{combatant.NamesRegex})",
                    PlaceholderTypes.Party));

                index++;
            }

            // ジョブ名によるプレースホルダを登録する
            foreach (var job in Jobs.List)
            {
                // このジョブに該当するパーティメンバを抽出する
                var combatantsByJob = (
                    from x in this.partyList
                    where
                    x.Job == (int)job.ID
                    orderby
                    x.ID == this.player.ID ? 0 : 1,
                    x.ID descending
                    select
                    x).ToArray();

                if (!combatantsByJob.Any())
                {
                    continue;
                }

                // <JOBn>形式を登録する
                // ex. <PLD1> → Taro Paladin
                // ex. <PLD2> → Jiro Paladin
                for (int i = 0; i < combatantsByJob.Length; i++)
                {
                    newList.Add(new PlaceholderContainer(
                        $"<{job.ID.ToString().ToUpper()}{i + 1}>",
                        $"(?<_{job.ID.ToString().ToUpper()}{i + 1}>{ combatantsByJob[i].NamesRegex})",
                        PlaceholderTypes.Party));
                }

                // <JOB>形式を登録する ただし、この場合は正規表現のグループ形式とする
                // また、グループ名にはジョブの略称を設定する
                // ex. <PLD> → (?<_PLD>Taro Paladin|Jiro Paladin)
                names = string.Join(
                    "|",
                    combatantsByJob.Select(x => x.NamesRegex).Concat(new[]
                    {
                        $@"\<{job.ID.ToString().ToUpper()}\>",
                        $@"\[{job.ID.ToString().ToUpper()}\]",
                    }));
                oldValue = $"<{job.ID.ToString().ToUpper()}>";
                newValue = $"(?<_{job.ID.ToString().ToUpper()}>{names})";

                newList.Add(new PlaceholderContainer(
                    oldValue.ToUpper(),
                    newValue,
                    PlaceholderTypes.Party));
            }

            // ロールによるプレースホルダを登録する
            // ex. <TANK>   -> (?<_TANK>Taro Paladin|Jiro Paladin)
            // ex. <HEALER> -> (?<_HEALER>Taro Paladin|Jiro Paladin)
            // ex. <DPS>    -> (?<_DPS>Taro Paladin|Jiro Paladin)
            // ex. <MELEE>  -> (?<_MELEE>Taro Paladin|Jiro Paladin)
            // ex. <RANGE>  -> (?<_RANGE>Taro Paladin|Jiro Paladin)
            // ex. <MAGIC>  -> (?<_MAGIC>Taro Paladin|Jiro Paladin)
            var partyListByRole = FFXIVPlugin.Instance.GetPatryListByRole();
            foreach (var role in partyListByRole)
            {
                names = string.Join("|", role.Combatants.Select(x => x.NamesRegex).ToArray());
                oldValue = $"<{role.RoleLabel}>";
                newValue = $"(?<_{role.RoleLabel}>{names})";

                newList.Add(new PlaceholderContainer(
                    oldValue.ToUpper(),
                    newValue,
                    PlaceholderTypes.Party));
            }

            // <RoleN>形式のプレースホルダを登録する
            foreach (var role in partyListByRole)
            {
                for (int i = 0; i < role.Combatants.Count; i++)
                {
                    var label = $"{role.RoleLabel}{i + 1}";
                    var o = $"<{label}>";
                    var n = $"(?<_{label}>{role.Combatants[i].NamesRegex})";

                    newList.Add(new PlaceholderContainer(
                        o.ToUpper(),
                        n,
                        PlaceholderTypes.Party));
                }
            }

            // 新しく生成したプレースホルダを登録する
            lock (this.PlaceholderListSyncRoot)
            {
                this.placeholderList.RemoveAll(x => x.Type == PlaceholderTypes.Party);
                this.placeholderList.AddRange(newList);

                // ついでにID系プレースホルダを登録する
                var toAdds = IDPlaceholders.Where(x => !this.placeholderList.Any(y => y.Placeholder == x.Placeholder));
                this.placeholderList.AddRange(toAdds);
            }
        }

        public void RefreshPetPlaceholder()
        {
            if (!Settings.Default.EnabledPartyMemberPlaceholder)
            {
                return;
            }

            var playerJob = this.player.AsJob();
            if (playerJob != null &&
                !playerJob.IsSummoner())
            {
                return;
            }

            void refreshPetID()
            {
                // 3秒毎に30秒間判定させる
                const int Interval = 3;
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        var combatants = FFXIVPlugin.Instance.GetCombatantList();
                        if (combatants == null)
                        {
                            continue;
                        }

                        var pet = (
                            from x in combatants
                            where
                            x.OwnerID == this.player.ID &&
                            (
                                x.Name.Contains("フェアリー・") ||
                                x.Name.Contains("・エギ") ||
                                x.Name.Contains("カーバンクル・")
                            )
                            select
                            x).FirstOrDefault();

                        if (pet != null)
                        {
                            lock (this.PlaceholderListSyncRoot)
                            {
                                this.placeholderList.RemoveAll(x => x.Type == PlaceholderTypes.Pet);
                                this.placeholderList.Add(new PlaceholderContainer(
                                    "<petid>",
                                    Convert.ToString((long)((ulong)pet.ID), 16).ToUpper(),
                                    PlaceholderTypes.Pet));
                            }

                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Write("refresh petid error:", ex);
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(Interval));
                }
            }

            Task.Run(() => refreshPetID())
                .ContinueWith((task) =>
                {
                    this.RecompileSpells();
                    this.RecompileTickers();
                });
        }

        public void RefreshPlayerPlacceholder()
        {
            if (string.IsNullOrEmpty(this.player.Name))
            {
                return;
            }

            lock (this.PlaceholderListSyncRoot)
            {
                this.placeholderList.RemoveAll(x => x.Type == PlaceholderTypes.Me);
                this.placeholderList.Add(new PlaceholderContainer(
                    "<me>",
                    this.player.Name,
                    PlaceholderTypes.Me));

                this.placeholderList.Add(new PlaceholderContainer(
                    "<mex>",
                    $@"(?<_mex>{this.player.NamesRegex}|\<mex\>|\[mex\])",
                    PlaceholderTypes.Me));
            }
        }

        #endregion プレースホルダに関するメソッド群

        #region カスタムプレースホルダに関するメソッド群

        /// <summary>
        /// カスタムプレースホルダーを削除する
        /// <param name="name">削除するプレースホルダーの名称</param>
        /// </summary>
        public void ClearCustomPlaceholder(string name)
        {
            lock (this.PlaceholderListSyncRoot)
            {
                this.placeholderList.RemoveAll(x =>
                    x.Placeholder == $"<{name}>" &&
                    x.Type == PlaceholderTypes.Custom);
            }

            this.RecompileSpells();
            this.RecompileTickers();
        }

        /// <summary>
        /// カスタムプレースホルダーを全て削除する
        /// </summary>
        public void ClearCustomPlaceholderAll()
        {
            lock (this.PlaceholderListSyncRoot)
            {
                this.placeholderList.RemoveAll(x =>
                    x.Type == PlaceholderTypes.Custom);
            }

            this.RecompileSpells();
            this.RecompileTickers();
        }

        /// <summary>
        /// カスタムプレースホルダーに追加する
        /// </summary>
        /// <param name="name">追加するプレースホルダーの名称</param>
        /// <paramname="value">置換する文字列</param>
        public void SetCustomPlaceholder(string name, string value)
        {
            lock (this.PlaceholderListSyncRoot)
            {
                this.placeholderList.Add(new PlaceholderContainer(
                    $"<{name}>",
                    value,
                    PlaceholderTypes.Custom));
            }

            this.RecompileSpells();
            this.RecompileTickers();
        }

        #endregion カスタムプレースホルダに関するメソッド群

        #region Sub classes

        public class CharacterCondition
        {
            public string Name { get; set; }
            public byte Job { get; set; }
        }

        public class PlaceholderContainer
        {
            public PlaceholderContainer(
                string placeholder,
                string replaceString,
                PlaceholderTypes type)
            {
                this.Placeholder = placeholder;
                this.ReplaceString = replaceString;
                this.Type = type;
            }

            public string Placeholder { get; set; }
            public string ReplaceString { get; set; }
            public PlaceholderTypes Type { get; set; }
        }

        private class RegexEx
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

        #endregion Sub classes
    }
}
