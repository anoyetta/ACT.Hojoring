using System;

namespace FFXIV.Framework.FFXIVHelper
{
    public class XIVLog
    {
        public XIVLog(
            DateTime detectTime,
            string logLine,
            string zoneName = null,
            bool isImport = false)
        {
            this.DetectTime = detectTime;
            this.ZoneName = zoneName;
            this.IsImport = isImport;

            if (!string.IsNullOrEmpty(logLine) &&
                logLine.Length >= 15)
            {
                this.Timestamp = logLine.Substring(0, 15).TrimEnd();
                this.Log = logLine.Remove(0, 15);
            }
            else
            {
                this.Timestamp = DateTime.Now.ToString("[HH:mm:ss.fff]");
                this.Log = string.Empty;
            }
        }

        public long ID { get; set; } = 0;

        public DateTime DetectTime { get; set; } = DateTime.Now;

        public string Timestamp { get; set; } = string.Empty;

        public string Log { get; set; } = string.Empty;

        public string ZoneName { get; set; } = string.Empty;

        public bool IsImport { get; set; } = false;

        public string LogLine => $"{this.Timestamp} {this.Log}";
    }
}
