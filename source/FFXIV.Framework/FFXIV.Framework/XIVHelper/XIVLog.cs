using System;
using static FFXIV.Framework.XIVHelper.LogMessageTypeExtensions;

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
                MessageType = LogMessageType.ChatLog,
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

        public LogMessageType MessageType { get; set; } = LogMessageType.ChatLog;

        public DateTime Timestamp { get; private set; } = DateTime.Now;

        public string Zone { get; set; } = string.Empty;

        public string LogLine { get; set; } = string.Empty;

        public string OriginalLogLine => $"[{this.Timestamp:HH:mm:ss.fff}] {this.LogLine}";

        public override string ToString()
            => $"{this.Timestamp:HH:mm:ss.fff} {this.LogLine}";
    }
}
