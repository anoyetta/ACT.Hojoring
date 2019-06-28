using System;
using FFXIV_ACT_Plugin.Logfile;

namespace FFXIV.Framework.XIVHelper
{
    public class XIVLog
    {
        private static uint simulationSequence = 0;

        public static XIVLog CreateToSimulation(
            DateTime timestamp,
            string logline)
            => new XIVLog()
            {
                Seq = simulationSequence++,
                MessageType = LogMessageType.LogLine,
                Timestamp = timestamp,
                LogLine = logline,
            };

        public XIVLog()
        {
        }

        public XIVLog(
            uint seq,
            int messageType,
            string logLine) : this(
                seq,
                (LogMessageType)Enum.ToObject(typeof(LogMessageType), messageType),
                logLine)
        {
        }

        public XIVLog(
            uint seq,
            LogMessageType messageType,
            string logLine)
        {
            this.Seq = seq;
            this.MessageType = messageType;
            this.LogLine = logLine;
        }

        public uint Seq { get; private set; } = 0;

        public LogMessageType MessageType { get; set; } = LogMessageType.LogLine;

        public DateTime Timestamp { get; private set; } = DateTime.Now;

        public string Zone { get; set; } = string.Empty;

        public string LogLine { get; set; } = string.Empty;

        public override string ToString()
            => $"{this.Timestamp:HH:mm:ss.fff} {this.LogLine}";
    }
}
