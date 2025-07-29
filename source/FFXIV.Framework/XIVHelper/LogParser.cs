﻿using System;
using System.Globalization;
using System.Linq;
using static FFXIV.Framework.XIVHelper.LogMessageTypeExtensions;

namespace FFXIV.Framework.XIVHelper
{
    public static partial class LogParser
    {
        public static string FormatLogLine(
            int type,
            string logLine) =>
            FormatLogLine(
                (LogMessageType)Enum.ToObject(typeof(LogMessageType), type),
                logLine);

        public static string FormatLogLine(
            LogMessageType type,
            string logLine)
        {
            if (!Config.Instance.IsEnabledCompatibleLogFormat)
            {
                return logLine;
            }

            var fmt = default(FormattableString);
            var typeText = type.ToHex();

            switch (type)
            {
                case LogMessageType.AddCombatant:
                    // 03:4000102A:Added new combatant 木人.  Job: N/A Level: 60 Max HP: 16000 Max MP: 10000 Pos: (-787.0482,-707.5104,20.02) (54100000006133).
                    // $"03|{CombatantID:X8}|{CombatantName}|{JobID:X2}|{Level:X1}|{OwnerID:X4}|{WorldID:X2}|{WorldName}|{BNpcNameID}|{BNpcID}|{currentHp}|{maxHp}|{currentMp}|{maxMp}|{damageShield}|{posX:0.00}|{posY:0.00}|{posZ:0.00}|{heading:0.00}";
                    // 03|40000c94|木人|0|1|0|0||541|901|44|44|0|10000|0|0|94.37677|55.711|7.070179|2.434896||6c1e2c671c62ff55a0c29bb6d5d65be9
                    var f = logLine.Split(':');

                    if (f.Length < (18 + 1))
                    {
                        break;
                    }

                    var id = Hex2Uint(f[1]);
                    var combatantName = f[2].ToProperCase();

                    var jobID = Jobs.IntToID(Hex2Byte(f[3]));
                    var job = jobID == JobIDs.Unknown || jobID == JobIDs.ADV ? "N/A" : jobID.ToStringEx();

                    var world = f[7];
                    world = !string.IsNullOrWhiteSpace(world) ? $"({world})" : string.Empty;

                    var level = Hex2Byte(f[4]);
                    var maxHP = Dec2Uint(f[11]);
                    var maxMP = Dec2Uint(f[13]);
                    var posX = f[16];
                    var posY = f[17];
                    var posZ = f[18];
                    var bNpcNameID = f[8].PadRight(7, '0');
                    var bNpcID = f[9].PadRight(7, '0');

                    fmt = $"{typeText}:{id:X8}:Added new combatant {combatantName}{world}.  Job: {job} Level: {level} Max HP: {maxHP} Max MP: {maxMP} Pos: ({posX},{posY},{posZ}) ({bNpcNameID}{bNpcID}).";
                    break;

                case LogMessageType.RemoveCombatant:
                    // 04:40001456:Removing combatant 小型二足.  Max HP: 670440. Pos: (884.9468,674.0366,-700).
                    f = logLine.Split(':');

                    if (f.Length < (18 + 1))
                    {
                        break;
                    }

                    id = Hex2Uint(f[1]);
                    combatantName = f[2].ToProperCase();

                    maxHP = Dec2Uint(f[11]);
                    posX = f[16];
                    posY = f[17];
                    posZ = f[18];

                    fmt = $"{typeText}:{id:X8}:Removing combatant {combatantName}.  Max HP: {maxHP} Pos: ({posX},{posY},{posZ}).";
                    break;

                case LogMessageType.StartsCasting:
                    // 14:4095:Naoki Yoshida starts using グレア on 多関節型：司令機.
                    // $"14:{sourceId:X4}|{sourceName}|{skillId:X2}|{skillName}|{targetId:X4}|{targetName}|{Duration:0.00}|{posX:0.00}|{posY:0.00}|{posZ:0.00}|{heading:0.00}"
                    f = logLine.Split(':');

                    if (f.Length < (11 + 1))
                    {
                        break;
                    }

                    var targetID = Hex2Uint(f[5]);
                    var target = f[6];
                    var sourceID = Hex2Uint(f[1]);
                    var source = f[2];
                    var duration = f[7];
                    var skillID = Hex2Uint(f[3]);
                    var skillName = f[4];
                    posX = f[8];
                    posY = f[9];
                    posZ = f[10];
                    var heading = f[11];

                    if (string.IsNullOrWhiteSpace(target))
                    {
                        target = CombatantsManager.Instance.GetCombatantMain(targetID)?.Name ?? string.Empty;
                    }

                    if (string.IsNullOrWhiteSpace(source))
                    {
                        source = CombatantsManager.Instance.GetCombatantMain(sourceID)?.Name ?? string.Empty;
                    }

                    // starts using については、Version 2.2.x.x系のログより情報を拡張した
                    fmt = $"{typeText}:{skillID:X4}:{source} starts using {skillName} on {target}. Duration: {duration} Pos: ({posX},{posY},{posZ}) Heading: {heading}";
                    break;

                case LogMessageType.Death:
                    // 19:Naoki Yoshida was defeated by 多関節型：司令機.
                    // Deathはパースしないものとする
                    break;

                case LogMessageType.StatusAdd:
                    // 1A:102E3219:Naoki Yoshida gains the effect of アクセラレーション from Naoki Yoshida for 20.00 Seconds.
                    // $"1A:{BuffID:X2}|{buffName}|{Duration:0.00}|{sourceId:X4}|{sourceName}|{TargetID:X4}|{TargetName}|{BuffExtra:X2}|{TargetMaxHP}|{sourceMaxHP}";
                    f = logLine.Split(':');

                    if (f.Length < (7 + 1))
                    {
                        break;
                    }

                    targetID = Hex2Uint(f[6]);
                    target = f[7];
                    sourceID = Hex2Uint(f[4]);
                    source = f[5];
                    duration = f[3];
                    var buffName = f[2];
                    var buffID = f[1].PadLeft(4, '0');
                    var buffExtraID = f[8].PadLeft(2, '0');

                    if (string.IsNullOrWhiteSpace(target))
                    {
                        target = CombatantsManager.Instance.GetCombatantMain(targetID)?.Name ?? string.Empty;
                    }

                    if (string.IsNullOrWhiteSpace(source))
                    {
                        source = CombatantsManager.Instance.GetCombatantMain(sourceID)?.Name ?? string.Empty;
                    }

                    fmt = $"{typeText}:{targetID:X8}:{target} gains the effect of {buffName} from {source} for {duration} Seconds. BuffID: {buffID}-{buffExtraID}";
                    break;

                case LogMessageType.StatusRemove:
                    // 1E:100D7101:Naoki Yoshida losses the effect of ミラージュダイブ実行可 from Naoki Yoshida.
                    // $"1E:{BuffID:X2}|{buffName}|{Duration:0.00}|{sourceId:X4}|{sourceName}|{TargetID:X4}|{TargetName}|{BuffExtra:X2}|{TargetMaxHP}|{sourceMaxHP}";
                    f = logLine.Split(':');

                    if (f.Length < (7 + 1))
                    {
                        break;
                    }

                    targetID = Hex2Uint(f[6]);
                    target = f[7];
                    sourceID = Hex2Uint(f[4]);
                    source = f[5];
                    buffName = f[2];
                    buffID = f[1].PadLeft(4, '0');
                    buffExtraID = f[8].PadLeft(2, '0');

                    if (string.IsNullOrWhiteSpace(target))
                    {
                        target = CombatantsManager.Instance.GetCombatantMain(targetID)?.Name ?? string.Empty;
                    }

                    if (string.IsNullOrWhiteSpace(source))
                    {
                        source = CombatantsManager.Instance.GetCombatantMain(sourceID)?.Name ?? string.Empty;
                    }

                    fmt = $"{typeText}:{targetID:X8}:{target} loses the effect of {buffName} from {source}. BuffID: {buffID}-{buffExtraID}";
                    break;

                case LogMessageType.Gauge:
                    // 1F:102A6F17:Naoki Yoshida:300001C:00:00:00
                    // 読みやすく独自にパースするつもりだが未実装
                    break;
            }

            var formatedLogLine = fmt != null ?
                fmt.ToString(CultureInfo.InvariantCulture) :
                logLine;

            // 終端の:を除去する
            if (formatedLogLine.EndsWith(":"))
            {
                formatedLogLine = formatedLogLine.Substring(0, formatedLogLine.Length - 1);
            }

            return formatedLogLine;
        }

        /// <summary>
        /// ツールチップシンボルを除去する
        /// </summary>
        /// <param name="logLine"></param>
        /// <returns>編集後のLogLine</returns>
        public static string RemoveTooltipSynbols(
            string logLine)
        {
            var result = logLine;

            if (!logLine.StartsWith($"{LogMessageType.ChatLog.ToHex()}:"))
            {
                return result;
            }

            DumpTooltipLogSample(result);

            // U+E000-U+EFFF の特殊文字を除去する
            result = result
                .Where(x => x < '\uE000' || '\uEFFF' < x)
                .Select(c => c.ToString())
                .Aggregate((a, b) => $"{a}{b}");

            // Trimをかける
            var parts = result.Split(':');
            parts[parts.Length - 1] = parts[parts.Length - 1].Trim();
            result = string.Join(":", parts);

            return result;
        }

        private static unsafe void DumpTooltipLogSample(
            string logLine)
        {
#if !DEBUG
            return;
#else
            // Tooltipシンボルをダンプするコード
            // 重いので殺しておく
#if false
            if (!logLine.EndsWith("の効果。"))
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Tooltip-> {logLine}");

            fixed (char* ps = logLine)
            {
                var chars = new List<string>();

                var p = (byte*)ps;
                for (int i = 0; i < logLine.Length * 2; i += 2)
                {
                    var bL = *(p + i);
                    var bH = *(p + i + 1);
                    chars.Add($"U+{bH:X2}{bL:X2}");
                }

                System.Diagnostics.Debug.WriteLine($"Tooltip-> {chars.Aggregate((a, b) => $"{a} {b}")}");
            }
#endif
            return;
#endif
        }

        public static string RemoveWorldName(
            string logLine)
        {
            var result = logLine;
            var code = LogMessageType.ChatLog.ToHex();

            if (!logLine.Contains($"] {code}:") &&
                !logLine.StartsWith($"{code}:"))
            {
                return result;
            }

            result = XIVPluginHelper.Instance.RemoveWorldName(logLine);

            return result;
        }

        private static uint Hex2Uint(string hex)
        {
            if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint v))
            {
                return v;
            }
            else
            {
                return 0;
            }
        }

        private static uint Dec2Uint(string dec)
        {
            if (uint.TryParse(dec, NumberStyles.Number, CultureInfo.InvariantCulture, out uint v))
            {
                return v;
            }
            else
            {
                return 0;
            }
        }

        private static byte Hex2Byte(string hex)
        {
            if (byte.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte v))
            {
                return v;
            }
            else
            {
                return 0;
            }
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
