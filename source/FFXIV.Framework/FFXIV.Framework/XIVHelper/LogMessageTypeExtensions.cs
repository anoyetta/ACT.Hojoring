using System;
using System.Collections.Generic;
using System.Linq;
using FFXIV_ACT_Plugin.Logfile;

namespace FFXIV.Framework.XIVHelper
{
    public static class LogMessageTypeExtensions
    {
        private static readonly Lazy<Dictionary<LogMessageType, string>> LazyLogMessageTypeTextStore =
            new Lazy<Dictionary<LogMessageType, string>>(() =>
            {
                var d = new Dictionary<LogMessageType, string>();

                foreach (LogMessageType e in Enum.GetValues(typeof(LogMessageType)))
                {
                    d.Add(e, e.ToString());
                }

                return d;
            });

        public static string ToStringEx(
            this LogMessageType type)
            => LazyLogMessageTypeTextStore.Value.ContainsKey(type) ?
                LazyLogMessageTypeTextStore.Value[type] :
                "Unknown";

        public static string[] GetNames()
            => LazyLogMessageTypeTextStore.Value.Select(x => x.Value).ToArray();

        public static LogMessageType[] GetValues()
            => LazyLogMessageTypeTextStore.Value.Select(x => x.Key).ToArray();

        public static string ToHex(
            this LogMessageType type)
            => ((byte)type).ToString("X2");

        public static string ToKeyword(
           this LogMessageType type)
            => $"] {type.ToHex()}:";

        public static string RemoveLogMessageType(
            LogMessageType type,
            string logLine,
            bool withoutTimestamp = false)
        {
            /*
            新しいログの書式
            [00:32:16.798] ActionEffect 15:102DB8BA:Naoki Yoshida:BA:士気高揚の策:102DB8BA:Naoki Yoshida:...
            */

            var timestampLength = withoutTimestamp ? 0 : 15;
            var result = logLine;

            if (string.IsNullOrEmpty(result))
            {
                return result;
            }

            if (logLine.Length < timestampLength)
            {
                return result;
            }

            var messageTypeNoIndex = timestampLength + type.ToStringEx().Length + 1;
            if (logLine.Length < messageTypeNoIndex)
            {
                return result;
            }

            result = !withoutTimestamp ?
                $"{logLine.Substring(0, timestampLength - 1)} {logLine.Substring(messageTypeNoIndex)}" :
                logLine.Substring(messageTypeNoIndex);

            return result;
        }

        public static string RemoveLogMessageType(
            int type,
            string logLine,
            bool withoutTimestamp = false)
            => RemoveLogMessageType(
                (LogMessageType)Enum.ToObject(typeof(LogMessageType), type),
                logLine,
                withoutTimestamp);
    }
}
