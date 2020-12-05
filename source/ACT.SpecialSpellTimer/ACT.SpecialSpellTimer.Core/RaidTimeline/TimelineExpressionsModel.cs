using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.RazorModel;
using FFXIV.Framework.Extensions;

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

        [XmlElement(ElementName = "table")]
        public TimelineExpressionsTableModel[] TableStatements
        {
            get => this.Statements
                .Where(x => x.TimelineType == TimelineElementTypes.ExpressionsTable)
                .Cast<TimelineExpressionsTableModel>()
                .ToArray();

            set => this.AddRange(value);
        }

        public void Add(TimelineBase timeline)
        {
            if (timeline.TimelineType == TimelineElementTypes.ExpressionsSet ||
                timeline.TimelineType == TimelineElementTypes.ExpressionsPredicate ||
                timeline.TimelineType == TimelineElementTypes.ExpressionsTable)
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

        public delegate void TableChangedHandler(EventArgs args);

        [field: NonSerialized]
        public static event VariableChangedHandler OnVariableChanged;

        [field: NonSerialized]
        public static event TableChangedHandler OnTableChanged;

        /// <summary>
        /// フラグ格納領域
        /// </summary>
        private static readonly Dictionary<string, TimelineVariable> Variables = new Dictionary<string, TimelineVariable>(128);

        /// <summary>
        /// テーブル格納領域
        /// </summary>
        private static readonly Dictionary<string, TimelineTable> Tables = new Dictionary<string, TimelineTable>(16);

        /// <summary>
        /// テーブルが存在するか？
        /// </summary>
        public static bool IsExistsTables
        {
            get;
            private set;
        }

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
        /// テーブルを取得する
        /// </summary>
        /// <param name="name">テーブル名</param>
        /// <returns>
        /// テーブル</returns>
        public static TimelineTable GetTable(string name)
        {
            lock (ExpressionLocker)
            {
                if (Tables.ContainsKey(name))
                {
                    IsExistsTables = true;
                    return Tables[name];
                }
                else
                {
                    var table = new TimelineTable()
                    {
                        Name = name
                    };

                    Tables[name] = table;
                    IsExistsTables = true;
                    OnTableChanged?.Invoke(new EventArgs());

                    return table;
                }
            }
        }

        /// <summary>
        /// テーブルを取得する
        /// </summary>
        /// <param name="name">テーブル名</param>
        /// <returns>
        /// テーブル</returns>
        public static IReadOnlyList<TimelineTable> GetTables()
        {
            lock (ExpressionLocker)
            {
                var tables = Tables.Values.ToList();
                IsExistsTables = tables.Count > 0;
                return tables;
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
                        $"{TimelineConstants.LogSymbol} clear VAR['{item.Key}']");
                }

                if (targets.Length > 0)
                {
                    OnVariableChanged?.Invoke(new EventArgs());
                }

                if (Tables.Count > 0)
                {
                    Tables.Clear();
                    IsExistsTables = false;
                    TimelineController.RaiseLog(
                        $"{TimelineConstants.LogSymbol} clear Tables.");

                    OnTableChanged?.Invoke(new EventArgs());
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
                        $"{TimelineConstants.LogSymbol} clear all variables.");

                    OnVariableChanged?.Invoke(new EventArgs());
                }

                if (Tables.Count > 0)
                {
                    Tables.Clear();
                    IsExistsTables = false;
                    TimelineController.RaiseLog(
                        $"{TimelineConstants.LogSymbol} clear Tables.");

                    OnTableChanged?.Invoke(new EventArgs());
                }
            }
        }

        /// <summary>
        /// 配下のSetステートメントを実行する
        /// </summary>
        public void Set(
            Match matched)
        {
            var sets = this.SetStatements
                .Where(x =>
                    x.Enabled.GetValueOrDefault() &&
                    !string.IsNullOrEmpty(x.Name));

            var tables = this.TableStatements
                .Where(x =>
                    x.Enabled.GetValueOrDefault());

            if (!sets.Any() &&
                !tables.Any())
            {
                return;
            }

            var isVaribleChanged = false;
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
                    variable.Value = ObjectComparer.ConvertToValue(set.Value, matched);
                }

                // カウンタを更新する
                variable.Counter = set.ExecuteCount(variable.Counter);

                // 有効期限を設定する
                variable.Expiration = expretion;

                // フラグの状況を把握するためにログを出力する
                TimelineController.RaiseLog(
                    string.IsNullOrEmpty(set.Count) ?
                    $"{TimelineConstants.LogSymbol} set VAR['{set.Name}'] = {variable.Value}" :
                    $"{TimelineConstants.LogSymbol} set VAR['{set.Name}'] = {variable.Counter}");

                isVaribleChanged = true;
            }

            // table を処理する
            var isTablechanged = false;
            foreach (var table in tables)
            {
                isTablechanged |= table.Execute(
                    table.ParseJson(matched),
                    (x) => TimelineController.RaiseLog($"{TimelineConstants.LogSymbol} {x}"));
            }

            if (isVaribleChanged)
            {
                OnVariableChanged?.Invoke(new EventArgs());
            }

            if (isTablechanged)
            {
                OnTableChanged?.Invoke(new EventArgs());
            }
        }

        /// <summary>
        /// 配下のPredicateを実行して結果を返す
        /// </summary>
        /// <returns>真偽</returns>
        public bool Predicate(
            Match matched)
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
                var result = false;

                if (TableDMLKeywords.Any(x => pre.Name.Contains(x)))
                {
                    result = this.PredicateTable(pre, matched);
                }
                else
                {
                    result = this.PredicateValue(pre, matched);
                }

                totalResult &= result;
            }

            return totalResult;
        }

        private bool PredicateValue(
            TimelineExpressionsPredicateModel pre,
            Match matched)
        {
            var result = false;
            var log = string.Empty;

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

                result = ObjectComparer.PredicateValue(current, pre.Value, matched, out string value);
                log = $"predicate ['{pre.Name}':{current}] equal [{value}] -> {result}";
            }
            else
            {
                var value = pre.Count.GetValueOrDefault();
                result = (value == variable.Counter);
                log = $"predicate ['{pre.Name}':{variable.Counter}] equal [{value}] -> {result}";
            }

            TimelineController.RaiseLog($"{TimelineConstants.LogSymbol} {log}");

            return result;
        }

        /// <summary>
        /// テーブル変数の参照書式
        /// </summary>
        /// <example>
        /// TABLE['table_name'][index]['column_name']
        /// </example>
        private static readonly Regex TableVarRegex = new Regex(
            @$"{TableVarKeyword}\['(?<TableName>.+)'\]\[(?<Index>\d+)\]\['(?<ColName>.+)'\]",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase);

        /// <summary>
        /// COUNT関数書式
        /// </summary>
        /// <example>
        /// COUNT('table_name')
        /// </example>
        private static readonly Regex CountTableRegex = new Regex(
            @$"{CountFunctionKeyword}\('(?<TableName>.+)'\)",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase);

        private static readonly string TableVarKeyword = "TABLE";
        private static readonly string CountFunctionKeyword = "COUNT";

        private static readonly string[] TableDMLKeywords = new[]
        {
            $"{TableVarKeyword}[",
            $"{CountFunctionKeyword}(",
        };

        /// <summary>
        /// テーブル関連の値を取得する
        /// </summary>
        /// <param name="keyword">キーワード</param>
        /// <returns>取得した値</returns>
        public static object GetTableValue(
            string keyword)
        {
            object value = null;

            if (!TableDMLKeywords.Any(x => keyword.ContainsIgnoreCase(x)))
            {
                return value;
            }

            // フィールドの参照？
            var match = TableVarRegex.Match(keyword);
            if (match.Success)
            {
                var tableName = match.Groups["TableName"].Value;
                var indexText = match.Groups["Index"].Value;
                var colName = match.Groups["ColName"].Value;

                if (int.TryParse(indexText, out int index))
                {
                    lock (TimelineTable.TableLocker)
                    {
                        var table = TimelineExpressionsModel.GetTable(tableName);
                        if (table.Rows.Count > index)
                        {
                            var row = table.Rows[index];
                            if (row.Cols.ContainsKey(colName))
                            {
                                value = row.Cols[colName].Value;
                            }
                        }
                    }
                }

                return value;
            }

            // COUNT関数？
            match = CountTableRegex.Match(keyword);
            if (match.Success)
            {
                lock (TimelineTable.TableLocker)
                {
                    var tableName = match.Groups["TableName"].Value;
                    var table = TimelineExpressionsModel.GetTable(tableName);
                    value = table.Rows.Count;
                }

                return value;
            }

            return value;
        }

        /// <summary>
        /// テーブル変数と評価する
        /// </summary>
        /// <param name="pre">predicateオブジェクト</param>
        /// <returns>評価結果</returns>
        private bool PredicateTable(
            TimelineExpressionsPredicateModel pre,
            Match matched)
        {
            var log = string.Empty;

            var current = GetTableValue(pre.Name) ?? false;
            var result = ObjectComparer.PredicateValue(current, pre.Value, matched, out string value);

            log = $"predicate table variable [{pre.Name}:{current}] equal [{value}] -> {result}";
            TimelineController.RaiseLog($"{TimelineConstants.LogSymbol} {log}");

            return result;
        }

        /// <summary>
        /// テキストに含まれるプレースホルダを変数値に置き換える
        /// </summary>
        /// <param name="text">
        /// インプットテキスト</param>
        /// <returns>
        /// 置換後のテキスト</returns>
        public static string ReplaceText(
            string text)
        {
            if (text == null)
            {
                text = string.Empty;
            }

            var placeholders = TimelineManager.Instance.GetPlaceholders();
            foreach (var ph in placeholders)
            {
                text = text.Replace(
                    ph.Placeholder,
                    ph.ReplaceString);
            }

            foreach (var item in Variables)
            {
                var variable = item.Value;

                if (DateTime.Now <= variable.Expiration)
                {
                    text = variable.Replace(text);
                }
            }

            if (IsExistsTables &&
                text.ContainsIgnoreCase("TABLE"))
            {
                var tables = GetTables();

                foreach (var table in tables)
                {
                    foreach (var ph in table.GetPlaceholders())
                    {
                        var valueText = ph.Value?.ToString();

                        if (!string.IsNullOrEmpty(valueText))
                        {
                            text = text.Replace(ph.Placeholder, valueText);
                        }
                    }
                }
            }

            return text;
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
    }

    public static class ObjectComparer
    {
        public static object ConvertToValue(
            object o,
            Match matched = null)
        {
            if (o == null)
            {
                return false;
            }

            var t = o.ToString();

            // 変数を置換する
            t = TimelineExpressionsModel.ReplaceText(t);

            // 正規表現を置換する
            if (matched != null &&
                matched.Success)
            {
                t = matched.Result(t);
            }

            if (bool.TryParse(t, out bool b))
            {
                return b;
            }

            if (double.TryParse(t, out double d))
            {
                return d;
            }

            return t;
        }

        public static bool PredicateValue(
            object inspectionValue,
            object expectedValue,
            Match matched,
            out string expectedValueReplaced)
        {
            expectedValueReplaced = string.Empty;
            var t1 = inspectionValue?.ToString() ?? false.ToString();
            var t2 = expectedValue?.ToString() ?? false.ToString();

            // 変数を置換する
            t2 = TimelineExpressionsModel.ReplaceText(t2);

            // 正規表現を置換する
            if (matched != null &&
                matched.Success)
            {
                t2 = matched.Result(t2);
            }

            expectedValueReplaced = t2;

            if (double.TryParse(t2, out double d2))
            {
                if (!double.TryParse(t1, out double d1))
                {
                    d1 = 0;
                }

                return d1 == d2;
            }

            if (bool.TryParse(t2, out bool b2))
            {
                bool.TryParse(t1, out bool b1);
                return b1 == b2;
            }

            return string.Equals(t1, t2, StringComparison.OrdinalIgnoreCase);
        }

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
