using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [XmlType(TypeName = "s")]
    [XmlInclude(typeof(TimelineDefaultModel))]
    [XmlInclude(typeof(TimelineActivityModel))]
    [XmlInclude(typeof(TimelineTriggerModel))]
    [XmlInclude(typeof(TimelineImportModel))]
    [XmlInclude(typeof(TimelineSubroutineModel))]
    public class TimelineSubroutineModel :
        TimelineBase
    {
        [XmlIgnore]
        public override TimelineElementTypes TimelineType => TimelineElementTypes.Subroutine;

        public override IList<TimelineBase> Children => this.statements;

        private readonly List<TimelineBase> statements = new List<TimelineBase>();
        private readonly List<TimelineBase> importTriggers = new List<TimelineBase>();

        [XmlIgnore]
        public IReadOnlyList<TimelineBase> Statements => this.statements;

        [XmlElement(ElementName = "a")]
        public TimelineActivityModel[] Activities
        {
            get => this.Statements
                .Where(x => x.TimelineType == TimelineElementTypes.Activity)
                .Cast<TimelineActivityModel>()
                .OrderBy(x => x.Time)
                .ToArray();

            set => this.AddRange(value);
        }

        /// <summary>
        /// Triggers
        /// </summary>
        /// <remarks>
        /// 必ずNoでソートされる</remarks>
        [XmlElement(ElementName = "t")]
        public TimelineTriggerModel[] Triggers
        {
            // インポートトリガとマージして取り出す
            get => this.Statements.Concat(this.importTriggers)
                .Where(x => x.TimelineType == TimelineElementTypes.Trigger)
                .Cast<TimelineTriggerModel>()
                .OrderBy(x => x.No.GetValueOrDefault())
                .ToArray();

            set => this.AddRange(value);
        }

        /// <summary>
        /// Imports
        /// </summary>
        /// <remarks>
        /// 必ずNoでソートされる</remarks>
        [XmlElement(ElementName = "import")]
        public TimelineImportModel[] Imports
        {
            get => this.Statements
                .Where(x => x.TimelineType == TimelineElementTypes.Import)
                .Cast<TimelineImportModel>()
                .ToArray();

            set => this.AddRange(value);
        }

        /// <summary>
        /// Script
        /// </summary>
        [XmlElement(ElementName = "script")]
        public TimelineScriptModel[] Scripts
        {
            get => this.Statements
                .Where(x => x.TimelineType == TimelineElementTypes.Script)
                .Cast<TimelineScriptModel>()
                .ToArray();

            set => this.AddRange(value);
        }

        /// <summary>
        /// トリガのインポートを実行する
        /// </summary>
        public void ExecuteImports()
        {
            this.importTriggers.Clear();

            var imports = this.Imports
                .Where(x => x.Enabled.GetValueOrDefault());

            if (!imports.Any())
            {
                return;
            }

            var timeline = this.Parent as TimelineModel;
            if (timeline == null)
            {
                return;
            }

            var subs = timeline.Subroutines
                .Where(x => x.Enabled.GetValueOrDefault());

            foreach (var import in imports)
            {
                if (string.IsNullOrEmpty(import.Source))
                {
                    continue;
                }

                var sub = subs.FirstOrDefault(x =>
                    x.Name.Equals(import.Source, StringComparison.OrdinalIgnoreCase));

                if (sub == null)
                {
                    continue;
                }

                var triggers = sub.Triggers
                    .Where(x => x.Enabled.GetValueOrDefault())
                    .Cast<TimelineTriggerModel>()
                    .OrderBy(x => x.No.GetValueOrDefault());

                if (triggers.Any())
                {
                    foreach (var t in triggers)
                    {
                        // トリガのクローンをこのサブルーチンに取り込む
                        var clone = t.Clone();
                        clone.Parent = this;
                        this.importTriggers.Add(clone);
                    }
                }
            }
        }

        #region Methods

        public void Add(TimelineBase timeline)
        {
            if (timeline.TimelineType == TimelineElementTypes.Activity ||
                timeline.TimelineType == TimelineElementTypes.Trigger ||
                timeline.TimelineType == TimelineElementTypes.Import ||
                timeline.TimelineType == TimelineElementTypes.Script)
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

        #endregion Methods
    }

    [XmlType(TypeName = "import")]
    public class TimelineImportModel :
        TimelineBase
    {
        [XmlIgnore]
        public override TimelineElementTypes TimelineType => TimelineElementTypes.Import;

        public override IList<TimelineBase> Children => null;

        private string source = null;

        [XmlAttribute(AttributeName = "source")]
        public string Source
        {
            get => this.source;
            set => this.SetProperty(ref this.source, value);
        }
    }
}
