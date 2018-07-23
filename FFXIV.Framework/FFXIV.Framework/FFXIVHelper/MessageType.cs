using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using Advanced_Combat_Tracker;
using Prism.Mvvm;

namespace FFXIV.Framework.FFXIVHelper
{
    public enum MessageType : byte
    {
        LogLine,
        ChangeZone,
        ChangePrimaryPlayer,
        AddCombatant,
        RemoveCombatant,
        AddBuff,
        RemoveBuff,
        FlyingText,
        OutgoingAbility,
        IncomingAbility = 10,
        PartyList,
        PlayerStats,
        CombatantHP,
        NetworkStartsCasting = 20,
        NetworkAbility,
        NetworkAOEAbility,
        NetworkCancelAbility,
        NetworkDoT,
        NetworkDeath,
        NetworkBuff,
        NetworkTargetIcon,
        NetworkRaidMarker,
        NetworkTargetMarker,
        NetworkBuffRemove,
        NetworkGauge,
        NetworkWorld,
        Debug = 251,
        PacketDump,
        Version,
        Error,
        Timer
    }

    public static class MessageTypeEx
    {
        public static string ToCode(
            this MessageType type)
            => ((byte)type).ToString("X2");

        public static string ToKeyword(
           this MessageType type)
            => $"] {type.ToCode()}:";
    }

    public static class LogParser
    {
        public static void RaiseLog(
            DateTime timestamp,
            MessageType type,
            string[] data,
            bool isImport = false)
        {
            var log = string.Join(
                "|",
                new[]
                {
                    type.ToCode(),
                    timestamp.ToString("O")
                }.Union(data));

            ActGlobals.oFormActMain.BeginInvoke((MethodInvoker)delegate
            {
                ActGlobals.oFormActMain.ParseRawLogLine(isImport, timestamp, log);
            });
        }

        public static void RaiseLog(
            DateTime timestamp,
            string log)
        {
            var logline = string.Join(
                "|",
                new[]
                {
                    MessageType.LogLine.ToCode(),
                    timestamp.ToString("O"),
                    "0000",
                    "Hojoring",
                    log,
                    string.Empty
                });

            var action = new MethodInvoker(() => ActGlobals.oFormActMain.ParseRawLogLine(false, timestamp, logline));

            if (ActGlobals.oFormActMain.InvokeRequired)
            {
                ActGlobals.oFormActMain.BeginInvoke(action);
            }
            else
            {
                action.Invoke();
            }
        }
    }

    [Serializable]
    public class IgnoreLogType :
        BindableBase
    {
        private MessageType messageType;
        private bool isIgnore;

        [XmlAttribute(AttributeName = "code")]
        public string Code
        {
            get => this.MessageType.ToCode();
            set { }
        }

        [XmlAttribute(AttributeName = "type")]
        public MessageType MessageType
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
