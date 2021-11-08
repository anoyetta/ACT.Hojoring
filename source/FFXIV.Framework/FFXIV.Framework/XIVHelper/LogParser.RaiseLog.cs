using System;
using System.Collections.Generic;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Logfile;

namespace FFXIV.Framework.XIVHelper
{
    public static partial class LogParser
    {
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
                output.WriteLine(LogMessageType.ChatLog, timestamp, FormatLogLine(log));
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

            output.WriteLine(LogMessageType.ChatLog, timestamp, FormatLogLine(log));
        }

        private const string GameEchoChatCode = "0038";

        private static string FormatLogLine(
            string log)
            => $"{GameEchoChatCode}||Hojoring>{log}";
    }
}
