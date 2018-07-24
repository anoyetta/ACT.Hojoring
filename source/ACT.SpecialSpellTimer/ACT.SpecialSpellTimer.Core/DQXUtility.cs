using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.Utility;

namespace ACT.SpecialSpellTimer
{
    /// <summary>
    /// DQX向けの便利機能
    /// </summary>
    public static class DQXUtility
    {
        /// <summary>
        /// パーティメンバリスト
        /// </summary>
        public static List<string> PartyMemberList = new List<string>();

        /// <summary>
        /// パーティメンバ追加正規表現
        /// </summary>
        private static readonly IReadOnlyCollection<Regex> PartyAddedRegex = new List<Regex>
        {
            new Regex(@"\t(?<member>\S+?)が\s+仲間に加わった！", RegexOptions.Compiled),
            new Regex(@"\t(?<member>\S+?)の\s+仲間になった！", RegexOptions.Compiled),
        };

        /// <summary>
        /// パーティ解散ワード
        /// </summary>
        private static readonly IReadOnlyCollection<string> PartyBreakWords = new List<string>
        {
            "仲間から はずれました",
            "パーティを 解散しました",
            "reset dqx party",
        };

        /// <summary>
        /// パーティ状況の変更ワード
        /// </summary>
        private static readonly IReadOnlyCollection<string> PartyChangeWords = new List<string>
        {
            "仲間に加わった！",
            "仲間を抜けました",
            "仲間になった！",
            "仲間から はずれました",
            "パーティを 解散しました",
            "reset dqx party",
        };

        /// <summary>
        /// パーティメンバ減少正規表現
        /// </summary>
        private static readonly IReadOnlyCollection<Regex> PartyLeftRegex = new List<Regex>
        {
            new Regex(@"\t(?<member>\S+?)が\s+仲間を抜けました", RegexOptions.Compiled),
        };

        /// <summary>
        /// プレイヤー名
        /// </summary>
        public static string PlayerName { get; set; }

        /// <summary>
        /// パーティに変更があったか？
        /// </summary>
        /// <param name="logLine">ログ</param>
        /// <returns>bool</returns>
        public static bool IsPartyChanged(
            string logLine)
        {
            if (!Settings.Default.DQXUtilityEnabled)
            {
                return false;
            }

            // パーティに変更があったか？
            var r = PartyChangeWords.AsParallel()
                .Any(word => logLine.EndsWith(word));

            if (!r)
            {
                return r;
            }

            // パーティの解散？
            if (PartyBreakWords.AsParallel()
                .Any(word => logLine.EndsWith(word)))
            {
                PartyMemberList.Clear();
                Logger.Write("[DQX] パーティは解散しました。");
                return r;
            }

            // パーティメンバの追加？
            var isAdded = false;
            foreach (var regex in PartyAddedRegex)
            {
                var match = regex.Match(logLine);

                if (match.Success)
                {
                    var member = match.Groups["member"].Value.Trim();

                    if (!string.IsNullOrWhiteSpace(member))
                    {
                        if (!PartyMemberList.Any(x => x == member))
                        {
                            PartyMemberList.Add(member);
                            Logger.Write("[DQX] パーティが追加されました。 -> " + member);
                        }
                    }

                    isAdded = true;
                    break;
                }
            }

            if (isAdded)
            {
                return r;
            }

            // パーティメンバの減少？
            foreach (var regex in PartyLeftRegex)
            {
                var match = regex.Match(logLine);

                if (match.Success)
                {
                    var member = match.Groups["member"].Value.Trim();

                    if (!string.IsNullOrWhiteSpace(member))
                    {
                        if (PartyMemberList.Any(x => x == member))
                        {
                            PartyMemberList.Remove(member);
                            Logger.Write("[DQX] パーティから抜けました。 -> " + member);
                        }
                    }

                    break;
                }
            }

            return r;
        }

        /// <summary>
        /// キーワードを生成する
        /// </summary>
        /// <param name="keyword">元のキーワード</param>
        /// <returns>変換後のキーワード</returns>
        public static string MakeKeyword(
            string keyword)
        {
            if (!Settings.Default.DQXUtilityEnabled)
            {
                return keyword;
            }

#if DEBUG
            var needsReplace = false;
            if (keyword.Contains("<me>") ||
                keyword.Contains("<2>") ||
                keyword.Contains("<3>") ||
                keyword.Contains("<4>") ||
                keyword.Contains("<5>") ||
                keyword.Contains("<6>") ||
                keyword.Contains("<7>") ||
                keyword.Contains("<8>"))
            {
                needsReplace = true;
            }
#endif

            // 設定からプレイヤー名を読み出す
            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                PlayerName = Settings.Default.DQXPlayerName;
            }

            // プレイヤー名を置換する
            if (!string.IsNullOrWhiteSpace(PlayerName))
            {
                keyword = keyword.Replace("<me>", PlayerName.Trim());
            }

            // パーティメンバを置換する<2>～<8>
            if (PartyMemberList.Any())
            {
                var i = 2;
                foreach (var member in PartyMemberList)
                {
                    if (!string.IsNullOrWhiteSpace(member))
                    {
                        keyword = keyword.Replace("<" + i + ">", member.Trim());
                    }

                    i++;
                }
            }

#if DEBUG
            if (needsReplace)
            {
                Logger.Write("[DQX] Keyword replased. -> " + keyword);
            }
#endif

            return keyword;
        }

        /// <summary>
        /// キーワードを再生成する
        /// </summary>
        public static void RefeshKeywords()
        {
            if (!Settings.Default.DQXUtilityEnabled)
            {
                return;
            }

            TableCompiler.Instance.RecompileSpells();
            TableCompiler.Instance.RecompileTickers();
        }
    }
}
