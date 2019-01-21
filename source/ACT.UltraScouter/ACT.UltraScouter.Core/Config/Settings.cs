using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using ACT.UltraScouter.Common;
using FFXIV.Framework.Common;
using FFXIV.Framework.FFXIVHelper;
using FFXIV.Framework.Globalization;

namespace ACT.UltraScouter.Config
{
    /// <summary>
    /// 設定
    /// </summary>
    [Serializable]
    [XmlRoot(ElementName = "ACT.UltraScouter.Settings", Namespace = "")]
    [DataContract(Name = "ACT.UltraScouter.Settings", Namespace = "")]
    public class Settings :
        INotifyPropertyChanged
    {
        /// <summary>
        /// LOCKオブジェクト
        /// </summary>
        private static readonly object locker = new object();

        #region Constants

        /// <summary>
        /// 更新チェックのインターバル(h)
        /// </summary>
        public const double UpdateCheckInterval = 12.0;

        #endregion Constants

        #region Singleton

        /// <summary>
        /// シングルトンinstance
        /// </summary>
        private static Settings instance;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        private Settings()
        {
            this.Reset();
        }

        /// <summary>
        /// シングルトンinstance
        /// </summary>
        public static Settings Instance
        {
            get
            {
                lock (locker)
                {
                    return instance ?? (instance = new Settings());
                }
            }
        }

        public static void Free()
        {
            lock (locker)
            {
                instance = null;
            }
        }

        #endregion Singleton

        #region Serializer

        /// <summary>
        /// 保存先ファイル名
        /// </summary>
        public readonly string FileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"anoyetta\ACT\ACT.UltraScouter.config");

        /// <summary>
        /// シリアライザ
        /// </summary>
        private readonly XmlSerializer Serializer = new XmlSerializer(typeof(Settings));

        /// <summary>
        /// XMLライターSettings
        /// </summary>
        private readonly XmlWriterSettings XmlWriterSettings = new XmlWriterSettings()
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
        };

        /// <summary>
        /// Load
        /// </summary>
        public void Load()
        {
            var utf8 = new UTF8Encoding(false);

            lock (locker)
            {
                var file = this.FileName;
                if (!File.Exists(file))
                {
                    this.Reset();
                    this.Save();
                    return;
                }

                var fi = new FileInfo(file);
                if (fi.Length <= 0)
                {
                    this.Reset();
                    this.Save();
                    return;
                }

                // typo を置換する
                var text = new StringBuilder(File.ReadAllText(file, utf8));
                text.Replace("Avalable", "Available");
                File.WriteAllText(file, text.ToString(), utf8);

                using (var xr = XmlReader.Create(file))
                {
                    var data = this.Serializer.Deserialize(xr) as Settings;
                    if (data != null)
                    {
                        instance = data;
                    }
                }
            }
        }

        /// <summary>
        /// Save
        /// </summary>
        public void Save()
        {
            lock (locker)
            {
                if (!Directory.Exists(Path.GetDirectoryName(this.FileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(this.FileName));
                }

                // MPTickerのジョブリストが欠けていたら補完する
                var missingJobs = DefaultMPTickerTargetJobs.Where(x =>
                    !this.MPTicker.TargetJobs.Any(y => y.Job == x.Job));
                if (missingJobs.Any())
                {
                    this.MPTicker.TargetJobs.AddRange(missingJobs);
                }

                var ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, string.Empty);

                var buffer = new StringBuilder();
                using (var sw = new StringWriter(buffer))
                using (var xw = XmlWriter.Create(sw, this.XmlWriterSettings))
                {
                    this.Serializer.Serialize(xw, instance, ns);
                }

                buffer.Replace("utf-16", "utf-8");
                File.WriteAllText(
                    this.FileName,
                    buffer.ToString() + Environment.NewLine,
                    new UTF8Encoding(false));
            }
        }

        #endregion Serializer

        #region Data - Update Setting

        /// <summary>Last update datetime</summary>
        private DateTime lastUpdateDateTime;

        [XmlIgnore]
        public DateTime LastUpdateDateTime
        {
            get => this.lastUpdateDateTime;
            set => this.lastUpdateDateTime = value;
        }

        [XmlElement(ElementName = "LastUpdateDateTime")]
        [DataMember(Order = 1, Name = "LastUpdateDateTime")]
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

        #endregion Data - Update Setting

        #region Data

        private double opacity;
        private Locales uiLocale;
        private Locales ffxivLocale;
        private int animationMaxFPS;
        private ThreadPriority scanMemoryThreadPriority = ThreadPriority.Normal;
        private DispatcherPriority uiThreadPriority = DispatcherPriority.Background;

        /// <summary>
        /// プラグインのUIのロケール
        /// </summary>
        [DataMember(Order = 8)]
        public Locales UILocale
        {
            get => this.uiLocale;
            set => this.SetProperty(ref this.uiLocale, value);
        }

        /// <summary>
        /// FFXIVのロケール
        /// </summary>
        [DataMember(Order = 9)]
        public Locales FFXIVLocale
        {
            get => this.ffxivLocale;
            set => this.SetProperty(ref this.ffxivLocale, value);
        }

        /// <summary>
        /// アイドル時のインターバル
        /// </summary>
        [DataMember(Order = 10)]
        public double IdleInterval { get; set; }

        /// <summary>
        /// 不透明度
        /// </summary>
        [DataMember(Order = 11)]
        public double Opacity
        {
            get => this.opacity;
            set => this.SetProperty(ref this.opacity, value);
        }

        /// <summary>
        /// クリックスルー
        /// </summary>
        [DataMember(Order = 12)]
        public bool ClickThrough { get; set; }

        /// <summary>
        /// テキストのアウトラインの増幅率
        /// </summary>
        [DataMember(Order = 13)]
        public double TextOutlineThicknessGain
        {
            get => FontInfo.OutlineThicknessRate;
            set => FontInfo.OutlineThicknessRate = value;
        }

        /// <summary>
        /// テキストのブラーの増幅率
        /// </summary>
        [DataMember(Order = 14)]
        public double TextBlurGain
        {
            get => FontInfo.BlurRadiusRate;
            set => FontInfo.BlurRadiusRate = value;
        }

        /// <summary>
        /// アニメーションの最大FPS
        /// </summary>
        [DataMember(Order = 15)]
        public int AnimationMaxFPS
        {
            get => this.animationMaxFPS;
            set => this.SetProperty(ref this.animationMaxFPS, value);
        }

        /// <summary>
        /// メモリスキャンスレッドタイマの優先順位
        /// </summary>
        [DataMember(Order = 17)]
        public ThreadPriority ScanMemoryThreadPriority
        {
            get => this.scanMemoryThreadPriority;
            set => this.SetProperty(ref this.scanMemoryThreadPriority, value);
        }

        /// <summary>
        /// UIスレッドタイマの優先順位
        /// </summary>
        [DataMember(Order = 17)]
        public DispatcherPriority UIThreadPriority
        {
            get => this.uiThreadPriority;
            set
            {
                switch (value)
                {
                    case DispatcherPriority.Invalid:
                    case DispatcherPriority.Inactive:
                        value = DispatcherPriority.SystemIdle;
                        break;
                }

                this.SetProperty(ref this.uiThreadPriority, value);
            }
        }

        /// <summary>
        /// FFXIVプラグインの監視間隔(msec)
        /// </summary>
        [DataMember(Order = 21)]
        public int PollingRate { get; set; }

        /// <summary>
        /// オーバーレイの更新間隔(msec)
        /// </summary>
        [DataMember(Order = 22)]
        public int OverlayRefreshRate { get; set; }

        /// <summary>
        /// HPバーアニメーションの間隔
        /// </summary>
        [DataMember(Order = 23)]
        public double HPBarAnimationInterval { get; set; }

        /// <summary>
        /// Action Viewカウンターを両方表示したときのフォントサイズ倍率
        /// </summary>
        [DataMember(Order = 24)]
        public double ActionCounterFontSizeRatio { get; set; }

        /// <summary>
        /// Action Viewカウンターを片方表示したときのフォントサイズ倍率
        /// </summary>
        [DataMember(Order = 25)]
        public double ActionCounterSingleFontSizeRatio { get; set; }

        /// <summary>
        /// プログレスバーの暗部の明度
        /// </summary>
        [DataMember(Order = 26)]
        public double ProgressBarDarkRatio { get; set; }

        /// <summary>
        /// プログレスバーのエフェクトの明度
        /// </summary>
        [DataMember(Order = 27)]
        public double ProgressBarEffectRatio { get; set; }

        /// <summary>
        /// HPが見えるか？
        /// </summary>
        [DataMember(Order = 28)]
        public bool HPVisible { get; set; }

        /// <summary>
        /// HP率が見えるか？
        /// </summary>
        [DataMember(Order = 29)]
        public bool HPRateVisible { get; set; }

        /// <summary>
        /// WaveをNAudioで再生するか？
        /// </summary>
        [DataMember(Order = 30)]
        public bool UseNAudio { get; set; }

        /// <summary>
        /// Waveボリューム
        /// </summary>
        [DataMember(Order = 31)]
        public float WaveVolume { get; set; }

        /// <summary>
        /// TTSの再生デバイス
        /// </summary>
        [DataMember(Order = 32)]
        public TTSDevices TTSDevice { get; set; }

        /// <summary>
        /// Circleの背景Opacity
        /// </summary>
        [DataMember(Order = 40)]
        public double CircleBackOpacity { get; set; }

        /// <summary>
        /// Circleのブラー半径
        /// </summary>
        [DataMember(Order = 41)]
        public double CircleBlurRadius { get; set; }

        /// <summary>
        /// Circleの背景明度補正率
        /// </summary>
        [DataMember(Order = 42)]
        public double CircleBackBrightnessRate { get; set; }

        #endregion Data

        #region Data - Overlays

        #region Target

        /// <summary>
        /// ターゲットの名前
        /// </summary>
        [DataMember(Order = 101)]
        public TargetName TargetName { get; set; }

        /// <summary>
        /// ターゲットのHP
        /// </summary>
        [DataMember(Order = 102)]
        public TargetHP TargetHP { get; set; }

        /// <summary>
        /// ターゲットのアクション
        /// </summary>
        [DataMember(Order = 103)]
        public TargetAction TargetAction { get; set; }

        /// <summary>
        /// ターゲットまでの距離
        /// </summary>
        [DataMember(Order = 104)]
        public TargetDistance TargetDistance { get; set; }

        /// <summary>
        /// ホバーターゲットを使用するか？
        /// </summary>
        [DataMember(Order = 105)]
        public bool UseHoverTarget { get; set; }

        /// <summary>
        /// ホバーの有効期限(秒)
        /// </summary>
        [DataMember(Order = 106)]
        public double HoverLifeLimit { get; set; }

        /// <summary>
        /// 敵視情報
        /// </summary>
        [DataMember(Order = 107)]
        public Enmity Enmity { get; set; }

        /// <summary>
        /// FFLogs情報
        /// </summary>
        [DataMember(Order = 108)]
        public FFLogs FFLogs { get; set; }

        #endregion Target

        #region Focus Target

        /// <summary>
        /// ターゲットの名前
        /// </summary>
        [DataMember(Order = 111)]
        public TargetName FTName { get; set; }

        /// <summary>
        /// ターゲットのHP
        /// </summary>
        [DataMember(Order = 112)]
        public TargetHP FTHP { get; set; }

        /// <summary>
        /// ターゲットのアクション
        /// </summary>
        [DataMember(Order = 113)]
        public TargetAction FTAction { get; set; }

        /// <summary>
        /// ターゲットまでの距離
        /// </summary>
        [DataMember(Order = 114)]
        public TargetDistance FTDistance { get; set; }

        #endregion Focus Target

        #region Target of Target

        /// <summary>
        /// ターゲットの名前
        /// </summary>
        [DataMember(Order = 121)]
        public TargetName ToTName { get; set; }

        /// <summary>
        /// ターゲットのHP
        /// </summary>
        [DataMember(Order = 122)]
        public TargetHP ToTHP { get; set; }

        /// <summary>
        /// ターゲットのアクション
        /// </summary>
        [DataMember(Order = 123)]
        public TargetAction ToTAction { get; set; }

        /// <summary>
        /// ターゲットまでの距離
        /// </summary>
        [DataMember(Order = 124)]
        public TargetDistance ToTDistance { get; set; }

        #endregion Target of Target

        #region BOSS

        /// <summary>
        /// ターゲットの名前
        /// </summary>
        [DataMember(Order = 131)]
        public TargetName BossName { get; set; }

        /// <summary>
        /// ターゲットのHP
        /// </summary>
        [DataMember(Order = 132)]
        public TargetHP BossHP { get; set; }

        /// <summary>
        /// ターゲットのアクション
        /// </summary>
        [DataMember(Order = 133)]
        public TargetAction BossAction { get; set; }

        /// <summary>
        /// ターゲットまでの距離
        /// </summary>
        [DataMember(Order = 134)]
        public TargetDistance BossDistance { get; set; }

        private double bossHPThreshold;
        private bool bossVSTargetHideBoss;
        private bool bossVSFTHideBoss;
        private bool bossVSToTHideBoss;

        /// <summary>
        /// パーティの平均HPに対するBOSSのHP倍率のしきい値
        /// </summary>
        [DataMember(Order = 141)]
        public double BossHPThreshold { get => this.bossHPThreshold; set => this.SetProperty(ref this.bossHPThreshold, value); }

        /// <summary>
        /// BOSSとターゲットが同じ時はボスを表示しない
        /// </summary>
        [DataMember(Order = 142)]
        public bool BossVSTargetHideBoss { get => this.bossVSTargetHideBoss; set => this.SetProperty(ref this.bossVSTargetHideBoss, value); }

        /// <summary>
        /// BOSSとFTが同じ時はボスを表示しない
        /// </summary>
        [DataMember(Order = 143)]
        public bool BossVSFTHideBoss { get => this.bossVSFTHideBoss; set => this.SetProperty(ref this.bossVSFTHideBoss, value); }

        /// <summary>
        /// BOSSとTTが同じ時はボスを表示しない
        /// </summary>
        [DataMember(Order = 144)]
        public bool BossVSToTHideBoss { get => this.bossVSToTHideBoss; set => this.SetProperty(ref this.bossVSToTHideBoss, value); }

        #endregion BOSS

        #region Me

        /// <summary>
        /// Meのアクション
        /// </summary>
        [DataMember(Order = 151)]
        public TargetAction MeAction { get; set; }

        /// <summary>
        /// MP Ticker
        /// </summary>
        [DataMember(Order = 152)]
        public MPTicker MPTicker { get; set; }

        #endregion Me

        #region MobList

        /// <summary>
        /// Meのアクション
        /// </summary>
        [DataMember(Order = 161)]
        public MobList MobList { get; set; }

        #endregion MobList

        #endregion Data - Overlays

        #region Data - Default

        /// <summary>
        /// 初期カラー（背景）
        /// </summary>
        public static readonly Color DefaultColorFill = Colors.White;

        /// <summary>
        /// 初期カラー（アウトライン）
        /// </summary>
        public static readonly Color DefaultColorStroke = Colors.DodgerBlue;

        /// <summary>
        /// 初期フォント
        /// </summary>
        public static readonly FontInfo DefaultFont = new FontInfo()
        {
            FontFamily = new FontFamily("Arial"),
            Size = 13,
            Style = FontStyles.Normal,
            Weight = FontWeights.Bold,
            Stretch = FontStretches.Normal,
        };

        /// <summary>
        /// 初期フォントL
        /// </summary>
        public static readonly FontInfo DefaultFontL = new FontInfo()
        {
            FontFamily = new FontFamily("Arial"),
            Size = 20,
            Style = FontStyles.Normal,
            Weight = FontWeights.Bold,
            Stretch = FontStretches.Normal,
        };

        /// <summary>
        /// 初期プログレスバー高さ
        /// </summary>
        public static readonly double DefaultProgressBarHeight = 18;

        /// <summary>
        /// 初期プログレスバー幅
        /// </summary>
        public static readonly double DefaultProgressBarWidth = 200;

        /// <summary>
        /// 初期値
        /// </summary>
        public static readonly Dictionary<string, object> DefaultValues = new Dictionary<string, object>()
        {
            { nameof(Settings.LastUpdateDateTime), DateTime.MinValue },
            { nameof(Settings.UILocale), Locales.JA },
            { nameof(Settings.FFXIVLocale), Locales.JA },
            { nameof(Settings.Opacity), 1.0 },
            { nameof(Settings.TextOutlineThicknessGain), 1.0d },
            { nameof(Settings.TextBlurGain), 2.0d },
            { nameof(Settings.ClickThrough), false },
            { nameof(Settings.PollingRate), 50 },
            { nameof(Settings.OverlayRefreshRate), 100 },
            { nameof(Settings.ScanMemoryThreadPriority), ThreadPriority.Normal },
            { nameof(Settings.UIThreadPriority), DispatcherPriority.Background },
            { nameof(Settings.AnimationMaxFPS), 30 },

            #region Sounds

            { nameof(Settings.UseNAudio), true },
            { nameof(Settings.WaveVolume), 100f },
            { nameof(Settings.TTSDevice), TTSDevices.Normal },

            #endregion Sounds

            #region Constants

            { nameof(Settings.IdleInterval), 1.0 },
            { nameof(Settings.HPBarAnimationInterval), 100 },
            { nameof(Settings.ActionCounterFontSizeRatio), 0.7 },
            { nameof(Settings.ActionCounterSingleFontSizeRatio), 0.85 },
            { nameof(Settings.ProgressBarDarkRatio), 0.35 },
            { nameof(Settings.ProgressBarEffectRatio), 1.2 },
            { nameof(Settings.HPVisible), true },
            { nameof(Settings.HPRateVisible), true },
            { nameof(Settings.HoverLifeLimit), 3.0 },
            { nameof(Settings.CircleBackOpacity), 0.8 },
            { nameof(Settings.CircleBackBrightnessRate), 0.3 },
            { nameof(Settings.CircleBlurRadius), 14.0 },

            #endregion Constants

            #region Target

            { nameof(Settings.UseHoverTarget), false },

            { nameof(Settings.TargetName), new TargetName()
            {
                Visible = true,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke,
                },
            }},

            { nameof(Settings.TargetHP), new TargetHP()
            {
                Visible = true,
                TextLocation = new Location() { X = 0, Y = 0 },
                BarLocation = new Location() { X = 0, Y = 20 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,

                    // ここは色を反転させておく
                    Color = DefaultColorStroke,
                    OutlineColor = DefaultColorFill,
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 500,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 10, Color = Colors.Red },
                        new ProgressBarColorRange() { Min = 10, Max = 20, Color = Colors.OrangeRed },
                        new ProgressBarColorRange() { Min = 20, Max = 50, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 50, Max = 75, Color = Colors.LightSeaGreen },
                        new ProgressBarColorRange() { Min = 75, Max = 100, Color = Colors.RoyalBlue },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                },
                HPBarVisible = true,
                IsHPValueOnHPBar = false,
                LinkFontColorToBarColor = true,
                LinkFontOutlineColorToBarColor = false,
            }},

            { nameof(Settings.TargetDistance), new TargetDistance()
            {
                Visible = true,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke
                },
            }},

            { nameof(Settings.TargetAction), new TargetAction()
            {
                Visible = true,
                Location = new Location() { X = 0, Y = 0 },
                CastingActionNameVisible = true,
                CastingRateVisible = true,
                CastingRemainVisible = false,
                CastingRemainInInteger = true,
                WaveSoundEnabled = false,
                WaveFile = string.Empty,
                TTSEnabled = false,
                UseCircle = false,
                IsCircleReverse = false,
                CircleBlurRadius = 14,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.Gold,
                    OutlineColor = Colors.LemonChiffon
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 300,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 68, Color = Colors.Gold },
                        new ProgressBarColorRange() { Min = 68, Max = 100, Color = Colors.OrangeRed },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                }
            }},

            { nameof(Settings.Enmity), new Enmity()
            {
                Visible = false,
                HideInNotCombat = true,
                HideInSolo = true,
                MaxCountOfDisplay = 8,
                IsDesignMode = false,
                Location = new Location() { X = 0, Y = 0 },
                Scale = 1.0d,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFontL,
                    Color = Colors.White,
                    OutlineColor = Color.FromRgb(0x11, 0x13, 0x2b),
                },
            }},

            { nameof(Settings.FFLogs), new FFLogs()
            {
                Visible = false,
                VisibleHistogram = true,
                HideInCombat = true,
                Location = new Location() { X = 0, Y = 0 },
                Scale = 1.0d,
                IsDesignMode = false,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFontL,
                    Color = Colors.White,
                    OutlineColor = Color.FromRgb(0x11, 0x13, 0x2b),
                },
                RefreshInterval = 8.0d,
                FromCommandTTL = 14.0d,
                CategoryColors = FFLogs.DefaultCategoryColors,
            }},

            #endregion Target

            #region Focus Target

            { nameof(Settings.FTName), new TargetName()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke,
                },
            }},

            { nameof(Settings.FTHP), new TargetHP()
            {
                Visible = false,
                TextLocation = new Location() { X = 0, Y = 0 },
                BarLocation = new Location() { X = 0, Y = 20 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,

                    // ここは色を反転させておく
                    Color = DefaultColorStroke,
                    OutlineColor = DefaultColorFill,
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 500,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 10, Color = Colors.Red },
                        new ProgressBarColorRange() { Min = 10, Max = 20, Color = Colors.OrangeRed },
                        new ProgressBarColorRange() { Min = 20, Max = 50, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 50, Max = 75, Color = Colors.LightSeaGreen },
                        new ProgressBarColorRange() { Min = 75, Max = 100, Color = Colors.RoyalBlue },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                },
                HPBarVisible = true,
                IsHPValueOnHPBar = false,
                LinkFontColorToBarColor = true,
                LinkFontOutlineColorToBarColor = false,
            }},

            { nameof(Settings.FTDistance), new TargetDistance()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke
                },
            }},

            { nameof(Settings.FTAction), new TargetAction()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                CastingActionNameVisible = true,
                CastingRateVisible = true,
                CastingRemainVisible = false,
                CastingRemainInInteger = true,
                WaveSoundEnabled = false,
                WaveFile = string.Empty,
                TTSEnabled = false,
                UseCircle = false,
                IsCircleReverse = false,
                CircleBlurRadius = 14,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.Gold,
                    OutlineColor = Colors.LemonChiffon
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 300,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 68, Color = Colors.Gold },
                        new ProgressBarColorRange() { Min = 68, Max = 100, Color = Colors.OrangeRed },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                }
            }},

            #endregion Focus Target

            #region Target of Target

            { nameof(Settings.ToTName), new TargetName()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke,
                },
            }},

            { nameof(Settings.ToTHP), new TargetHP()
            {
                Visible = false,
                TextLocation = new Location() { X = 0, Y = 0 },
                BarLocation = new Location() { X = 0, Y = 20 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,

                    // ここは色を反転させておく
                    Color = DefaultColorStroke,
                    OutlineColor = DefaultColorFill,
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 500,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 10, Color = Colors.Red },
                        new ProgressBarColorRange() { Min = 10, Max = 20, Color = Colors.OrangeRed },
                        new ProgressBarColorRange() { Min = 20, Max = 50, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 50, Max = 75, Color = Colors.LightSeaGreen },
                        new ProgressBarColorRange() { Min = 75, Max = 100, Color = Colors.RoyalBlue },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                },
                HPBarVisible = true,
                IsHPValueOnHPBar = false,
                LinkFontColorToBarColor = true,
                LinkFontOutlineColorToBarColor = false,
            }},

            { nameof(Settings.ToTDistance), new TargetDistance()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke
                },
            }},

            { nameof(Settings.ToTAction), new TargetAction()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                CastingActionNameVisible = true,
                CastingRateVisible = true,
                CastingRemainVisible = false,
                CastingRemainInInteger = true,
                WaveSoundEnabled = false,
                WaveFile = string.Empty,
                TTSEnabled = false,
                UseCircle = false,
                IsCircleReverse = false,
                CircleBlurRadius = 14,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.Gold,
                    OutlineColor = Colors.LemonChiffon
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 300,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 68, Color = Colors.Gold },
                        new ProgressBarColorRange() { Min = 68, Max = 100, Color = Colors.OrangeRed },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                }
            }},

            #endregion Target of Target

            #region BOSS

            { nameof(Settings.BossName), new TargetName()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke,
                },
            }},

            { nameof(Settings.BossHP), new TargetHP()
            {
                Visible = false,
                TextLocation = new Location() { X = 0, Y = 0 },
                BarLocation = new Location() { X = 0, Y = 20 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,

                    // ここは色を反転させておく
                    Color = DefaultColorStroke,
                    OutlineColor = DefaultColorFill,
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 500,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 10, Color = Colors.Red },
                        new ProgressBarColorRange() { Min = 10, Max = 20, Color = Colors.OrangeRed },
                        new ProgressBarColorRange() { Min = 20, Max = 50, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 50, Max = 75, Color = Colors.LightSeaGreen },
                        new ProgressBarColorRange() { Min = 75, Max = 100, Color = Colors.RoyalBlue },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                },
                HPBarVisible = true,
                IsHPValueOnHPBar = false,
                LinkFontColorToBarColor = true,
                LinkFontOutlineColorToBarColor = false,
            }},

            { nameof(Settings.BossDistance), new TargetDistance()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke
                },
            }},

            { nameof(Settings.BossAction), new TargetAction()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                CastingActionNameVisible = true,
                CastingRateVisible = true,
                CastingRemainVisible = false,
                CastingRemainInInteger = true,
                WaveSoundEnabled = false,
                WaveFile = string.Empty,
                TTSEnabled = false,
                UseCircle = false,
                IsCircleReverse = false,
                CircleBlurRadius = 14,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.Gold,
                    OutlineColor = Colors.LemonChiffon
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 300,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 68, Color = Colors.Gold },
                        new ProgressBarColorRange() { Min = 68, Max = 100, Color = Colors.OrangeRed },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                }
            }},

            { nameof(Settings.BossHPThreshold), 140.0 },
            { nameof(Settings.BossVSTargetHideBoss), true },
            { nameof(Settings.BossVSFTHideBoss), true },
            { nameof(Settings.BossVSToTHideBoss), true },

            #endregion BOSS

            #region Me

            { nameof(Settings.MeAction), new TargetAction()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                CastingActionNameVisible = true,
                CastingRateVisible = true,
                CastingRemainVisible = false,
                CastingRemainInInteger = true,
                WaveSoundEnabled = false,
                WaveFile = string.Empty,
                TTSEnabled = false,
                UseCircle = false,
                IsCircleReverse = false,
                CircleBlurRadius = 14,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.White,
                    OutlineColor = Colors.Gold
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 8,
                    Width = 250,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 74, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 74, Max = 100, Color = Colors.Gold },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                }
            }},

            { nameof(Settings.MPTicker), new MPTicker()
            {
                Visible = false,
                CounterVisible = true,
                Location = new Location() { X = 0, Y = 0 },
                TargetJobs = DefaultMPTickerTargetJobs,
                ExplationTimeForDisplay = 60,
                UseCircle = false,
                IsCircleReverse = false,
                SwapBarAndText = false,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.White,
                    OutlineColor = Colors.Gold
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 8,
                    Width = 100,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 1, Max = 3, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 0, Max = 1, Color = Colors.Gold },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                }
            }},

            #endregion Me

            #region MobList

            { nameof(Settings.MobList), new MobList()
            {
                Visible = false,
                Scale = 1.0d,
                RefreshRateMin = 300,
                VisibleZ = true,
                VisibleMe = true,
                DisplayCount = 10,
                NearDistance = 20,
                TTSEnabled = true,
                Location = new Location() { X = 0, Y = 0 },

                MeDisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.White,
                    OutlineColor = Colors.Navy
                },

                MobFont = DefaultFont,

                MobEXColor = new FontColor()
                {
                    Color = Colors.White,
                    OutlineColor = Colors.Gold
                },

                MobSColor = new FontColor()
                {
                    Color = Colors.White,
                    OutlineColor = Colors.DarkOrange
                },

                MobAColor = new FontColor()
                {
                    Color = Colors.White,
                    OutlineColor = Colors.DarkSeaGreen
                },

                MobBColor = new FontColor()
                {
                    Color = Colors.White,
                    OutlineColor = Colors.DeepSkyBlue
                },

                MobOtherColor = new FontColor()
                {
                    Color = Colors.White,
                    OutlineColor = Colors.Black
                },
            } },

            #endregion MobList
        };

        private static ObservableCollection<JobAvailablity> DefaultMPTickerTargetJobs
        {
            get
            {
                var jobs = new List<JobAvailablity>();
                foreach (var job in Jobs.SortedList.Where(x =>
                    x.Role == Roles.Tank ||
                    x.Role == Roles.Healer ||
                    x.Role == Roles.MeleeDPS ||
                    x.Role == Roles.RangeDPS ||
                    x.Role == Roles.MagicDPS))
                {
                    var entry = new JobAvailablity()
                    {
                        Job = job.ID,
                        Available =
                            job.ID == JobIDs.THM ||
                            job.ID == JobIDs.BLM,
                    };

                    jobs.Add(entry);
                }

                return new ObservableCollection<JobAvailablity>(jobs);
            }
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns>
        /// このオブジェクトのクローン</returns>
        public Settings Clone() => (Settings)this.MemberwiseClone();

        /// <summary>
        /// 初期値に戻す
        /// </summary>
        public void Reset()
        {
            lock (locker)
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

        #endregion Data - Default

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(
            [CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName]string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

            return true;
        }

        #endregion INotifyPropertyChanged
    }
}
