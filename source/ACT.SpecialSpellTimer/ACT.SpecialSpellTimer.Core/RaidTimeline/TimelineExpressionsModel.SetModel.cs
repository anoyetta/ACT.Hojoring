using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [XmlType(TypeName = "set")]
    [Serializable]
    public class TimelineExpressionsSetModel :
        TimelineBase
    {
        public enum Scopes
        {
            None,
            CurrentZone,
            Global
        }

        #region TimelineBase

        public override TimelineElementTypes TimelineType => TimelineElementTypes.ExpressionsSet;

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

        private bool? isToggle = null;

        [XmlIgnore]
        public bool? IsToggle
        {
            get => this.isToggle;
            set => this.SetProperty(ref this.isToggle, value);
        }

        [XmlAttribute(AttributeName = "toggle")]
        public string IsToggleXML
        {
            get => this.IsToggle?.ToString();
            set => this.IsToggle = bool.TryParse(value, out var v) ? v : (bool?)null;
        }

        private string count = null;

        [XmlAttribute(AttributeName = "count")]
        public string Count
        {
            get => this.count;
            set => this.SetProperty(ref this.count, value);
        }

        private Scopes scope = TimelineExpressionsSetModel.Scopes.None;

        [XmlAttribute(AttributeName = "scope")]
        public Scopes Scope
        {
            get => this.scope;
            set => this.SetProperty(ref this.scope, value);
        }

        public int ExecuteCount(
            int counter)
        {
            var result = counter;

            if (string.IsNullOrEmpty(this.Count))
            {
                return result;
            }

            if (!int.TryParse(this.Count, out int i))
            {
                return result;
            }

            if (this.Count.StartsWith("+") ||
                this.Count.StartsWith("-"))
            {
                result += i;
            }
            else
            {
                result = i;
            }

            return result;
        }

        private double? ttl = -1;

        [XmlIgnore]
        public double? TTL
        {
            get => this.ttl;
            set => this.SetProperty(ref this.ttl, value);
        }

        [XmlAttribute(AttributeName = "ttl")]
        public string TTLXML
        {
            get => this.TTL?.ToString();
            set => this.TTL = double.TryParse(value, out var v) ? v : (double?)null;
        }
    }
}
