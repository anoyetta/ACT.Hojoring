using System;
using System.Collections.Generic;
using System.Linq;
using Advanced_Combat_Tracker;
using static FFXIV.Framework.XIVHelper.LogMessageTypeExtensions;


namespace FFXIV.Framework.XIVHelper
{
    public static partial class LogParser
    {
        public static Action<DateTime, string> WriteLineDebugLogDelegate { get; set; }

        public static Action<DateTime, IEnumerable<string>> WriteLinesDebugLogDelegate { get; set; }

        public static void RaiseLog(
            DateTime timestamp,
            IEnumerable<string> logs)
        {
            if (!ActGlobals.oFormActMain.CanFocus)
            {
                return;
            }

            var output = XIVPluginHelper.Instance.LogOutput;
            if (output == null)
            {
                return;
            }

            var config = Config.Instance;

            var lines = logs
                .Where(log => !string.IsNullOrEmpty(log))
                .Select(log =>
                {
                    var line = FormatLogLine(log);

                    // 念のため改行コードを除去する
                    line = line
                        .Replace("\r\n", "\\n")
                        .Replace("\r", "\\n")
                        .Replace("\n", "\\n");

                    return line;
                })
                .ToArray();

            if (config.IsEnabledOutputDebugLog)
            {
                WriteLinesDebugLogDelegate?.Invoke(
                    timestamp,
                    lines.Select(x => $"{ChatLogCode}|{x}"));
            }

            foreach (var line in lines)
            {
                output.WriteLine(FFXIV_ACT_Plugin.Logfile.LogMessageType.ChatLog, timestamp, line);
            }
        }

        public static void RaiseLog(
            DateTime timestamp,
            string log)
        {
            if (!ActGlobals.oFormActMain.CanFocus)
            {
                return;
            }

            var output = XIVPluginHelper.Instance.LogOutput;
            if (output == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(log))
            {
                return;
            }

            var line = FormatLogLine(log);

            // 念のため改行コードを除去する
            line = line
                .Replace("\r\n", "\\n")
                .Replace("\r", "\\n")
                .Replace("\n", "\\n");

            if (Config.Instance.IsEnabledOutputDebugLog)
            {
                WriteLineDebugLogDelegate?.Invoke(timestamp, $"{ChatLogCode}|{line}");
            }

            output.WriteLine(FFXIV_ACT_Plugin.Logfile.LogMessageType.ChatLog, timestamp, line);
        }

        private static readonly string ChatLogCode = LogMessageType.ChatLog.ToHex();
        private static readonly string GameEchoChatCode = "0038";

        private static string FormatLogLine(
            string log)
            => $"{GameEchoChatCode}||Hojoring>{log}";
    }
}
