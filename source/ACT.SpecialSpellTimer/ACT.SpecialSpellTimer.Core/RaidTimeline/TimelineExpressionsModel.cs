using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using FFXIV.Framework.FFXIVHelper;

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

        #region Global Variable

        /// <summary>
        /// TargetOfTargetが自分か？
        /// </summary>
        public const string IS_TOT_ME = "IS_TOT_ME";

        /// <summary>
        /// IS_TOT_ME を更新する
        /// </summary>
        public static void RefreshIsToTMe()
        {
            var player = FFXIVPlugin.Instance.GetPlayer();
            if (player != null)
            {
                var value = player.IsTargetOfTargetMe;
                if (SetGlobal(IS_TOT_ME, value))
                {
                    TimelineController.RaiseLog(
                        $"{TimelineController.TLSymbol} set ENV[\"{IS_TOT_ME}\"] = {value}");
                }
            }
        }

        /// <summary>
        /// グローバル変数をセットする
        /// </summary>
        /// <param name="name">グローバル変数名</param>
        /// <param name="value">値</param>
        /// <returns>is changed</returns>
        public static bool SetGlobal(
            string name,
            object value)
        {
            var result = false;

            lock (ExpressionLocker)
            {
                var flag = default(Flag);
                if (Flags.ContainsKey(name))
                {
                    flag = Flags[name];
                }
                else
                {
                    flag = new Flag();
                    Flags[name] = flag;
                    result = true;
                }

                switch (value)
                {
                    case bool b:
                        if (flag.Value != b)
                        {
                            flag.Value = b;
                            flag.Expiration = DateTime.MaxValue;
                            result = true;
                        }
                        break;

                    case int i:
                        if (flag.Counter != i)
                        {
                            flag.Counter = i;
                            flag.Expiration = DateTime.MaxValue;
                            result = true;
                        }
                        break;
                }
            }

            return result;
        }

        #endregion Global Variable

        public static readonly object ExpressionLocker = new object();

        /// <summary>
        /// フラグ格納領域
        /// </summary>
        private static readonly Dictionary<string, Flag> Flags = new Dictionary<string, Flag>(128);

        /// <summary>
        /// すべてのフラグを消去する
        /// </summary>
        public static void Clear()
        {
            lock (ExpressionLocker)
            {
                var any = Flags.Any();

                Flags.Clear();

                if (any)
                {
                    TimelineController.RaiseLog(
                        $"{TimelineController.TLSymbol} cleared all Flags.");
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

                var flag = default(Flag);
                if (Flags.ContainsKey(set.Name))
                {
                    flag = Flags[set.Name];
                }
                else
                {
                    flag = new Flag();
                    Flags[set.Name] = flag;
                }

                // トグル？
                if (set.IsToggle.GetValueOrDefault())
                {
                    var current = false;
                    if (DateTime.Now <= flag.Expiration)
                    {
                        current = flag.Value;
                    }

                    flag.Value = current ^ true;
                }
                else
                {
                    flag.Value = set.Value.GetValueOrDefault();
                }

                // カウンタを更新する
                flag.Counter = set.ExecuteCount(flag.Counter);

                // フラグの状況を把握するためにログを出力する
                TimelineController.RaiseLog(
                    string.IsNullOrEmpty(set.Count) ?
                    $"{TimelineController.TLSymbol} set Flags[\"{set.Name}\"] = {flag.Value}" :
                    $"{TimelineController.TLSymbol} set Flags[\"{set.Name}\"] = {flag.Counter}");
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

            var totalResult = true;
            foreach (var pre in states)
            {
                var flag = Flags.ContainsKey(pre.Name) ?
                    Flags[pre.Name] :
                    Flag.EmptyFlag;

                if (!pre.Count.HasValue)
                {
                    var value = false;
                    if (DateTime.Now <= flag.Expiration)
                    {
                        value = flag.Value;
                    }

                    totalResult &= (pre.Value.GetValueOrDefault() == value);
                }
                else
                {
                    totalResult &= (pre.Count.GetValueOrDefault() == flag.Counter);
                }
            }

            return totalResult;
        }

        public class Flag
        {
            /// <summary>
            /// 空フラグ
            /// </summary>
            public static readonly Flag EmptyFlag = new Flag();

            public Flag()
            {
            }

            public Flag(
                int counter)
            {
                this.Counter = counter;
            }

            public Flag(
                bool value,
                DateTime expiration)
            {
                this.Value = value;
                this.Expiration = expiration;
            }

            public bool Value { get; set; } = false;

            public int Counter { get; set; } = 0;

            public DateTime Expiration { get; set; } = DateTime.MaxValue;
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
}
