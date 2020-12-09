using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.RazorModel
{
    public class TimelineTables
    {
        public static Func<string, TimelineTable> GetTableDelegate { get; set; }

        public TimelineTable this[string name] => GetTableDelegate?.Invoke(name);
    }

    public class TimelineTable :
        BindableBase
    {
        public static readonly object TableLocker = new object();

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
                lock (TableLocker)
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

            lock (TableLocker)
            {
                try
                {
                    this.isSuspendRefreshPlaceholders = true;

                    foreach (var row in rows)
                    {
                        var current = this.rows
                            .FirstOrDefault(x => object.Equals(x.KeyValue, row.KeyValue));

                        if (current == null)
                        {
                            row.ParentTable = this;
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
                finally
                {
                    this.isSuspendRefreshPlaceholders = false;
                }

                this.RefreshPlaceholders();
            }
        }

        public void Remove(
            object keyValue)
        {
            if (keyValue == null)
            {
                return;
            }

            lock (TableLocker)
            {
                var toRemove = this.rows
                    .Where(x => object.Equals(x.KeyValue, keyValue))
                    .ToArray();

                try
                {
                    this.isSuspendRefreshPlaceholders = true;

                    foreach (var row in toRemove)
                    {
                        row.ParentTable = null;
                        this.rows.Remove(row);
                    }
                }
                finally
                {
                    this.isSuspendRefreshPlaceholders = false;
                }

                if (toRemove.Any())
                {
                    this.RefreshPlaceholders();
                }
            }
        }

        public void Truncate()
        {
            lock (TableLocker)
            {
                if (this.rows.Count < 1)
                {
                    return;
                }

                try
                {
                    this.isSuspendRefreshPlaceholders = true;

                    this.rows.Clear();
                }
                finally
                {
                    this.isSuspendRefreshPlaceholders = false;
                }

                this.RefreshPlaceholders();
            }
        }

        private readonly List<(string Placeholder, object Value)> Placeholders = new List<(string Placeholder, object Value)>(128);
        private volatile bool isSuspendRefreshPlaceholders = false;

        public void RefreshPlaceholders()
        {
            if (this.isSuspendRefreshPlaceholders)
            {
                return;
            }

            var list = new List<(string Placeholder, object Value)>(128);

            var rows = this.Rows;

            // COUNT
            list.Add(($"COUNT('{this.Name}')", rows.Count));

            var i = 0;
            foreach (var row in rows)
            {
                var cols = row.Cols.Values;
                foreach (var col in cols)
                {
                    // TABLE Value
                    list.Add(($"TABLE['{this.Name}'][{i}]['{col.Name}']", col.Value));
                }

                i++;
            }

            lock (this.Placeholders)
            {
                this.Placeholders.Clear();
                this.Placeholders.AddRange(list);
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
        public IEnumerable<(string Placeholder, object Value)> GetPlaceholders() => this.Placeholders.ToArray();
    }

    public class TimelineRow :
        BindableBase
    {
        public TimelineTable ParentTable { get; set; }

        private readonly ConcurrentDictionary<string, TimelineColumn> columns = new ConcurrentDictionary<string, TimelineColumn>();

        public object this[string name] => this.columns.ContainsKey(name) ?
            this.columns[name].Value :
            null;

        public IReadOnlyDictionary<string, TimelineColumn> Cols => this.columns;

        public void AddCol(
            TimelineColumn col)
        {
            col.ParentRow = this;
            this.columns[col.Name] = col;
            this.ParentTable?.RefreshPlaceholders();
        }

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
            this.PropertyChanged += (_, __) => this.ParentRow?.ParentTable?.RefreshPlaceholders();
        }

        public TimelineColumn(
            string name,
            object value,
            bool isKey = false) : this()
        {
            this.name = name;
            this.value = value;
            this.isKey = isKey;
        }

        public TimelineRow ParentRow { get; set; }

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
