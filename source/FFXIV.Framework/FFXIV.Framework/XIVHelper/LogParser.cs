using System;
using System.Collections.Generic;
using System.Globalization;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Common.Models;
using FFXIV_ACT_Plugin.Logfile;

namespace FFXIV.Framework.XIVHelper
{
    public static partial class LogParser
    {
        public static string FormatLogLine(
            LogMessageType type,
            string logLine)
        {
            var fmt = default(FormattableString);
            var f = default(string[]);
            var typeText = ((byte)type).ToString("X2");

            switch (type)
            {
                case LogMessageType.AddCombatant:
                    // 03:4000102A:Added new combatant 木人.  Job: N/A Level: 60 Max HP: 16000 Max MP: 10000 Pos: (-787.0482,-707.5104,20.02) (54100000006133).
                    f = logLine.Split(':');

                    if (f.Length < (19 + 1))
                    {
                        break;
                    }

                    var id = Convert.ToUInt32(f[2], 16);
                    var b = Convert.ToByte(f[4], 16);
                    var job = b < 0 ? "N/A" : ((JobIDs)b).ToString();

                    fmt = $"{typeText}:{id:X8}:Added new combatant {f[3].ToProperCase()}{(!string.IsNullOrWhiteSpace(f[8]) ? $"({f[8]})" : String.Empty)}.  Job: {job} Level: {Convert.ToByte(f[5], 16)} Max HP: {Convert.ToUInt32(f[12])} Max MP: {Convert.ToUInt32(f[14])} Pos: ({f[17]},{f[18]},{f[19]}) ({f[9].PadRight(7, '0')}{f[10].PadLeft(7, '0')}).";
                    break;
                
                case LogMessageType.RemoveCombatant:
                    // 04:40001456:Removing combatant 小型二足.  Max HP: 670440. Pos: (884.9468,674.0366,-700).
                    f = logLine.Split(':');

                    if (f.Length < (19 + 1))
                    {
                        break;
                    }

                    id = Convert.ToUInt32(f[2], 16);
                    fmt = $"{typeText}:{id:X8}:Removing combatant {f[3].ToProperCase()}.  Max HP: {Convert.ToUInt32(f[12])} Pos: ({f[17]},{f[18]},{f[19]}).";
                    break;
                
                case LogMessageType.StartsCasting:
                    // 14:4095:Naoki Yoshida starts using グレア on 多関節型：司令機.
                    f = logLine.Split(':');
                    break;
                
                case LogMessageType.Death:
                    // 19:Naoki Yoshida was defeated by 多関節型：司令機.
                    f = logLine.Split(':');
                    break;
                
                case LogMessageType.StatusAdd:
                    // 1A:102E3219:Naoki Yoshida gains the effect of アクセラレーション from Naoki Yoshida for 20.00 Seconds.
                    // return ((FormattableString)$"{BuffID:X2}|{buffName}|{Duration:0.00}|{sourceId:X4}|{sourceName}|{TargetID:X4}|{TargetName}|{BuffExtra:X2}|{TargetMaxHP}|{sourceMaxHP}").ToString(CultureInfo.InvariantCulture);
                    f = logLine.Split(':');

                    if (f.Length < 7)
                    {
                        break;
                    }

                    /*
                    LogLineOut = LogLineOut + originalEntry.target.ToProperCase() + " gains the effect of " + originalEntry.skillname.ToProperCase() + " from " + originalEntry.actor.ToProperCase() + ".";

                    var targetID = Convert.ToUInt32(f[2], 16);
                    var target = 
                    fmt = $"{typeText}:{id:X8}:Removing combatant {f[3].ToProperCase()}.  Max HP: {Convert.ToUInt32(f[12])} Pos: ({f[17]},{f[18]},{f[19]}).";
                    */
                    break;
                
                case LogMessageType.StatusRemove:
                    // 1E:100D7101:Naoki Yoshida the effect of ミラージュダイブ実行可 from Naoki Yoshida.
                    f = logLine.Split(':');
                    break;
 
                case LogMessageType.Gauge:
                    // 1F:102A6F17:Naoki Yoshida:300001C:00:00:00
                    // 読みやすく独自にパースするつもりだが未実装
                    break;
            }

            return fmt != null ?
                fmt.ToString(CultureInfo.InvariantCulture) :
                logLine;
        }
    }

    public static class StringHelper
    {
        public static string ToProperCase(
            this string @this)
        {
            var text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(@this);

            if (text.EndsWith(" i (*)", StringComparison.Ordinal))
            {
                text = text.Substring(0, text.Length - 6) + " I (*)";
            }
            else if (text.EndsWith(" Ii", StringComparison.Ordinal))
            {
                text = text.Substring(0, text.Length - 3) + " II";
            }
            else if (text.EndsWith(" Ii (*)", StringComparison.Ordinal))
            {
                text = text.Substring(0, text.Length - 7) + " II (*)";
            }
            else if (text.EndsWith(" Iii", StringComparison.Ordinal))
            {
                text = text.Substring(0, text.Length - 4) + " III";
            }
            else if (text.EndsWith(" Iv", StringComparison.Ordinal))
            {
                text = text.Substring(0, text.Length - 3) + " IV";
            }
            else if (text.EndsWith(" V", StringComparison.Ordinal))
            {
                text = text.Substring(0, text.Length - 2) + " V";
            }

            return text;
        }
    }
}
