using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Models;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.FFXIVHelper;

namespace ACT.SpecialSpellTimer
{
    /// <summary>
    /// ログのバッファ
    /// </summary>
    public class LogBuffer :
        IDisposable
    {
        #region Constants

        /// <summary>
        /// 空のログリスト
        /// </summary>
        public static readonly List<XIVLog> EmptyLogLineList = new List<XIVLog>();

        /// <summary>
        /// ツールチップのサフィックス
        /// </summary>
        /// <remarks>
        /// ツールチップは計4charsで構成されるが先頭1文字目が可変で残り3文字が固定となっている</remarks>
        public const string TooltipSuffix = "\u0001\u0001\uFFFD";

        /// <summary>
        /// ツールチップで残るリプレースメントキャラ
        /// </summary>
        public const string TooltipReplacementChar = "\uFFFD";

        #endregion Constants

        public LogBuffer()
        {
            XIVLogBuffer.Instance.StartPolling();
            XIVLogBuffer.Instance.SetGlobalFilters(new Predicate<string>[]
            {
                x => IsDamageLog(x),
            });

            this.GetLogs = XIVLogBuffer.Instance.Subscribe(
                this,
                IsIgnoreLog,
                RemoveTooltipSynbols);

            // Added Combatantsイベントを登録する
            FFXIVPlugin.Instance.AddedCombatants -= this.OnAddedCombatants;
            FFXIVPlugin.Instance.AddedCombatants += this.OnAddedCombatants;

            // 生ログの書き出しバッファを開始する
            ChatLogWorker.Instance.Begin();
        }

        ~LogBuffer() => this.Dispose();

        public void Dispose()
        {
            XIVLogBuffer.Free();
            FFXIVPlugin.Instance.AddedCombatants -= this.OnAddedCombatants;
            ChatLogWorker.Instance.End();
        }

        public double LPS => XIVLogBuffer.Instance.LPS;

        private void OnAddedCombatants(
            object sender,
            FFXIVPlugin.AddedCombatantsEventArgs e)
        {
            lock (this)
            {
                var now = DateTime.Now;

                if (e != null &&
                    e.NewCombatants != null &&
                    e.NewCombatants.Any())
                {
                    foreach (var combatant in e.NewCombatants)
                    {
                        var log = $"[EX] Added new combatant. name={combatant.Name} X={combatant.PosXMap:N2} Y={combatant.PosYMap:N2} Z={combatant.PosZMap:N2} hp={combatant.CurrentHP}";
                        LogParser.RaiseLog(now, log);
                    }
                }
            }
        }

        #region ログ処理

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
        /// ダメージログのキーワード
        /// </summary>
        private static readonly string DamageLogKeyword = "] 00:";

        /// <summary>
        /// ダメージ関係のログを示すキーワード
        /// </summary>
        /// <remarks>
        /// </remarks>
        private static readonly Regex DamageLogPattern =
            new Regex(
                @"^00:..(29|a9|2d|ad):",
                RegexOptions.Compiled |
                RegexOptions.IgnoreCase |
                RegexOptions.ExplicitCapture);

        /// <summary>
        /// ダメージログか？
        /// </summary>
        /// <param name="logLine">対象のログ行</param>
        /// <returns>bool</returns>
        public static bool IsDamageLog(
            string logLine)
        {
            if (!Settings.Default.IgnoreDamageLogs)
            {
                return false;
            }

            if (logLine.Contains(DamageLogKeyword))
            {
                return DamageLogPattern.IsMatch(logLine.Remove(0, 15));
            }

            return false;
        }

        /// <summary>
        /// 設定によらず必ずカットするログのキーワード
        /// </summary>
        public static readonly string[] IgnoreLogKeywords = new[]
        {
            MessageType.NetworkDoT.ToKeyword(),
        };

        /// <summary>
        /// 設定によってカットする場合があるログのキーワード
        /// </summary>
        public static readonly string[] IgnoreDetailLogKeywords = new[]
        {
            MessageType.NetworkAbility.ToKeyword(),
            MessageType.NetworkAOEAbility.ToKeyword()
        };

        /// <summary>
        /// パーティメンバについてのHPログか？
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public static bool IsHPLogByPartyMember(
            string log)
        {
            if (!log.Contains(MessageType.CombatantHP.ToKeyword()))
            {
                return false;
            }

            return TableCompiler.Instance?.SortedPartyList?.Any(x => log.Contains(x.Name)) ?? false;
        }

        private Func<IEnumerable<XIVLog>> GetLogs;

        public IEnumerable<XIVLog> GetLogLines()
        {
            var player = FFXIVPlugin.Instance.GetPlayer();
            var palyerIsSummoner = player?
                .AsJob()?
                .IsSummoner() ?? false;

            var summoned = false;
            var doneCommand = false;

            var logs = this.GetLogs();
            foreach (var xlvLog in logs)
            {
                // ペットジョブで召喚をしたか？
                if (!summoned &&
                    palyerIsSummoner)
                {
                    summoned = isSummoned(xlvLog.LogLine);
                }

                // コマンドとマッチングする
                doneCommand |= TextCommandController.MatchCommandCore(xlvLog.LogLine);

                yield return xlvLog;
            }

            if (summoned)
            {
                TableCompiler.Instance.RefreshPetPlaceholder();
            }

            if (doneCommand)
            {
                SystemSounds.Asterisk.Play();
            }

            // ログファイルに出力する
            if (Settings.Default.SaveLogEnabled)
            {
                ChatLogWorker.Instance.AppendLinesAsync(logs);
            }

            // 召喚したか？
            bool isSummoned(string logLine)
            {
                var r = false;

                if (logLine.Contains("You cast Summon", StringComparison.OrdinalIgnoreCase))
                {
                    r = true;
                }
                else
                {
                    if (!string.IsNullOrEmpty(player.Name))
                    {
                        r = logLine.Contains(player.Name + "の「サモン", StringComparison.OrdinalIgnoreCase);
                    }

                    if (!string.IsNullOrEmpty(player.NameFI))
                    {
                        r = logLine.Contains(player.NameFI + "の「サモン", StringComparison.OrdinalIgnoreCase);
                    }

                    if (!string.IsNullOrEmpty(player.NameIF))
                    {
                        r = logLine.Contains(player.NameIF + "の「サモン", StringComparison.OrdinalIgnoreCase);
                    }

                    if (!string.IsNullOrEmpty(player.NameII))
                    {
                        r = logLine.Contains(player.NameII + "の「サモン", StringComparison.OrdinalIgnoreCase);
                    }
                }

                return r;
            }
        }

        private static bool IsIgnoreLog(
            string line)
        {
            if (IgnoreLogKeywords.Any(x => line.Contains(x)))
            {
                return false;
            }

            if (Settings.Default.IgnoreDetailLogs &&
                IgnoreDetailLogKeywords.Any(x => line.Contains(x)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// ツールチップシンボルを除去する
        /// </summary>
        /// <param name="logLine"></param>
        /// <returns>編集後のLogLine</returns>
        public static string RemoveTooltipSynbols(
            string logLine)
        {
            var result = logLine;

            // エフェクトに付与されるツールチップ文字を除去する
            if (Settings.Default.RemoveTooltipSymbols)
            {
                // 4文字分のツールチップ文字を除去する
                int index;
                if ((index = result.IndexOf(
                    TooltipSuffix,
                    0,
                    StringComparison.Ordinal)) > -1)
                {
                    const int removeLength = 4;
                    var startIndex = index - 1;

                    if (startIndex >= 0)
                    {
                        result = result.Remove(startIndex, removeLength);
                    }
                }

                // 残ったReplacementCharを除去する
                result = result.Replace(TooltipReplacementChar, string.Empty);
            }

            return result;
        }

        #endregion ログ処理

        #region その他のメソッド

        private static (float X, float Y, float Z) previousPos = (0, 0, 0);

        /// <summary>
        /// 自分の座標をダンプする
        /// </summary>
        /// <param name="isAuto">
        /// 自動出力？</param>
        public static void DumpPosition(
            bool isAuto = false)
        {
            var player = FFXIVPlugin.Instance.GetPlayer();
            if (player == null)
            {
                return;
            }

            if (previousPos.X == player.PosXMap &&
                previousPos.Y == player.PosYMap &&
                previousPos.Z == player.PosZMap)
            {
                return;
            }

            previousPos.X = player.PosXMap;
            previousPos.Y = player.PosYMap;
            previousPos.Z = player.PosZMap;

            var zone = ActGlobals.oFormActMain?.CurrentZone;
            if (string.IsNullOrEmpty(zone))
            {
                zone = "Unknown Zone";
            }

            LogParser.RaiseLog(
                DateTime.Now,
                $"[EX] {(isAuto ? "Beacon" : "POS")} X={player.PosXMap:N2} Y={player.PosYMap:N2} Z={player.PosZMap:N2} zone={zone}");
        }

        #endregion その他のメソッド
    }
}
