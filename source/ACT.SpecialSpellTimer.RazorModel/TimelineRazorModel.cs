using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using FFXIV.Framework.Extensions;
using Hjson;
using Newtonsoft.Json.Linq;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.RazorModel
{
    public class TimelineRazorModel
    {
        #region Lazy Singleton

        private static readonly Lazy<TimelineRazorModel> LazyInstance = new Lazy<TimelineRazorModel>(() => new TimelineRazorModel());

        public static TimelineRazorModel Instance => LazyInstance.Value;

        private TimelineRazorModel()
        {
        }

        #endregion Lazy Singleton

        public DateTimeOffset LT => DateTimeOffset.Now;

        public string ET { get; set; } = "00:00";

        public TimelineRazorVariable Var { get; } = new TimelineRazorVariable();

        public TimelineTables Tables { get; } = new TimelineTables();

        public bool SyncTTS { get; set; } = false;

        public int ZoneID { get; set; } = 0;

        public string Zone { get; set; } = string.Empty;

        public string Locale { get; set; } = "JA";

        public TimelineRazorPlayer Player { get; set; }

        public TimelineRazorPlayer[] Party { get; set; }

        public string TimelineDirectory => this.BaseDirectory;

        public string TimelineFile { get; private set; } = string.Empty;

        public string BaseDirectory { get; set; }

        public void UpdateCurrentTimelineFile(
            string currentTimelineFile)
            => this.TimelineFile = currentTimelineFile;

        /// <summary>
        /// 環境変数をセットする
        /// </summary>
        /// <remarks>
        /// 環境変数をセットする。
        /// 通常スコープの場合はゾーンチェンジに消去される。
        /// グローバルスコープの場合はアプリケーションの実行中、常に保持される。</remarks>
        /// <param name="name">変数名</param>
        /// <param name="value">値</param>
        /// <param name="global">グローバルスコープか？</param>
        public void SetVar(
            string name,
            object value,
            bool global = false) =>
            TimelineRazorVariable.SetVarDelegate?.Invoke(
                name,
                value,
                global ? "{GLOBAL}" : this.Zone);

        /// <summary>
        /// 一時変数をセットする
        /// </summary>
        /// <remarks>
        /// タイムラインのリセット時に消去される一時変数をセットする</remarks>
        /// <param name="name">変数名</param>
        /// <param name="value">値</param>
        public void SetTemp(
            string name,
            object value) =>
            TimelineRazorVariable.SetVarDelegate?.Invoke(
                name,
                value,
                null);

        public bool InZone(
            params string[] zones)
        {
            if (zones == null)
            {
                return false;
            }

            return zones.Any(x => this.Zone.ContainsIgnoreCase(x));
        }

        public bool InZone(
            params int[] zones)
        {
            if (zones == null)
            {
                return false;
            }

            return zones.Any(x => x == this.ZoneID);
        }

        public dynamic ParseJsonString(
            string hjson)
        {
            // HJSON -> JSON
            var json = HjsonValue.Parse(hjson).ToString();

            // JSON Parse
            return JObject.Parse(json);
        }

        public dynamic ParseJsonFile(
            string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return null;
            }

            if (!File.Exists(file))
            {
                file = Path.Combine(
                    this.BaseDirectory,
                    file);

                if (!File.Exists(file))
                {
                    return null;
                }
            }

            return this.ParseJsonString(
                File.ReadAllText(file, new UTF8Encoding(false)));
        }

        public string Include(
            string file)
        {
            var fileToLog = file;

            if (string.IsNullOrEmpty(file))
            {
                return string.Empty;
            }

            if (!File.Exists(file))
            {
                file = Path.Combine(
                    this.BaseDirectory,
                    file);

                if (!File.Exists(file))
                {
                    return $"<!-- include file not found. {fileToLog} -->\n";
                }
            }

            return File.ReadAllText(file, new UTF8Encoding(false));
        }
    }

    public class TimelineRazorPlayer
    {
        public int Number { get; set; } = 0;

        public string Name { get; set; } = string.Empty;

        public string Job { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool InJob(
            params string[] jobs)
        {
            if (jobs == null)
            {
                return false;
            }

            return jobs.Any(x => this.Job.ContainsIgnoreCase(x));
        }

        public bool InRole(
            params string[] roles)
        {
            if (roles == null)
            {
                return false;
            }

            return roles.Any(x => this.Role.ContainsIgnoreCase(x));
        }
    }

    public class TimelineRazorVariable
    {
        public static Func<IReadOnlyDictionary<string, TimelineVariable>> GetVarDelegate { get; set; }

        public static Func<string, object, string, bool> SetVarDelegate { get; set; }

        public TimelineRazorVariable()
        {
            this.variables = TimelineRazorVariable.GetVarDelegate?.Invoke();
        }

        private IReadOnlyDictionary<string, TimelineVariable> variables;

        public object this[string name]
        {
            get
            {
                if (this.variables == null ||
                    !this.variables.ContainsKey(name))
                {
                    return false;
                }

                return this.variables[name].Value;
            }
        }
    }

    public class TimelineVariable : BindableBase
    {
        /// <summary>
        /// 空フラグ
        /// </summary>
        public static readonly TimelineVariable EmptyVariable = new TimelineVariable("Empty");

        public TimelineVariable(
            string name)
        {
            this.Name = name;
        }

        private string name = string.Empty;

        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        private object value = null;

        public object Value
        {
            get => this.value;
            set
            {
                this.value = value;

                var action = new Action(() => this.RaisePropertyChanged());
                Application.Current?.Dispatcher.BeginInvoke(
                    action,
                    DispatcherPriority.Background);
            }
        }

        private int counter = 0;

        public int Counter
        {
            get => this.counter;
            set => this.SetProperty(ref this.counter, value);
        }

        private DateTime expiration = DateTime.MaxValue;

        public DateTime Expiration
        {
            get => this.expiration;
            set => this.SetProperty(ref this.expiration, value);
        }

        private string zone = string.Empty;

        public string Zone
        {
            get => this.zone;
            set => this.SetProperty(ref this.zone, value);
        }

        public string ToPlaceholder() => $"VAR['{this.Name}']";

        public string Replace(string text) => this.Value != null ?
            text.Replace(this.ToPlaceholder(), this.Value.ToString()) :
            text;

        public override string ToString() =>
            $"{this.Name}={this.Value}, counter={this.Counter}";
    }
}
