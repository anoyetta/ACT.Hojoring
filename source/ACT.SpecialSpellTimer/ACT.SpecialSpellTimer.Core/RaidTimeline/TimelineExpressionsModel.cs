using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [XmlType(TypeName = "expresions")]
    [Serializable]
    public class TimelineExpressionsModel :
        TimelineBase
    {
        #region TimelineBase

        public override TimelineElementTypes TimelineType => TimelineElementTypes.Expressions;

        public override IList<TimelineBase> Children => this.statements;

        #endregion TimelineBase

        #region Children

        private List<TimelineBase> statements = new List<TimelineBase>();

        [XmlIgnore]
        public IReadOnlyList<TimelineBase> Statements => this.statements;

        [XmlElement(ElementName = "set")]
        public TimelineExpressionsSetModel[] SetStatements
        {
            get => this.Statements
                .Where(x => x.TimelineType == TimelineElementTypes.ExpressionsSet)
                .Cast<TimelineExpressionsSetModel>()
                .ToArray();

            set => this.AddRange(value);
        }

        [XmlElement(ElementName = "pre")]
        public TimelineExpressionsPredicateModel[] PredicateStatements
        {
            get => this.Statements
                .Where(x => x.TimelineType == TimelineElementTypes.ExpressionsPredicate)
                .Cast<TimelineExpressionsPredicateModel>()
                .ToArray();

            set => this.AddRange(value);
        }

        public void Add(TimelineBase timeline)
        {
            if (timeline.TimelineType == TimelineElementTypes.ExpressionsSet ||
                timeline.TimelineType == TimelineElementTypes.ExpressionsPredicate)
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

        /// <summary>
        /// フラグ格納領域
        /// </summary>
        private static readonly Dictionary<string, (bool Value, DateTime Expiration)> Flags = new Dictionary<string, (bool, DateTime)>();

        /// <summary>
        /// すべてのフラグを消去する
        /// </summary>
        public static void Clear() => Flags.Clear();

        /// <summary>
        /// 配下のSetステートメントを実行する
        /// </summary>
        public void Set()
        {
            var sets = this.SetStatements
                .Where(x =>
                    x.Enabled.GetValueOrDefault() &&
                    !string.IsNullOrEmpty(x.Name));

            foreach (var set in sets)
            {
                var expretion = DateTime.MaxValue;
                if (set.TTL > 0)
                {
                    expretion = DateTime.Now.AddSeconds(set.TTL.GetValueOrDefault());
                }

                Flags[set.Name] =
                    (set.Value.GetValueOrDefault(), expretion);
            }
        }

        /// <summary>
        /// 配下のPredicateを実行して結果を返す
        /// </summary>
        /// <returns>真偽</returns>
        public bool Predicate()
        {
            var states = this.PredicateStatements
                .Where(x =>
                    x.Enabled.GetValueOrDefault() &&
                    !string.IsNullOrEmpty(x.Name));

            if (!states.Any())
            {
                return true;
            }

            var result = true;

            foreach (var pre in states)
            {
                if (!Flags.ContainsKey(pre.Name))
                {
                    return false;
                }

                var flag = false;

                var container = Flags[pre.Name];
                if (DateTime.Now <= container.Expiration)
                {
                    flag = container.Value;
                }

                result &= (pre.Value.GetValueOrDefault() == flag);
            }

            return result;
        }
    }

    [XmlType(TypeName = "set")]
    [Serializable]
    public class TimelineExpressionsSetModel :
        TimelineBase
    {
        #region TimelineBase

        public override TimelineElementTypes TimelineType => TimelineElementTypes.ExpressionsSet;

        public override IList<TimelineBase> Children => null;

        #endregion TimelineBase

        private bool? value = null;

        [XmlIgnore]
        public bool? Value
        {
            get => this.value;
            set => this.SetProperty(ref this.value, value);
        }

        [XmlAttribute(AttributeName = "value")]
        public string ValueXML
        {
            get => this.Value?.ToString();
            set => this.Value = bool.TryParse(value, out var v) ? v : (bool?)null;
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

    [XmlType(TypeName = "pre")]
    [Serializable]
    public class TimelineExpressionsPredicateModel :
        TimelineBase
    {
        #region TimelineBase

        public override TimelineElementTypes TimelineType => TimelineElementTypes.ExpressionsPredicate;

        public override IList<TimelineBase> Children => null;

        #endregion TimelineBase

        private bool? value = null;

        [XmlIgnore]
        public bool? Value
        {
            get => this.value;
            set => this.SetProperty(ref this.value, value);
        }

        [XmlAttribute(AttributeName = "value")]
        public string ValueXML
        {
            get => this.Value?.ToString();
            set => this.Value = bool.TryParse(value, out var v) ? v : (bool?)null;
        }
    }
}
