using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.XIVHelper;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [Serializable]
    [XmlType(TypeName = "p-sync")]
    public class TimelinePositionSyncModel :
        TimelineBase
    {
        [XmlIgnore]
        public override TimelineElementTypes TimelineType => TimelineElementTypes.PositionSync;

        #region Children

        public override IList<TimelineBase> Children => this.statements;

        private List<TimelineBase> statements = new List<TimelineBase>();

        [XmlIgnore]
        public IReadOnlyList<TimelineBase> Statements => this.statements;

        [XmlElement(ElementName = "combatant")]
        public TimelineCombatantModel[] Combatants
        {
            get => this.Statements
                .Where(x => x.TimelineType == TimelineElementTypes.Combatant)
                .Cast<TimelineCombatantModel>()
                .ToArray();

            set => this.AddRange(value);
        }

        public void Add(TimelineBase timeline)
        {
            if (timeline.TimelineType == TimelineElementTypes.Combatant)
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

        private double? interval = null;

        [XmlIgnore]
        public double? Interval
        {
            get => this.interval;
            set => this.SetProperty(ref this.interval, value);
        }

        [XmlAttribute(AttributeName = "interval")]
        public string IntervalXML
        {
            get => this.Interval?.ToString();
            set => this.Interval = float.TryParse(value, out var v) ? v : (double?)null;
        }

        private DateTime lastSyncTimestamp;

        [XmlIgnore]
        public DateTime LastSyncTimestamp
        {
            get => this.lastSyncTimestamp;
            set => this.SetProperty(ref this.lastSyncTimestamp, value);
        }
    }

    [Serializable]
    [XmlType(TypeName = "combatant")]
    public class TimelineCombatantModel :
        TimelineBase
    {
        [XmlIgnore]
        public override TimelineElementTypes TimelineType => TimelineElementTypes.Combatant;

        public override IList<TimelineBase> Children => null;

        [XmlAttribute(AttributeName = "name")]
        public override string Name
        {
            get => this.name;
            set
            {
                if (this.SetProperty(ref this.name, value))
                {
                    this.NameMatch = null;

                    if (string.IsNullOrEmpty(this.name))
                    {
                        this.NameRegex = null;
                    }
                    else
                    {
                        this.NameRegex = new Regex(
                            this.name,
                            RegexOptions.Compiled |
                            RegexOptions.IgnoreCase);
                    }
                }
            }
        }

        private Regex nameRegex = null;

        [XmlIgnore]
        public Regex NameRegex
        {
            get => this.nameRegex;
            private set => this.SetProperty(ref this.nameRegex, value);
        }

        private Match nameMatch = null;

        [XmlIgnore]
        public Match NameMatch

        {
            get => this.nameMatch;
            set => this.SetProperty(ref this.nameMatch, value);
        }

        public bool IsMatchName(
            string name)
        {
            this.NameMatch = null;

            if (this.nameRegex == null)
            {
                return false;
            }

            this.NameMatch = this.nameRegex.Match(name);
            return this.NameMatch.Success;
        }

        private string action;

        [XmlAttribute(AttributeName = "action")]
        public string Action
        {
            get => this.action;
            set => this.SetProperty(ref this.action, value);
        }

        public bool IsMatchAction(
            string action)
        {
            if (string.IsNullOrEmpty(this.action) ||
                string.IsNullOrEmpty(action))
            {
                return true;
            }

            return action.ContainsIgnoreCase(this.action);
        }

        /// <summary>
        /// 無効な座標を示す値
        /// </summary>
        public const float InvalidPosition = -9999;

        private float? x = null;

        [XmlIgnore]
        public float? X
        {
            get => this.x;
            set => this.SetProperty(ref this.x, value);
        }

        [XmlAttribute(AttributeName = "X")]
        public string XXML
        {
            get => this.X?.ToString();
            set => this.X = float.TryParse(value, out var v) ? v : (float?)null;
        }

        private float? y = null;

        [XmlIgnore]
        public float? Y
        {
            get => this.y;
            set => this.SetProperty(ref this.y, value);
        }

        [XmlAttribute(AttributeName = "Y")]
        public string YXML
        {
            get => this.Y?.ToString();
            set => this.Y = float.TryParse(value, out var v) ? v : (float?)null;
        }

        private float? z = null;

        [XmlIgnore]
        public float? Z
        {
            get => this.z;
            set => this.SetProperty(ref this.z, value);
        }

        [XmlAttribute(AttributeName = "Z")]
        public string ZXML
        {
            get => this.Z?.ToString();
            set => this.Z = float.TryParse(value, out var v) ? v : (float?)null;
        }

        private float? tolerance = null;

        [XmlIgnore]
        public float? Tolerance
        {
            get => this.tolerance;
            set => this.SetProperty(ref this.tolerance, value);
        }

        [XmlAttribute(AttributeName = "tolerance")]
        public string ToleranceXML
        {
            get => this.Tolerance?.ToString();
            set => this.Tolerance = float.TryParse(value, out var v) ? v : (float?)null;
        }

        private CombatantEx actualCombatant;

        [XmlIgnore]
        public CombatantEx ActualCombatant
        {
            get => this.actualCombatant;
            set => this.SetProperty(ref this.actualCombatant, value);
        }
    }
}
