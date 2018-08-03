using System;
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

        #endregion Load & Save

        private bool supportWin7 = false;

        public bool SupportWin7
        {
            get => this.supportWin7;
            set => this.SetProperty(ref this.supportWin7, value);
        }

        private int wasapiMultiplePlaybackCount = 4;

        public int WasapiMultiplePlaybackCount
        {
            get => this.wasapiMultiplePlaybackCount;
            set => this.SetProperty(ref this.wasapiMultiplePlaybackCount, value);
        }

        private TimeSpan wasapiLoopBufferDuration = TimeSpan.FromSeconds(60);

        public TimeSpan WasapiLoopBufferDuration
        {
            get => this.wasapiLoopBufferDuration;
            set => this.SetProperty(ref this.wasapiLoopBufferDuration, value);
        }
    }
}
