using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.XIVHelper;
using static ACT.SpecialSpellTimer.Models.TableCompiler;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [XmlType(TypeName = "v-notice")]
    [Serializable]
    public class TimelineVisualNoticeModel :
        TimelineBase,
        IStylable
    {
        #region TimelineBase

        public override TimelineElementTypes TimelineType => TimelineElementTypes.VisualNotice;

        public override IList<TimelineBase> Children => null;

        #endregion TimelineBase

        public const string ParentTextPlaceholder = "{text}";
        public const string ParentNoticePlaceholder = "{notice}";

        private string text = null;

        [XmlAttribute(AttributeName = "text")]
        [DefaultValue(ParentTextPlaceholder)]
        public string Text
        {
            get => this.text;
            set => this.SetProperty(ref this.text, value);
        }

        private string textToDisplay = ParentTextPlaceholder;

        [XmlIgnore]
        public string TextToDisplay
        {
            get => this.textToDisplay;
            set
            {
                var text = value?.Replace("\\n", Environment.NewLine);
                this.SetProperty(ref this.textToDisplay, text);
            }
        }

        private double? duration = null;

        [XmlIgnore]
        public double? Duration
        {
            get => this.duration;
            set => this.SetProperty(ref this.duration, value);
        }

        private double durationToDisplay = 0;

        [XmlIgnore]
        public double DurationToDisplay
        {
            get => this.durationToDisplay;
            set => this.SetProperty(ref this.durationToDisplay, value);
        }

        private void RefreshDuration()
        {
            var remain = (this.TimeToHide - DateTime.Now).TotalSeconds;
            if (remain < 0d)
            {
                remain = 0d;
            }

            this.DurationToDisplay = remain.CeilingEx();
        }

        [XmlAttribute(AttributeName = "duration")]
        public string DurationXML
        {
            get => this.Duration?.ToString();
            set => this.Duration = double.TryParse(value, out var v) ? v : (double?)null;
        }

        private bool? durationVisible = null;

        [XmlIgnore]
        public bool? DurationVisible
        {
            get => this.durationVisible;
            set => this.SetProperty(ref this.durationVisible, value);
        }

        [XmlAttribute(AttributeName = "duration-visible")]
        public string DurationVisibleXML
        {
            get => this.DurationVisible?.ToString();
            set => this.DurationVisible = bool.TryParse(value, out var v) ? v : (bool?)null;
        }

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

        private int stack = 0;

        [XmlIgnore]
        public int Stack
        {
            get => this.stack;
            private set => this.SetProperty(ref this.stack, value);
        }

        public void IncrementStack()
        {
            this.Stack++;
            TimelineController.RaiseLog($"increments stacks of {this.TextToDisplay} to {this.Stack}.");
        }

        public void ClearStack()
        {
            this.Stack = 0;
            TimelineController.RaiseLog($"clear stacks of {this.TextToDisplay}.");
        }

        private bool? stackVisible = null;

        [XmlIgnore]
        public bool? StackVisible
        {
            get => this.stackVisible;
            set => this.SetProperty(ref this.stackVisible, value);
        }

        [XmlAttribute(AttributeName = "stack-visible")]
        public string StackVisibleXML
        {
            get => this.StackVisible?.ToString();
            set => this.StackVisible = bool.TryParse(value, out var v) ? v : (bool?)null;
        }

        private int? order = null;

        [XmlIgnore]
        public int? Order
        {
            get => this.order;
            set => this.SetProperty(ref this.order, value);
        }

        [XmlAttribute(AttributeName = "order")]
        public string OrderXML
        {
            get => this.Order?.ToString();
            set => this.Order = int.TryParse(value, out var v) ? v : (int?)null;
        }

        #region sync-to-hide

        private static readonly List<TimelineVisualNoticeModel> syncToHideList = new List<TimelineVisualNoticeModel>();

        public static TimelineVisualNoticeModel[] GetSyncToHideList()
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
                        RegexOptions.Compiled);
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

        private bool isVisible = false;

        [XmlIgnore]
        public bool IsVisible
        {
            get => this.isVisible;
            set => this.SetProperty(ref this.isVisible, value);
        }

        private long logSeq = 0L;

        [XmlIgnore]
        public long LogSeq
        {
            get => this.logSeq;
            set => this.SetProperty(ref this.logSeq, value);
        }

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

        private volatile bool toHide = false;

        public void StartNotice(
            Action<TimelineVisualNoticeModel> removeCallback,
            bool isDummyMode = false)
        {
            this.IsVisible = true;
            this.RefreshDuration();

            EnqueueToHide(
                this,
                (sender, model) =>
                {
                    var result = false;
                    var notice = model as TimelineVisualNoticeModel;

                    notice.RefreshDuration();

                    var delay = notice.Delay.HasValue ?
                        notice.Delay.Value :
                        0;
                    if (DateTime.Now >= notice.TimeToHide.AddSeconds(delay + 1.0d) ||
                        notice.toHide)
                    {
                        if (!sender.IsDummyMode)
                        {
                            if (notice.IsVisible)
                            {
                                notice.RemoveSyncToHide();
                                DequeueToHide(sender);
                            }

                            notice.toHide = false;
                            notice.IsVisible = false;
                            removeCallback?.Invoke(notice);
                        }

                        result = true;
                    }

                    return result;
                },
                isDummyMode);
        }

        private static readonly double ToHideSubscriberInterval = 0.1d;

        private static readonly DispatcherTimer ToHideTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(ToHideSubscriberInterval),
            DispatcherPriority.Normal,
            ToHideTimerOnTick,
            Application.Current.Dispatcher);

        private static readonly List<TimelineVisualNoticeToHideEntry> ToHideEntryList = new List<TimelineVisualNoticeToHideEntry>(32);

        private static void ToHideTimerOnTick(
            object sender,
            EventArgs e)
        {
            var list = default(IEnumerable<TimelineVisualNoticeToHideEntry>);

            lock (ToHideEntryList)
            {
                list = ToHideEntryList.ToArray();
            }

            foreach (var entry in list)
            {
                if (entry.TryHideCallback(entry, entry.NoticeModel))
                {
                    lock (ToHideEntryList)
                    {
                        ToHideEntryList.Remove(entry);
                    }
                }
            }
        }

        public static void EnqueueToHide(
            TimelineBase noticeModel,
            Func<TimelineVisualNoticeToHideEntry, TimelineBase, bool> tryHideCallback,
            bool isDummyMode = false)
        {
            lock (ToHideEntryList)
            {
                ToHideEntryList.Add(new TimelineVisualNoticeToHideEntry()
                {
                    NoticeModel = noticeModel,
                    IsDummyMode = isDummyMode,
                    TryHideCallback = tryHideCallback
                });
            }

            if (!ToHideTimer.IsEnabled)
            {
                ToHideTimer.Start();
            }
        }

        public static void DequeueToHide(TimelineVisualNoticeToHideEntry entry)
        {
            lock (ToHideEntryList)
            {
                ToHideEntryList.Remove(entry);
            }
        }

        public static void ClearToHideEntry()
        {
            lock (ToHideEntryList)
            {
                ToHideEntryList.Clear();
            }

            ToHideTimer.Stop();
        }

        #region IStylable

        private string style = null;

        [XmlAttribute(AttributeName = "style")]
        public string Style
        {
            get => this.style;
            set => this.SetProperty(ref this.style, value);
        }

        private TimelineStyle styleModel = null;

        [XmlIgnore]
        public TimelineStyle StyleModel
        {
            get => this.styleModel;
            set => this.SetProperty(ref this.styleModel, value);
        }

        private string icon = null;

        [XmlAttribute(AttributeName = "icon")]
        public string Icon
        {
            get => this.icon;
            set
            {
                if (this.SetProperty(ref this.icon, value))
                {
                    this.RaisePropertyChanged(nameof(this.IconImage));
                    this.RaisePropertyChanged(nameof(this.ThisIconImage));
                    this.RaisePropertyChanged(nameof(this.ExistsIcon));
                }
            }
        }

        [XmlIgnore]
        public bool ExistsIcon => this.GetExistsIcon();

        [XmlIgnore]
        public BitmapSource IconImage => this.GetIconImage();

        [XmlIgnore]
        public BitmapSource ThisIconImage => this.GetThisIconImage();

        #endregion IStylable

        #region JobIcon

        private bool? isJobIcon = null;

        [XmlIgnore]
        public bool? IsJobIcon
        {
            get => this.isJobIcon;
            set => this.SetProperty(ref this.isJobIcon, value);
        }

        [XmlAttribute(AttributeName = "job-icon")]
        public string IsJobIconXML
        {
            get => this.IsJobIcon?.ToString();
            set => this.IsJobIcon = bool.TryParse(value, out var v) ? v : (bool?)null;
        }

        /// <summary>
        /// ジョブアイコンを適用する
        /// </summary>
        public void SetJobIcon()
        {
            if (!this.isJobIcon ?? false)
            {
                return;
            }

            var tri = this.GetParent<TimelineTriggerModel>();
            if (tri == null ||
                tri.SyncMatch == null ||
                !tri.SyncMatch.Success ||
                tri.SyncMatch.Groups.Count < 1)
            {
                this.Icon = null;
                return;
            }

            var combatant = default(CombatantEx);
            foreach (Group g in tri.SyncMatch.Groups)
            {
                foreach (Capture cap in g.Captures)
                {
                    var c = CombatantsManager.Instance.GetCombatant(cap.Value);
                    if (c != null)
                    {
                        combatant = c;
                    }
                }
            }

            if (combatant == null)
            {
                this.Icon = null;
                return;
            }

            this.Icon = $"{combatant.JobID.ToString()}.png";
        }

        #endregion JobIcon

        #region IClonable

        public TimelineVisualNoticeModel Clone()
        {
            var clone = this.MemberwiseClone() as TimelineVisualNoticeModel;

            clone.ClearToHide();

            return clone;
        }

        #endregion IClonable

        #region Dummy Notice

        private static List<TimelineVisualNoticeModel> dummyNotices;

        public static List<TimelineVisualNoticeModel> DummyNotices =>
            dummyNotices ?? (dummyNotices = CreateDummyNotices());

        public static List<TimelineVisualNoticeModel> CreateDummyNotices(
            TimelineStyle testStyle = null)
        {
            var notices = new List<TimelineVisualNoticeModel>();

            if (testStyle == null)
            {
                testStyle = TimelineStyle.SuperDefaultStyle;
                if (!WPFHelper.IsDesignMode)
                {
                    testStyle = TimelineSettings.Instance.DefaultNoticeStyle;
                }
            }

            var notice1 = new TimelineVisualNoticeModel()
            {
                Enabled = true,
                TextToDisplay = "デスセンテンス\n→ タンク",
                Duration = 3,
                DurationVisible = true,
                StyleModel = testStyle,
                Icon = "1マーカー.png",
                IsVisible = true,
            };

            var notice2 = new TimelineVisualNoticeModel()
            {
                Enabled = true,
                TextToDisplay = "ツイスター",
                Duration = 10,
                DurationVisible = true,
                StyleModel = testStyle,
                Icon = "2マーカー.png",
                IsVisible = true,
            };

            var notice3 = new TimelineVisualNoticeModel()
            {
                Enabled = true,
                TextToDisplay = "デバフ",
                Duration = 10,
                DurationVisible = false,
                StyleModel = testStyle,
                Stack = 3,
                StackVisible = true,
                Icon = "ファイア系.png",
                IsVisible = true,
            };

            notices.Add(notice1);
            notices.Add(notice2);
            notices.Add(notice3);

            for (int i = 0; i < 6; i++)
            {
                notices.Add(new TimelineVisualNoticeModel()
                {
                    Enabled = true,
                    TextToDisplay = "マーカー" + (i + 1),
                    Duration = 10 + i + 1,
                    DurationVisible = false,
                    StyleModel = testStyle,
                    Icon = "Marker.png",
                    IsVisible = true,
                    Order = i + 1
                });
            }

            return notices;
        }

        #endregion Dummy Notice
    }

    public class TimelineVisualNoticeToHideEntry
    {
        public TimelineBase NoticeModel { get; set; }

        public bool IsDummyMode { get; set; }

        public Func<TimelineVisualNoticeToHideEntry, TimelineBase, bool> TryHideCallback { get; set; }
    }
}
