using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Config;
using FFXIV.Framework.Common;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [Serializable]
    [XmlRoot(ElementName = "TimelineConfig")]
    [XmlType(TypeName = "TimelineConfig")]
    public class TimelineSettings :
        BindableBase
    {
        private static readonly object Locker = new object();

        #region Singleton

        private static TimelineSettings instance;

        public static TimelineSettings Instance =>
            instance ?? (instance = Load(FileName));

        public TimelineSettings()
        {
            if (!WPFHelper.IsDesignMode)
            {
                this.PropertyChanged += this.TimelineSettings_PropertyChanged;
            }
        }

        #endregion Singleton

        #region Data

        private bool designMode = false;

        [XmlIgnore]
        public bool DesignMode
        {
            get => this.designMode;
            set => this.SetProperty(ref this.designMode, value);
        }

        private bool hideDesignINotice = false;

        public bool HideDesignINotice
        {
            get => this.hideDesignINotice;
            set => this.SetProperty(ref this.hideDesignINotice, value);
        }

        private bool enabled = false;

        public bool Enabled
        {
            get => this.enabled;
            set => this.SetProperty(ref this.enabled, value);
        }

        private bool overlayVisible = true;

        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set => this.SetProperty(ref this.overlayVisible, value);
        }

        private double left = 10;

        public double Left
        {
            get => this.left;
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.left, Math.Round(value));
                }
            }
        }

        private double top = 10;

        public double Top
        {
            get => this.top;
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.top, Math.Round(value));
                }
            }
        }

        private double width = 300;

        public double Width
        {
            get => this.width;
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.width, Math.Round(value));
                }
            }
        }

        private double height = 400;

        public double Height
        {
            get => this.height;
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.height, Math.Round(value));
                }
            }
        }

        private double noticeLeft = 10;

        public double NoticeLeft
        {
            get => this.noticeLeft;
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.noticeLeft, Math.Round(value));
                }
            }
        }

        private double noticeTop = 10;

        public double NoticeTop
        {
            get => this.noticeTop;
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.noticeTop, Math.Round(value));
                }
            }
        }

        private double noticeWidth = 300;

        public double NoticeWidth
        {
            get => this.noticeWidth;
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.noticeWidth, Math.Round(value));
                }
            }
        }

        private double noticeHeight = 250;

        public double NoticeHeight
        {
            get => this.noticeHeight;
            set
            {
                if (!FFXIV.Framework.Config.Instance.IsOverlaysAllLocked)
                {
                    this.SetProperty(ref this.noticeHeight, Math.Round(value));
                }
            }
        }

        private bool isTimelineLiveUpdate = false;

        public bool IsTimelineLiveUpdate
        {
            get => this.isTimelineLiveUpdate;
            set => this.SetProperty(ref this.isTimelineLiveUpdate, value);
        }

        private double notifyInterval = 20;

        public double NotifyInterval
        {
            get => this.notifyInterval;
            set => this.SetProperty(ref this.notifyInterval, value);
        }

        private double progressBarRefreshInterval = 50;

        public double ProgressBarRefreshInterval
        {
            get => this.progressBarRefreshInterval;
            set => this.SetProperty(ref this.progressBarRefreshInterval, value);
        }

        private double timelineRefreshInterval = 200;

        public double TimelineRefreshInterval
        {
            get => this.timelineRefreshInterval;
            set => this.SetProperty(ref this.timelineRefreshInterval, value);
        }

        private double psyncDetectInterval = 100;

        public double PSyncDetectInterval
        {
            get => this.psyncDetectInterval;
            set => this.SetProperty(ref this.psyncDetectInterval, value);
        }

        private double residentScriptInterval = 20;

        public double ResidentScriptInterval
        {
            get => this.residentScriptInterval;
            set => this.SetProperty(ref this.residentScriptInterval, value);
        }

        private ThreadPriority notifyThreadPriority = ThreadPriority.Normal;

        public ThreadPriority NotifyThreadPriority
        {
            get => this.notifyThreadPriority;
            set => this.SetProperty(ref this.notifyThreadPriority, value);
        }

        private DispatcherPriority timelineThreadPriority = DispatcherPriority.Normal;

        public DispatcherPriority TimelineThreadPriority
        {
            get => this.timelineThreadPriority;
            set => this.SetProperty(ref this.timelineThreadPriority, value);
        }

        private bool isMute = false;

        public bool IsMute
        {
            get => this.isMute;
            set => this.SetProperty(ref this.isMute, value);
        }

        private bool clickthrough = false;

        public bool Clickthrough
        {
            get => this.clickthrough;
            set => this.SetProperty(ref this.clickthrough, value);
        }

        private double overlayOpacity = 0.95;

        public double OverlayOpacity
        {
            get => this.overlayOpacity;
            set => this.SetProperty(ref this.overlayOpacity, value);
        }

        private double overlayScale = 1.0;

        public double OverlayScale
        {
            get => this.overlayScale;
            set => this.SetProperty(ref this.overlayScale, value);
        }

        private double nearestActivityScale = 1.2;

        public double NearestActivityScale
        {
            get => this.nearestActivityScale;
            set => this.SetProperty(ref this.nearestActivityScale, value);
        }

        private double nextActivityBrightness = 0.7;

        public double NextActivityBrightness
        {
            get => this.nextActivityBrightness;
            set => this.SetProperty(ref this.nextActivityBrightness, value);
        }

        private int showActivitiesCount = 8;

        public int ShowActivitiesCount
        {
            get => this.showActivitiesCount;
            set => this.SetProperty(ref this.showActivitiesCount, value);
        }

        private double showActivitiesTime = 120;

        public double ShowActivitiesTime
        {
            get => this.showActivitiesTime;
            set => this.SetProperty(ref this.showActivitiesTime, value);
        }

        private double showProgressBarTime = 15;

        public double ShowProgressBarTime
        {
            get => this.showProgressBarTime;
            set => this.SetProperty(ref this.showProgressBarTime, value);
        }

        private bool indicatorVisible = true;

        public bool IndicatorVisible
        {
            get => this.indicatorVisible;
            set => this.SetProperty(ref this.indicatorVisible, value);
        }

        private string indicatorStyle = "Default";

        public string IndicatorStyle
        {
            get => this.indicatorStyle;
            set
            {
                if (this.SetProperty(ref this.indicatorStyle, value))
                {
                    this.RaisePropertyChanged(nameof(this.IndicatorStyleModel));
                }
            }
        }

        [XmlIgnore]
        public TimelineStyle IndicatorStyleModel
        {
            get
            {
                var style = this.DefaultStyle;

                if (!string.IsNullOrEmpty(this.IndicatorStyle))
                {
                    var s = this.Styles.FirstOrDefault(x =>
                        string.Equals(x.Name, this.IndicatorStyle, StringComparison.OrdinalIgnoreCase));
                    if (s != null)
                    {
                        style = s;
                    }
                }

                return style;
            }
        }

        private ObservableCollection<TimelineStyle> styles = new ObservableCollection<TimelineStyle>();

        private string activityMarginString = "0 15 0 0";

        [XmlElement(ElementName = "ActivityMargin")]
        public string ActivityMarginString
        {
            get => this.activityMarginString;
            set
            {
                if (this.SetProperty(ref this.activityMarginString, value))
                {
                    this.RaisePropertyChanged(nameof(this.ActivityMargin));
                }
            }
        }

        private static readonly ThicknessConverter thicknessConverter = new ThicknessConverter();

        [XmlIgnore]
        public Thickness ActivityMargin =>
            (Thickness)thicknessConverter.ConvertFromString(this.ActivityMarginString);

        private string overlayBackgroundString = "#F0000000";

        [XmlElement(ElementName = "OverlayBackground")]
        public string OverlayBackgroundString
        {
            get => this.overlayBackgroundString;
            set
            {
                if (this.SetProperty(ref this.overlayBackgroundString, value))
                {
                    this.RaisePropertyChanged(nameof(this.OverlayBackground));
                    this.RaisePropertyChanged(nameof(this.OverlayBackgroundColor));
                }
            }
        }

        private static readonly BrushConverter brushConverter = new BrushConverter();

        [XmlIgnore]
        public Brush OverlayBackground
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(this.OverlayBackgroundString))
                    {
                        return null;
                    }

                    if (File.Exists(this.OverlayBackgroundString))
                    {
                        var img = new BitmapImage();

                        img.BeginInit();
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.CreateOptions = BitmapCreateOptions.None;
                        img.UriSource = new Uri(this.OverlayBackgroundString);
                        img.EndInit();
                        img.Freeze();

                        var brush = new ImageBrush(img);
                        brush.Stretch = Stretch.Uniform;
                        RenderOptions.SetEdgeMode(brush, EdgeMode.Aliased);
                        RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.NearestNeighbor);

                        return brush;
                    }

                    return (Brush)brushConverter.ConvertFromString(this.OverlayBackgroundString);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        [XmlIgnore]
        public Color OverlayBackgroundColor => (this.OverlayBackground as SolidColorBrush)?.Color ?? Colors.Black;

        public ObservableCollection<TimelineStyle> Styles
        {
            get => this.styles;
            set
            {
                this.styles.Clear();
                this.styles.AddRange(value);
                this.RaisePropertyChanged(nameof(this.DefaultStyle));
            }
        }

        [XmlIgnore]
        public TimelineStyle DefaultStyle =>
            this.Styles.FirstOrDefault(x => x.IsDefault) ?? TimelineStyle.SuperDefaultStyle;

        public TimelineStyle DefaultNoticeStyle =>
            this.Styles.FirstOrDefault(x => x.IsDefaultNotice) ?? this.DefaultStyle;

        private List<KeyValue<string, bool>> timelineFiles = new List<KeyValue<string, bool>>(64);

        [XmlArray("TimelineFiles")]
        [XmlArrayItem("File")]
        public List<KeyValue<string, bool>> TimelineFiles
        {
            get => this.timelineFiles;
            set
            {
                this.timelineFiles.Clear();
                this.timelineFiles.AddRange(value);
            }
        }

        #endregion Data

        #region Methods

        public static string TimelineDirectory
        {
            get
            {
                var dir = Settings.Default.TimelineDirectory;

                if (string.IsNullOrEmpty(dir) ||
                    !Directory.Exists(dir))
                {
                    dir = DirectoryHelper.FindSubDirectory(@"resources\timeline");
                }

                return dir;
            }
        }

        public static readonly string FileName = Path.Combine(
            TimelineDirectory,
            @"Timeline.config");

        public static readonly string MasterFile = Path.Combine(
            Path.Combine(TimelineDirectory, "sample"),
            @"Timeline.master.config");

        public static void Load() => instance = Load(FileName);

        public static void Save() => instance?.Save(FileName);

        public static TimelineSettings Load(
            string file)
        {
            var data = default(TimelineSettings);

            lock (Locker)
            {
                try
                {
                    autoSave = false;

                    // サイズ0のファイルがもしも存在したら消す
                    if (File.Exists(file))
                    {
                        var fi = new FileInfo(file);
                        if (fi.Length <= 0)
                        {
                            File.Delete(file);
                        }
                    }

                    if (!File.Exists(file))
                    {
                        if (File.Exists(MasterFile))
                        {
                            File.Copy(MasterFile, file);
                        }
                        else
                        {
                            data = new TimelineSettings();
                            return data;
                        }
                    }

                    using (var sr = new StreamReader(file, new UTF8Encoding(false)))
                    {
                        if (sr.BaseStream.Length > 0)
                        {
                            var xs = new XmlSerializer(typeof(TimelineSettings));
                            data = xs.Deserialize(sr) as TimelineSettings;
                        }
                    }
                }
                finally
                {
                    if (data != null &&
                        !data.Styles.Any())
                    {
                        var style = TimelineStyle.SuperDefaultStyle;
                        style.IsDefault = true;
                        style.IsDefaultNotice = true;
                        data.Styles.Add(style);
                    }

                    autoSave = true;
                }
            }

            return data;
        }

        private volatile bool isSaving;

        public void Save(
            string file)
        {
            if (this.isSaving)
            {
                return;
            }

            try
            {
                this.isSaving = true;
            }
            finally
            {
                lock (Locker)
                {
                    FileHelper.CreateDirectory(file);

                    var ns = new XmlSerializerNamespaces();
                    ns.Add(string.Empty, string.Empty);

                    var buffer = new StringBuilder();
                    using (var sw = new StringWriter(buffer))
                    {
                        var xs = new XmlSerializer(this.GetType());
                        xs.Serialize(sw, this, ns);
                    }

                    buffer.Replace("utf-16", "utf-8");

                    using (var sw = new StreamWriter(file, false, new UTF8Encoding(false)))
                    {
                        sw.Write(buffer.ToString() + Environment.NewLine);
                        sw.Flush();
                    }
                }

                this.isSaving = false;
            }
        }

        private static bool autoSave = false;

        private void TimelineSettings_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (autoSave)
            {
                TimelineSettings.Save();
            }
        }

        #endregion Methods
    }
}
