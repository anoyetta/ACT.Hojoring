using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Serialization;
using FFXIV.Framework.Common;
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
            instance ??= (Load() ?? new Config());

        private volatile bool isAutoSaving;

        private Config()
        {
            this.PropertyChanged += async (_, __) =>
            {
                if (this.isAutoSaving)
                {
                    return;
                }

                this.isAutoSaving = true;

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    await Task.Run(() => Save());
                }
                finally
                {
                    this.isAutoSaving = false;
                }
            };
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
                            if (xs.Deserialize(sr) is Config data)
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

        private const double WriteIntervalDefault = 30;
        private const double FlushIntervalDefault = 600;
        private const bool IsReplacePCNameDefault = false;

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

        private bool isEnabledRecording;

        [DefaultValue(false)]
        public bool IsEnabledRecording
        {
            get => this.isEnabledRecording;
            set => this.SetProperty(ref this.isEnabledRecording, value);
        }

        private double stopRecordingAfterCombatMinutes;

        [DefaultValue(0d)]
        public double StopRecordingAfterCombatMinutes
        {
            get => this.stopRecordingAfterCombatMinutes;
            set => this.SetProperty(ref this.stopRecordingAfterCombatMinutes, value);
        }

        private double stopRecordingSubscribeInterval;

        [DefaultValue(10d)]
        public double StopRecordingSubscribeInterval
        {
            get => this.stopRecordingSubscribeInterval;
            set => this.SetProperty(ref this.stopRecordingSubscribeInterval, value);
        }

        private bool isShowTitleCard;

        [DefaultValue(false)]
        public bool IsShowTitleCard
        {
            get => this.isShowTitleCard;
            set => this.SetProperty(ref this.isShowTitleCard, value);
        }

        private double titleCardLeft;

        public double TitleCardLeft
        {
            get => this.titleCardLeft;
            set => this.SetProperty(ref this.titleCardLeft, value);
        }

        private double titleCardTop;

        public double TitleCardTop
        {
            get => this.titleCardTop;
            set => this.SetProperty(ref this.titleCardTop, value);
        }

        private double scale = 1.0d;

        public double Scale
        {
            get => this.scale;
            set => this.SetProperty(ref this.scale, value);
        }

        private bool isAlwaysShow;

        public bool IsAlwaysShow
        {
            get => this.isAlwaysShow;
            set
            {
                if (this.SetProperty(ref this.isAlwaysShow, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsNotAlwaysShow));
                }
            }
        }

        [XmlIgnore]
        public bool IsNotAlwaysShow => !this.IsAlwaysShow;

        private TitleCardView titleCardPreview;
        private bool isPreviewTitleCard;

        [XmlIgnore]
        public bool IsPreviewTitleCard
        {
            get => this.isPreviewTitleCard;
            set
            {
                if (this.SetProperty(ref this.isPreviewTitleCard, value))
                {
                    if (this.isPreviewTitleCard)
                    {
                        this.titleCardPreview?.Close();
                        this.titleCardPreview = new TitleCardView();
                        this.titleCardPreview.Show();
                    }
                    else
                    {
                        this.titleCardPreview?.Close();
                    }
                }
            }
        }

        private string videoSaveDictory;

        [DefaultValue("")]
        public string VideoSaveDictory
        {
            get => this.videoSaveDictory;
            set => this.SetProperty(ref this.videoSaveDictory, value);
        }

        private KeyShortcut startRecordingShortcut = new KeyShortcut()
        {
            IsWin = true,
            IsAlt = true,
            Key = Key.R,
        };

        public KeyShortcut StartRecordingShortcut
        {
            get => this.startRecordingShortcut;
            set => this.SetProperty(ref this.startRecordingShortcut, value);
        }

        private KeyShortcut stopRecordingShortcut = new KeyShortcut()
        {
            IsWin = true,
            IsAlt = true,
            Key = Key.R,
        };

        public KeyShortcut StopRecordingShortcut
        {
            get => this.stopRecordingShortcut;
            set => this.SetProperty(ref this.stopRecordingShortcut, value);
        }

        private bool useObsRpc;

        public bool UseObsRpc
        {
            get => this.useObsRpc;
            set
            {
                if (this.SetProperty(ref this.useObsRpc, value))
                {
                    this.RaisePropertyChanged(nameof(this.NotUseObsRpc));
                }
            }
        }

        [XmlIgnore]
        public bool NotUseObsRpc => !this.UseObsRpc;

        private string videFilePrefix = "FFXIV";

        public string VideFilePrefix
        {
            get => this.videFilePrefix;
            set => this.SetProperty(ref this.videFilePrefix, value);
        }

        private bool isRecording;

        [XmlIgnore]
        public bool IsRecording
        {
            get => this.isRecording;
            set => this.SetProperty(ref this.isRecording, value);
        }

        private int videoTryCount;

        public int VideoTryCount
        {
            get => this.videoTryCount;
            set => this.SetProperty(ref this.videoTryCount, value);
        }

        private DateTime tryCountTimestamp;

        public DateTime TryCountTimestamp
        {
            get => this.tryCountTimestamp;
            set => this.SetProperty(ref this.tryCountTimestamp, value);
        }

        private string tryCountContentName;

        public string TryCountContentName
        {
            get => this.tryCountContentName;
            set => this.SetProperty(ref this.tryCountContentName, value);
        }

        private int tryCountResetInterval = 5;

        public int TryCountResetInterval
        {
            get => this.tryCountResetInterval;
            set => this.SetProperty(ref this.tryCountResetInterval, value);
        }
    }
}
