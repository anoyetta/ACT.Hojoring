using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Config.Models;
using ACT.SpecialSpellTimer.Image;
using ACT.SpecialSpellTimer.Sound;
using ACT.SpecialSpellTimer.Utility;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Views;

namespace ACT.SpecialSpellTimer.Models
{
    /// <summary>
    /// スペルタイマ
    /// </summary>
    [Serializable]
    [XmlType(TypeName = "SpellTimer")]
    public class Spell :
        TreeItemBase,
        IDisposable,
        ITrigger
    {
        [XmlIgnore]
        public override ItemTypes ItemType => ItemTypes.Spell;

        #region ITrigger

        public void MatchTrigger(string logLine)
            => SpellsController.Instance.MatchCore(this, logLine);

        #endregion ITrigger

        #region ITreeItem

        private bool enabled = false;

        [XmlIgnore]
        public override string DisplayText => this.SpellTitle;

        [XmlIgnore]
        public override int SortPriority { get; set; }

        [XmlIgnore]
        public override bool IsExpanded
        {
            get => false;
            set { }
        }

        public override bool Enabled
        {
            get => this.enabled;
            set => this.SetProperty(ref this.enabled, value);
        }

        [XmlIgnore]
        public override ICollectionView Children => null;

        #endregion ITreeItem

        [XmlIgnore]
        public volatile bool UpdateDone;

        [XmlIgnore]
        private Timer overSoundTimer = new Timer() { AutoReset = false, Enabled = false };

        [XmlIgnore]
        private Timer beforeSoundTimer = new Timer() { AutoReset = false, Enabled = false };

        [XmlIgnore]
        private Timer timeupSoundTimer = new Timer() { AutoReset = false, Enabled = false };

        public Spell()
        {
            this.overSoundTimer.Elapsed += this.OverSoundTimer_Elapsed;
            this.beforeSoundTimer.Elapsed += this.BeforeSoundTimer_Elapsed;
            this.timeupSoundTimer.Elapsed += this.TimeupSoundTimer_Elapsed;
        }

        private double left, top;

        public double Left
        {
            get => this.left;
            set
            {
                if (this.SetProperty(ref this.left, Math.Round(value)))
                {
                }
            }
        }

        public double Top
        {
            get => this.top;
            set
            {
                if (this.SetProperty(ref this.top, Math.Round(value)))
                {
                }
            }
        }

        private long id;

        public long ID
        {
            get => this.id;
            set => this.SetProperty(ref id, value);
        }

        private Guid guid = Guid.NewGuid();

        public Guid Guid
        {
            get => this.guid;
            set => this.SetProperty(ref this.guid, value);
        }

        private Guid panelID = Guid.Empty;

        public Guid PanelID
        {
            get => this.panelID;
            set => this.SetProperty(ref this.panelID, value);
        }

        [XmlIgnore]
        public SpellPanel Panel => SpellPanelTable.Instance.Table.FirstOrDefault(x => x.ID == this.PanelID);

        private string panelName = string.Empty;

        [XmlElement(ElementName = "Panel")]
        public string PanelName
        {
            get
            {
                if (this.PanelID == Guid.Empty)
                {
                    return this.panelName;
                }

                return this.Panel?.PanelName;
            }

            set => this.panelName = value;
        }

        private bool isDesignMode = false;

        [XmlIgnore]
        public bool IsDesignMode
        {
            get => this.isDesignMode;
            set => this.SetProperty(ref this.isDesignMode, value);
        }

        private bool isTest = false;

        /// <summary>
        /// 動作テスト用のフラグ
        /// </summary>
        /// <remarks>擬似的にマッチさせるで使用するテストモード用フラグ</remarks>
        [XmlIgnore]
        public bool IsTest
        {
            get => this.isTest;
            set => this.SetProperty(ref this.isTest, value);
        }

        private string jobFilter = string.Empty;

        public string JobFilter
        {
            get => this.jobFilter;
            set => this.SetProperty(ref this.jobFilter, value);
        }

        private string partyJobFilter = string.Empty;

        public string PartyJobFilter
        {
            get => this.partyJobFilter;
            set => this.SetProperty(ref this.partyJobFilter, value);
        }

        private string zoneFilter = string.Empty;

        public string ZoneFilter
        {
            get => this.zoneFilter;
            set => this.SetProperty(ref this.zoneFilter, value);
        }

        private string spellTitle = string.Empty;

        public string SpellTitle
        {
            get => this.spellTitle;
            set
            {
                if (this.SetProperty(ref this.spellTitle, value))
                {
                    this.RaisePropertyChanged(nameof(this.DisplayText));
                }
            }
        }

        private string spellTitleReplaced = string.Empty;

        [XmlIgnore]
        public string SpellTitleReplaced
        {
            get => this.spellTitleReplaced;
            set => this.SetProperty(ref this.spellTitleReplaced, value);
        }

        /// <summary>
        /// ※注意が必要な項目※
        /// 昔の名残で項目名と異なる動作になっている。
        /// プログレスバーの表示／非表示ではなく、スペル全体の表示／非表示を司る重要な項目として動作している
        /// </summary>
        private bool progressBarVisible = true;

        public bool ProgressBarVisible
        {
            get => this.progressBarVisible;
            set => this.SetProperty(ref this.progressBarVisible, value);
        }

        public FontInfo Font { get; set; } = FontInfo.DefaultFont;

        public string FontColor { get; set; } = Colors.White.ToLegacy().ToHTML();

        public string FontOutlineColor { get; set; } = Colors.Navy.ToLegacy().ToHTML();

        private double warningTime = 0;

        public double WarningTime
        {
            get => this.warningTime;
            set => this.SetProperty(ref this.warningTime, value);
        }

        private bool changeFontColorsWhenWarning;

        public bool ChangeFontColorsWhenWarning
        {
            get => this.changeFontColorsWhenWarning;
            set => this.SetProperty(ref this.changeFontColorsWhenWarning, value);
        }

        public string WarningFontColor { get; set; } = Colors.White.ToLegacy().ToHTML();

        public string WarningFontOutlineColor { get; set; } = Colors.Red.ToLegacy().ToHTML();

        private int barWidth;

        public int BarWidth
        {
            get => this.barWidth;
            set => this.SetProperty(ref this.barWidth, value);
        }

        private int barHeight;

        public int BarHeight
        {
            get => this.barHeight;
            set => this.SetProperty(ref this.barHeight, value);
        }

        public string BarColor { get; set; } = Colors.White.ToLegacy().ToHTML();

        public string BarOutlineColor { get; set; } = Colors.Navy.ToLegacy().ToHTML();

        public string BackgroundColor { get; set; } = Colors.Black.ToLegacy().ToHTML();

        public int BackgroundAlpha { get; set; } = 0;

        [XmlIgnore]
        public DateTime CompleteScheduledTime { get; set; }

        private long displayNo;

        public long DisplayNo
        {
            get => this.displayNo;
            set => this.SetProperty(ref this.displayNo, value);
        }

        private bool dontHide;

        public bool DontHide
        {
            get => this.dontHide;
            set => this.SetProperty(ref this.dontHide, value);
        }

        public bool ExtendBeyondOriginalRecastTime { get; set; }

        private bool hideSpellName;

        public bool HideSpellName
        {
            get => this.hideSpellName;
            set => this.SetProperty(ref this.hideSpellName, value);
        }

        private bool hideCounter;

        public bool HideCounter
        {
            get => this.hideCounter;
            set => this.SetProperty(ref this.hideCounter, value);
        }

        private bool isCounterToCenter = false;

        public bool IsCounterToCenter
        {
            get => this.isCounterToCenter;
            set
            {
                if (this.SetProperty(ref this.isCounterToCenter, value))
                {
                    this.RaisePropertyChanged(nameof(this.CounterAlignment));
                }
            }
        }

        [XmlIgnore]
        public HorizontalAlignment CounterAlignment =>
            this.isCounterToCenter ?
            HorizontalAlignment.Center :
            HorizontalAlignment.Right;

        /// <summary>インスタンス化されたスペルか？</summary>
        [XmlIgnore]
        public bool IsInstance { get; set; }

        private bool isReverse;

        public bool IsReverse
        {
            get => this.isReverse;
            set => this.SetProperty(ref this.isReverse, value);
        }

        public DateTime MatchDateTime { get; set; } = DateTime.MinValue;

        [XmlIgnore]
        public string MatchedLog { get; set; } = string.Empty;

        private bool overlapRecastTime;

        public bool OverlapRecastTime
        {
            get => this.overlapRecastTime;
            set => this.SetProperty(ref this.overlapRecastTime, value);
        }

        public double RecastTime { get; set; } = 0;

        private bool useHotbarRecastTime = false;

        public bool UseHotbarRecastTime
        {
            get => this.useHotbarRecastTime;
            set => this.SetProperty(ref this.useHotbarRecastTime, value);
        }

        private string hotbarName = string.Empty;

        public string HotbarName
        {
            get => this.hotbarName;
            set => this.SetProperty(ref this.hotbarName, value);
        }

        public double RecastTimeExtending1 { get; set; } = 0;

        public double RecastTimeExtending2 { get; set; } = 0;

        private bool reduceIconBrightness;

        public bool ReduceIconBrightness
        {
            get => this.reduceIconBrightness;
            set => this.SetProperty(ref this.reduceIconBrightness, value);
        }

        private string spellIcon = string.Empty;

        public string SpellIcon
        {
            get => this.spellIcon;
            set
            {
                if (this.SetProperty(ref this.spellIcon, value))
                {
                    this.RaisePropertyChanged(nameof(this.SpellIconFullPath));
                }
            }
        }

        [XmlIgnore]
        public string SpellIconFullPath =>
            string.IsNullOrEmpty(this.SpellIcon) ?
            string.Empty :
            IconController.Instance.GetIconFile(this.SpellIcon)?.FullPath;

        private int spellIconSize = 24;

        public int SpellIconSize
        {
            get => this.spellIconSize;
            set => this.SetProperty(ref this.spellIconSize, value);
        }

        /// <summary>スペルが作用した対象</summary>
        [XmlIgnore]
        public string TargetName { get; set; } = string.Empty;

        public Guid[] TimersMustRunningForStart { get; set; } = new Guid[0];

        public Guid[] TimersMustStoppingForStart { get; set; } = new Guid[0];

        public bool TimeupHide { get; set; }

        /// <summary>インスタンス化する</summary>
        /// <remarks>表示テキストが異なる条件でマッチングした場合に当該スペルの新しいインスタンスを生成する</remarks>
        public bool ToInstance { get; set; }

        public double UpperLimitOfExtension { get; set; } = 0;

        public double BlinkTime { get; set; } = 0;

        public bool BlinkIcon { get; set; } = false;

        public bool BlinkBar { get; set; } = false;

        private int actualSortOrder = 0;

        [XmlIgnore]
        public int ActualSortOrder
        {
            get => this.actualSortOrder;
            set => this.SetProperty(ref this.actualSortOrder, value);
        }

        private Visibility visibility = Visibility.Collapsed;

        [XmlIgnore]
        public Visibility Visibility
        {
            get => this.visibility;
            set => this.SetProperty(ref this.visibility, value);
        }

        #region Sequential TTS

        /// <summary>
        /// 同時再生を抑制してシーケンシャルにTTSを再生する
        /// </summary>
        public bool IsSequentialTTS { get; set; } = false;

        public void Play(string tts, AdvancedNoticeConfig config)
            => Spell.PlayCore(tts, this.IsSequentialTTS, config, this);

        public static void PlayCore(
            string tts,
            bool isSync,
            AdvancedNoticeConfig noticeConfig,
            ITrigger trigger)
        {
            if (string.IsNullOrEmpty(tts))
            {
                return;
            }

            if (noticeConfig == null)
            {
                SoundController.Instance.Play(tts);
                return;
            }

            var isWave = false;
            if (tts.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                tts.EndsWith(".wave", StringComparison.OrdinalIgnoreCase) ||
                tts.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                isWave = true;
            }

            void play(string source)
            {
                if (isWave)
                {
                    noticeConfig.PlayWave(source);
                }
                else
                {
                    noticeConfig.Speak(source);
                }
            }

            // waveサウンドはシンクロ再生しない
            if (isWave)
            {
                play(tts);
                return;
            }

            // ゆっくりがいないならシンクロ再生はしない
            if (!PlayBridge.Instance.IsAvailable)
            {
                play(tts);
                return;
            }

            // シンクロ再生ならばシンクロコマンドを付与する
            // /sync [優先順位] テキスト
            // ex. /sync 9999 よしだーー
            // 同期的に発声するが優先順位は最後になる
            if (isSync)
            {
                if (!tts.Contains(AdvancedNoticeConfig.SyncKeyword))
                {
                    var priority = 9999L;
                    if (trigger is Spell spell)
                    {
                        priority = spell.DisplayNo;
                    }

                    tts = $"{AdvancedNoticeConfig.SyncKeyword} {priority} {tts}";
                }
            }

            play(tts);
        }

        #endregion Sequential TTS

        #region to Notice

        public string MatchTextToSpeak { get; set; } = string.Empty;

        public AdvancedNoticeConfig MatchAdvancedConfig { get; set; } = new AdvancedNoticeConfig();

        public double OverTime { get; set; } = 0;

        public string OverTextToSpeak { get; set; } = string.Empty;

        [XmlIgnore]
        public bool OverDone { get; set; }

        public AdvancedNoticeConfig OverAdvancedConfig { get; set; } = new AdvancedNoticeConfig();

        public double BeforeTime { get; set; } = 0;

        public string BeforeTextToSpeak { get; set; } = string.Empty;

        [XmlIgnore]
        public bool BeforeDone { get; set; }

        public AdvancedNoticeConfig BeforeAdvancedConfig { get; set; } = new AdvancedNoticeConfig();

        public string TimeupTextToSpeak { get; set; } = string.Empty;

        [XmlIgnore]
        public bool TimeupDone { get; set; }

        public AdvancedNoticeConfig TimeupAdvancedConfig { get; set; } = new AdvancedNoticeConfig();

        #endregion to Notice

        #region to Notice wave files

        [XmlIgnore]
        private string matchSound = string.Empty;

        [XmlIgnore]
        private string overSound = string.Empty;

        [XmlIgnore]
        private string beforeSound = string.Empty;

        [XmlIgnore]
        private string timeupSound = string.Empty;

        [XmlIgnore]
        public string MatchSound { get => this.matchSound; set => this.matchSound = value; }

        [XmlElement(ElementName = "MatchSound")]
        public string MatchSoundToFile
        {
            get => Path.GetFileName(this.matchSound);
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this.matchSound = Path.Combine(SoundController.Instance.WaveDirectory, value);
                }
            }
        }

        [XmlIgnore]
        public string OverSound { get => this.overSound; set => this.overSound = value; }

        [XmlElement(ElementName = "OverSound")]
        public string OverSoundToFile
        {
            get => Path.GetFileName(this.overSound);
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this.overSound = Path.Combine(SoundController.Instance.WaveDirectory, value);
                }
            }
        }

        [XmlIgnore]
        public string BeforeSound { get => this.beforeSound; set => this.beforeSound = value; }

        [XmlElement(ElementName = "BeforeSound")]
        public string BeforeSoundToFile
        {
            get => Path.GetFileName(this.beforeSound);
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this.beforeSound = Path.Combine(SoundController.Instance.WaveDirectory, value);
                }
            }
        }

        [XmlIgnore]
        public string TimeupSound { get => this.timeupSound; set => this.timeupSound = value; }

        [XmlElement(ElementName = "TimeupSound")]
        public string TimeupSoundToFile
        {
            get => Path.GetFileName(this.timeupSound);
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this.timeupSound = Path.Combine(SoundController.Instance.WaveDirectory, value);
                }
            }
        }

        #endregion to Notice wave files

        #region Performance Monitor

        [XmlIgnore]
        public double MatchingDuration { get; set; } = 0.0;

        [XmlIgnore]
        private DateTime matchingStartDateTime;

        public void StartMatching()
        {
            this.matchingStartDateTime = DateTime.Now;
        }

        public void EndMatching()
        {
            var ticks = (DateTime.Now - this.matchingStartDateTime).Ticks;
            if (ticks == 0)
            {
                return;
            }

            var cost = ticks / 1000;

            if (this.MatchingDuration != 0)
            {
                this.MatchingDuration += cost;
                this.MatchingDuration /= 2;
            }
            else
            {
                this.MatchingDuration += cost;
            }
        }

        #endregion Performance Monitor

        public void Dispose()
        {
            if (this.overSoundTimer != null)
            {
                this.overSoundTimer.Stop();
                this.overSoundTimer.Dispose();
                this.overSoundTimer = null;
            }

            if (this.beforeSoundTimer != null)
            {
                this.beforeSoundTimer.Stop();
                this.beforeSoundTimer.Dispose();
                this.beforeSoundTimer = null;
            }

            if (this.timeupSoundTimer != null)
            {
                this.timeupSoundTimer.Stop();
                this.timeupSoundTimer.Dispose();
                this.timeupSoundTimer = null;
            }
        }

        /// <summary>
        /// リキャストｎ秒前のサウンドタイマを開始する
        /// </summary>
        public void StartBeforeSoundTimer()
        {
            var timer = this.beforeSoundTimer;

            if (timer == null)
            {
                return;
            }

            if (timer.Enabled)
            {
                timer.Stop();
            }

            if (this.BeforeTime <= 0 ||
                this.MatchDateTime <= DateTime.MinValue)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(this.BeforeSound) &&
                string.IsNullOrWhiteSpace(this.BeforeTextToSpeak))
            {
                return;
            }

            if (this.CompleteScheduledTime <= DateTime.MinValue)
            {
                return;
            }

            // タイマをセットする
            var timeToPlay = this.CompleteScheduledTime.AddSeconds(this.BeforeTime * -1);
            var duration = (timeToPlay - DateTime.Now).TotalMilliseconds;

            if (duration > 0d)
            {
                // タイマスタート
                timer.Interval = duration;
                timer.Start();
            }
        }

        /// <summary>
        /// マッチ後ｎ秒後のサウンドタイマを開始する
        /// </summary>
        public void StartOverSoundTimer()
        {
            var timer = this.overSoundTimer;

            if (timer == null)
            {
                return;
            }

            if (timer.Enabled)
            {
                timer.Stop();
            }

            if (this.OverTime <= 0 ||
                this.MatchDateTime <= DateTime.MinValue)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(this.OverSound) &&
                string.IsNullOrWhiteSpace(this.OverTextToSpeak))
            {
                return;
            }

            // タイマをセットする
            var timeToPlay = this.MatchDateTime.AddSeconds(this.OverTime);
            var duration = (timeToPlay - DateTime.Now).TotalMilliseconds;

            if (duration > 0d)
            {
                // タイマスタート
                timer.Interval = duration;
                timer.Start();
            }
        }

        /// <summary>
        /// 遅延処理のタイマを開始する
        /// </summary>
        public void StartTimer()
        {
            this.StartOverSoundTimer();
            this.StartBeforeSoundTimer();
            this.StartTimeupSoundTimer();
        }

        /// <summary>
        /// リキャスト完了のサウンドタイマを開始する
        /// </summary>
        public void StartTimeupSoundTimer()
        {
            var timer = this.timeupSoundTimer;

            if (timer == null)
            {
                return;
            }

            if (timer.Enabled)
            {
                timer.Stop();
            }

            if (this.CompleteScheduledTime <= DateTime.MinValue ||
                this.MatchDateTime <= DateTime.MinValue)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(this.TimeupSound) &&
                string.IsNullOrWhiteSpace(this.TimeupTextToSpeak))
            {
                return;
            }

            // タイマをセットする
            var timeToPlay = this.CompleteScheduledTime;
            var duration = (timeToPlay - DateTime.Now).TotalMilliseconds;

            if (duration > 0d)
            {
                // タイマスタート
                timer.Interval = duration;
                timer.Start();
            }
            else
            {
                // 過ぎているなら即座に通知する
                this.TimeupSoundTimer_Elapsed(null, null);
            }
        }

        private void BeforeSoundTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.BeforeDone = true;

            var regex = this.Regex;
            var wave = this.BeforeSound;
            var speak = this.BeforeTextToSpeak;

            this.Play(wave, this.BeforeAdvancedConfig);

            if (!string.IsNullOrWhiteSpace(speak))
            {
                if (regex == null ||
                    !speak.Contains("$"))
                {
                    this.Play(speak, this.BeforeAdvancedConfig);
                    return;
                }

                var match = regex.Match(this.MatchedLog);
                speak = match.Result(speak);

                this.Play(speak, this.BeforeAdvancedConfig);
            }
        }

        private void OverSoundTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.OverDone = true;

            var regex = this.Regex;
            var wave = this.OverSound;
            var speak = this.OverTextToSpeak;

            this.Play(wave, this.OverAdvancedConfig);

            if (!string.IsNullOrWhiteSpace(speak))
            {
                if (regex == null ||
                    !speak.Contains("$"))
                {
                    this.Play(speak, this.OverAdvancedConfig);
                    return;
                }

                var match = regex.Match(this.MatchedLog);
                speak = match.Result(speak);

                this.Play(speak, this.OverAdvancedConfig);
            }
        }

        private void TimeupSoundTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.TimeupDone = true;

            var regex = this.Regex;
            var wave = this.TimeupSound;
            var speak = this.TimeupTextToSpeak;

            this.Play(wave, this.TimeupAdvancedConfig);

            if (!string.IsNullOrWhiteSpace(speak))
            {
                if (regex == null ||
                    !speak.Contains("$"))
                {
                    this.Play(speak, this.TimeupAdvancedConfig);
                    return;
                }

                var match = regex.Match(this.MatchedLog);
                speak = match.Result(speak);

                this.Play(speak, this.TimeupAdvancedConfig);
            }
        }

        #region Clone

        public Spell Clone() => (Spell)this.MemberwiseClone();

        #endregion Clone

        #region NewSpell

        public static Spell CreateNew()
        {
            var n = new Spell();

            lock (SpellTable.Instance.Table)
            {
                n.ID = SpellTable.Instance.Table.Any() ?
                    SpellTable.Instance.Table.Max(x => x.ID) + 1 :
                    1;

                n.DisplayNo = SpellTable.Instance.Table.Any() ?
                    SpellTable.Instance.Table.Max(x => x.DisplayNo) + 1 :
                    50;
            }

            n.PanelID = SpellPanel.GeneralPanel.ID;

            n.SpellTitle = "New Spell";
            n.SpellIconSize = 24;
            n.FontColor = Colors.White.ToLegacy().ToHTML();
            n.FontOutlineColor = Colors.MidnightBlue.ToLegacy().ToHTML();
            n.WarningFontColor = Colors.White.ToLegacy().ToHTML();
            n.WarningFontOutlineColor = Colors.OrangeRed.ToLegacy().ToHTML();
            n.BarColor = Colors.White.ToLegacy().ToHTML();
            n.BarOutlineColor = Colors.MidnightBlue.ToLegacy().ToHTML();
            n.BackgroundColor = Colors.Transparent.ToLegacy().ToHTML();
            n.BarWidth = 190;
            n.BarHeight = 8;

            n.Enabled = true;

            return n;
        }

        /// <summary>
        /// 同様のインスタンスを作る（新規スペルの登録用）
        /// </summary>
        /// <returns>
        /// 同様のインスタンス</returns>
        public Spell CreateSimilarNew()
        {
            var n = Spell.CreateNew();

            n.PanelID = this.PanelID;
            n.SpellTitle = this.SpellTitle + " New";
            n.SpellIcon = this.SpellIcon;
            n.SpellIconSize = this.SpellIconSize;
            n.Keyword = this.Keyword;
            n.RegexEnabled = this.RegexEnabled;
            n.RecastTime = this.RecastTime;
            n.KeywordForExtend1 = this.KeywordForExtend1;
            n.RecastTimeExtending1 = this.RecastTimeExtending1;
            n.KeywordForExtend2 = this.KeywordForExtend2;
            n.RecastTimeExtending2 = this.RecastTimeExtending2;
            n.ExtendBeyondOriginalRecastTime = this.ExtendBeyondOriginalRecastTime;
            n.UpperLimitOfExtension = this.UpperLimitOfExtension;
            n.ProgressBarVisible = this.ProgressBarVisible;
            n.IsReverse = this.IsReverse;
            n.FontColor = this.FontColor;
            n.FontOutlineColor = this.FontOutlineColor;
            n.WarningFontColor = this.WarningFontColor;
            n.WarningFontOutlineColor = this.WarningFontOutlineColor;
            n.BarColor = this.BarColor;
            n.BarOutlineColor = this.BarOutlineColor;
            n.DontHide = this.DontHide;
            n.HideSpellName = this.HideSpellName;
            n.WarningTime = this.WarningTime;
            n.BlinkTime = this.BlinkTime;
            n.BlinkIcon = this.BlinkIcon;
            n.BlinkBar = this.BlinkBar;
            n.ChangeFontColorsWhenWarning = this.ChangeFontColorsWhenWarning;
            n.OverlapRecastTime = this.OverlapRecastTime;
            n.ReduceIconBrightness = this.ReduceIconBrightness;
            n.Font = this.Font.Clone() as FontInfo;
            n.BarWidth = this.BarWidth;
            n.BarHeight = this.BarHeight;
            n.BackgroundColor = this.BackgroundColor;
            n.BackgroundAlpha = this.BackgroundAlpha;
            n.HideCounter = this.HideCounter;
            n.JobFilter = this.JobFilter;
            n.ZoneFilter = this.ZoneFilter;
            n.TimersMustRunningForStart = this.TimersMustRunningForStart;
            n.TimersMustStoppingForStart = this.TimersMustStoppingForStart;

            n.MatchAdvancedConfig = this.MatchAdvancedConfig.Clone() as AdvancedNoticeConfig;
            n.OverAdvancedConfig = this.OverAdvancedConfig.Clone() as AdvancedNoticeConfig;
            n.BeforeAdvancedConfig = this.BeforeAdvancedConfig.Clone() as AdvancedNoticeConfig;
            n.TimeupAdvancedConfig = this.TimeupAdvancedConfig.Clone() as AdvancedNoticeConfig;
            n.IsSequentialTTS = this.IsSequentialTTS;

            n.ToInstance = this.ToInstance;

            n.Enabled = this.Enabled;

            return n;
        }

        public Spell CreateInstanceNew(
            string title)
        {
            var n = Spell.CreateNew();

            n.SpellTitleReplaced = title;

            n.PanelID = this.PanelID;
            n.SpellTitle = this.SpellTitle;
            n.SpellIcon = this.SpellIcon;
            n.SpellIconSize = this.SpellIconSize;
            n.Keyword = this.Keyword;
            n.KeywordForExtend1 = this.KeywordForExtend1;
            n.KeywordForExtend2 = this.KeywordForExtend2;
            n.RecastTime = this.RecastTime;
            n.RecastTimeExtending1 = this.RecastTimeExtending1;
            n.RecastTimeExtending2 = this.RecastTimeExtending2;
            n.ExtendBeyondOriginalRecastTime = this.ExtendBeyondOriginalRecastTime;
            n.UpperLimitOfExtension = this.UpperLimitOfExtension;
            n.ProgressBarVisible = this.ProgressBarVisible;
            n.MatchSound = this.MatchSound;
            n.MatchTextToSpeak = this.MatchTextToSpeak;
            n.OverSound = this.OverSound;
            n.OverTextToSpeak = this.OverTextToSpeak;
            n.OverTime = this.OverTime;
            n.BeforeSound = this.BeforeSound;
            n.BeforeTextToSpeak = this.BeforeTextToSpeak;
            n.BeforeTime = this.BeforeTime;
            n.TimeupSound = this.TimeupSound;
            n.TimeupTextToSpeak = this.TimeupTextToSpeak;
            n.MatchDateTime = this.MatchDateTime;
            n.TimeupHide = this.TimeupHide;
            n.IsReverse = this.IsReverse;
            n.Font = this.Font;
            n.FontOutlineColor = this.FontOutlineColor;
            n.WarningFontColor = this.WarningFontColor;
            n.WarningFontOutlineColor = this.WarningFontOutlineColor;
            n.BarColor = this.BarColor;
            n.BarOutlineColor = this.BarOutlineColor;
            n.BarWidth = this.BarWidth;
            n.BarHeight = this.BarHeight;
            n.BackgroundColor = this.BackgroundColor;
            n.BackgroundAlpha = this.BackgroundAlpha;
            n.HideCounter = this.HideCounter;
            n.DontHide = this.DontHide;
            n.HideSpellName = this.HideSpellName;
            n.WarningTime = this.WarningTime;
            n.ChangeFontColorsWhenWarning = this.ChangeFontColorsWhenWarning;
            n.BlinkTime = this.BlinkTime;
            n.BlinkIcon = this.BlinkIcon;
            n.BlinkBar = this.BlinkBar;
            n.OverlapRecastTime = this.OverlapRecastTime;
            n.ReduceIconBrightness = this.ReduceIconBrightness;
            n.RegexEnabled = this.RegexEnabled;
            n.JobFilter = this.JobFilter;
            n.ZoneFilter = this.ZoneFilter;
            n.TimersMustRunningForStart = this.TimersMustRunningForStart;
            n.TimersMustStoppingForStart = this.TimersMustStoppingForStart;
            n.Enabled = this.Enabled;

            n.MatchedLog = this.MatchedLog;
            n.Regex = this.Regex;
            n.RegexPattern = this.RegexPattern;
            n.KeywordReplaced = this.KeywordReplaced;
            n.RegexForExtend1 = this.RegexForExtend1;
            n.RegexForExtendPattern1 = this.RegexForExtendPattern1;
            n.KeywordForExtendReplaced1 = this.KeywordForExtendReplaced1;
            n.RegexForExtend2 = this.RegexForExtend2;
            n.RegexForExtendPattern2 = this.RegexForExtendPattern2;
            n.KeywordForExtendReplaced2 = this.KeywordForExtendReplaced2;

            n.MatchAdvancedConfig = this.MatchAdvancedConfig;
            n.OverAdvancedConfig = this.OverAdvancedConfig;
            n.BeforeAdvancedConfig = this.BeforeAdvancedConfig;
            n.TimeupAdvancedConfig = this.TimeupAdvancedConfig;
            n.IsSequentialTTS = this.IsSequentialTTS;

            n.ToInstance = false;
            n.IsInstance = true;
            n.IsDesignMode = false;

            return n;
        }

        #endregion NewSpell

        #region Regex compiler

        [XmlIgnore]
        public bool IsRealtimeCompile { get; set; } = false;

        private bool regexEnabled;
        private string keyword;
        private string keywordForExtend1;
        private string keywordForExtend2;

        public bool RegexEnabled
        {
            get => this.regexEnabled;
            set
            {
                if (this.SetProperty(ref this.regexEnabled, value))
                {
                    this.KeywordReplaced = string.Empty;
                    this.KeywordForExtendReplaced1 = string.Empty;
                    this.KeywordForExtendReplaced2 = string.Empty;

                    if (this.IsRealtimeCompile)
                    {
                        var ex = this.CompileRegex();
                        if (ex != null)
                        {
                            ModernMessageBox.ShowDialog(
                                "Regex compile error ! This is invalid keyword.",
                                "Regex compiler",
                                MessageBoxButton.OK,
                                ex);
                        }

                        ex = this.CompileRegexExtend1();
                        if (ex != null)
                        {
                            ModernMessageBox.ShowDialog(
                                "Regex compile error ! This is invalid keyword.",
                                "Regex compiler",
                                MessageBoxButton.OK,
                                ex);
                        }

                        ex = this.CompileRegexExtend2();
                        if (ex != null)
                        {
                            ModernMessageBox.ShowDialog(
                                "Regex compile error ! This is invalid keyword.",
                                "Regex compiler",
                                MessageBoxButton.OK,
                                ex);
                        }
                    }
                }
            }
        }

        public string Keyword
        {
            get => this.keyword;
            set
            {
                if (this.SetProperty(ref this.keyword, value))
                {
                    this.KeywordReplaced = string.Empty;
                    if (this.IsRealtimeCompile)
                    {
                        var ex = this.CompileRegex();
                        if (ex != null)
                        {
                            ModernMessageBox.ShowDialog(
                                "Regex compile error ! This is invalid keyword.",
                                "Regex compiler",
                                MessageBoxButton.OK,
                                ex);
                        }
                    }
                }
            }
        }

        public string KeywordForExtend1
        {
            get => this.keywordForExtend1;
            set
            {
                if (this.SetProperty(ref this.keywordForExtend1, value))
                {
                    this.KeywordForExtendReplaced1 = string.Empty;
                    if (this.IsRealtimeCompile)
                    {
                        var ex = this.CompileRegexExtend1();
                        if (ex != null)
                        {
                            ModernMessageBox.ShowDialog(
                                "Regex compile error ! This is invalid keyword.",
                                "Regex compiler",
                                MessageBoxButton.OK,
                                ex);
                        }
                    }
                }
            }
        }

        public string KeywordForExtend2
        {
            get => this.keywordForExtend2;
            set
            {
                if (this.SetProperty(ref this.keywordForExtend2, value))
                {
                    this.KeywordForExtendReplaced2 = string.Empty;
                    if (this.IsRealtimeCompile)
                    {
                        var ex = this.CompileRegexExtend2();
                        if (ex != null)
                        {
                            ModernMessageBox.ShowDialog(
                                "Regex compile error ! This is invalid keyword.",
                                "Regex compiler",
                                MessageBoxButton.OK,
                                ex);
                        }
                    }
                }
            }
        }

        [XmlIgnore]
        public string KeywordReplaced { get; set; }

        [XmlIgnore]
        public string KeywordForExtendReplaced1 { get; set; }

        [XmlIgnore]
        public string KeywordForExtendReplaced2 { get; set; }

        [XmlIgnore]
        public Regex Regex { get; set; }

        [XmlIgnore]
        public Regex RegexForExtend1 { get; set; }

        [XmlIgnore]
        public Regex RegexForExtend2 { get; set; }

        [XmlIgnore]
        public string RegexPattern { get; set; }

        [XmlIgnore]
        public string RegexForExtendPattern1 { get; set; }

        [XmlIgnore]
        public string RegexForExtendPattern2 { get; set; }

        public Exception CompileRegex()
        {
            var pattern = string.Empty;

            try
            {
                this.KeywordReplaced = TableCompiler.Instance.GetMatchingKeyword(
                    this.KeywordReplaced,
                    this.Keyword);

                if (this.RegexEnabled)
                {
                    pattern = this.KeywordReplaced.ToRegexPattern();

                    if (this.Regex == null ||
                        this.RegexPattern != pattern)
                    {
                        this.Regex = pattern.ToRegex();
                        this.RegexPattern = pattern;
                    }
                }
                else
                {
                    this.Regex = null;
                    this.RegexPattern = string.Empty;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        public Exception CompileRegexExtend1()
        {
            var pattern = string.Empty;

            try
            {
                this.KeywordForExtendReplaced1 = TableCompiler.Instance.GetMatchingKeyword(
                    this.KeywordForExtendReplaced1,
                    this.KeywordForExtend1);

                if (this.RegexEnabled)
                {
                    pattern = this.KeywordForExtendReplaced1.ToRegexPattern();

                    if (this.RegexForExtend1 == null ||
                        this.RegexForExtendPattern1 != pattern)
                    {
                        this.RegexForExtend1 = pattern.ToRegex();
                        this.RegexForExtendPattern1 = pattern;
                    }
                }
                else
                {
                    this.RegexForExtend1 = null;
                    this.RegexForExtendPattern1 = string.Empty;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        public Exception CompileRegexExtend2()
        {
            var pattern = string.Empty;

            try
            {
                this.KeywordForExtendReplaced2 = TableCompiler.Instance.GetMatchingKeyword(
                    this.KeywordForExtendReplaced2,
                    this.KeywordForExtend2);

                if (this.RegexEnabled)
                {
                    pattern = this.KeywordForExtendReplaced2.ToRegexPattern();

                    if (this.RegexForExtend2 == null ||
                        this.RegexForExtendPattern2 != pattern)
                    {
                        this.RegexForExtend2 = pattern.ToRegex();
                        this.RegexForExtendPattern2 = pattern;
                    }
                }
                else
                {
                    this.RegexForExtend2 = null;
                    this.RegexForExtendPattern2 = string.Empty;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        #endregion Regex compiler

        public void SimulateMatch()
        {
            var now = DateTime.Now;

            // 擬似的にマッチ状態にする
            this.IsTest = true;
            this.MatchDateTime = now;
            this.CompleteScheduledTime = now.AddSeconds(this.RecastTime);

            this.UpdateDone = false;
            this.OverDone = false;
            this.BeforeDone = false;
            this.TimeupDone = false;

            // マッチ時点のサウンドを再生する
            this.MatchAdvancedConfig.PlayWave(this.MatchSound);
            this.MatchAdvancedConfig.Speak(this.MatchTextToSpeak);

            // 遅延サウンドタイマを開始する
            this.StartOverSoundTimer();
            this.StartBeforeSoundTimer();
            this.StartTimeupSoundTimer();

            // トリガリストに加える
            TableCompiler.Instance.AddTestTrigger(this);
        }

        /// <summary>
        /// ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => !string.IsNullOrEmpty(this.SpellTitleReplaced) ?
                this.SpellTitleReplaced :
                this.SpellTitle;

        #region Sample Spells

        public static readonly Spell[] SampleSpells = new[]
        {
            // ランパート
            new Spell()
            {
                PanelID = SpellPanel.GeneralPanel.ID,
                SpellTitle = "ランパート",
                Keyword = "<mex>の「ランパート」",
                RegexEnabled = true,
                RecastTime = 90,
                BarHeight = 8,
                BarWidth = 120,
            }
        };

        #endregion Sample Spells
    }
}
