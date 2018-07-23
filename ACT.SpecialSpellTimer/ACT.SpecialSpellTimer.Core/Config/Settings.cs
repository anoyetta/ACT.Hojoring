using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Interop;
using System.Xml;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Config.Models;
using ACT.SpecialSpellTimer.Config.Views;
using ACT.SpecialSpellTimer.Views;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.FFXIVHelper;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.WPF.Views;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Config
{
    [Serializable]
    public class Settings :
        BindableBase
    {
        #region Singleton

        private static Settings instance;
        private static object singletonLocker = new object();

        public static Settings Default
        {
            get
            {
#if DEBUG
                if (WPFHelper.IsDesignMode)
                {
                    return new Settings();
                }
#endif
                lock (singletonLocker)
                {
                    if (instance == null)
                    {
                        instance = new Settings();
                    }
                }

                return instance;
            }
        }

        #endregion Singleton

        public Settings()
        {
            this.Reset();
        }

        public readonly string FileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"anoyetta\ACT\ACT.SpecialSpellTimer.config");

        #region Constants

        [XmlIgnore]
        public const double UpdateCheckInterval = 12;

        #endregion Constants

        #region Data

        [XmlIgnore]
        public string Language => this.UILocale == Locales.JA ? "JP" : "EN";

        private Locales uiLocale = Locales.JA;

        public Locales UILocale
        {
            get => this.uiLocale;
            set
            {
                if (this.SetProperty(ref this.uiLocale, value))
                {
                    EorzeaTime.DefaultLocale = this.uiLocale == Locales.JA ?
                        EorzeaCalendarLocale.JA :
                        EorzeaCalendarLocale.EN;
                }
            }
        }

        private Locales ffxivLocale = Locales.JA;

        public Locales FFXIVLocale
        {
            get => this.ffxivLocale;
            set => this.SetProperty(ref this.ffxivLocale, value);
        }

        private bool overlayVisible;

        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set
            {
                if (this.SetProperty(ref this.overlayVisible, value))
                {
                    var button = PluginCore.Instance.SwitchVisibleButton;
                    if (button != null)
                    {
                        if (this.overlayVisible)
                        {
                            button.BackColor = Color.SandyBrown;
                            button.ForeColor = Color.WhiteSmoke;
                        }
                        else
                        {
                            button.BackColor = SystemColors.Control;
                            button.ForeColor = Color.Black;
                        }
                    }
                }
            }
        }

        private static DesignGridView[] gridViews;
        private bool visibleDesignGrid;

        [XmlIgnore]
        public bool VisibleDesignGrid
        {
            get => this.visibleDesignGrid;
            set
            {
                if (this.SetProperty(ref this.visibleDesignGrid, value))
                {
                    if (this.visibleDesignGrid)
                    {
                        lock (this)
                        {
                            if (gridViews == null)
                            {
                                var views = new List<DesignGridView>();

                                foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                                {
                                    var view = new DesignGridView()
                                    {
                                        WindowStartupLocation = System.Windows.WindowStartupLocation.Manual,
                                        Left = screen.Bounds.X,
                                        Top = screen.Bounds.Y,
                                        Width = screen.Bounds.Width,
                                        Height = screen.Bounds.Height,
                                    };

                                    view.ToTransparent();
                                    views.Add(view);
                                }

                                gridViews = views.ToArray();
                            }
                        }

                        foreach (var view in gridViews)
                        {
                            view.Show();
                            view.ShowOverlay();
                        }
                    }
                    else
                    {
                        if (gridViews != null)
                        {
                            foreach (var view in gridViews)
                            {
                                view?.HideOverlay();
                                view?.Hide();
                            }
                        }
                    }
                }
            }
        }

        private bool clickThroughEnabled;

        public bool ClickThroughEnabled
        {
            get => this.clickThroughEnabled;
            set => this.SetProperty(ref this.clickThroughEnabled, value);
        }

        private int opacity;

        /// <summary>
        /// !注意! OpacityToView を使用すること！
        /// </summary>
        public int Opacity
        {
            get => this.opacity;
            set
            {
                if (this.SetProperty(ref this.opacity, value))
                {
                    this.RaisePropertyChanged(nameof(this.OpacityToView));

                    if (LPSView.Instance != null &&
                        LPSView.Instance.OverlayVisible)
                    {
                        LPSView.Instance.Opacity = this.OpacityToView;
                    }

                    if (POSView.Instance != null &&
                        POSView.Instance.OverlayVisible)
                    {
                        POSView.Instance.Opacity = this.OpacityToView;
                    }
                }
            }
        }

        [XmlIgnore]
        public double OpacityToView => (100d - this.Opacity) / 100d;

        public bool HideWhenNotActive { get; set; }

        /// <summary>
        /// FFXIVのプロセスが存在しなくてもオーバーレイを表示する
        /// </summary>
        public bool VisibleOverlayWithoutFFXIV { get; set; } = false;

        public long LogPollSleepInterval { get; set; }

        public long RefreshInterval { get; set; }

        public int MaxFPS { get; set; }
        public bool RenderCPUOnly { get; set; } = false;

        private NameStyles pcNameInitialOnDisplayStyle = NameStyles.FullName;

        public NameStyles PCNameInitialOnDisplayStyle
        {
            get => this.pcNameInitialOnDisplayStyle;
            set => this.SetProperty(ref pcNameInitialOnDisplayStyle, value);
        }

        public double TextBlurRate
        {
            get => FontInfo.BlurRadiusRate;
            set => FontInfo.BlurRadiusRate = value;
        }

        public double TextOutlineThicknessRate
        {
            get => FontInfo.OutlineThicknessRate;
            set => FontInfo.OutlineThicknessRate = value;
        }

        public int ReduceIconBrightness { get; set; }

        public string ReadyText { get; set; }

        public string OverText { get; set; }

        #region Data - ProgressBar Background

        [XmlIgnore] private readonly System.Windows.Media.Color DefaultBackgroundColor = System.Windows.Media.Colors.Black;
        [XmlIgnore] private bool barBackgroundFixed;
        [XmlIgnore] private double barBackgroundBrightness;
        [XmlIgnore] private System.Windows.Media.Color barDefaultBackgroundColor;

        /// <summary>
        /// プログレスバーの背景が固定色か？
        /// </summary>
        public bool BarBackgroundFixed
        {
            get => this.barBackgroundFixed;
            set => this.SetProperty(ref this.barBackgroundFixed, value);
        }

        /// <summary>
        /// プログレスバーの背景の輝度
        /// </summary>
        public double BarBackgroundBrightness
        {
            get => this.barBackgroundBrightness;
            set => this.barBackgroundBrightness = value;
        }

        /// <summary>
        /// プログレスバーの標準の背景色
        /// </summary>
        [XmlIgnore]
        public System.Windows.Media.Color BarDefaultBackgroundColor
        {
            get => this.barDefaultBackgroundColor;
            set => this.barDefaultBackgroundColor = value;
        }

        /// <summary>
        /// プログレスバーの標準の背景色
        /// </summary>
        [XmlElement(ElementName = "BarDefaultBackgroundColor")]
        public string BarDefaultBackgroundColorText
        {
            get => this.BarDefaultBackgroundColor.ToString();
            set => this.BarDefaultBackgroundColor = this.DefaultBackgroundColor.FromString(value);
        }

        #endregion Data - ProgressBar Background

        public double TimeOfHideSpell { get; set; }

        public bool EnabledPartyMemberPlaceholder { get; set; }

        public bool EnabledSpellTimerNoDecimal { get; set; }

        public bool EnabledNotifyNormalSpellTimer { get; set; }

        public string NotifyNormalSpellTimerPrefix { get; set; }

        public bool SimpleRegex { get; set; }

        public bool RemoveTooltipSymbols { get; set; }

        private bool ignoreDetailLogs = false;

        public bool IgnoreDetailLogs
        {
            get => this.ignoreDetailLogs;
            set => this.SetProperty(ref this.ignoreDetailLogs, value);
        }

        private bool ignoreDamageLogs = true;

        public bool IgnoreDamageLogs
        {
            get => this.ignoreDamageLogs;
            set => this.SetProperty(ref this.ignoreDamageLogs, value);
        }

        public bool DetectPacketDump { get; set; }

        private bool resetOnWipeOut;

        public bool ResetOnWipeOut
        {
            get => this.resetOnWipeOut;
            set => this.SetProperty(ref this.resetOnWipeOut, value);
        }

        public bool WipeoutNotifyToACT { get; set; }

        private double waitingTimeToSyncTTS = 100;

        public double WaitingTimeToSyncTTS
        {
            get => this.waitingTimeToSyncTTS;
            set => this.SetProperty(ref this.waitingTimeToSyncTTS, value);
        }

        #region LPS View

        private bool lpsViewVisible = false;
        private double lpsViewX;
        private double lpsViewY;
        private double lpsViewScale = 1.0;

        public bool LPSViewVisible
        {
            get => this.lpsViewVisible;
            set
            {
                if (this.SetProperty(ref this.lpsViewVisible, value))
                {
                    if (LPSView.Instance != null)
                    {
                        LPSView.Instance.OverlayVisible = value;
                    }
                }
            }
        }

        public double LPSViewX
        {
            get => this.lpsViewX;
            set => this.SetProperty(ref this.lpsViewX, Math.Floor(value));
        }

        public double LPSViewY
        {
            get => this.lpsViewY;
            set => this.SetProperty(ref this.lpsViewY, Math.Floor(value));
        }

        public double LPSViewScale
        {
            get => this.lpsViewScale;
            set => this.SetProperty(ref this.lpsViewScale, value);
        }

        #endregion LPS View

        #region POS View

        private bool posViewVisible = false;
        private double posViewX;
        private double posViewY;
        private double posViewScale = 1.0;
        private bool posViewVisibleDebugInfo = WPFHelper.IsDesignMode ? true : false;

        public bool POSViewVisible
        {
            get => this.posViewVisible;
            set
            {
                if (this.SetProperty(ref this.posViewVisible, value))
                {
                    if (POSView.Instance != null)
                    {
                        POSView.Instance.OverlayVisible = value;
                    }
                }
            }
        }

        public double POSViewX
        {
            get => this.posViewX;
            set => this.SetProperty(ref this.posViewX, Math.Floor(value));
        }

        public double POSViewY
        {
            get => this.posViewY;
            set => this.SetProperty(ref this.posViewY, Math.Floor(value));
        }

        public double POSViewScale
        {
            get => this.posViewScale;
            set => this.SetProperty(ref this.posViewScale, value);
        }

        public bool POSViewVisibleDebugInfo
        {
            get => this.posViewVisibleDebugInfo;
            set => this.SetProperty(ref this.posViewVisibleDebugInfo, value);
        }

        #endregion POS View

        private bool saveLogEnabled;
        private string saveLogDirectory;

        public bool SaveLogEnabled
        {
            get => this.saveLogEnabled;
            set

            {
                if (this.SetProperty(ref this.saveLogEnabled, value))
                {
                    if (!this.saveLogEnabled)
                    {
                        ChatLogWorker.Instance.Flush();
                    }
                }
            }
        }

        public string SaveLogDirectory
        {
            get => this.saveLogDirectory;
            set => this.SetProperty(ref this.saveLogDirectory, value);
        }

        private string timelineDirectory = string.Empty;

        public string TimelineDirectory
        {
            get => this.timelineDirectory;
            set => this.SetProperty(ref this.timelineDirectory, value);
        }

        private bool isMinimizeOnStart = false;

        public bool IsMinimizeOnStart
        {
            get => this.isMinimizeOnStart;
            set => this.SetProperty(ref this.isMinimizeOnStart, value);
        }

        #region Data - Timeline

        private bool timelineTotalSecoundsFormat = false;

        public bool TimelineTotalSecoundsFormat
        {
            get => this.timelineTotalSecoundsFormat;
            set => this.SetProperty(ref this.timelineTotalSecoundsFormat, value);
        }

        private bool autoCombatLogAnalyze;

        public bool AutoCombatLogAnalyze
        {
            get => this.autoCombatLogAnalyze;
            set => this.SetProperty(ref this.autoCombatLogAnalyze, value);
        }

        private bool autoCombatLogSave;

        public bool AutoCombatLogSave
        {
            get => this.autoCombatLogSave;
            set => this.SetProperty(ref this.autoCombatLogSave, value);
        }

        private string combatLogSaveDirectory = string.Empty;

        public string CombatLogSaveDirectory
        {
            get => this.combatLogSaveDirectory;
            set => this.SetProperty(ref this.combatLogSaveDirectory, value);
        }

        #endregion Data - Timeline

        #endregion Data

        #region Data - Hidden

        public bool AutoSortEnabled { get; set; }

        public bool AutoSortReverse { get; set; }

        public bool SingleTaskLogMatching { get; set; }

        public bool DisableStartCondition { get; set; }

        public bool EnableMultiLineMaching { get; set; }

        /// <summary>点滅の輝度倍率 暗</summary>
        public double BlinkBrightnessDark { get; set; }

        /// <summary>点滅の輝度倍率 明</summary>
        public double BlinkBrightnessLight { get; set; }

        /// <summary>点滅のピッチ(秒)</summary>
        public double BlinkPitch { get; set; }

        /// <summary>点滅のピーク状態でのホールド時間(秒)</summary>
        public double BlinkPeekHold { get; set; }

        public List<ExpandedContainer> ExpandedList
        {
            get;
            set;
        } = new List<ExpandedContainer>();

        private DateTime lastUpdateDateTime;

        [XmlIgnore]
        public DateTime LastUpdateDateTime
        {
            get => this.lastUpdateDateTime;
            set => this.lastUpdateDateTime = value;
        }

        [XmlElement(ElementName = "LastUpdateDateTime")]
        public string LastUpdateDateTimeCrypted
        {
            get => Crypter.EncryptString(this.lastUpdateDateTime.ToString("o"));
            set
            {
                DateTime d;
                if (DateTime.TryParse(value, out d))
                {
                    if (d > DateTime.Now)
                    {
                        d = DateTime.Now;
                    }

                    this.lastUpdateDateTime = d;
                    return;
                }

                try
                {
                    var decrypt = Crypter.DecryptString(value);
                    if (DateTime.TryParse(decrypt, out d))
                    {
                        if (d > DateTime.Now)
                        {
                            d = DateTime.Now;
                        }

                        this.lastUpdateDateTime = d;
                        return;
                    }
                }
                catch (Exception)
                {
                }

                this.lastUpdateDateTime = DateTime.MinValue;
            }
        }

        #endregion Data - Hidden

        #region Load & Save

        private readonly object locker = new object();

        public void Load()
        {
            lock (this.locker)
            {
                this.Reset();

                if (!File.Exists(this.FileName))
                {
                    this.Save();
                    return;
                }

                var fi = new FileInfo(this.FileName);
                if (fi.Length <= 0)
                {
                    this.Save();
                    return;
                }

                using (var sr = new StreamReader(this.FileName, new UTF8Encoding(false)))
                {
                    if (sr.BaseStream.Length > 0)
                    {
                        var xs = new XmlSerializer(this.GetType());
                        var data = xs.Deserialize(sr) as Settings;
                        if (data != null)
                        {
                            instance = data;
                        }
                    }
                }
            }
        }

        public void Save()
        {
            lock (this.locker)
            {
                var directoryName = Path.GetDirectoryName(this.FileName);

                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                var sb = new StringBuilder();
                using (var sw = new StringWriter(sb))
                {
                    var xs = new XmlSerializer(this.GetType());
                    xs.Serialize(sw, this);
                }

                sb.Replace("utf-16", "utf-8");

                File.WriteAllText(
                    this.FileName,
                    sb.ToString(),
                    new UTF8Encoding(false));
            }
        }

        /// <summary>
        /// レンダリングモードを適用する
        /// </summary>
        public void ApplyRenderMode()
        {
            var renderMode =
                this.RenderCPUOnly ? RenderMode.SoftwareOnly : RenderMode.Default;

            if (System.Windows.Media.RenderOptions.ProcessRenderMode != renderMode)
            {
                System.Windows.Media.RenderOptions.ProcessRenderMode = renderMode;
            }
        }

        #endregion Load & Save

        #region Default Values & Reset

        public static readonly Dictionary<string, object> DefaultValues = new Dictionary<string, object>()
        {
            { nameof(Settings.NotifyNormalSpellTimerPrefix), "spespe_" },
            { nameof(Settings.ReadyText), "Ready" },
            { nameof(Settings.OverText), "Over" },
            { nameof(Settings.TimeOfHideSpell), 1.0d },
            { nameof(Settings.LogPollSleepInterval), 10 },
            { nameof(Settings.RefreshInterval), 60 },
            { nameof(Settings.ReduceIconBrightness), 55 },
            { nameof(Settings.Opacity), 10 },
            { nameof(Settings.OverlayVisible), true },
            { nameof(Settings.AutoSortEnabled), true },
            { nameof(Settings.ClickThroughEnabled), false },
            { nameof(Settings.AutoSortReverse), false },
            { nameof(Settings.EnabledPartyMemberPlaceholder), true },
            { nameof(Settings.AutoCombatLogAnalyze), false },
            { nameof(Settings.EnabledSpellTimerNoDecimal), true },
            { nameof(Settings.EnabledNotifyNormalSpellTimer), false },
            { nameof(Settings.SaveLogEnabled), false },
            { nameof(Settings.SaveLogDirectory), string.Empty },
            { nameof(Settings.HideWhenNotActive), false },
            { nameof(Settings.ResetOnWipeOut), true },
            { nameof(Settings.WipeoutNotifyToACT), true },
            { nameof(Settings.RemoveTooltipSymbols), true },
            { nameof(Settings.SimpleRegex), true },
            { nameof(Settings.DetectPacketDump), false },
            { nameof(Settings.TextBlurRate), 1.2d },
            { nameof(Settings.TextOutlineThicknessRate), 1.0d },
            { nameof(Settings.PCNameInitialOnDisplayStyle), NameStyles.FullName },
            { nameof(Settings.RenderCPUOnly), true },
            { nameof(Settings.SingleTaskLogMatching), false },
            { nameof(Settings.DisableStartCondition), false },
            { nameof(Settings.EnableMultiLineMaching), false },
            { nameof(Settings.MaxFPS), 30 },

            { nameof(Settings.LPSViewVisible), false },
            { nameof(Settings.LPSViewX), 0 },
            { nameof(Settings.LPSViewY), 0 },
            { nameof(Settings.LPSViewScale), 1.0 },

            { nameof(Settings.BarBackgroundFixed), false },
            { nameof(Settings.BarBackgroundBrightness), 0.3 },
            { nameof(Settings.BarDefaultBackgroundColor), System.Windows.Media.Color.FromArgb(240, 0, 0, 0) },

            // 設定画面のない設定項目
            { nameof(Settings.LastUpdateDateTime), DateTime.Parse("2000-1-1") },
            { nameof(Settings.BlinkBrightnessDark), 0.3d },
            { nameof(Settings.BlinkBrightnessLight), 2.5d },
            { nameof(Settings.BlinkPitch), 0.5d },
            { nameof(Settings.BlinkPeekHold), 0.08d },
        };

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns>
        /// このオブジェクトのクローン</returns>
        public Settings Clone() => (Settings)this.MemberwiseClone();

        public void Reset()
        {
            lock (this.locker)
            {
                var pis = this.GetType().GetProperties();
                foreach (var pi in pis)
                {
                    try
                    {
                        var defaultValue =
                            DefaultValues.ContainsKey(pi.Name) ?
                            DefaultValues[pi.Name] :
                            null;

                        if (defaultValue != null)
                        {
                            pi.SetValue(this, defaultValue);
                        }
                    }
                    catch
                    {
                        Debug.WriteLine($"Settings Reset Error: {pi.Name}");
                    }
                }
            }
        }

        #endregion Default Values & Reset
    }
}
