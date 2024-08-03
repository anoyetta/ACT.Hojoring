using System;
using System.Collections.Generic;
using System.Linq;

namespace FFXIV.Framework.XIVHelper
{
    public static class LogMessageTypeExtensions
    {
        public enum LogMessageType
        {
            ChatLog = 0,
            Territory = 1,
            ChangePrimaryPlayer = 2,
            AddCombatant = 3,
            RemoveCombatant = 4,
            PartyList = 11,
            PlayerStats = 12,
            StartsCasting = 20,
            ActionEffect = 21,
            AOEActionEffect = 22,
            CancelAction = 23,
            DoTHoT = 24,
            Death = 25,
            StatusAdd = 26,
            TargetIcon = 27,
            WaymarkMarker = 28,
            SignMarker = 29,
            StatusRemove = 30,
            Gauge = 31,
            World = 32,
            Director = 33,
            NameToggle = 34,
            Tether = 35,
            LimitBreak = 36,
            EffectResult = 37,
            StatusList = 38,
            UpdateHp = 39,
            ChangeMap = 40,
            SystemLogMessage = 41,
            StatusList3 = 42,
            Settings = 249,
            Process = 250,
            Debug = 251,
            PacketDump = 252,
            Version = 253,
            Error = 254,

            LineRegistration = 256,
            MapEffect = 257,
            FateDirector = 258,
            CEDirector = 259,
            InCombat = 260,
            CombatantMemory = 261,
            RSVData = 262,
            StartsUsingExtra = 263,
            AbilityExtra = 264,
            ContentFinderSettings = 265,
            NpcYell = 266,
            BattleTalk2 = 267,
            Countdown = 268,
            CountdownCancel = 269,
            ActorMove = 270,
            ActorSetPos = 271,
            SpawnNpcExtra = 272,
            ActorControlExtra = 273,
            ActorControlSelfExtra = 274
        }

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

        public static int max_messagetype = 274;

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
            var i = message.IndexOf(" ");
            if (i < 0)
            {
                return result;
            }

            message = message.Substring(i + 1);

            result = withoutTimestamp ?
                message :
                $"{timestamp}{message}";

            return result;
        }
    }
}
