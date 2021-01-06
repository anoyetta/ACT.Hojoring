using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using ACT.SpecialSpellTimer.RazorModel;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.XIVHelper;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    public partial class TimelineExpressionsModel :
        TimelineBase
    {
        public static void RefreshGlobalVariables()
        {
            RefreshIsToTMe();
            RefreshInTankStance();
            RefreshIsFirstEnmityMe();
            RefreshET();
            RefreshZone();
        }

        /// <summary>
        /// TargetOfTargetが自分か？
        /// </summary>
        public const string IS_TOT_ME = "IS_TOT_ME";

        private static DateTime totChangedTimestamp;

        /// <summary>
        /// IS_TOT_ME を更新する
        /// </summary>
        public static void RefreshIsToTMe()
        {
            var name = IS_TOT_ME;

            var player = CombatantsManager.Instance.Player;
            if (player != null)
            {
                if (player.TargetOfTargetID != 0)
                {
                    var value = player.IsTargetOfTargetMe;
                    if (SetVariable(name, value))
                    {
                        TimelineController.RaiseLog(
                            $"{TimelineConstants.LogSymbol} set VAR['{name}'] = {value}");

                        if ((DateTime.Now - totChangedTimestamp) > TimeSpan.FromSeconds(2))
                        {
                            totChangedTimestamp = DateTime.Now;
                            TimelineController.RaiseLog(
                                $"Target-of-Target has been changed.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// タンクスタンス中か？
        /// </summary>
        public const string IN_TANK_STANCE = "IN_TANK_STANCE";

        /// <summary>
        /// IN_TANK_STANCE を更新する
        /// </summary>
        public static void RefreshInTankStance()
        {
            var name = IN_TANK_STANCE;

            var player = CombatantsManager.Instance.Player;
            if (player != null)
            {
                var value = player.InTankStance();
                if (SetVariable(name, value))
                {
                    TimelineController.RaiseLog(
                        $"{TimelineConstants.LogSymbol} set VAR['{name}'] = {value}");
                }
            }
        }

        /// <summary>
        /// 第一敵視が自分か？
        /// </summary>
        public const string IS_FIRST_ENMITY_ME = "IS_FIRST_ENMITY_ME";

        /// <summary>
        /// IS_FIRST_ENMITY_ME を更新する
        /// </summary>
        public static void RefreshIsFirstEnmityMe()
        {
            var name = IS_FIRST_ENMITY_ME;

            var value = SharlayanHelper.Instance.IsFirstEnmityMe;

            if (SetVariable(name, value))
            {
                TimelineController.RaiseLog(
                    $"{TimelineConstants.LogSymbol} set VAR['{name}'] = {value}");
            }
        }

        /// <summary>
        /// エオルゼア時刻
        /// </summary>
        public const string ET = "ET";

        /// <summary>
        /// ET を更新する
        /// </summary>
        public static void RefreshET()
        {
            var name = ET;

            var value = $"{EorzeaTime.Now.Hour:00}:00";

            if (SetVariable(name, value))
            {
                TimelineController.RaiseLog(
                    $"{TimelineConstants.LogSymbol} set VAR['{name}'] = {value}");
            }
        }

        /// <summary>
        /// カレントゾーンID
        /// </summary>
        public const string ZoneID = "ZONE_ID";

        /// <summary>
        /// カレントゾーン名
        /// </summary>
        public const string ZoneName = "ZONE_NAME";

        /// <summary>
        /// カレントゾーン情報 を更新する
        /// </summary>
        public static void RefreshZone()
        {
            var zoneID = XIVPluginHelper.Instance.GetCurrentZoneID();
            var zoneName = XIVPluginHelper.Instance.GetCurrentZoneName();

            if (SetVariable(ZoneID, zoneID))
            {
                SetVariable(ZoneName, zoneName);
                TimelineController.RaiseLog(
                    $"{TimelineConstants.LogSymbol} set VAR['{ZoneID}'] = {zoneID}");
                TimelineController.RaiseLog(
                    $"{TimelineConstants.LogSymbol} set VAR['{ZoneName}'] = {zoneName}");
            }
        }

        /// <summary>
        /// 1B Sign の基底値
        /// </summary>
        public const string Origin1B = "1B_ORIGIN";

        private static readonly Regex SignRegex = new Regex(
            @"^(1B|00:0000:Hojoring:1B):[0-9a-fA-F]{8}:.+:[0-9a-fA-F]{4}:[0-9a-fA-F]{4}:(?<sign_code>[0-9a-fA-F]{4}):");

        /// <summary>
        /// 1B Sign の最小値の更新を試みる
        /// </summary>
        /// <param name="logLine">
        /// ログ行</param>
        public static void TryRefresh1BSignOrigin(
            string logLine)
        {
            if (string.IsNullOrEmpty(logLine))
            {
                return;
            }

#if DEBUG
            if (logLine.Contains("1B:"))
            {
                Debug.WriteLine("TryRefresh1BSignOrigin");
            }
#endif

            if (!logLine.StartsWith("1B:") &&
                !logLine.StartsWith("00:0000:Hojoring:1B:"))
            {
                return;
            }

            var match = SignRegex.Match(logLine);
            if (!match.Success)
            {
                return;
            }

            var newSign = match.Groups["sign_code"].Value;

            if (!int.TryParse(
                newSign,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out int newSignValue))
            {
                return;
            }

            var currentSignValue = int.MaxValue;

            if (Variables.ContainsKey(Origin1B))
            {
                currentSignValue = (int)(Variables[Origin1B].Value ?? int.MaxValue);
            }

            if (newSignValue < currentSignValue)
            {
                TimelineController.RaiseLog(
                    $"{TimelineConstants.LogSymbol} set VAR['{Origin1B}'] = {newSignValue} ({newSignValue:X4})");

                SetVariable(Origin1B, newSignValue, TimelineController.CurrentController?.CurrentZoneName);
            }
        }

        /// <summary>
        /// グローバル変数をセットする
        /// </summary>
        /// <param name="name">グローバル変数名</param>
        /// <param name="value">値</param>
        /// <param name="zone"ゾーン名</param>
        /// <returns>is changed</returns>
        public static bool SetVariable(
            string name,
            object value,
            string zone = null)
        {
            var result = false;
            var variable = default(TimelineVariable);

            lock (ExpressionLocker)
            {
                if (Variables.ContainsKey(name))
                {
                    variable = Variables[name];
                }
                else
                {
                    variable = new TimelineVariable(name);
                    Variables[name] = variable;
                    result = true;
                }

                switch (value)
                {
                    case bool b:
                        if (!(variable.Value is bool current1) ||
                            current1 != b)
                        {
                            variable.Value = b;
                            variable.Expiration = DateTime.MaxValue;
                            result = true;
                        }
                        break;

                    case string s:
                        if (s.TryParse0xString2Int(out int i2))
                        {
                            if (!(variable.Value is int current2) ||
                                current2 != i2)
                            {
                                variable.Value = i2;
                                variable.Expiration = DateTime.MaxValue;
                                result = true;
                            }
                        }
                        else
                        {
                            goto default;
                        }
                        break;

                    default:
                        if (!ObjectComparer.Equals(value, variable.Value))
                        {
                            variable.Value = value;
                            variable.Expiration = DateTime.MaxValue;
                            result = true;
                        }
                        break;
                }

                variable.Zone = zone ?? string.Empty;
            }

            if (result)
            {
                OnVariableChanged?.Invoke(new EventArgs());

                if (ReferedTriggerRecompileDelegates.ContainsKey(variable.Name))
                {
                    lock (variable)
                    {
                        ReferedTriggerRecompileDelegates[variable.Name]?.Invoke();
                    }
                }
            }

            return result;
        }
    }
}
