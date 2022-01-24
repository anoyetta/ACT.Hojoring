using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.Utility;
using ACT.SpecialSpellTimer.Views;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Views;
using FFXIV.Framework.XIVHelper;
using Sharlayan.Core;

namespace ACT.SpecialSpellTimer
{
    public class SpellsController
    {
        #region Singleton

        private static SpellsController instance = new SpellsController();

        public static SpellsController Instance => instance;

        #endregion Singleton

        /// <summary>
        /// Spellをマッチングする
        /// </summary>
        /// <param name="spells">Spell</param>
        /// <param name="logLines">ログ</param>
        public void Match(
            IReadOnlyList<Models.Spell> spells,
            IReadOnlyList<string> logLines)
        {
            if (spells.Count < 1 ||
                logLines.Count < 1)
            {
                return;
            }

#if DEBUG
            var sw = Stopwatch.StartNew();
#endif
            try
            {
                logLines.AsParallel().ForAll(logLine =>
                {
                    foreach (var spell in spells)
                    {
                        try
                        {
                            spell.StartMatching();
                            this.MatchCore(spell, logLine);
                        }
                        finally
                        {
                            spell.EndMatching();
                        }
                    }
                });
            }
            finally
            {
#if DEBUG
                sw.Stop();
                Debug.WriteLine($"●SpellsController.Match() {sw.Elapsed.TotalMilliseconds:N1}ms spells={spells.Count:N0} lines={logLines.Count:N0}");
#endif
            }
        }

        /// <summary>
        /// 1ログ1スペルに対して判定する
        /// </summary>
        /// <param name="spell">スペル</param>
        /// <param name="logLine">ログ</param>
        public void MatchCore(
            Models.Spell spell,
            string logLine)
        {
            var regex = spell.Regex;
            var notifyNeeded = false;

            if (!spell.IsInstance)
            {
                // マッチング計測開始
                spell.StartMatching();

                // 開始条件を確認する
                if (ConditionUtility.CheckConditionsForSpell(spell))
                {
                    // 正規表現が無効？
                    if (!spell.RegexEnabled ||
                        regex == null)
                    {
                        var keyword = spell.KeywordReplaced;
                        if (string.IsNullOrWhiteSpace(keyword))
                        {
                            return;
                        }

                        // キーワードが含まれるか？
                        if (logLine.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            var targetSpell = spell;

                            // ヒットしたログを格納する
                            targetSpell.MatchedLog = logLine;

                            // スペル名（表示テキスト）を置換する
                            var replacedTitle = ConditionUtility.GetReplacedTitle(targetSpell);

                            // PC名を置換する
                            replacedTitle = XIVPluginHelper.Instance.ReplacePartyMemberName(
                                replacedTitle,
                                Settings.Default.PCNameInitialOnDisplayStyle);

                            targetSpell.SpellTitleReplaced = replacedTitle;
                            targetSpell.UpdateDone = false;
                            targetSpell.OverDone = false;
                            targetSpell.BeforeDone = false;
                            targetSpell.TimeupDone = false;

                            var now = DateTime.Now;

                            // ホットバーからリキャスト時間の読込みを試みる
                            if (!this.TryGetHotbarRecast(targetSpell, out double d))
                            {
                                d = targetSpell.RecastTime;
                            }

                            targetSpell.CompleteScheduledTime = now.AddSeconds(d);
                            targetSpell.MatchDateTime = now;

                            // マッチング計測終了
                            spell.EndMatching();

                            // マッチ時点のサウンドを再生する
                            targetSpell.Play(targetSpell.MatchSound, targetSpell.MatchAdvancedConfig);
                            targetSpell.Play(targetSpell.MatchTextToSpeak, targetSpell.MatchAdvancedConfig);

                            notifyNeeded = true;

                            // 遅延サウンドタイマを開始する
                            targetSpell.StartOverSoundTimer();
                            targetSpell.StartBeforeSoundTimer();
                            targetSpell.StartTimeupSoundTimer();
                        }
                    }
                    else
                    {
                        // 正規表現でマッチングする
                        var match = regex.Match(logLine);
                        if (match.Success)
                        {
#if DEBUG
                            if (logLine.Contains("MARK"))
                            {
                                Debug.WriteLine("MARK");
                            }
#endif
                            var targetSpell = default(Spell);

                            void setTitle()
                            {
                                targetSpell = spell;

                                // ヒットしたログを格納する
                                targetSpell.MatchedLog = logLine;

                                // スペル名（表示テキスト）を置換する
                                var replacedTitle = match.Result(ConditionUtility.GetReplacedTitle(targetSpell));

                                // PC名を置換する
                                replacedTitle = XIVPluginHelper.Instance.ReplacePartyMemberName(
                                    replacedTitle,
                                    Settings.Default.PCNameInitialOnDisplayStyle);

                                // インスタンス化する？
                                if (targetSpell.ToInstance)
                                {
                                    // 同じタイトルのインスタンススペルを探す
                                    // 存在すればそれを使用して、なければ新しいインスタンスを生成する
                                    targetSpell = SpellTable.Instance.GetOrAddInstance(
                                        replacedTitle,
                                        targetSpell);
                                }
                                else
                                {
                                    targetSpell.SpellTitleReplaced = replacedTitle;
                                }
                            }

                            // スペルタイトルを編集する
                            setTitle();

                            targetSpell.UpdateDone = false;
                            targetSpell.OverDone = false;
                            targetSpell.BeforeDone = false;
                            targetSpell.TimeupDone = false;

                            var now = DateTime.Now;

                            // 効果時間を決定する
                            // グループ "duration" をキャプチャーしていた場合は効果時間を置換する
                            // 最大値9999を超えていた場合は無視する
                            var duration = targetSpell.RecastTime;

                            if (RegexExtensions.TryGetDuration(match, out double d))
                            {
                                duration = d;
                            }
                            else
                            {
                                // ホットバーからリキャスト時間の読込みを試みる
                                if (this.TryGetHotbarRecast(targetSpell, out double durationFromHotbar))
                                {
                                    duration = durationFromHotbar;
                                }
                            }

                            targetSpell.CompleteScheduledTime = now.AddSeconds(duration);

                            // スペル対象を保存する
                            // グループ "target" をキャプチャーしていた場合はその文字列を保存する
                            var targetName = match.Groups["target"].Value;
                            if (!string.IsNullOrWhiteSpace(targetName))
                            {
                                targetSpell.TargetName = targetName;
                            }

                            // マッチ日時を格納する
                            targetSpell.MatchDateTime = now;

                            // マッチング計測終了
                            spell.EndMatching();

                            // マッチ時点のサウンドを再生する
                            targetSpell.Play(targetSpell.MatchSound, targetSpell.MatchAdvancedConfig);

                            if (!string.IsNullOrWhiteSpace(targetSpell.MatchTextToSpeak))
                            {
                                var tts = match.Result(targetSpell.MatchTextToSpeak);
                                targetSpell.Play(tts, targetSpell.MatchAdvancedConfig);
                            }

                            notifyNeeded = true;

                            // 遅延サウンドタイマを開始する
                            targetSpell.StartOverSoundTimer();
                            targetSpell.StartBeforeSoundTimer();
                            targetSpell.StartTimeupSoundTimer();
                        }
                    }
                }
            }

            // 延長をマッチングする
            if (spell.MatchDateTime > DateTime.MinValue || spell.ToInstance)
            {
                var targetSpell = spell;

                var keywords = new string[] { targetSpell.KeywordForExtendReplaced1, targetSpell.KeywordForExtendReplaced2, targetSpell.KeywordForExtendReplaced3 };
                var regexes = new Regex[] { targetSpell.RegexForExtend1, targetSpell.RegexForExtend2, targetSpell.RegexForExtend3 };
                var timeToExtends = new double[] { targetSpell.RecastTimeExtending1, targetSpell.RecastTimeExtending2, targetSpell.RecastTimeExtending3 };

                for (int i = 0; i < keywords.Length; i++)
                {
                    var keywordToExtend = keywords[i];
                    var regexToExtend = regexes[i];
                    var timeToExtend = timeToExtends[i];

                    // マッチングする
                    var extended = false;

                    if (!targetSpell.RegexEnabled ||
                        regexToExtend == null)
                    {
                        if (!string.IsNullOrWhiteSpace(keywordToExtend))
                        {
                            extended = logLine.Contains(keywordToExtend, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    else
                    {
                        var match = regexToExtend.Match(logLine);
                        extended = match.Success;

                        if (extended)
                        {
                            // ヒットしたログを格納する
                            targetSpell.MatchedLog = logLine;

                            // スペル名（表示テキスト）を置換する
                            var replacedTitle = match.Result(ConditionUtility.GetReplacedTitle(targetSpell));

                            // PC名を置換する
                            replacedTitle = XIVPluginHelper.Instance.ReplacePartyMemberName(
                                replacedTitle,
                                Settings.Default.PCNameInitialOnDisplayStyle);

                            // インスタンス化する？
                            if (targetSpell.ToInstance)
                            {
                                // 同じタイトルのインスタンススペルを探す
                                // 存在すればそれを使用して、なければ新しいインスタンスを生成する
                                targetSpell = SpellTable.Instance.GetOrAddInstance(
                                    replacedTitle,
                                    targetSpell);
                            }
                            else
                            {
                                targetSpell.SpellTitleReplaced = replacedTitle;
                            }

                            // targetをキャプチャーしている？
                            if (!string.IsNullOrWhiteSpace(targetSpell.TargetName))
                            {
                                var targetName = match.Groups["target"].Value;
                                if (!string.IsNullOrWhiteSpace(targetName))
                                {
                                    // targetが当初のマッチングと一致するか確認する
                                    if (targetSpell.TargetName != targetName)
                                    {
                                        extended = false;
                                    }
                                }
                            }
                        }
                    }

                    if (!extended)
                    {
                        continue;
                    }

                    var now = DateTime.Now;

                    // リキャストタイムを延長する
                    var newSchedule = targetSpell.CompleteScheduledTime.AddSeconds(timeToExtend);
                    targetSpell.BeforeDone = false;
                    targetSpell.UpdateDone = false;

                    if (targetSpell.ExtendBeyondOriginalRecastTime)
                    {
                        if (targetSpell.UpperLimitOfExtension > 0)
                        {
                            var newDuration = (newSchedule - now).TotalSeconds;
                            if (newDuration > (double)targetSpell.UpperLimitOfExtension)
                            {
                                newSchedule = newSchedule.AddSeconds(
                                    (newDuration - (double)targetSpell.UpperLimitOfExtension) * -1);
                            }
                        }
                    }
                    else
                    {
                        var newDuration = (newSchedule - now).TotalSeconds;
                        if (newDuration > (double)targetSpell.RecastTime)
                        {
                            newSchedule = newSchedule.AddSeconds(
                                (newDuration - (double)targetSpell.RecastTime) * -1);
                        }
                    }

                    targetSpell.CompleteScheduledTime = newSchedule;

                    if (!targetSpell.IsNotResetBarOnExtended)
                    {
                        targetSpell.MatchDateTime = now;
                    }

                    notifyNeeded = true;

                    // 遅延サウンドタイマを開始(更新)する
                    targetSpell.StartOverSoundTimer();
                    targetSpell.StartBeforeSoundTimer();
                    targetSpell.StartTimeupSoundTimer();
                }
            }
            // end if 延長マッチング

            // ACT標準のSpellTimerに変更を通知する
            if (notifyNeeded)
            {
                this.UpdateNormalSpellTimer(spell, false);
                this.NotifyNormalSpellTimer(spell);
            }
        }

        private Dictionary<string, ActionItem> hotbarInfoDictionary;

        private Dictionary<string, ActionItem> GetHotbarInfo()
            => this.hotbarInfoDictionary;

        private static readonly double HotbarAdjustThreshold = 1200d;
        private static readonly double HotbarPollingRate = 4;

        public void StoreHotbarInfo()
        {
            SharlayanHelper.Instance.ActionsPollingInterval = HotbarAdjustThreshold / HotbarPollingRate;
            this.hotbarInfoDictionary = SharlayanHelper.Instance.ActionDictionary;
        }

        public bool UpdateHotbarRecast(
            Spell spell)
        {
            var result = false;

            if (!spell.UseHotbarRecastTime ||
                string.IsNullOrEmpty(spell.HotbarName))
            {
                return result;
            }

            var now = DateTime.Now;

            if ((now - spell.MatchDateTime).TotalSeconds <= 0.1)
            {
                return result;
            }

            if (spell.CompleteScheduledTime.AddMilliseconds(HotbarAdjustThreshold * 2) >= now)
            {
                if (!this.TryGetHotbarRecast(spell, out double d))
                {
                    return result;
                }

                var newSchedule = now.AddSeconds(d);

                // ホットバー情報とnミリ秒以上乖離したら補正する
                var gap = Math.Abs((newSchedule - spell.CompleteScheduledTime).TotalMilliseconds);
                if (d > 1.0 &&
                    gap >= HotbarAdjustThreshold)
                {
                    result = true;

                    if (spell.CompleteScheduledTime.AddSeconds(-1) <= now)
                    {
                        spell.MatchDateTime = now;
                        spell.OverDone = false;
                        spell.TimeupDone = false;
                    }

                    spell.CompleteScheduledTime = newSchedule;
                    spell.BeforeDone = false;
                    spell.UpdateDone = false;

                    spell.StartOverSoundTimer();
                    spell.StartBeforeSoundTimer();
                    spell.StartTimeupSoundTimer();
                }
            }

            return result;
        }

        /// <summary>
        /// SpellTimerのPanelリスト
        /// </summary>
        private volatile List<ISpellPanelWindow> spellPanelWindows =
            new List<ISpellPanelWindow>();

        /// <summary>
        /// スペルパネルWindowを更新する
        /// </summary>
        /// <param name="spells">
        /// 対象のスペル</param>
        public void RefreshSpellOverlays(
            IReadOnlyList<Models.Spell> spells)
        {
            var doneTest = false;

            var query =
                from s in spells
                where
                s.Panel != null &&
                (
                    !s.ToInstance ||
                    s.IsDesignMode
                )
                group s by s.Panel.PanelName.Trim();

            var panelWindow = default(ISpellPanelWindow);
            foreach (var spellsByPanel in query)
            {
                var panel = spellsByPanel.First().Panel;
                panelWindow = panel.PanelWindow;

                // 違うタイプのウィンドウならば閉じる
                if (panel.EnabledAdvancedLayout)
                {
                    if (panelWindow is SpellPanelWindow window)
                    {
                        window.Close();
                        lock (this.spellPanelWindows)
                        {
                            this.spellPanelWindows.Remove(window);
                        }

                        panelWindow = null;
                    }
                }
                else
                {
                    if (panelWindow is AdvancedSpellPanelWindow window)
                    {
                        window.Close();
                        lock (this.spellPanelWindows)
                        {
                            this.spellPanelWindows.Remove(window);
                        }

                        panelWindow = null;
                    }
                }

                if (panelWindow == null)
                {
                    panelWindow = AdvancedSpellPanelWindow.GetWindow(panel);
                    panelWindow.ToWindow().Title = "Spell Panel - " + panel.PanelName;

                    lock (this.spellPanelWindows)
                    {
                        this.spellPanelWindows.Add(panelWindow);
                    }

                    /// このパネルに属するスペルを再描画させる
                    foreach (var spell in spellsByPanel)
                    {
                        spell.UpdateDone = false;
                    }

                    panelWindow.ToWindow().Show();
                    panelWindow.ToWindow().Topmost = false;
                }

                // クリックスルーを反映する
                panelWindow.IsClickthrough = Settings.Default.ClickThroughEnabled;

                panelWindow.Spells = spellsByPanel.ToArray();
                panelWindow.Refresh();

                // テストモードの終了を判定する
                foreach (var test in spellsByPanel.Where(x => x.IsTest))
                {
                    if (DateTime.Now >
                        test.CompleteScheduledTime.AddSeconds(Settings.Default.TimeOfHideSpell))
                    {
                        test.IsTest = false;
                        doneTest = true;
                    }
                }
            }

            // 不要なWindow（デザインモードの残骸など）を閉じる
            lock (this.spellPanelWindows)
            {
                var toHide = this.spellPanelWindows
                    .Where(x => !query.Any(y => y.Key == x.Panel.PanelName));
                foreach (var window in toHide)
                {
                    window.HideOverlay();
                    (window as Window).Topmost = false;
                }
            }

            // TESTモードが終わったならフィルタし直す
            if (doneTest)
            {
                TableCompiler.Instance.CompileSpells();
            }
        }

        private bool TryGetHotbarRecast(
            Spell spell,
            out double recastTime)
        {
            var result = false;
            recastTime = 0;

            if (spell.UseHotbarRecastTime &&
                !string.IsNullOrEmpty(spell.HotbarName))
            {
                var actions = this.GetHotbarInfo();
                if (actions != null)
                {
                    if (actions.ContainsKey(spell.HotbarName))
                    {
                        var action = actions[spell.HotbarName];

                        if (action.RemainingCost > 0)
                        {
                            recastTime = action.RemainingCost;
                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        #region Panel controller

        /// <summary>
        /// Panelを閉じる
        /// </summary>
        public void ClosePanels()
        {
            lock (this.spellPanelWindows)
            {
                foreach (var panel in SpellPanelTable.Instance.Table)
                {
                    panel.ToClose = true;
                }
            }
        }

        public void ExecuteClosePanels()
        {
            var closed = false;

            lock (this.spellPanelWindows)
            {
                var targets = SpellPanelTable.Instance.Table
                    .Where(x => x.ToClose).ToList();

                foreach (var panel in targets)
                {
                    var window = panel.PanelWindow;
                    if (window == null)
                    {
                        continue;
                    }

                    if (panel.ToClose)
                    {
                        panel.ToClose = false;

                        window.ToWindow().Close();
                        this.spellPanelWindows.Remove(window);

                        closed = true;
                    }
                }
            }

            if (closed)
            {
                SpellPanelTable.Instance.Save();
            }
        }

        /// <summary>
        /// 不要なスペルタイマWindowを閉じる
        /// </summary>
        /// <param name="spells">Spell</param>
        public void GarbageSpellPanelWindows(
            IReadOnlyList<Models.Spell> spells)
        {
            lock (this.spellPanelWindows)
            {
                foreach (var panel in SpellPanelTable.Instance.Table)
                {
                    // スペルリストに存在しないパネルを閉じる
                    if (!spells.Any(x => x.PanelID == panel.ID))
                    {
                        panel.ToClose = true;
                    }
                }
            }
        }

        /// <summary>
        /// Panelを隠す
        /// </summary>
        public void HidePanels()
        {
            lock (this.spellPanelWindows)
            {
                foreach (var panel in SpellPanelTable.Instance.Table)
                {
                    if (panel.PanelWindow != null &&
                        panel.PanelWindow is Window window)
                    {
                        window.Topmost = false;
                    }

                    if (panel.PanelWindow != null)
                    {
                        panel.PanelWindow.HideOverlay();
                        (panel.PanelWindow as Window).Topmost = false;
                    }
                }
            }
        }

        #endregion Panel controller

        #region To Normal SpellTimer

        /// <summary>
        /// 有効なSpellTimerをACT標準のSpellTimerに設定を反映させる
        /// </summary>
        public void ApplyToNormalSpellTimer()
        {
            // 標準スペルタイマーへの通知が無効であれば何もしない
            if (!Settings.Default.EnabledNotifyNormalSpellTimer)
            {
                return;
            }

            // 設定を一旦すべて削除する
            ClearNormalSpellTimer();

            var spells = SpellTable.Instance.Table.Where(x => x.Enabled);
            foreach (var spell in spells)
            {
                UpdateNormalSpellTimer(spell, true);
            }

            var telops = TickerTable.Instance.Table.Where(x => x.Enabled);
            foreach (var telop in telops)
            {
                UpdateNormalSpellTimerForTelop(telop, false);
            }

            // ACTのスペルタイマーに変更を反映する
            ActGlobals.oFormSpellTimers.RebuildSpellTreeView();
        }

        /// <summary>
        /// ACT標準のスペルタイマーから設定を削除する
        /// </summary>
        /// <param name="immediate">変更を即時に反映させるか？</param>
        public void ClearNormalSpellTimer(bool immediate = false)
        {
            var prefix = Settings.Default.NotifyNormalSpellTimerPrefix;
            var timerDefs = ActGlobals.oFormSpellTimers.TimerDefs
                .Where(p => p.Key.StartsWith(prefix))
                .Select(x => x.Value)
                .ToList();
            foreach (var timerDef in timerDefs)
            {
                ActGlobals.oFormSpellTimers.RemoveTimerDef(timerDef);
            }

            // ACTのスペルタイマーに変更を反映する
            if (immediate)
            {
                ActGlobals.oFormSpellTimers.RebuildSpellTreeView();
            }
        }

        /// <summary>
        /// ACT標準のスペルタイマーに通知する
        /// </summary>
        /// <param name="spellTimer">通知先に対応するスペルタイマー</param>
        public void NotifyNormalSpellTimer(Models.Spell spellTimer)
        {
            if (!Settings.Default.EnabledNotifyNormalSpellTimer)
            {
                return;
            }

            var prefix = Settings.Default.NotifyNormalSpellTimerPrefix;
            var spellName = prefix + "spell_" + spellTimer.SpellTitle;
            ActGlobals.oFormSpellTimers.NotifySpell("attacker", spellName, false, "victim", false);
        }

        /// <summary>
        /// ACT標準のスペルタイマーに通知する（テロップ用）
        /// </summary>
        /// <param name="telopTitle">通知先に対応するテロップ名</param>
        public void NotifyNormalSpellTimerForTelop(string telopTitle)
        {
            if (!Settings.Default.EnabledNotifyNormalSpellTimer)
            {
                return;
            }

            var prefix = Settings.Default.NotifyNormalSpellTimerPrefix;
            var spellName = prefix + "telop_" + telopTitle;
            ActGlobals.oFormSpellTimers.NotifySpell("attacker", spellName, false, "victim", false);
        }

        /// <summary>
        /// ACT標準のスペルタイマーの設定を追加・更新する
        /// </summary>
        /// <param name="spellTimer">元になるスペルタイマー</param>
        /// <param name="useRecastTime">リキャスト時間にRecastの値を使うか。falseの場合はCompleteScheduledTimeから計算される</param>
        public void UpdateNormalSpellTimer(Models.Spell spellTimer, bool useRecastTime)
        {
            if (!Settings.Default.EnabledNotifyNormalSpellTimer)
            {
                return;
            }

            var prefix = Settings.Default.NotifyNormalSpellTimerPrefix;
            var spellName = prefix + "spell_" + spellTimer.SpellTitle;
            var categoryName = prefix + spellTimer.Panel.PanelName;
            var recastTime = useRecastTime ? spellTimer.RecastTime : (spellTimer.CompleteScheduledTime - DateTime.Now).TotalSeconds;

            var timerData = new TimerData(spellName, categoryName);
            timerData.TimerValue = (int)recastTime;
            timerData.RemoveValue = (int)-Settings.Default.TimeOfHideSpell;
            timerData.WarningValue = 0;
            timerData.OnlyMasterTicks = true;
            timerData.Tooltip = spellTimer.SpellTitleReplaced;

            timerData.Panel1Display = false;
            timerData.Panel2Display = false;

            // disable warning sound
            timerData.WarningSoundData = "none";

            // initialize other parameters
            timerData.RestrictToMe = false;
            timerData.AbsoluteTiming = false;
            timerData.RestrictToCategory = false;

            ActGlobals.oFormSpellTimers.AddEditTimerDef(timerData);
        }

        /// <summary>
        /// ACT標準のスペルタイマーの設定を追加・更新する（テロップ用）
        /// </summary>
        /// <param name="spellTimer">元になるテロップ</param>
        /// <param name="forceHide">強制非表示か？</param>
        public void UpdateNormalSpellTimerForTelop(Ticker telop, bool forceHide)
        {
            if (!Settings.Default.EnabledNotifyNormalSpellTimer)
            {
                return;
            }

            var prefix = Settings.Default.NotifyNormalSpellTimerPrefix;
            var spellName = prefix + "telop_" + telop.Title;
            var categoryName = prefix + "telops";

            var timerData = new TimerData(spellName, categoryName);
            timerData.TimerValue = forceHide ? 1 : (int)(telop.DisplayTime + telop.Delay);
            timerData.RemoveValue = forceHide ? -timerData.TimerValue : 0;
            timerData.WarningValue = (int)telop.DisplayTime;
            timerData.OnlyMasterTicks = telop.AddMessageEnabled ? false : true;
            timerData.Tooltip = telop.MessageReplaced;

            timerData.Panel1Display = false;
            timerData.Panel2Display = false;

            // disable warning sound
            timerData.WarningSoundData = "none";

            // initialize other parameters
            timerData.RestrictToMe = false;
            timerData.AbsoluteTiming = false;
            timerData.RestrictToCategory = false;

            ActGlobals.oFormSpellTimers.AddEditTimerDef(timerData);
        }

        #endregion To Normal SpellTimer
    }
}
