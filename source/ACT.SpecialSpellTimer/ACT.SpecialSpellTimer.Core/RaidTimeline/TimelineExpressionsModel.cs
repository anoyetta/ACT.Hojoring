using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.RazorModel;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [XmlType(TypeName = "expresions")]
    [Serializable]
    public partial class TimelineExpressionsModel :
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

        public static readonly object ExpressionLocker = new object();

        public delegate void VariableChangedHandler(EventArgs args);

        public static event VariableChangedHandler OnVariableChanged;

        /// <summary>
        /// フラグ格納領域
        /// </summary>
        private static readonly Dictionary<string, TimelineVariable> Variables = new Dictionary<string, TimelineVariable>(128);

        /// <summary>
        /// 変数領域のクローンを取得する
        /// </summary>
        /// <returns>
        /// Variable Dictionary</returns>
        public static IReadOnlyDictionary<string, TimelineVariable> GetVariables()
        {
            lock (ExpressionLocker)
            {
                return new Dictionary<string, TimelineVariable>(Variables);
            }
        }

        /// <summary>
        /// 一時変数とカレントゾーンと異なる変数をクリアする
        /// </summary>
        public static void Clear(
            string currentZoneName)
        {
            lock (ExpressionLocker)
            {
                var targets = Variables.Where(x =>
                    string.IsNullOrEmpty(x.Value.Zone) ||
                    (x.Value.Zone != TimelineModel.GlobalZone && x.Value.Zone != currentZoneName))
                    .ToArray();

                foreach (var item in targets)
                {
                    Variables.Remove(item.Key);
                    TimelineController.RaiseLog(
                        $"{TimelineController.TLSymbol} clear VAR[\"{item.Key}\"]");
                }

                if (targets.Length > 0)
                {
                    OnVariableChanged?.Invoke(new EventArgs());
                }
            }
        }

        /// <summary>
        /// すべてのフラグを消去する
        /// </summary>
        public static void ClearAll()
        {
            lock (ExpressionLocker)
            {
                var any = Variables.Count > 0;

                Variables.Clear();

                if (any)
                {
                    TimelineController.RaiseLog(
                        $"{TimelineController.TLSymbol} clear all variables.");

                    OnVariableChanged?.Invoke(new EventArgs());
                }
            }
        }

        /// <summary>
        /// 配下のSetステートメントを実行する
        /// </summary>
        public void Set()
        {
            var sets = this.SetStatements
                .Where(x =>
                    x.Enabled.GetValueOrDefault() &&
                    !string.IsNullOrEmpty(x.Name));

            if (!sets.Any())
            {
                return;
            }

            foreach (var set in sets)
            {
                var expretion = DateTime.MaxValue;
                if (set.TTL > 0)
                {
                    expretion = DateTime.Now.AddSeconds(set.TTL.GetValueOrDefault());
                }

                var variable = default(TimelineVariable);
                if (Variables.ContainsKey(set.Name))
                {
                    variable = Variables[set.Name];
                }
                else
                {
                    variable = new TimelineVariable(set.Name);
                    Variables[set.Name] = variable;
                }

                // トグル？
                if (set.IsToggle.GetValueOrDefault())
                {
                    var current = false;
                    if (DateTime.Now <= variable.Expiration &&
                        variable.Value is bool b)
                    {
                        current = b;
                    }

                    variable.Value = !current;
                }
                else
                {
                    var value = set.Value;

                    switch (this.Parent)
                    {
                        case TimelineActivityModel a:
                            if (a.SyncMatch?.Success ?? false)
                            {
                                value = a.SyncMatch.Result(value.ToString());
                            }
                            break;

                        case TimelineTriggerModel t:
                            if (t.SyncMatch?.Success ?? false)
                            {
                                value = t.SyncMatch.Result(value.ToString());
                            }
                            break;
                    }

                    variable.Value = value;
                }

                // カウンタを更新する
                variable.Counter = set.ExecuteCount(variable.Counter);

                // 有効期限を設定する
                variable.Expiration = expretion;

                // フラグの状況を把握するためにログを出力する
                TimelineController.RaiseLog(
                    string.IsNullOrEmpty(set.Count) ?
                    $"{TimelineController.TLSymbol} set VAR[\"{set.Name}\"] = {variable.Value}" :
                    $"{TimelineController.TLSymbol} set VAR[\"{set.Name}\"] = {variable.Counter}");
            }

            OnVariableChanged?.Invoke(new EventArgs());
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

            var totalResult = true;
            foreach (var pre in states)
            {
                var variable = Variables.ContainsKey(pre.Name) ?
                    Variables[pre.Name] :
                    TimelineVariable.EmptyVariable;

                if (!pre.Count.HasValue)
                {
                    object current = null;
                    if (DateTime.Now <= variable.Expiration)
                    {
                        current = variable.Value;
                    }

                    if (pre.Value is bool &&
                        current == null)
                    {
                        current = false;
                    }

                    totalResult &= pre.EqualsValue(current);
                }
                else
                {
                    totalResult &= (pre.Count.GetValueOrDefault() == variable.Counter);
                }
            }

            return totalResult;
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
            set
            {
                if (bool.TryParse(value, out bool b))
                {
                    this.Value = b;
                    return;
                }

                if (int.TryParse(value, out int i))
                {
                    this.Value = i;
                    return;
                }

                if (double.TryParse(value, out double d))
                {
                    this.Value = d;
                    return;
                }

                this.Value = value;
            }
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

        public int ExecuteCount(
            int counter)
        {
            var result = counter;

            if (string.IsNullOrEmpty(this.Count))
            {
                return result;
            }

            int i;
            if (!int.TryParse(this.Count, out i))
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
            set
            {
                if (bool.TryParse(value, out bool b))
                {
                    this.Value = b;
                    return;
                }

                if (int.TryParse(value, out int i))
                {
                    this.Value = i;
                    return;
                }

                if (double.TryParse(value, out double d))
                {
                    this.Value = d;
                    return;
                }

                this.Value = value;
            }
        }

        public bool EqualsValue(
            object predicateValue)
            => ObjectComparer.Equals(predicateValue, this.Value);

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
    }

    public static class ObjectComparer
    {
        public new static bool Equals(
            object x,
            object y)
        {
            var result = false;

            switch (y)
            {
                case bool b:
                    if (x is bool b2)
                    {
                        result = b == b2;
                    }
                    break;

                case int i:
                    if (x is int i2)
                    {
                        result = i == i2;
                    }
                    break;

                case double d:
                    if (x is double d2)
                    {
                        result = d == d2;
                    }
                    break;

                default:
                    result = string.Equals(
                        x?.ToString() ?? string.Empty,
                        y?.ToString() ?? string.Empty,
                        StringComparison.OrdinalIgnoreCase);
                    break;
            }

            return result;
        }
    }
}
