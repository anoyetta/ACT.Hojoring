using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ACT.SpecialSpellTimer.Config;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;

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
        NetworkAbility,
        NetworkAOEAbility,
        Start,
        End,
        AnalyzeEnd,
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
        NetworkAbility,
        NetworkAOEAbility,
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
                "Ability",
                "AOE",
                "HP Rate",
                "Dialog",
            }[(int)t];

        public static Color ToBackgroundColor(
            this LogTypes t)
            => new[]
            {
                (Color)cc.ConvertFrom("#FFFFFFFF"), // UNKNOWN
                (Color)cc.ConvertFrom("#FFDC143C"), // Combat Start
                (Color)cc.ConvertFrom("#FFDC143C"), // Combat End
                (Color)cc.ConvertFrom("#FF4169E1"), // Starts Using
                (Color)cc.ConvertFrom("#FF98fb98"), // Action
                (Color)cc.ConvertFrom("#FFF0E68C"), // Effect
                (Color)cc.ConvertFrom("#FFFFA500"), // Marker
                (Color)cc.ConvertFrom("#00000000"), // Added
                (Color)cc.ConvertFrom("#FFbed2c3"), // Ability
                (Color)cc.ConvertFrom("#FFbed2c3"), // AOE
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
                (Color)cc.ConvertFrom("#FF000000"), // Ability
                (Color)cc.ConvertFrom("#FF000000"), // AOE
                (Color)cc.ConvertFrom("#FF000000"), // HP Rate
                (Color)cc.ConvertFrom("#FF000000"), // Dialog
            }[(int)t];
    }

    #endregion Enum

    public static class ConstantKeywords
    {
        public const string ImportLog = "00:0000:import";

        private static Dictionary<Locales, AnalyzerContainer> analyzerDictionary;

        private static Dictionary<Locales, AnalyzerContainer> AnalyzerDictionary =>
            analyzerDictionary ?? (analyzerDictionary = new Dictionary<Locales, AnalyzerContainer>()
            {
                { Locales.JA, new AnalyzerContainer(CombatStartNowJA, KeywordsJA, AnalyzeRegexesJA) },
                { Locales.EN, new AnalyzerContainer(CombatStartNowEN, KeywordsEN, AnalyzeRegexesEN) },
                { Locales.FR, new AnalyzerContainer(CombatStartNowEN, KeywordsEN, AnalyzeRegexesEN) },
                { Locales.DE, new AnalyzerContainer(CombatStartNowEN, KeywordsEN, AnalyzeRegexesEN) },
                { Locales.TW, new AnalyzerContainer(CombatStartNowCN, KeywordsCN, AnalyzeRegexesCN) },
                { Locales.CN, new AnalyzerContainer(CombatStartNowCN, KeywordsCN, AnalyzeRegexesCN) },
                { Locales.KO, new AnalyzerContainer(CombatStartNowKO, KeywordsKO, AnalyzeRegexesKO) },
            });

        #region Common

        public class AnalyzerContainer
        {
            public AnalyzerContainer(
                string combatStartNow,
                IList<AnalyzeKeyword> keywords,
                IDictionary<string, Regex> regexDictinary)
            {
                this.CombatStartNow = combatStartNow;
                this.Keywords = keywords;
                this.RegexDictinary = regexDictinary;
            }

            public string CombatStartNow { get; set; }

            public IList<AnalyzeKeyword> Keywords { get; set; }

            public IDictionary<string, Regex> RegexDictinary { get; set; }
        }

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

        public static Regex NetworkAbility => AnalyzeRegexes[nameof(NetworkAbility)];

        public static Regex NetworkAOEAbility => AnalyzeRegexes[nameof(NetworkAOEAbility)];

        public static string CombatStartNow => AnalyzerDictionary[Settings.Default.FFXIVLocale].CombatStartNow;

        public static IList<AnalyzeKeyword> Keywords => AnalyzerDictionary[Settings.Default.FFXIVLocale].Keywords;

        private static IDictionary<string, Regex> AnalyzeRegexes => AnalyzerDictionary[Settings.Default.FFXIVLocale].RegexDictinary;

        private static Regex CreateRegex(string pattern)
            => new Regex(
                pattern,
                RegexOptions.Compiled |
                RegexOptions.ExplicitCapture);

        #endregion Common

        #region JA

        private const string CombatStartNowJA = "0039:戦闘開始！";

        private static readonly IList<AnalyzeKeyword> KeywordsJA = new[]
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
            new AnalyzeKeyword() { Keyword = "オートマトン・", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "セラフィム", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "デミ・フェニックス", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "英雄の影身", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "分身", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "を唱えた。", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "の構え。", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "starts using", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "HP at", Category = KewordTypes.HPRate },
            new AnalyzeKeyword() { Keyword = "[EX] Added new combatant", Category = KewordTypes.Added },
            new AnalyzeKeyword() { Keyword = "] 1B:", Category = KewordTypes.Marker },
            new AnalyzeKeyword() { Keyword = "「マーキング」", Category = KewordTypes.Marker },
            new AnalyzeKeyword() { Keyword = "] 1A:", Category = KewordTypes.Effect },
            new AnalyzeKeyword() { Keyword = "] 15:", Category = KewordTypes.NetworkAbility },
            new AnalyzeKeyword() { Keyword = "] 16:", Category = KewordTypes.NetworkAOEAbility },
            new AnalyzeKeyword() { Keyword = "00:0044:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = "00:0839:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = ImportLog, Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "/spespetime -a start", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "/analyze start", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:戦闘開始", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:戦闘開始まで5秒！", Category = KewordTypes.TimelineStart },
            new AnalyzeKeyword() { Keyword = "/spespetime -a end", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "/analyze end", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "の攻略を終了した。", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "ロットを行ってください。", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "01:Changed Zone", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "00:0139:戦闘開始まで", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = WipeoutKeywords.WipeoutLog, Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = WipeoutKeywords.WipeoutLogEcho, Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "レディチェックを開始しました。", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "「", Category = KewordTypes.Action },
            new AnalyzeKeyword() { Keyword = "」", Category = KewordTypes.Action },
        };

        private static readonly Dictionary<string, Regex> AnalyzeRegexesJA = new Dictionary<string, Regex>()
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
                CreateRegex(@"00:(0044|0839):(?<speaker>.+?):(?<dialog>.+?)$")
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
                CreateRegex(@"1A:(?<id>[0-9a-fA-F]{8}):(?<victim>.+?) gains the effect of (?<effect>.+?) from (?<actor>.*?) for (?<duration>[0-9\.]*?) Seconds.$")
            },
            {
                nameof(MarkerRegex),
                CreateRegex(@"1B:(?<id>.{8}):(?<target>.+?):0000:....:(?<type>....):0000:0000:0000:$")
            },
            {
                nameof(MarkingRegex),
                CreateRegex(@"00:(?<id>....):(?<target>.+?)に「マーキング」の効果。")
            },
            {
                nameof(NetworkAbility),
                CreateRegex(@"15:(?<id>[0-9a-fA-F]{8}):(?<actor>.*?):(?<skill_id>[0-9a-fA-F]{2,4}):(?<skill>.+?):(?<victim_id>[0-9a-fA-F]{8}):(?<victim>.*?):")
            },
            {
                nameof(NetworkAOEAbility),
                CreateRegex(@"16:(?<id>[0-9a-fA-F]{8}):(?<actor>.*?):(?<skill_id>[0-9a-fA-F]{2,4}):(?<skill>.+?):(?<victim_id>[0-9a-fA-F]{8}):(?<victim>.*?):")
            },
        };

        #endregion JA

        #region EN

        private const string CombatStartNowEN = "0039:Engage!";

        private static readonly IList<AnalyzeKeyword> KeywordsEN = new[]
        {
            new AnalyzeKeyword() { Keyword = "[EX] Added", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "[EX] POS", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "[EX] Beacon", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "-Egi", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Eos", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Selene", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Seraph", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Carbuncle", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Autoturret", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Automaton Queen", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Demi-Bahamut", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Demi-Phoenix", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Earthly Star", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Esteem", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "Bunshin", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "begins casting", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "readies", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "starts using", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "HP at", Category = KewordTypes.HPRate },
            new AnalyzeKeyword() { Keyword = "[EX] Added new combatant", Category = KewordTypes.Added },
            new AnalyzeKeyword() { Keyword = "] 1B:", Category = KewordTypes.Marker },
            new AnalyzeKeyword() { Keyword = "suffers the effect of Prey.", Category = KewordTypes.Marker },
            new AnalyzeKeyword() { Keyword = "] 1A:", Category = KewordTypes.Effect },
            new AnalyzeKeyword() { Keyword = "] 15:", Category = KewordTypes.NetworkAbility },
            new AnalyzeKeyword() { Keyword = "] 16:", Category = KewordTypes.NetworkAOEAbility },
            new AnalyzeKeyword() { Keyword = "00:0044:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = "00:0839:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = ImportLog, Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "/spespetime -a start", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "/analyze start", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:Engage!", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:Battle commencing in 5 seconds!", Category = KewordTypes.TimelineStart },
            new AnalyzeKeyword() { Keyword = "/spespetime -a end", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "/analyze end", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "has ended.", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "Cast your lot.", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "01:Changed Zone", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "00:0139:", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = WipeoutKeywords.WipeoutLog, Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = WipeoutKeywords.WipeoutLogEcho, Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "uses", Category = KewordTypes.Action },    //not used in E1s and E2s instead of 'casts',considering deleting.
            new AnalyzeKeyword() { Keyword = "casts", Category = KewordTypes.Action },
        };

        private static readonly Dictionary<string, Regex> AnalyzeRegexesEN = new Dictionary<string, Regex>()
        {
            {
                nameof(ActionRegex),
                CreateRegex(@"\[.+?\] 00:....:(?<actor>.+?) (casts|uses) (?<skill>.+?)\.$")
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
                CreateRegex(@"00:(0044|0839):(?<speaker>.+?):(?<dialog>.+?)$")
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
                CreateRegex(@"1A:(?<id>[0-9a-fA-F]{8}):(?<victim>.+?) gains the effect of (?<effect>.+?) from (?<actor>.*?) for (?<duration>[0-9\.]*?) Seconds.$")
            },
            {
                nameof(MarkerRegex),
                CreateRegex(@"1B:(?<id>.{8}):(?<target>.+?):0000:....:(?<type>....):0000:0000:0000:$")
            },
            {
                nameof(MarkingRegex),
                CreateRegex(@"00:(?<id>....):(?<target>.+?) suffers the effect of Prey\.$")
            },
            {
                nameof(NetworkAbility),
                CreateRegex(@"15:(?<id>[0-9a-fA-F]{8}):(?<actor>.*?):(?<skill_id>[0-9a-fA-F]{2,4}):(?<skill>.+?):(?<victim_id>[0-9a-fA-F]{8}):(?<victim>.*?):")
            },
            {
                nameof(NetworkAOEAbility),
                CreateRegex(@"16:(?<id>[0-9a-fA-F]{8}):(?<actor>.*?):(?<skill_id>[0-9a-fA-F]{2,4}):(?<skill>.+?):(?<victim_id>[0-9a-fA-F]{8}):(?<victim>.*?):")
            },
        };

        #endregion EN

        #region KO

        private const string CombatStartNowKO = "0039:전투 시작!";

        private static readonly IList<AnalyzeKeyword> KeywordsKO = new[]
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
            new AnalyzeKeyword() { Keyword = "표식 효과를", Category = KewordTypes.Marker },
            new AnalyzeKeyword() { Keyword = "] 1A:", Category = KewordTypes.Effect },
            new AnalyzeKeyword() { Keyword = "] 15:", Category = KewordTypes.NetworkAbility },
            new AnalyzeKeyword() { Keyword = "] 16:", Category = KewordTypes.NetworkAOEAbility },
            new AnalyzeKeyword() { Keyword = "00:0044:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = "00:0839:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = ImportLog, Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "/spespetime -a start", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "/analyze start", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:전투 시작!", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:전투 시작 5초 전!", Category = KewordTypes.TimelineStart },
            new AnalyzeKeyword() { Keyword = "/spespetime -a end", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "/analyze end", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "공략을 종료했습니다.", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "입찰을 진행하십시오", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "01:Changed Zone", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "00:0139:", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = WipeoutKeywords.WipeoutLog, Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = WipeoutKeywords.WipeoutLogEcho, Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "준비 확인을 시작했습니다.", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "시전했습니다.", Category = KewordTypes.Action },
        };

        private static readonly Dictionary<string, Regex> AnalyzeRegexesKO = new Dictionary<string, Regex>()
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
                CreateRegex(@"00:(0044|0839):(?<speaker>.+?):(?<dialog>.+?)$")
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
                CreateRegex(@"1A:(?<id>[0-9a-fA-F]{8}):(?<victim>.+?) gains the effect of (?<effect>.+?) from (?<actor>.*?) for (?<duration>[0-9\.]*?) Seconds.$")
            },
            {
                nameof(MarkerRegex),
                CreateRegex(@"1B:(?<id>.{8}):(?<target>.+?):0000:....:(?<type>....):0000:0000:0000:$")
            },
            {
                nameof(MarkingRegex),
                CreateRegex(@"00:(?<id>....):(?<target>.+?)가 표식 효과를 받았습니다.")
            },
            {
                nameof(NetworkAbility),
                CreateRegex(@"15:(?<id>[0-9a-fA-F]{8}):(?<actor>.*?):(?<skill_id>[0-9a-fA-F]{2,4}):(?<skill>.+?):(?<victim_id>[0-9a-fA-F]{8}):(?<victim>.*?):")
            },
            {
                nameof(NetworkAOEAbility),
                CreateRegex(@"16:(?<id>[0-9a-fA-F]{8}):(?<actor>.*?):(?<skill_id>[0-9a-fA-F]{2,4}):(?<skill>.+?):(?<victim_id>[0-9a-fA-F]{8}):(?<victim>.*?):")
            },
        };

        #endregion KO

        #region CN

        private const string CombatStartNowCN = "0039:战斗开始！";

        private static readonly IList<AnalyzeKeyword> KeywordsCN = new[]
        {
            new AnalyzeKeyword() { Keyword = "[EX] Added", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "[EX] POS", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "[EX] Beacon", Category = KewordTypes.Record },
            new AnalyzeKeyword() { Keyword = "之灵", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "朝日小仙女", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "夕月小仙女", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "宝石兽", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "式浮空炮塔", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "亚灵神巴哈姆特", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "地星", Category = KewordTypes.Pet },
            new AnalyzeKeyword() { Keyword = "正在咏唱", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "正在发动", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "starts using", Category = KewordTypes.Cast },
            new AnalyzeKeyword() { Keyword = "HP at", Category = KewordTypes.HPRate },
            new AnalyzeKeyword() { Keyword = "[EX] Added new combatant", Category = KewordTypes.Added },
            new AnalyzeKeyword() { Keyword = "] 1B:", Category = KewordTypes.Marker },
            new AnalyzeKeyword() { Keyword = "陷入了“猎物”效果", Category = KewordTypes.Marker },
            new AnalyzeKeyword() { Keyword = "] 1A:", Category = KewordTypes.Effect },
            new AnalyzeKeyword() { Keyword = "] 15:", Category = KewordTypes.NetworkAbility },
            new AnalyzeKeyword() { Keyword = "] 16:", Category = KewordTypes.NetworkAOEAbility },
            new AnalyzeKeyword() { Keyword = "00:0044:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = "00:0839:", Category = KewordTypes.Dialogue },
            new AnalyzeKeyword() { Keyword = ImportLog, Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "/spespetime -a start", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "/analyze start", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:战斗开始！", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "00:0039:距离战斗开始还有5秒！", Category = KewordTypes.TimelineStart },
            new AnalyzeKeyword() { Keyword = "/spespetime -a end", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "/analyze end", Category = KewordTypes.Start },
            new AnalyzeKeyword() { Keyword = "结束了", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "00:0839:请掷骰。", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "01:Changed Zone", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "00:0139:", Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = WipeoutKeywords.WipeoutLog, Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = WipeoutKeywords.WipeoutLogEcho, Category = KewordTypes.End },
            new AnalyzeKeyword() { Keyword = "发动了", Category = KewordTypes.Action },
        };

        private static readonly Dictionary<string, Regex> AnalyzeRegexesCN = new Dictionary<string, Regex>()
        {
            {
                nameof(ActionRegex),
                CreateRegex(@"\[.+?\] 00:....:(?<actor>.+?)发动了\“(?<skill>.+?)\”\。$")
            },
            {
                nameof(AddedRegex),
                CreateRegex(@":[EX] Added new combatant. name=(?<actor>.+) X=")
            },
            {
                nameof(CastRegex),
                CreateRegex(@"\[.+?\] 00:....:(?<actor>.+?)(正在发动|正在咏唱)\“(?<skill>.+?)\”\。$")
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
                CreateRegex(@"00:(0044|0839):(?<speaker>.+?):(?<dialog>.+?)$")
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
                CreateRegex(@"1A:(?<id>[0-9a-fA-F]{8}):(?<victim>.+?) gains the effect of (?<effect>.+?) from (?<actor>.*?) for (?<duration>[0-9\.]*?) Seconds.$")
            },
            {
                nameof(MarkerRegex),
                CreateRegex(@"1B:(?<id>.{8}):(?<target>.+?):0000:....:(?<type>....):0000:0000:0000:$")
            },
            {
                nameof(MarkingRegex),
                CreateRegex(@"00:(?<id>....):(?<target>.+?)陷入了“猎物”效果\。$")
            },
            {
                nameof(NetworkAbility),
                CreateRegex(@"15:(?<id>[0-9a-fA-F]{8}):(?<actor>.*?):(?<skill_id>[0-9a-fA-F]{2,4}):(?<skill>.+?):(?<victim_id>[0-9a-fA-F]{8}):(?<victim>.*?):")
            },
            {
                nameof(NetworkAOEAbility),
                CreateRegex(@"16:(?<id>[0-9a-fA-F]{8}):(?<actor>.*?):(?<skill_id>[0-9a-fA-F]{2,4}):(?<skill>.+?):(?<victim_id>[0-9a-fA-F]{8}):(?<victim>.*?):")
            },
        };

        #endregion CN
    }
}
