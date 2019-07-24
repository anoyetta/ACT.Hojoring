using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using ACT.UltraScouter.Common;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;

namespace ACT.UltraScouter.Config
{
    /// <summary>
    /// 設定
    /// </summary>
    [Serializable]
    [XmlRoot(ElementName = "ACT.UltraScouter.Settings", Namespace = "")]
    [DataContract(Name = "ACT.UltraScouter.Settings", Namespace = "")]
    public partial class Settings :
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
                        this.Migrate(data);
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

                using (var sw = new StreamWriter(this.FileName, false, new UTF8Encoding(false)))
                {
                    sw.Write(buffer.ToString() + Environment.NewLine);
                    sw.Flush();
                }
            }
        }

        private void Migrate(
            Settings settings)
        {
            // 矢印の基準をカメラ基準だった場合、北基準に変更する
            if (settings.MobList.DirectionOrigin == DirectionOrigin.Camera)
            {
                settings.MobList.DirectionOrigin = DirectionOrigin.North;
            }

            // 矢印の基準をカメラ基準だった場合、北基準に変更する
            if (settings.TacticalRadar.DirectionOrigin == DirectionOrigin.Camera)
            {
                settings.TacticalRadar.DirectionOrigin = DirectionOrigin.North;
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

        public bool IsAnyDesignMode =>
            this.MPTicker.TestMode ||
            this.FFLogs.IsDesignMode ||
            this.Enmity.IsDesignMode ||
            this.MyHP.IsDesignMode ||
            this.MyMP.IsDesignMode;

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
        /// MyHP
        /// </summary>
        [DataMember(Order = 152)]
        public MyStatus MyHP { get; set; }

        /// <summary>
        /// MyMP
        /// </summary>
        [DataMember(Order = 153)]
        public MyStatus MyMP { get; set; }

        /// <summary>
        /// MP Ticker
        /// </summary>
        [DataMember(Order = 154)]
        public MPTicker MPTicker { get; set; }

        /// <summary>
        /// MyMarker
        /// </summary>
        [DataMember(Order = 155)]
        public MyMarker MyMarker { get; set; }

        #endregion Me

        #region MobList

        /// <summary>
        /// Meのアクション
        /// </summary>
        [DataMember(Order = 161)]
        public MobList MobList { get; set; }

        #endregion MobList

        #region MobList

        /// <summary>
        /// 戦術レーダー
        /// </summary>
        [DataMember(Order = 162)]
        public TacticalRadar TacticalRadar { get; set; }

        #endregion MobList

        #endregion Data - Overlays

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
