using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
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

            return instance;
        }

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

                using (var sw = new StreamWriter(FileName, false, new UTF8Encoding(false)))
                {
                    sw.Write(sb.ToString() + Environment.NewLine);
                    sw.Flush();
                }
            }
        }

        #endregion Load & Save

        #region Default Values

        private const bool SupportWin7Default = false;
        private const int WasapiLatencyDefault = 200;
        private const int WasapiMultiplePlaybackCountDefault = 4;
        private const double WasapiLoopBufferDurationDefault = 20;

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
    }
}
