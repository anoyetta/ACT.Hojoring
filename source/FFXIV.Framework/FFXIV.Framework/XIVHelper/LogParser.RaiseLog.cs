using System;
using System.Collections.Generic;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Logfile;

namespace FFXIV.Framework.XIVHelper
{
    public static partial class LogParser
    {
        public static Action<DateTime, string> WriteLineDebugLogDelegate { get; set; }

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

            foreach (var log in logs)
            {
                if (string.IsNullOrEmpty(log))
                {
                    continue;
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

                output.WriteLine(LogMessageType.ChatLog, timestamp, line);
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

            output.WriteLine(LogMessageType.ChatLog, timestamp, line);
        }

        private static readonly string ChatLogCode = LogMessageType.ChatLog.ToHex();
        private static readonly string GameEchoChatCode = "0038";

        private static string FormatLogLine(
            string log)
            => $"{GameEchoChatCode}||Hojoring>{log}";
    }
}
