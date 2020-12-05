using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Models;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.XIVHelper;
using FFXIV_ACT_Plugin.Logfile;

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

        private readonly Lazy<ConcurrentQueue<XIVLog>> LazyXIVLogBuffer = new Lazy<ConcurrentQueue<XIVLog>>(()
            => XIVPluginHelper.Instance.SubscribeXIVLog(() => true));

        public ConcurrentQueue<XIVLog> XIVLogQueue => LazyXIVLogBuffer.Value;

        #region コンストラクター/デストラクター/Dispose

        private const int FALSE = 0;

        private const int TRUE = 1;

        private int disposed = FALSE;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public LogBuffer()
        {
            // Added Combatantsイベントを登録する
            XIVPluginHelper.Instance.AddedCombatants -= this.OnAddedCombatants;
            XIVPluginHelper.Instance.AddedCombatants += this.OnAddedCombatants;

            // 生ログの書き出しバッファを開始する
            ChatLogWorker.Instance.Begin();
        }

        /// <summary>
        /// デストラクター
        /// </summary>
        ~LogBuffer() => this.Dispose();

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref disposed, TRUE, FALSE) != FALSE)
            {
                return;
            }

            XIVPluginHelper.Instance.AddedCombatants -= this.OnAddedCombatants;

            // 生ログの書き出しバッファを停止する
            ChatLogWorker.Instance.End();
        }

        #endregion コンストラクター/デストラクター/Dispose

        #region ACT event hander

#if false
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
#endif

#if false
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
#endif

        /// <summary>
        /// OnAddedCombatants
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAddedCombatants(
            object sender,
            XIVPluginHelper.AddedCombatantsEventArgs e)
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

        /// <summary>
        /// 設定によってカットする場合があるログのキーワード
        /// </summary>
        public static readonly string[] IgnoreDetailLogKeywords = new[]
        {
            LogMessageType.CombatantHP.ToKeyword(),
            LogMessageType.NetworkAbility.ToKeyword(),
            LogMessageType.NetworkAOEAbility.ToKeyword(),
            LogMessageType.NetworkCancelAbility.ToKeyword(),
            LogMessageType.NetworkDoT.ToKeyword(),
            LogMessageType.NetworkEffectResult.ToKeyword(),
            LogMessageType.NetworkStatusList.ToKeyword(),
            LogMessageType.NetworkUpdateHp.ToKeyword(),
        };

        public bool IsEmpty => this.XIVLogQueue.IsEmpty;

        /// <summary>
        /// パーティメンバについてのHPログか？
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public static bool IsHPLogByPartyMember(
            string log)
        {
            if (!log.Contains(LogMessageType.CombatantHP.ToKeyword()))
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
            if (this.XIVLogQueue.IsEmpty)
            {
                return EmptyLogLineList;
            }

            // プレイヤー情報を取得する
            var player = CombatantsManager.Instance.Player;

            // プレイヤーが召喚士か？
            var palyerIsSummoner = false;
            if (player != null)
            {
                var job = player.JobInfo;
                if (job != null)
                {
                    palyerIsSummoner = job.IsSummoner();
                }
            }

            // マッチング用のログリスト
            var list = new List<XIVLog>(this.XIVLogQueue.Count);

            var summoned = false;
            var doneCommand = false;
            var isDefeated = false;
            var isCombatEnd = false;

            var preLog = new string[3];
            var preLogIndex = 0;
#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
#endif
            while (this.XIVLogQueue.TryDequeue(
                out XIVLog xivlog))
            {
                var logLine = xivlog.LogLine;

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

                if (IsAutoIgnoreLog(logLine))
                {
                    continue;
                }

                // ツールチップシンボル, ワールド名を除去する
                logLine = RemoveTooltipSynbols(logLine);
                logLine = RemoveWorldName(logLine);
                xivlog.LogLine = logLine;

                // ペットジョブで召喚をしたか？
                if (!summoned &&
                    palyerIsSummoner)
                {
                    summoned = isSummoned(logLine);
                }

                // 誰かが倒された？
                if (!isDefeated)
                {
                    isDefeated = this.IsDefeated(logLine);
                }

                // 戦闘終了？
                if (!isCombatEnd)
                {
                    isCombatEnd = this.IsCombatEnd(logLine);
                }

                // コマンドとマッチングする
                doneCommand |= TextCommandController.MatchCommandCore(logLine);
                doneCommand |= TextCommandBridge.Instance.TryExecute(logLine);

                list.Add(xivlog);
            }

            if (summoned)
            {
                TableCompiler.Instance.RefreshPetPlaceholder();
            }

            if (isDefeated)
            {
                PluginMainWorker.Instance.ResetCountAtRestart();
            }

            if (isCombatEnd)
            {
                PluginMainWorker.Instance.Wipeout();
            }

            if (doneCommand)
            {
                CommonSounds.Instance.PlayAsterisk();
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

        private static readonly string[] EndDummyTrialLogs = new[]
        {
            "木人討滅戦を達成した！",
            "木人討滅戦に失敗した……",
            "Your trial is a success!",
            "You have failed in your trial...",
            "Vous avez réussi votre entraînement !",
            "Vous avez failli à votre entraînement...",
            "Du hast das Trainingsziel erreicht!",
            "Du hast das Trainingsziel nicht erreicht ...",
        };

        private bool IsDefeated(string logLine)
        {
            var result = false;

            var party = CombatantsManager.Instance.GetPartyList();
            if (party == null ||
                party.Count() < 1)
            {
                var player = CombatantsManager.Instance.Player;
                if (player == null ||
                    player.ID == 0)
                {
                    return false;
                }

                party = new[] { player };
            }

            foreach (var combatant in party)
            {
                result = logLine.Contains($"19:{combatant.Name} was defeated");

                if (result)
                {
                    break;
                }
            }

            return result;
        }

        private bool IsCombatEnd(string logLine)
            => EndDummyTrialLogs.Any(x => logLine.Contains(x));

        /// <summary>
        /// 自動カット対象のログか？
        /// </summary>
        /// <param name="logLine"></param>
        /// <returns></returns>
        public static bool IsAutoIgnoreLog(
            string logLine)
        {
            if (!Settings.Default.IsAutoIgnoreLogs)
            {
                return false;
            }

            if (CombatantsManager.Instance.CombatantsPCCount <= 16)
            {
                return false;
            }

            return IgnoreDetailLogKeywords.Any(x => logLine.Contains(x));
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

        public static string RemoveWorldName(
            string logLine)
        {
            var result = logLine;

            if (!Settings.Default.RemoveWorldName)
            {
                return result;
            }

            if (!logLine.Contains("] 00:") &&
                !logLine.StartsWith("00:"))
            {
                return result;
            }

            result = XIVPluginHelper.Instance.RemoveWorldName(logLine);

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
            var player = CombatantsManager.Instance.Player;
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

        private static double previousPetDistance = 0;

        /// <summary>
        /// ペットとの距離をログにダンプする
        /// </summary>
        public static void DumpMyPetDistance()
        {
            var player = CombatantsManager.Instance.Player;
            if (player == null ||
                !player.IsPetJob)
            {
                previousPetDistance = 0;
                return;
            }

            var combatants = CombatantsManager.Instance.GetCombatants();

            var pet = combatants.FirstOrDefault(x =>
                x.OwnerID == player.ID);

            if (pet == null)
            {
                previousPetDistance = 0;
                return;
            }

            var distance = pet.HorizontalDistanceByPlayer;

            try
            {
                var distance10m = (int)distance / 10;
                var distance10mPrevious = (int)previousPetDistance / 10;

                if (distance10m <= distance10mPrevious ||
                    distance10m < 3)
                {
                    return;
                }

                LogParser.RaiseLog(
                    DateTime.Now,
                    $"[EX] Pet distance is over {distance10m * 10:N0}m.");
            }
            finally
            {
                previousPetDistance = distance;
            }
        }

        #endregion その他のメソッド
    }
}
