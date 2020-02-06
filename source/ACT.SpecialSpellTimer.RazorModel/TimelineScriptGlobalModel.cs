using System;
using System.Dynamic;
using FFXIV.Framework.XIVHelper;

namespace ACT.SpecialSpellTimer.RazorModel
{
    public class TimelineScriptGlobalModel
    {
        public string LogLine { get; set; }

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
}
