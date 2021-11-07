using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Serialization;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Logfile;
using Prism.Mvvm;

namespace FFXIV.Framework.XIVHelper
{
    public static class LogMessageTypeEx
    {
        public static string ToCode(
            this LogMessageType type)
            => ((byte)type).ToString("X2");

        public static string ToKeyword(
           this LogMessageType type)
            => $"] {type.ToCode()}:";
    }

    public static class LogParser
    {
        private const string GameEchoChatCode = "0038";

        public static void RaiseLog(
            DateTime timestamp,
            IEnumerable<string> logs)
        {

            var action = new MethodInvoker(() =>
            {
                foreach (var log in logs)
                {
                    ActGlobals.oFormActMain.ParseRawLogLine(
                        false,
                        timestamp,
                        FormatLogLine(timestamp, log));
                }
            });

            if (ActGlobals.oFormActMain.InvokeRequired)
            {
                ActGlobals.oFormActMain.BeginInvoke(action);
            }
            else
            {
                action.Invoke();
            }

#if DEBUG
            foreach (var log in logs)
            {
                Debug.WriteLine($"RaiseLog -> {FormatLogLine(timestamp, log)}");
            }
#endif
        }

        public static void RaiseLog(
            DateTime timestamp,
            string log)
        {
            var logLine = FormatLogLine(timestamp, log);

            var action = new MethodInvoker(() => ActGlobals.oFormActMain.ParseRawLogLine(
                false,
                timestamp,
                logLine));

            if (ActGlobals.oFormActMain.InvokeRequired)
            {
                ActGlobals.oFormActMain.BeginInvoke(action);
            }
            else
            {
                action.Invoke();
            }

#if DEBUG
            Debug.WriteLine($"RaiseLog -> {logLine}");
#endif
        }

        private static string FormatLogLine(
            DateTime timestamp,
            string log)
            => $"{(int)LogMessageType.ChatLog}|{timestamp:O}|{GameEchoChatCode}|Hojoring>{log}";
    }

    [Serializable]
    public class IgnoreLogType :
        BindableBase
    {
        private LogMessageType messageType;
        private bool isIgnore;

        [XmlAttribute(AttributeName = "code")]
        public string Code
        {
            get => this.MessageType.ToCode();
            set { }
        }

        [XmlAttribute(AttributeName = "type")]
        public LogMessageType MessageType
        {
            get => this.messageType;
            set => this.SetProperty(ref this.messageType, value);
        }

        [XmlAttribute(AttributeName = "ignore")]
        public bool IsIgnore
        {
            get => this.isIgnore;
            set => this.SetProperty(ref this.isIgnore, value);
        }

        [XmlIgnore]
        public string Keyword => this.MessageType.ToKeyword();
    }
}
