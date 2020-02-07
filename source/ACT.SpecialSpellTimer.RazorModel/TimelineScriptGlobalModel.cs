using System;
using System.Collections.Generic;
using System.Dynamic;
using FFXIV.Framework.XIVHelper;

namespace ACT.SpecialSpellTimer.RazorModel
{
    public class TimelineScriptGlobalModel
    {
        #region Lazy Singleton

        private static readonly Lazy<TimelineScriptGlobalModel> LazyInstance = new Lazy<TimelineScriptGlobalModel>(() => new TimelineScriptGlobalModel());

        public static TimelineScriptGlobalModel Instance => LazyInstance.Value;

        private TimelineScriptGlobalModel()
        {
        }

        #endregion Lazy Singleton

        public static TimelineScriptGlobalModel CreateTestInstance() => new TimelineScriptGlobalModel();

        public string TimelineDirectory => TimelineRazorModel.Instance.TimelineDirectory;

        public string TimelineFile => TimelineRazorModel.Instance.TimelineFile;

        public DateTimeOffset LT => TimelineRazorModel.Instance.LT;

        public string ET => TimelineRazorModel.Instance.ET;

        public int ZoneID => TimelineRazorModel.Instance.ZoneID;

        public string Zone => TimelineRazorModel.Instance.Zone;

        public TimelineRazorVariable Var => TimelineRazorModel.Instance.Var;

        public TimelineTables Tables => TimelineRazorModel.Instance.Tables;

        public void SetVar(
            string name,
            object value,
            bool global = false)
            => TimelineRazorModel.Instance.SetVar(name, value, global);

        public void SetTemp(
            string name,
            object value)
            => TimelineRazorModel.Instance.SetTemp(name, value);

        public bool InZone(
            params string[] zones)
            => TimelineRazorModel.Instance.InZone(zones);

        public bool InZone(
            params int[] zones)
            => TimelineRazorModel.Instance.InZone(zones);

        public dynamic ParseJsonString(
            string hjson)
            => TimelineRazorModel.Instance.InZone(hjson);

        public dynamic ParseJsonFile(
            string file)
            => TimelineRazorModel.Instance.InZone(file);

        public TimelineScriptingHost ScriptingHost { get; } = new TimelineScriptingHost();

        public dynamic ExpandoObject { get; } = new ExpandoObject();

        public void Speak(
            string tts,
            double delay = 0)
        {
        }

        public void ShowVNotice(
            string message,
            string icon = null,
            double duration = 5.0)
        {
        }

        public void ShowINotice(
            string image,
            double duration = 5.0,
            double x = 0,
            double y = 0)
        {
        }

        public CombatantEx GetPlayer()
        {
            return null;
        }

        public CombatantEx[] GetParty()
        {
            return null;
        }

        public CombatantEx[] GetCombatants()
        {
            return null;
        }

        public void RaiseLogLine(
            string logLine)
        {
        }

        public void Trace(
            string message)
        {
        }
    }

    public class TimelineScriptingHost
    {
        public TimelineScriptingDelegates Global { get; } = new TimelineScriptingDelegates();

        public TimelineScriptingDelegates CurrentSub { get; } = new TimelineScriptingDelegates();
    }

    public class TimelineScriptingDelegates
    {
        public Action OnLoad { get; set; }

        public Action<string> OnLoglineRead { get; set; }

        private readonly List<TimelineScriptingSubscriber> Subscribers = new List<TimelineScriptingSubscriber>();

        public void Subscribe(
            Action subscriber,
            double interval = 20)
        {
            lock (this.Subscribers)
            {
                this.Subscribers.Add(new TimelineScriptingSubscriber()
                {
                    Action = subscriber,
                    Interval = interval,
                });
            }
        }

        public void ExecuteSubscribers()
        {
            lock (this.Subscribers)
            {
                var array = this.Subscribers.ToArray();

                foreach (var s in array)
                {
                    s.Execute();
                }
            }
        }

        public void Clear()
        {
            this.OnLoad = null;
            this.OnLoglineRead = null;

            lock (this.Subscribers)
            {
                this.Subscribers.Clear();
            }
        }
    }

    public class TimelineScriptingSubscriber
    {
        public Action Action { get; set; }

        public double Interval { get; set; }

        public DateTime LastExecutionTimestamp { get; private set; }

        public void Execute()
        {
            lock (this)
            {
                if (this.Action == null)
                {
                    return;
                }

                if ((DateTime.Now - this.LastExecutionTimestamp) < TimeSpan.FromMilliseconds(this.Interval))
                {
                    return;
                }

                try
                {
                    this.Action.Invoke();
                }
                finally
                {
                    this.LastExecutionTimestamp = DateTime.Now;
                }
            }
        }
    }
}
