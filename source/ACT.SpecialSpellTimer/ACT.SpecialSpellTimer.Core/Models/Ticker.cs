using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Config.Models;
using ACT.SpecialSpellTimer.Sound;
using ACT.SpecialSpellTimer.Utility;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Views;

namespace ACT.SpecialSpellTimer.Models
{
    /// <summary>
    /// ワンポイントテロップ
    /// </summary>
    [Serializable]
    [XmlType(TypeName = "OnePointTelop")]
    public class Ticker :
        TreeItemBase,
        IDisposable,
        IFilterizableTrigger
    {
        [XmlIgnore]
        public override ItemTypes ItemType => ItemTypes.Ticker;

        #region ITrigger

        public void MatchTrigger(string logLine)
            => TickersController.Instance.MatchCore(this, logLine);

        #endregion ITrigger

        #region ITreeItem

        private bool enabled = false;

        [XmlIgnore]
        public override string DisplayText => this.Title;

        [XmlIgnore]
        public override int SortPriority { get; set; }

        [XmlIgnore]
        public override bool IsExpanded
        {
            get => false;
            set { }
        }

        [XmlElement(ElementName = "Enabled")]
        public override bool Enabled
        {
            get => this.enabled;
            set => this.SetProperty(ref this.enabled, value);
        }

        [XmlIgnore]
        public override ICollectionView Children => null;

        #endregion ITreeItem

        [XmlIgnore]
        public bool ToClose { get; set; } = false;

        public Ticker()
        {
        }

        private long id;

        public long ID
        {
            get => this.id;
            set => this.SetProperty(ref this.id, value);
        }

        private Guid guid = Guid.NewGuid();

        public Guid Guid
        {
            get => this.guid;
            set => this.SetProperty(ref this.guid, value);
        }

        private double left = 0;
        private double top = 0;

        public double Left
        {
            get => this.left;
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.left, Math.Round(value));
                }
            }
        }

        public double Top
        {
            get => this.top;
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.top, Math.Round(value));
                }
            }
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

        private string title = string.Empty;

        public string Title
        {
            get => this.title;
            set
            {
                if (this.SetProperty(ref this.title, value))
                {
                    this.RaisePropertyChanged(nameof(this.DisplayText));
                }
            }
        }

        public string Message { get; set; } = string.Empty;

        [XmlIgnore]
        public string MessageReplaced { get; set; } = string.Empty;

        #region Keywords & Regex compiler

        [XmlIgnore]
        public bool IsRealtimeCompile { get; set; } = false;

        private bool regexEnabled;
        private string keyword;
        private string keywordToHide;

        public bool RegexEnabled
        {
            get => this.regexEnabled;
            set
            {
                if (this.SetProperty(ref this.regexEnabled, value))
                {
                    this.KeywordReplaced = string.Empty;
                    this.KeywordToHideReplaced = string.Empty;

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

                        ex = this.CompileRegexToHide();
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

        public string KeywordToHide
        {
            get => this.keywordToHide;
            set
            {
                if (this.SetProperty(ref this.keywordToHide, value))
                {
                    this.KeywordToHideReplaced = string.Empty;
                    if (this.IsRealtimeCompile)
                    {
                        var ex = this.CompileRegexToHide();
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

        /// <summary>
        /// 表示までのディレイ
        /// </summary>
        public double Delay { get; set; } = 0;

        /// <summary>
        /// 表示期間
        /// </summary>
        public double DisplayTime { get; set; } = 0;

        [XmlIgnore]
        public string KeywordReplaced { get; set; }

        [XmlIgnore]
        public string KeywordToHideReplaced { get; set; }

        [XmlIgnore]
        public Regex Regex { get; set; }

        [XmlIgnore]
        public Regex RegexToHide { get; set; }

        [XmlIgnore]
        public string RegexPattern { get; set; }

        [XmlIgnore]
        public string RegexPatternToHide { get; set; }

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

        public Exception CompileRegexToHide()
        {
            var message = string.Empty;
            var pattern = string.Empty;

            try
            {
                this.KeywordToHideReplaced = TableCompiler.Instance.GetMatchingKeyword(
                    this.KeywordToHideReplaced,
                    this.KeywordToHide);

                if (this.RegexEnabled)
                {
                    pattern = this.KeywordToHideReplaced.ToRegexPattern();

                    if (this.RegexToHide == null ||
                        this.RegexPatternToHide != pattern)
                    {
                        this.RegexToHide = pattern.ToRegex();
                        this.RegexPatternToHide = pattern;
                    }
                }
                else
                {
                    this.RegexToHide = null;
                    this.RegexPatternToHide = string.Empty;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        #endregion Keywords & Regex compiler

        [XmlIgnore]
        public DateTime MatchDateTime { get; set; } = DateTime.MinValue;

        [XmlIgnore]
        public string MatchedLog { get; set; } = string.Empty;

        public FontInfo Font { get; set; } = FontInfo.DefaultFont;

        public string FontColor { get; set; } = Colors.White.ToLegacy().ToHTML();

        public string FontOutlineColor { get; set; } = Colors.OrangeRed.ToLegacy().ToHTML();

        private double barBlurRadius = 14;

        public double BarBlurRadius
        {
            get => this.barBlurRadius;
            set => this.SetProperty(ref this.barBlurRadius, value);
        }

        public int BackgroundAlpha { get; set; } = 0;

        public string BackgroundColor { get; set; } = Colors.Black.ToLegacy().ToHTML();

        public bool AddMessageEnabled { get; set; } = false;

        private bool progressBarEnabled = false;

        public bool ProgressBarEnabled
        {
            get => this.progressBarEnabled;
            set => this.SetProperty(ref this.progressBarEnabled, value);
        }

        [XmlIgnore]
        public bool ForceHide { get; set; }

        #region Filters & Conditions

        private string jobFilter = string.Empty;

        public string JobFilter
        {
            get => this.jobFilter;
            set => this.SetProperty(ref this.jobFilter, value);
        }

        /// <summary>
        /// パーティジョブフィルタ（未実装）
        /// </summary>
        public string PartyJobFilter
        {
            get => string.Empty;
            set { }
        }

        private string partyCompositionFilter = string.Empty;

        public string PartyCompositionFilter
        {
            get => this.partyCompositionFilter;
            set => this.SetProperty(ref this.partyCompositionFilter, value);
        }

        private string zoneFilter = string.Empty;

        public string ZoneFilter
        {
            get => this.zoneFilter;
            set => this.SetProperty(ref this.zoneFilter, value);
        }

        public Guid[] TimersMustRunningForStart { get; set; } = new Guid[0];

        public Guid[] TimersMustStoppingForStart { get; set; } = new Guid[0];

        private ExpressionFilter[] expressionFilters = new ExpressionFilter[]
        {
            new ExpressionFilter(),
            new ExpressionFilter(),
            new ExpressionFilter(),
            new ExpressionFilter(),
        };

        [XmlArray("ExpressionFilter")]
        [XmlArrayItem("expression")]
        public ExpressionFilter[] ExpressionFilters
        {
            get => this.expressionFilters;
            set => this.SetProperty(ref this.expressionFilters, value);
        }

        #endregion Filters & Conditions

        #region Sequential TTS

        /// <summary>
        /// 同時再生を抑制してシーケンシャルにTTSを再生する
        /// </summary>
        public bool IsSequentialTTS { get; set; } = false;

        public void Play(string tts, AdvancedNoticeConfig config, bool forceSync = false)
            => Spell.PlayCore(tts, this.IsSequentialTTS | forceSync, config, this);

        #endregion Sequential TTS

        #region to Notice

        public string MatchTextToSpeak { get; set; } = string.Empty;

        [XmlIgnore]
        public bool Delayed { get; set; }

        public string DelayTextToSpeak { get; set; } = string.Empty;

        public AdvancedNoticeConfig MatchAdvancedConfig { get; set; } = new AdvancedNoticeConfig();

        public AdvancedNoticeConfig DelayAdvancedConfig { get; set; } = new AdvancedNoticeConfig();

        #endregion to Notice

        #region to Notice wave files

        [XmlIgnore]
        private string delaySound = string.Empty;

        [XmlIgnore]
        private string matchSound = string.Empty;

        [XmlIgnore]
        public string DelaySound { get => this.delaySound; set => this.delaySound = value; }

        [XmlElement(ElementName = "DelaySound")]
        public string DelaySoundToFile
        {
            get => Path.GetFileName(this.delaySound);
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this.delaySound = Path.Combine(SoundController.Instance.WaveDirectory, value);
                }
            }
        }

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

        [XmlIgnore]
        private DelayableTask delayedSoundTask;

        public void Dispose()
        {
            if (this.delayedSoundTask != null)
            {
                this.delayedSoundTask.IsCancel = true;
            }
        }

        /// <summary>
        /// ディレイサウンドのタイマを開始する
        /// </summary>
        public void StartDelayedSoundTimer()
        {
            if (this.delayedSoundTask != null)
            {
                this.delayedSoundTask.IsCancel = true;
            }

            if (this.Delay <= 0 ||
                this.MatchDateTime <= DateTime.MinValue)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(this.DelaySound) &&
                string.IsNullOrWhiteSpace(this.DelayTextToSpeak))
            {
                return;
            }

            var timeToPlay = this.MatchDateTime.AddSeconds(this.Delay);
            var duration = (timeToPlay - DateTime.Now).TotalMilliseconds;

            if (duration > 0d)
            {
                this.delayedSoundTask = DelayableTask.Run(
                    this.PlayDelayedSound,
                    TimeSpan.FromMilliseconds(duration));
            }
        }

        private void PlayDelayedSound()
        {
            this.Delayed = true;

            var regex = this.Regex;
            var wave = this.DelaySound;
            var speak = this.DelayTextToSpeak;

            this.Play(this.DelaySound, this.DelayAdvancedConfig);

            if (!string.IsNullOrWhiteSpace(this.DelayTextToSpeak))
            {
                if (regex == null ||
                    !speak.Contains("$"))
                {
                    this.Play(speak, this.DelayAdvancedConfig);
                    return;
                }

                var match = regex.Match(this.MatchedLog);
                speak = match.Result(speak);

                this.Play(speak, this.DelayAdvancedConfig);
            }
        }

        #region Clone

        public Ticker Clone() => (Ticker)this.MemberwiseClone();

        #endregion Clone

        #region NewTicker

        public static Ticker CreateNew()
        {
            var n = new Ticker();

            lock (TickerTable.Instance.Table)
            {
                n.ID = TickerTable.Instance.Table.Any() ?
                    TickerTable.Instance.Table.Max(x => x.ID) + 1 :
                    1;
            }

            n.Title = "New Ticker";
            n.DisplayTime = 3;
            n.FontColor = Colors.White.ToLegacy().ToHTML();
            n.FontOutlineColor = Colors.Crimson.ToLegacy().ToHTML();
            n.BackgroundColor = Colors.Transparent.ToLegacy().ToHTML();
            n.Top = 30.0d;
            n.Left = 40.0d;

            return n;
        }

        public Ticker CreateSimilarNew()
        {
            var n = Ticker.CreateNew();

            n.Title = this.Title + " New";
            n.Message = this.Message;
            n.Keyword = this.Keyword;
            n.KeywordToHide = this.KeywordToHide;
            n.RegexEnabled = this.RegexEnabled;
            n.Delay = this.Delay;
            n.DisplayTime = this.DisplayTime;
            n.AddMessageEnabled = this.AddMessageEnabled;
            n.ProgressBarEnabled = this.ProgressBarEnabled;
            n.FontColor = this.FontColor;
            n.FontOutlineColor = this.FontOutlineColor;
            n.Font = this.Font.Clone() as FontInfo;
            n.BackgroundColor = this.BackgroundColor;
            n.BackgroundAlpha = this.BackgroundAlpha;
            n.Left = this.Left;
            n.Top = this.Top;
            n.JobFilter = this.JobFilter;
            n.ZoneFilter = this.ZoneFilter;
            n.TimersMustRunningForStart = this.TimersMustRunningForStart;
            n.TimersMustStoppingForStart = this.TimersMustStoppingForStart;

            n.MatchAdvancedConfig = this.MatchAdvancedConfig.Clone() as AdvancedNoticeConfig;
            n.DelayAdvancedConfig = this.DelayAdvancedConfig.Clone() as AdvancedNoticeConfig;
            n.IsSequentialTTS = this.IsSequentialTTS;

            n.Enabled = this.Enabled;

            return n;
        }

        #endregion NewTicker

        public void SimulateMatch()
        {
            var now = DateTime.Now;

            // 擬似的にマッチ状態にする
            this.IsTest = true;
            this.MatchDateTime = now;

            this.Delayed = false;

            // マッチ時点のサウンドを再生する
            this.MatchAdvancedConfig.PlayWave(this.MatchSound);
            this.MatchAdvancedConfig.Speak(this.MatchTextToSpeak);

            // 遅延サウンドタイマを開始する
            this.StartDelayedSoundTimer();

            // トリガリストに加える
            TableCompiler.Instance.AddTestTrigger(this);
        }

        #region Sample Tickers

        public static readonly Ticker[] SampleTickers = new[]
        {
            // インビンシブル
            new Ticker()
            {
                Title = "インビンシブル",
                Message = "{COUNT0} インビンシビル",
                Keyword = "<mex>の「インビンシブル」",
                RegexEnabled = true,
                DisplayTime = 10,
                MatchTextToSpeak = "インビンシブル。",
                Top = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / 2,
                Left = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width /2,
            },

            // インビンシブルサウンド
            new Ticker()
            {
                Title = "インビンシブル（サウンドのみ）",
                Message = "",
                Keyword = "<mex>の「インビンシブル」",
                RegexEnabled = true,
                Delay = 5,
                DisplayTime = 0,
                MatchTextToSpeak = "",
                DelayTextToSpeak = "インビンシブルの終了まで後5秒。",
                Top = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / 2,
                Left = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width /2,
            }
        };

        #endregion Sample Tickers
    }
}
