using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        IFilterizableTrigger
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

        public Spell()
        {
        }

        private bool isCircleStyle;

        public bool IsCircleStyle
        {
            get => this.isCircleStyle;
            set
            {
                if (this.SetProperty(ref this.isCircleStyle, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsStandardStyle));
                    this.RaisePropertyChanged(nameof(this.DefaultSpellMargin));
                }
            }
        }

        private VerticalAlignment titleVerticalAlignmentInCircle = VerticalAlignment.Center;

        public VerticalAlignment TitleVerticalAlignmentInCircle
        {
            get => this.titleVerticalAlignmentInCircle;
            set => this.SetProperty(ref this.titleVerticalAlignmentInCircle, value);
        }

        [XmlIgnore]
        public bool IsStandardStyle => !this.isCircleStyle;

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

        private static readonly Thickness HorizontalDefaultMargin = new Thickness(0, 0, 10, 0);
        private static readonly Thickness VerticalDefaultMarginStandard = new Thickness(0, 2, 0, 10);
        private static readonly Thickness VerticalDefaultMarginCircle = new Thickness(0, 0, 0, 10);

        [XmlIgnore]
        public Thickness DefaultSpellMargin
        {
            get
            {
                var result = new Thickness();

                var panel = this.Panel;
                if (panel == null)
                {
                    return result;
                }

                if (!panel.EnabledAdvancedLayout)
                {
                    result = !panel.Horizontal ?
                        getVerticalMargin() :
                        HorizontalDefaultMargin;
                }
                else
                {
                    if (panel.IsStackLayout)
                    {
                        switch (panel.StackPanelOrientation)
                        {
                            case Orientation.Horizontal:
                                result = HorizontalDefaultMargin;
                                break;

                            case Orientation.Vertical:
                                result = getVerticalMargin();
                                break;
                        }
                    }
                }

                return result;

                // Standard スタイルでは互換性のため上のマージン2pxを残している
                // Circle スタイルでは2pxのマージンを廃止したため使い分ける
                Thickness getVerticalMargin()
                    => this.IsCircleStyle ? VerticalDefaultMarginCircle : VerticalDefaultMarginStandard;
            }
        }

        public void RaiseSpellMarginChanged()
            => this.RaisePropertyChanged(nameof(this.DefaultSpellMargin));

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

        private string spellTitle = string.Empty;

        /// <summary>
        /// スペルタイトル（スペル表示名）
        /// </summary>
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

        #region Keywords & Regex compiler

        [XmlIgnore]
        public bool IsRealtimeCompile { get; set; } = false;

        private bool regexEnabled;
        private string keyword;
        private string keywordForExtend1;
        private string keywordForExtend2;
        private string keywordForExtend3;

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
                    this.KeywordForExtendReplaced3 = string.Empty;

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

                        ex = this.CompileRegexExtend3();
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

        public string KeywordForExtend3
        {
            get => this.keywordForExtend3;
            set
            {
                if (this.SetProperty(ref this.keywordForExtend3, value))
                {
                    this.KeywordForExtendReplaced3 = string.Empty;
                    if (this.IsRealtimeCompile)
                    {
                        var ex = this.CompileRegexExtend3();
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
        /// リキャスト時間
        /// </summary>
        public double RecastTime { get; set; } = 0;

        private double delayToShow = 0;

        /// <summary>
        /// 表示までのディレイ
        /// </summary>
        public double DelayToShow
        {
            get => this.delayToShow;
            set => this.SetProperty(ref this.delayToShow, value);
        }

        /// <summary>
        /// 延長する時間1
        /// </summary>
        public double RecastTimeExtending1 { get; set; } = 0;

        /// <summary>
        /// 延長する時間2
        /// </summary>
        public double RecastTimeExtending2 { get; set; } = 0;

        /// <summary>
        /// 延長する時間3
        /// </summary>
        public double RecastTimeExtending3 { get; set; } = 0;

        private bool overlapRecastTime;

        /// <summary>
        /// 元のリキャスト時間を超えて延長するか？
        /// </summary>
        public bool OverlapRecastTime
        {
            get => this.overlapRecastTime;
            set => this.SetProperty(ref this.overlapRecastTime, value);
        }

        private bool isNotResetBarOnExtended = false;

        /// <summary>
        /// 延長したときにバーをリセットしない？
        /// </summary>
        public bool IsNotResetBarOnExtended
        {
            get => this.isNotResetBarOnExtended;
            set => this.SetProperty(ref this.isNotResetBarOnExtended, value);
        }

        [XmlIgnore]
        public string KeywordReplaced { get; set; }

        [XmlIgnore]
        public string KeywordForExtendReplaced1 { get; set; }

        [XmlIgnore]
        public string KeywordForExtendReplaced2 { get; set; }

        [XmlIgnore]
        public string KeywordForExtendReplaced3 { get; set; }

        [XmlIgnore]
        public Regex Regex { get; set; }

        [XmlIgnore]
        public Regex RegexForExtend1 { get; set; }

        [XmlIgnore]
        public Regex RegexForExtend2 { get; set; }

        [XmlIgnore]
        public Regex RegexForExtend3 { get; set; }

        [XmlIgnore]
        public string RegexPattern { get; set; }

        [XmlIgnore]
        public string RegexForExtendPattern1 { get; set; }

        [XmlIgnore]
        public string RegexForExtendPattern2 { get; set; }

        [XmlIgnore]
        public string RegexForExtendPattern3 { get; set; }

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

        public Exception CompileRegexExtend3()
        {
            var pattern = string.Empty;

            try
            {
                this.KeywordForExtendReplaced3 = TableCompiler.Instance.GetMatchingKeyword(
                    this.KeywordForExtendReplaced3,
                    this.KeywordForExtend3);

                if (this.RegexEnabled)
                {
                    pattern = this.KeywordForExtendReplaced3.ToRegexPattern();

                    if (this.RegexForExtend3 == null ||
                        this.RegexForExtendPattern3 != pattern)
                    {
                        this.RegexForExtend3 = pattern.ToRegex();
                        this.RegexForExtendPattern3 = pattern;
                    }
                }
                else
                {
                    this.RegexForExtend3 = null;
                    this.RegexForExtendPattern3 = string.Empty;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        #endregion Keywords & Regex compiler

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

        private bool changeFontColorWhenContainsMe;

        public bool ChangeFontColorWhenContainsMe
        {
            get => this.changeFontColorWhenContainsMe;
            set => this.SetProperty(ref this.changeFontColorWhenContainsMe, value);
        }

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

        private double barBlurRadius = 11;

        public double BarBlurRadius
        {
            get => this.barBlurRadius;
            set => this.SetProperty(ref this.barBlurRadius, value);
        }

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

        private bool isHideInNotCombat;

        public bool IsHideInNotCombat
        {
            get => this.isHideInNotCombat;
            set => this.SetProperty(ref this.isHideInNotCombat, value);
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
                    this.RaisePropertyChanged(nameof(this.SpellTitleColumnWidth));
                    this.RaisePropertyChanged(nameof(this.CounterColumnWidth));
                }
            }
        }

        [XmlIgnore]
        public HorizontalAlignment CounterAlignment =>
            this.isCounterToCenter ?
            HorizontalAlignment.Center :
            HorizontalAlignment.Right;

        [XmlIgnore]
        public GridLength SpellTitleColumnWidth =>
            this.isCounterToCenter ?
            GridLength.Auto :
            new GridLength(1.0, GridUnitType.Star);

        [XmlIgnore]
        public GridLength CounterColumnWidth =>
            this.isCounterToCenter ?
            new GridLength(1.0, GridUnitType.Star) :
            GridLength.Auto;

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
                    this.RaisePropertyChanged(nameof(this.SpellIconImage));
                }
            }
        }

        [XmlIgnore]
        public string SpellIconFullPath =>
            string.IsNullOrEmpty(this.SpellIcon) ?
            string.Empty :
            IconController.Instance.GetIconFile(this.SpellIcon)?.FullPath;

        [XmlIgnore]
        public BitmapSource SpellIconImage =>
            string.IsNullOrEmpty(this.SpellIcon) ?
            IconController.BlankBitmap :
            IconController.Instance.GetIconFile(this.SpellIcon)?.BitmapImage;

        private int spellIconSize = 24;

        public int SpellIconSize
        {
            get => this.spellIconSize;
            set => this.SetProperty(ref this.spellIconSize, value);
        }

        private bool isNotResetAtWipeout = false;

        public bool IsNotResetAtWipeout
        {
            get => this.isNotResetAtWipeout;
            set => this.SetProperty(ref this.isNotResetAtWipeout, value);
        }

        /// <summary>スペルが作用した対象</summary>
        [XmlIgnore]
        public string TargetName { get; set; } = string.Empty;

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

        #region Filters & Conditions

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

            // waveサウンドはシンクロ再生しない
            if (isWave)
            {
                noticeConfig.PlayWave(tts);
                return;
            }

            // ゆっくりがいないならシンクロ再生はしない
            if (!PlayBridge.Instance.IsAvailable ||
                !isSync)
            {
                noticeConfig.Speak(tts);
                return;
            }

            var priority = int.MaxValue;
            if (trigger is Spell spell)
            {
                priority = (int)spell.DisplayNo;
            }

            noticeConfig.Speak(tts, true, priority);
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

        [XmlIgnore]
        private DelayableTask overSoundTask;

        [XmlIgnore]
        private DelayableTask beforeSoundTask;

        [XmlIgnore]
        private DelayableTask timeupSoundTask;

        public void Dispose()
        {
            if (this.overSoundTask != null)
            {
                this.overSoundTask.IsCancel = true;
            }

            if (this.beforeSoundTask != null)
            {
                this.beforeSoundTask.IsCancel = true;
            }

            if (this.timeupSoundTask != null)
            {
                this.timeupSoundTask.IsCancel = true;
            }
        }

        /// <summary>
        /// マッチ後ｎ秒後のサウンドタイマを開始する
        /// </summary>
        public void StartOverSoundTimer()
        {
            if (this.overSoundTask != null)
            {
                this.overSoundTask.IsCancel = true;
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

            var timeToPlay = this.MatchDateTime.AddSeconds(this.OverTime);
            var duration = (timeToPlay - DateTime.Now).TotalMilliseconds;

            if (duration > 0d)
            {
                this.overSoundTask = DelayableTask.Run(
                    this.PlayOverSound,
                    TimeSpan.FromMilliseconds(duration));
            }
        }

        /// <summary>
        /// リキャストｎ秒前のサウンドタイマを開始する
        /// </summary>
        public void StartBeforeSoundTimer()
        {
            if (this.beforeSoundTask != null)
            {
                this.beforeSoundTask.IsCancel = true;
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

            var timeToPlay = this.CompleteScheduledTime.AddSeconds(this.BeforeTime * -1);
            var duration = (timeToPlay - DateTime.Now).TotalMilliseconds;

            if (duration > 0d)
            {
                this.beforeSoundTask = DelayableTask.Run(
                    this.PlayBeforeSound,
                    TimeSpan.FromMilliseconds(duration));
            }
        }

        /// <summary>
        /// リキャスト完了のサウンドタイマを開始する
        /// </summary>
        public void StartTimeupSoundTimer()
        {
            if (this.timeupSoundTask != null)
            {
                this.timeupSoundTask.IsCancel = true;
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

            var timeToPlay = this.CompleteScheduledTime;
            var duration = (timeToPlay - DateTime.Now).TotalMilliseconds;

            if (duration > 0d)
            {
                this.timeupSoundTask = DelayableTask.Run(
                    this.PlayTimeupSound,
                    TimeSpan.FromMilliseconds(duration));
            }
            else
            {
                this.PlayTimeupSound();
            }
        }

        private void PlayBeforeSound()
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

        private void PlayOverSound()
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

        private void PlayTimeupSound()
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
            n.KeywordForExtend3 = this.KeywordForExtend3;
            n.RecastTimeExtending3 = this.RecastTimeExtending3;

            n.IsNotResetBarOnExtended = this.IsNotResetBarOnExtended;
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
            n.BarBlurRadius = this.BarBlurRadius;
            n.DontHide = this.DontHide;
            n.HideSpellName = this.HideSpellName;
            n.WarningTime = this.WarningTime;
            n.BlinkTime = this.BlinkTime;
            n.BlinkIcon = this.BlinkIcon;
            n.BlinkBar = this.BlinkBar;
            n.ChangeFontColorsWhenWarning = this.ChangeFontColorsWhenWarning;
            n.ChangeFontColorWhenContainsMe = this.ChangeFontColorWhenContainsMe;
            n.OverlapRecastTime = this.OverlapRecastTime;
            n.ReduceIconBrightness = this.ReduceIconBrightness;
            n.Font = this.Font.Clone() as FontInfo;
            n.IsCircleStyle = this.IsCircleStyle;
            n.IsCounterToCenter = this.IsCounterToCenter;
            n.TitleVerticalAlignmentInCircle = this.TitleVerticalAlignmentInCircle;
            n.BarWidth = this.BarWidth;
            n.BarHeight = this.BarHeight;
            n.BackgroundColor = this.BackgroundColor;
            n.BackgroundAlpha = this.BackgroundAlpha;
            n.HideCounter = this.HideCounter;
            n.JobFilter = this.JobFilter;
            n.PartyJobFilter = this.PartyJobFilter;
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
            n.KeywordForExtend3 = this.KeywordForExtend3;
            n.RecastTimeExtending1 = this.RecastTimeExtending1;
            n.RecastTimeExtending2 = this.RecastTimeExtending2;
            n.RecastTimeExtending3 = this.RecastTimeExtending3;

            n.RecastTime = this.RecastTime;
            n.IsNotResetBarOnExtended = this.IsNotResetBarOnExtended;
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
            n.FontColor = this.FontColor;
            n.FontOutlineColor = this.FontOutlineColor;
            n.WarningFontColor = this.WarningFontColor;
            n.WarningFontOutlineColor = this.WarningFontOutlineColor;
            n.BarColor = this.BarColor;
            n.BarOutlineColor = this.BarOutlineColor;
            n.BarBlurRadius = this.BarBlurRadius;
            n.IsCircleStyle = this.IsCircleStyle;
            n.IsCounterToCenter = this.IsCounterToCenter;
            n.TitleVerticalAlignmentInCircle = this.TitleVerticalAlignmentInCircle;
            n.BarWidth = this.BarWidth;
            n.BarHeight = this.BarHeight;
            n.BackgroundColor = this.BackgroundColor;
            n.BackgroundAlpha = this.BackgroundAlpha;
            n.HideCounter = this.HideCounter;
            n.DontHide = this.DontHide;
            n.HideSpellName = this.HideSpellName;
            n.WarningTime = this.WarningTime;
            n.ChangeFontColorsWhenWarning = this.ChangeFontColorsWhenWarning;
            n.ChangeFontColorWhenContainsMe = this.ChangeFontColorWhenContainsMe;
            n.BlinkTime = this.BlinkTime;
            n.BlinkIcon = this.BlinkIcon;
            n.BlinkBar = this.BlinkBar;
            n.OverlapRecastTime = this.OverlapRecastTime;
            n.ReduceIconBrightness = this.ReduceIconBrightness;
            n.RegexEnabled = this.RegexEnabled;
            n.IsNotResetAtWipeout = this.IsNotResetAtWipeout;
            n.JobFilter = this.JobFilter;
            n.PartyJobFilter = this.PartyJobFilter;
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
            n.RegexForExtend3 = this.RegexForExtend3;
            n.RegexForExtendPattern3 = this.RegexForExtendPattern3;
            n.KeywordForExtendReplaced3 = this.KeywordForExtendReplaced3;

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
