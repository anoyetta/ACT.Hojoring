using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.RazorModel;
using FFXIV.Framework.Common;
using Hjson;
using Newtonsoft.Json;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    public partial class TimelineExpressionsTableModel :
        TimelineBase
    {
        #region Logger

        private NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        #region TimelineBase

        public override TimelineElementTypes TimelineType => TimelineElementTypes.ExpressionsTable;

        public override IList<TimelineBase> Children => null;

        #endregion TimelineBase

        private string jsonText;

        [XmlText]
        public string JsonText
        {
            get => this.jsonText;
            set => this.SetProperty(ref this.jsonText, value);
        }

        private TimelineExpressionsTableJsonModel model;

        [XmlIgnore]
        public TimelineExpressionsTableJsonModel Model
        {
            get => this.model;
            set => this.SetProperty(ref this.model, value);
        }

        public void ParseJson()
        {
            if (string.IsNullOrEmpty(this.JsonText))
            {
                this.Model = null;
                return;
            }

            var parent = this.Parent?.Parent;

            try
            {
                var matched = parent switch
                {
                    TimelineActivityModel a => a.SyncMatch,
                    TimelineTriggerModel t => t.SyncMatch,
                    _ => null,
                };

                var json = ObjectComparer.ConvertToValue(this.JsonText, matched).ToString();

                // HJSON -> JSON
                json = HjsonValue.Parse(json).ToString();

                // JSON Parse
                this.Model = JsonConvert.DeserializeObject<TimelineExpressionsTableJsonModel>(json);
            }
            catch (Exception ex)
            {
                this.Model = null;

                var parentName = string.IsNullOrEmpty(parent.Name) ?
                    (parent as dynamic).Text :
                    parent.Name;

                this.AppLogger.Error(
                    ex,
                    $"[TL] Error on parsing table JSON. parent={parentName}.\n{this.JsonText}");
            }
        }

        public bool Execute(
            Action<string> raiseLog = null)
        {
            if (this.Model == null)
            {
                return false;
            }

            var result = false;
            var parent = this.Parent?.Parent;
            var model = this.Model;

            try
            {
                var table = TimelineExpressionsModel.GetTable(model.Table);

                var key = model.Cols
                    .FirstOrDefault(x => x.IsKey)?
                    .Val ?? null;

                if (model.Method == TalbeJsonMethods.Delete)
                {
                    table.Remove(key);
                    raiseLog?.Invoke($"Delete row from TABLE['{model.Table}'] by key='{key.ToString()}'");
                }
                else
                {
                    var row = new TimelineRow();

                    var logs = new List<string>();
                    var keyLog = string.Empty;

                    foreach (var col in model.Cols)
                    {
                        row.Cols[col.Name] = new TimelineColumn(
                            col.Name,
                            col.Val,
                            col.IsKey);

                        logs.Add($"{col.Name}:'{col.Val}'");

                        if (col.IsKey)
                        {
                            keyLog = $"key='{col.Name}'";
                        }
                    }

                    table.Add(row);

                    var colLog = string.Join(", ", logs.ToArray());
                    raiseLog?.Invoke($"Merge row into TABLE['{model.Table}'] cols ({colLog}) {keyLog}");
                }

                result = true;
            }
            catch (Exception ex)
            {
                var parentName = string.IsNullOrEmpty(parent.Name) ?
                    (parent as dynamic).Text :
                    parent.Name;

                this.AppLogger.Error(
                    ex,
                    $"[TL] Error on execute table JSON. parent={parentName}.\n{this.JsonText}");

                result = false;
            }

            return result;
        }
    }

    public enum TalbeJsonMethods
    {
        Insert,
        Update,
        Delete
    }

    [JsonObject]
    public class TimelineExpressionsTableJsonModel :
        BindableBase
    {
        private TalbeJsonMethods method = TalbeJsonMethods.Insert;

        [JsonProperty(PropertyName = "method")]
        public TalbeJsonMethods Method
        {
            get => this.method;
            set => this.SetProperty(ref this.method, value);
        }

        private string table;

        [JsonProperty(PropertyName = "table")]
        public string Table
        {
            get => this.table;
            set => this.SetProperty(ref this.table, value);
        }

        private TimelineExpressionsColumnJsonModel[] cols;

        [JsonProperty(PropertyName = "cols")]
        public TimelineExpressionsColumnJsonModel[] Cols
        {
            get => this.cols;
            set => this.SetProperty(ref this.cols, value);
        }
    }

    [JsonObject]
    public class TimelineExpressionsColumnJsonModel :
        BindableBase
    {
        private string name;

        [JsonProperty(PropertyName = "name")]
        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        private object val;

        [JsonProperty(PropertyName = "val")]
        public object Val
        {
            get => this.val;
            set => this.SetProperty(ref this.val, value);
        }

        private bool isKey;

        [JsonProperty(PropertyName = "key")]
        public bool IsKey
        {
            get => this.isKey;
            set => this.SetProperty(ref this.isKey, value);
        }
    }
}
