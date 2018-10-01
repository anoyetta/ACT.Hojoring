using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace ACT.XIVLog
{
    [Serializable]
    [XmlType(TypeName = "ACT.XIVLog")]
    public class Config :
        BindableBase
    {
        #region Singleton

        private static Config instance;

        public static Config Instance =>
            instance ?? (instance = (Load() ?? new Config()));

        private Config()
        {
            this.PropertyChanged +=
                async (x, y) => await Task.Run(() => Save());
        }

        #endregion Singleton

        #region Load & Save

        private static readonly object Locker = new object();
        private volatile static bool isInitializing = false;

        public static string FileName =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "anoyetta\\ACT\\ACT.XIVLog.config");

        public static Config Load()
        {
            lock (Locker)
            {
                try
                {
                    isInitializing = true;

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
                finally
                {
                    isInitializing = false;
                }
            }
        }

        public static void Save()
        {
            lock (Locker)
            {
                if (isInitializing)
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

        private const double WriteIntervalDefault = 5;
        private const double FlushIntervalDefault = 180;
        private const bool IsReplacePCNameDefault = true;

        #endregion Default Values

        private string outputDirectory = string.Empty;

        [DefaultValue("")]
        public string OutputDirectory
        {
            get => this.outputDirectory;
            set => this.SetProperty(ref this.outputDirectory, value);
        }

        private double writeInterval = WriteIntervalDefault;

        [DefaultValue(WriteIntervalDefault)]
        public double WriteInterval
        {
            get => this.writeInterval;
            set => this.SetProperty(ref this.writeInterval, value);
        }

        private double flushInterval = FlushIntervalDefault;

        [DefaultValue(FlushIntervalDefault)]
        public double FlushInterval
        {
            get => this.flushInterval;
            set => this.SetProperty(ref this.flushInterval, value);
        }

        private bool isReplacePCName = IsReplacePCNameDefault;

        [DefaultValue(IsReplacePCNameDefault)]
        public bool IsReplacePCName
        {
            get => this.isReplacePCName;
            set => this.SetProperty(ref this.isReplacePCName, value);
        }
    }
}
