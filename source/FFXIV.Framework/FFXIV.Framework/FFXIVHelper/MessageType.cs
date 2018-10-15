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
        LogLine = 0,
        ChangeZone = 1,
        ChangePrimaryPlayer = 2,
        AddCombatant = 3,
        RemoveCombatant = 4,
        AddBuff = 5,
        RemoveBuff = 6,
        FlyingText = 7,
        OutgoingAbility = 8,
        IncomingAbility = 10,
        PartyList = 11,
        PlayerStats = 12,
        CombatantHP = 13,
        NetworkStartsCasting = 20,
        NetworkAbility = 21,
        NetworkAOEAbility = 22,
        NetworkCancelAbility = 23,
        NetworkDoT = 24,
        NetworkDeath = 25,
        NetworkBuff = 26,
        NetworkTargetIcon = 27,
        NetworkRaidMarker = 28,
        NetworkTargetMarker = 29,
        NetworkBuffRemove = 30,
        NetworkGauge = 0x1F,
        NetworkWorld = 0x20,
        Network6D = 33,
        NetworkNameToggle = 34,
        Debug = 251,
        PacketDump = 252,
        Version = 253,
        Error = 254,
        Timer = 0xFF
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
