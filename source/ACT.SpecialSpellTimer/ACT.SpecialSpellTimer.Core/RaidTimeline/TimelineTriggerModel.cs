using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FFXIV.Framework.Common;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [XmlType(TypeName = "t")]
    public class TimelineTriggerModel :
        TimelineBase,
        ISynchronizable
    {
        [XmlIgnore]
        public override TimelineElementTypes TimelineType => TimelineElementTypes.Trigger;

        #region Children

        public override IList<TimelineBase> Children => this.statements;

        private List<TimelineBase> statements = new List<TimelineBase>();

        [XmlIgnore]
        public IReadOnlyList<TimelineBase> Statements => this.statements;

        [XmlElement(ElementName = "load")]
        public TimelineLoadModel[] LoadStatements
        {
            get => this.Statements
                .Where(x => x.TimelineType == TimelineElementTypes.Load)
                .Cast<TimelineLoadModel>()
                .ToArray();

            set => this.AddRange(value);
        }

        [XmlElement(ElementName = "v-notice")]
        public TimelineVisualNoticeModel[] VisualNoticeStatements
        {
            get => this.Statements
                .Where(x => x.TimelineType == TimelineElementTypes.VisualNotice)
                .Cast<TimelineVisualNoticeModel>()
                .ToArray();

            set => this.AddRange(value);
        }

        [XmlElement(ElementName = "i-notice")]
        public TimelineImageNoticeModel[] ImageNoticeStatements
        {
            get => this.Statements
                .Where(x => x.TimelineType == TimelineElementTypes.ImageNotice)
                .Cast<TimelineImageNoticeModel>()
                .ToArray();

            set => this.AddRange(value);
        }

        [XmlElement(ElementName = "p-sync")]
        public TimelinePositionSyncModel[] PositionSyncStatements
        {
            get => this.Statements
                .Where(x => x.TimelineType == TimelineElementTypes.PositionSync)
                .Cast<TimelinePositionSyncModel>()
                .ToArray();

            set => this.AddRange(value);
        }

        /// <summary>
        /// 条件式
        /// </summary>
        /// <remarks>
        /// 構文上複数定義できるが最初の定義しか使用しない
        /// </remarks>
        [XmlElement(ElementName = "expressions")]
        public TimelineExpressionsModel[] ExpressionsStatements
        {
            get => this.Statements
                .Where(x => x.TimelineType == TimelineElementTypes.Expressions)
                .Cast<TimelineExpressionsModel>()
                .ToArray();

            set => this.AddRange(value);
        }

        [XmlIgnore]
        public bool IsExpressionAvailable =>
            this.ExpressionsStatements.Any(x => x.Enabled.GetValueOrDefault());

        public bool ExecuteExpressions()
        {
            var expressions = this.ExpressionsStatements.FirstOrDefault(x =>
                x.Enabled.GetValueOrDefault());

            if (expressions == null)
            {
                return true;
            }

            lock (TimelineExpressionsModel.ExpressionLocker)
            {
                var result = expressions.Predicate();
                if (result)
                {
                    expressions.Set();
                }

                return result;
            }
        }

        [XmlIgnore]
        public bool IsPositionSyncAvailable =>
            this.PositionSyncStatements.Any(x => x.Enabled.GetValueOrDefault());

        public void Add(TimelineBase timeline)
        {
            if (timeline.TimelineType == TimelineElementTypes.Load ||
                timeline.TimelineType == TimelineElementTypes.VisualNotice ||
                timeline.TimelineType == TimelineElementTypes.ImageNotice ||
                timeline.TimelineType == TimelineElementTypes.PositionSync ||
                timeline.TimelineType == TimelineElementTypes.Expressions)
            {
                timeline.Parent = this;
                this.statements.Add(timeline);
            }
        }

        public void AddRange(IEnumerable<TimelineBase> timelines)
        {
            if (timelines != null)
            {
                foreach (var tl in timelines)
                {
                    this.Add(tl);
                }
            }
        }

        #endregion Children

        private int? no = null;

        [XmlIgnore]
        public int? No
        {
            get => this.no;
            set => this.SetProperty(ref this.no, value);
        }

        [XmlAttribute(AttributeName = "no")]
        public string NoXML
        {
            get => this.No?.ToString();
            set => this.No = int.TryParse(value, out var v) ? v : (int?)null;
        }

        private string text = null;

        [XmlAttribute(AttributeName = "text")]
        public string Text
        {
            get => this.text;
            set => this.SetProperty(ref this.text, value?.Replace("\\n", Environment.NewLine));
        }

        private string textReplaced = null;

        /// <summary>
        /// 正規表現置換後のText
        /// </summary>
        [XmlIgnore]
        public string TextReplaced
        {
            get => this.textReplaced;
            set => this.SetProperty(ref this.textReplaced, value);
        }

        private string syncKeyword = null;

        [XmlAttribute(AttributeName = "sync")]
        public string SyncKeyword
        {
            get => this.syncKeyword;
            set
            {
                if (this.SetProperty(ref this.syncKeyword, value))
                {
                    this.SyncKeywordReplaced = null;
                }
            }
        }

        private string syncKeywordReplaced = null;

        [XmlIgnore]
        public string SyncKeywordReplaced
        {
            get => this.syncKeywordReplaced;
            set
            {
                if (this.SetProperty(ref this.syncKeywordReplaced, value))
                {
                    if (string.IsNullOrEmpty(this.syncKeywordReplaced))
                    {
                        this.SyncRegex = null;
                    }
                    else
                    {
                        this.SyncRegex = new Regex(
                            this.syncKeywordReplaced,
                            RegexOptions.Compiled);
                    }
                }
            }
        }

        private Regex syncRegex = null;

        [XmlIgnore]
        public Regex SyncRegex
        {
            get => this.syncRegex;
            private set => this.SetProperty(ref this.syncRegex, value);
        }

        private Match syncMatch = null;

        [XmlIgnore]
        public Match SyncMatch

        {
            get => this.syncMatch;
            set => this.SetProperty(ref this.syncMatch, value);
        }

        private int? syncCount = null;

        [XmlIgnore]
        public int? SyncCount
        {
            get => this.syncCount;
            set => this.SetProperty(ref this.syncCount, value);
        }

        [XmlAttribute(AttributeName = "sync-count")]
        public string SyncCountXML
        {
            get => this.SyncCount?.ToString();
            set => this.SyncCount = int.TryParse(value, out var v) ? v : (int?)null;
        }

        private int? syncInterval = null;

        [XmlIgnore]
        public int? SyncInterval
        {
            get => this.syncInterval;
            set => this.SetProperty(ref this.syncInterval, value);
        }

        [XmlAttribute(AttributeName = "sync-interval")]
        public string SyncIntervalXML
        {
            get => this.SyncInterval?.ToString();
            set => this.SyncInterval = int.TryParse(value, out var v) ? v : (int?)null;
        }

        public string gotoDestination = null;

        [XmlAttribute(AttributeName = "goto")]
        public string GoToDestination
        {
            get => this.gotoDestination;
            set => this.SetProperty(ref this.gotoDestination, value);
        }

        public string callTarget = null;

        [XmlAttribute(AttributeName = "call")]
        public string CallTarget
        {
            get => this.callTarget;
            set => this.SetProperty(ref this.callTarget, value);
        }

        private string notice = null;

        [XmlAttribute(AttributeName = "notice")]
        public string Notice
        {
            get => this.notice;
            set => this.SetProperty(ref this.notice, value);
        }

        private string noticeReplaced = null;

        /// <summary>
        /// 正規表現置換後のNotice
        /// </summary>
        [XmlIgnore]
        public string NoticeReplaced
        {
            get => this.noticeReplaced;
            set => this.SetProperty(ref this.noticeReplaced, value);
        }

        private NoticeDevices? noticeDevice = null;

        [XmlIgnore]
        public NoticeDevices? NoticeDevice
        {
            get => this.noticeDevice;
            set => this.SetProperty(ref this.noticeDevice, value);
        }

        [XmlAttribute(AttributeName = "notice-d")]
        public string NoticeDeviceXML
        {
            get => this.NoticeDevice?.ToString();
            set => this.NoticeDevice = Enum.TryParse<NoticeDevices>(value, out var v) ? v : (NoticeDevices?)null;
        }

        private float? noticeVolume = null;

        [XmlIgnore]
        public float? NoticeVolume
        {
            get => this.noticeVolume;
            set => this.SetProperty(ref this.noticeVolume, value);
        }

        [XmlAttribute(AttributeName = "notice-vol")]
        public string NoticeVolumeXML
        {
            get => this.NoticeVolume?.ToString();
            set => this.NoticeVolume = float.TryParse(value, out var v) ? v : (float?)null;
        }

        private bool? noticeSync = null;

        [XmlIgnore]
        public bool? NoticeSync
        {
            get => this.noticeSync;
            set => this.SetProperty(ref this.noticeSync, value);
        }

        [XmlAttribute(AttributeName = "notice-sync")]
        public string NoticeSyncXML
        {
            get => this.NoticeSync?.ToString();
            set => this.NoticeSync = bool.TryParse(value, out var v) ? v : (bool?)null;
        }

        private string style = null;

        [XmlAttribute(AttributeName = "style")]
        public string Style
        {
            get => this.style;
            set => this.SetProperty(ref this.style, value);
        }

        private string executeFileName = null;

        [XmlAttribute(AttributeName = "exec")]
        public string ExecuteFileName
        {
            get => this.executeFileName;
            set => this.SetProperty(ref this.executeFileName, value);
        }

        private string arguments = null;

        [XmlAttribute(AttributeName = "args")]
        public string Arguments
        {
            get => this.arguments;
            set => this.SetProperty(ref this.arguments, value);
        }

        private bool? isExecuteHidden = null;

        [XmlIgnore]
        public bool? IsExecuteHidden
        {
            get => this.isExecuteHidden;
            set => this.SetProperty(ref this.isExecuteHidden, value);
        }

        [XmlAttribute(AttributeName = "exec-hidden")]
        public string IsExecuteHiddenXML
        {
            get => this.isExecuteHidden?.ToString();
            set => this.isExecuteHidden = bool.TryParse(value, out var v) ? v : (bool?)null;
        }

        private int matchedCounter = 0;

        [XmlIgnore]
        public int MatchedCounter
        {
            get => this.matchedCounter;
            set => this.SetProperty(ref this.matchedCounter, value);
        }

        private DateTime matchedTimestamp;

        [XmlIgnore]
        public DateTime MatchedTimestamp
        {
            get => this.matchedTimestamp;
            set => this.SetProperty(ref this.matchedTimestamp, value);
        }

        private long logSeq = 0L;

        [XmlIgnore]
        public long LogSeq
        {
            get => this.logSeq;
            set => this.SetProperty(ref this.logSeq, value);
        }

        public bool IsAvailable()
        {
            if (this.Enabled.GetValueOrDefault())
            {
                if (this.IsPositionSyncAvailable)
                {
                    return true;
                }
                else
                {
                    if (!string.IsNullOrEmpty(this.SyncKeyword) &&
                        this.SyncRegex != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Init()
        {
            lock (this)
            {
                this.MatchedTimestamp = DateTime.MinValue;
                this.MatchedCounter = 0;

                if (this.PositionSyncStatements != null)
                {
                    foreach (var psync in this.PositionSyncStatements)
                    {
                        if (psync != null)
                        {
                            psync.LastSyncTimestamp = DateTime.MinValue;
                        }
                    }
                }
            }
        }

        public void Execute()
        {
            if (string.IsNullOrEmpty(this.ExecuteFileName))
            {
                return;
            }

            var path = this.ExecuteFileName;

            Task.Run(() =>
            {
                try
                {
                    var ps = new ProcessStartInfo()
                    {
                        WorkingDirectory = TimelineManager.Instance.TimelineDirectory,
                    };

                    var isHidden = this.IsExecuteHidden ?? false;
                    var ext = Path.GetExtension(path).ToLower();

                    switch (ext)
                    {
                        case ".ps1":
                            ps.FileName = EnvironmentHelper.Pwsh;
                            ps.Arguments = $@"-File ""{path}"" {this.Arguments}";
                            ps.UseShellExecute = false;

                            if (isHidden)
                            {
                                ps.WindowStyle = ProcessWindowStyle.Hidden;
                                ps.CreateNoWindow = true;
                            }
                            break;

                        case ".bat":
                            ps.FileName = Environment.GetEnvironmentVariable("ComSpec");
                            ps.Arguments = $@"/C ""{path}"" {this.Arguments}";
                            ps.UseShellExecute = false;

                            if (isHidden)
                            {
                                ps.WindowStyle = ProcessWindowStyle.Hidden;
                                ps.CreateNoWindow = true;
                            }
                            break;

                        default:
                            ps.FileName = path;
                            ps.Arguments = this.Arguments;
                            ps.UseShellExecute = true;

                            if (isHidden)
                            {
                                ps.WindowStyle = ProcessWindowStyle.Hidden;
                            }
                            break;
                    }

                    Process.Start(ps);

                    TimelineController.RaiseLog(
                        $"[TL] trigger executed. exec={this.ExecuteFileName}, args={this.Arguments}");
                }
                catch (Exception ex)
                {
                    AppLog.DefaultLogger.Error(
                        ex,
                        $"[TL] Error at execute external tool. exec={this.ExecuteFileName}, args={this.Arguments}");
                }
            });
        }

        public TimelineTriggerModel Clone()
        {
            var clone = this.MemberwiseClone() as TimelineTriggerModel;

            if (this.SyncMatch != null)
            {
                var b = new BinaryFormatter();

                using (var ms = new WrappingStream(new MemoryStream()))
                {
                    b.Serialize(ms, this.SyncMatch);
                    ms.Position = 0;
                    clone.SyncMatch = b.Deserialize(ms) as Match;
                }
            }

            clone.statements = new List<TimelineBase>();

            var statements = new List<TimelineBase>();
            foreach (var stat in this.statements)
            {
                var child = stat;

                switch (stat)
                {
                    case TimelineVisualNoticeModel v:
                        child = v.Clone();
                        break;

                    case TimelineImageNoticeModel i:
                        child = i.Clone();
                        break;
                }

                statements.Add(child);
            }

            clone.AddRange(statements);

            return clone;
        }

        public override string ToString() =>
            !string.IsNullOrEmpty(this.SyncKeywordReplaced) ?
            this.SyncKeywordReplaced :
            this.SyncKeyword;
    }
}
