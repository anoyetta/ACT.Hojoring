using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [XmlType(TypeName = "pre")]
    [Serializable]
    public class TimelineExpressionsPredicateModel :
        TimelineBase
    {
        #region TimelineBase

        public override TimelineElementTypes TimelineType => TimelineElementTypes.ExpressionsPredicate;

        public override IList<TimelineBase> Children => null;

        #endregion TimelineBase

        private object value = null;

        [XmlIgnore]
        public object Value
        {
            get => this.value;
            set => this.SetProperty(ref this.value, value);
        }

        [XmlAttribute(AttributeName = "value")]
        public string ValueXML
        {
            get => this.Value?.ToString();
            set => this.Value = ObjectComparer.ConvertToValue(value);
        }

        private int? count = null;

        [XmlIgnore]
        public int? Count
        {
            get => this.count;
            set => this.SetProperty(ref this.count, value);
        }

        [XmlAttribute(AttributeName = "count")]
        public string CountXML
        {
            get => this.Count?.ToString();
            set => this.Count = int.TryParse(value, out var v) ? v : (int?)null;
        }

        [XmlIgnore]
        public string LastestLog { get; set; } = string.Empty;
    }
}
