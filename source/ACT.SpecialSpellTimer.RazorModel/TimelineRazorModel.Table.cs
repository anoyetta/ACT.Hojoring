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

                    foreach (var col in row.Col.Values)
                    {
                        var currentCol = current.Col[col.Name];
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
    }

    public class TimelineRow :
        BindableBase
    {
        private readonly Dictionary<string, TimelineColumn> columns = new Dictionary<string, TimelineColumn>(16);

        public object this[string name] => this.columns.ContainsKey(name) ?
            this.columns[name].Value :
            null;

        public Dictionary<string, TimelineColumn> Col => this.columns;

        public object KeyValue => this.columns
            .FirstOrDefault(x => x.Value.IsKey)
            .Value;
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
    }
}
