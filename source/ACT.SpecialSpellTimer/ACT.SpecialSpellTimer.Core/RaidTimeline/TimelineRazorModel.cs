using System;
using System.IO;
using System.Linq;
using System.Text;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.Globalization;
using Hjson;
using Newtonsoft.Json.Linq;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    public class TimelineRazorModel
    {
        /// <summary>
        /// Local Time
        /// </summary>
        public DateTimeOffset LT => DateTimeOffset.Now;

#if false
        public EorzeaTime ET => this.LT.ToEorzeaTime();
#endif

        public bool SyncTTS { get; set; } = false;

        public string Zone { get; set; } = string.Empty;

        public string Locale { get; set; } = Locales.JA.ToString();

        public TimelineRazorPlayer Player { get; set; }

        public TimelineRazorPlayer[] Party { get; set; }

        public string TimelineDirectory => TimelineManager.Instance.TimelineDirectory;

        public string TimelineFile { get; private set; } = string.Empty;

        internal void UpdateCurrentTimelineFile(
            string currentTimelineFile)
            => this.TimelineFile = currentTimelineFile;

        public bool InZone(
            params string[] zones)
        {
            if (zones == null)
            {
                return false;
            }

            return zones.Any(x => this.Zone.ContainsIgnoreCase(x));
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
                    TimelineManager.Instance.TimelineDirectory,
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
                    TimelineManager.Instance.TimelineDirectory,
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
}
