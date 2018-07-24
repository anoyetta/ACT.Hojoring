using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.Utility;
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

        /// <summary>
        /// 内部バッファ
        /// </summary>
        private readonly ConcurrentQueue<LogLineEventArgs> logInfoQueue = new ConcurrentQueue<LogLineEventArgs>();

        /// <summary>
        /// 内部バッファ
        /// </summary>
        public ConcurrentQueue<LogLineEventArgs> LogInfoQueue => this.logInfoQueue;

        /// <summary>
        /// 最初のログが到着したか？
        /// </summary>
        private bool firstLogArrived;

        #region コンストラクター/デストラクター/Dispose

        private const int FALSE = 0;

        private const int TRUE = 1;

        private int disposed = FALSE;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public LogBuffer()
        {
            // BeforeLogLineReadイベントを登録する 無理やり一番目に処理されるようにする
            this.AddOnBeforeLogLineRead();

            // LogLineReadイベントを登録する
            ActGlobals.oFormActMain.OnLogLineRead -= this.OnLogLineRead;
            ActGlobals.oFormActMain.OnLogLineRead += this.OnLogLineRead;

            // Added Combatantsイベントを登録する
            FFXIVPlugin.Instance.AddedCombatants -= this.OnAddedCombatants;
            FFXIVPlugin.Instance.AddedCombatants += this.OnAddedCombatants;

            // 生ログの書き出しバッファを開始する
            ChatLogWorker.Instance.Begin();
        }

        /// <summary>
        /// デストラクター
        /// </summary>
        ~LogBuffer()
        {
            this.Dispose();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref disposed, TRUE, FALSE) != FALSE)
            {
                return;
            }

            ActGlobals.oFormActMain.BeforeLogLineRead -= this.OnBeforeLogLineRead;
            ActGlobals.oFormActMain.OnLogLineRead -= this.OnLogLineRead;
            FFXIVPlugin.Instance.AddedCombatants -= this.OnAddedCombatants;

            // 生ログの書き出しバッファを停止する
            ChatLogWorker.Instance.End();
        }

        #endregion コンストラクター/デストラクター/Dispose

        #region ACT event hander

        /// <summary>
        /// OnBeforeLogLineRead イベントを追加する
        /// </summary>
        /// <remarks>
        /// スペスペのOnBeforeLogLineReadをACT本体に登録する。
        /// ただし、FFXIVプラグインよりも先に処理する必要があるのでイベントを一旦除去して
        /// スペスペのイベントを登録した後に元のイベントを登録する
        /// </remarks>
        private void AddOnBeforeLogLineRead()
        {
            if (!Settings.Default.DetectPacketDump)
            {
                return;
            }

            try
            {
                var fi = ActGlobals.oFormActMain.GetType().GetField(
                    "BeforeLogLineRead",
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.GetField |
                    BindingFlags.Public |
                    BindingFlags.Static);

                var beforeLogLineReadDelegate =
                    fi.GetValue(ActGlobals.oFormActMain)
                    as Delegate;

                if (beforeLogLineReadDelegate != null)
                {
                    var handlers = beforeLogLineReadDelegate.GetInvocationList();

                    // 全てのイベントハンドラを一度解除する
                    foreach (var handler in handlers)
                    {
                        ActGlobals.oFormActMain.BeforeLogLineRead -= (LogLineEventDelegate)handler;
                    }

                    // スペスペのイベントハンドラを最初に登録する
                    ActGlobals.oFormActMain.BeforeLogLineRead += this.OnBeforeLogLineRead;

                    // 解除したイベントハンドラを登録し直す
                    foreach (var handler in handlers)
                    {
                        ActGlobals.oFormActMain.BeforeLogLineRead += (LogLineEventDelegate)handler;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("AddOnBeforeLogLineRead error:", ex);
            }
        }

        /// <summary>
        /// OnBeforeLogLineRead
        /// </summary>
        /// <param name="isImport">Importか？</param>
        /// <param name="logInfo">ログ情報</param>
        /// <remarks>
        /// FFXIVプラグインが加工する前のログが通知されるイベント こちらは一部カットされてしまうログがカットされずに通知される
        /// またログのデリミタが異なるため、通常のログと同様に扱えるようにデリミタを変換して取り込む
        /// </remarks>
        private void OnBeforeLogLineRead(
            bool isImport,
            LogLineEventArgs logInfo)
        {
#if !DEBUG
            if (isImport)
            {
                return;
            }
#endif
            // PacketDumpを解析対象にしていないならば何もしない
            if (!Settings.Default.DetectPacketDump)
            {
                return;
            }

            try
            {
                /*
                Debug.WriteLine(logInfo.logLine);
                */
                var data = logInfo.logLine.Split('|');

                if (data.Length >= 2)
                {
                    var messageType = int.Parse(data[0]);
                    var timeStamp = DateTime.Parse(data[1]);

                    switch (messageType)
                    {
                        // 251:Debug, 252:PacketDump, 253:Version
                        case 251:
                        case 252:
                        case 253:
                            // ログオブジェクトをコピーする
                            var copyLogInfo = new LogLineEventArgs(
                                logInfo.logLine,
                                logInfo.detectedType,
                                logInfo.detectedTime,
                                logInfo.detectedZone,
                                logInfo.inCombat);

                            // ログを出力用に書き換える
                            copyLogInfo.logLine =
                                $"[{timeStamp:HH:mm:ss.fff}] {messageType:X2}:{string.Join(":", data)}";

                            this.logInfoQueue.Enqueue(copyLogInfo);
                            break;
                    }
                }
            }
            catch (Exception)
            {
                // 例外は握りつぶす
            }
        }

        private double[] lpss = new double[60];
        private int currentLpsIndex;
        private long currentLineCount;
        private Stopwatch lineCountTimer = new Stopwatch();

        /// <summary>
        /// LPS lines/s
        /// </summary>
        public double LPS
        {
            get
            {
                var availableLPSs = this.lpss.Where(x => x > 0);
                if (!availableLPSs.Any())
                {
                    return 0;
                }

                return availableLPSs.Sum() / availableLPSs.Count();
            }
        }

        /// <summary>
        /// LPSを計測する
        /// </summary>
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

        /// <summary>
        /// OnLogLineRead
        /// </summary>
        /// <param name="isImport">Importか？</param>
        /// <param name="logInfo">ログ情報</param>
        /// <remarks>FFXIVプラグインが加工した後のログが通知されるイベント</remarks>
        private void OnLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            // 18文字以下のログは読み捨てる
            // なぜならば、タイムスタンプ＋ログタイプのみのログだから
            if (logInfo.logLine.Length <= 18)
            {
                return;
            }

            // ログをキューに格納する
            this.logInfoQueue.Enqueue(logInfo);

            // LPSを計測する
            this.CountLPS();

            // 最初のログならば動作ログに出力する
            if (!this.firstLogArrived)
            {
                Logger.Write("First log has arrived.");
            }

            this.firstLogArrived = true;
        }

        /// <summary>
        /// OnAddedCombatants
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        #endregion ACT event hander

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
        /// ダメージ関係のログを示すキーワード
        /// </summary>
        /// <remarks>
        /// </remarks>
        public static readonly Regex DamageLogPattern =
            new Regex(
                @"\] 00:..(29|a9|2d|ad):",
                RegexOptions.Compiled |
                RegexOptions.IgnoreCase |
                RegexOptions.ExplicitCapture);

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

        public bool IsEmpty => this.logInfoQueue.IsEmpty;

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

        /// <summary>
        /// ログ行を返す
        /// </summary>
        /// <returns>ログ行の配列</returns>
        public IReadOnlyList<XIVLog> GetLogLines()
        {
            if (this.logInfoQueue.IsEmpty)
            {
                return EmptyLogLineList;
            }

            // プレイヤー情報を取得する
            var player = FFXIVPlugin.Instance.GetPlayer();

            // プレイヤーが召喚士か？
            var palyerIsSummoner = false;
            if (player != null)
            {
                var job = player.AsJob();
                if (job != null)
                {
                    palyerIsSummoner = job.IsSummoner();
                }
            }

            // マッチング用のログリスト
            var list = new List<XIVLog>(logInfoQueue.Count);

            var summoned = false;
            var doneCommand = false;

            var preLog = new string[3];
            var preLogIndex = 0;
#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
#endif
            while (logInfoQueue.TryDequeue(
                out LogLineEventArgs logInfo))
            {
                var logLine = logInfo.logLine;

                // 直前とまったく同じ行はカットする
                if (preLog[0] == logLine ||
                    preLog[1] == logLine ||
                    preLog[2] == logLine)
                {
                    continue;
                }

                preLog[preLogIndex++] = logLine;
                if (preLogIndex >= 3)
                {
                    preLogIndex = 0;
                }

                // 無効なログ行をカットする
                if (IgnoreLogKeywords.Any(x => logLine.Contains(x)))
                {
                    continue;
                }

                // ダメージ系ログをカットする
                if (Settings.Default.IgnoreDamageLogs &&
                    DamageLogPattern.IsMatch(logLine))
                {
                    continue;
                }

                // 詳細なログをカット
                if (Settings.Default.IgnoreDetailLogs &&
                    IgnoreDetailLogKeywords.Any(x => logLine.Contains(x)))
                {
                    continue;
                }

                // エフェクトに付与されるツールチップ文字を除去する
                if (Settings.Default.RemoveTooltipSymbols)
                {
                    // 4文字分のツールチップ文字を除去する
                    int index;
                    if ((index = logLine.IndexOf(
                        TooltipSuffix,
                        0,
                        StringComparison.Ordinal)) > -1)
                    {
                        logLine = logLine.Remove(index - 1, 4);
                    }

                    // 残ったReplacementCharを除去する
                    logLine = logLine.Replace(TooltipReplacementChar, string.Empty);
                }

                // ペットジョブで召喚をしたか？
                if (!summoned &&
                    palyerIsSummoner)
                {
                    summoned = isSummoned(logLine);
                }

                // コマンドとマッチングする
                doneCommand |= TextCommandController.MatchCommandCore(logLine);

                list.Add(new XIVLog(logLine));
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
                ChatLogWorker.Instance.AppendLinesAsync(list);
            }

#if DEBUG
            sw.Stop();
            System.Diagnostics.Debug.WriteLine($"★GetLogLines {sw.Elapsed.TotalMilliseconds:N1} ms");
#endif
            // 冒頭のタイムスタンプを除去して返す
            return list;

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

        #endregion ログ処理

        #region その他のメソッド

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

    public class XIVLog
    {
        public XIVLog(
            string logLine)
        {
            this.Timestamp = logLine.Substring(0, 15).TrimEnd();
            this.Log = logLine.Remove(0, 15);
        }

        public XIVLog(
            string timestamp,
            string log)
        {
            this.Timestamp = timestamp;
            this.Log = log;
        }

        public long No { get; set; } = 0;

        public string Timestamp { get; set; } = string.Empty;

        public string Log { get; set; } = string.Empty;

        public string LogLine => $"{this.Timestamp} {this.Log}";
    }
}
