using System.Collections.Generic;
using System.Xml.Serialization;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [XmlType(TypeName = "default")]
    public class TimelineDefaultModel :
        TimelineBase
    {
        [XmlIgnore]
        public override TimelineElementTypes TimelineType => TimelineElementTypes.Default;

        public override IList<TimelineBase> Children => null;

        private TimelineElementTypes targetElement = TimelineElementTypes.Activity;

        [XmlAttribute(AttributeName = "target-element")]
        public TimelineElementTypes TargetElement
        {
            get => this.targetElement;
            set => this.SetProperty(ref this.targetElement, value);
        }

        private string targetAttribute = string.Empty;

        [XmlAttribute(AttributeName = "target-attr")]
        public string TargetAttribute
        {
            get => this.targetAttribute;
            set => this.SetProperty(ref this.targetAttribute, value);
        }

        private string value = string.Empty;

        [XmlAttribute(AttributeName = "value")]
        public string Value
        {
            get => this.value;
            set => this.SetProperty(ref this.value, value);
        }

        public override string ToString() => $"target-element={this.TargetElement}, target-attr={this.TargetAttribute}, value={this.Value}";
    }
}
