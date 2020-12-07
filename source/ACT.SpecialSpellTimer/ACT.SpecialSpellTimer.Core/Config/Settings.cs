using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Xml;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Config.Models;
using ACT.SpecialSpellTimer.Config.Views;
using ACT.SpecialSpellTimer.Views;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.WPF.Views;
using FFXIV.Framework.XIVHelper;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Config
{
    [Serializable]
    public partial class Settings :
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

        public void DeInit()
        {
            if (this.polonTimer != null)
            {
                this.polonTimer.Stop();
                this.polonTimer.Dispose();
                this.polonTimer = null;
            }
        }

        #region Constants

        [XmlIgnore]
        public const double UpdateCheckInterval = 12;

        #endregion Constants

        #region Data

        [XmlIgnore]
        public Locales UILocale => FFXIV.Framework.Config.Instance.UILocale;

        public Locales FFXIVLocale => FFXIV.Framework.Config.Instance.XIVLocale;

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
                                        Topmost = true,
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
            set
            {
                if (this.SetProperty(ref pcNameInitialOnDisplayStyle, value))
                {
                    ConfigBridge.Instance.PCNameStyle = value;
                }
            }
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

        private bool isAutoIgnoreLogs = false;

        public bool IsAutoIgnoreLogs
        {
            get => this.isAutoIgnoreLogs;
            set => this.SetProperty(ref this.isAutoIgnoreLogs, value);
        }

        private bool removeWorldName = true;

        public bool RemoveWorldName
        {
            get => this.removeWorldName;
            set => this.SetProperty(ref this.removeWorldName, value);
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

        private bool isDefaultNoticeToOnlyMain = true;

        public bool IsDefaultNoticeToOnlyMain
        {
            get => this.isDefaultNoticeToOnlyMain;
            set => this.SetProperty(ref this.isDefaultNoticeToOnlyMain, value);
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
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.lpsViewX, Math.Round(value));
                }
            }
        }

        public double LPSViewY
        {
            get => this.lpsViewY;
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.lpsViewY, Math.Round(value));
                }
            }
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
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.posViewX, Math.Round(value));
                }
            }
        }

        public double POSViewY
        {
            get => this.posViewY;
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.posViewY, Math.Round(value));
                }
            }
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
                        ChatLogWorker.Instance.Write(true);
                        ChatLogWorker.Instance.Close();
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

        #region Polon

        private System.Timers.Timer polonTimer = new System.Timers.Timer();
        private bool isEnabledPolon = false;

        public bool IsEnabledPolon
        {
            get => this.isEnabledPolon;
            set
            {
                if (this.SetProperty(ref this.isEnabledPolon, value))
                {
                    lock (this.polonTimer)
                    {
                        if (!this.isEnabledPolon)
                        {
                            this.polonTimer.Stop();
                        }
                        else
                        {
                            this.polonCounter = 0;
                            this.polonTimer.Elapsed -= this.PolonTimer_Elapsed;
                            this.polonTimer.Elapsed += this.PolonTimer_Elapsed;
                            this.polonTimer.Interval = 15 * 1000;
                            this.polonTimer.AutoReset = true;
                            this.polonTimer.Start();
                        }
                    }
                }
            }
        }

        private void PolonTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (this.polonTimer)
            {
                if (this.isEnabledPolon)
                {
                    if (this.polonCounter < NomarlPolonCount)
                    {
                        CommonSounds.Instance.PlayAsterisk();
                    }
                    else
                    {
                        var index = this.polonCounter - NomarlPolonCount;
                        if (index >= this.PolonMessages.Length)
                        {
                            CommonSounds.Instance.PlayAsterisk();
                        }
                        else
                        {
                            if (!PlayBridge.Instance.IsAvailable)
                            {
                                CommonSounds.Instance.PlayAsterisk();
                            }
                            else
                            {
                                var tts = this.PolonMessages[index];
                                PlayBridge.Instance.Play(tts, false, 1.0f);
                            }
                        }
                    }

                    this.polonCounter++;
                }
            }
        }

        private int polonCounter = 0;
        private const int NomarlPolonCount = 3;

        private readonly string[] PolonMessages = new[]
        {
            "ぽろん",
            "ぽろろーん",
            "ぴろーん？",
            "ぽぽろん！",
            "ポリネシア！",
            "ポンキッキ！",
            "びろーん",
            "ぽぽぽぽーん！",
            "えーしー",
            "ねぇ、いっしょにポロンしよ？",
            "せーのっ！",
            "さっきのはフェイント",
            "つぎはほんとうにいっしょに！",
            "ぽろん！",
            "あしたもいっしょにポロンしてね。",
        };

        #endregion Polon

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

        private string toAnalyzeLogDirectory = string.Empty;

        public string ToAnalyzeLogDirectory
        {
            get => this.toAnalyzeLogDirectory;
            set => this.SetProperty(ref this.toAnalyzeLogDirectory, value);
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

        private bool isLoaded = false;

        public void Load()
        {
            lock (this.locker)
            {
                this.Reset();

                if (!File.Exists(this.FileName))
                {
                    this.isLoaded = true;
                    this.Save();
                    return;
                }

                var fi = new FileInfo(this.FileName);
                if (fi.Length <= 0)
                {
                    this.isLoaded = true;
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
                            instance.isLoaded = true;
                        }
                    }
                }
            }
        }

        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);

        public void Save()
        {
            lock (this.locker)
            {
                if (!this.isLoaded)
                {
                    return;
                }

                var directoryName = Path.GetDirectoryName(this.FileName);

                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                var ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, string.Empty);

                var buffer = new StringBuilder();
                using (var sw = new StringWriter(buffer))
                {
                    var xs = new XmlSerializer(this.GetType());
                    xs.Serialize(sw, this, ns);
                }

                buffer.Replace("utf-16", "utf-8");

                File.WriteAllText(
                    this.FileName,
                    buffer.ToString() + Environment.NewLine,
                    DefaultEncoding);
            }
        }

        private DateTime lastAutoSaveTimestamp = DateTime.MinValue;

        public void StartAutoSave()
        {
            this.PropertyChanged += async (_, __) =>
            {
                var now = DateTime.Now;

                if ((now - this.lastAutoSaveTimestamp).TotalSeconds > 20)
                {
                    this.lastAutoSaveTimestamp = now;
                    await Task.Run(() => this.Save());
                }
            };
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

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns>
        /// このオブジェクトのクローン</returns>
        public Settings Clone() => (Settings)this.MemberwiseClone();
    }
}
