using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [XmlType(TypeName = "s")]
    [XmlInclude(typeof(TimelineDefaultModel))]
    [XmlInclude(typeof(TimelineActivityModel))]
    [XmlInclude(typeof(TimelineTriggerModel))]
    [XmlInclude(typeof(TimelineSubroutineModel))]
    public class TimelineSubroutineModel :
        TimelineBase
    {
        [XmlIgnore]
        public override TimelineElementTypes TimelineType => TimelineElementTypes.Subroutine;

        public override IList<TimelineBase> Children => this.statements;

        private List<TimelineBase> statements = new List<TimelineBase>();

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

        [XmlElement(ElementName = "t")]
        public TimelineTriggerModel[] Triggers
        {
            get => this.Statements.Where(x => x.TimelineType == TimelineElementTypes.Trigger).Cast<TimelineTriggerModel>().ToArray();
            set => this.AddRange(value);
        }

        #region Methods

        public void Add(TimelineBase timeline)
        {
            if (timeline.TimelineType == TimelineElementTypes.Activity ||
                timeline.TimelineType == TimelineElementTypes.Trigger)
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
}
