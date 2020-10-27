using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Config.Models;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.Sound;
using ACT.SpecialSpellTimer.Utility;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;

namespace ACT.SpecialSpellTimer
{
    /// <summary>
    /// SpellTimerの中核
    /// </summary>
    public class PluginMainWorker
    {
        private const int INVALID = 1;

        private const int VALID = 0;

        #region Singleton

        /// <summary>
        /// シングルトンinstance
        /// </summary>
        private static PluginMainWorker instance;

        /// <summary>
        /// シングルトンinstance
        /// </summary>
        public static PluginMainWorker Instance =>
            instance ?? (instance = new PluginMainWorker());

        public static void Free() => instance = null;

        #endregion Singleton

        #region Thread

        private System.Timers.Timer backgroudWorker;
        private ThreadWorker detectLogsWorker;
        private ThreadWorker syncHotbarWorker;
        private volatile bool isOver;
        private DispatcherTimer refreshSpellOverlaysWorker;
        private DispatcherTimer refreshTickerOverlaysWorker;

        #endregion Thread

        private volatile bool existFFXIVProcess;

        public bool IsFFXIVActive => XIVPluginHelper.Instance.IsFFXIVActive;

        private DateTime lastSaveTickerTableDateTime = DateTime.Now;

        public LogBuffer LogBuffer { get; private set; }

        public bool InSimulation { get; set; }

        #region Begin / End

        /// <summary>
        /// 開始する
        /// </summary>
        public async void Begin()
        {
            this.isOver = false;

            // FFXIVのスキャンを開始する
            // FFXIVプラグインへのアクセスを開始する
            await Task.Run(() => XIVPluginHelper.Instance.Start(
                Settings.Default.LogPollSleepInterval,
                Settings.Default.FFXIVLocale));

            // ログバッファを生成する
            this.LogBuffer = new LogBuffer();

            await Task.Run(() =>
            {
                // テーブルコンパイラを開始する
                TableCompiler.Instance.Begin();

                // サウンドコントローラを開始する
                SoundController.Instance.Begin();
            });

            // Overlayの更新スレッドを開始する
            this.BeginOverlaysThread();

            // ログ監視タイマを開始する
            this.detectLogsWorker = new ThreadWorker(
                () => this.DetectLogsCore(),
                0,
                nameof(this.detectLogsWorker));

            // ホットバー同期タイマを開始する
            this.syncHotbarWorker = new ThreadWorker(
                () => this.SyncHotbarCore(),
                SyncHotbarInterval,
                nameof(this.syncHotbarWorker),
                ThreadPriority.BelowNormal);

            // Backgroudスレッドを開始する
            this.backgroudWorker = new System.Timers.Timer();
            this.backgroudWorker.AutoReset = true;
            this.backgroudWorker.Interval = 5000;
            this.backgroudWorker.Elapsed += (s, e) =>
            {
                this.BackgroundCore();
            };

            this.detectLogsWorker.Run();
            this.syncHotbarWorker.Run();
            this.backgroudWorker.Start();
        }

        private int refreshingSpellsLock;
        private int refreshingTickersLock;

        public void BeginOverlaysThread()
        {
            // スペルのスレッドを開始する
            this.refreshSpellOverlaysWorker = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(Settings.Default.RefreshInterval)
            };

            this.refreshSpellOverlaysWorker.Tick += (s, e) =>
            {
                if (Interlocked.CompareExchange(ref this.refreshingSpellsLock, 1, 0) < 1)
                {
                    try
                    {
                        this.RefreshSpellOverlaysCore();
                    }
                    catch (Exception ex)
                    {
                        Logger.Write("refresh spell overlays error:", ex);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref this.refreshingSpellsLock, 0);
                    }
                }
            };

            this.refreshingSpellsLock = 0;
            this.refreshSpellOverlaysWorker.Start();

            // テロップのスレッドを開始する
            this.refreshTickerOverlaysWorker = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(Settings.Default.RefreshInterval)
            };

            this.refreshTickerOverlaysWorker.Tick += (s, e) =>
            {
                if (Interlocked.CompareExchange(ref this.refreshingTickersLock, 1, 0) < 1)
                {
                    try
                    {
                        this.RefreshTickerOverlaysCore();
                    }
                    catch (Exception ex)
                    {
                        Logger.Write("refresh ticker overlays error:", ex);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref this.refreshingTickersLock, 0);
                    }
                }
            };

            this.refreshingTickersLock = 0;
            this.refreshTickerOverlaysWorker.Start();
        }

        /// <summary>
        /// 終了する
        /// </summary>
        public void End()
        {
            this.isOver = true;

            // Workerを開放する
            this.refreshSpellOverlaysWorker?.Stop();
            this.refreshTickerOverlaysWorker?.Stop();
            this.detectLogsWorker?.Abort();
            this.syncHotbarWorker?.Abort();

            this.refreshSpellOverlaysWorker = null;
            this.refreshTickerOverlaysWorker = null;
            this.detectLogsWorker = null;
            this.syncHotbarWorker = null;

            this.backgroudWorker?.Stop();
            this.backgroudWorker?.Dispose();
            this.backgroudWorker = null;

            // ログバッファを開放する
            if (this.LogBuffer != null)
            {
                this.LogBuffer.Dispose();
                this.LogBuffer = null;
            }

            // Windowを閉じる
            SpellsController.Instance.ClosePanels();
            TickersController.Instance.CloseTelops();
            SpellsController.Instance.ExecuteClosePanels();
            TickersController.Instance.ExecuteCloseTelops();

            // 設定を保存する
            Settings.Default.Save();
            SpellPanelTable.Instance.Save();
            SpellTable.Instance.Save();
            TickerTable.Instance.Save();
            TagTable.Instance.Save();

            // サウンドコントローラを停止する
            SoundController.Instance.End();

            // テーブルコンパイラを停止する
            TableCompiler.Instance.End();
            TableCompiler.Free();

            // FFXIVのスキャンを停止する
            XIVPluginHelper.Instance.End();
            XIVPluginHelper.Free();
        }

        #endregion Begin / End

        #region Core

        private double lastLPS;
        private int lastActiveTriggerCount;
        private int lastLogedActiveTriggerCount;
        private EorzeaTime previousET = EorzeaTime.Now;

        private void BackgroundCore()
        {
            // FFXIVプロセスの有無を取得する
            this.existFFXIVProcess =
                XIVPluginHelper.Instance.CurrentFFXIVProcess != null &&
                !XIVPluginHelper.Instance.CurrentFFXIVProcess.HasExited;

            if ((DateTime.Now - this.lastSaveTickerTableDateTime).TotalMinutes >= 1)
            {
                this.lastSaveTickerTableDateTime = DateTime.Now;

                if (this.existFFXIVProcess)
                {
                    // ついでにLPSを出力する
                    var lps = XIVPluginHelper.Instance.LPS;
                    if (lps > 0 &&
                        this.lastLPS != lps)
                    {
                        Logger.Write($"LPS={lps.ToString("N1")}");
                        this.lastLPS = lps;
                    }

                    // ついでにアクティブなトリガ数を出力する
                    var count = this.lastActiveTriggerCount;
                    if (count > 0 &&
                        this.lastLogedActiveTriggerCount != count)
                    {
                        this.lastLogedActiveTriggerCount = count;
                        Logger.Write($"ActiveTriggers={count.ToString("N0")}");
                    }
                }
            }

            if (this.existFFXIVProcess)
            {
                var zoneID = XIVPluginHelper.Instance.GetCurrentZoneID();
                var zoneName = XIVPluginHelper.Instance.GetCurrentZoneName();

                // ついでにETを出力する
                var nowET = EorzeaTime.Now;
                if (nowET.Hour != this.previousET.Hour)
                {
                    LogParser.RaiseLog(
                        DateTime.Now,
                        $"[EX] ETTick ET{nowET.Hour:00}:00 Zone:{zoneID:000} {zoneName}");
                }

                this.previousET = nowET;
            }
        }

        /// <summary>
        /// ログを監視する
        /// </summary>
        private void DetectLogsCore()
        {
            var existsLog = false;

            if (!this.InSimulation)
            {
                if (!Settings.Default.VisibleOverlayWithoutFFXIV)
                {
                    // FFXIVがいない？
                    if (!this.existFFXIVProcess)
                    {
#if !DEBUG
                        // importログの解析用にログを取り出しておく
                        if (!this.LogBuffer.IsEmpty)
                        {
                            this.LogBuffer.GetLogLines();
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(3));
                        return;
#endif
                    }
                }
            }

            // ログがないなら抜ける
            if (this.LogBuffer.IsEmpty)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(Settings.Default.LogPollSleepInterval));
                return;
            }
#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
#endif
            // ログを取り出す
            // 0D: 残HP率 のログは判定から除外する
            var logsTask = Task.Run(() => this.LogBuffer.GetLogLines());

            // 有効なスペルとテロップのリストを取得する
            var triggers = TableCompiler.Instance.TriggerList;
            this.lastActiveTriggerCount = triggers.Count;

            var xivlogs = logsTask.Result;
            if (xivlogs.Count > 0)
            {
                if (triggers.Count > 0)
                {
                    SpellsController.Instance.StoreHotbarInfo();

                    triggers.AsParallel().ForAll((trigger) =>
                    {
                        foreach (var xivlog in xivlogs)
                        {
                            trigger.MatchTrigger(xivlog.LogLine);
                        }
                    });
                }

                existsLog = true;
            }

#if DEBUG
            sw.Stop();
            if (xivlogs.Count != 0)
            {
                var time = sw.ElapsedMilliseconds;
                var count = xivlogs.Count;
                System.Diagnostics.Debug.WriteLine(
                    $"●DetectLogs\t{time:N1} ms\t{count:N0} lines\tavg {time / count:N2}");
            }
#endif

            if (existsLog)
            {
                Thread.Yield();
            }
            else
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(Settings.Default.LogPollSleepInterval));
            }
        }

        private static readonly int SyncHotbarInterval = 100;
        private static readonly int SyncHotbarIdelInterval = 5000;

        private void SyncHotbarCore()
        {
            if (!this.InSimulation)
            {
                if (!Settings.Default.VisibleOverlayWithoutFFXIV)
                {
                    if (!this.existFFXIVProcess)
                    {
                        this.syncHotbarWorker.Interval = SyncHotbarIdelInterval;
                        return;
                    }
                }
            }

            SpellsController.Instance.StoreHotbarInfo();

            var exists = false;
            var spells = TableCompiler.Instance.SpellList;
            foreach (var spell in spells)
            {
                exists |= spell.UseHotbarRecastTime && !string.IsNullOrEmpty(spell.HotbarName);
                SpellsController.Instance.UpdateHotbarRecast(spell);
                Thread.Yield();
            }

            this.syncHotbarWorker.Interval = exists ? SyncHotbarInterval : SyncHotbarIdelInterval;
        }

        private void RefreshSpellOverlaysCore()
        {
            // Close要求を処理する
            SpellsController.Instance.ExecuteClosePanels();

            if (this.isOver)
            {
                return;
            }

            // 有効なスペルを取得する
            var spells = TableCompiler.Instance.TriggerList
                .Where(x => x.ItemType == ItemTypes.Spell)
                .Select(x => x as Spell)
                .Concat(SpellTable.Instance.GetInstanceSpells())
                .ToList();

            var isHideOverlay =
                !Settings.Default.OverlayVisible ||
                (Settings.Default.HideWhenNotActive && !this.IsFFXIVActive);

            // FFXIVが実行されていない？
            if (!this.InSimulation)
            {
                if (!Settings.Default.VisibleOverlayWithoutFFXIV &&
                    !this.existFFXIVProcess)
                {
                    var toShow = spells.Where(x =>
                    {
                        if (x.IsDesignMode || x.IsTest)
                        {
                            return true;
                        }

                        if (x.Panel?.SortOrder == SpellOrders.Fixed)
                        {
                            return true;
                        }

                        return false;
                    });

                    // 一時表示スペルがない？
                    if (!toShow.Any())
                    {
                        SpellsController.Instance.ClosePanels();
                        return;
                    }

                    if (!isHideOverlay)
                    {
                        // 一時表示スペルだけ表示する
                        SpellsController.Instance.RefreshSpellOverlays(toShow.ToList());
                        return;
                    }
                }
            }

            if (isHideOverlay)
            {
                SpellsController.Instance.HidePanels();
                return;
            }

            // スペルWindowを表示する
            SpellsController.Instance.RefreshSpellOverlays(spells);
        }

        private void RefreshTickerOverlaysCore()
        {
            // Close要求を処理する
            TickersController.Instance.ExecuteCloseTelops();

            if (this.isOver)
            {
                return;
            }

            // 有効なテロップを取得する
            var telops = TableCompiler.Instance.TriggerList
                .Where(x => x.ItemType == ItemTypes.Ticker)
                .Select(x => x as Ticker)
                .ToList();

#if DEBUG
            if (telops.Any(x => x.Title.Contains("TEST")))
            {
                ;
            }
#endif

            var isHideOverlay =
                !Settings.Default.OverlayVisible ||
                (Settings.Default.HideWhenNotActive && !this.IsFFXIVActive);

            // FFXIVが実行されていない？
            if (!this.InSimulation)
            {
                if (!Settings.Default.VisibleOverlayWithoutFFXIV &&
                    !this.existFFXIVProcess)
                {
                    // デザインモードのテロップがない？
                    // テストモードのテロップがない？
                    if (!telops.Any(x =>
                        x.IsDesignMode ||
                        x.IsTest))
                    {
                        TickersController.Instance.CloseTelops();
                        return;
                    }

                    if (!isHideOverlay)
                    {
                        TickersController.Instance.RefreshTelopOverlays(
                            telops.Where(x =>
                                x.IsDesignMode ||
                                x.IsTest).ToList());
                        return;
                    }
                }
            }

            if (isHideOverlay)
            {
                TickersController.Instance.HideTelops();
                return;
            }

            // テロップWindowを表示する
            TickersController.Instance.RefreshTelopOverlays(telops);
        }

        #endregion Core

        #region Misc

        private DateTime lastWipeOutDetectDateTime = DateTime.Now;
        private DateTime lastWipeOutDateTime = DateTime.Now;

        /// <summary>
        /// リスタートのときスペルのカウントをリセットする
        /// </summary>
        public void ResetCountAtRestart()
        {
            // 無効？
            if (!Settings.Default.ResetOnWipeOut)
            {
                return;
            }

            if ((DateTime.Now - this.lastWipeOutDetectDateTime).TotalSeconds <= 0.1)
            {
                return;
            }

            this.lastWipeOutDetectDateTime = DateTime.Now;

            var player = CombatantsManager.Instance.Player;
            var party = CombatantsManager.Instance.GetPartyList();

            if (party == null ||
                party.Count() < 1)
            {
                if (player == null ||
                    player.ID == 0)
                {
                    return;
                }

                party = new[] { player };
            }

            // 異常なデータ？
            if (party.Count() > 1)
            {
                var first = party.First();
                if (party.Count() ==
                    party.Count(x =>
                        x.CurrentHP == first.CurrentHP &&
                        x.MaxHP == first.MaxHP))
                {
                    return;
                }

                if (!party.Any(x => x.IsPlayer))
                {
                    return;
                }
            }

            if (player != null)
            {
                switch (player.JobInfo.Role)
                {
                    case Roles.Crafter:
                    case Roles.Gatherer:
                        return;
                }
            }

            // 関係者が全員死んでる？
            if (party.Count() ==
                party.Count(x =>
                    x.CurrentHP <= 0 &&
                    x.MaxHP > 0))
            {
                this.Wipeout();
            }
        }

        public void Wipeout(
            bool isRaiseWipeoutLog = true)
        {
            // リセットするのは10秒に1回にする
            // 暗転中もずっとリセットし続けてしまうので
            var now = DateTime.Now;
            if ((now - this.lastWipeOutDateTime).TotalSeconds >= 10.0)
            {
                this.lastWipeOutDateTime = now;

                Task.Run(() =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));

                    // ACT本体に戦闘終了を通知する
                    if (Settings.Default.WipeoutNotifyToACT)
                    {
                        ActInvoker.Invoke(() => ActGlobals.oFormActMain.EndCombat(true));
                        CommonSounds.Instance.PlayWipeout();
                    }

                    // トリガーをリセットする
                    SpellTable.ResetCount();
                    TickerTable.Instance.ResetCount();

                    // wipeoutログを発生させる
                    if (isRaiseWipeoutLog)
                    {
                        Task.Run(() =>
                        {
                            Thread.Sleep(200);
                            LogParser.RaiseLog(now, WipeoutKeywords.Wipeout);
                        });
                    }
                });
            }
        }

        #endregion Misc
    }
}
