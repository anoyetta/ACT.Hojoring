using System;
using System.Collections.Generic;
using System.Data;
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
        /// 変数・テーブルを参照しているトリガをリコンパイルするためのデリゲート
        /// </summary>
        public static readonly Dictionary<string, Action> ReferedTriggerRecompileDelegates = new Dictionary<string, Action>();

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

                variable.Zone = set.Scope switch
                {
                    TimelineExpressionsSetModel.Scopes.CurrentZone => TimelineController.CurrentController?.CurrentZoneName ?? string.Empty,
                    TimelineExpressionsSetModel.Scopes.Global => TimelineModel.GlobalZone,
                    _ => string.Empty
                };

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
                    var newValue = ObjectComparer.ConvertToValue(set.Value, matched);

                    if (!ObjectComparer.Equals(variable.Value, newValue))
                    {
                        variable.Value = newValue;
                        isVaribleChanged = true;
                    }
                }

                // カウンタを更新する
                if (!string.IsNullOrEmpty(set.Count))
                {
                    variable.Counter = set.ExecuteCount(variable.Counter);
                    isVaribleChanged = true;
                }

                // 有効期限を設定する
                variable.Expiration = expretion;

                if (isVaribleChanged)
                {
                    // フラグの状況を把握するためにログを出力する
                    TimelineController.RaiseLog(
                        string.IsNullOrEmpty(set.Count) ?
                        $"{TimelineConstants.LogSymbol} set VAR['{set.Name}'] = {variable.Value}" :
                        $"{TimelineConstants.LogSymbol} set VAR['{set.Name}'] = {variable.Counter}");

                    if (ReferedTriggerRecompileDelegates.ContainsKey(set.Name))
                    {
                        lock (variable)
                        {
                            ReferedTriggerRecompileDelegates[set.Name]?.Invoke();
                        }
                    }
                }
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

            if (pre.LastestLog != log)
            {
                TimelineController.RaiseLog($"{TimelineConstants.LogSymbol} {log}");
            }

            pre.LastestLog = log;

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
        /// <param name="input">
        /// インプットテキスト</param>
        /// <returns>
        /// 置換後のテキスト</returns>
        public static string ReplaceText(
            string input)
        {
            var text = input;

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

            foreach (var item in GetVariables())
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
                    var before = text;

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

        private static readonly string EVALKeyword = "EVAL";

        private static readonly Regex EVALRegex = new Regex(
            @"EVAL\((?<expressions>.+?)(\s*,\s*(?<format>[^)]*))?\)",
            RegexOptions.Compiled);

        public static string ReplaceEval(
            string input)
        {
            var text = input;

            if (text == null)
            {
                text = string.Empty;
            }

            if (!text.Contains(EVALKeyword))
            {
                return input;
            }

            var matches = EVALRegex.Matches(text);
            if (matches.Count < 1)
            {
                return input;
            }

            foreach (Match match in matches)
            {
                var expressions = match.Groups["expressions"].Value;
                var format = match.Groups["format"].Value;

                expressions = expressions?
                    .Replace("'", string.Empty)
                    .Replace("\"", string.Empty);

                format = format?
                    .Replace("'", string.Empty)
                    .Replace("\"", string.Empty);

                if (string.IsNullOrEmpty(expressions))
                {
                    return input;
                }

                var result = default(object);
                var resultText = string.Empty;

                try
                {
                    result = expressions.Eval();

                    if (result == null)
                    {
                        return input;
                    }
                }
                catch (SyntaxErrorException)
                {
                    TimelineController.RaiseLog(
                        $"{TimelineConstants.LogSymbol} EVAL syntax error. expressions=\"{expressions}\"");

                    return input;
                }

                if (string.IsNullOrEmpty(format))
                {
                    resultText = result.ToString();
                }
                else
                {
                    if (result is IFormattable f)
                    {
                        resultText = f.ToString(format, null);
                    }
                    else
                    {
                        resultText = result.ToString();
                    }
                }

                text = text.Replace(match.Value, resultText);
            }

            return text;
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

            // EVAL関数を判定する
            t = TimelineExpressionsModel.ReplaceEval(t);

            if (bool.TryParse(t, out bool b))
            {
                return b;
            }

            if (int.TryParse(t, out int i))
            {
                return i;
            }

            if (double.TryParse(t, out double d))
            {
                return d;
            }

            if (t.TryParse0xString2Int(out int i2))
            {
                return i2;
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

            // EVAL関数を判定する
            t2 = TimelineExpressionsModel.ReplaceEval(t2);

            expectedValueReplaced = t2;

            // 16進数文字列を10進数文字列に変換する
            if (t2.TryParse0xString2Int(out int ii))
            {
                t2 = ii.ToString();
            }

            if (int.TryParse(t2, out int i2))
            {
                if (!int.TryParse(t1, out int i1))
                {
                    i1 = 0;
                }

                return i1 == i2;
            }

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
