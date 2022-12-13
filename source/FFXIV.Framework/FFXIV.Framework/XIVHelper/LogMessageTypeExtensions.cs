using FFXIV_ACT_Plugin.Logfile;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static string[] GetNames()
            => LazyLogMessageTypeTextStore.Value.Select(x => x.Value).ToArray();

        public static LogMessageType[] GetValues()
            => LazyLogMessageTypeTextStore.Value.Select(x => x.Key).ToArray();

        public static string ToHex(
            this LogMessageType type)
            => ((int)type).ToString("X2");

        public static string ToKeyword(
           this LogMessageType type)
            => $"] {type.ToHex()}:";

        public static string RemoveLogMessageType(
            int type,
            string logLine,
            bool withoutTimestamp = false)
        {
            /*
            新しいログの書式
            [00:32:16.798] ActionEffect 15:102DB8BA:Naoki Yoshida:BA:士気高揚の策:102DB8BA:Naoki Yoshida:...
            */

            const int TimestampLength = 15;
            var result = logLine;

            if (logLine.Length < TimestampLength)
            {
                return result;
            }

            var timestamp = logLine.Substring(0, TimestampLength);
            var message = logLine.Substring(TimestampLength);

            if (string.IsNullOrEmpty(message))
            {
                return result;
            }

            // ログタイプを除去する
            var i = message.IndexOf(' ');
            if (i < 0)
            {
                return result;
            }

            message = message.Substring(i);

            result = withoutTimestamp ?
                message :
                $"{timestamp}{message}";

            return result;
        }
    }
}
