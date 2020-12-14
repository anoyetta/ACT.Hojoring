using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
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
            => TimelineRazorModel.Instance.ParseJsonString(hjson);

        public dynamic ParseJsonFile(
            string file)
            => TimelineRazorModel.Instance.ParseJsonFile(file);

        public XIVLog[] CurrentLogs { get; set; } = new XIVLog[0];

        public TimelineScriptingHost ScriptingHost { get; } = new TimelineScriptingHost();

        public DynamicObject DynamicObject { get; } = new DynamicObject();

        /// <summary>
        /// TTS
        /// </summary>
        /// <param name="tts">読み上げる文字列</param>
        /// <param name="device">再生デバイス。Main, Sub, Both</param>
        /// <param name="sync">同期再生するか</param>
        /// <param name="volume">ボリューム。0.0-1.0</param>
        /// <param name="delay">再生までの遅延秒数(s) ex. 1.1秒</param>
        public void TTS(
            string tts,
            string device = "Main",
            bool sync = false,
            float volume = 1.0f,
            double delay = 0)
            => this.TTSDelegate?.Invoke(
                tts,
                device,
                sync,
                volume,
                delay);

        /// <summary>
        /// スペスペたいむ専用Ticker（v-notice）を表示する
        /// </summary>
        /// <param name="message">表示するメッセージ</param>
        /// <param name="icon">表示するアイコン。キャラ名を指定するとそのキャラのジョブアイコンが表示される</param>
        /// <param name="order">表示順。数字が小さいほど上に表示される</param>
        /// <param name="delay">表示までの遅延秒数</param>
        /// <param name="duration">表示秒数</param>
        /// <param name="durationVisible">秒数のカウントダウンを表示するか？</param>
        /// <param name="syncToHide">強制的に非表示にする文字列</param>
        /// <param name="fontScale">フォントスケール</param>
        public void ShowTicker(
            string message,
            string icon = null,
            int order = 0,
            double delay = 0,
            double duration = 5.0,
            bool durationVisible = false,
            string syncToHide = null,
            double fontScale = 1.0)
            => this.ShowTickerDelegate?.Invoke(
                message,
                icon,
                order,
                delay,
                duration,
                durationVisible,
                syncToHide,
                fontScale);

        public void ShowImage(
            string image,
            double duration = 5.0,
            double x = 0,
            double y = 0)
        {
            // NO-OP
        }

        private static readonly CombatantEx[] EmptyCombatants = new CombatantEx[0];

        public CombatantEx GetPlayer()
            => this.GetPlayerDelegate?.Invoke() ?? CombatantsManager.DummyPlayer;

        public CombatantEx[] GetParty()
            => this.GetPartyDelegate?.Invoke() ?? EmptyCombatants;

        public CombatantEx[] GetCombatants()
            => this.GetCombatantsDelegate?.Invoke() ?? EmptyCombatants;

        public string GetCurrentSubRoutineName()
            => this.GetCurrentSubRoutineNameDelegate?.Invoke() ?? string.Empty;

        public void RaiseLogLine(
            string logLine)
            => this.RaiseLogLineDelegate?.Invoke(logLine);

        public void Trace(
            string message)
            => this.TraseDelegate?.Invoke(message);

        #region Delegates

        internal Action<string> RaiseLogLineDelegate { get; set; }

        internal Action<string> TraseDelegate { get; set; }

        internal Func<string> GetCurrentSubRoutineNameDelegate { get; set; }

        internal Func<CombatantEx> GetPlayerDelegate { get; set; }

        internal Func<CombatantEx[]> GetPartyDelegate { get; set; }

        internal Func<CombatantEx[]> GetCombatantsDelegate { get; set; }

        internal Action<string, string, bool, float, double> TTSDelegate { get; set; }

        internal Action<string, string, int, double, double, bool, string, double> ShowTickerDelegate { get; set; }

        #endregion Delegates
    }

    public class DynamicObject
    {
        public dynamic ZoneGlobal { get; private set; } = new ExpandoObject();

        public dynamic CurrentTry { get; private set; } = new ExpandoObject();

        internal void ClearZoneGlobal()
            => this.ZoneGlobal = new ExpandoObject();

        internal void ClearCurrentTry()
            => this.CurrentTry = new ExpandoObject();
    }

    public class TimelineScriptingHost
    {
        public readonly object ScriptingBlocker = new object();

        private readonly List<ITimelineScript> Scripts = new List<ITimelineScript>();

        public static int AnonymouseScriptNo { get; internal set; } = 1;

        public void AddScript(
            ITimelineScript script)
        {
            lock (this.ScriptingBlocker)
            {
                this.Scripts.Add(script);
            }
        }

        public void Clear()
        {
            lock (this.ScriptingBlocker)
            {
                AnonymouseScriptNo = 1;
                TimelineScriptGlobalModel.Instance.DynamicObject.ClearCurrentTry();
                this.Scripts.Clear();
            }
        }

        public void ExecuteResidents(
            string currentSubRoutine)
        {
            lock (this.ScriptingBlocker)
            {
                var now = DateTime.Now;

                var scripts = this.Scripts.Where(x =>
                {
                    if (!x.Enabled.GetValueOrDefault())
                    {
                        return false;
                    }

                    if (x.ScriptingEvent != TimelineScriptEvents.Resident)
                    {
                        return false;
                    }

                    if (string.IsNullOrEmpty(x.ParentSubRoutine) ||
                        x.ParentSubRoutine == currentSubRoutine)
                    {
                        if ((now - x.LastExecutedTimestamp).TotalMilliseconds > x.Interval)
                        {
                            return true;
                        }
                    }

                    return false;
                });

                if (!scripts.Any())
                {
                    return;
                }

                foreach (var script in scripts)
                {
                    var returnValue = script.Run();
                }
            }
        }

        public void ExecuteOnLogs(
            string currentSubRoutine,
            IEnumerable<XIVLog> logs)
        {
            lock (this.ScriptingBlocker)
            {
                var now = DateTime.Now;

#if DEBUG
                if (currentSubRoutine == "応用フェーズ")
                {
                    Debug.WriteLine("応用フェーズ");
                }
#endif

                var scripts = this.Scripts.Where(x =>
                {
                    if (!x.Enabled.GetValueOrDefault())
                    {
                        return false;
                    }

                    if (x.ScriptingEvent != TimelineScriptEvents.OnLogs)
                    {
                        return false;
                    }

                    if (string.IsNullOrEmpty(x.ParentSubRoutine) ||
                        x.ParentSubRoutine == currentSubRoutine)
                    {
                        return true;
                    }

                    return false;
                });

                if (!scripts.Any())
                {
                    return;
                }

                TimelineScriptGlobalModel.Instance.CurrentLogs = logs.ToArray();

                foreach (var script in scripts)
                {
                    var returnValue = script.Run();
                }
            }
        }

        public void ExecuteOnLoad()
        {
            lock (this.ScriptingBlocker)
            {
                var scripts = this.Scripts.Where(x =>
                    x.Enabled.GetValueOrDefault() &&
                    x.ScriptingEvent == TimelineScriptEvents.OnLoad);

                if (!scripts.Any())
                {
                    return;
                }

                foreach (var script in scripts)
                {
                    var returnValue = script.Run();
                }
            }
        }

        public void ExecuteOnSub(
            string currentSubRoutine)
        {
            lock (this.ScriptingBlocker)
            {
                var scripts = this.Scripts.Where(x =>
                    x.Enabled.GetValueOrDefault() &&
                    x.ScriptingEvent == TimelineScriptEvents.OnSub &&
                    x.ParentSubRoutine == currentSubRoutine);

                if (!scripts.Any())
                {
                    return;
                }

                foreach (var script in scripts)
                {
                    var returnValue = script.Run();
                }
            }
        }

        public void ExecuteOnWipeout()
        {
            lock (this.ScriptingBlocker)
            {
                var scripts = this.Scripts.Where(x =>
                    x.Enabled.GetValueOrDefault() &&
                    x.ScriptingEvent == TimelineScriptEvents.OnWipeout);

                if (!scripts.Any())
                {
                    return;
                }

                foreach (var script in scripts)
                {
                    var returnValue = script.Run();
                }
            }
        }
    }
}
