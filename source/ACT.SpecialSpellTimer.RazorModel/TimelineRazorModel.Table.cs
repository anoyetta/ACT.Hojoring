using System.Collections.Generic;
using System.Linq;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.RazorModel
{
    public class TimelineTable :
        BindableBase
    {
        private string name;

        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        private readonly List<TimelineRow> rows = new List<TimelineRow>(16);

        public IReadOnlyList<TimelineRow> Rows
        {
            get
            {
                lock (this)
                {
                    return this.rows.OrderBy(x => x.KeyValue?.ToString()).ToList();
                }
            }
        }

        public void Add(
            params TimelineRow[] rows)
        {
            if (rows == null ||
                rows.Length < 1)
            {
                return;
            }

            lock (this)
            {
                foreach (var row in rows)
                {
                    var current = this.rows
                        .FirstOrDefault(x => object.Equals(x.KeyValue, row.KeyValue));

                    if (current == null)
                    {
                        this.rows.Add(row);
                        continue;
                    }

                    foreach (var col in row.Cols.Values)
                    {
                        var currentCol = current.Cols[col.Name];
                        currentCol.Value = col.Value;
                        currentCol.IsKey = col.IsKey;
                    }
                }
            }
        }

        public void Remove(
            object keyValue)
        {
            if (keyValue == null)
            {
                return;
            }

            lock (this)
            {
                this.rows.RemoveAll(x => object.Equals(x.KeyValue, keyValue));
            }
        }

        /// <summary>
        /// 当該テーブル含まれる値のプレースホルダを生成する
        /// </summary>
        /// <returns>
        /// プレースホルダのリスト</returns>
        /// <example>
        /// TABLE['table_name'][index]['column_name']
        /// COUNT('table_name')
        /// </example>
        public IEnumerable<(string Placeholder, object Value)> GetPlaceholders()
        {
            var rows = this.Rows;

            // COUNT
            yield return ($"COUNT('{this.Name}')", rows.Count);

            var i = 0;
            foreach (var row in rows)
            {
                var cols = row.Cols.Values;
                foreach (var col in cols)
                {
                    // TABLE Value
                    yield return ($"TABLE['{this.Name}'][{i}]['{col.Name}']", col.Value);
                }

                i++;
            }
        }
    }

    public class TimelineRow :
        BindableBase
    {
        private readonly Dictionary<string, TimelineColumn> columns = new Dictionary<string, TimelineColumn>(16);

        public object this[string name] => this.columns.ContainsKey(name) ?
            this.columns[name].Value :
            null;

        public Dictionary<string, TimelineColumn> Cols => this.columns;

        public object KeyValue => this.columns
            .FirstOrDefault(x => x.Value.IsKey)
            .Value?
            .Value;

        public override string ToString() =>
            "{ " +
            string.Join(
                ", ",
                this.Cols
                    .OrderBy(x => x.Value.IsKey ? 0 : 1)
                    .Select(x => x.ToString())) +
            " }";
    }

    public class TimelineColumn :
        BindableBase
    {
        public TimelineColumn()
        {
        }

        public TimelineColumn(
            string name,
            object value,
            bool isKey = false)
        {
            this.Name = name;
            this.Value = value;
            this.IsKey = isKey;
        }

        private string name;

        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        private object value;

        public object Value
        {
            get => this.value;
            set => this.SetProperty(ref this.value, value);
        }

        private bool isKey;

        public bool IsKey
        {
            get => this.isKey;
            set => this.SetProperty(ref this.isKey, value);
        }

        public override string ToString() => $"\"{this.name}\":\"{this.value?.ToString()}\"";
    }
}
