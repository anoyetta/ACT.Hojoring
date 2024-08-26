using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;
using System.Xml.Serialization;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.WPF.Views;
using FFXIV.Framework.XIVHelper;
using static FFXIV.Framework.XIVHelper.LogMessageTypeExtensions;
using Prism.Mvvm;

namespace FFXIV.Framework
{
    [Serializable]
    [XmlType(TypeName = "FFXIV.Framework")]
    public class Config :
        BindableBase
    {
        public static readonly object ConfigBlocker = new object();

        #region Singleton

        private static volatile Config instance;

        public static Config Instance =>
            instance ?? (instance = (Load() ?? new Config()));

        private Config()
        {
        }

        #endregion Singleton

        #region Load & Save

        public static string FileName => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "anoyetta",
            "ACT",
            Path.GetFileName(Assembly.GetExecutingAssembly().Location.Replace(".dll", ".config")));

        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
        private static bool bSaved = false;

        public static Config Load()
        {
            lock (ConfigBlocker)
            {
                if (!File.Exists(FileName))
                {
                    return null;
                }

                var fi = new FileInfo(FileName);
                if (fi.Length <= 0)
                {
                    return null;
                }

                try
                {
                    MigrateConfig(FileName);

                    using (var sr = new StreamReader(FileName, DefaultEncoding))
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

                    if (instance.RegexCacheSize != RegexCacheSizeDefault)
                    {
                        if (instance.RegexCacheSize < RegexCacheSizeDefault)
                        {
                            instance.RegexCacheSize = instance.RegexCacheSize <= 0 ?
                                RegexCacheSizeDefaultOverride :
                                RegexCacheSizeDefault;
                        }

                        instance.ApplyRegexCacheSize();
                    }

                    return instance;
                }
                catch(Exception ex)
                {
                    var info = ex.GetType().ToString() + Environment.NewLine + Environment.NewLine;
                    info += ex.Message + Environment.NewLine;
                    info += ex.StackTrace.ToString();

                    if (ex.InnerException != null)
                    {
                        info += Environment.NewLine + Environment.NewLine;
                        info += "Inner Exception :" + Environment.NewLine;
                        info += ex.InnerException.GetType().ToString() + Environment.NewLine + Environment.NewLine;
                        info += ex.InnerException.Message + Environment.NewLine;
                        info += ex.InnerException.StackTrace.ToString();
                    }

                    var result = MessageBox.Show("faild config load\n\n"+ FileName +"\n" + info + "\n\ntry to load backup?", "error!", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        if (EnvironmentHelper.RestoreFile(FileName))
                        {
                            return Load();
                        }
                    }
                    return null;
                }
            }
        }

        private static void MigrateConfig(
            string file)
        {
            var buffer = new StringBuilder();
            buffer.Append(File.ReadAllText(file, new UTF8Encoding(false)));

            var xdoc = XDocument.Parse(buffer.ToString());
            var xelements = xdoc.Root.Elements();

            if (xelements.Any(x => x.Name == nameof(GlobalLogFilters)))
            {
                var filtersParent = xelements
                    .FirstOrDefault(x => x.Name == nameof(GlobalLogFilters));

                var filters = filtersParent.Elements();

                var typeNames = LogMessageTypeExtensions.GetNames();

                // 消滅したLogTypeを削除する
                var toRemove = filters
                    .Where(x => !typeNames.Contains(x.Attribute("Key").Value))
                    .ToArray();

                foreach (var e in toRemove)
                {
                    e.Remove();
                }

                // 新しく増えたLogTypeを追加する
                var toAdd = typeNames
                    .Where(x => !filters.Any(y => y.Attribute("Key").Value == x));

                foreach (var key in toAdd)
                {
                    var e = new XElement("Filter");
                    e.SetAttributeValue("Key", key);
                    e.SetAttributeValue("Value", false);
                    filtersParent.Add(e);
                }
            }

            File.WriteAllText(file, xdoc.ToString(), DefaultEncoding);
        }

        private static volatile bool isSaving;

        public static void Save(bool bForce = false)
        {
            if (bSaved)
            {
                if (!bForce)
                {
                    return;
                }
            }
            if (isSaving)
            {
                return;
            }

            isSaving = true;

            try
            {
                lock (ConfigBlocker)
                {
                    if (instance == null)
                    {
                        return;
                    }

                    var directoryName = Path.GetDirectoryName(FileName);

                    if (!Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    var ns = new XmlSerializerNamespaces();
                    ns.Add(string.Empty, string.Empty);

                    using (var sw = new StreamWriter(FileName, false, DefaultEncoding))
                    {
                        var xs = new XmlSerializer(instance.GetType());
                        xs.Serialize(sw, instance, ns);
                        sw.Close();
                    }
                    bSaved = true;
                }
            }
            finally
            {
                isSaving = false;
            }
        }

        #endregion Load & Save

        #region Default Values

        private const bool SupportWin7Default = false;
        private const int WasapiLatencyDefault = 200;
        private const int WasapiMultiplePlaybackCountDefault = 4;
        private const double WasapiLoopBufferDurationDefault = 20;
        private const int RegexCacheSizeDefault = 15;
        private const int RegexCacheSizeDefaultOverride = 128;

        private bool bAllowHyphen = false;

        public bool AllowHyphen
        {
            get { return bAllowHyphen; }
            set { bAllowHyphen = value; }
        }

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
                        LogMessageType.DoTHoT => true,
                        LogMessageType.CancelAction => true,
                        LogMessageType.EffectResult => true,
                        LogMessageType.UpdateHp => true,
                        LogMessageType.Settings => true,
                        LogMessageType.Process => true,
                        LogMessageType.Debug => true,
                        LogMessageType.PacketDump => true,
                        LogMessageType.Version => true,
                        LogMessageType.Error => true,
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

                    Save(true);
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

        private bool isEnabledCompatibleLogFormat = true;

        public bool IsEnabledCompatibleLogFormat
        {
            get => this.isEnabledCompatibleLogFormat;
            set => this.SetProperty(ref this.isEnabledCompatibleLogFormat, value);
        }

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

        private bool isEnabledOutputDebugLog;

        public bool IsEnabledOutputDebugLog
        {
            get => this.isEnabledOutputDebugLog;
            set => this.SetProperty(ref this.isEnabledOutputDebugLog, value);
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

        private int regexCacheSize = RegexCacheSizeDefaultOverride;

        [DefaultValue(RegexCacheSizeDefault)]
        public int RegexCacheSize
        {
            get => this.regexCacheSize;
            set
            {
                if (this.SetProperty(ref this.regexCacheSize, value))
                {
                    this.ApplyRegexCacheSize();
                }
            }
        }

        private void ApplyRegexCacheSize()
        {
            Regex.CacheSize = this.regexCacheSize;
        }

        private Dictionary<LogMessageType, ObservableKeyValue<LogMessageType, bool>> globalLogFilterDictionary;
        private ObservableKeyValue<LogMessageType, bool>[] globalLogFilters = GetDefaultGlobalLogFilter();

        [XmlArrayItem("Filter")]
        public ObservableKeyValue<LogMessageType, bool>[] GlobalLogFilters
        {
            get => this.globalLogFilters.OrderBy(x => x.Key).ToArray();
            set => this.SetProperty(ref this.globalLogFilters, value);
        }

        public bool IsFilterdLog(
            LogMessageType type)
        {
            if (this.globalLogFilterDictionary == null)
            {
                return false;
            }

            return this.globalLogFilterDictionary.ContainsKey(type) ?
                this.globalLogFilterDictionary[type].Value :
                false;
        }
    }
}
