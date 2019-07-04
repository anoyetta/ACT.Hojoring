using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace FFXIV.Framework.TTS.Server
{
    public class Config : BindableBase
    {
        #region Lazy Instance

        private static readonly Lazy<Config> LazyInstance = new Lazy<Config>(() => Load());

        public static Config Instance => LazyInstance.Value;

        private Config()
        {
        }

        #endregion Lazy Instance

        public static readonly string FileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "anoyetta",
            "ACT",
            $"TTServer.config");

        public static Config Load() => Load(FileName);

        public static Config Load(
            string fileName)
        {
            if (!File.Exists(fileName))
            {
                return new Config();
            }

            var fi = new FileInfo(fileName);
            if (fi.Length <= 0)
            {
                return new Config();
            }

            using (var sr = new StreamReader(fileName, new UTF8Encoding(false)))
            {
                if (sr.BaseStream.Length > 0)
                {
                    var xs = new XmlSerializer(typeof(Config));
                    var data = xs.Deserialize(sr) as Config;
                    if (data != null)
                    {
                        return data;
                    }
                }
            }

            return new Config();
        }

        public void Save() => this.Save(FileName);

        public void Save(
            string fileName)
        {
            lock (this)
            {
                var directoryName = Path.GetDirectoryName(fileName);

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
                    fileName,
                    buffer.ToString() + Environment.NewLine,
                    new UTF8Encoding(false));
            }
        }

        public void StartAutoSave()
        {
            this.PropertyChanged += async (_, __) => await Task.Run(() => this.Save());
        }

        private int boyomiServerPortNo = 50002;

        public int BoyomiServerPortNo
        {
            get => this.boyomiServerPortNo;
            set => this.SetProperty(ref this.boyomiServerPortNo, value);
        }

        private bool isBoyomiServerAutoStart;

        public bool IsBoyomiServerAutoStart
        {
            get => this.isBoyomiServerAutoStart;
            set => this.SetProperty(ref this.isBoyomiServerAutoStart, value);
        }
    }
}
