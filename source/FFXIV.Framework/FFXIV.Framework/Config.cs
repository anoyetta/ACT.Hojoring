using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
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

        #endregion Singleton

        #region Load & Save

        public static string FileName => Assembly.GetExecutingAssembly().Location.Replace(".dll", ".config");

        public static Config Load()
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

                File.WriteAllText(
                    FileName,
                    sb.ToString(),
                    new UTF8Encoding(false));
            }
        }

        #endregion Load & Save

        #region Default Values

        private const bool SupportWin7Default = false;
        private const int WasapiLatencyDefault = 200;
        private const int WasapiMultiplePlaybackCountDefault = 4;
        private const double WasapiLoopBufferDurationDefault = 20;

        #endregion Default Values

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
