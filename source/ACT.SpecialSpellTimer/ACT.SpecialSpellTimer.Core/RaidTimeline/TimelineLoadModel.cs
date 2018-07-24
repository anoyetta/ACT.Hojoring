using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [Serializable]
    [XmlType(TypeName = "load")]
    public class TimelineLoadModel :
        TimelineBase
    {
        [XmlIgnore]
        public override TimelineElementTypes TimelineType => TimelineElementTypes.Load;

        public override IList<TimelineBase> Children => null;

        private string target = null;

        [XmlAttribute(AttributeName = "target")]
        public string Target
        {
            get => this.target;
            set => this.SetProperty(ref this.target, value);
        }

        private bool isTruncate = false;

        [XmlAttribute(AttributeName = "truncate")]
        [DefaultValue(false)]
        public bool IsTruncate
        {
            get => this.isTruncate;
            set => this.SetProperty(ref this.isTruncate, value);
        }
    }
}
