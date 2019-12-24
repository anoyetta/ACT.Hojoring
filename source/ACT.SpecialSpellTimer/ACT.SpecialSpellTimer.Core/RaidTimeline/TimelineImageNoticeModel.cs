using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.RaidTimeline.Views;
using FFXIV.Framework.Common;
using Prism.Commands;
using static ACT.SpecialSpellTimer.Models.TableCompiler;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [XmlType(TypeName = "i-notice")]
    [Serializable]
    public class TimelineImageNoticeModel :
        TimelineBase
    {
        #region TimelineBase

        public override TimelineElementTypes TimelineType => TimelineElementTypes.ImageNotice;

        public override IList<TimelineBase> Children => null;

        #endregion TimelineBase

        private static readonly List<TimelineImageNoticeModel> INoticeList = new List<TimelineImageNoticeModel>();

        public TimelineImageNoticeModel()
        {
            this.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(this.Left):
                    case nameof(this.Top):
                    case nameof(this.Scale):
                    case nameof(this.Duration):
                        this.RaisePropertyChanged(nameof(this.Tag));
                        break;
                }

                switch (e.PropertyName)
                {
                    case nameof(this.Left):
                    case nameof(this.Top):
                        this.RaisePropertyChanged(nameof(this.StartupLocation));
                        break;
                }
            };
        }

        public static void Collect()
        {
            lock (INoticeList)
            {
                foreach (var i in INoticeList)
                {
                    i.CloseNotice();
                }

                INoticeList.Clear();
            }
        }

        #region Left

        private double? left = null;

        [XmlIgnore]
        public double? Left
        {
            get => this.left;
            set => this.SetProperty(ref this.left, value);
        }

        [XmlAttribute(AttributeName = "left")]
        public string LeftXML
        {
            get => this.Left?.ToString();
            set => this.Left = double.TryParse(value, out var v) ? v : (double?)null;
        }

        #endregion Left

        #region Top

        private double? top = null;

        [XmlIgnore]
        public double? Top
        {
            get => this.top;
            set => this.SetProperty(ref this.top, value);
        }

        [XmlAttribute(AttributeName = "top")]
        public string TopXML
        {
            get => this.Top?.ToString();
            set => this.Top = double.TryParse(value, out var v) ? v : (double?)null;
        }

        #endregion Top

        #region Scale

        private double? scale = null;

        [XmlIgnore]
        public double? Scale
        {
            get => this.scale;
            set => this.SetProperty(ref this.scale, value);
        }

        [XmlAttribute(AttributeName = "scale")]
        public string ScaleXML
        {
            get => this.Scale?.ToString();
            set => this.Scale = double.TryParse(value, out var v) ? v : (double?)null;
        }

        #endregion Scale

        #region Duration

        private double? duration = null;

        [XmlIgnore]
        public double? Duration
        {
            get => this.duration;
            set => this.SetProperty(ref this.duration, value);
        }

        [XmlAttribute(AttributeName = "duration")]
        public string DurationXML
        {
            get => this.Duration?.ToString();
            set => this.Duration = double.TryParse(value, out var v) ? v : (double?)null;
        }

        #endregion Duration

        #region Delay

        private double? delay = null;

        [XmlIgnore]
        public double? Delay
        {
            get => this.delay;
            set => this.SetProperty(ref this.delay, value);
        }

        [XmlAttribute(AttributeName = "delay")]
        public string DelayXML
        {
            get => this.Delay?.ToString();
            set => this.Delay = double.TryParse(value, out var v) ? v : (double?)null;
        }

        #endregion Delay

        #region sync-to-hide

        private static readonly List<TimelineImageNoticeModel> syncToHideList = new List<TimelineImageNoticeModel>();

        public static TimelineImageNoticeModel[] GetSyncToHideList()
        {
            lock (syncToHideList)
            {
                return syncToHideList.ToArray();
            }
        }

        public static void ClearSyncToHideList()
        {
            lock (syncToHideList)
            {
                syncToHideList.Clear();
            }
        }

        public void SetSyncToHide(
            IEnumerable<PlaceholderContainer> placeholders = null)
        {
            if (string.IsNullOrEmpty(this.SyncToHideKeyword))
            {
                this.SyncToHideKeywordReplaced = null;
                this.SynqToHideRegex = null;

                lock (syncToHideList)
                {
                    syncToHideList.Remove(this);
                }

                return;
            }

            if (this.Parent is ISynchronizable syn &&
                syn.SyncMatch != null &&
                syn.SyncMatch.Success)
            {
                var replaced = this.SyncToHideKeyword;
                replaced = TimelineManager.Instance.ReplacePlaceholder(replaced, placeholders);
                replaced = syn.SyncMatch.Result(replaced);

                if (this.SynqToHideRegex == null ||
                    this.SyncToHideKeywordReplaced != replaced)
                {
                    this.SyncToHideKeywordReplaced = replaced;
                    this.SynqToHideRegex = new Regex(
                        replaced,
                        RegexOptions.Compiled |
                        RegexOptions.IgnoreCase);
                }
            }
        }

        public void AddSyncToHide()
        {
            lock (syncToHideList)
            {
                syncToHideList.Remove(this);

                if (this.SynqToHideRegex != null)
                {
                    syncToHideList.Add(this);
                }
            }
        }

        public void RemoveSyncToHide()
        {
            lock (syncToHideList)
            {
                syncToHideList.Remove(this);
            }
        }

        public void ClearToHide()
        {
            this.SyncToHideKeywordReplaced = null;
            this.SynqToHideRegex = null;
        }

        public bool TryHide(
            string logLine)
        {
            if (this.SynqToHideRegex == null)
            {
                return false;
            }

            if (this.toHide)
            {
                return true;
            }

            var match = this.SynqToHideRegex.Match(logLine);
            if (!match.Success)
            {
                return false;
            }

            this.toHide = true;
            return true;
        }

        private string syncToHideKeyword = null;

        [XmlAttribute(AttributeName = "sync-to-hide")]
        public string SyncToHideKeyword
        {
            get => this.syncToHideKeyword;
            set
            {
                if (this.SetProperty(ref this.syncToHideKeyword, value))
                {
                    this.syncToHideKeywordReplaced = null;
                    this.syncToHideRegex = null;
                }
            }
        }

        private string syncToHideKeywordReplaced = null;

        [XmlIgnore]
        public string SyncToHideKeywordReplaced
        {
            get => this.syncToHideKeywordReplaced;
            private set => this.SetProperty(ref this.syncToHideKeywordReplaced, value);
        }

        private Regex syncToHideRegex = null;

        [XmlIgnore]
        public Regex SynqToHideRegex
        {
            get => this.syncToHideRegex;
            private set => this.SetProperty(ref this.syncToHideRegex, value);
        }

        #endregion sync-to-hide

        [XmlIgnore]
        public string Tag =>
            $@"<i-notice" + Environment.NewLine +
            $@"  image=""{this.Image}""" + Environment.NewLine +
            $@"  scale=""{this.Scale}""" + Environment.NewLine +
            $@"  left=""{this.Left}""" + Environment.NewLine +
            $@"  top=""{this.Top}""" + Environment.NewLine +
            $@"  duration=""{this.Duration}""" + " />";

        [XmlIgnore]
        public WindowStartupLocation StartupLocation
            => this.Left == -1 && this.Top == -1 ?
                WindowStartupLocation.CenterScreen :
                WindowStartupLocation.Manual;

        private DateTime timestamp = DateTime.MinValue;

        [XmlIgnore]
        public DateTime Timestamp
        {
            get => this.timestamp;
            set => this.SetProperty(ref this.timestamp, value);
        }

        [XmlIgnore]
        public DateTime TimeToHide
            => this.Timestamp.AddSeconds(this.Duration.GetValueOrDefault());

        private TimelineImageNoticeOverlay overlay;
        private volatile bool toHide = false;

        public void StanbyNotice()
        {
            lock (INoticeList)
            {
                INoticeList.Add(this);
            }

            lock (this)
            {
                if (this.overlay != null)
                {
                    this.overlay.Close();
                    this.overlay = null;
                }

                this.overlay = new TimelineImageNoticeOverlay()
                {
                    Model = this,
                    DummyMode = false,
                };

                this.overlay.WindowStartupLocation = this.StartupLocation;
                this.overlay.Show();
            }
        }

        public void StartNotice()
        {
            TimelineVisualNoticeModel.EnqueueToHide(
                this,
                (sender, model) =>
                {
                    var result = false;
                    var notice = model as TimelineImageNoticeModel;

                    lock (notice)
                    {
                        var delay = notice.Delay.HasValue ?
                            notice.Delay.Value :
                            0;
                        if (DateTime.Now >= notice.TimeToHide.AddSeconds(delay + 0.1d) ||
                            notice.toHide)
                        {
                            if (notice.overlay != null)
                            {
                                if (notice.overlay.OverlayVisible)
                                {
                                    notice.RemoveSyncToHide();
                                    TimelineVisualNoticeModel.DequeueToHide(sender);
                                }

                                notice.overlay.OverlayVisible = false;
                            }

                            notice.toHide = false;
                        }
                    }

                    return result;
                });

            this.overlay?.ShowNotice();
        }

        public void CloseNotice()
        {
            lock (this)
            {
                if (this.overlay != null)
                {
                    this.toHide = false;
                    this.RemoveSyncToHide();
                    this.overlay.Model = null;
                    this.overlay.Close();
                    this.overlay = null;
                }
            }
        }

        private string image = null;

        [XmlAttribute(AttributeName = "image")]
        public string Image
        {
            get => this.image;
            set
            {
                if (this.SetProperty(ref this.image, value))
                {
                    this.RaisePropertyChanged(nameof(this.BitmapImage));
                    this.RaisePropertyChanged(nameof(this.ExistsImage));
                }
            }
        }

        [XmlIgnore]
        public BitmapSource BitmapImage => GetBitmapImageAsync(this.Image);

        [XmlIgnore]
        public bool ExistsImage => this.BitmapImage != null;

        private static readonly Dictionary<string, BitmapSource> ImageDictionary = new Dictionary<string, BitmapSource>();

        private static readonly Regex UriRegex = new Regex(
            @"^s?https?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+$",
            RegexOptions.Compiled);

        private static BitmapSource GetBitmapImageAsync(
            string image)
        {
            var webImage = GetBitmapImageFromUrl(image);
            if (webImage != null)
            {
                return webImage;
            }

            lock (ImageDictionary)
            {
                image = image.ToLower();

                if (ImageDictionary.ContainsKey(image))
                {
                    return ImageDictionary[image];
                }

                var file = string.Empty;

                if (File.Exists(image))
                {
                    file = image;
                }
                else
                {
                    var root = DirectoryHelper.FindSubDirectory(@"resources\images");
                    if (Directory.Exists(root))
                    {
                        file = FileHelper.FindFiles(root, image).FirstOrDefault();
                    }
                }

                if (string.IsNullOrEmpty(file) ||
                    !File.Exists(file))
                {
                    return null;
                }

                var bitmap = default(WriteableBitmap);
                using (var ms = new WrappingStream(new MemoryStream(File.ReadAllBytes(file))))
                {
                    bitmap = new WriteableBitmap(BitmapFrame.Create(ms));
                }

                bitmap.Freeze();
                ImageDictionary[image] = bitmap;

                return bitmap;
            }
        }

        private static readonly Dictionary<string, BitmapImage> WebImageDictionary = new Dictionary<string, BitmapImage>(128);

        private static BitmapSource GetBitmapImageFromUrl(
            string image)
        {
            if (!UriRegex.IsMatch(image))
            {
                return null;
            }

            var bitmap = default(BitmapImage);

            if (WebImageDictionary.ContainsKey(image))
            {
                bitmap = WebImageDictionary[image];
            }
            else
            {
                using (var web = new WebClient())
                {
                    var bytes = web.DownloadData(image);
                    using (var stream = new MemoryStream(bytes))
                    {
                        bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                    }
                }

                WebImageDictionary[image] = bitmap;
            }

            return bitmap;
        }

        #region Commmands

        private static readonly System.Windows.Forms.OpenFileDialog OpenFileDialog = new System.Windows.Forms.OpenFileDialog()
        {
            InitialDirectory = DirectoryHelper.FindSubDirectory(@"resources\images"),
            RestoreDirectory = true,
            Filter = "PNG Images|*.png|All Files|*.*",
            FilterIndex = 0,
            DefaultExt = ".png",
            SupportMultiDottedExtensions = true,
        };

        private ICommand browseImageCommand;

        public ICommand BrowseImageCommand =>
            this.browseImageCommand ?? (this.browseImageCommand = new DelegateCommand(() =>
            {
                var result = OpenFileDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var file = OpenFileDialog.FileName;

                    if (UriRegex.IsMatch(file))
                    {
                        this.Image = file;
                    }
                    else
                    {
                        this.Image = Path.GetFileName(OpenFileDialog.FileName);
                    }
                }
            }));

        private ICommand copyTagCommand;

        public ICommand CcopyTagCommand =>
            this.copyTagCommand ?? (this.copyTagCommand = new DelegateCommand(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        Clipboard.SetDataObject(this.Tag + Environment.NewLine);
                        break;
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is COMException)
                    {
                        Thread.Sleep(10);
                    }
                }
            }));

        #endregion Commmands

        #region IClonable

        public TimelineImageNoticeModel Clone()
        {
            var clone = this.MemberwiseClone() as TimelineImageNoticeModel;

            clone.ClearToHide();

            return clone;
        }

        #endregion IClonable

        #region Dummy Notice

        private static TimelineImageNoticeModel dummyNotice;

        public static TimelineImageNoticeModel DummyNotice =>
            dummyNotice ?? (dummyNotice = CreateDummyNotice());

        private static TimelineImageNoticeModel CreateDummyNotice()
        {
            var dummy = new TimelineImageNoticeModel();

            dummy.Image = "Sample.png";
            dummy.Scale = 1.0;
            dummy.Duration = 5;
            dummy.Left = -1;
            dummy.Top = -1;

            return dummy;
        }

        #endregion Dummy Notice
    }
}
