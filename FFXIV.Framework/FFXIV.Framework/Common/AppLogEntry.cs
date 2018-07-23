using System;

namespace FFXIV.Framework.Common
{
    public class AppLogEntry
    {
        public DateTime DateTime { get; set; }
        public string Level { get; set; }
        public string CallSite { get; set; }
        public string Message { get; set; }

        public override string ToString() =>
            $"{this.DateTime:yyyy-MM-dd HH:mm:ss.ffff} [{this.Level.PadRight(5, ' ')}] {this.Message}";
    }
}
