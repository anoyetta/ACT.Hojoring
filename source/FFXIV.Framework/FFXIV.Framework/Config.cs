using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using FFXIV_ACT_Plugin.Logfile;
using Prism.Mvvm;

namespace FFXIV.Framework
{
    [Serializable]
    [XmlType(TypeName = "FFXIV.Framework")]
    public class Config :
        BindableBase
    {
        #region Singleton

        private static Config instance;

        public static Config Instance =>
            instance ?? (instance = (Load() ?? new Config()));

        private Config()
        {
        }

        public static void Free() => instance = null;

        #endregion Singleton

        #region Load & Save

        private static string OldFileName => Assembly.GetExecutingAssembly().Location.Replace(".dll", ".config");

        public static string FileName => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "anoyetta",
            "ACT",
            Path.GetFileName(Assembly.GetExecutingAssembly().Location.Replace(".dll", ".config")));

        public static Config Load()
        {
            if (File.Exists(OldFileName) &&
                !File.Exists(FileName))
            {
                File.Move(OldFileName, FileName);
            }

            if (!File.Exists(FileName))
            {
                return null;
            }

            var fi = new FileInfo(FileName);
            if (fi.Length <= 0)
            {
                return null;
            }

            MigrateConfig(FileName);

            using (var sr = new StreamReader(FileName, new UTF8Encoding(false)))
            {
                if (sr.BaseStream.Length > 0)
                {
                    var xs = new XmlSerializer(typeof(Config));
                    var data = xs.Deserialize(sr) as Config;
                    if (data != null)
                    {
                        instance = data;
                    }
                }
            }

            foreach (var item in instance.globalLogFilters)
            {
                item.FormatTextDelegate = (t, _) => FormatLogMessageType(t);
            }

            instance.globalLogFilterDictionary = instance.globalLogFilters.ToDictionary(x => x.Key);

            return instance;
        }

        private static void MigrateConfig(
            string file)
        {
            var buffer = new StringBuilder();
            buffer.Append(File.ReadAllText(file, new UTF8Encoding(false)));

            buffer.Replace("NetworkTargetMarker", "NetworkSignMarker");

            File.WriteAllText(file, buffer.ToString(), new UTF8Encoding(false));
        }

        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);

        public static void Save()
        {
            if (instance == null)
            {
                return;
            }

            lock (instance)
            {
                var directoryName = Path.GetDirectoryName(FileName);

                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                var ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, string.Empty);

                var sb = new StringBuilder();
                using (var sw = new StringWriter(sb))
                {
                    var xs = new XmlSerializer(instance.GetType());
                    xs.Serialize(sw, instance, ns);
                }

                sb.Replace("utf-16", "utf-8");

                File.WriteAllText(
                    FileName,
                    sb.ToString() + Environment.NewLine,
                    DefaultEncoding);
            }
        }

        #endregion Load & Save

        #region Default Values

        private const bool SupportWin7Default = false;
        private const int WasapiLatencyDefault = 200;
        private const int WasapiMultiplePlaybackCountDefault = 4;
        private const double WasapiLoopBufferDurationDefault = 20;

        private static ObservableKeyValue<LogMessageType, bool>[] GetDefaultGlobalLogFilter()
        {
            var filters = new List<ObservableKeyValue<LogMessageType, bool>>();

            foreach (LogMessageType type in Enum.GetValues(typeof(LogMessageType)))
            {
                filters.Add(new ObservableKeyValue<LogMessageType, bool>(type, false)
                {
                    FormatTextDelegate = (t, _) => FormatLogMessageType(t),
                    Value = type switch
                    {
                        LogMessageType.CombatantHP => true,
                        LogMessageType.NetworkDoT => true,
                        LogMessageType.NetworkCancelAbility => true,
                        LogMessageType.NetworkEffectResult => true,
                        LogMessageType.NetworkUpdateHp => true,
                        LogMessageType.Settings => true,
                        LogMessageType.Process => true,
                        LogMessageType.Debug => true,
                        LogMessageType.PacketDump => true,
                        LogMessageType.Version => true,
                        LogMessageType.Error => true,
                        LogMessageType.Timer => true,
                        _ => false,
                    }
                });
            }

            return filters.ToArray();
        }

        private static string FormatLogMessageType(LogMessageType t)
            => $"0x{((int)t):X2}:{t}";

        private const float CommonSoundVolumeDefault = 0.5f;

        #endregion Default Values

        [field: NonSerialized]
        public event EventHandler UILocaleChanged;

        public void SubscribeUILocale(
            Action changedAction)
            => this.UILocaleChanged += (_, __) => changedAction?.Invoke();

        private Locales uiLocale = GetDefaultLocale();

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

                    this.UILocaleChanged?.Invoke(this, new EventArgs());

                    Task.Run(() => Save());
                }
            }
        }

        private Locales xivLocale = GetDefaultLocale();

        public Locales XIVLocale
        {
            get => this.xivLocale;
            set => this.SetProperty(ref this.xivLocale, value);
        }

        private bool isOverlaysAllLocked;

        public bool IsOverlaysAllLocked
        {
#if false
            get => this.isOverlaysAllLocked;
            set => this.SetProperty(ref this.isOverlaysAllLocked, value);
#else
            get => this.isOverlaysAllLocked;
            set => this.isOverlaysAllLocked = false;
#endif
        }

        public static Locales GetDefaultLocale()
            => CultureInfo.CurrentCulture.Name switch
            {
                "ja-JP" => Locales.JA,
                "en-US" => Locales.EN,
                "fr-FR" => Locales.FR,
                "de-DE" => Locales.DE,
                "ko-KR" => Locales.KO,
                "zh-CN" => Locales.CN,
                "zh-TW" => Locales.CN,
                _ => Locales.EN,
            };

        private bool isEnabledSharlayan = true;

        public bool IsEnabledSharlayan
        {
            get => this.isEnabledSharlayan;
            set => this.SetProperty(ref this.isEnabledSharlayan, value);
        }

        private bool isSimplifiedInCombat = false;

        public bool IsSimplifiedInCombat
        {
            get => this.isSimplifiedInCombat;
            set => this.SetProperty(ref this.isSimplifiedInCombat, value);
        }

        private bool isForceFlushSharlayanResources = true;

        public bool IsForceFlushSharlayanResources
        {
            get => this.isForceFlushSharlayanResources;
            set => this.SetProperty(ref this.isForceFlushSharlayanResources, value);
        }

        [XmlIgnore]
        public bool SupportWin7 => SupportWin7Default;

        private int wasapiLatency = WasapiLatencyDefault;

        [DefaultValue(WasapiMultiplePlaybackCountDefault)]
        public int WasapiLatency
        {
            get => this.wasapiLatency;
            set => this.SetProperty(ref this.wasapiLatency, value);
        }

        private int wasapiMultiplePlaybackCount = WasapiMultiplePlaybackCountDefault;

        [DefaultValue(WasapiMultiplePlaybackCountDefault)]
        public int WasapiMultiplePlaybackCount
        {
            get => this.wasapiMultiplePlaybackCount;
            set => this.SetProperty(ref this.wasapiMultiplePlaybackCount, value);
        }

        private TimeSpan wasapiLoopBufferDuration = TimeSpan.FromSeconds(WasapiLoopBufferDurationDefault);

        [XmlIgnore]
        public TimeSpan WasapiLoopBufferDuration => this.wasapiLoopBufferDuration;

        [XmlElement(ElementName = nameof(WasapiLoopBufferDuration))]
        [DefaultValue(WasapiLoopBufferDurationDefault)]
        public double WasapiLoopBufferDurationXML
        {
            get => this.wasapiLoopBufferDuration.TotalSeconds;
            set
            {
                if (this.wasapiLoopBufferDuration.TotalSeconds != value)
                {
                    this.wasapiLoopBufferDuration = TimeSpan.FromSeconds(value);
                    this.RaisePropertyChanged();
                    this.RaisePropertyChanged(nameof(WasapiLoopBufferDuration));
                }
            }
        }

        private float commonSoundVolume = CommonSoundVolumeDefault;

        public float CommonSoundVolume
        {
            get => this.commonSoundVolume;
            set => this.SetProperty(ref this.commonSoundVolume, value);
        }

        private Dictionary<LogMessageType, ObservableKeyValue<LogMessageType, bool>> globalLogFilterDictionary;
        private ObservableKeyValue<LogMessageType, bool>[] globalLogFilters = GetDefaultGlobalLogFilter();

        [XmlArrayItem("Filter")]
        public ObservableKeyValue<LogMessageType, bool>[] GlobalLogFilters
        {
            get => this.globalLogFilters;
            set => this.SetProperty(ref this.globalLogFilters, value);
        }

        public bool IsFilterdLog(
            LogMessageType type)
            => this.globalLogFilterDictionary.ContainsKey(type) ?
            this.globalLogFilterDictionary[type].Value :
            false;
    }
}
