using System;
using ACT.SpecialSpellTimer.RazorModel;
using FFXIV.Framework.Common;
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

            lock (ExpressionLocker)
            {
                var variable = default(TimelineVariable);
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
                        if (!(variable.Value is bool current) ||
                            current != b)
                        {
                            variable.Value = b;
                            variable.Expiration = DateTime.MaxValue;
                            result = true;
                        }
                        break;

                    case int i:
                        if (variable.Counter != i)
                        {
                            variable.Counter = i;
                            variable.Expiration = DateTime.MaxValue;
                            result = true;
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
            }

            return result;
        }
    }
}
