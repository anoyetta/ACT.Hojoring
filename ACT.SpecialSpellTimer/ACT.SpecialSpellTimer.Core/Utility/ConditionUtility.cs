using System;
using System.Text;
using System.Text.RegularExpressions;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Models;

namespace ACT.SpecialSpellTimer.Utility
{
    public class ConditionUtility
    {
        /// <summary>
        /// 指定されたSpellTimerの条件を確認する
        /// </summary>
        /// <param name="spell">SpellTimer</param>
        /// <returns>条件を満たしていればtrue</returns>
        public static bool CheckConditionsForSpell(
            Spell spell)
        {
            if (Settings.Default.DisableStartCondition)
            {
                return true;
            }

            return CheckConditions(
                spell.TimersMustRunningForStart,
                spell.TimersMustStoppingForStart);
        }

        /// <summary>
        /// 指定されたOnePointTelopの条件を確認する
        /// </summary>
        /// <param name="telop">OnePointTelop</param>
        /// <returns>条件を満たしていればtrue</returns>
        public static bool CheckConditionsForTelop(
            Ticker telop)
        {
            if (Settings.Default.DisableStartCondition)
            {
                return true;
            }

            return CheckConditions(
                telop.TimersMustRunningForStart,
                telop.TimersMustStoppingForStart);
        }

        /// <summary>
        /// 後方参照を置換したMessageを返す
        /// </summary>
        /// <param name="telop">OnePointTelop</param>
        /// <returns>置換後のMessage</returns>
        public static string GetReplacedMessage(
            Ticker telop)
        {
            var builder = new StringBuilder(telop.Message);

            int index = 1;
            index += ReplaceMessageWithSpell(builder, telop.TimersMustRunningForStart, index);
            index += ReplaceMessageWithSpell(builder, telop.TimersMustStoppingForStart, index);
            index += ReplaceMessageWithTelop(builder, telop.TimersMustRunningForStart, index);
            index += ReplaceMessageWithTelop(builder, telop.TimersMustStoppingForStart, index);

            return builder.ToString();
        }

        /// <summary>
        /// 後方参照を置換したTitleを返す
        /// </summary>
        /// <param name="spell">SpellTimer</param>
        /// <returns>置換後のTitle</returns>
        public static string GetReplacedTitle(
            Spell spell)
        {
            var builder = new StringBuilder(spell.SpellTitle);

            int index = 1;
            index += ReplaceMessageWithSpell(builder, spell.TimersMustRunningForStart, index);
            index += ReplaceMessageWithSpell(builder, spell.TimersMustStoppingForStart, index);
            index += ReplaceMessageWithTelop(builder, spell.TimersMustRunningForStart, index);
            index += ReplaceMessageWithTelop(builder, spell.TimersMustStoppingForStart, index);

            return builder.ToString();
        }

        /// <summary>
        /// 指定された全てのTimerが条件を満たしているか確認する
        /// </summary>
        /// <param name="timersMustRunning">稼働中であることが求められているTimerの配列</param>
        /// <param name="timersMustStopping">停止中であることが求められているTimerの配列</param>
        /// <returns>全てのTimerが条件を満たしていればtrue</returns>
        private static bool CheckConditions(
            Guid[] timersMustRunning,
            Guid[] timersMustStopping)
        {
            if (timersMustRunning.Length == 0 && 
                timersMustStopping.Length == 0)
            {
                return true;
            }

            // 動作中か確認する
            var condition = true;
            foreach (var guid in timersMustRunning)
            {
                var spell = SpellTable.Instance.GetSpellTimerByGuid(guid);
                if (spell != null)
                {
                    condition = condition & IsRunning(spell);
                }

                var telop = TickerTable.Instance.GetOnePointTelopByGuid(guid);
                if (telop != null)
                {
                    condition = condition & IsRunning(telop);
                }
            }

            // 停止中か確認する（稼働中でなければ停止中として扱う）
            foreach (var guid in timersMustStopping)
            {
                var spell = SpellTable.Instance.GetSpellTimerByGuid(guid);
                if (spell != null)
                {
                    condition = condition & !IsRunning(spell);
                }

                var telop = TickerTable.Instance.GetOnePointTelopByGuid(guid);
                if (telop != null)
                {
                    condition = condition & !IsRunning(telop);
                }
            }

            return condition;
        }

        /// <summary>
        /// 指定されたSpellTimerが稼働中か判定する
        /// </summary>
        /// <param name="spell">SpellTimer</param>
        /// <returns>稼働中であればtrue</returns>
        private static bool IsRunning(
            Spell spell)
        {
            var recastTime = (spell.CompleteScheduledTime - DateTime.Now).TotalSeconds;
            return recastTime >= 0;
        }

        /// <summary>
        /// 指定されたOnePointTelopが稼働中か判定する
        /// Delayがある場合はDelay経過後から稼働中として扱う
        /// </summary>
        /// <param name="telop">OnePointTelop</param>
        /// <returns>稼働中であればtrue</returns>
        private static bool IsRunning(
            Ticker telop)
        {
            if (telop.MatchDateTime > DateTime.MinValue && !telop.ForceHide)
            {
                var start = telop.MatchDateTime.AddSeconds(telop.Delay);
                var end = telop.MatchDateTime.AddSeconds(telop.Delay + telop.DisplayTime);

                if (start <= DateTime.Now && DateTime.Now <= end)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 指定されたGuidに対応するSpellTimerの後方参照で置換を行う
        /// 置換文字列は "$C" + 条件の出現順（1～） + "-" + 後方参照の番号
        /// 例： $C1-2（一つ目の条件となるタイマーの二つ目のグループに置換）
        /// </summary>
        /// <param name="message">元の文字列</param>
        /// <param name="timers">置換候補のタイマー</param>
        /// <param name="baseIndex">条件の出現順の開始番号</param>
        /// <returns>候補となったタイマーの個数</returns>
        private static int ReplaceMessageWithSpell(
            StringBuilder message,
            Guid[] timers,
            int baseIndex)
        {
            int count = 0;

            for (int i = 0; i < timers.Length; i++)
            {
                var spell = SpellTable.Instance.GetSpellTimerByGuid(timers[i]);
                if (spell != null)
                {
                    count++;

                    if (spell.RegexEnabled && spell.Regex != null && spell.MatchedLog != string.Empty)
                    {
                        foreach (Match match in spell.Regex.Matches(spell.MatchedLog))
                        {
                            foreach (var number in spell.Regex.GetGroupNumbers())
                            {
                                message.Replace(String.Format("$C{0}-{1}", (baseIndex + i), number), match.Groups[number].Value);
                            }
                        }
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// 指定されたGuidに対応するOnePointTelopの後方参照で置換を行う
        /// 置換文字列は "$C" + 条件の出現順（1～） + "-" + 後方参照の番号
        /// 例： $C1-2（一つ目の条件となるタイマーの二つ目のグループに置換）
        /// </summary>
        /// <param name="message">元の文字列</param>
        /// <param name="timers">置換候補のタイマー</param>
        /// <param name="baseIndex">条件の出現順の開始番号</param>
        /// <returns>候補となったタイマーの個数</returns>
        private static int ReplaceMessageWithTelop(
            StringBuilder message,
            Guid[] timers,
            int baseIndex)
        {
            int count = 0;

            for (int i = 0; i < timers.Length; i++)
            {
                var telop = TickerTable.Instance.GetOnePointTelopByGuid(timers[i]);
                if (telop != null)
                {
                    count++;

                    if (telop != null && telop.RegexEnabled && telop.Regex != null && !string.IsNullOrEmpty(telop.MatchedLog))
                    {
                        foreach (Match match in telop.Regex.Matches(telop.MatchedLog))
                        {
                            foreach (var number in telop.Regex.GetGroupNumbers())
                            {
                                message.Replace(String.Format("$C{0}-{1}", (baseIndex + i), number), match.Groups[number].Value);
                            }
                        }
                    }
                }
            }

            return count;
        }
    }
}
