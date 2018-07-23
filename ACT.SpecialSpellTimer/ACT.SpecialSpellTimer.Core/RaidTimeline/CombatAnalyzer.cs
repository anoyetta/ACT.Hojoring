using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Data;
using System.Windows.Media;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Utility;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.FFXIVHelper;
using FFXIV.Framework.Globalization;
using NPOI.SS.UserModel;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    #region Enum

    /// <summary>
    /// 分析キーワードの分類
    /// </summary>
    public enum KewordTypes
    {
        Unknown = 0,
        Record,
        Me,
        PartyMember,
        Pet,
        Cast,
        CastStartsUsing,
        Action,
        Effect,
        Marker,
        Dialogue,
        HPRate,
        Added,
        Start,
        End,
        TimelineStart,
    }

    /// <summary>
    /// 戦闘ログの種類
    /// </summary>
    public enum LogTypes
    {
        Unknown = 0,
        CombatStart,
        CombatEnd,
        CastStart,
        Action,
        Effect,
        Marker,
        Added,
        HPRate,
        Dialog,
    }

    public static class LogTypesExtensions
    {
        private static ColorConverter cc = new ColorConverter();

        public static string ToText(
            this LogTypes t)
            => new[]
            {
                "UNKNOWN",
                "Combat Start",
                "Combat End",
                "Starts Using",
                "Action",
                "Effect",
                "Marker",
                "Added",
                "HP Rate",
                "Dialog",
            }[(int)t];

        public static Color ToBackgroundColor(
            this LogTypes t)
            => new[]
            {
                /*
                (Color)cc.ConvertFrom("#FFFFFFFF"), // UNKNOWN
                (Color)cc.ConvertFrom("#FFFF9999"), // Combat Start
                (Color)cc.ConvertFrom("#FFFF9999"), // Combat End
                (Color)cc.ConvertFrom("#FFCEE6F4"), // Starts Using
                (Color)cc.ConvertFrom("#FFEEFBDD"), // Action
                (Color)cc.ConvertFrom("#FFEFF6FB"), // Effect
                (Color)cc.ConvertFrom("#FFFFDA97"), // Marker
                (Color)cc.ConvertFrom("#FFFFFFFF"), // Added
                (Color)cc.ConvertFrom("#FFFFFFFF"), // HP Rate
                (Color)cc.ConvertFrom("#FFD8D8D8"), // Dialog
                */
                (Color)cc.ConvertFrom("#FFFFFFFF"), // UNKNOWN
                (Color)cc.ConvertFrom("#FFDC143C"), // Combat Start
                (Color)cc.ConvertFrom("#FFDC143C"), // Combat End
                (Color)cc.ConvertFrom("#FF4169E1"), // Starts Using
                (Color)cc.ConvertFrom("#FF98fb98"), // Action
                (Color)cc.ConvertFrom("#FFF0E68C"), // Effect
                (Color)cc.ConvertFrom("#FFFFA500"), // Marker
                (Color)cc.ConvertFrom("#00000000"), // Added
                (Color)cc.ConvertFrom("#00000000"), // HP Rate
                (Color)cc.ConvertFrom("#FFD8D8D8"), // Dialog
            }[(int)t];

        public static Color ToForegroundColor(
            this LogTypes t)
            => new[]
            {
                (Color)cc.ConvertFrom("#FF000000"), // UNKNOWN
                (Color)cc.ConvertFrom("#FF000000"), // Combat Start
                (Color)cc.ConvertFrom("#FF000000"), // Combat End
                (Color)cc.ConvertFrom("#FF000000"), // Starts Using
                (Color)cc.ConvertFrom("#FF000000"), // Action
                (Color)cc.ConvertFrom("#FF000000"), // Effect
                (Color)cc.ConvertFrom("#FF000000"), // Marker
                (Color)cc.ConvertFrom("#FF7F7F7F"), // Added
                (Color)cc.ConvertFrom("#FF000000"), // HP Rate
                (Color)cc.ConvertFrom("#FF000000"), // Dialog
            }[(int)t];
    }

    #endregion Enum

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

        #region Keywords

        public const string Wipeout = "wipeout";
        public const string ImportLog = "00:0000:import";
        public static readonly string WipeoutLog = $"00:0000:{Wipeout}";

        public static Regex ActionRegex => AnalyzeRegexes[nameof(ActionRegex)];
        public static Regex AddedRegex => AnalyzeRegexes[nameof(AddedRegex)];
        public static Regex CastRegex => AnalyzeRegexes[nameof(CastRegex)];
        public static Regex HPRateRegex => AnalyzeRegexes[nameof(HPRateRegex)];
        public static Regex StartsUsingRegex => AnalyzeRegexes[nameof(StartsUsingRegex)];
        public static Regex StartsUsingUnknownRegex => AnalyzeRegexes[nameof(StartsUsingUnknownRegex)];
        public static Regex DialogRegex => AnalyzeRegexes[nameof(DialogRegex)];
        public static Regex CombatStartRegex => AnalyzeRegexes[nameof(CombatStartRegex)];
        public static Regex CombatEndRegex => AnalyzeRegexes[nameof(CombatEndRegex)];
        public static Regex EffectRegex => AnalyzeRegexes[nameof(EffectRegex)];
        public static Regex MarkerRegex => AnalyzeRegexes[nameof(MarkerRegex)];
        public static Regex MarkingRegex => AnalyzeRegexes[nameof(MarkingRegex)];

        public static string CombatStartNow
        {
            get
            {
                var keyword = default(string);

                switch (Settings.Default.FFXIVLocale)
                {
                    case Locales.JA:
                        keyword = CombatStartNowJA;
                        break;

                    case Locales.KO:
                        keyword = CombatStartNowKO;
                        break;

                    case Locales.EN:
                    case Locales.FR:
                    case Locales.DE:
                    default:
                        keyword = CombatStartNowEN;
                        break;
                }

                return keyword;
            }
        }

        public static IList<AnalyzeKeyword> Keywords
        {
            get
            {
                var keywords = default(IList<AnalyzeKeyword>);

                switch (Settings.Default.FFXIVLocale)
                {
                    case Locales.JA:
                        keywords = KeywordsJA;
                        break;

                    case Locales.KO:
                        keywords = KeywordsKO;
                        break;

                    case Locales.EN:
                    case Locales.FR:
                    case Locales.DE:
                    default:
                        keywords = KeywordsEN;
                        break;
                }

                return keywords;
            }
        }

        private static IDictionary<string, Regex> AnalyzeRegexes
        {
            get
            {
                var regexes = default(IDictionary<string, Regex>);

                switch (Settings.Default.FFXIVLocale)
                {
                    case Locales.JA:
                        regexes = AnalyzeRegexedJA;
                        break;

                    case Locales.KO:
                        regexes = AnalyzeRegexedKO;
                        break;

                    case Locales.EN:
                    case Locales.FR:
                    case Locales.DE:
                    default:
                        regexes = AnalyzeRegexedEN;
                        break;
                }

                return regexes;
            }
        }

        private static Regex CreateRegex(string pattern)
            => new Regex(
                pattern,
                RegexOptions.Compiled |
                RegexOptions.ExplicitCapture);

        #region JA

        public const string CombatStartNowJA = "0039:戦闘開始！";

        public static readonly IList<AnalyzeKeyword> KeywordsJA = new[]
        {
            new AnalyzeKeyword() { Keyword = "[EX] Added", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "[EX] POS", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "[EX] Beacon", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "・エギ", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "フェアリー・", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "カーバンクル・", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "オートタレット", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "デミ・バハムート", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "アーサリースター", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "を唱えた。", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "の構え。", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "starts using", Category = KewordTypes.Cast },
            /*
            new AnalyzeKeyword() { Keyword = "starts using", Category = KewordTypes.CastStartsUsing },
            */
            new AnalyzeKeyword() { Keyword = "HP at", Category = KewordTypes.HPRate },
            new AnalyzeKeyword() { Keyword = "[EX] Added new combatant", Category = KewordTypes.Added },
            new AnalyzeKeyword() { Keyword = "] 1B:", Category = KewordTypes.Marker },
            new AnalyzeKeyword() { Keyword = "「マーキング」", Category = KewordTypes.Marker },
            new AnalyzeKeyword() { Keyword = "] 1A:", Category = KewordTypes.Effect },
            new AnalyzeKeyword() { Keyword = "00:0044:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = "00:0839:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = ImportLog, Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "/spespetime -a start", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:戦闘開始", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:戦闘開始まで5秒！", Category = KewordTypes.TimelineStart },
            new AnalyzeKeyword() { Keyword = "/spespetime -a end", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "の攻略を終了した。", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "ロットを行ってください。", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = WipeoutLog, Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "「", Category = KewordTypes.Action },
            new AnalyzeKeyword() { Keyword = "」", Category = KewordTypes.Action },
        };

        public static readonly Dictionary<string, Regex> AnalyzeRegexedJA = new Dictionary<string, Regex>()
        {
            {
                nameof(ActionRegex),
                CreateRegex(@"\[.+?\] 00:....:(?<actor>.+?)の「(?<skill>.+?)」$")
            },
            {
                nameof(AddedRegex),
                CreateRegex(@":[EX] Added new combatant. name=(?<actor>.+) X=")
            },
            {
                nameof(CastRegex),
                CreateRegex(@"\[.+?\] 00:....:(?<actor>.+?)は「(?<skill>.+?)」(を唱えた。|の構え。)$")
            },
            {
                nameof(HPRateRegex),
                CreateRegex(@"\[.+?\] ..:(?<actor>.+?) HP at (?<hprate>\d+?)%")
            },
            {
                nameof(StartsUsingRegex),
                CreateRegex(@"14:....:(?<actor>.+?) starts using (?<skill>.+?) on (?<target>.+?)\.$")
            },
            {
                nameof(StartsUsingUnknownRegex),
                CreateRegex(@"14:....:(?<actor>.+?) starts using (?<skill>.+?) on Unknown\.$")
            },
            {
                nameof(DialogRegex),
                CreateRegex(@"00:(0044|0839):(?<dialog>.+?)$")
            },
            {
                nameof(CombatStartRegex),
                CreateRegex(@"00:(0038|0039):(?<discription>.+?)$")
            },
            {
                nameof(CombatEndRegex),
                CreateRegex(@"00:....:(?<discription>.+?)$")
            },
            {
                nameof(EffectRegex),
                CreateRegex(@"1A:(?<victim>.+?) gains the effect of (?<effect>.+?) from (?<actor>.+?) for (?<duration>[0-9\.]*?) Seconds.$")
            },
            {
                nameof(MarkerRegex),
                CreateRegex(@"1B:(?<id>.{8}):(?<target>.+?):0000:....:(?<type>....):0000:0000:0000:$")
            },
            {
                nameof(MarkingRegex),
                CreateRegex(@"00:(?<id>....):(?<target>.+?)に「マーキング」の効果。")
            },
        };

        #endregion JA

        #region EN

        public const string CombatStartNowEN = "0039:Engage!";

        public static readonly IList<AnalyzeKeyword> KeywordsEN = new[]
        {
            new AnalyzeKeyword() { Keyword = "[EX] Added", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "[EX] POS", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "[EX] Beacon", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "-Egi", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Eos", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Selene", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Carbuncle", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Autoturret", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Demi-Bahamut", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Earthly Star", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "begins casting", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "readies", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "starts using", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "HP at", Category = KewordTypes.HPRate },
            new AnalyzeKeyword() { Keyword = "[EX] Added new combatant", Category = KewordTypes.Added },
            new AnalyzeKeyword() { Keyword = "] 1B:", Category = KewordTypes.Marker },
            new AnalyzeKeyword() { Keyword = "suffers the effect of Prey.", Category = KewordTypes.Marker },
            new AnalyzeKeyword() { Keyword = "] 1A:", Category = KewordTypes.Effect },
            new AnalyzeKeyword() { Keyword = "00:0044:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = "00:0839:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = ImportLog, Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "/spespetime -a start", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:Engage!", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:Battle commencing in 5 seconds!", Category = KewordTypes.TimelineStart },
            new AnalyzeKeyword() { Keyword = "/spespetime -a end", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "has ended.", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "Cast your lot.", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = WipeoutLog, Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "uses", Category = KewordTypes.Action },
        };

        public static readonly Dictionary<string, Regex> AnalyzeRegexedEN = new Dictionary<string, Regex>()
        {
            {
                nameof(ActionRegex),
                CreateRegex(@"\[.+?\] 00:....:(?<actor>.+?) uses (?<skill>.+?)\.$")
            },
            {
                nameof(AddedRegex),
                CreateRegex(@":[EX] Added new combatant. name=(?<actor>.+) X=")
            },
            {
                nameof(CastRegex),
                CreateRegex(@"\[.+?\] 00:....:(?<actor>.+?) (readies|begins casting) (?<skill>.+?)\.$")
            },
            {
                nameof(HPRateRegex),
                CreateRegex(@"\[.+?\] ..:(?<actor>.+?) HP at (?<hprate>\d+?)%")
            },
            {
                nameof(StartsUsingRegex),
                CreateRegex(@"14:....:(?<actor>.+?) starts using (?<skill>.+?) on (?<target>.+?)\.$")
            },
            {
                nameof(StartsUsingUnknownRegex),
                CreateRegex(@"14:....:(?<actor>.+?) starts using (?<skill>.+?) on Unknown\.$")
            },
            {
                nameof(DialogRegex),
                CreateRegex(@"00:(0044|0839):(?<dialog>.+?)$")
            },
            {
                nameof(CombatStartRegex),
                CreateRegex(@"00:(0038|0039):(?<discription>.+?)$")
            },
            {
                nameof(CombatEndRegex),
                CreateRegex(@"00:....:(?<discription>.+?)$")
            },
            {
                nameof(EffectRegex),
                CreateRegex(@"1A:(?<victim>.+?) gains the effect of (?<effect>.+?) from (?<actor>.+?) for (?<duration>[0-9\.]*?) Seconds.$")
            },
            {
                nameof(MarkerRegex),
                CreateRegex(@"1B:(?<id>.{8}):(?<target>.+?):0000:....:(?<type>....):0000:0000:0000:$")
            },
            {
                nameof(MarkingRegex),
                CreateRegex(@"00:(?<id>....):(?<target>.+?) suffers the effect of Prey\.$")
            },
        };

        #endregion EN

        #region KO

        public const string CombatStartNowKO = "0039:전투 시작!";

        public static readonly IList<AnalyzeKeyword> KeywordsKO = new[]
        {
            new AnalyzeKeyword() { Keyword = "[EX] Added", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "[EX] POS", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "[EX] Beacon", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "에기", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "요정", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "카벙클" , Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "자동포탑", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "데미바하무트", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "지상의 별", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "시전합니다.", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "starts using", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "HP at", Category = KewordTypes.HPRate },
            new AnalyzeKeyword() { Keyword = "[EX] Added new combatant", Category = KewordTypes.Added },
            new AnalyzeKeyword() { Keyword = "] 1B:", Category = KewordTypes.Marker },
            new AnalyzeKeyword() { Keyword = "「マーキング」", Category = KewordTypes.Marker },
            new AnalyzeKeyword() { Keyword = "] 1A:", Category = KewordTypes.Effect },
            new AnalyzeKeyword() { Keyword = "00:0044:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = "00:0839:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = ImportLog, Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "/spespetime -a start", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:전투 시작!", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:전투 시작 5초 전!", Category = KewordTypes.TimelineStart },
            new AnalyzeKeyword() { Keyword = "/spespetime -a end", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "공략을 종료했습니다.", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "입찰을 진행하십시오", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = WipeoutLog, Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "시전했습니다.", Category = KewordTypes.Action },
        };

        public static readonly Dictionary<string, Regex> AnalyzeRegexedKO = new Dictionary<string, Regex>()
        {
            {
                nameof(ActionRegex),
                CreateRegex(@"\[.+?\] 00:....:(?<actor>.+?)(이|가) (?<skill>.+?)(을|를) 시전했습니다.$")
            },
            {
                nameof(AddedRegex),
                CreateRegex(@":[EX] Added new combatant. name=(?<actor>.+) X=")
            },
            {
                nameof(CastRegex),
                CreateRegex(@"\[.+?\] 00:....:(?<actor>.+?)(이|가) (?<skill>.+?)(을|를) 시전합니다.$")
            },
            {
                nameof(HPRateRegex),
                CreateRegex(@"\[.+?\] ..:(?<actor>.+?) HP at (?<hprate>\d+?)%")
            },
            {
                nameof(StartsUsingRegex),
                CreateRegex(@"14:....:(?<actor>.+?) starts using (?<skill>.+?) on (?<target>.+?)\.$")
            },
            {
                nameof(StartsUsingUnknownRegex),
                CreateRegex(@"14:....:(?<actor>.+?) starts using (?<skill>.+?) on Unknown\.$")
            },
            {
                nameof(DialogRegex),
                CreateRegex(@"00:(0044|0839):(?<dialog>.+?)$")
            },
            {
                nameof(CombatStartRegex),
                CreateRegex(@"00:(0038|0039):(?<discription>.+?)$")
            },
            {
                nameof(CombatEndRegex),
                CreateRegex(@"00:....:(?<discription>.+?)$")
            },
            {
                nameof(EffectRegex),
                CreateRegex(@"1A:(?<victim>.+?) gains the effect of (?<effect>.+?) from (?<actor>.+?) for (?<duration>[0-9\.]*?) Seconds.$")
            },
            {
                nameof(MarkerRegex),
                CreateRegex(@"1B:(?<id>.{8}):(?<target>.+?):0000:....:(?<type>....):0000:0000:0000:$")
            },
            {
                nameof(MarkingRegex),
                CreateRegex(@"00:(?<id>....):(?<target>.+?)に「マーキング」の効果。")
            },
        };

        #endregion KO

        #endregion Keywords

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
                    3 * 1000,
                    "CombatLog Analyer",
                    ThreadPriority.BelowNormal);

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
        private void FormActMain_OnLogLineRead(
            bool isImport,
            LogLineEventArgs logInfo)
        {
            try
            {
                if (!Settings.Default.AutoCombatLogAnalyze)
                {
                    return;
                }

                // キューに貯める
                this.logInfoQueue.Enqueue(logInfo);
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
        private IList<Combatant> combatants = null;

        private static readonly Regex PCNameRegex = new Regex(
            @"[a-zA-Z'\.]+ [a-zA-Z'\.]+",
            RegexOptions.Compiled);

        /// <summary>
        /// Combatantsを取得する
        /// </summary>
        /// <returns>Combatants</returns>
        private IList<Combatant> GetCombatants()
        {
            lock (this)
            {
                if (this.combatants == null)
                {
                    // プレイヤ情報とパーティリストを取得する
                    var ptlist = FFXIVPlugin.Instance.GetPartyList();

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
                names.AsParallel().Contains(actor))
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
            var jobName = "[pc]";

            var combs = this.GetCombatants();

            var com = combs.FirstOrDefault(x =>
                x?.Name == name ||
                x?.NameFI == name ||
                x?.NameIF == name ||
                x?.NameII == name);

            if (com != null)
            {
                jobName = com.IsPlayer ?
                    $"[mex]" :
                    $"[{com.JobID.ToString()}]";
            }

            return jobName;
        }

        #endregion PC Name

        #region Store Log

        private long no;
        private bool inCombat;
        private bool isImporting;

        /// <summary>
        /// ログを格納するスレッド
        /// </summary>
        private void StoreLogPoller()
        {
            if (this.logInfoQueue.IsEmpty)
            {
                return;
            }

            var preLog = string.Empty;

            var ignores = TimelineSettings.Instance.IgnoreLogTypes.Where(x => x.IsIgnore);

            var logs = new List<LogLineEventArgs>(this.logInfoQueue.Count);

            while (this.logInfoQueue.TryDequeue(out LogLineEventArgs log))
            {
                if (preLog == log.logLine)
                {
                    continue;
                }

                preLog = log.logLine;

                logs.Add(log);
            }

            foreach (var log in logs)
            {
                // 無効なログ？
                if (ignores.Any(x => log.logLine.Contains(x.Keyword)))
                {
                    continue;
                }

                // ダメージ系の不要なログか？
                if (LogBuffer.DamageLogPattern.IsMatch(log.logLine))
                {
                    continue;
                }

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
            var category = analyzeLogLine(logLine.logLine, Keywords);
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
                    starts using は準備動作をかぶるので無視する
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

                case KewordTypes.Dialogue:
                    if (this.inCombat)
                    {
                        this.StoreDialog(logLine);
                    }
                    break;

                case KewordTypes.Start:
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
                    break;

                case KewordTypes.End:
                    lock (this.CurrentCombatLogList)
                    {
                        if (this.inCombat)
                        {
                            this.inCombat = false;
                            this.StoreEndCombat(logLine);

                            this.AutoSaveToSpreadsheet();

                            Logger.Write("End Combat");
                        }
                    }
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
        /// ログを格納する
        /// </summary>
        /// <param name="log">ログ</param>
        private void StoreLog(
            CombatLog log)
        {
            var zone = ActGlobals.oFormActMain.CurrentZone;
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
                    log.RawWithoutTimestamp != ImportLog)
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

            this.StoreLog(log);
        }

        /// <summary>
        /// アクションログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreActionLog(
            LogLineEventArgs logInfo)
        {
            var match = ActionRegex.Match(logInfo.logLine);
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
                this.StoreLog(log);
            }
        }

        /// <summary>
        /// Addedのログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreAddedLog(
            LogLineEventArgs logInfo)
        {
            var match = AddedRegex.Match(logInfo.logLine);
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
                this.StoreLog(log);
            }
        }

        /// <summary>
        /// キャストログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreCastLog(
            LogLineEventArgs logInfo)
        {
            var match = CastRegex.Match(logInfo.logLine);
            if (!match.Success)
            {
                match = StartsUsingRegex.Match(logInfo.logLine);
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
                this.StoreLog(log);
            }
        }

        /// <summary>
        /// キャストログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreCastStartsUsingLog(
            LogLineEventArgs logInfo)
        {
            var match = StartsUsingRegex.Match(logInfo.logLine);
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
                this.StoreLog(log);
            }
        }

        /// <summary>
        /// Effectログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreEffectLog(
            LogLineEventArgs logInfo)
        {
            var match = EffectRegex.Match(logInfo.logLine);
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
                    this.StoreLog(log);
                }
            }
        }

        /// <summary>
        /// マーカーログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreMarkerLog(
            LogLineEventArgs logInfo)
        {
            const string PCIDPlaceholder = "(?<pcid>.{8})";

            var log = default(CombatLog);

            var match = MarkerRegex.Match(logInfo.logLine);
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
                match = MarkingRegex.Match(logInfo.logLine);
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
                    this.StoreLog(log);
                }
            }
        }

        /// <summary>
        /// HP率のログを格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreHPRateLog(
            LogLineEventArgs logInfo)
        {
            var match = HPRateRegex.Match(logInfo.logLine);
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
            var match = DialogRegex.Match(logInfo.logLine);
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

            this.StoreLog(log);
        }

        /// <summary>
        /// 戦闘開始を格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreStartCombat(
            LogLineEventArgs logInfo)
        {
            var match = CombatStartRegex.Match(logInfo.logLine);
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

            this.StoreLog(log);
        }

        /// <summary>
        /// 戦闘終了を格納する
        /// </summary>
        /// <param name="logInfo">ログ情報</param>
        private void StoreEndCombat(
            LogLineEventArgs logInfo)
        {
            var match = CombatEndRegex.Match(logInfo.logLine);
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

            this.StoreLog(log);
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

                // 冒頭にインポートを示すログを加える
                logLines.Insert(0, $"[00:00:00.000] {ImportLog}");

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
                    if (line.Length < 14)
                    {
                        continue;
                    }

                    var timeAsText = line.Substring(0, 14)
                        .Replace("[", string.Empty)
                        .Replace("]", string.Empty);

                    DateTime time;
                    if (!DateTime.TryParse(timeAsText, out time))
                    {
                        continue;
                    }

                    var detectTime = new DateTime(
                        now.Year,
                        now.Month,
                        now.Day,
                        time.Hour,
                        time.Minute,
                        time.Second,
                        time.Millisecond);

                    var arg = new LogLineEventArgs(
                        line,
                        0,
                        detectTime,
                        string.Empty,
                        true);

                    this.AnalyzeLogLine(arg);
                }

                var startCombat = this.CurrentCombatLogList.FirstOrDefault(x => x.Raw.Contains(CombatStartNow));
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

            var startCombat = logs.FirstOrDefault(x => x.Raw.Contains(CombatStartNow));
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

        public void SaveToSpreadsheet(
            string file,
            IList<CombatLog> combatLogs)
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

            // セルの編集内部メソッド
            void writeCell<T>(ISheet sh, int r, int c, T v, ICellStyle style)
            {
                var rowObj = sh.GetRow(r) ?? sheet.CreateRow(r);
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
            };

            var timeline = new TimelineModel();

            timeline.Zone = combatLogs.First().Zone;
            timeline.Locale = Settings.Default.FFXIVLocale;
            timeline.TimelineName = $"{timeline.Zone} draft timeline";
            timeline.Revision = "draft";
            timeline.Description =
                "自動生成によるドラフト版タイムラインです。タイムライン定義の作成にご活用ください。" + Environment.NewLine +
                "なお未編集のままで運用できるようには設計されていません。";

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
                        else
                        {
                            continue;
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
                            x.Text == a.Text))
                        {
                            continue;
                        }

                        break;
                }

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
                var startCombat = combatLogs.FirstOrDefault(x => x.Raw.Contains(CombatStartNow));
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
                        .Replace("(?<pcid>.{8})", "00000000"));

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
